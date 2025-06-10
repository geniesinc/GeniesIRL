using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL 
{
    /// <summary>
    /// A drawing space is a place where the Genie can draw pictures. Similar to a Seat, this is spawned at runtime and assigned to an ARPlane. it is
    /// also possible to place these prefabs in the scene for dev purposes.
    /// </summary>
    public class DrawingSpace : MonoBehaviour
    {
        /// <summary>
        /// This gets set to true the moment the DrawingSpace is assigned to an AR Plane. If we're in a debug environment, where a dev
        /// has spawned the seat manually, this will be false. We use this to determine whether to destroy the DrawingSpace if the assigned
        /// AR Plane gets destroyed.
        /// </summary>

        public ARPlane ARPlane { get; private set; }

        /// <summary>
        /// The pose at which the drawing should be placed on the wall.
        /// </summary>
        public Pose DrawingPose {get; private set;}

        [SerializeField, Tooltip("The collision layer to check for during drawing space validation. (By default this is the Spatial Mesh layer)")]
        private LayerMask hitmask = 1 << 29;

        [SerializeField, Tooltip("The offset to apply to the drawing space when placing it on the wall, to avoid z-fighting")]
        private float _extraDrawingZOffset = 0.01f;
        [Header("Debug")]
        public Color debugColorWhenValidated = Color.green;
        public Color debugColorWhenInvalidated = Color.red;

        // By default, any DrawingSpace that is dragged into the scene at runtime will be in debug mode. This gets overridden
        // when the DrawingSpace is assigned to an AR Plane, where it will use the WallProcessor's debug state.
        private bool _isDebugMode = true; 

        private GameObject _debugDrawingQuad;

        public void AssignToARPlane(ARPlane arPlane, bool isDebugMode = false)
        {
            ARPlane = arPlane;
            _isDebugMode = isDebugMode;

            // Show the renderer only if we're in debug mode.
            Renderer rend = GetComponent<Renderer>();
            rend.enabled = _isDebugMode;
        }

        /// <summary>
        /// Determines whether a drawing space is a valid place to draw on. If it is, it returns true and sets the DrawingPose,
        /// which is the position and rotation at which a drawing should be placed on the wall.
        /// </summary>
        /// <returns></returns>
        public bool Evaluate()
        {
            DrawingPose = new Pose(transform.position, transform.rotation);

            // If not assigned to a wall, we treat the DrawingSpace is always valid since it is a dev-placed object.
            if (ARPlane == null)
            {
                UpdateDebugVisualization(true);
                return true;
            }

            // If we're too close to any other wall drawings, we shouldn't draw here.
            if (IsTooCloseToAnyWallDrawings())
            {
                UpdateDebugVisualization(false);
                return false;
            }

            // Do five spherecasts, one in the center, and one at each corner of the drawing space.
            Vector3 center = transform.position;
            Vector3 extents = transform.localScale/2;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 up = transform.up;

            Vector3 frontCenter = center + forward * extents.z;
            float thickness = 0.02f; // The radius of the spherecast.

            Vector3[] rayOrigins = new Vector3[5];
            rayOrigins[0] = frontCenter + right * (extents.x - thickness) + up * (extents.y - thickness); // Top right
            rayOrigins[1] = frontCenter - right * (extents.x - thickness) + up * (extents.y - thickness); // Top left
            rayOrigins[2] = frontCenter + right * (extents.x - thickness) - up * (extents.y - thickness); // Bottom right
            rayOrigins[3] = frontCenter - right * (extents.x - thickness)- up * (extents.y - thickness); // Bottom left    
            rayOrigins[4] = frontCenter; // Center

            float threshold = 0.02f; // Hit points must be within this threshold to count as a flat surface.
            
            float maxDistance = extents.z * 2 + thickness; // The maximum distance to cast the spherecast.

             // This is how we'll detect the flatness of the surface.
            float[] hitDistances = new float[5];
            
            // Initialize each elment as -1 to indicate it hasn't been set.
            for (int i = 0; i < hitDistances.Length; i++)
            {
                hitDistances[i] = -1;
            }

            for (int i = 0; i < rayOrigins.Length; i++)
            {
                Vector3 origin = rayOrigins[i];

                if (Physics.SphereCast(origin, thickness, -forward, out RaycastHit hit, maxDistance, hitmask))
                {
                    Vector3 hitVector = origin - hit.point;
                    hitVector = new Vector3(hitVector.x, 0, hitVector.z); // Ignore the y component.
                    hitDistances[i] = hitVector.magnitude;
                }
                else 
                {
                    Debug.DrawLine(origin, origin - forward * maxDistance, Color.red, 2);
                    UpdateDebugVisualization(false);
                    return false; // Failure! We hit nothing, meaning this drawing space is hanging off a wall or something.
                }

                // Check if the hit distances are within the threshold.
                if (!AreDifferencesWithinThreshold(hitDistances, threshold, i))
                {
                    Debug.DrawLine(origin, origin - forward * hitDistances[i], Color.yellow, 2);
                    UpdateDebugVisualization(false);
                    return false;
                }

                Debug.DrawLine(origin, origin - forward * hitDistances[i], Color.green, 2);
            }

            // If we've made it here, all spherecasts have hit a surface within the threshold, meaning that the surface is reasonably flat to draw on.
            DrawingPose = CalculateDrawingPose(hitDistances, forward);
            UpdateDebugVisualization(true);
            return true;
        }

        private bool IsTooCloseToAnyWallDrawings()
        {
            // Find any objects with the WallDrawing component.
            WallDrawing[] wallDrawings = FindObjectsByType<WallDrawing>(FindObjectsSortMode.None);

            if (wallDrawings == null || wallDrawings.Length == 0)
            {
                return false; // No drawings in the scene, all clear.
            }

            foreach (WallDrawing wallDrawing in wallDrawings)
            {
                float minDist = wallDrawing.drawingRadius * 2f;
                if (VectorUtils.IsWithinDistance(wallDrawing.transform.position, transform.position, minDist))
                {
                    return true; // We're too close to another drawing.
                }
            }

            return false; // All clear.
        }

        private void UpdateDebugVisualization(bool isValid)
        {
            Renderer rend = GetComponent<Renderer>();
            rend.enabled = _isDebugMode;

            if (!_isDebugMode) return;
            
            float alpha = rend.material.color.a;
            rend.material.color = isValid ? debugColorWhenValidated : debugColorWhenInvalidated;
            rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, alpha);
            
            ShowDebugDrawingQuad();
        }

        private bool AreDifferencesWithinThreshold(float[] values, float threshold, int maxIndex)
        {
            if (values == null || maxIndex < 2)
            {
                // If there are fewer than 2 elements, return true as there's no comparison to make.
                return true;
            }

            for (int i = 0; i < maxIndex; i++)
            {
                for (int j = i + 1; j < maxIndex + 1; j++)
                {
                    if (Math.Abs(values[i] - values[j]) > threshold)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the point and rotation at which the drawing should be placed on the wall.
        /// </summary>
        /// <param name="hitDistances">The distances from the </param>
        /// <param name="forward"></param>
        /// <returns></returns>
        private Pose CalculateDrawingPose(float[] hitDistances, Vector3 forward)
        {
            if (ARPlane == null) 
            {
                return new Pose(transform.position, transform.rotation);
            }

            // Determine the smallest hit distance from the spherecasts. Remember, the spherecasts start from the front of the drawing space and shoot backwards,
            // so a smaller hit distance means that the spherecast hit a surface closer to the front of the drawing space. We want to find the "front" most point
            // and use that to place our drawings, to prevent it from clipping into the wall.
            float smallestDistance = Mathf.Infinity;
            for (int i = 0; i < hitDistances.Length; i++)
            {
                if (hitDistances[i] < 0) continue; // Skip negative values -- this means it wasn't set.

                if (hitDistances[i] < smallestDistance)
                {
                    smallestDistance = hitDistances[i];
                }
            }

            // Convert this 'smallest distance' into a distance from the center of the DrawingSpace.
            float distanceForward = (transform.localScale.z / 2) - smallestDistance;
            distanceForward += _extraDrawingZOffset; // Add an extra offset to prevent z-fighting.

            // Now we can use the center of the DrawingSpace and its forward vector to find the point at which the drawing should be placed.
            Vector3 pos = transform.position + forward * distanceForward;

            // Find the rotation as well.
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

            Pose pose = new Pose(pos, rot);
            return pose;
        }

        private void Start()
        {
            if (_isDebugMode) 
            {
                Evaluate();
            }
        }

        private void ShowDebugDrawingQuad() 
        {
            if (_debugDrawingQuad == null) 
            {
                _debugDrawingQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            }

            GameObject.Destroy(_debugDrawingQuad.GetComponent<Collider>());
            _debugDrawingQuad.transform.SetParent(transform);
             _debugDrawingQuad.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            _debugDrawingQuad.transform.position = DrawingPose.position;
            _debugDrawingQuad.transform.rotation = DrawingPose.rotation;
            
            // Flip the quad on the local y axis.
            _debugDrawingQuad.transform.Rotate(Vector3.up, 180);
        }
    }
}
