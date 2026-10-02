
using Cosmetic;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class ReceiveBoxItem : MonoBehaviour
{
    public LocalizeStringEvent nameText;

    public RawImage image;

    [SerializeField] private Transform t;

    public string playerName, itemName;
    public void SetValue(string name, int itemdefid)
    {
        CosmeticItem item = CosmeticManager.ItemIdToItem[itemdefid];

        playerName = name;
        itemName = item.displayName;

        // nameText.StringReference.Arguments = new List<object>() { name,item.name };
        // nameText.RefreshString();
        
        nameText.RefreshString();
        

        image.texture = item.icon;
    }

    private void Update()
    {
        t.localPosition = Vector3.Lerp(t.localPosition,Vector3.zero, Time.deltaTime*15f);
    }
}
