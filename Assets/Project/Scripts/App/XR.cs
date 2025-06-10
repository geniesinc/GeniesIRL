using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.XR.Management;

namespace GeniesIRL.App
{
    /// <summary>
    /// Static class for describing the app's current XR state.
    /// </summary>
    public class XR
    {
        // Cached result to ensure high performance during per-frame queries
        private static readonly bool isPolySpatialEnabled = CheckPolySpatialEnabled();

        /// <summary>
        /// Returns true if PolySpatial is enabled for the current active Loader. This will always return true when running
        /// on Apple Vision Pro. In the Editor, it will return true if 'PolySpatial XR' is ticked in the Standalone tab
        /// of XR Plug-In Management in Project Settings. Enabling 'PolySpatial XR' in the Standalone tab is required to use
        /// the Play To Device feature for rapid iteration. Otherwise, if it's un-ticked the app will launch with the
        /// XR Simulation environment and other modifications (such as mouse input) to simulate the XR experience in the Editor
        /// without connecting to the device.
        /// 
        /// Note: For performance, this value is set only set the first time it is called and does not change throughout
        /// the lifetime of the application. If we ever implement switching between Metal and RealityKit, this script would
        /// have to be made more robust.
        /// </summary>
        public static bool IsPolySpatialEnabled => isPolySpatialEnabled;

        private static bool CheckPolySpatialEnabled()
        {
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings == null)
                return false;

            var xrManager = xrSettings.Manager;
            if (xrManager == null)
                return false;

            // Iterate through the active XR loaders to check for PolySpatial
            foreach (var loader in xrManager.activeLoaders)
            {
                Debug.Log("Loader: " + loader.name);
                if (loader != null && (loader.name.Contains("PolySpatial", System.StringComparison.CurrentCultureIgnoreCase)
                    || loader.name.Contains("VisionOS", System.StringComparison.CurrentCultureIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if we're in the Editor, and PlayToDevice is enabled. 
        /// </summary>
        /// <returns></returns>
        private static bool IsUsingPlayToDevice()
        {
#if UNITY_EDITOR
        return PolySpatialUserSettings.Instance.ConnectToPlayToDevice;
#else
        return false;
#endif
        }
    }
}


