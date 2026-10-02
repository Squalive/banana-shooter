using System;
using System.Collections.Generic;
using Mode;
using Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class GunGameWeapon : MonoBehaviour
{
    public static GunGameWeapon Instance;
    public Transform[] weaponTrans;

    private Queue<GunGameWeaponItemUI> qWeapon = new Queue<GunGameWeaponItemUI>();

    public RawImage[] weaponImages;

    public bool start=false, next=false;

    public float offset = 280;

    public GameObject mask;

    public GameObject firstArrow;

    [SerializeField]private CanvasGroup canvas;
    [Serializable]
    class GunGameWeaponItemUI
    {
        public Transform item;
        public RawImage image;

        public GunGameWeaponItemUI(Transform item, RawImage image)
        {
            this.item = item;
            this.image = image;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private float desiredAlpha = 0;

    private void Start()
    {
        if (NetworkManager.ClientGameMode == GameMode.GunGame)
        {
            mask.SetActive(true);
            for (int i = 0; i < weaponTrans.Length; i++)
            {
                qWeapon.Enqueue(new GunGameWeaponItemUI(weaponTrans[i],weaponImages[i]));
            }
        }
        else
        {
            enabled = false;
        }
    }

    void Show()
    {
        desiredAlpha = 1f;
        Invoke(nameof(Disable),5f);
    }

    void Disable()
    {
        desiredAlpha = 0f;
    }

    private short[] weaponIndexs = GunGame.WeaponIds;

    public void Open(int weaponLevel)
    {
        canvas.alpha = 0f;
        desiredAlpha = 0;
        CancelInvoke(nameof(Show));
        Invoke(nameof(Show),GunGame.Instance.started ? 0.8f : 8.5f);

        SetTexture(weaponLevel);
    }

    public void UpdateWeapon(int weaponLevel)
    {
        if (!start)
        {
            start = true;
            firstArrow.SetActive(true);
        }
        else
        {
            next = true;
        }
        Show();
        SetTexture(weaponLevel);
    }
    void SetTexture(int weaponLevel)
    {
        int of = start ? -1 : 0;
        int i = next ? weaponLevel-1 : weaponLevel;
        foreach (var weaponItemUI in qWeapon)
        {
            weaponItemUI.image.texture = NetworkManager.Instance.weaponInfo[weaponIndexs[of + i]].texture;
            i++;
        }
    }

    private void Update()
    {
        canvas.alpha = Mathf.Lerp(canvas.alpha, desiredAlpha, Time.deltaTime * 15f);
        int target = 1;
        float x = 0;
        if (!start)
        {
            x = 0;
            target = 0;
        }
        else 
        {
            x = -offset;
            if (next)
            {
                x = -offset*2;
                if (Math.Abs(qWeapon.Peek().item.localPosition.x - x) < 1f)
                {
                    GunGameWeaponItemUI t = qWeapon.Dequeue();
                    qWeapon.Enqueue(t);
                    t.item.localPosition = new Vector3(2 * offset, 0, 0);
                    next = false;
                    x = -offset;
                }
            }
        }


        int index = 0;
        foreach (var t in qWeapon)
        {
            Vector3 desiredPos = new Vector3(x, 0, 0);
            t.item.localPosition = Vector3.Lerp(t.item.localPosition,desiredPos,Time.deltaTime*15f);
            x += offset;
            Color color = Color.white;
            color.a = index == target ? 1 : 80 / 255f;
            t.image.color = color;
            index++;
        }
    }
}
