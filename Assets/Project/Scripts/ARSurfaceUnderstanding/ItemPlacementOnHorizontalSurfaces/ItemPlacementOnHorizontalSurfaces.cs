using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GeniesIRL 
{
    /// <summary>
    /// Used to find places to put items on horizontal surfaces. At the time of writing, it is intended to be used on surfaces classified
    /// as tables, but eventually it could be expanded to include seats as well.
    /// </summary>
    /// 
    [Serializable]
    public class ItemPlacementOnHorizontalSurfaces
    {
        [Tooltip("The maximum elevation that the Genie can place an object.")]
        [SerializeField] private float maxElevationAboveFloor = 1.5f;

        [Tooltip("The layers that the item placement system will consider when checking for obstacles.")]
        [SerializeField] private LayerMask hitmask = 1 << 1 | 1 << 29; 

        [SerializeField, Tooltip("If true, you'll be able to see debug cubes showing potential placements and validations. Green means valid, red means invalid.")]
        private bool debugMode = false;
        [SerializeField, Tooltip("If true, placement processing will happen automatically every interval, and cubes will be placed to show the potential placements and validations.")] 
        private bool debugEnableAutoUpdateEveryInterval = false;

        [SerializeField, Tooltip("Used when auto updating is enabled -- defines how often processing will happen.")] 
        private float debugInterval = 5f;
        [SerializeField] private Material debugMaterial;
        [SerializeField] private Vector3 debugItemSize = new (0.2f, 0.2f, 0.2f);

        private ARPlaneManager _arPlaneManager;

        private FloorManager _xrFloorManager;

        private List<GameObject> _debugPlacements = new List<GameObject>();

        private float _latestDebugProcessTime = 0;

        public void OnSceneBootstrapped(ARPlaneManager arPlaneManager, FloorManager xrFloorManager)
        {
            _arPlaneManager = arPlaneManager;
            _xrFloorManager = xrFloorManager;
        }

        public void OnUpdate()
        {
            if (debugMode && debugEnableAutoUpdateEveryInterval) 
            {
                // Automatically scan for placements every interval regardless of whether the Genie is currently trying to place an item. 
                // (Otherwise, the placement logic would only happen when the Genie needs it, and if debugMode is enabled, you'll see the volumes visualized as cubes.)
                UpdateAutoDebug(); 
            }
        }

        private void UpdateAutoDebug()
        {
            // Wait for the next interval
            if (Time.time - _latestDebugProcessTime > debugInterval)
            {
                _latestDebugProcessTime = Time.time;
                List<Vector3> potentialPlacements = FindPotentialItemPlacements(debugItemSize);

                foreach (Vector3 placement in potentialPlacements)
                {
                    Bounds bounds = new Bounds(placement, debugItemSize);

                    bool isValid = EvaluateVolume(ref bounds);
                }
            }
        }

        /// <summary>
        /// Scans AR Planes for spots that could fit an item of the given radius. Doesn't do any collision or pathfinding validation -- it just finds the potential spots.
        /// This is essentially a 2D operation, taking into account only the X and Z axes.
        /// NOTE: If debug mode is enabled, this function will destroy any and all debug cube visualizations, replacing them with new, "unvalidated" cubes.
        /// </summary>
        /// <param name="itemRadius"></param>
        /// <returns></returns>
        public List<Vector3> FindPotentialItemPlacements(Vector3 itemSize) 
        {   
            // To simplify things, we're going to get a "radius" for the item in 2D. This will be the diagonal of the item size on the X and Z axes.
            float itemRadius = Mathf.Sqrt(itemSize.x * itemSize.x + itemSize.z * itemSize.z) / 2;

            List<Vector3> potentialPlacements = new List<Vector3>();

            foreach (ARPlane plane in _arPlaneManager.trackables)
            {
                if (!IsHorizontalSurface(plane)) continue; // The plane must be a horizontal surface to be considered.

                if (plane.center.y > maxElevationAboveFloor + _xrFloorManager.FloorY) continue; // The plane must be below the max elevation.

                // If the plane is smaller than the item itself, just use the center point. The Genie could potentially
                // balance an object there.
                // NOTE: This is not a great way to deal with a long, narrow table, but it's a start.
                if (plane.extents.x < itemRadius || plane.extents.y < itemRadius) 
                {
                    Vector3 placementPoint = plane.center + Vector3.up * itemSize.y / 2; // Elevate the point so the whole object fits on the table.
                    potentialPlacements.Add(placementPoint);
                    continue;
                }

                // Next, we're going to use the plane's boundary, that is, the points that define the plane's shape.
                foreach (Vector2 localVertex in plane.boundary)
                {
                    // Convert from plane space to world space.
                    Vector3 vertex = plane.transform.TransformPoint(new Vector3(localVertex.x, 0, localVertex.y));
                    Vector3 dirToCenter = (plane.center - vertex).normalized;

                    // Find a point on the edge that can contain the item.
                    Vector3 pointOnEdge = vertex + dirToCenter * itemRadius;
                    Vector3 placementPoint = pointOnEdge + Vector3.up * itemSize.y / 2; // Elevate the point so the whole object fits on the table.

                    potentialPlacements.Add(placementPoint);
                }
            }

            if (debugMode) 
            {
                _debugPlacements.ForEach((gameObject) => GameObject.Destroy(gameObject));
                _debugPlacements.Clear();

                foreach (Vector3 placement in potentialPlacements)
                {
                    Bounds bounds = new Bounds(placement, itemSize);
                    GetOrCreateDebugCube(bounds); // Create an "unvalidated" debug cube for each potential placement.
                }
            }
  
            return potentialPlacements;
        }

        private bool IsHorizontalSurface(ARPlane plane)
        {
            // If Polyspatial is enabled, use the plane's classification as a table to ensure we're only looking at tables.
            // (We may change this later to include seats as well.)
            if (GeniesIRL.App.XR.IsPolySpatialEnabled) 
            {
                if (plane.classifications != PlaneClassifications.Table)
                {
                    return false;
                }
            }

            // To prevent the genie from placing objects on the floor, let's make sure the plane is high enough off the ground.
            if (plane.center.y < _xrFloorManager.FloorY + 0.1f) 
            {
                return false;
            }
            
            // Additionally, check the plane's normal to make sure it's mostly pointing up.
            float absDot = Vector3.Dot(plane.normal, Vector3.up);
            return absDot > 0.9f;
        }
        
        /// <summary>
        /// Checks the volume for obstacles and finds a clear space for the item. If it finds a clear space, it returns true and modifies
        /// the bounds to reflect the valid item volume.
        /// <param name="bounds">Size should be the item size.</param>
        /// <returns></returns>
        public bool EvaluateVolume(ref Bounds bounds)
        {
            GameObject debugCube = debugMode ? GetOrCreateDebugCube(bounds) : null; // Set up for debug mode, if needed.

            float maxElevation = maxElevationAboveFloor + _xrFloorManager.FloorY;

            float tableY = bounds.min.y;

            float startElevation = 0.01f; // Starting height off top of table.
            float elevationBetweenChecks = 0.05f; // The height distance between checks.
            int numberOfChecks = 3; // The number of checks to make at different heights.

            // For each check, we'll move the bounds up by the elevationBetweenChecks amount.
            for (int i = 0; i < numberOfChecks; i++)
            {
                float y = tableY + bounds.extents.y + startElevation + elevationBetweenChecks * i;

                if (y > maxElevation) 
                {
                    return false; // We've exceeded the max elevation -- the Genie cannot reach this placement.
                }

                bounds.center = new Vector3(bounds.center.x, y, bounds.center.z);

                if (debugMode) debugCube.transform.position = bounds.center; // Position the debug cube at the current elevation.

                // Check for any obstacles in the volume.
                if (!Physics.CheckBox(bounds.center, bounds.extents, Quaternion.identity, hitmask))
                {
                    // The volume is clear.
                    Debug.Log("Volume is clear at elevation " + i);
                    if (debugMode) SetDebugCubeColor(debugCube, Color.green); // Make the debug cube appear green.

                    return true; 
                }
                
                continue; // The volume is obstructed. Try the next elevation.
            }

            // The volume is obstructed at all elevations.
            Debug.Log("Volume is obstructed at all elevations.");
            if (debugMode) SetDebugCubeColor(debugCube, Color.red); // Make the debug cube appear red.

            return false; 
        }

        private GameObject GetOrCreateDebugCube(Bounds bounds)
        {
            // Check to see if we already have a debug cube for this volume. Use the center point as a key.
            foreach (GameObject cube in _debugPlacements)
            {
                if ((cube.transform.position - bounds.center).sqrMagnitude < 0.01f)
                {
                    return cube;
                }
            }

            GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newCube.name = "DebugPlacementCube";
            GameObject.Destroy(newCube.GetComponent<Collider>());
            newCube.transform.position = bounds.center;
            newCube.transform.localScale = bounds.size;
            newCube.GetComponent<Renderer>().material = debugMaterial;
            _debugPlacements.Add(newCube);

            return newCube;
        }

        private void SetDebugCubeColor(GameObject debugCube, Color color)
        {
            MeshRenderer meshRenderer = debugCube.GetComponent<MeshRenderer>();
            float alpha = meshRenderer.material.color.a;
            meshRenderer.material.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}

