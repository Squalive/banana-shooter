using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using UnityEngine;

namespace CodingDaniel.MapEditor.UI.AddObject
{
    [CreateAssetMenu(menuName = "MapEditor/Mesh Object", fileName = "Null Mesh")]
    public class MeshObjectItem : ObjectItem
    {
        public PBShapeType shapeType = PBShapeType.Cube;
    }
}
