using System;
using System.Collections;
using GeniesIRL.GlobalEvents;
using Unity.PolySpatial;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL
{
    /// <summary>
    /// Serves as a container for everything you need to do XR. The prefab is spawned via the GeniesIrlBootstrapper,
    /// however it is also intended to work by just dragging it into a scene.
    /// </summary>
    public class XRNode : GeniesIrlSubManager
    {
        /// <summary>
        /// True if we've acquired tracking, even once. This is helpful in bootstrapping the app.
        /// </summary>
        public bool HasAquiredTracking {get; private set;} = false;
        public ARSession arSession;
        public XROrigin xrOrigin;
        public ARPlaneManager arPlaneManager;
        public ARTrackedImageManager arTrackedImageManager;
        public ARMeshManager arMeshManager;
        public XRInputWrapper xrInputWrapper;
        public ARFloorDetection arFloorDetection;
        public FloorManager xrFloorManager;
        public VolumeCamera volumeCamera;
        private bool _isInitialized = false;

        public override void OnSceneBootstrapped(GeniesIrlBootstrapper bootstrapper)
        {
            base.OnSceneBootstrapped(bootstrapper);

            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void Start()
        {
            // In order to make the XR Node capable of running in an external, isolated scene, we'll call Initialize on start if needed.
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            _isInitialized = true;
            xrInputWrapper.OnInitialize(this);
            xrFloorManager.OnInitialize(this);

            StartCoroutine(CheckForTrackingAcquired_C());
            
            GlobalEventManager.Subscribe<GlobalEvents.DebugScanForNewSpatialMeshes>(OnDebugScanForNewSpatialMeshes);
        }

        private IEnumerator CheckForTrackingAcquired_C() 
        {
            if (Application.isEditor) 
            {
                HasAquiredTracking = true; // Assume we have tracking in the Editor.
            }

            while (xrInputWrapper.Head.transform.localPosition == Vector3.zero) yield return null;

            HasAquiredTracking = true;
        }

        private void OnDebugScanForNewSpatialMeshes(DebugScanForNewSpatialMeshes args)
        {
            arMeshManager.enabled = args.Scan;
        }


    }

}
