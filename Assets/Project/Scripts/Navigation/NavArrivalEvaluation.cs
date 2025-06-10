using UnityEngine;
using static Unity.Behavior.Node;

namespace GeniesIRL 
{
    public static class NavArrivalEvaluation
    {
        /// <summary>
        /// Simpler verion of Evaluate that returns true if the evaluation is possible, false if it's not. Used by the GoalDecider to determine whether a Goal should be attempted.
        /// </summary>
        /// <param name="evaluationType"></param>
        /// <param name="genie"></param>
        /// <param name="targetPosition"></param>
        /// <param name="basicArrivalDistance"></param>
        /// <param name="idealArrivalDistance"></param>
        /// <returns></returns>
        public static bool EvaluateIfPossible(NavArrivalEvaluationType evaluationType, Genie genie, Vector3 targetPosition, float basicArrivalDistance = -1, float idealArrivalDistance = -1, Vector3 lineSegmentDirection = default) 
        {
            Status status = Evaluate(evaluationType, genie, targetPosition, out Vector3 processedTargetPosition, basicArrivalDistance, idealArrivalDistance, lineSegmentDirection);

            return status != Status.Failure;
        }
        /// <summary>
        /// Designed to be used with NavigateToLocationIRL or NavigateToTargetIRL, determines if the Genie is done arriving at the target
        /// and returns the appropriate status.
        /// </summary>
        /// <param name="evaluationType">The type of evaluation to perform.</param>
        /// <param name="genie">The Genie that is navigating.</param>
        /// <param name="targetPosition">The position of the target.</param>
        /// <param name="basicArrivalDistance">Baseline arrival distance used in most evaluation types. If the Genie cannot reach this distance, the Action will fail.
        /// <param name="idealArrivalDistance">Only used in GetAsCloseToIdealDistanceAsPossible evaluation type. The Genie will stop at this distance or get as close as possible.</param>
        /// <param name="lineSegmentDirection">Only used in BestPointOnLineSegment evaluation type. The direction of the line segment, from the target location to the last acceptable point.</param>
        /// <param name="processedTargetPosition">The target position that the Genie should navigate to. This may be different from the original target position if the Genie cannot reach the original target.</param>
        /// <returns>The appropriate status the Action should ingest.</returns>
        public static Status Evaluate(NavArrivalEvaluationType evaluationType, Genie genie, Vector3 targetPosition, out Vector3 processedTargetPosition, float basicArrivalDistance = -1, float idealArrivalDistance = -1, Vector3 lineSegmentDirection = default)
        {
            processedTargetPosition = targetPosition;

            // ********** REACH END OF PATH **********
            // Genie is considered to have arrived at the target only if it has reached the end of its path, which is as close as they
            // can get to the target. No checks are made to see if that path gets within any specific range of the target.
            if (evaluationType == NavArrivalEvaluationType.ReachEndOfPath) 
            {
                if (genie.genieNavigation.AIPath.reachedEndOfPath)
                {
                    return Status.Success;
                }

                return Status.Running;
            }

            // ********** STOP AT ARRIVAL DISTANCE **********
            // Genie is considered to have arrived at the as soon as is within the basic arrival distance. If it cannot reach this distance,
            // the Action will fail.
            bool isWithinBasicRange = VectorUtils.IsWithinDistanceXZ(genie.transform.position, targetPosition, basicArrivalDistance);

            if (evaluationType == NavArrivalEvaluationType.StopAtArrivalDistance)
            {
                if (basicArrivalDistance < 0)
                {
                    Debug.LogError("Basic arrival distance must be set for StopAtArrivalDistance evaluation.");
                    return Status.Failure;
                }

                return ProcessStopAtArrivalDistance(genie, targetPosition, basicArrivalDistance);
            }

            // ********** CLOSEST WITHIN ARRIVAL DISTANCE **********
            // Genie will try to reach the exact destination, but if it can't, it will get as close as possible within the arrival distance.
            // If it can't at least reach the arrival distance, it will fail.
            if (evaluationType == NavArrivalEvaluationType.ClosestWithinArrivalDistance)
            {
                if (basicArrivalDistance < 0)
                {
                    Debug.LogError("Basic arrival distance must be set for ClosestWithinArrivalDistance evaluation.");
                    return Status.Failure;
                }

                if (!isWithinBasicRange)
                {
                    // We haven't made it within the basic arrival distance yet -- check to see if this is still reachable.
                    if (!genie.genieNavigation.IsPathReachable(targetPosition, basicArrivalDistance))
                    {
                        Debug.Log("Path to target not reachable.");
                        return Status.Failure; // The max range is not reachable, so we fail.
                    }

                    return Status.Running; // There's still a valid path to the basic arrival distance, so keep running.
                }

                // We've made it within the basic arrival distance. Keep moving as close as you can to the destination.

                if (genie.genieNavigation.AIPath.reachedEndOfPath)
                {
                    return Status.Success; // We've made it to the exact destination, so we can stop here.
                }

                return Status.Running; // We're still trying to reach the exact destination, so keep running.
            }

            // ********** GET AS CLOSE AS POSSIBLE TO IDEAL ARRIVAL DISTANCE **********
            // Genie will try to reach the ideal arrival distance, but if it can't, it will get as close to this distance as possible. If it can't even reach
            // the basic arrival distance, it will fail.
            if (evaluationType == NavArrivalEvaluationType.GetAsCloseToIdealDistanceAsPossible)
            {
                if (basicArrivalDistance < 0 || idealArrivalDistance < 0 || basicArrivalDistance < idealArrivalDistance)
                {
                    Debug.LogError("Basic arrival distance and ideal arrival distance must be set for GetAsCloseToIdealDistanceAsPossible evaluation, and Ideal Distance must be less than or equal to Basic Arrival Distance.");
                    return Status.Failure;
                }

                return ProcessGetAsCloseToIdealDistanceAsPossible(genie, targetPosition, basicArrivalDistance, idealArrivalDistance);
            }

            // ********** REACHABLE WITH ARMS **********
            // Uses the Genie's arm reach length and other determining factors to see if the Genie can get close enough to the target to 
            // reach it with her arms.
            if (evaluationType == NavArrivalEvaluationType.ReachableWithArms)
            {
                // Prototype code -- get reach info from the GenieIKComponent.
                float basicArmReachLength = GeniesIKComponent.dontLeanInsideXZDistance;
                float maxReachWithLeaning = GeniesIKComponent.maxReachWithLeaning;

                float minYForLeaning = GeniesIKComponent.dontLeanBelowHeight;
                float maxYForLeaning = GeniesIKComponent.dontLeanAboveHeight;

                float targetY = targetPosition.y - genie.transform.position.y;

                if (targetY >= minYForLeaning && targetY <= maxYForLeaning)
                {
                    // Genie can lean forward to reach the target (but she'll try not to lean).
                    return ProcessGetAsCloseToIdealDistanceAsPossible(genie, targetPosition, maxReachWithLeaning, basicArmReachLength);
                }

                // Genie can't lean forward at all to reach the target. Just have her stop at arms length.
                return ProcessStopAtArrivalDistance(genie, targetPosition, basicArmReachLength);
            }

            // ********** BEST POINT ON LINE SEGMENT **********
            // Uses a line segment with an target point and an end point. The nav system will try to reach the target point exactly, but if it can't, it will
            // scan along the line segment to find the closest point to the target that it can reach. If it can't reach the end point, it will fail.
            if (evaluationType == NavArrivalEvaluationType.BestPointOnLineSegment)
            {
                Vector3 idealPoint = targetPosition;

                if (lineSegmentDirection == Vector3.zero)
                {
                    Debug.LogError("Line segment direction must be set for BestPointOnLineSegment evaluation.");
                    return Status.Failure;
                }
                
                // Use the ideal arrival distance as the "starting point" of the line segment.

                if (idealArrivalDistance > 0)
                {
                    Debug.Assert(idealArrivalDistance < basicArrivalDistance, "Ideal arrival distance must be less than basic arrival distance.");
                    idealPoint = idealPoint + lineSegmentDirection * idealArrivalDistance;
                }

                Vector3 pointToCheck = idealPoint;
                float checkDistInterval = AstarPath.active.data.gridGraph.nodeSize;
                bool isPathable = false;

                while ((pointToCheck - targetPosition).sqrMagnitude < basicArrivalDistance * basicArrivalDistance)
                {
                    // Pick the best point on the line segment that we can reach.
                    if (genie.genieNavigation.IsPathReachable(pointToCheck))
                    {
                        isPathable = true;
                        break;
                    }

                    pointToCheck = pointToCheck + lineSegmentDirection * checkDistInterval;
                }

                processedTargetPosition = pointToCheck; // Update the AStarDestination to point to this new location

                Vector3 lastPossiblePoint = targetPosition + lineSegmentDirection * basicArrivalDistance;

                if (!isPathable)
                {
                    Debug.DrawLine(targetPosition, lastPossiblePoint, Color.red, 5f);
                    Debug.Log("BestPointOnLineSegment: No viable path to any point on segment.");

                    return Status.Failure;
                }

                Debug.DrawLine(idealPoint, pointToCheck, Color.green);
                Debug.DrawLine(pointToCheck, lastPossiblePoint, Color.yellow);

                if (genie.genieNavigation.AIPath.reachedEndOfPath)
                {
                    return Status.Success;
                }

                return Status.Running;
            }

            Debug.Log("Invalid NavArrivalEvaluationType: " + evaluationType);
            return Status.Failure;
        }

