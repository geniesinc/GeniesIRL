using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.VisionOS;
using UnityEngine.SceneManagement;
using Unity.PolySpatial;

namespace GeniesIRL 
{
    /// <summary>
    /// In case the user doesn't accept the permissions, we'll show them this warning message to instruct them on how to
    /// enable them in visionOS settings.
    /// </summary>
    public class PermissionsRequiredWarning : MonoBehaviour
    {

        public SpatialButton button;

        private void Awake()
        {
            button.OnPressButton.AddListener(OnButtonPressed);

            VolumeCamera volume = FindFirstObjectByType<VolumeCamera>();
        
            volume.WindowStateChanged.AddListener(OnWindowEvent);
        }

        private void OnButtonPressed()
        {
            Application.OpenURL("app-settings:");   // Opens up the app settings page.
        }

        private void OnWindowEvent(VolumeCamera volumeCamera, VolumeCamera.WindowState s)
        {
            if (s.WindowEvent == VolumeCamera.WindowEvent.Focused && !s.IsFocused)
            {
                // Assume that if the application loses focus, the user is accessing Settings to enable permissions.
                // We want to quit here to avoid some weird graphical and audio things we saw while trying to resume the application after 
                // re-enabling permissions. Quitting allows us to start fresh.
               Application.Quit();
            }
        }
    }
}

