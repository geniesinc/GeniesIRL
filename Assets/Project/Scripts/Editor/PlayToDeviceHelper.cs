using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using Unity.PolySpatial;
using UnityEditor.XR.Management.Metadata;

namespace GeniesIRL.Editor
{
    /// <summary>
    /// This tool attempts to auto-update the XR Plug-In Management settings to match the "Enable" setting in the
    /// Polyspatial Play to Device window. The idea is to have PolySpatial activate in the Standalone tab to enable
    /// connection with the AVP for rapid iteration.
    /// update as expected.
    /// </summary>
    [InitializeOnLoad]
    public class PlayToDeviceHelper
    {
        // Static constructor is called when Unity loads
        static PlayToDeviceHelper()
        {
            // Subscribe to the update callback
            EditorApplication.update += OnEditorUpdate;

            previousPlayToDeviceState = EditorPrefs.GetBool(kPlayToDevicePrefKey, false);
        }

        private static bool previousPlayToDeviceState = false;

        private const string kPlayToDevicePrefKey = "previousPlayToDeviceState";

        private static void OnEditorUpdate()
        {
            // Replace this with the actual method to check if Play to Device is enabled
            bool isPlayToDeviceEnabled = CheckPlayToDeviceState();

            previousPlayToDeviceState = EditorPrefs.GetBool(kPlayToDevicePrefKey, false);

            if (isPlayToDeviceEnabled != previousPlayToDeviceState)
            {
                previousPlayToDeviceState = isPlayToDeviceEnabled;

                EditorPrefs.SetBool(kPlayToDevicePrefKey, isPlayToDeviceEnabled);

                SetPolySpatialState(isPlayToDeviceEnabled);
            }
        }

        private static bool CheckPlayToDeviceState()
        {
            return PolySpatialUserSettings.Instance.ConnectToPlayToDevice;
        }

        private static void SetPolySpatialState(bool enable)
        {
            // Get the Build Target Group for PC/Mac/Linux
            BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone;

            string loaderName = "Unity.PolySpatial.XR.Internals.PolySpatialXRLoader";

            var xrGeneralSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            var pluginsSettings = xrGeneralSettings.AssignedSettings;

            if (enable)
            {

                var success = XRPackageMetadataStore.AssignLoader(pluginsSettings, loaderName, buildTargetGroup);
                if (success)
                {
                    Debug.Log("PolySpatial XR Enabled for Standalone");
                    EditorUtility.DisplayDialog("PolySpatial XR Enabled", "To support Play-To-Device, we'll now enable the PolySpatial XR Plug-In in the Standalone tab " +
                        " of the XR Plug-In Management panel in ProjectSettings.", "Ok");
                }
            }
            else
            {
                var success = XRPackageMetadataStore.RemoveLoader(pluginsSettings, loaderName, buildTargetGroup);
                if (success)
                {
                    Debug.Log("PolySpatial XR Disabled for Standalone");
                    EditorUtility.DisplayDialog("PolySpatial XR Disabled", "To support XR Simulation Environments, we'll now disable " +
                        "the PolySpatial XR Plug-In in the Standalone tab of the XR Plug-In Managment panel in ProjectSettings.", "Ok");
                }

            }

            EditorUtility.SetDirty(pluginsSettings);
            EditorUtility.SetDirty(xrGeneralSettings);
            SettingsService.NotifySettingsProviderChanged();
            AssetDatabase.SaveAssets();
        }
    }

}