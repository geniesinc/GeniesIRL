using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace GeniesIRL
{
    public class SpatialButton : MonoBehaviour
    {
        public UnityEvent OnPressButton;
        public TMP_Text Label;

        [Tooltip("Time in seconds to wait before allowing a second press. Set to -1 to disable.")]
        public float dblClickPreventionTime = -1f;

        private float _latestTimePressed = -1f;


        public void Press()
        {

            if (dblClickPreventionTime > 0 && _latestTimePressed > 0 && Time.time - _latestTimePressed < dblClickPreventionTime) return; // Ignore double presses

            _latestTimePressed = Time.time;
            OnPressButton?.Invoke();
        }
    }
}

