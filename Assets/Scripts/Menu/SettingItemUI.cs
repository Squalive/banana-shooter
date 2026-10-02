
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingItemUI : MonoBehaviour,IPointerEnterHandler
{
    [HideInInspector] public string key;
    [SerializeField] private GameObject targetInteractor;

    private Toggle _toggle;
    private Button _button;

    private int _idx = -1;
    
    public void Init()
    {
        key = name.ToLower();
        if (!targetInteractor) return;
        _toggle = targetInteractor.GetComponent<Toggle>();
        _button = targetInteractor.GetComponent<Button>();

        if (_toggle) _idx = 0;
        else if (_button) _idx = 1;
        
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        switch (_idx)
        {
            case 0 :
                _toggle.onValueChanged.Invoke(!_toggle.isOn);
                break;
                
            case 1:
                _button.onClick.Invoke();
                break;
            default:
                break;
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.Play("select_button");
    }
}
