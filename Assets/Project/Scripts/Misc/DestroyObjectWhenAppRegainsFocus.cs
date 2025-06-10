using Unity.PolySpatial;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// When the app loses and gains focus, world-space objects can get messed up. For the most part, this isn't an issue because most objects in the app are ephemeral
    /// or otherwise self-managing. 
    /// </summary>
    public class DestroyObjectWhenAppRegainsFocus : MonoBehaviour
    {
        private void Awake() 
        {
            VolumeCamera volume;

            if (GeniesIrlBootstrapper.Instance != null && GeniesIrlBootstrapper.Instance.XRNode != null) 
            {
                volume = GeniesIrlBootstrapper.Instance.XRNode.volumeCamera;
            }
            else 
            {
                volume = FindFirstObjectByType<VolumeCamera>();
            }

            volume.WindowStateChanged.AddListener(OnWindowEvent);
        }

        private void OnWindowEvent(VolumeCamera volumeCamera, VolumeCamera.WindowState s)
        {
            if (s.WindowEvent == VolumeCamera.WindowEvent.Focused && s.IsFocused)
            {
                Destroy(this.gameObject);
            }
        }
    }
}

