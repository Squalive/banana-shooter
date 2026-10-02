
using Audio;

using Menu;
using UnityEngine;

public class WeaponBuyStation : MonoBehaviour
{
    private Animator _anim;
    private static readonly int Open1 = Animator.StringToHash("open");

    private void Start()
    {
        _anim = GetComponent<Animator>();
    }

    
    public void Open()
    {
        _anim.SetBool(Open1,true);
        AudioManager.Instance.Play("weapon_buy_station_open");
        
    }

    
    public void Close()
    {
        _anim.SetBool(Open1,false);
        AudioManager.Instance.Stop("weapon_buy_station_open");
    }

    
    public void StartBuy()
    {
        BuyWeaponMenu.Instance.OpenMenu();
        AudioManager.Instance.Play("weapon_buy_station_click");
    }
}
