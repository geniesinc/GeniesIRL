using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using Pathfinding;
using System.Linq;
using GeniesIRL.GlobalEvents;
using System.Collections;

namespace GeniesIRL
{
    /// <summary>
    /// Automatically finds the AR Mesh Manager and leverages the data to update an Astar grid.
    /// </summary>
    [RequireComponent(typeof(AstarPath))]
    public class ARNavigation : GeniesIrlSubManager
    {
        public event Action OnScanComplete;
        public AstarPath AstarPath { get; private set; }

        public UserObstacleAStar UserObstacle { get; private set; }

        [SerializeField] private UserObstacleAStar userObstaclePrefab;

        [SerializeField, Tooltip("The tolerance in meters for the user's height above the floor. Anything above this height will be marked as not walkable.")]
        private float yTolerance = 0.1f;

        [SerializeField, Tooltip("The tolerance in meters for the world bounds to change in X and Z dimensions before updating the grid.")]
        private float worldBoundsToleranceForGridUpdate = 1f;

        [Header("Debug")]
        [SerializeField, Tooltip("If true, a cube will be shown in the scene to represent the scanned world size.")]
        private bool debugShowWorldSizeCube = false;
        [SerializeField, ConditionalField("debugShowWorldSizeCube"), Tooltip("If true, the world bounds will be defined by the WorldSizeCube, instead of by the scanned environment. " +
        "Can be useful for debugging changing conditions in the Editor. Only works if debugShowWorldSizeCube is true.")]
        private bool debugDefineWorldBoundsWithWorldSizeCube = false;
        [SerializeField, Tooltip("This material will be applied to the worldSizeCube")]
        private Material worldSizeCubeSharedMaterial;

        private bool _hasScannedAtLeastOnce = false;

        private bool _enableAutoNavMeshUpdates = true;

        private ARMeshManager _arMeshManager;

        [NonSerialized]
        private FloorManager _xrFloorManager;

        private GameObject _worldSizeCube;

        private bool _isScanInProgress = false;
        private bool _performAnotherScanAfterThis = false;

        public Bounds WorldBounds { get; private set; }

        private bool _isPathfindingProVersion;

        public int CountWalkableNodes()
        {
            return AstarPath.data.gridGraph.nodes.Count(node => node.Walkable);
        }

        /// <summary>
        /// Returns true if the seeker at <paramref name="start"/> can end up within
        /// <paramref name="threshold"/> world-units (2-D) of <paramref name="target"/>.
        /// Works only with a GridGraph and assumes a flat floor (Y ignored).
        /// </summary>
        public bool IsPathReachable(Vector3 start, Vector3 target, float threshold = -1f)
        {
            float astarGraphNodeSize = AstarPath.active.data.gridGraph.nodeSize;

            if (threshold < astarGraphNodeSize)
            {
                threshold = astarGraphNodeSize;
            }

            var gg = AstarPath.active.data.gridGraph;

            // ---- start node (for area test) ----------------------------------
            //var startNode = AstarPath.active.GetNearest(start).node as GridNodeBase;
            //if (startNode == null || !startNode.Walkable) return false;
            var startNode = AstarPath.active.GetNearest(start, new NNConstraint() { constrainWalkability = true }).node as GridNodeBase;
            if (startNode == null) return false;

            uint startArea = startNode.Area;

            // ---- grid coordinates of the target centre -----------------------
            var targetNode = AstarPath.active.GetNearest(target).node as GridNodeBase;
            int tx = targetNode != null ? targetNode.XCoordinateInGrid : 0;
            int tz = targetNode != null ? targetNode.ZCoordinateInGrid : 0;

            int cellRadius = Mathf.CeilToInt(threshold / gg.nodeSize);
            float sqrThresh = threshold * threshold;

            int minX = Mathf.Max(0, tx - cellRadius);
            int maxX = Mathf.Min(gg.width - 1, tx + cellRadius);
            int minZ = Mathf.Max(0, tz - cellRadius);
            int maxZ = Mathf.Min(gg.depth - 1, tz + cellRadius);

            // ---- scan square; exit on first valid hit ------------------------
            for (int z = minZ; z <= maxZ; z++)
            {
                int rowOffset = z * gg.width;
                for (int x = minX; x <= maxX; x++)
                {
                    var node = gg.nodes[rowOffset + x];
                    if (!node.Walkable || node.Area != startArea) continue;

                    Vector3 wp = (Vector3)node.position;          // node centre
                    Vector2 n2 = new Vector2(wp.x, wp.z);
                    Vector2 t2 = new Vector2(target.x, target.z);

                    if ((n2 - t2).sqrMagnitude <= sqrThresh)
                        return true;               // reachable & close enough
                }
            }
            return false;
        }


