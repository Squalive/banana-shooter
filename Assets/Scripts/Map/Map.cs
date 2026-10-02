using UnityEngine;

namespace Map
{
    public enum MapExternal
    {
        None,
        Rainy,
        Snowy,
    }
    
    [CreateAssetMenu(menuName = "Banana Shooter/Map",fileName = "New Map")]
    public class Map : ScriptableObject
    {
        public Texture2D texture;

        public MapExternal external=MapExternal.None;
    }
}