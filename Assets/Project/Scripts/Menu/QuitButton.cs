using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeniesIRL
{
    /// <summary>
    ///     A simple helper that turns a SpatialButton into a hidden "Quit" shortcut.
    ///     - Press <see cref="pressesRequired"/> times within <see cref="resetTime"/> seconds to Quit the app.
    ///     - The label shows how many presses remain; if the timer expires, the button resets to its default state.
    ///     NOTE: iOS/visionOS does **not** provide an official way to relaunch the application from user‑space. The best we
    ///     can do is terminate the process (exit(0)) and rely on the OS to present the launcher again. In the Editor and on
    ///     other platforms we fall back to reloading the bootstrap scene (build index 0).
    /// </summary>
    public class QuitButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private SpatialButton spatialButton;
        [SerializeField] private TextMeshProUGUI label;

        [Header("Restart Logic")]
        [Tooltip("Number of presses required before the restart triggers.")]
        [SerializeField] private int pressesRequired = 5;

        [Tooltip("Seconds the user has to complete the sequence. Each press resets this timer.")]
        [SerializeField] private float resetTime = 4f;

        private string _defaultStateText;
        private int    _pressCount;
        private float  _timer;
        private bool   _armed; // True once the first press has happened

#if UNITY_IOS && !UNITY_EDITOR
        // iOS/visionOS gives us standard C exit(); this will terminate the process.
        [DllImport("__Internal")] private static extern void exit(int status);
#endif

        private void Awake()
        {
            if (spatialButton == null)
            {
                Debug.LogError("[RestartAppButton] SpatialButton reference missing.");
                enabled = false;
                return;
            }

            if (label == null)
            {
                Debug.LogError("[RestartAppButton] Label reference missing.");
                enabled = false;
                return;
            }

            _defaultStateText = label.text;
            spatialButton.OnPressButton.AddListener(OnButtonPressed);
            ResetState();
        }

        private void Update()
        {
            if (!_armed) return;

            _timer -= Time.unscaledDeltaTime; // independent of timeScale so pausing won’t break it
            if (_timer <= 0f)
            {
                ResetState();
            }
        }

        private void OnButtonPressed()
        {
            _pressCount++;

            if (_pressCount >= pressesRequired)
            {
                RestartApplication();
                return; // The process may quit immediately afterwards on iOS
            }

            _armed  = true;
            _timer  = resetTime; // Reset the countdown
            var remaining = pressesRequired - _pressCount;
            label.text   = $"Press {remaining} more time{(remaining == 1 ? string.Empty : "s")}";
        }

        private void ResetState()
        {
            _armed      = false;
            _pressCount = 0;
            _timer      = 0f;
            label.text  = _defaultStateText;
        }

        private void RestartApplication()
        {
            Debug.Log("[RestartAppButton] Restarting application...");

            if (Application.isEditor) return;

            Application.Quit();
        }
    }
}