        private void Start()
        {
            _isPathfindingProVersion = IsPathfindingProVersionAvailable();

            if (_isPathfindingProVersion)
            {
                Debug.Log("***Using Astar Pathfinding Project Pro version. Scans will be performed asynchronously. " +
                          "If you intend to use the Free version, please remove the USES_ASTAR_PRO define from the project settings.***");
            }
            else
            {
                Debug.Log("*** Using Astar Pathfinding Project Free version. Scans will be performed synchronously. " +
                          "If you intend to use the Pro version, please add the USES_ASTAR_PRO define to the project settings. ***");
            }

            if (Bootstrapper != null)
            {
                _xrFloorManager = Bootstrapper.XRNode.xrFloorManager;
            }
            else
            {
                _xrFloorManager = FindFirstObjectByType<FloorManager>(); // In a debug environment, we may not have a bootstrapper.
            }

            _xrFloorManager.OnFloorRecalibrated += OnFloorRecalibrated;

            AstarPath = GetComponent<AstarPath>();
            AstarPath.OnGraphsUpdated += OnGraphsUpdated;

            StartCoroutine(InitializeAutoScan_C());

            GlobalEventManager.Subscribe<GlobalEvents.DebugEnableNavMeshUpdates>(OnDebugEnableNavMeshUpdates);

            if (debugShowWorldSizeCube)
            {
                _worldSizeCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _worldSizeCube.transform.localScale = new Vector3(10f, 0.01f, 10f);
                _worldSizeCube.GetComponent<MeshRenderer>().sharedMaterial = worldSizeCubeSharedMaterial;
                GameObject.Destroy(_worldSizeCube.GetComponent<BoxCollider>());
                _worldSizeCube.name = "WorldSizeCube";
            }
        }

        private IEnumerator InitializeAutoScan_C()
        {
            // Wait an arbitrary number of frames to ensure other systems get initialized (without doing this, we observed
            // a higher likelihood of a crash in Polyspatial in the Editor while the Behavior Graph tab is open. 
            // See: https://discussions.unity.com/t/polyspatial-behavior-graph-compatibility/1583784
            yield return null;
            yield return null;
            yield return null;

            if (Bootstrapper != null)
            {
                _arMeshManager = Bootstrapper.XRNode.arMeshManager;
            }
            else
            {
                _arMeshManager = FindFirstObjectByType<ARMeshManager>(); // In a debug environment, we may not have a bootstrapper.
            }

            _arMeshManager.meshesChanged += OnMeshesChanged;

            StartScan();
        }

        private void OnMeshesChanged(ARMeshesChangedEventArgs args)
        {
            if (_enableAutoNavMeshUpdates)
            {
                StartScan();
            }
        }

        // This can get called when the user obstacle moves around. We want to make sure the grid is properly post-processed in this case.
        private void OnGraphsUpdated(AstarPath script)
        {
            PostProccessGrid();
        }

        // This gets called when the Floor height changes due to new information from the user's surroundings.
        private void OnFloorRecalibrated(float newFloorY)
        {
            Debug.Log("FloorY changed to " + _xrFloorManager.FloorY);
            StartScan();
        }

        private void StartScan()
        {
            if (_isScanInProgress)
            {
                // Let the current scan finish before starting a new one.
                _performAnotherScanAfterThis = true;
                return;
            }

            _isScanInProgress = true;

            StartCoroutine(Scan_C());
        }

        IEnumerator Scan_C()
        {
            UpdateGridHeight();

            UpdateGridToFitWorldXZ();

            // Only the Pro version of Aron Granberg's Pathfinding Project only supports Async scanning.

            if (_isPathfindingProVersion)
            {
                // Perform async scan over multple frames to improve performance.
                foreach (var p in AstarPath.ScanAsync(AstarPath.active.data.graphs[0]))
                {
                    //Debug.Log("ScanOverManyFrames progress: " + (p.progress * 100).ToString("0") + "%");
                    yield return null;
                }
            }
            else
            {
                // Perform a syncrhonous scan if the Pro version is not available.
                AstarPath.Scan();
            }

            PostProccessGrid();

            OnScanComplete?.Invoke();

            if (!_hasScannedAtLeastOnce)
            {
                SetUpUserObstacle();
            }

            _hasScannedAtLeastOnce = true;
            _isScanInProgress = false;
        }

        private void UpdateGridHeight()
        {
            GridGraph gridGraph = AstarPath.data.gridGraph;

            // Update the grid height to match the floor height
            Vector3 newCenter = gridGraph.center;
            float yAdjustment = -0.05f; // A 5cm adjustment places the grid a bit below the floor, to grab as many nodes as possible in the area.
            newCenter.y = _xrFloorManager.FloorY + yAdjustment;
            gridGraph.center = newCenter;
        }

