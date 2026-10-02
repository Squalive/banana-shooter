using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

namespace CodingDaniel.MapEditor.Graphics
{
    public interface IOutlineManager
    {
        IMESelection Selection
        {
            get;
            set;
        }

        /// <summary>
        /// The renderers currently outlined because the pointer is over them.
        /// </summary>
        bool HasHover
        {
            get;
        }
        
        bool ContainsRenderer(Renderer renderer);
        void AddRenderers(Renderer[] renderers);
        void RemoveRenderers(Renderer[] renderers);
        void RecreateCommandBuffer();
    }
}
