using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using LethalCompanyAI.Bracken;
using LethalCompanyAI.CoilHead;
using LethalCompanyAI.EyelessDog;
using LethalCompanyAI.Nutcracker;
using LethalCompanyAI.Thumper;

namespace LethalCompanyAI
{
    public enum MonsterType
    {
        Bracken,
        CoilHead,
        EyelessDog,
        Nutcracker,
        Thumper
    }

    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class MonsterController : MonoBehaviour
    {
        [Header("Enemy / Player")]
        public MonsterType enemyType = MonsterType.Bracken;
        public string playerTag = "Player";
        [Tooltip("Optional camera/eye transform used to determine whether the player is looking at this enemy.")]
        public Transform playerView;

        [Header("Detection")]
        public float viewDistance = 18f;
        [Range(1f, 179f)] public float fieldOfView = 100f;
        public float eyeHeight = 1.4f;
        public float playerTargetHeight = 1f;
        [Tooltip("Include the player and all vision-blocking geometry in this mask.")]
        public LayerMask lineOfSightMask = ~0;
        [Tooltip("Geometry that blocks a straight-line charge. Player and this enemy are ignored automatically.")]
        public LayerMask obstacleMask = ~0;

        [Header("Patrol")]
        public Transform[] patrolPoints;
        public float wanderRadius = 9f;
        public float patrolArrivalDistance = 1.4f;

        [Header("General Movement Speeds")]
        public float patrolSpeed = 2f;
        public float chaseSpeed = 7f;

        [Header("Bracken")]
        public float brackenLookAngle = 35f;
        public float brackenLookToRetreat = 0.15f;
        public float brackenStareToEnrage = 2.2f;
        public float brackenStalkSpeed = 2.2f;
        public float brackenRetreatSpeed = 4f;
        public float brackenRetreatFreezeTime = 0.45f;
        public float brackenRetreatDuration = 2.5f;
        public float brackenAttackSpeed = 8f;
        public float brackenAttackDistance = 1.6f;
        public float brackenAttackDuration = 3f;
        public float brackenPostAttackPause = 0.8f;
        public float brackenTrailUpdateInterval = 0.5f;
        public float brackenTrailOffset = 2.5f;
        public UnityEvent onBrackenAttack = new UnityEvent();

        [Header("Coil-Head")]
        public float coilLookAngle = 30f;
        public float coilPursuitSpeed = 12f;
        public float coilChaseDuration = 8f;
        public float coilRechargeDuration = 3f;

        [Header("Eyeless Dog")]
        [Range(0f, 1f)] public float dogHearingThreshold = 0.2f;
        [Range(0f, 1f)] public float dogChargeLoudnessThreshold = 0.75f;
        public float dogNoiseMemoryDuration = 8f;
        public float dogRoamSpeed = 2.5f;
        public float dogInvestigateSpeed = 4f;
        public float dogChargeStartSpeed = 7f;
        public float dogChargeAcceleration = 5f;
        public float dogChargeMaxSpeed = 16f;
        public float dogChargeMaxDuration = 3.5f;
        public float dogChargeMaxDistance = 22f;

        [Header("Nutcracker")]
        public float nutcrackerScanInterval = 5f;
        public float nutcrackerScanDuration = 3f;
        public float nutcrackerScanAngle = 140f;
        public float nutcrackerScanTurnSpeed = 90f;
        public float nutcrackerMovementThreshold = 0.15f;
        public float nutcrackerShootingRange = 22f;
        public int nutcrackerShotsPerBurst = 2;
        public float nutcrackerFireInterval = 0.55f;
        public float nutcrackerReloadDuration = 1.8f;
        public UnityEvent onNutcrackerShot = new UnityEvent();

        [Header("Thumper")]
        [Range(1f, 179f)] public float thumperFieldOfView = 55f;
        public float thumperChargeStartSpeed = 4f;
        public float thumperChargeAcceleration = 8f;
        public float thumperChargeMaxSpeed = 14f;
        public float thumperWallCheckDistance = 1.2f;
        public float thumperChargeMaxDuration = 5f;
        public float thumperChargeMaxDistance = 28f;
        public float thumperStuckTime = 0.35f;
        public float thumperStuckMovementThreshold = 0.04f;
        public float thumperStunDuration = 2f;

        private NavMeshAgent agent;
        private Animator animator;
        private Transform player;
        private State currentState;
        private int patrolIndex;
        private float playerSpeed;
        private Vector3 lastPlayerPosition;
        private float playerSearchTimer;
        private bool invalidTagWarningShown;

        private int noiseVersion;
        private Vector3 lastNoisePosition;
        private float lastNoiseLoudness;
        private float lastNoiseTime = -1000f;
        private bool wasAttacked;
        private float brackenStareTimer;

        public Transform Player { get { return player; } }
        public float PlayerSpeed { get { return playerSpeed; } }
        public float BrackenStareTimer { get { return brackenStareTimer; } }
        public int NoiseVersion { get { return noiseVersion; } }

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            if (agent == null || animator == null)
            {
                Debug.LogError(name + ": MonsterController requires a NavMeshAgent and Animator.", this);
                enabled = false;
                return;
            }

            FindPlayer();
            lastPlayerPosition = player != null ? player.position : Vector3.zero;
            currentState = CreateStartingState();
        }