        private void UpdateGridToFitWorldXZ()
        {
            Bounds worldBounds = CalculateScannedWorldBounds();

            if (!IsWorldBoundsDifferentEnoughToUpdateGrid(worldBounds))
            {
                return; // No need to update the grid if the world bounds haven't changed enough to warrant it.
            }

            WorldBounds = worldBounds;

            GridGraph gridGraph = AstarPath.data.gridGraph;

            float gridNodeSize = AstarPath.data.gridGraph.nodeSize;

            // Calculate the new center for the grid
            Vector3 newCenter = WorldBounds.center;
            newCenter.x = Mathf.Round(newCenter.x / gridNodeSize) * gridNodeSize;
            newCenter.y = gridGraph.center.y; // Keep the original Y value
            newCenter.z = Mathf.Round(newCenter.z / gridNodeSize) * gridNodeSize;

            // Calculate the width and depth of the grid to fully contain the worldBounds
            float width = Mathf.Ceil(WorldBounds.size.x / gridNodeSize) * gridNodeSize;
            float depth = Mathf.Ceil(WorldBounds.size.z / gridNodeSize) * gridNodeSize;

            // Update the grid graph properties
            gridGraph.center = newCenter;
            gridGraph.Width = Mathf.CeilToInt(width / gridNodeSize);
            gridGraph.Depth = Mathf.CeilToInt(depth / gridNodeSize);
            gridGraph.SetDimensions(gridGraph.Width, gridGraph.Depth, gridNodeSize);

            // Update the debug cube to fit the new world bounds.
            if (_worldSizeCube != null)
            {
                _worldSizeCube.transform.position = newCenter;
                _worldSizeCube.transform.localScale = new Vector3(width, _worldSizeCube.transform.localScale.y, depth);
            }
        }

        private bool IsWorldBoundsDifferentEnoughToUpdateGrid(Bounds newWorldBounds)
        {
            float tolerance = worldBoundsToleranceForGridUpdate;

            if (Mathf.Abs(newWorldBounds.size.x - WorldBounds.size.x) >= tolerance ||
                Mathf.Abs(newWorldBounds.size.z - WorldBounds.size.z) >= tolerance ||
                Mathf.Abs(newWorldBounds.center.x - WorldBounds.center.x) >= tolerance ||
                Mathf.Abs(newWorldBounds.center.z - WorldBounds.center.z) >= tolerance)
            {
                return true;
            }

            return false;
        }

        private Bounds CalculateScannedWorldBounds()
        {
            if (debugDefineWorldBoundsWithWorldSizeCube && _worldSizeCube != null)
            {
                // Special debug mode that allows us to force the world bounds to be defined by the WorldSizeCube.
                return _worldSizeCube.GetComponent<MeshRenderer>().bounds;
            }

            Bounds worldBounds = new Bounds();

            foreach (var mesh in _arMeshManager.meshes)
            {
                MeshRenderer meshRenderer = mesh.GetComponent<MeshRenderer>();
                Debug.Assert(meshRenderer != null, "Every spatial mesh must have a MeshRenderer attached to it.");
                worldBounds.Encapsulate(meshRenderer.bounds);
            }

            return worldBounds;
        }

        private void PostProccessGrid()
        {
            GridGraph gridGraph = AstarPath.data.gridGraph;

            // Iterate through the list as if it were a 2D array
            for (int i = 0; i < gridGraph.depth; i++) // Row index
            {
                for (int j = 0; j < gridGraph.width; j++) // Column index
                {
                    GridNodeBase gridNode = gridGraph.GetNode(j, i);

                    Vector3 worldPosition = (Vector3)gridNode.position;

                    if (worldPosition.y > _xrFloorManager.FloorY + yTolerance)
                    {
                        gridNode.Walkable = false;
                    }
                }
            }
        }

        private void Update()
        {
            if (_performAnotherScanAfterThis && !_isScanInProgress)
            {
                // Start a new scan.
                _performAnotherScanAfterThis = false;
                StartScan();
            }

            // Debugging: Press 'U' to force a scan.
            if (Input.GetKeyDown(KeyCode.U))
            {
                StartScan();
            }
        }

        private void SetUpUserObstacle()
        {
            if (Bootstrapper == null) return; // In a debug environment, we may not have a bootstrapper.

            Vector3 userPositionXZ = Bootstrapper.XRNode.xrInputWrapper.Head.transform.position;
            userPositionXZ.y = _xrFloorManager.FloorY;

            UserObstacle = GameObject.Instantiate(userObstaclePrefab, userPositionXZ, Quaternion.identity);
            UserObstacle.OnSpawned(Bootstrapper.XRNode);
        }

        private void OnDebugEnableNavMeshUpdates(DebugEnableNavMeshUpdates args)
        {
            _enableAutoNavMeshUpdates = args.Enable;
        }

        private bool IsPathfindingProVersionAvailable()
        {
#if ASTAR_PRO
            return true;
#else
            return false;
#endif
        }

    }
}

