using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CodingDaniel.MapEditor.MEEditor
{
    public delegate void RuntimeSelectionChanged(Object[] unselectedObjects);
    public interface IMESelection : IEnumerable
    {
        event RuntimeSelectionChanged SelectionChanged;
        bool Enabled
        {
            get;
            set;
        }

        bool EnableUndo
        {
            get;
            set;
        }

        GameObject ActiveGameObject
        {
            get;
            set;
        }
        Object ActiveObject
        {
            get;
            set;
        }
        Object[] Objects
        {
            get;
            set;
        }

        GameObject[] GameObjects
        {
            get;
        }

        Transform ActiveTransform
        {
            get;
        }

        int Length
        {
            get;
        }

        bool IsSelected(Object obj);

        void Select(Object activeObject, Object[] selection);
    }

    public class MESelection : IMESelection
    {
        public event RuntimeSelectionChanged SelectionChanged;
        
        private bool _isEnabled = true;
        public bool Enabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                if (!_isEnabled)
                {
                    Objects = null;
                }
            }
        }

        public bool EnableUndo { get; set; } = true;
        
        void RaiseSelectionChanged(Object[] unselectedObjects)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(unselectedObjects);
            }
        }
        
        public GameObject ActiveGameObject
        {
            get { return ActiveObject as GameObject; }
            set
            {
                ActiveObject = value;
            }
        }

        protected Object _activeObject;
        public Object ActiveObject
        {
            get { return _activeObject; }
            set
            {
                if (value == null)
                {
                    Objects = null;
                }
                else
                {
                    Objects = new[] { value };
                }
            }
        }

        private Object[] _objects;
        public Object[] Objects
        {
            get { return _objects; }
            set
            {
                if (!_isEnabled)
                {
                    return;
                }

                if (IsSelectionChanged(value))
                {
                    SetObjects(value);
                    // if (_editor != null && _editor.Undo.Enabled && EnableUndo)
                    // {
                    //     _editor.Undo.Select(this, value, null);
                    // }
                    // else
                    // {
                    //     SetObjects(value);
                    // }
                }
            }
        }
        
        public int Length
        {
            get
            {
                if (_objects == null)
                {
                    return 0;
                }
                return _objects.Length;
            }
        }
        private IME _editor;

        public MESelection(IME me)
        {
            _editor = me;
        }

        private HashSet<Object> _selectionHS = null;
        
        public bool IsSelected(Object obj)
        {
            if (_selectionHS == null)
            {
                return false;
            }
            return _selectionHS.Contains(obj);
        }

        private void UpdateHS()
        {
            if (_objects != null)
            {
                _selectionHS = new HashSet<Object>(_objects);
            }
            else
            {
                _selectionHS = null;
            }
        }

        private bool IsSelectionChanged(Object[] value)
        {
            if (_objects == value)
            {
                return false;
            }

            if (_objects == null)
            {
                return value.Length != 0;
            }

            if (value == null)
            {
                return _objects.Length != 0;
            }

            if (_objects.Length != value.Length)
            {
                return true;
            }

            for (int i = 0; i < _objects.Length; ++i)
            {
                if (_objects[i] != value[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void SetObjects(Object[] value)
        {
            if (!IsSelectionChanged(value))
            {
                return;
            }
            
            Object[] oldObjects = _objects != null ? _objects.Where(obj => obj != null).ToArray() : _objects;
            if (value == null)
            {
                _objects = null;
                _activeObject = null;
                
                // Debug.Log("null reference");
            }
            else
            {
                // Debug.Log("select " + value.FirstOrDefault().name);
                _objects = value.Where(v => v != null).ToArray();
                if (_activeObject == null || !_objects.Contains(_activeObject))
                {
                    _activeObject = _objects.OfType<Object>().FirstOrDefault();
                }
            }

            UpdateHS();
            RaiseSelectionChanged(oldObjects);
        }

        public GameObject[] GameObjects
        {
            get
            {
                if (_objects == null)
                {
                    return null;
                }

                return _objects.OfType<GameObject>().ToArray();
            }
        }
        
        public Transform ActiveTransform
        {
            get
            {
                if (_activeObject == null)
                {
                    return null;
                }

                if (_activeObject is GameObject)
                {
                    return ((GameObject)_activeObject).transform;
                }
                return null;
            }
        }

        public void Select(Object activeObject, Object[] selection)
        {
            if (IsSelectionChanged(selection))
            {
                _activeObject = activeObject;
                SetObjects(selection);
            }
        }

        private Object[] _empty = Array.Empty<Object>();
        public IEnumerator GetEnumerator()
        {
            if (_objects != null)
            {
                return _objects.GetEnumerator();
            }
            return _empty.GetEnumerator();
        }
    }
}