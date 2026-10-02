using UnityEngine;

namespace CodingDaniel.MapEditor.MEEditor
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        public void Init(Mesh mesh,Material material)
        {
            transform.localScale = new Vector3(1, 1.5f, 1);
            
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.material = material;
        }
    }
}
