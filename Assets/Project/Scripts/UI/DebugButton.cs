using GeniesIRL;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public enum DebugButtonStyle
{
    None,
    Top,
    Bottom
}

public class DebugButton : MonoBehaviour
{
    [SerializeField] private GameObject _onStateIndicator;
    [SerializeField] private SpatialButton _spatialButton;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private GameObject[] _stylizedMeshOptions;

    private bool _isOnState = false;
    private string _defaultStateText;
    private UnityAction<bool> _assignedAction;

    public void Initialize(bool defaultState, string defaultStateText, UnityAction<bool> onClickAction)
    {
        _defaultStateText = defaultStateText;
        _label.text = _defaultStateText;
        _isOnState = defaultState;
        _onStateIndicator.SetActive(defaultState);
        _assignedAction = onClickAction;
        _spatialButton.OnPressButton.AddListener(OnButtonPressed);
    }

    public void SetButtonStyle(DebugButtonStyle style)
    {
        for(int i = 0; i < _stylizedMeshOptions.Length; i++)
        {
            _stylizedMeshOptions[i].SetActive(i == (int)style);
        }
    }

    public void OnButtonPressed()
    {
        Debug.Log($"Button pressed: {_label.text}");
        _isOnState = !_isOnState;
        _onStateIndicator.SetActive(_isOnState);
        _assignedAction?.Invoke(_isOnState);
    }

}
