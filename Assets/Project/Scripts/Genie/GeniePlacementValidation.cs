using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using System.Linq;

namespace GeniesIRL
{
    /// <summary>
    /// Uses the nav grid in an attempt to find a suitable location to spawn or teleport a Genie character.
    /// </summary>
    public class GeniePlacementValidation
    {
        /// <summary>
        /// Serializable class you can use to define placement settings, such as min/max range and FOV
        /// </summary>
        [System.Serializable]
        public class GeniePlacementValidationSettings
        {
            [Tooltip("The minimum XZ distance from the user's head to allow genie placement.")]
            public float minDistanceFromUser = 0.75f;

            [Tooltip("The maximum XZ distance from the user's head to allow genie placement.")]
            public float maxDistanceFromUser = 4f;

            [Tooltip("The angle, in degrees, within the user's view frustum on the XZ plane, to allow genie placement. " +
                "Must be 0-180 degrees.")]
            public float fieldOfView = 60f;

            [Tooltip("Multiplier defines weight of distance adherence on score. The total of all weights when added " +
                "together should equal 100.")]
            public int distanceScoreWeight = 50;
            [Tooltip("Multiplier defines weight of directional adherence on score. The total of all weights when added " +
                "together should equal 100.")]
            public int directionScoreWeight = 60;
        }
       
        public ARNavigation ARNavigation { get; private set; }

        // The below can be useful for debugging, by exposing the underlying node detection and scoring system. Uncomment
        // these lines and their member variable declarations to visualize what's happening under the hood.
        public List<GraphNode> Nodes { get; private set; }
        public int[] NodeScores { get; private set; }
        //

        public GeniePlacementValidation(ARNavigation arNavigation)
        {
            ARNavigation = arNavigation;
        }

