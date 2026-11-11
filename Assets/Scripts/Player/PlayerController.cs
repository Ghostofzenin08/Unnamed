using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Outpost404.Player
{
    /// <summary>
    /// Rigidbody-based top-down player movement and mouse-look controller.
    /// Handles responsive WASD physics locomotion and cursor-aiming plane raycasts.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion Settings")]
        [Tooltip("Standard walking speed in world units per second.")]
        [SerializeField] private float moveSpeed = 6.5f;

        [Tooltip("Sprint speed multiplier applied when holding Shift.")]
        [SerializeField] private float sprintMultiplier = 1.45f;

        [Tooltip("Acceleration factor for responsive, non-slippery physics movement.")]
        [SerializeField] private float acceleration = 30f;

        [Header("Aim & Rotation")]
        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float turnSpeed = 720f;

        [Tooltip("When enabled, the character faces the cursor immediately without rotational lag.")]
        [SerializeField] private bool instantRotation = false;

        [Header("Camera & Raycast References")]
        [Tooltip("Reference to the main gameplay camera. Auto-assigned if empty.")]
        [SerializeField] private Camera mainCamera;

        // Cached physics and state references
        private Rigidbody rb;
        private Vector2 rawInput;
        private Vector3 targetMoveVelocity;
        private Vector3 currentVelocity;
        private Vector3 lookTargetPoint;
        private bool hasLookTarget;
        private bool isSprinting;

        public Vector3 CurrentMoveDirection => targetMoveVelocity.normalized;
        public bool IsMoving => targetMoveVelocity.sqrMagnitude > 0.01f;
        public bool IsSprinting => isSprinting;
        public Vector3 LookTargetPoint => lookTargetPoint;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ConfigureRigidbody();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void ConfigureRigidbody()
        {
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
            else
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

            isSprinting = sprintPressed;
            float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

            // World-space XZ velocity vector
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

            // Infinite horizontal plane anchored at the character's feet/center
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

        private void ExecutePhysicsMovement()
        {
            // Rapid acceleration to eliminate sluggishness
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetMoveVelocity, acceleration * Time.fixedDeltaTime);

            Vector3 displacement = currentVelocity * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + displacement);
        }

        private void ExecutePhysicsRotation()
        {
            if (!hasLookTarget) return;

            Vector3 lookDir = lookTargetPoint - rb.position;
            lookDir.y = 0f; // Constrain rotation strictly to horizontal plane

            if (lookDir.sqrMagnitude > 0.001f)
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

        private void OnDrawGizmosSelected()
        {
            if (hasLookTarget)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lookTargetPoint, 0.12f);
                Gizmos.DrawLine(transform.position, lookTargetPoint);
            }
        }
    }
}
