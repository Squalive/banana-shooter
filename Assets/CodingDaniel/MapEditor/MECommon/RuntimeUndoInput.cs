using System;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

namespace CodingDaniel.MapEditor.MECommon
{
    public class RuntimeUndoInput : MonoBehaviour
    {
        public KeyCode UndoKey = KeyCode.Z;
        public KeyCode RedoKey = KeyCode.Y;
        public KeyCode RuntimeModifierKey = KeyCode.LeftControl;
        public KeyCode EditorModifierKey = KeyCode.LeftShift;
        public KeyCode ModifierKey
        {
            get
            {
#if UNITY_EDITOR
                return EditorModifierKey;
#else
                return RuntimeModifierKey;
#endif
            }
        }

        public static RuntimeUndoInput Instance { private set; get; }

        private IME _me;

        private void Awake()
        {
            _me=MEBase.Instance;
            Instance = this;
        }
        
        private void Update()
        {
            if (UndoAction())
            {
                _me.Undo.Undo();
            }
            else if (RedoAction())
            {
                _me.Undo.Redo();
            }
        }

        protected virtual bool UndoAction()
        {
            return Input.GetKeyDown(UndoKey) && Input.GetKey(ModifierKey);
        }

        protected virtual bool RedoAction()
        {
            return Input.GetKeyDown(RedoKey) && Input.GetKey(ModifierKey);
        }
    }
}
