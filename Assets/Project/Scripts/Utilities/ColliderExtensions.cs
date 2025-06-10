using GeniesIRL;
using UnityEngine;

namespace GeneisIRL 
{
    public static class ColliderExtensions
    {
        /// <summary>
        /// Returns true if any part of the collider is within the given radius (in the XZ plane) of the target point.
        /// </summary>
        /// <param name="col">The collider to check.</param>
        /// <param name="target">The point (in world space) to check against.</param>
        /// <param name="radius">The radius to check within (in the XZ plane).</param>
        public static bool IsWithinXZRadius(this Collider col, Vector3 target, float radius)
        {
            // Use a Y value that is definitely within the collider.
            // Using the collider's bounds center is a good choice.
            float y = col.bounds.center.y;
            
            // Create a query point that has the target's X and Z but the chosen Y.
            Vector3 queryPoint = new Vector3(target.x, y, target.z);

            // If the bounds doesn't even contain the query point, so we can stop here.
            if (!col.bounds.Contains(queryPoint)) return false; 
            
            // Get the point on the collider that is closest to queryPoint.
            Vector3 closest = col.ClosestPoint(queryPoint);

            bool isWithinRadius = VectorUtils.IsWithinDistanceXZ(closest, target, radius);

            return isWithinRadius;
        }

        /// <summary>
        /// More accurate than bounds.Countains, because this takes into account the actual shape of the collider and its rotation.
        /// </summary>
        /// <param name="collider"></param>
        /// <param name="point"></param>
        public static bool Contains(this Collider collider, Vector3 point)
        {
            Vector3 closestPoint = collider.ClosestPoint(point);

            if (VectorUtils.Approximately(closestPoint, point))
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Get the bounds of the target item when it it is not rotated at all. The easiest way to do this is to temporarily zero out the rotation, 
        /// get the bounds, and then restore the rotation.
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static Bounds GetBoundsWhenNotRotated(this Collider collider) 
        {
            Transform transform = collider.transform;

            Quaternion origItemRot = transform.rotation;
            transform.rotation = Quaternion.identity;
            Physics.SyncTransforms(); // This will force the collider bounds to reflect the new rotation. It can be expensive, so if we need an alternate solution here we can look into it.
            Bounds bounds = collider.bounds;
            transform.rotation = origItemRot;
            return bounds;
        }
    }
}

