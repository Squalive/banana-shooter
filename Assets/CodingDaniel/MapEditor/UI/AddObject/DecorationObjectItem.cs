using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.MEEditor.MESave;
using UnityEngine;
using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.UI.AddObject
{
    [CreateAssetMenu(menuName = "MapEditor/Decoration Object", fileName = "Null Decoration")]
    public class DecorationObjectItem : ObjectItem
    {
        [FormerlySerializedAs("decorationType")] public MEDecoration.EDecorationType eDecorationType = MEDecoration.EDecorationType.Obstacle;
    }
}
