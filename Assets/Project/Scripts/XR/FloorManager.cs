using System;
using System.Collections;
using System.Collections.Generic;
using GeniesIRL.App;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Manages functionality related to the floor. 
    /// </summary>
    public class FloorManager : MonoBehaviour
    {
        [Header("Safety Floor")]
        [Tooltip("If ticked, an invisible floor will spawn at the floor position to catch Items that might otherwise fall through the floor.")]
        [SerializeField] private bool enableSafetyFloor;
        [ConditionalField("enableSafetyFloor"), SerializeField] private float safetyFloorX = 40f;
        [ConditionalField("enableSafetyFloor"), SerializeField] private float safetyFloorZ = 40f;

        /// <summary>
        /// Starts at zero, but gets updated as new flooring information is recieved.
        /// </summary>
        public float FloorY { get; private set; } = 0;

        /// <summary>
        /// Fires when the floor height is recalibrated. The parameter is the new floor height.
        /// </summary>
        public event Action<float> OnFloorRecalibrated;

        [SerializeField, Tooltip("Any floor change must occur by at least this amount in order to trigger a change in FloorY. This is too" +
            " jittery behavior")]
        private float floorYChangeThreshold = 0.05f;

        [Header("Debug")]
        [SerializeField, Tooltip("The floor Y value to use in the editor when Polyspatial is disabled (i.e., when we're not using Play to Device)." +
            " Useful for testing how rest of the application behaves when the floor height is chagned at runtime, which is what happens in PolySpatial when correct " + 
            " floor hieght based on new information.")]
        private float debugEditorFloorY = 0;

        [SerializeField, Tooltip("When ticked, will display a cube at the XZ origin, with the Y value set to the current FloorY.")]
        private bool debugEnableFloorYVisualizer = false;
        private GameObject _debugFloorYVisualizer = null;

        [NonSerialized]
        private XRNode _xrNode;

        private GameObject safetyFloor;

        private float? prevArFloorDetectionFloorY = null;

        public void OnInitialize(XRNode xrNode)
        {
            _xrNode = xrNode;

            if (enableSafetyFloor)
            {
                StartCoroutine(SetupSafetyFloor());
            }

            if (Application.isEditor)
            {
                StartCoroutine(DebugUpdateEditorFloorY_C());
            }

            if (debugEnableFloorYVisualizer)
            {
                _debugFloorYVisualizer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _debugFloorYVisualizer.transform.SetParent(this.transform);
                _debugFloorYVisualizer.name = "FloorY Visualizer";
                GameObject.Destroy(_debugFloorYVisualizer.GetComponent<Collider>());
                _debugFloorYVisualizer.transform.localScale = new Vector3(0.5f, 0.025f, 0.5f);
            }
        }
    
        private IEnumerator SetupSafetyFloor()
        {
            yield return null; // Wait a couple frames to allow for ar stuff to initialize
            yield return null;

            safetyFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safetyFloor.name = "Safety Floor";
            safetyFloor.layer = LayerMask.GetMask("SefetyFloorCollider");
            safetyFloor.transform.localScale = new Vector3(safetyFloorX, FloorY, safetyFloorZ);
            Destroy(safetyFloor.GetComponent<Renderer>());
        }

        // Runs while in the editor and while Polyspatial is disabled.
        private IEnumerator DebugUpdateEditorFloorY_C()
        {
            while (true) 
            {
                if (Mathf.Abs(FloorY - debugEditorFloorY) >= floorYChangeThreshold)
                {
                    Debug.Log("[XRFloorManager] FloorY changed to " + debugEditorFloorY);
                    float oldFloorY = FloorY;
                    FloorY = debugEditorFloorY;
                    OnFloorRecalibrated?.Invoke(FloorY);
                }

                yield return null;
            }
        }

        private void Update ()
        {
            if (App.XR.IsPolySpatialEnabled) 
            {
                ManageRecalibration(); // Use ARFloorDetection data to determine floor hieght (only works on device).
            }
            
            if (_debugFloorYVisualizer != null)
            {
                Vector3 pos = _xrNode.xrOrigin.transform.position;
                pos.y = FloorY;
                _debugFloorYVisualizer.transform.position = pos;
            }
        }

        private void ManageRecalibration()
        {
            if (_isRecalibrating) return; // Wait for current recalibration to finish.

            if (_xrNode.arFloorDetection.FloorY == null) return; // Wait for the floor detection to give us a value.

            ARNavigation arNav;

            if (_xrNode.Bootstrapper != null)
            {
                // Normal Codepath -- grab ARNavigation from the bootstrapper.
                arNav = _xrNode.Bootstrapper.ARNavigation; 
            }
            else
            {
                // Debug environment. XRNode was not spawned by the Bootstrapper. Try to find ARNavigation by doing a general FindFirstObject
                arNav = FindFirstObjectByType<ARNavigation>(); 
            }

            // NOTE: It's still possible for arNav to be null here if we're in a debug environment. 

            float actualFloorY = _xrNode.arFloorDetection.FloorY.Value; // This is the most up-to-date floor height based on ARFloorDetection.

            bool isFirstTimeGettingFloorDetectionValue = prevArFloorDetectionFloorY == null; // If this is the first time we're getting a floor y value.
            bool didFloorChangeEnough = Math.Abs(actualFloorY - FloorY) >= floorYChangeThreshold; // FloorY has moved a significant amount.
            bool isFloorAboveGridGraph = arNav != null && arNav.AstarPath.data.gridGraph.center.y - actualFloorY >= 0.01f; // The Grid height is too far above the floor height, which can lead to a bad scan.

            bool shouldRecalibrate = isFirstTimeGettingFloorDetectionValue || didFloorChangeEnough || isFloorAboveGridGraph;
                
            if (shouldRecalibrate)
            {
                Debug.Log("Recalibrating because: " +
                    "isFirstTimeGettingFloorDetectionValue: " + isFirstTimeGettingFloorDetectionValue + ", " +
                    "didFloorChangeEnough: " + didFloorChangeEnough + ", " +
                    "isFloorAboveGridGraph: " + isFloorAboveGridGraph);
                _isRecalibrating = true;
                StartCoroutine(Recalibrate_C());
            }
        }

        private IEnumerator Recalibrate_C()
        {
            yield return new WaitForSeconds(2f); // 2s seems like a good recalibration time. (TODO: consider using a more scientific approach to knowing when a "good" scan has been achieved.)
            float oldFloorY = FloorY;
            FloorY = _xrNode.arFloorDetection.FloorY.Value;
            prevArFloorDetectionFloorY = _xrNode.arFloorDetection.FloorY;
            _isRecalibrating = false;

            OnFloorRecalibrated?.Invoke(FloorY);
        }

        private bool _isRecalibrating = false;
    }
} 