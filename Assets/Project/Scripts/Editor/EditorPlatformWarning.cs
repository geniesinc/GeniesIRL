using UnityEngine;
using UnityEditor;

namespace GeniesIRL 
{
    /// <summary>
    /// GeniesIRL doesn't work properly in the Editor if not on visionOS mode. This will pop up with a warnning if not in that mode,
    /// and if not, it will provide a button to do so.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlatformWarning
    {
        // EditorPref keys
        private const string IgnoreKey = "VisionOSCheckIgnore";

        static EditorPlatformWarning()
        {
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            CheckBuildTarget();
        }

        private static void CheckBuildTarget()
        {
            // Only makes sense in Editor
            if (!Application.isEditor)
                return;

            // If we're already on VisionOS, do nothing
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.VisionOS)
                return;

            // Check if user has previously ignored this warning.
            bool hasIgnored = EditorPrefs.GetBool(IgnoreKey, false);

            if (hasIgnored)
                return;

            // Show the dialog
            int result = EditorUtility.DisplayDialogComplex(
                "Platform Not VisionOS",
                "This project will only work properly in the Editor if it is set to VisionOS in the build platform settings.",
                "Switch to VisionOS",  // index 0
                "Ignore",             // index 1
                ""                    // index 2 (left blank to avoid a third button label)
            );

            // Handle dialog result
            switch (result)
            {
                case 0: // "Switch to VisionOS"
                    // Switch the build target automatically
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.VisionOS, BuildTarget.VisionOS);
                    break;
                case 1: // "Ignore"
                case 2: // If user closed or pressed a hidden 3rd option
                    // Remember that we’re ignoring this platform
                    EditorPrefs.SetBool(IgnoreKey, true);
                    break;
            }
        }

        private static void OnEditorQuitting()
        {
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.update -= OnEditorUpdate;
            EditorPrefs.SetBool(IgnoreKey, false); // Reset ignore so it can happen the next time we launch.
        }
    }
}

