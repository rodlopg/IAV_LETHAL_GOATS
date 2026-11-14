using System;
using UnityEngine;
using UnityEngine.Events;

namespace LethalCompanyAI
{
    /// <summary>
    /// Add this component to the Player object alongside your existing movement script.
    /// It exposes the player's view transform, reports movement noise to the GameManager,
    /// and provides a simple health/damage API for monster attacks.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public class PlayerMonsterIntegration : MonoBehaviour
    {
        [Header("Player View")]
        [Tooltip("Assign the player's camera or eye transform. If empty, a child Camera is detected automatically.")]
        [SerializeField] private Transform viewTransform;
        [SerializeField] private Camera viewCamera;

        [Header("Footstep Noise - Eyeless Dog")]
        [Min(0f)] public float walkingSpeedThreshold = 0.15f;
        [Min(0f)] public float runningSpeedThreshold = 4f;
        [Min(0.05f)] public float walkingStepInterval = 0.65f;
        [Min(0.05f)] public float runningStepInterval = 0.35f;
        [Range(0f, 1f)] public float walkingNoiseLoudness = 0.35f;
        [Range(0f, 1f)] public float runningNoiseLoudness = 0.85f;
        [Min(0f)] public float footstepNoiseRange = 28f;
        [Tooltip("When a CharacterController is present, only emit automatic footsteps while grounded.")]
        public bool footstepsOnlyWhenGrounded = true;

        [Header("Health")]
        [Min(1)] public int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        public UnityEvent<int> onHealthChanged = new UnityEvent<int>();
        public UnityEvent onDeath = new UnityEvent();

        public event Action<Vector3, float, float> NoiseEmitted;
        public event Action Died;

        public Transform ViewTransform { get; private set; }
        public int CurrentHealth { get { return currentHealth; } }
        public bool IsAlive { get { return currentHealth > 0; } }
        public float MovementSpeed { get; private set; }

        private CharacterController characterController;
        private Vector3 previousPosition;
        private float stepTimer;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (viewTransform == null)
            {
                if (viewCamera == null)
                    viewCamera = GetComponentInChildren<Camera>();

                if (viewCamera == null && Camera.main != null)
                    viewCamera = Camera.main;

                viewTransform = viewCamera != null ? viewCamera.transform : transform;
            }

            ViewTransform = viewTransform;
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
            previousPosition = transform.position;
        }

        private void Update()
        {
            UpdateMovementNoise();
        }

        private void UpdateMovementNoise()
        {
            Vector3 currentPosition = transform.position;
            Vector3 movement = currentPosition - previousPosition;
            movement.y = 0f;
            previousPosition = currentPosition;

            if (Time.deltaTime <= 0f)
                return;

            MovementSpeed = movement.magnitude / Time.deltaTime;

            if (!IsAlive)
                return;

            if (footstepsOnlyWhenGrounded && characterController != null && !characterController.isGrounded)
            {
                stepTimer = 0f;
                return;
            }

            if (MovementSpeed < walkingSpeedThreshold)
            {
                stepTimer = 0f;
                return;
            }

            bool running = MovementSpeed >= runningSpeedThreshold;
            float interval = running ? runningStepInterval : walkingStepInterval;
            stepTimer += Time.deltaTime;

            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                float loudness = running ? runningNoiseLoudness : walkingNoiseLoudness;
                EmitNoise(loudness, footstepNoiseRange);
            }
        }

        /// <summary>
        /// Manually emit a sound, for example a shout, landing, door slam, or interaction.
        /// Loudness is normalized from 0 to 1. Values at/above the Dog's charge threshold
        /// can trigger its BlindCharging state when the sound is within hearing range.
        /// </summary>
        public void EmitNoise(float loudness, float range = -1f)
        {
            if (!IsAlive)
                return;

            float resolvedRange = range > 0f ? range : footstepNoiseRange;
            NoiseEmitted?.Invoke(transform.position, Mathf.Clamp01(loudness), resolvedRange);
        }

        /// <summary>Called by GameManager when a monster successfully attacks the player.</summary>
        public void TakeDamage(int amount, string source = "Unknown")
        {
            if (!IsAlive || amount <= 0)
                return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            onHealthChanged.Invoke(currentHealth);

            if (currentHealth == 0)
            {
                Debug.Log("Player defeated by " + source + ".", this);
                onDeath.Invoke();
                Died?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0)
                return;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            if (currentHealth != oldHealth)
                onHealthChanged.Invoke(currentHealth);
        }

        public void SetViewTransform(Transform newViewTransform)
        {
            if (newViewTransform == null)
                return;

            viewTransform = newViewTransform;
            ViewTransform = newViewTransform;
        }
    }
}
