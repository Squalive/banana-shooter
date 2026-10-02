using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

// The VFXBinder Attribute will populate this class into the property binding's add menu.
namespace Cosmetic
{
    [VFXBinder("Transform/MyTransform")]
// The class need to extend VFXBinderBase
    public class MyVFXTransformBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; UpdateSubProperties(); } }
 
        [VFXPropertyBinding("UnityEditor.VFX.Transform"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "Transform";
        public Transform Target = null;
 
        private ExposedProperty Position;
        private ExposedProperty Angles;
        private ExposedProperty Scale;

        public bool localScale = false;
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }
 
        void OnValidate()
        {
            UpdateSubProperties();
        }
 
        void UpdateSubProperties()
        {
            Position = m_Property + "_position";
            Angles = m_Property + "_angles";
            Scale = m_Property + "_scale";
        }
 
        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3((int)Position) && component.HasVector3((int)Angles) && component.HasVector3((int)Scale);
        }
 
        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3((int)Position, Target.position);
            component.SetVector3((int)Angles, Target.eulerAngles);
            component.SetVector3((int)Scale,localScale ?  Target.localScale : Target.lossyScale);
        }
 
        public override string ToString()
        {
            return string.Format("Transform : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}