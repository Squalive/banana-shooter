using Audio;

using Manager;
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weapon;

namespace Menu
{
    public class ThrowObjectMenu : MonoBehaviour
    {
    
        public enum ThrowObjectType
        {
            Grenade = 0,
            Knife,
            FlashBang,
            MolotovCocktail,
            JumpPad,
        }

        private int propIndex=0;

        public GameObject selectMenu;

        public Button tacticalBtn;
        public TextMeshProUGUI tacticalText;
        public RawImage tacticalImage;
    
        private void Start()
        {
            
            tacticalBtn.onClick.AddListener(delegate { SetPropIndex(0); });
        
            tacticalText.SetText(GameManager.Instance.tacticalProp.ToString());
            tacticalImage.texture = PrefabManager.Instance.throwObjTexture[(int) GameManager.Instance.tacticalProp];
        
            selectMenu.SetActive(false);
        }

        void SetPropIndex(int i)
        {
            propIndex = i;
            selectMenu.SetActive(true);
        }

        
        public void SelectProp(int type)
        {
            selectMenu.SetActive(false);
            ThrowObjectType objectType = (ThrowObjectType) type;
            switch (propIndex)
            {
                case 0:
                    GameManager.Instance.tacticalProp = objectType;
                    GameManager.throwableChanged = true;
                    break;
            }
            
            tacticalText.SetText(GameManager.Instance.tacticalProp.ToString());
        
            tacticalImage.texture = PrefabManager.Instance.throwObjTexture[(int) GameManager.Instance.tacticalProp];
        
            AudioManager.Instance.PlayButton();

            if (GameUIManager.Instance && WeaponManager.Instance)
            {
                GameUIManager.Instance.throwObjCount.SetText(NetworkManager.ClientGameMode == GameMode.SpecialGameMode
                    ? "∞"
                    : WeaponManager.Instance.throwableManager.TacticalThrowCount.ToString());
                GameUIManager.Instance.throwObjImage.texture=!InfectedHand.Instance.isInfected ? PrefabManager.Instance.throwObjTexture[(int) GameManager.Instance.tacticalProp] : PrefabManager.Instance.throwObjTexture[1] ;
            }
        }
    }
}
