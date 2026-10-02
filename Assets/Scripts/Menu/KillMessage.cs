using Manager;
using Multiplayer;
using UnityEngine;

namespace Menu
{
    public class KillMessage : MonoBehaviour
    {
        public static KillMessage Instance;

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] Transform content;
        public void AddKillMessage(string killer, string killed,ushort how,bool hitHead,bool wall,bool noscope)
        {
            if (content.childCount > 4)
            {
                for (int i = 0; i < content.childCount-2; i++)
                {
                    Destroy(content.GetChild(i).gameObject);
                }
            }
            KillMessageItem item = Instantiate(PrefabManager.Instance.GetPrefab("KillMessage"), content).GetComponent<KillMessageItem>();

            Texture2D texture2D = how>=NetworkManager.Instance.weaponInfo.Count ? NetworkManager.Instance.weaponInfo[0].texture : NetworkManager.Instance.weaponInfo[how].texture;
            if (how == 1003)
                texture2D = PrefabManager.Instance.throwObjTexture[3];
            else if (how == 1000)
                texture2D = PrefabManager.Instance.throwObjTexture[1];
            else if(how == 1002)
                texture2D = PrefabManager.Instance.throwObjTexture[0];
            else if(how == 1004)
                texture2D = PrefabManager.Instance.throwObjTexture[4];
            item.how.texture = texture2D;
            RectTransform rectTransform = item.how.GetComponent<RectTransform>();

            rectTransform.sizeDelta = new Vector2(rectTransform.rect.height * texture2D.width / texture2D.height,
                rectTransform.rect.height);
            item.killer.SetText(killer);
            item.killed.SetText(killed);
            item.hitHead.SetActive(hitHead);
            item.wall.SetActive(wall);
            item.noscope.SetActive(noscope);

            if (item.how.texture == null)
            {
                item.how.texture = NetworkManager.Instance.GetWeaponTexture("knife");
            }
        }
    }
}
