using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using UnityEngine;

namespace CodingDaniel.MapEditor.UI.AddObject
{
    [CreateAssetMenu(menuName = "MapEditor/Light Object", fileName = "Null Light")]
    public class LightObjectItem : ObjectItem
    {
        public LightType lightType = LightType.Point;
    }
}
