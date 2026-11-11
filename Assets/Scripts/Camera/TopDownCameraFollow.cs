using UnityEngine;

namespace Outpost404.CameraControl
{
    /// <summary>
    /// Smooth-damped top-down / bird's-eye camera follow script.
    /// Follows the player target with customizable high-angle pitch, yaw-aware orbit offset,
    /// target center-of-mass framing, and deadzone damping.
    /// </summary>
    [ExecuteAlways]
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Target transform to follow (typically the Player).")]
        [SerializeField] private Transform target;

        [Tooltip("Vertical/horizontal target offset to focus on character's chest rather than feet.")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("Automatically look for GameObject tagged 'Player' if target reference is unassigned.")]
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Framing & Perspective")]
        [Tooltip("Positional distance offset relative to the target before yaw rotation.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -9f);

        [Tooltip("Smooth damp latency in seconds. Lower is tighter, higher provides cinematic lag.")]
        [Range(0.01f, 0.8f)]
        [SerializeField] private float smoothTime = 0.16f;

        [Header("Fixed Angle")]
        [Tooltip("Downwards pitch angle in degrees (50-65 is ideal for top-down survival).")]
        [Range(30f, 85f)]
        [SerializeField] private float cameraPitch = 56f;

        [Tooltip("Horizontal orientation angle around the Y axis (0 = North-facing bird's-eye, 45 = isometric).")]
        [SerializeField] private float cameraYaw = 0f;

        [Header("Boundary Constraints (Optional)")]
        [Tooltip("Restrict camera movement to world XZ boundary box.")]
        [SerializeField] private bool clampToBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 maxBounds = new Vector2(100f, 100f);

        private Vector3 currentVelocity = Vector3.zero;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Vector3 Offset { get => offset; set => offset = value; }
        public Vector3 TargetOffset { get => targetOffset; set => targetOffset = value; }
        public float SmoothTime { get => smoothTime; set => smoothTime = value; }
        public float CameraPitch { get => cameraPitch; set => cameraPitch = value; }
        public float CameraYaw { get => cameraYaw; set => cameraYaw = value; }

        private void Awake()
        {
            LocateTarget();
        }

        private void Start()
        {
            LocateTarget();
            ApplyCameraRotation();

            // Snap directly to target position on initialization to eliminate initial panning lag
            if (target != null)
            {
                transform.position = ComputeDestination();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                // In edit mode without play, avoid running expensive FindWithTag searches every frame
                if (!Application.isPlaying) return;

                if (autoFindPlayer) LocateTarget();
                if (target == null) return;
            }

            Vector3 desiredPosition = ComputeDestination();

            if (Application.isPlaying)
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            }
            else
            {
                transform.position = desiredPosition;
                ApplyCameraRotation();
            }
        }

        public Vector3 ComputeDestination()
        {
            if (target == null) return transform.position;

            // Rotate offset by cameraYaw so rotating camera in editor/runtime orbits around target correctly
            Vector3 rotatedOffset = Quaternion.Euler(0f, cameraYaw, 0f) * offset;
            Vector3 destination = (target.position + targetOffset) + rotatedOffset;

            if (clampToBounds)
            {
                destination.x = Mathf.Clamp(destination.x, minBounds.x, maxBounds.x);
                destination.z = Mathf.Clamp(destination.z, minBounds.y, maxBounds.y);
            }

            return destination;
        }

        public void ApplyCameraRotation()
        {
            transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        }

        private void LocateTarget()
        {
            if (target != null) return;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        public void SnapToTarget()
        {
            if (target != null)
            {
                currentVelocity = Vector3.zero;
                transform.position = ComputeDestination();
                ApplyCameraRotation();
            }
        }

        private void OnValidate()
        {
            ApplyCameraRotation();
        }
    }
}
