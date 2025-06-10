using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GeniesIRL 
{
    public static class GizmoUtilities
    {
        /// <summary>
        /// Draws a wireframe cube at the specified position, with the specified rotation and size.
        /// </summary>
        /// <param name="position">Center of the cube in world space.</param>
        /// <param name="rotation">Rotation of the cube in world space.</param>
        /// <param name="size">Size (scale) of the cube.</param>
        public static void DrawWireCube(Vector3 position, Quaternion rotation, Vector3 size)
        {
            // Save the current Gizmos matrix so we can restore it later
            Matrix4x4 oldMatrix = Gizmos.matrix;

            // Construct a TRS matrix from the specified position, rotation, and uniform scale
            // Note: The 'size' parameter is given directly to DrawWireCube, so 
            // we pass Vector3.one as the scale in the TRS. Alternatively, if you want
            // to unify "scale" with "size," you can pass Vector3.one to DrawWireCube
            // and do the scaling in the TRS matrix. This version keeps it straightforward.
            Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

            // Draw the wire cube around Vector3.zero using the given size
            Gizmos.DrawWireCube(Vector3.zero, size);

            // Restore the original Gizmos matrix
            Gizmos.matrix = oldMatrix;
        }

        /// <summary>
        /// Draws a circle in the Scene view using Handles.DrawWireDisc. (Handles are only available in the Editor.)
        /// </summary>
        /// <param name="origin">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="normal">
        /// The normal of the circle's plane (perpendicular to the circle).
        /// For example, if the circle lies flat on the ground, use Vector3.up.
        /// </param>
        public static void DrawCircle(Vector3 origin, float radius, Vector3 normal)
        {
            #if UNITY_EDITOR
                    // Ensure the normal is normalized.
                    normal.Normalize();

                    // Optionally set a color for the disc
                    Handles.color = Gizmos.color;

                    // Draw the circle (wire disc) in the Scene view.
                    Handles.DrawWireDisc(origin, normal, radius);
            #endif
        }
    }
}

