using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Places a marker one metre in front of the main camera and keeps its Y
    /// coordinate locked to the floor height reported by <see cref="ARFloorDetection"/>.
    /// Movement is smoothed so the marker feels anchored to the world, not the head.
    /// </summary>
    [RequireComponent(typeof(Renderer))]   // purely to make the object visible in scene
    public class DebugFloorMarker : MonoBehaviour
    {
        [Tooltip("Time (seconds) the marker takes to follow the camera.")]
        [Range(0.01f, 1f)]
        public float smoothTime = 0.15f;

        Vector3 _velocity;          // for SmoothDamp

        private FloorManager _floorManager;


        void Awake()
        {
            
        }

        void Update()
        {
            if (_floorManager == null) 
            {
                _floorManager = FindFirstObjectByType<FloorManager>();
            }

            if (Camera.main == null || _floorManager == null)
                return;

            float floorY = _floorManager.FloorY;

            // --- target position -------------------------------------------------
            Vector3 camPos = Camera.main.transform.position;
            Vector3 fwd    = Camera.main.transform.forward;
            fwd.y = 0f;                             // discard vertical component
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            Vector3 target = camPos + fwd * 1.0f;   // 1 metre in front (XZ only)
            target.y = floorY;                // lock to detected floor height
            // ---------------------------------------------------------------------

            // Smooth, frame‑rate independent follow
            transform.position = Vector3.SmoothDamp(transform.position,
                                                    target,
                                                    ref _velocity,
                                                    smoothTime);
        }
    }
}