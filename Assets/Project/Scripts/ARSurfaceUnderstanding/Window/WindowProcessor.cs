using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GeniesIRL
{
    /// <summary>
    /// Looks for AR Planes marked as Windows figures out where the Genie can stand in order to look out of them.
    /// </summary>
    [System.Serializable]
    public class WindowProcessor
    {
        [SerializeField] private Window windowPrefab;
        [SerializeField] private bool debugMode = false;
        private ARPlaneManager _arPlaneManager;

        private List<Window> _windowsSpawned = new List<Window>();

        public void OnSceneBootstrapped(ARPlaneManager arPlaneManager)
        {
            _arPlaneManager = arPlaneManager;
        }

        public void OnUpdate()
        {
            
        }

        /// <summary>
        /// Generates windows based on AR planes marked as WindowFrame, deleting any windows that were previously spawned. Also 
        /// returns windows that weren't spawned here but were manually placed in the scene by a developer for testing.
        /// </summary>
        /// <returns></returns>
        public List<Window> FindWindows() 
        {
            // Destroy any windows we spawned.
            foreach (Window window in _windowsSpawned)
            {
                GameObject.Destroy(window.gameObject);
            }

            _windowsSpawned.Clear();

            foreach (ARPlane plane in _arPlaneManager.trackables)
            {
                if (plane.classifications == PlaneClassifications.WindowFrame)
                {
                    GenerateWindow(plane);
                }
            }

            Window[] windows = GameObject.FindObjectsByType<Window>(FindObjectsSortMode.None);

            return new List<Window>(windows);
        }

        private void GenerateWindow(ARPlane plane)
        {
            Window window = GameObject.Instantiate(windowPrefab, plane.transform.position, plane.transform.rotation);

            window.Initialize(plane, debugMode);
            
            _windowsSpawned.Add(window);
        }
    }
}
