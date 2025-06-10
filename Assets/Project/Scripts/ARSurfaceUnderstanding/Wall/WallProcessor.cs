using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GeniesIRL
{
    /// <summary>
    /// At the time of writing, this class is chiefly responsible for helping the Genie find good places to 
    /// draw pictures on walls. Note that this script assumes walls are perfictly vertical, which, I'm pretty sure they 
    /// should be on-device.
    /// </summary>
    [System.Serializable]
    public class WallProcessor
    {
        [SerializeField] 
        private DrawingSpace drawingSpacePrefab;
        [SerializeField, Tooltip("The center elevation of the box, above FloorY. Ensures the Genie will be able to reach it.")] 
        private float y = 1f;
        [SerializeField, Tooltip("For some reason the start and end points often end up going outside the walls, so we'll add some extra padding on the left and right sides to minimize this.")]
        float margin = 0.1f;
        [SerializeField, Tooltip("The Drawing Spaces should appear off the wall by a small amount, so when we detect for collisions we don't hit the wall mesh itself.")]
        private float additionalBoxOffset = 0.01f; 
        [SerializeField, Tooltip("The dimensions of the Drawing Spaces that will be spawned on the walls.")]
        private Vector3 boxDimensions = new Vector3(0.5f, 0.5f, 0.1f);
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        private ARPlaneManager _arPlaneManager;
        private List<DrawingSpace> _drawingSpacesAssignedToPlanes = new List<DrawingSpace>();
        private float _debugAutoInterval = 5f;
        private float _debugAutoTimer = -1f;

        private FloorManager _floorManager;
    
        public void OnSceneBootstrapped(ARPlaneManager arPlaneManager, FloorManager floorManager)
        {
            _arPlaneManager = arPlaneManager;
            _floorManager = floorManager;
        }

        /// <summary>
        /// Uses AR planes to generate Drawing Spaces on walls. At present, it does not attempt to validate the wall space by checking
        /// for obstructions in the spatial mesh.
        /// </summary>
        /// <returns></returns>
        public DrawingSpace[] GenerateAndFindAvailableDrawingSpaces()
        {
            // Destroy any existing drawing spaces.
            foreach (DrawingSpace drawingSpace in _drawingSpacesAssignedToPlanes)
            {
                GameObject.Destroy(drawingSpace.gameObject);
            }

            _drawingSpacesAssignedToPlanes.Clear();

            foreach (ARPlane plane in _arPlaneManager.trackables)
            {
                if (!IsWall(plane)) continue;

                // Spawn drawing spaces on the wall if it's wide enough.
                if (GetStartAndEndPoints(plane, margin, out Vector3 start, out Vector3 end)) 
                {
                    _drawingSpacesAssignedToPlanes.AddRange(GenerateDrawingSpacesForPlane(plane, start, end, boxDimensions, additionalBoxOffset));
                }
            }

            // Find any drawing spaces in the world, including ones that were manually placed in the scene
            // by a developer for testing.
            DrawingSpace[] drawingSpaces = GameObject.FindObjectsByType<DrawingSpace>(FindObjectsSortMode.None);

            return drawingSpaces;
        }

        public void OnUpdate()
        {
            if (debugMode)
            {
                UpdateForDebugMode();
            }
        }

        private bool IsWall(ARPlane plane)
        {
            if (GeniesIRL.App.XR.IsPolySpatialEnabled) 
            {
                return plane.classifications == PlaneClassifications.WallFace;  
            }
            
            // At the time of writing, we can't do plane classifications in the Editor unless we're using 
            // PlayToDevice. In other words, we cannot use PlaneClassifications unless Polyspatial is enabled.

            // Here, use the plane's normal to determine if it's a wall.
            float absDot = Vector3.Dot(plane.normal, Vector3.up);
            return absDot < 0.1f;
        }

        private bool GetStartAndEndPoints(ARPlane arPlane, float margin, out Vector3 start, out Vector3 end)
        {
            start = (default);
            end = (default);

            Vector3 center = arPlane.center;

            // Check to make sure the plane can contain the margins, otherwise, fail the operation.
            if (arPlane.size.x < margin * 2)
            {
                return false;
            }

            // Get the left and right directions.
            Vector3 left = Vector3.Cross(arPlane.normal, Vector3.up).normalized;
            Vector3 right = -left;
            
            // Now get the points at each edge of the wall plane.
            start = center + left * (arPlane.extents.x - margin);
            end = center + right * (arPlane.extents.x + margin);

            // Set the y value to ensure our Genie can actually reach the wall.
            start.y = y + _floorManager.FloorY;
            end.y = y + _floorManager.FloorY;

            // Ensure these start and end points fall within the y bounds of the plane.
            if (start.y < arPlane.center.y - arPlane.extents.y || start.y > arPlane.center.y + arPlane.extents.y)
            {
                return false;
            }

            // Draw the lines
            Debug.DrawLine(start, end, Color.red, 5f); 

            return true; // Success           
        }

        /// <summary>
        /// Spawns cubes in a line between startPosition and endPosition.
        /// Each cube's transform.forward is set to forwardDirection.
        /// The cubes are spaced so that the left edge of the first cube is at startPosition,
        /// and then each subsequent cube is placed to the right (along the line from startPosition to endPosition)
        /// until you can no longer fit another full box before reaching endPosition.
        /// Additionally, each cube is offset along its own transform.forward by half of its depth (boxSize.z),
        /// so that the "back" of the cube lies on the line between start and end.
        /// </summary>
        /// <param name="startPosition">World-space position of the leftmost edge of the first cube.</param>
        /// <param name="endPosition">World-space position that defines how far to place cubes to the right.</param>
        /// <param name="forwardDirection">The direction each cube should face. (cube.transform.forward)</param>
        /// <param name="boxSize">The local scale of each box (x = width, y = height, z = depth).</param>
        private List<DrawingSpace> GenerateDrawingSpacesForPlane(ARPlane wallPlane, Vector3 startPosition, Vector3 endPosition, Vector3 boxSize, float additionalBoxOffset)
        {
            List<DrawingSpace> drawingSpaces = new List<DrawingSpace>();

            // 1. Calculate the distance between start and end
            float distance = Vector3.Distance(startPosition, endPosition);

            // 2. If we can't fit even one box width, exit immediately
            if (distance < boxSize.x)
            {
                Debug.Log("Not enough room to place even one box.");
                return drawingSpaces;
            }

            // 3. Determine how many boxes we can fit
            //    We'll place boxes at intervals of 'boxSize.x' along the line from start to end.
            int numBoxes = Mathf.FloorToInt(distance / boxSize.x);
            if (numBoxes <= 0)
            {
                Debug.Log("No boxes fit within the given distance.");
                return drawingSpaces;
            }

            // 4. Normalize the direction from start to end. We'll call this our "horizontal" direction.
            Vector3 horizontalDir = (endPosition - startPosition).normalized;

            // 5. Optionally, normalize the forward direction (good practice, ensures uniform scale).
            Vector3 fwdDir = wallPlane.normal;

            // 6. Loop through and spawn the boxes
            for (int i = 0; i < numBoxes; i++)
            {
                // a) Create the drawing space.
                DrawingSpace drawingSpace = GameObject.Instantiate(drawingSpacePrefab); 

                // b) Compute the position.
                //    - (i + 0.5f) * boxSize.x moves the cube's *center* so that the left edge
                //      is exactly at 'startPosition' for the first cube, then one box-width apart
                //      for each subsequent cube.
                //    - Add half of the box's depth (boxSize.z / 2f) in the forward direction
                //      so that the "back" of the cube intersects the line (start->end).
                Vector3 boxCenterOffset = horizontalDir * ((i + 0.5f) * boxSize.x);
                Vector3 boxForwardOffset = fwdDir * ((boxSize.z * 0.5f) + additionalBoxOffset);
                Vector3 finalPosition = startPosition + boxCenterOffset + boxForwardOffset;
                
                // c) Set the box's position in the world
                drawingSpace.transform.position = finalPosition;

                // d) Orient the box so that its .forward is the specified forward direction
                drawingSpace.transform.rotation = Quaternion.LookRotation(fwdDir);

                // e) Scale the box so that it has the desired width (x), height (y), and depth (z)
                drawingSpace.transform.localScale = boxSize;

                // f) Initialize the drawing space and add it to the list.
                drawingSpace.AssignToARPlane(wallPlane, debugMode);
                drawingSpaces.Add(drawingSpace);
            }

            return drawingSpaces;
        }

        private void UpdateForDebugMode()
        {
            // Every interval, auto-refresh the available drawing spaces and evaluate each one.
            if (_debugAutoTimer == -1 || _debugAutoTimer >= _debugAutoInterval)
            {
                Debug.Log("Refresh Drawing Spaces.");
                // Generate drawing spaces on AR planes and find any others in the world that the developer may have placed.
                DrawingSpace[] drawingSpaces = GenerateAndFindAvailableDrawingSpaces();

                // For debug mode, evaluate each drawing space now. (Typically this would only happen while the Genie is deciding where to draw, to cut
                // down on raycasts.) This will allow us to instantly see which drawing spaces are valid and which are not.
                foreach (DrawingSpace drawingSpace in drawingSpaces)
                {
                    drawingSpace.Evaluate();
                }

                _debugAutoTimer = 0;
            }
            else
            {
                _debugAutoTimer += Time.deltaTime;
            }
        }
    }
}

