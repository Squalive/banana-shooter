
using System.Collections;
using UnityEngine;

namespace Menu
{
    public class TransitionUI : MonoBehaviour
    {
        public static TransitionUI Instance { private set; get; }
        
        [SerializeField] private CanvasGroup canvas;

        private float _desiredAlpha = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        // private IEnumerator Start()
        // {
        //     canvas.alpha = 1f;
        //     yield return new WaitForSeconds(0.5f);
        //     ClearTransition();
        // }

        public void ClearTransition()
        {
            _desiredAlpha = 0f;
            canvas.blocksRaycasts = false;
        }
        
        public void StartTransition()
        {
            _desiredAlpha = 1f;
            canvas.blocksRaycasts = true;
        }

        private void Update()
        {
            canvas.alpha = Mathf.Lerp(canvas.alpha, _desiredAlpha, Time.deltaTime * 10f);
        }
    }
}
