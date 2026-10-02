using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menu
{
    public class MaskedObject : UIBehaviour
    {
        private CanvasRenderer[] canvasRenderersToClip = null;
 
        private Canvas rootCanvas = null;
        [SerializeField]
        private RectTransform maskRectTransform = null;
        private bool initialized = false;
        
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if( initialized )
            {
                SetTargetClippingRect();
            }
        }

        private void Update()
        {
            if (initialized)
            {
                SetTargetClippingRect();
            }
        }

        public void Initialize(Canvas rootCanvas, RectTransform maskRectTransform)
        {
            this.rootCanvas = rootCanvas;
            this.maskRectTransform = maskRectTransform;
        }

        protected override void Awake()
        {
            base.Awake();
            if (rootCanvas == null)
            {
                rootCanvas = transform.root.GetComponent<Canvas>();
            }
            StartCoroutine(Init());
        }

        IEnumerator Init()
        {
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
            canvasRenderersToClip = gameObject.GetComponentsInChildren<CanvasRenderer>(true);

            SetTargetClippingRect();
            initialized = true;
        }
 
        private void SetTargetClippingRect()
        {
            Rect rect = maskRectTransform.rect;
            // Get local position of maskRect as if it was direct child of root canvas, then offset mask rect by that amount
            rect.center += (Vector2)rootCanvas.transform.InverseTransformPoint( maskRectTransform.position );
            foreach (var canvasRendererToClip in canvasRenderersToClip)
            {
                canvasRendererToClip.EnableRectClipping( rect );
            }
        }
    }
}
