using UnityEngine;

namespace Extensions
{
    [ExecuteInEditMode]
    public class SkinnedmeshBonesTransfer : MonoBehaviour
    {
        public SkinnedMeshRenderer sourceMeshRenderer;
        public SkinnedMeshRenderer targetMeshRenderer;
        public Transform[] bones;

        public bool getBones;
        public bool setBones;

        private void Update()
        {
            if(getBones)
            {
                getBones = false;

                bones = sourceMeshRenderer.bones;
            }
            if (setBones)
            {
                setBones = false;

                targetMeshRenderer.bones = bones;
            }
        }
    }
}