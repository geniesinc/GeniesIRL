using GeniesIRL.GlobalEvents;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL 
{
    [RequireComponent(typeof(ARPlaneManager))]
    public class DebugARPlaneVisualization : MonoBehaviour
    {
        public bool visualizeARPlanes = false;
        private ARPlaneManager _arPlaneManager;
        private bool _visualizeARPlanesLastFrame = false;
        private bool _visualizerHelperOriginallyEnabledInPrefab = false;

        private void Start()
        {
            _arPlaneManager = GetComponent<ARPlaneManager>();
            _visualizerHelperOriginallyEnabledInPrefab = _arPlaneManager.planePrefab.GetComponent<DebugARPlaneVisualizationHelper>().enabled;
            GlobalEventManager.Subscribe<GlobalEvents.DebugVisualizeARPlanes>(OnVisualizeARPlanes);
        }

        private void OnVisualizeARPlanes(DebugVisualizeARPlanes args)
        {
            visualizeARPlanes = args.Show;
        }

        private void Update()
        {
            if (_visualizeARPlanesLastFrame != visualizeARPlanes)
            {
                _visualizeARPlanesLastFrame = visualizeARPlanes;

                UpdateARPlaneVisualization();
            }
        }

        private void UpdateARPlaneVisualization()
        {
            // Update the visualization of the already existing planes.
            foreach (var plane in _arPlaneManager.trackables)
            {
                plane.GetComponent<DebugARPlaneVisualizationHelper>().enabled = visualizeARPlanes;
            }

            // Temporarily modify the prefab future planes will have the correct setting.
            _arPlaneManager.planePrefab.GetComponent<DebugARPlaneVisualizationHelper>().enabled = visualizeARPlanes;
        }

        private void OnDestroy()
        {
            // Restore prefab values
            _arPlaneManager.planePrefab.GetComponent<DebugARPlaneVisualizationHelper>().enabled = _visualizerHelperOriginallyEnabledInPrefab;
        }
    }

}
