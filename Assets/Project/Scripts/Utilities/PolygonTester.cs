using UnityEngine;

namespace GeniesIRL 
{
    public static class PolygonTester
    {
        /// <summary>
        /// Determines whether a point is inside a convex polygon.
        /// </summary>
        /// <param name="polygon">An array of Vector2 that defines a convex polygon (ordered vertices).</param>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is inside the polygon; otherwise, false.</returns>
        public static bool IsPointInConvexPolygon(Vector2[] polygon, Vector2 point)
        {
            // A valid convex polygon must have at least 3 vertices.
            if (polygon == null || polygon.Length < 3)
            {
                Debug.LogError("Polygon must have at least 3 vertices.");
                return false;
            }

            // This variable will hold the sign of the cross product.
            bool? isPositive = null;

            // Loop over each edge of the polygon.
            for (int i = 0; i < polygon.Length; i++)
            {
                // Get the next vertex index, wrapping around at the end.
                int nextIndex = (i + 1) % polygon.Length;

                // Compute the edge vector.
                Vector2 edge = polygon[nextIndex] - polygon[i];

                // Compute the vector from the current vertex to the point.
                Vector2 pointVector = point - polygon[i];

                // Compute the cross product of edge and pointVector.
                float cross = edge.x * pointVector.y - edge.y * pointVector.x;

                // Use a small threshold to handle floating-point imprecision.
                if (Mathf.Abs(cross) < Mathf.Epsilon)
                {
                    // The point is exactly on the edge; you might consider this "inside".
                    continue;
                }

                // Determine the sign of the cross product.
                bool currentSign = cross > 0f;

                // If this is the first non-zero cross product, store its sign.
                if (isPositive == null)
                {
                    isPositive = currentSign;
                }
                // If the current sign doesn't match the stored sign, the point is outside.
                else if (isPositive != currentSign)
                {
                    return false;
                }
            }

            // If we got through all edges without finding a discrepancy, the point is inside.
            return true;
        }
    }

}
