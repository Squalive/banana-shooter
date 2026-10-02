using UnityEngine;

namespace CodingDaniel.MapEditor.UI.AddObject
{
    public enum ObjectType
    {
        None,
        Mesh,
        Decoration,
        Light,
        Special,
        PlayerSpawnPoint,
        Decal,
        AudioSource,
    }
    [CreateAssetMenu(menuName = "MapEditor/Object", fileName = "Null Object")]
    public class ObjectItem : ScriptableObject
    {
        public string ObjectName => name;
        public ObjectType type=ObjectType.None;
        public GameObject prefab;
        public Texture2D icon;
    }
}