using System;

using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public class ProBuilderToolUI : MonoBehaviour
    {
        private ProBuilderTool _tool;
        private IME _me;
        private void Awake()
        {
            _me=MEBase.Instance;
            _tool = ProBuilderTool.Instance;
        }

        
        public void SetProBuilderMode(int index)
        {
            _tool.Mode = (ProBuilderToolMode) index;
        }

        
        public void FlipNormal()
        {
            ProBuilderTool.Instance.FlipNormal();
        }
        
        public void SubdivideObject()
        {
            ProBuilderTool.Instance.Subdivide();
        }
        
        public void SubdivideEdge()
        {
            ProBuilderTool.Instance.SubdivideEdges();
        }
        
        public void SubdivideFace()
        {
            ProBuilderTool.Instance.SubdivideFaces();
        }

        
        public void InsertEdgeLoop()
        {
            ProBuilderTool.Instance.InsertEdgeLoop();
        }

        
        public void ResetUV()
        {
            ProBuilderTool.Instance.ResetUVs();
        }

        
        public void MergeFace()
        {
            ProBuilderTool.Instance.MergeFaces();
        }

        
        public void FillHole()
        {
            ProBuilderTool.Instance.FillHoles();
        }

        
        public void Undo()
        {
            _me.Undo.Undo();
        }

        
        public void Redo()
        {
            _me.Undo.Redo();
        }

        
        public void Combine()
        {
            ProBuilderTool.Instance.Combine();
        }
        
        public void Bridge()
        {
            ProBuilderTool.Instance.Bridge();
        }
        public void ConvertUV()
        {
        }
        public void Bevel()
        {
            // ProBuilderTool.Instance.be
        }
    }
}
