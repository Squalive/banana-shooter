using System;
using System.Collections;
using Audio;
using Extensions;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Level
{
    public class LevelMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform obj,levelObj;

        private Vector2 desiredPos;

        public RawImage[] images;

        public AudioSource source;
        
        public TextMeshProUGUI levelText,experienceText;
        private LevelSystemAnimated levelSystemAnimated;

        private int _maxValue = 0,_minValue=0;

        [SerializeField] private Image defaultFill, addFill;

        private Vector2 levelDefaultPos;
        private bool _shaking = false;

        private Vector3 levelDesiredScale = Vector3.one;
        private void Start()
        {
            levelDefaultPos = levelObj.anchoredPosition;
            UnDisplay();
            if (LevelManager.Initialized)
            {
                Init();
                return;
            }
            LevelManager.Instance.OnInitialize += Init;
        }


        private void OnDisable()
        {
            LevelManager.Instance.OnInitialize -= Init;
            
        }

        void Init()
        {
            SetLevelSystemAnimated(LevelManager.Instance.levelSystemAnimated);
        }

        public void Display()
        {
            desiredPos=Vector2.zero;
        }

        public void UnDisplay()
        {
            desiredPos = new Vector2(0, -300);
        }

        public void AddVisualXp()
        {
            LevelManager.Instance.levelSystem.AddExperience(25000);
        }

        private void Update()
        {
            levelSystemAnimated?.Update();

            obj.anchoredPosition = Vector2.Lerp(obj.anchoredPosition, desiredPos, Time.deltaTime * 7f);

            levelObj.localScale = Vector3.Slerp(levelObj.localScale, levelDesiredScale, Time.deltaTime * 10f);

            if (_shaking)
            {
                float offset = 2f;
                levelObj.anchoredPosition = levelDefaultPos +
                                            new Vector2(Random.Range(-offset, offset), Random.Range(-offset, offset));
            }
        }

        void StopShaking()
        {
            levelObj.anchoredPosition = levelDefaultPos;
            _shaking = false;
        }

        private void SetExperienceBarSize(int exp,int expToNext)
        {
            addFill.fillAmount= (float)(exp - _minValue) / (_maxValue - _minValue);
            experienceText.SetText($"{exp} / {expToNext}xp");
        }
    
        private void SetLevelNumber(int levelNumber,int lastExp,int expToNext)
        {
            _minValue = lastExp;
            _maxValue = expToNext;
            levelText.text = levelNumber.ToString();
            defaultFill.fillAmount= 0;

            Color color = LevelManager.Instance.GetColor(levelNumber);

            foreach (var image in images)
            {
                image.color = color;
            }

            levelText.color = color;
            defaultFill.color = color;
            addFill.color = color;
        }

        void SetLevelSystemAnimated(LevelSystemAnimated levelSystemAnimated) {
            // Set the LevelSystemAnimated object
            this.levelSystemAnimated = levelSystemAnimated;
            this.levelSystemAnimated.source = source;
        
            // Update the starting values
            SetLevelNumber(levelSystemAnimated.GetLevel(),levelSystemAnimated.GetMinExp(),levelSystemAnimated.GetExpToNext());
            SetExperienceBarSize(levelSystemAnimated.GetExp(),levelSystemAnimated.GetExpToNext());

            defaultFill.fillAmount = (float)(levelSystemAnimated.GetExp() - _minValue) / (_maxValue - _minValue);
            experienceText.SetText($"{levelSystemAnimated.GetExp()} / {levelSystemAnimated.GetExpToNext()}xp");

            // Surbscribe to the changed events
            levelSystemAnimated.OnExpChanged += LevelSystemAnimated_OnExperienceChanged;
            levelSystemAnimated.OnLevelChanged += LevelSystemAnimated_OnLevelChanged;
            levelSystemAnimated.OnAnimateStart += OnAnimateStart;
            levelSystemAnimated.OnAnimateStop += OnAnimateStop;
            
        }

        private void OnAnimateStop(object sender, EventArgs e)
        {
            StopShaking();
            StopAllCoroutines();
            StartCoroutine(AnimateStopAnimation((LevelSystemAnimated)sender));
        }

        IEnumerator AnimateStopAnimation(LevelSystemAnimated levelSystemAnimated)
        {
            var color = LevelManager.Instance.GetColor(levelSystemAnimated.GetLevel());
            var c = color * 0.55f;
            var unColor=new Color(c.r,c.g,c.b,1f);
            var desiredColor = color;
            defaultFill.fillAmount = addFill.fillAmount;

            float speed = 3f;
            while ((defaultFill.color - desiredColor).Magnitude() > 0.03f)
            {
                defaultFill.color = Color.Lerp(defaultFill.color, desiredColor,Time.deltaTime*speed);
                yield return null;
            }

            desiredColor = unColor;
            while ((defaultFill.color - desiredColor).Magnitude() > 0.03f)
            {
                defaultFill.color = Color.Lerp(defaultFill.color, desiredColor,Time.deltaTime*speed);
                yield return null;
            }
            
            desiredColor = color;
            while ((defaultFill.color - desiredColor).Magnitude() > 0.03f)
            {
                defaultFill.color = Color.Lerp(defaultFill.color, desiredColor,Time.deltaTime*speed);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            UnDisplay();
        }

        private void OnAnimateStart(object sender, EventArgs e)
        {
            Color color = LevelManager.Instance.GetColor(((LevelSystemAnimated) sender).GetLevel()) * 0.55f;
            defaultFill.color = new Color(color.r,color.g,color.b,1f);
            
            _shaking = true;
            Display();
            StopAllCoroutines();
        }

        private void LevelSystemAnimated_OnLevelChanged(object sender, EventArgs e) {
            // Level changed, update text
            SetLevelNumber(levelSystemAnimated.GetLevel(),levelSystemAnimated.GetMinExp(),levelSystemAnimated.GetExpToNext());
        
            AudioManager.Instance.Play("Reward");

            levelDesiredScale = Vector3.one*1.25f;
            
            Invoke(nameof(ResetLevelScale),0.7f);

            if (levelSystemAnimated.GetLevel() % 5 == 0)
                InventoryManager.Instance.GetLevelUpReward();
        }

        void ResetLevelScale()
        {
            levelDesiredScale=Vector3.one;
        }

        private void LevelSystemAnimated_OnExperienceChanged(object sender, EventArgs e) {
            // Experience changed, update bar size
            SetExperienceBarSize(levelSystemAnimated.GetExp(),levelSystemAnimated.GetExpToNext());
        }
    }
}