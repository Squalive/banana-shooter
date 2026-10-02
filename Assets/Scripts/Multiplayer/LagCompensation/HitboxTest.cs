
using UnityEngine;

namespace Multiplayer.LagCompensation
{
    public class HitboxTest : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            
            Gizmos.DrawMesh(mesh, 0, transform.position, Quaternion.identity);
        }
    }
}