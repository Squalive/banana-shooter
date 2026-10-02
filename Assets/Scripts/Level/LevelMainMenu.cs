using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level
{
    public class LevelMainMenu : MonoBehaviour
    {

        public RawImage[] images;

        private IEnumerator Start()
        {
            LevelSystem levelSystem = LevelManager.Instance.levelSystem;
            SetLevelNumber(levelSystem.GetLevel());

            yield return new WaitForSeconds(1f);

            LevelManager.Instance.StoreExp();
        }
        
        public TextMeshProUGUI levelText;
    
        private void SetLevelNumber(int levelNumber)
        {
            levelText.text = levelNumber.ToString();

            Color color = LevelManager.Instance.GetColor(levelNumber);

            foreach (var image in images)
            {
                image.color = color;
            }

            levelText.color = color;
        }
    }
}
