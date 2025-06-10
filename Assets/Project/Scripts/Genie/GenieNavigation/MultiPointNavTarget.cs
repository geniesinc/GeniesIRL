using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// The multi-point nav target can be optionally used by NavigateToTargetIRLAction to navigate to a target that has multiple possible destination points.
    /// If one of the points becomes unavailable, the action can try to find another suitable point.
    /// </summary>
    public class MultiPointNavTarget : MonoBehaviour
    {
        public Vector3[] Points { get; private set; } = new Vector3[0];

        /// <summary>
        /// Indicates the index of the latest point that was used by NavigateToTargetIRLAction.
        /// </summary>
        public int LatestSelectedPointIndex { get; private set; } = -1;

        public void SetPoints(Vector3[] points)
        {
            Points = points;
        }

        public void SetLatestSelectedPointIndex(int index)
        {
            LatestSelectedPointIndex = index;
        }
    }
}