        private static Status ProcessStopAtArrivalDistance(Genie genie, Vector3 targetPosition, float basicArrivalDistance)
        {
            bool isWithinBasicRange = VectorUtils.IsWithinDistanceXZ(genie.transform.position, targetPosition, basicArrivalDistance);

            // We're within range. Stop immediately.
            if (isWithinBasicRange)
            {
                return Status.Success;
            }

            // We're not within range -- check if the path is still reachable.
            if (!genie.genieNavigation.IsPathReachable(targetPosition, basicArrivalDistance))
            {
                Debug.Log("Path to location is no longer reachable. Action failed.");
                return Status.Failure;
            }

            // The path is still reachable -- keep running.
            return Status.Running;
        }

        private static Status ProcessGetAsCloseToIdealDistanceAsPossible(Genie genie, Vector3 targetPosition, float basicArrivalDistance, float idealArrivalDistance)
        {
            bool isWithinBasicRange = VectorUtils.IsWithinDistanceXZ(genie.transform.position, targetPosition, basicArrivalDistance);

            if (!isWithinBasicRange)
            {
                // We haven't made it to the basic arrival distance yet -- check to see if this is still reachable.
                if (!genie.genieNavigation.IsPathReachable(targetPosition, basicArrivalDistance))
                {
                    Debug.Log("Path to target is no longer reachable. Action failed.");
                    return Status.Failure; // The max range is not reachable, so we fail.
                }

                return Status.Running; // There's still a valid path to the basic arrival distance, so keep running.
            }

            // We've made it to the basic arrival distance. Check to see if we're in the ideal range.

            bool isWithinIdealRange = VectorUtils.IsWithinDistanceXZ(genie.transform.position, targetPosition, idealArrivalDistance);

            if (isWithinIdealRange)
            {
                return Status.Success; // We've arrived at the ideal range -- stop immediately here, as this is the best place for the Genie to be.
            }

            // We're not within the ideal range yet. Get as close as possible.
            if (genie.genieNavigation.AIPath.reachedEndOfPath)
            {
                Debug.Log("Couldn't make it to the best range of the target, but we got as close as possible. Stopping here.");
                return Status.Success; // We can't make it to the best range, but we've made it to the max range, so we can stop here.
            }

            return Status.Running;
        }
    }
}

