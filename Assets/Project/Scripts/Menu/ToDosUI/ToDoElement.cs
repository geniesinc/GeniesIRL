using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace GeniesIRL
{
    public enum ToDoButtonStyle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public class ToDoElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private GameObject _checkedBox;
        [SerializeField] private GameObject _uncheckedBox;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _inProgressOutline;
        [SerializeField] private SpatialButton _spatialButton;
        [SerializeField] private GameObject[] _stylizedMeshOptions;

        private int _animKeyIsBlinking;
        private BrainInspector.BrainTaskStatus _brainTaskStatus;


        public void SetButtonStyle(ToDoButtonStyle style)
        {
            for (int i = 0; i < _stylizedMeshOptions.Length; i++)
            {
                _stylizedMeshOptions[i].SetActive(i == (int)style);
            }
        }

        public SpatialButton Initialize(BrainInspector.BrainTaskStatus brainTaskStatus)
        {
            _brainTaskStatus = brainTaskStatus;
            _label.text = brainTaskStatus.taskName;
            brainTaskStatus.onTaskAccomplished += OnBrainTaskAccomplished;

            SetChecked(brainTaskStatus.IsAccomplished);

            _animKeyIsBlinking = Animator.StringToHash("IsBlinking");

            // Set the initial state of the UI elements
            _inProgressOutline.SetActive(false);
            _animator.SetBool(_animKeyIsBlinking, false);

            // For the intializer to subscribe to
            return _spatialButton;
        }

        private void OnDestroy()
        {
            if (_brainTaskStatus != null)
            {
                _brainTaskStatus.onTaskAccomplished -= OnBrainTaskAccomplished;
            }
        }

        private void Update()
        {
            if (_brainTaskStatus == null)
            {
                return;
            }

            // Idk how this is ever null bc it's a serialized field,
            // but here we are!
            if (_inProgressOutline != null)
            {
                _inProgressOutline.SetActive(_brainTaskStatus.IsInProgress);
            }

            // Support old menu while transitioning...
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.SetBool(_animKeyIsBlinking, _brainTaskStatus.IsInProgress);
            }
        }

        private void OnBrainTaskAccomplished(BrainInspector.BrainTaskStatus brainTaskStatus)
        {
            SetChecked(true);
        }

        private void SetChecked(bool isChecked)
        {
            _checkedBox.SetActive(isChecked);
            _uncheckedBox.SetActive(!isChecked);
        }   
    }
}
