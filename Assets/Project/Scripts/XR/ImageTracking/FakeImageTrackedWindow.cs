using UnityEngine;

namespace GeniesIRL
{
    /// <summary>
    /// Spawns when the XRImageTrackingObjectManager sees a "window" image. We use this to tell the game that there's a window
    /// here, in case the AR Plane system is unable to detect the window itself.
    /// </summary>
    public class FakeImageTrackedWindow : MonoBehaviour
    {
        [SerializeField]
        private Window windowPrefab;

        [SerializeField]
        private Transform innerAnchor;

        [SerializeField]
        private Vector3 windowOffset;

        [SerializeField]
        private Vector2 windowSize;

        private Window _window;

        private void Start()
        {
            _window = Instantiate(windowPrefab);
            _window.transform.SetParent(innerAnchor);
            _window.transform.localPosition = windowOffset;
            _window.transform.localRotation = Quaternion.identity;
            _window.transform.localScale = new Vector3(windowSize.x, windowSize.y, _window.transform.localScale.z);
            PerfectWindowOrientation();
        }

        private void LateUpdate()
        {
            _window.transform.localPosition = windowOffset;
            _window.transform.localScale = new Vector3(windowSize.x, windowSize.y, _window.transform.localScale.z);
            PerfectWindowOrientation();
        }

        private void PerfectWindowOrientation()
        {
            // Take the current world rotation of the window and tweak it so it is vertically aligned.
            Vector3 eulerAngles = _window.transform.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            _window.transform.eulerAngles = eulerAngles;
        }
    }
}