        private void Update()
        {
            if (currentState == null)
                return;

            playerSearchTimer += Time.deltaTime;
            if (player == null && playerSearchTimer >= 1f)
            {
                playerSearchTimer = 0f;
                FindPlayer();
            }

            UpdatePlayerMovement();
            currentState = currentState.Process();
        }

        private State CreateStartingState()
        {
            switch (enemyType)
            {
                case MonsterType.Bracken:
                    return new Stalking(gameObject, agent, animator, player);
                case MonsterType.CoilHead:
                    return new Patrol(gameObject, agent, animator, player);
                case MonsterType.EyelessDog:
                    return new Roaming(gameObject, agent, animator, player);
                case MonsterType.Nutcracker:
                    return new Wandering(gameObject, agent, animator, player);
                case MonsterType.Thumper:
                    return new Patrolling(gameObject, agent, animator, player);
                default:
                    return new Stalking(gameObject, agent, animator, player);
            }
        }

        private void FindPlayer()
        {
            if (string.IsNullOrEmpty(playerTag))
                return;

            try
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObject != null)
                {
                    player = playerObject.transform;
                    if (playerView == null)
                        playerView = player;
                    lastPlayerPosition = player.position;
                }
            }
            catch (UnityException)
            {
                if (!invalidTagWarningShown)
                {
                    Debug.LogWarning(name + ": Player Tag '" + playerTag + "' does not exist. Add it in Project Settings > Tags and Layers.", this);
                    invalidTagWarningShown = true;
                }
            }
        }

        private void UpdatePlayerMovement()
        {
            if (player == null)
            {
                playerSpeed = 0f;
                return;
            }

            if (Time.deltaTime > 0f)
                playerSpeed = Vector3.Distance(player.position, lastPlayerPosition) / Time.deltaTime;
            lastPlayerPosition = player.position;
        }

        // Call this from a sound emitter / player voice system for each heard sound.
        // Loudness is normalized to 0..1. Large values can trigger a blind charge.
        public void ReportNoise(Vector3 worldPosition, float loudness)
        {
            noiseVersion++;
            lastNoisePosition = worldPosition;
            lastNoiseLoudness = Mathf.Clamp01(loudness);
            lastNoiseTime = Time.time;
        }

        public bool TryGetNewNoise(ref int seenVersion, float minimumLoudness,
            out Vector3 position, out float loudness)
        {
            position = Vector3.zero;
            loudness = 0f;

            if (noiseVersion == seenVersion)
                return false;

            seenVersion = noiseVersion;
            if (Time.time - lastNoiseTime > dogNoiseMemoryDuration || lastNoiseLoudness < minimumLoudness)
                return false;

            position = lastNoisePosition;
            loudness = lastNoiseLoudness;
            return true;
        }

        public bool TryGetLatestNoise(out Vector3 position, out float loudness)
        {
            position = lastNoisePosition;
            loudness = lastNoiseLoudness;
            return noiseVersion > 0 && Time.time - lastNoiseTime <= dogNoiseMemoryDuration;
        }

        // Call this from damage/impact handling when the Bracken is attacked.
        public void NotifyAttacked()
        {
            wasAttacked = true;
        }

        public bool ConsumeAttackedFlag()
        {
            bool result = wasAttacked;
            wasAttacked = false;
            return result;
        }

        public void UpdateBrackenStare(bool playerLooking)
        {
            if (playerLooking)
                brackenStareTimer += Time.deltaTime;
            else
                brackenStareTimer = 0f;
        }

        public void ResetBrackenStare()
        {
            brackenStareTimer = 0f;
        }

        public void InvokeBrackenAttack()
        {
            if (onBrackenAttack != null)
                onBrackenAttack.Invoke();
        }

        public void InvokeNutcrackerShot()
        {
            if (onNutcrackerShot != null)
                onNutcrackerShot.Invoke();
        }

        public bool CanSeePlayer()
        {
            return CanSeePlayer(fieldOfView);
        }

        public bool CanSeePlayer(float fov)
        {
            if (player == null)
                return false;

            Vector3 target = player.position + Vector3.up * playerTargetHeight;
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = target - origin;
            float distance = toPlayer.magnitude;

            if (distance > viewDistance || distance <= 0.01f)
                return false;

            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > fov * 0.5f)
                return false;

            return HasClearPath(origin, target, player);
        }

        public bool IsPlayerLookingAtMonster(float viewAngle)
        {
            if (player == null)
                return false;

            Transform view = playerView != null ? playerView : player;
            Vector3 target = transform.position + Vector3.up * eyeHeight;
            Vector3 direction = target - view.position;
            float distance = direction.magnitude;

            if (distance > viewDistance || distance <= 0.01f)
                return false;

            if (Vector3.Angle(view.forward, direction) > viewAngle * 0.5f)
                return false;

            return HasClearPath(view.position, target, transform);
        }

        private bool HasClearPath(Vector3 origin, Vector3 target, Transform expectedTarget)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return true;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance + 0.1f,
                lineOfSightMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, delegate (RaycastHit a, RaycastHit b)
            {
                return a.distance.CompareTo(b.distance);
            });

            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;

                bool hitPlayer = player != null &&
                    (hitTransform == player || hitTransform.IsChildOf(player));
                if (hitPlayer)
                {
                    if (expectedTarget == player || (expectedTarget != null && expectedTarget.IsChildOf(player)))
                        return true;
                    // Ignore the observer's own colliders when the ray starts at the player's view.
                    continue;
                }

                if (expectedTarget != null &&
                    (hitTransform == expectedTarget || hitTransform.IsChildOf(expectedTarget)))
                    return true;

                return false;
            }

            // If the target has no collider (or its layer is not in the mask),
            // an unobstructed ray is still considered clear.
            return true;
        }

        public bool IsPlayerMoving(float minimumSpeed)
        {
            return player != null && playerSpeed >= minimumSpeed;
        }

        public bool TryGetPatrolDestination(out Vector3 destination)
        {
            destination = transform.position;

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                for (int attempt = 0; attempt < patrolPoints.Length; attempt++)
                {
                    Transform point = patrolPoints[patrolIndex % patrolPoints.Length];
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    if (point == null)
                        continue;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(point.position, out hit, 2f, NavMesh.AllAreas))
                    {
                        destination = hit.position;
                        return true;
                    }
                }
            }

            return TryGetRandomNavMeshPoint(transform.position, wanderRadius, out destination);
        }

        public bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 destination)
        {
            destination = transform.position;
            for (int i = 0; i < 12; i++)
            {
                Vector3 candidate = center + Random.insideUnitSphere * radius;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidate, out hit, radius, NavMesh.AllAreas))
                {
                    destination = hit.position;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetRetreatDestination(Vector3 threatPosition, out Vector3 destination)
        {
            Vector3 away = transform.position - threatPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = -transform.forward;
            away.Normalize();

            Vector3 candidate = transform.position + away * Mathf.Max(4f, wanderRadius * 0.75f);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, wanderRadius, NavMesh.AllAreas))
            {
                destination = hit.position;
                return true;
            }

            return TryGetRandomNavMeshPoint(transform.position + away * 3f, wanderRadius, out destination);
        }

        public bool HasObstacleAhead(Vector3 direction, float distance)
        {
            if (direction.sqrMagnitude < 0.001f)
                return false;

            Vector3 origin = transform.position + Vector3.up * 0.65f;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance,
                obstacleMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform))
                    continue;
                if (player != null && (hitTransform == player || hitTransform.IsChildOf(player)))
                    continue;
                return true;
            }

            return false;
        }
    }
}
