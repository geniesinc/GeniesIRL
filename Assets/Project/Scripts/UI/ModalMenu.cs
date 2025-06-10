using GeniesIRL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModalMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private RawImage _screencapImage;
    [SerializeField] private TextMeshProUGUI _bodyText;
    [SerializeField] private SpatialButton _closeButton;
    
    private void Awake()
    {
        _closeButton.OnPressButton.AddListener(() => gameObject.SetActive(false));
    }

    public void Initialize(string title, Texture screencap, string bodyText)
    {
        _title.text = title;
        _screencapImage.texture = screencap;
        _bodyText.text = bodyText;
    }

    private void OnDestroy()
    {
        _closeButton.OnPressButton.RemoveAllListeners();
    }

}
