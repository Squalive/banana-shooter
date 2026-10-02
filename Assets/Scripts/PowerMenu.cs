
using System.Collections;

using Manager;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class PowerMenu : MonoBehaviour
{
    public static PowerMenu Instance;

    private void Awake()
    {
        Instance = this;
    }

    public GameObject powerScroll;

    public LocalizeStringEvent text;
    public RawImage image;

    private void Start()
    {
        GameManager.PowerDetail detail;
        detail = GameManager.Instance.GetPowerDetail(GameManager.Instance.power);

        if (detail != null)
        {
            text.SetEntry(detail.key);
            text.RefreshString();
            image.texture = detail.texture2D;
        }
    }

    
    public void SetPower(int index)
    {
        GameManager.Instance.power = (GameManager.PowerType)index;

        GameManager.PowerDetail detail = GameManager.Instance.GetPowerDetail(GameManager.Instance.power);

        if (detail != null)
        {
            text.SetEntry(detail.key);
            text.RefreshString();
            image.texture = detail.texture2D;

            if (PowerInGameMenu.Instance)
                PowerInGameMenu.Instance.texture.texture = detail.texture2D;
        }
        
        powerScroll.SetActive(false);

        SaveSystem.SaveData("power", GameManager.Instance.power);
    }
}
