using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.VisionOS;

namespace GeniesIRL
{
    public class GeniesIrlBootstrapper : MonoBehaviour
    {
        public static GeniesIrlBootstrapper Instance;

        // GeniesIrl SubManagers //
        public XRNode XRNode { get; private set; }
        public ARNavigation ARNavigation { get; private set; }
        public GenieManager GenieManager { get; private set; }
        public UIManager UIManager { get; private set; }
        public LaunchUX LaunchUX { get; private set; }
        public ARSurfaceUnderstanding ARSurfaceUnderstanding { get; private set; }
        public AutoItemSpawner AutoItemSpawner { get; private set; }

        public XRImageTrackingObjectManager XRImageTrackingObjectManager { get; private set; }

        [Tooltip("When ticked, the app will skip scanning, tutorial text, etc. and get straight to trying to spawn a Genie.")]
        public bool DebugSkipLaunchUX = false;

        [Header("Scene Setup")]
        [Space(8)]
        [SerializeField] XRNode xrNodePrefab;
        [SerializeField] GenieManager genieManagerPrefab;
        [SerializeField] UIManager uiManagerPrefab;
        [SerializeField] ARNavigation arNavigationPrefab;
        [SerializeField] LaunchUX launchUXPrefab;
        [SerializeField] ARSurfaceUnderstanding arSurfaceUnderstandingPrefab;
        [SerializeField] AutoItemSpawner autoItemSpawnerPrefab;
        [SerializeField] XRImageTrackingObjectManager xrImageTrackingObjectManagerPrefab;
        [SerializeField] PermissionsRequiredWarning permissionsRequiredWarningPrefab;
        
        private void Awake()
        {
            Instance = this;

            StartCoroutine(Bootstrap_C());
        }

        private IEnumerator Bootstrap_C()
        {
            XRNode = Instantiate(xrNodePrefab); // Spawning XRNode triggers permission request

            if (!Application.isEditor) 
            {
                // This app requires some permissions to run. The user must accept these permissions before the app can continue.
                // --- poll until user has answered every request -----------
                VisionOSAuthorizationStatus worldStatus;
                VisionOSAuthorizationStatus handStatus;

                do
                {
                    worldStatus = VisionOS.QueryAuthorizationStatus(VisionOSAuthorizationType.WorldSensing);  // plane / mesh
                    handStatus  = VisionOS.QueryAuthorizationStatus(VisionOSAuthorizationType.HandTracking);  // XRHands

                    yield return null;              // wait one frame
                }
                while (worldStatus == VisionOSAuthorizationStatus.NotDetermined || handStatus  == VisionOSAuthorizationStatus.NotDetermined);

                // --- act on the result ------------------------------------
                if (worldStatus == VisionOSAuthorizationStatus.Denied || handStatus  == VisionOSAuthorizationStatus.Denied)
                {
                    // If we've gotten here, it means that the user has denied the permissions. We now need to instruct them 
                    // on how to enable them in the visionOS settings.
                    Instantiate(permissionsRequiredWarningPrefab);
                    yield break;
                }
            }

            ARNavigation = Instantiate(arNavigationPrefab);
            ARSurfaceUnderstanding = Instantiate(arSurfaceUnderstandingPrefab);
        
            GenieManager = Instantiate(genieManagerPrefab);
            UIManager = Instantiate(uiManagerPrefab);
            LaunchUX = Instantiate(launchUXPrefab);
            AutoItemSpawner = Instantiate(autoItemSpawnerPrefab);
            XRImageTrackingObjectManager = Instantiate(xrImageTrackingObjectManagerPrefab);
            
            // Initialize the GeniesIrlSubManagers
            XRNode.OnSceneBootstrapped(this);
            ARNavigation.OnSceneBootstrapped(this);
            ARSurfaceUnderstanding.OnSceneBootstrapped(this);
            GenieManager.OnSceneBootstrapped(this);
            UIManager.OnSceneBootstrapped(this);
            LaunchUX.OnSceneBootstrapped(this);
            AutoItemSpawner.OnSceneBootstrapped(this);
            xrImageTrackingObjectManagerPrefab.OnSceneBootstrapped(this);
        }
    }
}