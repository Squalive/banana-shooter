
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StatusItem : MonoBehaviour,IPointerEnterHandler,IPointerMoveHandler,IPointerExitHandler
{
    public enum StatusType
    {
        StartGame,
        Kill,
        Win,
        Experience,
        Target_Score,
        Parkour_Time,
        Death
    }

    private void Start()
    {
        _statusMenu = StatusMenu.Instance;
    }

    private StatusMenu _statusMenu;
    public StatusType type;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_statusMenu == null)
        {
            _statusMenu = StatusMenu.Instance;
            return;
        }
        int data;
        string text;
        List<object> list;
        switch (type)
        {
            case StatusType.StartGame:
                    _statusMenu.EnableDetailed();
                    data = _statusMenu.startGame;
                    text = "start_game_detail";
                    _statusMenu.statusDetailed.SetEntry(text);
                    _statusMenu.statusDetailedTrick.SetEntry(text);
                    
                    list = new List<object>() {data};
                    
                    _statusMenu.statusDetailedTrick.StringReference.Arguments = list;
                    _statusMenu.statusDetailed.StringReference.Arguments = list;
                    
                    _statusMenu.statusDetailed.RefreshString();
                    _statusMenu.statusDetailedTrick.RefreshString();
                break;
            case StatusType.Kill:
                    _statusMenu.EnableDetailed();
                    data = _statusMenu.kills;
                    text = "kill_detail";
                    _statusMenu.statusDetailed.SetEntry(text);
                    _statusMenu.statusDetailedTrick.SetEntry(text);
                    
                    list = new List<object>() {data};
                    
                    _statusMenu.statusDetailedTrick.StringReference.Arguments = list;
                    _statusMenu.statusDetailed.StringReference.Arguments = list;
                    
                    _statusMenu.statusDetailed.RefreshString();
                    _statusMenu.statusDetailedTrick.RefreshString();
                break;
            case StatusType.Win:
                    _statusMenu.EnableDetailed();
                    data = _statusMenu.wins;
                    text = "win_detail";
                    _statusMenu.statusDetailed.SetEntry(text);
                    _statusMenu.statusDetailedTrick.SetEntry(text);
                    
                    list = new List<object>() {data};
                    
                    _statusMenu.statusDetailedTrick.StringReference.Arguments = list;
                    _statusMenu.statusDetailed.StringReference.Arguments = list;
                    
                    _statusMenu.statusDetailed.RefreshString();
                    _statusMenu.statusDetailedTrick.RefreshString();
                break;
            case StatusType.Experience:
                    _statusMenu.EnableDetailed();
                    data = _statusMenu.exp;
                    text = "exp_detail";
                    _statusMenu.statusDetailed.SetEntry(text);
                    _statusMenu.statusDetailedTrick.SetEntry(text);
                    
                    list = new List<object>() {data};
                    
                    _statusMenu.statusDetailedTrick.StringReference.Arguments = list;
                    _statusMenu.statusDetailed.StringReference.Arguments = list;
                    
                    _statusMenu.statusDetailed.RefreshString();
                    _statusMenu.statusDetailedTrick.RefreshString();
                break;
            case StatusType.Death:
                    _statusMenu.EnableDetailed();
                    data = _statusMenu.death;
                    text = "death_detail";
                    _statusMenu.statusDetailed.SetEntry(text);
                    _statusMenu.statusDetailedTrick.SetEntry(text);
                    
                    list = new List<object>() {data};
                    
                    _statusMenu.statusDetailedTrick.StringReference.Arguments = list;
                    _statusMenu.statusDetailed.StringReference.Arguments = list;
                    
                    _statusMenu.statusDetailed.RefreshString();
                    _statusMenu.statusDetailedTrick.RefreshString();
                break;
            case StatusType.Target_Score:
                    _statusMenu.EnableDetailed();
                    data = _statusMenu.targetScore;
                    text = "score_detail";
                    _statusMenu.statusDetailed.SetEntry(text);
                    _statusMenu.statusDetailedTrick.SetEntry(text);
                    
                    list = new List<object>() {data};
                    
                    _statusMenu.statusDetailedTrick.StringReference.Arguments = list;
                    _statusMenu.statusDetailed.StringReference.Arguments = list;
                    
                    _statusMenu.statusDetailed.RefreshString();
                    _statusMenu.statusDetailedTrick.RefreshString();
                break;
            case StatusType.Parkour_Time:
                
                    _statusMenu.EnableDetailed();
                    float data2 = _statusMenu.parkourTime;
                    // text = UIManager.IsItChinese() ? $"靶场跑酷时间: {data2.ToString("F1")}" : $"Shooting Range Parkour Time: {data2.ToString("F1")}";
                    text = "time_detail";
                    _statusMenu.statusDetailed.SetEntry(text);
                    _statusMenu.statusDetailedTrick.SetEntry(text);
                    
                    list = new List<object>() {data2.ToString("F1")};
                    
                    _statusMenu.statusDetailedTrick.StringReference.Arguments = list;
                    _statusMenu.statusDetailed.StringReference.Arguments = list;
                    
                    _statusMenu.statusDetailed.RefreshString();
                    _statusMenu.statusDetailedTrick.RefreshString();
                break;
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_statusMenu != null)
        {
            _statusMenu.statusDetailed.transform.position = Input.mousePosition;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_statusMenu != null)
        {
            _statusMenu.DisableDetailed();
        }
    }
}
