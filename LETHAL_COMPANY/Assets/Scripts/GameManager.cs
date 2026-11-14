using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LethalCompanyAI
{
    /// <summary>
    /// Connects the PlayerMonsterIntegration component to the existing MonsterController
    /// instances. Add this component to one GameObject in the scene named GameManager.
    /// The monster FSMs continue to run themselves; this script only wires shared gameplay
    /// interactions (player view, Dog sound events, and player damage).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        [Header("Player")]
        public string playerTag = "Player";
        [Tooltip("Optional. If empty, the GameManager searches for the Player Tag.")]
        public PlayerMonsterIntegration player;

        [Header("Monster Discovery")]
        [Tooltip("Optional manual list. Scene MonsterControllers are also discovered automatically.")]
        public MonsterController[] additionalMonsters;

        [Header("Damage From Monster Events")]
        [Min(0)] public int brackenDamage = 100;
        [Min(0)] public int nutcrackerShotDamage = 45;
        [Min(0)] public int nutcrackerMeleeDamage = 35;

        [Header("Contact Damage")]
        [Tooltip("Coil-Head, Eyeless Dog, and Thumper have no attack UnityEvent in the existing FSM scripts, so contact is checked here.")]
        public bool enableContactDamage = true;
        [Min(0.1f)] public float contactDamageDistance = 1.45f;
        [Min(0.1f)] public float contactDamageCooldown = 1f;
        [Min(0)] public int coilHeadContactDamage = 100;
        [Min(0)] public int eyelessDogContactDamage = 100;
        [Min(0)] public int thumperContactDamage = 50;

        [Header("Game Over")]
        public bool pauseGameOnPlayerDeath = false;
        public UnityEvent onGameOver = new UnityEvent();

        public bool IsGameOver { get { return gameOver; } }
        public PlayerMonsterIntegration Player { get { return player; } }

        private readonly List<MonsterController> monsters = new List<MonsterController>();
        private readonly HashSet<MonsterController> registeredMonsters = new HashSet<MonsterController>();
        private readonly Dictionary<MonsterController, UnityAction> brackenHandlers = new Dictionary<MonsterController, UnityAction>();
        private readonly Dictionary<MonsterController, UnityAction> nutcrackerHandlers = new Dictionary<MonsterController, UnityAction>();
        private readonly Dictionary<MonsterController, UnityAction> nutcrackerMeleeHandlers = new Dictionary<MonsterController, UnityAction>();
        private readonly Dictionary<MonsterController, float> nextContactDamageTime = new Dictionary<MonsterController, float>();

        private float playerSearchTimer;
        private bool gameOver;

        private void Awake()
        {
            FindPlayer();
            RefreshMonsterReferences();
            BindPlayer();
            ConfigureMonsterReferences();
        }

        private void Start()
        {
            // Repeat once in Start as a safety net for scene objects initialized by other scripts.
            if (player == null)
                FindPlayer();

            BindPlayer();
            RefreshMonsterReferences();
            ConfigureMonsterReferences();
        }

        private void Update()
        {
            if (player == null)
            {
                playerSearchTimer += Time.deltaTime;
                if (playerSearchTimer >= 1f)
                {
                    playerSearchTimer = 0f;
                    FindPlayer();
                    BindPlayer();
                    ConfigureMonsterReferences();
                }
            }

            if (gameOver || player == null || !player.IsAlive || !enableContactDamage)
                return;

            UpdateContactDamage();
        }

        private void FindPlayer()
        {
            if (player != null || string.IsNullOrEmpty(playerTag))
                return;

            try
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObject != null)
                    player = playerObject.GetComponent<PlayerMonsterIntegration>();

                if (playerObject != null && player == null)
                {
                    Debug.LogWarning("The object tagged '" + playerTag + "' has no PlayerMonsterIntegration component.", playerObject);
                }
            }
            catch (UnityException)
            {
                Debug.LogError("GameManager: Player Tag '" + playerTag + "' does not exist. Create it in Project Settings > Tags and Layers.", this);
            }
        }

        private void BindPlayer()
        {
            // Avoid subscribing twice when Start follows Awake or player discovery happens later.
            if (player == null)
                return;

            player.NoiseEmitted -= HandlePlayerNoise;
            player.Died -= HandlePlayerDied;
            player.NoiseEmitted += HandlePlayerNoise;
            player.Died += HandlePlayerDied;

            // MonsterController searches by tag in Start(), so make sure the assigned player has it.
            try
            {
                if (player.gameObject.tag != playerTag)
                    player.gameObject.tag = playerTag;
            }
            catch (UnityException)
            {
                Debug.LogError("GameManager: Create the tag '" + playerTag + "' and assign it to the Player.", player);
            }
        }

        /// <summary>
        /// Finds all MonsterControllers in the scene and adds the optional Inspector list.
        /// Call this again after spawning monsters at runtime.
        /// </summary>
        public void RefreshMonsterReferences()
        {
            MonsterController[] sceneMonsters = FindObjectsByType<MonsterController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < sceneMonsters.Length; i++)
                RegisterMonster(sceneMonsters[i]);

            if (additionalMonsters != null)
            {
                for (int i = 0; i < additionalMonsters.Length; i++)
                    RegisterMonster(additionalMonsters[i]);
            }
        }

        private void RegisterMonster(MonsterController monster)
        {
            if (monster == null || !registeredMonsters.Add(monster))
                return;

            monsters.Add(monster);
            monster.playerTag = playerTag;

            if (player != null)
                monster.playerView = player.ViewTransform;

            if (monster.enemyType == MonsterType.Bracken)
            {
                UnityAction handler = delegate { DamagePlayer(brackenDamage, "Bracken"); };
                monster.onBrackenAttack.AddListener(handler);
                brackenHandlers.Add(monster, handler);
            }
            else if (monster.enemyType == MonsterType.Nutcracker)
            {
                // Shot event is invoked by NutcrackerProjectile only when a bullet hits the player.
                UnityAction shotHandler = delegate { DamagePlayer(nutcrackerShotDamage, "Nutcracker shotgun"); };
                monster.onNutcrackerShot.AddListener(shotHandler);
                nutcrackerHandlers.Add(monster, shotHandler);

                UnityAction meleeHandler = delegate { DamagePlayer(nutcrackerMeleeDamage, "Nutcracker melee"); };
                monster.onNutcrackerMelee.AddListener(meleeHandler);
                nutcrackerMeleeHandlers.Add(monster, meleeHandler);
            }
        }

        private void ConfigureMonsterReferences()
        {
            if (player == null)
                return;

            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterController monster = monsters[i];
                if (monster == null)
                    continue;

                monster.playerTag = playerTag;
                monster.playerView = player.ViewTransform;
            }
        }

        private void HandlePlayerNoise(Vector3 position, float loudness, float range)
        {
            if (gameOver)
                return;

            float rangeSquared = range * range;
            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterController monster = monsters[i];
                if (monster == null || !monster.isActiveAndEnabled || monster.enemyType != MonsterType.EyelessDog)
                    continue;

                if ((monster.transform.position - position).sqrMagnitude <= rangeSquared)
                    monster.ReportNoise(position, loudness);
            }
        }

        private void UpdateContactDamage()
        {
            Vector3 playerPosition = player.transform.position;
            float contactDistanceSquared = contactDamageDistance * contactDamageDistance;

            for (int i = 0; i < monsters.Count; i++)
            {
                MonsterController monster = monsters[i];
                if (monster == null || !monster.isActiveAndEnabled)
                    continue;

                int damage;
                string source;
                switch (monster.enemyType)
                {
                    case MonsterType.CoilHead:
                        damage = coilHeadContactDamage;
                        source = "Coil-Head";
                        break;
                    case MonsterType.EyelessDog:
                        damage = eyelessDogContactDamage;
                        source = "Eyeless Dog";
                        break;
                    case MonsterType.Thumper:
                        damage = thumperContactDamage;
                        source = "Thumper";
                        break;
                    default:
                        continue; // Bracken and Nutcracker damage is driven by their existing UnityEvents.
                }

                if ((monster.transform.position - playerPosition).sqrMagnitude > contactDistanceSquared)
                    continue;

                float nextTime;
                if (nextContactDamageTime.TryGetValue(monster, out nextTime) && Time.time < nextTime)
                    continue;

                nextContactDamageTime[monster] = Time.time + contactDamageCooldown;
                DamagePlayer(damage, source);

                if (gameOver)
                    return;
            }
        }

        private void DamagePlayer(int amount, string source)
        {
            if (gameOver || player == null || !player.IsAlive)
                return;

            player.TakeDamage(amount, source);
        }

        private void HandlePlayerDied()
        {
            if (gameOver)
                return;

            gameOver = true;
            onGameOver.Invoke();

            if (pauseGameOnPlayerDeath)
                Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.NoiseEmitted -= HandlePlayerNoise;
                player.Died -= HandlePlayerDied;
            }

            foreach (KeyValuePair<MonsterController, UnityAction> pair in brackenHandlers)
            {
                if (pair.Key != null)
                    pair.Key.onBrackenAttack.RemoveListener(pair.Value);
            }

            foreach (KeyValuePair<MonsterController, UnityAction> pair in nutcrackerHandlers)
            {
                if (pair.Key != null)
                    pair.Key.onNutcrackerShot.RemoveListener(pair.Value);
            }

            foreach (KeyValuePair<MonsterController, UnityAction> pair in nutcrackerMeleeHandlers)
            {
                if (pair.Key != null)
                    pair.Key.onNutcrackerMelee.RemoveListener(pair.Value);
            }

            if (pauseGameOnPlayerDeath && gameOver)
                Time.timeScale = 1f;
        }
    }
}
