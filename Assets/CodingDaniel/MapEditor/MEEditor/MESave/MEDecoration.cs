using UnityEngine;

namespace CodingDaniel.MapEditor.MEEditor.MESave
{
    
    public class MEDecoration : MonoBehaviour
    {
        public enum EDecorationType
        {
            None=0,
            Obstacle,
            Water,
            GrapplePoint,
            DeathZone,
        }

        public EDecorationType type = EDecorationType.Obstacle;
        public bool enableCollision=true;
        
    }
}