using System;
using System.Collections.Generic;
using Pathfinding;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL
{
    /// <summary>
    /// Used by GeniesIRL.Genie to process navigation for Genie characters
    /// </summary>
    [System.Serializable]
    public class GenieNavigation
    {
        public AIPath AIPath { get; private set; }

        /// <summary>
        /// When true, the Genie will be repositioned each frame to the FloorY value. This is temporarily turned off when the Genie is sitting on a Chair, for example.
        /// </summary>
        public bool PositionCharacterOnFloor {get; set;} = true;

        [SerializeField] private float floorSmoothSpeed = 10f;
        [SerializeField] private float rotationAccelerationFactor = 5f;

        [Header("Debug")]
        [Tooltip("When ticked, the Genie will attempt to reach a DebugBotTarget, so long as there is one in the scene and, " +
            "only if Debug.BotTarget.EnableFollow is true.")]
        public bool followDebugBotTarget;

        private bool isInitialized = false;
        public Genie Genie {get; private set;}
        private Transform _transform;
        private DebugBotTarget _debugBotTarget;
        //private bool _allowAStarRotationWhenMoving = true;
        private float _targetRotationSpeed;
        private AIPath _aIPath;
        private float _defaultRotationSpeed;

        // private GameObject _pathPossibleTester;
        public void OnStart(Genie genie)
        {
            Genie = genie;

            _transform = genie.transform;

            AIPath = genie.GetComponent<AIPath>();
            
            _defaultRotationSpeed = AIPath.rotationSpeed;
            _targetRotationSpeed = _defaultRotationSpeed;
            
            //_pathPossibleTester = new GameObject("PathPossibleTester");

            // #if UNITY_EDITOR
            // UnityEditor.Selection.activeGameObject = _pathPossibleTester;
            // #endif
        }

        public void OnUpdate()
        {
            // Wait until the Bot has been repositioned onto the Grid
            if (!isInitialized)
            {
                if (AstarPath.active == null) return;

                NNInfo nodeInfo = AstarPath.active.GetNearest(_transform.position, NNConstraint.Default);

                if (nodeInfo.node == null || !nodeInfo.node.Walkable) return;

                Vector3 nodePosition = (Vector3)nodeInfo.node.position;
                _transform.position = nodePosition;

                isInitialized = true;
            }

            if (!isInitialized)
                return;

            // Place the character's feet on the floor.
            if (PositionCharacterOnFloor) 
            {
                var pos = Genie.transform.position;
                float targetY = Genie.GenieManager.Bootstrapper.XRNode.xrFloorManager.FloorY;
                pos.y = Mathf.Lerp(pos.y, targetY, floorSmoothSpeed * Time.deltaTime);
                Genie.transform.position = pos;
            }

            // Update rotation speed.
            float rotSpeed = Mathf.Lerp(AIPath.rotationSpeed, _targetRotationSpeed, rotationAccelerationFactor * Time.deltaTime);
            
            if (rotSpeed < 1f) rotSpeed = 0;

            AIPath.rotationSpeed = rotSpeed;

            // If there is a valid DebugBotTarget in the scene, try to reach it.
            if (_debugBotTarget == null)
            {
                _debugBotTarget = GameObject.FindFirstObjectByType<DebugBotTarget>();
            }

            if (_debugBotTarget != null && _debugBotTarget.EnableFollow)
            {
                AIPath.destination = _debugBotTarget.transform.position;
            }

            // Test reachability
            // float distanceThreshold = _pathPossibleTester.transform.localScale.x;
            // bool isPathReachable = IsPathReachable(_pathPossibleTester.transform.position, distanceThreshold);

            // Vector3 center = _pathPossibleTester.transform.position;
            // int sides = 8;
            // float angleStep = 360f / sides;
            // Color octagonColor = isPathReachable ? Color.green : Color.red;
            // Vector3[] vertices = new Vector3[sides];

            // for (int i = 0; i < sides; i++)
            // {
            //     float angleRad = Mathf.Deg2Rad * i * angleStep;
            //     vertices[i] = center + new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * distanceThreshold;
            // }

            // for (int i = 0; i < sides; i++)
            // {
            //     Debug.DrawLine(vertices[i], vertices[(i + 1) % sides], octagonColor);
            // }
        }

        /// <summary>
        /// Sets the AIStar destination and triggers the bot to start moving towards it.
        /// </summary>
        /// <param name="destination"></param>
        public void SetAStarDestination(Vector3 destination, bool enableAStarRotation = true)
        {
            AIPath.destination = destination;
            // Deal with rotation

            if (AIPath.enableRotation != enableAStarRotation)
            {
                _targetRotationSpeed = enableAStarRotation ? _defaultRotationSpeed : 0;
                AIPath.rotationSpeed = 0;
            }

            AIPath.enableRotation = enableAStarRotation;
        }
        
        /// <summary>
        /// Stops the bot from moving towards its current destination. 
        /// Based upon: https://forum.arongranberg.com/t/how-to-cancel-movement/8181/2
        /// </summary>
        public void StopNavigation()
        {
            var inf = float.PositiveInfinity;
            AIPath.destination = new Vector3(inf, inf, inf);
            AIPath.SetPath(null);

            // Fully stop Astar rotation
            AIPath.enableRotation = false;
            AIPath.rotationSpeed = 0f;
            _targetRotationSpeed = 0f;
        }

        /// <summary>
        /// Uses a simple heuristic to determine if the path to the desired position is reachable. It does this by finding
        /// the nearest node to the desired position and checking if it is within a certain distance threshold. Next, it validates
        /// whether a path to that node is possible. 
        /// </summary>
        /// <param name="desiredPosition"></param>
        /// <param name="distanceThreshold">Leave as default -1 to default to the minimum size, which is the AStarPath.active.data.gridgraph.nodeSize.</param>
        /// <returns></returns>
        public bool IsPathReachable(Vector3 desiredPosition, float distanceThreshold = -1)
        {
            bool isReachable = Genie.GenieManager.Bootstrapper.ARNavigation.IsPathReachable(_transform.position, desiredPosition, distanceThreshold);

            return isReachable;
        }

        /// <summary>
        /// To do: consider moving this into a static utility class somewhere.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static GridNode GetNearestWalkableNode(Vector3 position)
        {
            NNConstraint constraint = new NNConstraint(){constrainWalkability = true};

            NNInfo nodeInfo = AstarPath.active.GetNearest(position, constraint);

            if (nodeInfo.node == null || !nodeInfo.node.Walkable) return null;

            return nodeInfo.node as GridNode;
        }
    }
}
