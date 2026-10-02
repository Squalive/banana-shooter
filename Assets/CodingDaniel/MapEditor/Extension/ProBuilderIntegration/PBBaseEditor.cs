
using UnityEngine;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public abstract class PBBaseEditor : MonoBehaviour,IMeshEditor
    {
        public virtual int GraphicsLayer
        {
            get;
            set;
        }

        public abstract bool HasSelection
        {
            get;
        }

        public virtual bool CenterMode
        {
            get;
            set;
        }

        public virtual bool GlobalMode
        {
            get;
            set;
        }

        public virtual bool UVEditingMode
        {
            get;
            set;
        }

        public abstract Vector3 Position
        {
            get;
            set;
        }

        public abstract Vector3 Normal
        {
            get;
        }

        public virtual Quaternion Rotation
        {
            get { return Quaternion.identity; }
        }
        
        public abstract GameObject Target
        {
            get;
        }

        public virtual MeshSelection ClearSelection()
        {
            return null;
        }

        public virtual void Delete()
        {
        }

        public virtual void Subdivide()
        {

        }

        public virtual void Merge()
        {

        }

        public virtual void Extrude(float distance = 0)
        {

        }

        public virtual MeshSelection SelectHoles()
        {
            return null;
        }

        public virtual void FillHoles()
        {
        }

        public virtual void Bridge()
        {
            
        }

        public virtual void InsertEdgeLoop()
        {
            
        }

        public virtual void InsertFace()
        {
            
        }

        public virtual void SetSelection(MeshSelection selection)
        {
            
        }

        public virtual MeshSelection GetSelection()
        {
            return null;
        }

        public virtual void Hover(Camera camera, Vector3 pointer)
        {
            
        }

        public virtual void BeginMove()
        {

        }

        public virtual void EndMove()
        {

        }


        public virtual void BeginRotate(Quaternion initialRotation)
        {
        }

        public virtual void BeginScale()
        {

        }

        public virtual void EndRotate()
        {

        }

        public virtual void EndScale()
        {

        }

        public virtual void Rotate(Quaternion rotation)
        {
        }

        public virtual void Scale(Vector3 scale, Quaternion rotation)
        {
        }

        public virtual MeshSelection Select(Camera camera, Vector3 pointer, bool shift, bool ctrl, bool depthTest)
        {
            return null;
        }

        public virtual MeshSelection Select(Camera camera, Rect rect, Rect uiRootRect, GameObject[] gameObjects, bool depthTest, MeshEditorSelectionMode mode)
        {
            return null;
        }

        public virtual MeshSelection Select(Material material)
        {
            return null;
        }

        public virtual MeshSelection Unselect(Material material)
        {
            return null;
        }


        public virtual MeshEditorState GetState(bool recordUV)
        {
            return null;
        }

        public virtual void SetState(MeshEditorState state)
        {
            
        }
        
        
    }
}
