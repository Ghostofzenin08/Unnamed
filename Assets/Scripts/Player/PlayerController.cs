using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Outpost404.Player
{
    /// <summary>
    /// Rigidbody-based top-down player movement and cursor-aiming controller.
    /// Handles responsive WASD/gamepad physics locomotion, proper 3D world raycasting,
    /// and smooth rotation with close-range deadzone protection.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion Settings")]
        [Tooltip("Standard walking speed in world units per second.")]
        [SerializeField] private float moveSpeed = 6.5f;

        [Tooltip("Sprint speed multiplier applied when holding Shift or Sprint button.")]
        [SerializeField] private float sprintMultiplier = 1.45f;

        [Tooltip("Acceleration factor for responsive, non-slippery physics movement.")]
        [SerializeField] private float acceleration = 30f;

        [Header("Aim & Rotation")]
        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float turnSpeed = 720f;

        [Tooltip("When enabled, the character faces the cursor immediately without rotational lag.")]
        [SerializeField] private bool instantRotation = false;

        [Tooltip("Minimum distance from player to cursor required to rotate (prevents violent spinning when cursor is over player).")]
        [SerializeField] private float rotationDeadzone = 0.35f;

        [Tooltip("Layers checked for mouse-to-world raycasting (terrain, ground, obstacles).")]
        [SerializeField] private LayerMask aimLayerMask = ~0;

        [Header("Camera & Raycast References")]
        [Tooltip("Reference to the main gameplay camera. Auto-assigned if empty.")]
        [SerializeField] private Camera mainCamera;

        // Cached physics and state references
        private Rigidbody rb;
        private Outpost404.Survival.PlayerStats playerStats;
        private Vector2 rawInput;
        private Vector3 targetMoveVelocity;
        private Vector3 currentVelocity;
        private Vector3 lookTargetPoint;
        private bool hasLookTarget;
        private bool isSprinting;

        [Header("Footstep Audio Cadence")]
        [SerializeField] private float walkStepInterval = 0.36f;
        [SerializeField] private float sprintStepInterval = 0.24f;
        private float stepTimer = 0f;

        public event System.Action OnFootstepTaken;
        public float WalkStepInterval { get => walkStepInterval; set => walkStepInterval = Mathf.Max(0.05f, value); }
        public float SprintStepInterval { get => sprintStepInterval; set => sprintStepInterval = Mathf.Max(0.05f, value); }
        public float StepTimer => stepTimer;

        public Vector3 CurrentMoveDirection => targetMoveVelocity.normalized;
        public Vector3 CurrentVelocity => currentVelocity;
        public Vector3 TargetMoveVelocity => targetMoveVelocity;
        public float CurrentSpeed => targetMoveVelocity.magnitude;
        public bool IsMoving => targetMoveVelocity.sqrMagnitude > 0.01f;
        public bool IsSprinting => isSprinting;
        public Vector3 LookTargetPoint => lookTargetPoint;
        public bool HasLookTarget => hasLookTarget;
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
        public float SprintMultiplier { get => sprintMultiplier; set => sprintMultiplier = value; }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (playerStats == null) playerStats = GetComponent<Outpost404.Survival.PlayerStats>();
            ConfigureRigidbody();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Reset()
        {
            ConfigureRigidbody();
        }

        public void ConfigureRigidbody()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            // Lock X and Z rotation axes to maintain upright stance
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.useGravity = true;
        }

        private void Update()
        {
            ReadInput();
            ComputeCursorLookTarget();
            UpdateFootsteps();
        }

        public void UpdateFootsteps(float deltaTime = -1f)
        {
            float dt = (deltaTime >= 0f) ? deltaTime : Time.deltaTime;
            if (IsMoving)
            {
                stepTimer += dt;
                float interval = isSprinting ? sprintStepInterval : walkStepInterval;
                if (stepTimer >= interval)
                {
                    stepTimer = 0f;
                    OnFootstepTaken?.Invoke();
                    Outpost404.Audio.AudioManager.Instance?.PlayFootstep();
                }
            }
            else
            {
                stepTimer = 0f;
            }
        }

        private void FixedUpdate()
        {
            ExecutePhysicsMovement();
            ExecutePhysicsRotation();
        }

        private void ReadInput()
        {
            float horizontal = 0f;
            float vertical = 0f;
            bool sprintPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
                sprintPressed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    horizontal = stick.x;
                    vertical = stick.y;
                }
                if (Gamepad.current.leftStickButton.isPressed || Gamepad.current.rightTrigger.isPressed)
                {
                    sprintPressed = true;
                }
            }

            if (Keyboard.current == null && Gamepad.current == null)
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
                sprintPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            sprintPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

            rawInput = new Vector2(horizontal, vertical);
            if (rawInput.sqrMagnitude > 1f)
            {
                rawInput.Normalize();
            }

            if (playerStats != null && playerStats.IsDead)
            {
                rawInput = Vector2.zero;
                targetMoveVelocity = Vector3.zero;
                isSprinting = false;
                return;
            }

            isSprinting = sprintPressed && (playerStats == null || playerStats.CanSprint);
            float speedModifier = playerStats != null ? playerStats.MovementSpeedModifier : 1.0f;
            float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f) * speedModifier;

            // World-space horizontal velocity target
            Vector3 desiredDirection = new Vector3(rawInput.x, 0f, rawInput.y);
            targetMoveVelocity = desiredDirection * speed;
        }

        private void ComputeCursorLookTarget()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            Vector2 screenPoint;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                screenPoint = Mouse.current.position.ReadValue();
            }
            else
            {
                screenPoint = Input.mousePosition;
            }
