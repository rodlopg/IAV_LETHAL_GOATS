using System;
using UnityEngine;
using UnityEngine.Events;

namespace LethalCompanyAI
{
    /// <summary>
    /// Three-tone whistle (Low = F, Mid = G, High = H). Put this on the Player next to
    /// PlayerMonsterIntegration. Each whistle plays its own sound and emits a noise through
    /// PlayerMonsterIntegration.EmitNoise, which GameManager forwards to Eyeless Dogs in range.
    /// Higher whistles are louder and carry further, so they are riskier.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerWhistle : MonoBehaviour
    {
        public enum WhistleTone { Low = 0, Mid = 1, High = 2 }

        [Serializable]
        public class WhistleSettings
        {
            public string name = "Whistle";
            public KeyCode key = KeyCode.F;
            [Range(0f, 1f)] public float loudness = 0.5f;
            [Min(0f)] public float range = 25f;
            [Tooltip("Time before ANY whistle can be used again after this one.")]
            [Min(0f)] public float cooldown = 2f;
            public AudioClip sound;
            [Range(0f, 1f)] public float volume = 1f;

            public WhistleSettings(string name, KeyCode key, float loudness, float range, float cooldown)
            {
                this.name = name;
                this.key = key;
                this.loudness = loudness;
                this.range = range;
                this.cooldown = cooldown;
            }
        }

        [Header("Whistles (index 0 = Low, 1 = Mid, 2 = High)")]
        public WhistleSettings[] whistles = new WhistleSettings[]
        {
            new WhistleSettings("Low",  KeyCode.F, 0.35f, 20f, 1.5f),
            new WhistleSettings("Mid",  KeyCode.G, 0.65f, 32f, 2.5f),
            new WhistleSettings("High", KeyCode.H, 1.00f, 45f, 4.0f),
        };

        [Header("Audio")]
        [Tooltip("Optional. If empty, clips are played at the player's position.")]
        public AudioSource audioSource;

        [Header("Events")]
        [Tooltip("Invoked with the tone index: 0 = Low, 1 = Mid, 2 = High.")]
        public UnityEvent<int> onWhistle = new UnityEvent<int>();

        public float CooldownRemaining { get; private set; }
        public bool IsReady { get { return CooldownRemaining <= 0f; } }
        public WhistleTone LastTone { get; private set; }
        /// <summary>1 = ready, 0 = just used. Handy for a UI bar.</summary>
        public float Ready01 { get { return cooldownTotal <= 0f ? 1f : 1f - Mathf.Clamp01(CooldownRemaining / cooldownTotal); } }

        private PlayerMonsterIntegration player;
        private PlayerController controller;
        private float cooldownTotal;

        private void Awake()
        {
            player = GetComponent<PlayerMonsterIntegration>();
            controller = GetComponent<PlayerController>();

            if (player == null)
                Debug.LogError("PlayerWhistle: PlayerMonsterIntegration not found on the Player.", this);
        }

        private void Update()
        {
            if (CooldownRemaining > 0f)
                CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);

            if (player == null || !player.IsAlive || whistles == null)
                return;

            // Ignore input while the end menu is open.
            if (controller != null && !controller.InputEnabled)
                return;

            for (int i = 0; i < whistles.Length; i++)
            {
                if (whistles[i] != null && Input.GetKeyDown(whistles[i].key))
                {
                    Whistle(i);
                    break; // one whistle per frame
                }
            }
        }

        public void Whistle(WhistleTone tone)
        {
            Whistle((int)tone);
        }

        /// <summary>Index 0 = Low, 1 = Mid, 2 = High. Can also be called from UI buttons.</summary>
        public void Whistle(int index)
        {
            if (player == null || !player.IsAlive || !IsReady)
                return;

            if (whistles == null || index < 0 || index >= whistles.Length || whistles[index] == null)
                return;

            WhistleSettings settings = whistles[index];

            cooldownTotal = settings.cooldown;
            CooldownRemaining = settings.cooldown;
            LastTone = (WhistleTone)Mathf.Clamp(index, 0, 2);

            if (settings.sound != null)
            {
                if (audioSource != null)
                    audioSource.PlayOneShot(settings.sound, settings.volume);
                else
                    AudioSource.PlayClipAtPoint(settings.sound, transform.position, settings.volume);
            }

            player.EmitNoise(settings.loudness, settings.range);
            onWhistle.Invoke(index);
        }
    }
}