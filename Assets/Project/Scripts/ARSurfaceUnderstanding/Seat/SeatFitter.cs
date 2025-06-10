using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL 
{
    public static class SeatFitter
    {
        // Structure for holding the oriented rectangle data.
        public struct OrientedRect
        {
            public Vector2 center;   // Center in ARPlane local 2D space.
            public Vector2 size;     // Width and height of the rectangle.
            public float angle;      // Rotation (in degrees) relative to the ARPlane's local x-axis.
        }

        /// <summary>
        /// Adjusts the SeatGroup so that it encapsulates the shape of the AR Plane. The child 'inner cube' of the seat group is scaled instead of its parent.
        /// </summary>
        /// <param name="arPlane">The ARPlane to fit the cube to.</param>
        public static void FitSeatToARPlane(ARPlane arPlane, Seat seat)
        {
            if (arPlane == null || arPlane.boundary == null || arPlane.boundary.Length == 0)
            {
                Debug.LogWarning("Invalid ARPlane or empty boundary.");
                return;
            }

            // Compute the oriented bounding rectangle directly from the convex boundary.
            Vector2[] boundaryPoints = arPlane.boundary.ToArray();
            OrientedRect rect = ComputeMinAreaRect(boundaryPoints);

            // Position the seatGroup.
            Vector3 planeSpaceCenter = new Vector3(rect.center.x, 0f, rect.center.y);
            Vector3 worldCenter = arPlane.transform.TransformPoint(planeSpaceCenter);
            seat.transform.position = worldCenter;

            // Rotate the cube to match the ARPlane's rotation, then adjust it to match the rectangle's orientation.
            seat.transform.rotation = arPlane.transform.rotation;
            seat.transform.rotation *= Quaternion.AngleAxis(-rect.angle, arPlane.transform.up);

            // Adjust the inner cube's scale in-plane scale (x and y) to match the rectangle dimensions,
            // and set the y-scale (thickness) to 0.05f.
            seat.innerCube.transform.localScale = new Vector3(rect.size.x, 0.05f, rect.size.y);
        }

        /// <summary>
        /// Computes the minimum area oriented rectangle for the given convex 2D points.
        /// Iterates over each edge of the boundary and finds the orientation with the smallest area.
        /// </summary>
        private static OrientedRect ComputeMinAreaRect(Vector2[] points)
        {
            OrientedRect bestRect = new OrientedRect();
            if (points.Length == 0)
                return bestRect;

            float minArea = float.MaxValue;

            // Iterate over each edge of the convex boundary.
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % points.Length];
                Vector2 edge = p2 - p1;

                // Determine the angle of the edge in radians and then in degrees.
                float angle = Mathf.Atan2(edge.y, edge.x);
                float angleDeg = angle * Mathf.Rad2Deg;

                // Rotate points so that the current edge aligns with the x-axis.
                Quaternion rotation = Quaternion.Euler(0, 0, -angleDeg);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);

                foreach (Vector2 p in points)
                {
                    Vector2 rotatedP = RotatePoint(p, rotation);
                    min = Vector2.Min(min, rotatedP);
                    max = Vector2.Max(max, rotatedP);
                }

                Vector2 size = max - min;
                float area = size.x * size.y;

                // Keep track of the orientation that gives the smallest bounding area.
                if (area < minArea)
                {
                    minArea = area;
                    Vector2 centerRot = (min + max) / 2f;
                    // Rotate the center back to the original coordinate space.
                    Vector2 center = RotatePoint(centerRot, Quaternion.Euler(0, 0, angleDeg));
                    bestRect.center = center;
                    bestRect.size = size;
                    bestRect.angle = angleDeg;
                }
            }

            return bestRect;
        }

        /// <summary>
        /// Rotates a 2D point by a given quaternion.
        /// </summary>
        private static Vector2 RotatePoint(Vector2 point, Quaternion rotation)
        {
            return rotation * point;
        }
    }
}

