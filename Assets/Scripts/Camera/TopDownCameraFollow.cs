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

        [Header("Screen Shake & Combat Juice")]
        [Tooltip("Frequency of trauma oscillation.")]
        [SerializeField] private float shakeFrequency = 35f;

        private Vector3 currentVelocity = Vector3.zero;
        private float shakeIntensity = 0f;
        private float shakeDuration = 0f;
        private float shakeTimer = 0f;
        private Vector3 currentShakeOffset = Vector3.zero;

        public float CurrentShakeIntensity => (shakeDuration > 0.0001f && shakeTimer > 0f) ? (shakeIntensity * Mathf.Clamp01(shakeTimer / shakeDuration)) : 0f;
        public bool IsShaking => shakeTimer > 0.0001f && shakeIntensity > 0.0001f;
        public Vector3 CurrentShakeOffset => currentShakeOffset;
        public float ShakeDuration => shakeDuration;
        public float ShakeTimer => shakeTimer;

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
                UpdateShake();
                desiredPosition += currentShakeOffset;
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

        /// <summary>
        /// Triggers smooth damped screen shake decaying smoothly over duration.
        /// Ignores non-positive or NaN values safely.
        /// </summary>
        public void TriggerShake(float intensity, float duration)
        {
            if (float.IsNaN(intensity) || float.IsInfinity(intensity) || intensity <= 0f) return;
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f) return;

            float currentRemaining = CurrentShakeIntensity;
            shakeIntensity = Mathf.Max(currentRemaining, intensity);
            shakeDuration = Mathf.Max(shakeTimer, duration);
            shakeTimer = shakeDuration;
        }

        public void UpdateShake(float deltaTime = -1f)
        {
            float dt = (deltaTime >= 0f) ? deltaTime : Time.deltaTime;
            if (shakeTimer > 0f && shakeDuration > 0f)
            {
                shakeTimer -= dt;
                float progress = Mathf.Clamp01(shakeTimer / shakeDuration);
                float magnitude = shakeIntensity * progress;

                float sample = (Time.time > 0f ? Time.time : (shakeDuration - shakeTimer)) * shakeFrequency;
                float offsetX = (Mathf.PerlinNoise(sample, 0.15f) * 2f - 1f) * magnitude;
                float offsetY = (Mathf.PerlinNoise(0.25f, sample) * 2f - 1f) * (magnitude * 0.6f);
                float offsetZ = (Mathf.PerlinNoise(sample, sample) * 2f - 1f) * magnitude;

                currentShakeOffset = new Vector3(offsetX, offsetY, offsetZ);

                if (shakeTimer <= 0f)
                {
                    StopShake();
                }
            }
            else
            {
                currentShakeOffset = Vector3.zero;
            }
        }

        public void StopShake()
        {
            shakeTimer = 0f;
            shakeIntensity = 0f;
            shakeDuration = 0f;
            currentShakeOffset = Vector3.zero;
        }

        private void OnValidate()
        {
            ApplyCameraRotation();
        }
    }
}