#else
            screenPoint = Input.mousePosition;
#endif

            Ray ray = mainCamera.ScreenPointToRay(screenPoint);

            // 1. Raycast against physical 3D colliders (terrain, ground, obstacles)
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, aimLayerMask, QueryTriggerInteraction.Ignore))
            {
                lookTargetPoint = hit.point;
                hasLookTarget = true;
            }
            else
            {
                // 2. Fallback: mathematical horizontal plane anchored at player height
                Plane groundPlane = new Plane(Vector3.up, transform.position);
                if (groundPlane.Raycast(ray, out float hitDistance))
                {
                    lookTargetPoint = ray.GetPoint(hitDistance);
                    hasLookTarget = true;
                }
                else
                {
                    hasLookTarget = false;
                }
            }
        }

        private void ExecutePhysicsMovement()
        {
            // Rapid acceleration curve to eliminate sluggishness while maintaining smooth transitions
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetMoveVelocity, acceleration * Time.fixedDeltaTime);

            // Apply dynamic Rigidbody velocity, preserving vertical velocity (gravity / falling)
#if UNITY_6000_0_OR_NEWER
            Vector3 currentRbVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(currentVelocity.x, currentRbVelocity.y, currentVelocity.z);
#else
            Vector3 currentRbVelocity = rb.velocity;
            rb.velocity = new Vector3(currentVelocity.x, currentRbVelocity.y, currentVelocity.z);
#endif
        }

        private void ExecutePhysicsRotation()
        {
            if (!hasLookTarget) return;

            Vector3 lookDir = lookTargetPoint - rb.position;
            lookDir.y = 0f; // Constrain rotation strictly to horizontal plane

            // Only rotate if cursor is outside the deadzone to prevent erratic spinning near character center
            if (lookDir.sqrMagnitude >= (rotationDeadzone * rotationDeadzone))
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);

                if (instantRotation)
                {
                    rb.MoveRotation(targetRotation);
                }
                else
                {
                    Quaternion smoothedRotation = Quaternion.RotateTowards(
                        rb.rotation,
                        targetRotation,
                        turnSpeed * Time.fixedDeltaTime
                    );
                    rb.MoveRotation(smoothedRotation);
                }
            }
        }

        public void SetDirectTargetVelocity(Vector3 velocity)
        {
            targetMoveVelocity = velocity;
        }

        /// <summary>
        /// Programmatically inject movement input (useful for testing and AI).
        /// </summary>
        public void SetInputOverride(Vector2 input, bool sprint)
        {
            if (playerStats == null) playerStats = GetComponent<Outpost404.Survival.PlayerStats>();
            rawInput = Vector2.ClampMagnitude(input, 1f);
            isSprinting = sprint;
            float speedModifier = playerStats != null ? playerStats.MovementSpeedModifier : 1.0f;
            float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f) * speedModifier;
            targetMoveVelocity = new Vector3(rawInput.x, 0f, rawInput.y) * speed;
        }

        private void OnDrawGizmosSelected()
        {
            if (hasLookTarget)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lookTargetPoint, 0.15f);
                Gizmos.DrawLine(transform.position, lookTargetPoint);
            }

            // Draw rotation deadzone
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, rotationDeadzone);
        }
    }
}