        /// <summary>
        /// Given the user's head position, and orientation, find a valid placement point for the Genie. on the navigation grid. Without a 'Genie'
        /// parameter, it will be used to find a valid placement point IN FRONT of the user to spawn a Genie. Otherwise, if a Genie is provided,
        /// it will be used to find a valid placement point to move the Genie to, to maintain personal space.
        /// </summary>
        /// <param name="userHead"></param>
        /// <param name="minDistanceFromUser"></param>
        /// <param name="maxDistanceFromUser"></param>
        /// <param name="fieldOfView">In degrees and disregarding any pitch rotation, what is the max 'field of view' the
        /// the spawn point must be inside of in order to spawn? Works up to 180 degrees.</param>
        /// <param name="directionScoreWeight">Multiplier defines weight of direction adherence on score. The total of all weights when added together should equal 100.</param>
        /// /// <param name="distanceScoreWeight">Multiplier defines weight of distance adherence on score. The total of all weights when added together should equal 100.</param>
        /// <param name="placementPoint">The point where we can position the Genie</param>
        /// <param name="genie">The Genie to find a valid placement point for. If null, will be used to find a valid placement point in front of the user. Otherwise, 
        /// it will use the direction from the user to the Genie, and will test out each node for pathability until it finds one that works.</param>
        /// <returns>True if a valid placement point was found, false if otherwise.</returns>
        private bool TryFindValidPlacementAwayFromUser(Transform userHead, float minDistanceFromUser, float maxDistanceFromUser,
            float fieldOfView, int directionScoreWeight, int distanceScoreWeight, out Vector3 placementPoint, Genie genie = null)
        {
            placementPoint = Vector3.zero;

            fieldOfView = Mathf.Clamp(fieldOfView, 0, 180f);

            Vector3 userPosXZ = userHead.position;
            userPosXZ.y = 0; // Disregard Y value. (NavGraph is assumed to be at the world origin)

            Vector3 forwardXZ;

            if (genie == null || VectorUtils.ApproximatelyXZ(genie.transform.position, userPosXZ)) 
            {
                // No Genie, or the user and Genie are occupying the same point in XZ. Simply use the forward direction of the user's head
                forwardXZ = userHead.forward;
                forwardXZ = Vector3.ProjectOnPlane(forwardXZ, Vector3.up);
                forwardXZ.Normalize();
            }
            else
            {
                // Use the direction from the user to the Genie. This will allow us to find a place for the Genie to walk away from the user.
                Vector3 geniePosXZ = genie.transform.position;
                geniePosXZ.y = 0;

                forwardXZ = (geniePosXZ - userPosXZ).normalized;
            }

            // Make the center of the bounds be the same as the height of the grid.
            Vector3 boundsCenter = userPosXZ;
            boundsCenter.y = AstarPath.active.data.gridGraph.center.y;
            
            // Define a perimeter around possible nodes.
            Bounds searchBounds = CalculateFovBounds(boundsCenter, forwardXZ, fieldOfView, maxDistanceFromUser);

            List<GraphNode> nodes = AstarPath.active.data.gridGraph.GetNodesInRegion(searchBounds);

            // Next, we're going to iterate through each node to find Walkable nodes within the angle and min/max range, and assigning scores to each and picking
            // the one with the best score.

            // Scoring will be based upon distance from the userPos, as well as angle from the user's head. We should use gizmos to draw a heatmap
            // of best and worst scores. We'll apply scores from 0 to 100

            int highestScore = 0;
            int highestScoringNodeIdx = 0;

            int[] nodeScores = new int[nodes.Count];

            for (int i=0; i<nodeScores.Length; i++)
            {
                GraphNode node = nodes[i];

                if (!node.Walkable) continue; // Just leave the score zero for unwalkable nodes.

                Vector3 nodePosXZ = (Vector3)node.position;
                nodePosXZ.y = 0;

                float dist = Vector3.Distance(userPosXZ, nodePosXZ);

                if (dist < minDistanceFromUser) continue; // Leave the score zero if it's too close to the user.

                // Check to make sure the node is inside the view frustum
                Vector3 dirUserToNode = (nodePosXZ - userPosXZ).normalized;

                float angleFromForward = Vector3.Angle(dirUserToNode, forwardXZ);

                if (angleFromForward * 2f > fieldOfView) continue; // Leave the score zero if it's outside of the view frustum.

                // Check to make sure the node is not on an "island" - that is, if the Genie spawns here, can it reach the user?
                bool isReachableToUser = ARNavigation.IsPathReachable(nodePosXZ, userPosXZ, 0.75f);

                if (!isReachableToUser) continue; // Leave the score zero if it's on an island.

                // SCORING

                // *******
                // 1. Determine a score based on directionality. The closer it is to the center of the user's view frustum, the higher the score.
                float directionalAdherence = 1f-Mathf.Clamp01((angleFromForward * 2f) / fieldOfView);
                int directionScore = Mathf.FloorToInt((float)directionScoreWeight * directionalAdherence);

                // *******
                // 2. Determine a score based on distance. The lower the distance, the better the score.

                float distanceScoreF = Mathf.InverseLerp(maxDistanceFromUser, minDistanceFromUser, dist);

                int distanceScore = Mathf.FloorToInt((float)distanceScoreWeight * distanceScoreF);

                // *****
                // 3. Add up for final score
                int totalScore = directionScore + distanceScore;

                nodeScores[i] = totalScore;

                // Compare with previous scores to find the highest scoring node.
                if (totalScore > highestScore)
                {
                    highestScore = totalScore;
                    highestScoringNodeIdx = i;
                }
            }

            // The below can be useful for debugging, by exposing the underlying node detection and scoring system.
            // You can use these member variables to visualize what's happening under the hood.
            Nodes = nodes;
            NodeScores = nodeScores;

            if (genie == null) 
            {
                // Null Genie means we're just looking for a good place to spawn a Genie. Pathfinding validation isn't necessary.
                // If there's a score above zero, set the placement point at the highest scoring node.
                if (highestScore > 0)
                {
                    placementPoint = (Vector3)nodes[highestScoringNodeIdx].position;
                    return true;
                }
            }
            else 
            {
                // Check each node to see if it's currently pathable by the Genie

                // Start by sorting node indices into a list with with the highest scoring nodes first.
                List<int> nodeIndicesSortedByLowToHighScore = nodeScores
                    .Select((score, index) => new { Score = score, Index = index })
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.Index)
                    .ToList();
                
                for (int i=0; i<nodeIndicesSortedByLowToHighScore.Count; i++)
                {
                    int nodeIdx = nodeIndicesSortedByLowToHighScore[i];

                    if (nodeScores[nodeIdx] == 0) 
                    {
                        return false; // No more valid nodes to check.
                    }

                    GraphNode node = nodes[nodeIdx];
                    Vector3 nodePos = (Vector3)node.position;

                    if (genie.genieNavigation.IsPathReachable(nodePos, 0.01f))
                    {
                        placementPoint = nodePos;
                        return true;
                    }
                }
            }
            
