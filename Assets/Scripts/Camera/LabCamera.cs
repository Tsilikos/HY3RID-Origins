using UnityEngine;

namespace HY3RIDOrigins.Camera
{
    // Top-down follow camera for the laboratory.
    // Smooth lerp follow with configurable world bounds clamping.
    // Attach to the Main Camera.
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class LabCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow")]
        [SerializeField] private float lerpSpeed = 6f;

        [Header("Orthographic Size")]
        [SerializeField] private float orthographicSize = 7f;

        [Header("World Bounds (set to match your room)")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private float boundsMinX = -20f;
        [SerializeField] private float boundsMaxX =  20f;
        [SerializeField] private float boundsMinY = -15f;
        [SerializeField] private float boundsMaxY =  15f;

        private UnityEngine.Camera cam;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }

        // Expose so game code can assign the target at runtime (e.g., after spawning Leonidas).
        public void SetTarget(Transform t) => target = t;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position;
            desired.z = transform.position.z; // keep camera Z fixed

            if (useBounds)
            {
                // Clamp so the camera never shows outside the room.
                float halfH = cam.orthographicSize;
                float halfW = halfH * cam.aspect;
                desired.x = Mathf.Clamp(desired.x, boundsMinX + halfW, boundsMaxX - halfW);
                desired.y = Mathf.Clamp(desired.y, boundsMinY + halfH, boundsMaxY - halfH);
            }

            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * lerpSpeed);
        }

        // Called from LabRoomBuilder or scene initializer when room size is known.
        public void SetBounds(Rect worldRect)
        {
            boundsMinX = worldRect.xMin;
            boundsMaxX = worldRect.xMax;
            boundsMinY = worldRect.yMin;
            boundsMaxY = worldRect.yMax;
            useBounds = true;
        }
    }
}
