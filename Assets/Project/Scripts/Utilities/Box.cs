using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// A cube that defines a box in 3D space, with a center, size, and rotation.
    /// </summary>
    public class Box 
    {
        public Vector3 center;
        public Vector3 size;
        public Quaternion rotation;

        /// <summary>
        /// Returns the bounds in world space that completely encapsulates, changing depending how the box is rotated.
        /// </summary>
        public Bounds bounds {
            get 
            {
                // Half size helps to define the 8 corners in local space
                Vector3 halfSize = size * 0.5f;

                // Define all 8 corners of the box in its local space
                Vector3[] corners = new Vector3[8]
                {
                    new Vector3( halfSize.x,  halfSize.y,  halfSize.z),
                    new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
                    new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
                    new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
                    new Vector3(-halfSize.x,  halfSize.y,  halfSize.z),
                    new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
                    new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),
                    new Vector3(-halfSize.x, -halfSize.y, -halfSize.z)
                };

                // Initialize the Bounds with the first corner
                Vector3 firstCorner = rotation * corners[0] + center;
                Bounds bounds = new Bounds(firstCorner, Vector3.zero);

                // Encapsulate each rotated corner
                for (int i = 1; i < corners.Length; i++)
                {
                    Vector3 worldCorner = rotation * corners[i] + center;
                    bounds.Encapsulate(worldCorner);
                }

                return bounds;
            }
        }
    }
}