            // No valid nodes found.
            return false;
        }

        /// <summary>
        /// Given the user's head position, and orientation, find a valid placement point for the Genie. on the navigation grid.
        /// </summary>
        /// <param name="userHead"></param>
        /// <param name="settings"></param>
        /// <param name="placementPoint">The point where we can position the genie.</param>
        /// <returns>True if a valid placement was found, false if otherwise.</returns>
        public bool TryFindValidPlacementInFrontOfUser(Transform userHead, GeniePlacementValidationSettings settings, out Vector3 placementPoint)
        {
            bool success = TryFindValidPlacementAwayFromUser(userHead, settings.minDistanceFromUser, settings.maxDistanceFromUser,
                settings.fieldOfView, settings.directionScoreWeight, settings.distanceScoreWeight, out Vector3 placementPoint1);
            
            placementPoint = placementPoint1;

            return success;
        }

        /// <summary>
        /// For an already-spawned Genie, uses the user's position to find a valid target point for the Genie to move to, to maintain personal space.
        /// </summary>
        /// <param name="userHead"></param>
        /// <param name="genie"></param>
        /// <param name="settings"></param>
        /// <param name="placementPoint"></param>
        /// <returns></returns>
        public bool TryFindValidAvoidancePlacement(Transform userHead, Genie genie, GeniePlacementValidationSettings settings, out Vector3 placementPoint) 
        {
            bool success = TryFindValidPlacementAwayFromUser(userHead, settings.minDistanceFromUser, settings.maxDistanceFromUser,
                settings.fieldOfView, settings.directionScoreWeight, settings.distanceScoreWeight, out Vector3 placementPoint1);
            
            placementPoint = placementPoint1;

            return success;
        }

        private Bounds CalculateFovBounds(Vector3 center, Vector3 forward, float angle, float radius)
        {
            // Calculate the rotation based on the forward direction
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

            // Convert the angle to radians and calculate the half angle
            float halfAngle = angle * 0.5f * Mathf.Deg2Rad;

            // Calculate the extreme points of the pizza slice
            Vector3 startDirection = rotation * new Vector3(Mathf.Sin(-halfAngle), 0, Mathf.Cos(-halfAngle)) * radius;
            Vector3 endDirection = rotation * new Vector3(Mathf.Sin(halfAngle), 0, Mathf.Cos(halfAngle)) * radius;
            Vector3 forwardPoint = rotation * new Vector3(0, 0, 1) * radius;

            // Collect all points to consider for the bounds
            Vector3[] points = new Vector3[4]
            {
            center,
            center + startDirection,
            center + endDirection,
            center + forwardPoint
            };

            // Initialize min and max points for the bounding box
            Vector3 min = points[0];
            Vector3 max = points[0];

            // Iterate through all points to find the min and max bounds
            foreach (var point in points)
            {
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }

            // Create and return the Bounds
            Bounds bounds = new Bounds((min + max) * 0.5f, max - min);

            // Make sure it has a healthy Y size to capture nodes within.
            bounds.size = new Vector3(bounds.size.x, 1f, bounds.size.z); 

            return bounds;
        }

        
    }
}

