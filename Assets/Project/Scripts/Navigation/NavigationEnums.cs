using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Arrival evaluation types are used to determine if a character has arrived at a destination.
    /// </summary>
    public enum NavArrivalEvaluationType
    {
        /// <summary>
        /// Arrival happens only when the character has reached the path, which is as close as they can get to the destination. No checks are made to see if that path
        /// is within any specific range of the destination, and this Evaluation cannot fail.
        /// </summary>
        ReachEndOfPath,
        /// <summary>
        /// 'Character arrives' as soon as they are within a certain distance of the destination. If they cannot reach this distance, the Action will fail.
        /// </summary>
        StopAtArrivalDistance,
        /// <summary>
        /// Character will try to reach the exact destination, but if it can't, it will get as close as possible. If it can't even reach the arrival distance,
        /// the Action will fail.
        /// </summary>
        ClosestWithinArrivalDistance,
        /// <summary>
        /// Character will attempt to reach an 'ideal' arrival distance and stop. If they cannot reach this distance, they will get as close as possible. 
        /// If they cannot even reach the basic arrival distance, the Action will fail.
        /// </summary>
        GetAsCloseToIdealDistanceAsPossible,
        /// <summary>
        /// Arrival is determined by whether the character is able to reach it with their arms. This will take into account the Genie's
        /// arm reach length, as well as her ability to lean forward in certain situations.
        /// </summary>
        ReachableWithArms,
        /// <summary>
        /// Uses a line segment with an ideal point and an end point. The nav system will try to reach the ideal point exactly, but if it can't, it will
        /// scan along the line segment to find the closest point to the target that it can reach. If it can't reach the end point, it will fail. Dev must provide
        /// the direction of the line segment, a basic arrival distance, and, optionally, an ideal arrival distance.
        /// </summary>
        BestPointOnLineSegment,
    }
}
