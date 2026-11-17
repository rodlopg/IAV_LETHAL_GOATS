using UnityEngine;

namespace LethalCompanyAI
{
    /// <summary>
    /// Slender-style first person controller: slow walk, short stamina-limited sprint,
    /// no jumping, heavy head bob. Put it on the Player next to CharacterController and
    /// PlayerMonsterIntegration. Footstep noise for the Eyeless Dog is produced by
    /// PlayerMonsterIntegration, which measures our movement speed (sprint speed must stay
    /// above its runningSpeedThreshold of 4 to count as "running").
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The camera (or a parent of it) that is a child of the Player. Auto-detected if empty.")]
        public Transform cameraPivot;

        [Header("Movement")]
        [Min(0f)] public float walkSpeed = 2.8f;
        [Min(0f)] public float sprintSpeed = 4.8f;
        [Min(0.1f)] public float acceleration = 12f;
        public float gravity = -20f;
        public KeyCode sprintKey = KeyCode.LeftShift;

        [Header("Stamina")]
        [Min(0.1f)] public float maxStamina = 5f;
        [Min(0f)] public float staminaDrainPerSecond = 1f;
        [Min(0f)] public float staminaRegenPerSecond = 0.7f;
        [Min(0f)] public float regenDelay = 1.2f;
        [Range(0f, 1f)] public float exhaustedRecoverFraction = 0.3f;

        [Header("Look")]
        [Min(0.01f)] public float mouseSensitivity = 2f;
        [Range(1f, 20f)] public float lookSmoothing = 1f;   // 1 = raw input, higher = heavier camera
        [Range(30f, 89f)] public float maxLookAngle = 80f;

        [Header("Head Bob")]
        public bool enableHeadBob = true;
        public float bobFrequency = 1.6f;
        public float bobAmplitude = 0.045f;

        public bool InputEnabled { get; private set; }
        public bool IsSprinting { get; private set; }
        public float Stamina01 { get { return stamina / maxStamina; } }

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;
        private Vector2 smoothedLook;
        private float stamina;
        private float regenTimer;
        private bool exhausted;
        private float bobTimer;
        private Vector3 cameraBaseLocalPosition;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            stamina = maxStamina;
            InputEnabled = true;
        }

        private void Start()
        {
            if (cameraPivot == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null)
                    cameraPivot = cam.transform;
            }

            if (cameraPivot == null)
                Debug.LogError("PlayerController: assign a Camera Pivot (the player's camera).", this);
            else
                cameraBaseLocalPosition = cameraPivot.localPosition;

            SetInputEnabled(true);
        }

        private void Update()
        {
            if (InputEnabled)
                UpdateLook();

            UpdateMovement();
            UpdateHeadBob();
        }

        /// <summary>Enable/disable player input and lock/unlock the cursor accordingly.</summary>
        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }

        private void UpdateLook()
        {
            Vector2 raw = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity;
            smoothedLook = Vector2.Lerp(smoothedLook, raw, 1f / lookSmoothing);

            transform.Rotate(Vector3.up, smoothedLook.x, Space.Self);

            pitch = Mathf.Clamp(pitch - smoothedLook.y, -maxLookAngle, maxLookAngle);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            Vector2 input = Vector2.zero;
            bool wantsSprint = false;

            if (InputEnabled)
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                input = Vector2.ClampMagnitude(input, 1f);
                wantsSprint = Input.GetKey(sprintKey);
            }

            bool moving = input.sqrMagnitude > 0.01f;
            IsSprinting = wantsSprint && moving && !exhausted && stamina > 0f;
            UpdateStamina();

            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f; // keeps isGrounded reliable (PlayerMonsterIntegration checks it)
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateStamina()
        {
            if (IsSprinting)
            {
                stamina -= staminaDrainPerSecond * Time.deltaTime;
                regenTimer = regenDelay;

                if (stamina <= 0f)
                {
                    stamina = 0f;
                    exhausted = true;
                    IsSprinting = false;
                }
                return;
            }

            regenTimer -= Time.deltaTime;
            if (regenTimer > 0f)
                return;

            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.deltaTime);
            if (exhausted && stamina >= maxStamina * exhaustedRecoverFraction)
                exhausted = false;
        }

        private void UpdateHeadBob()
        {
            if (!enableHeadBob || cameraPivot == null)
                return;

            Vector3 flatVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            float speed = flatVelocity.magnitude;
            Vector3 target = cameraBaseLocalPosition;

            if (controller.isGrounded && speed > 0.1f)
            {
                bobTimer += Time.deltaTime * bobFrequency * speed;
                float strength = IsSprinting ? 1.4f : 1f;
                target.y += Mathf.Sin(bobTimer * 2f) * bobAmplitude * strength;
                target.x += Mathf.Cos(bobTimer) * bobAmplitude * 0.5f * strength;
            }
            else
            {
                bobTimer = 0f;
            }

            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, target, 10f * Time.deltaTime);
        }
    }
}