
using UnityEngine;
using UnityEngine.VFX;

namespace Cosmetic
{
    public class CosmeticVFX : MonoBehaviour
    {
        [SerializeField]
        private VisualEffect visualEffect;

        private MyVFXTransformBinder _myTransformBinder;

        MyVFXTransformBinder MyVFXTransformBinder
        {
            get
            {
                if(_myTransformBinder == null)
                    _myTransformBinder= visualEffect.GetComponent<MyVFXTransformBinder>();
                return _myTransformBinder;
            }
        }

        private Transform _target;

        public void SetMesh(Mesh mesh)
        {
            if(mesh==null)
                visualEffect.ResetOverride("Mesh");
            else visualEffect.SetMesh("Mesh",mesh);
        }

        public void SetSkinnedMeshRenderer(SkinnedMeshRenderer meshRenderer)
        {
            visualEffect.SetSkinnedMeshRenderer("SkinnedMeshRenderer",meshRenderer);
        }

        public void SetTransform(Transform t,bool localScale=false)
        {
            _target = t;
            MyVFXTransformBinder.Target = t;
            MyVFXTransformBinder.localScale = localScale;
        }

    }
}