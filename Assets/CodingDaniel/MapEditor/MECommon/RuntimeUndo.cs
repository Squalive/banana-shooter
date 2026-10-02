using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using UnityEngine.UI;
using ExposeToEditor = CodingDaniel.MapEditor.MEEditor.ExposeToEditor;
using UnityObject = UnityEngine.Object;
namespace CodingDaniel.MapEditor.MECommon
{
    public class UndoStack<T> : IEnumerable<UndoStack<T>.Node> where T : class
    {
        public class Node
        {
            public Node Next;
            public Node Prev;
            public T Data;

            public UndoStack<T> Stack
            {
                get { return _stack; }
            }

            private UndoStack<T> _stack;
            public Node(UndoStack<T> stack)
            {
                _stack = stack;
            }
        }

        private Node _first;
        private Node _last;
        private Node _tos;
        private int _tosIndex;
        private int _count;
        private T[] _empty = new T[0];

        public int Count
        {
            get { return _count; }
        }

        public bool CanPop
        {
            get { return _tosIndex > 0; }
        }

        public bool CanRestore
        {
            get { return _tosIndex < _count; }
        }

        public UndoStack(int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException("size", "size < 1");
            }

            size = size + 1;

            _first = new Node(this);

            Node node = _first;
            for (int i = 1; i < size; ++i)
            {
                Node next = new Node(this)
                {
                    Prev = node,
                };
                node.Next = next;
                node = next;
            }

            _last = node;
            _last.Next = _first;
            _first.Prev = _last;

            _tos = _first;
        }

        public void Push(T item, List<T> purgeList = null)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            //T[] purgeItems = _empty;
            if (_tos == _last)
            {
                _last = _last.Next;
                _first = _first.Next;

                if (purgeList != null)
                {
                    if (_tos.Next.Data != null)
                    {
                        purgeList.Add(_tos.Next.Data);
                    }
                }
            }
            else
            {
                if (purgeList != null)
                {
                    Node node = _tos;
                    for (int i = 0; i < (_count - _tosIndex); ++i)
                    {
                        if (node.Data != null)
                        {
                            purgeList.Add(node.Data);
                        }

                        node = node.Next;
                    }
                }

                _tosIndex++;
                _count = _tosIndex;
            }

            _tos.Data = item;
            _tos = _tos.Next;
            _tos.Data = null;
        }

        public T Pop()
        {
            if (!CanPop)
            {
                throw new InvalidOperationException("Stack is empty");
            }

            _tos = _tos.Prev;
            _tosIndex--;
            return _tos.Data;
        }

        public T Peek()
        {
            if (!CanPop)
            {
                throw new InvalidOperationException("Stack is empty");
            }

            return _tos.Prev.Data;
        }

        public T Restore()
        {
            if (!CanRestore)
            {
                throw new InvalidOperationException("Nothing to restore");
            }

            T restored = _tos.Data;
            _tos = _tos.Next;
            _tosIndex++;
            return restored;
        }

        public void Clear()
        {
            Node node = _first;
            do
            {
                node.Data = null;
                node = node.Next;
            }
            while (node.Prev != _last);

            _tosIndex = 0;
            _count = 0;
            _tos = _first;
        }

        public Node Find(T data)
        {
            Node node = _first;
            do
            {
                if (node.Data == data)
                {
                    return node;
                }
                node = node.Next;
            }
            while (node != _first);
            return null;
        }

        public T Purge(Node node)
        {
            if (node.Stack != this)
            {
                throw new ArgumentException("node does not belong to this stack");
            }

            if (_count == 0)
            {
                throw new InvalidOperationException("stack is empty");
            }

            if (node.Data == null)
            {
                return null;
            }

            if (node != _last)
            {
                if (node == _first)
                {
                    _first = _first.Next;
                }

                if (_tos == node)
                {
                    _tos = _tos.Next;
                }

                Node prev = node.Prev;
                Node next = node.Next;
                if (prev != next)
                {
                    prev.Next = next;
                    next.Prev = prev;
                }

                _last.Next = node;
                node.Prev = _last;

                _last = node;
                _last.Next = _first;
                _first.Prev = _last;
            }

            _count--;
            if (_count < _tosIndex)
            {
                _tosIndex = _count;
            }

            T data = _last.Data;
            _last.Data = null;
            return data;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _GetEnumerator();
        }

        IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
        {
            return _GetEnumerator();
        }

        private IEnumerator<Node> _GetEnumerator()
        {
            int index = 0;
            Node node = _first;
            do
            {
                if (index == _count)
                {
                    yield break;
                }
                index++;

                yield return node;
                node = node.Next;
            }
            while (node != _first);
        }
    }

    public delegate bool UndoRedoCallback(Record record);
    public delegate void PurgeCallback(Record record);
    public delegate bool EraseReferenceCallback(Record record, object oldRef, object newRef);

    public class Record
    {
        private object _oldState;
        private object _newState;
        private object _target;

        /// <summary>
        /// Apply changes to object. Return true if object state has been changed
        /// </summary>
        private UndoRedoCallback _redoCallback;

        /// <summary>
        /// Revert object state changes. Return true if object state has been changed
        /// </summary>
        private UndoRedoCallback _undoCallback;

        /// <summary>
        /// Cleanup. Record is removed from stack and object state could not be reverted anymore.
        /// </summary>
        private PurgeCallback _purgeCallback;

        /// <summary>
        /// Erase/Repalce reference to object. Return false is record is still valid and can change object state, otherwise return true.
        /// </summary>
        private EraseReferenceCallback _eraseCallback;

        public object Target
        {
            get { return _target; }
            set { _target = value; }
        }

        public object OldState
        {
            get { return _oldState; }
            set { _oldState = value; }
        }

        public object NewState
        {
            get { return _newState; }
            set { _newState = value; }
        }

        public Record(object target, object newState, object oldState, UndoRedoCallback redoCallback, UndoRedoCallback undoCallback, PurgeCallback purgeCallback, EraseReferenceCallback eraseCallback)
        {
            if (redoCallback == null)
            {
                throw new ArgumentNullException("redoCallback");
            }

            if (undoCallback == null)
            {
                throw new ArgumentNullException("undoCallback");
            }

            _target = target;
            _redoCallback = redoCallback;
            _undoCallback = undoCallback;
            _purgeCallback = purgeCallback;
            _eraseCallback = eraseCallback;
            _newState = newState;
            _oldState = oldState;
        }

        public bool Undo()
        {
            return _undoCallback(this);
        }

        public bool Redo()
        {
            return _redoCallback(this);
        }

        public void Purge()
        {
            if (_purgeCallback != null)
            {
                _purgeCallback(this);
            }

        }

        public bool Erase(object oldRef, object newRef)
        {
            bool result = false;
            if (_eraseCallback != null)
            {
                result = _eraseCallback(this, oldRef, newRef);
            }
            return result;
        }
    }


    public delegate void RuntimeUndoEventHandler();
    public interface IRuntimeUndo
    {
        event RuntimeUndoEventHandler BeforeUndo;
        event RuntimeUndoEventHandler UndoCompleted;
        event RuntimeUndoEventHandler BeforeRedo;
        event RuntimeUndoEventHandler RedoCompleted;
        event RuntimeUndoEventHandler StateChanged;

        bool Enabled
        {
            get;
            set;
        }

        bool CanUndo
        {
            get;
        }

        bool CanRedo
        {
            get;
        }

        bool IsRecording
        {
            get;
        }

        void BeginRecord();
        void EndRecord();
        void Redo();
        void Undo();
        void Purge();
        void Erase(object oldRef, object newRef, bool ignoreLock = false);

        void Store();
        void Restore();

        Record CreateRecord(UndoRedoCallback redoCallback, UndoRedoCallback undoCallback, PurgeCallback purgeCallback = null, EraseReferenceCallback eraseCallback = null);
        Record CreateRecord(object target, object newState, object oldState, UndoRedoCallback redoCallback, UndoRedoCallback undoCallback, PurgeCallback purgeCallback = null, EraseReferenceCallback eraseCallback = null);
        void Select(IMESelection selection, UnityObject[] objects, UnityObject activeObject);
        void EraseFromSelection(UnityObject[] objects);
        void RegisterCreatedObjects(ExposeToEditor[] createdObjects, Action afterRedo = null, Action afterUndo = null);
        void DestroyObjects(ExposeToEditor[] destoryedObjects, Action afterRedo = null, Action afterUndo = null);

        void RecordValue(object target, MemberInfo memberInfo, Action afterRedo = null, Action afterUndo = null);
        void RecordValue(object target, object accessor, MemberInfo memberInfo, Action afterRedo = null, Action afterUndo = null);
        void BeginRecordValue(object target, MemberInfo memberInfo);
        void BeginRecordValue(object target, object accessor, MemberInfo memberInfo);
        void EndRecordValue(object target, MemberInfo memberInfo, Action afterRedo = null, Action afterUndo = null);
        void EndRecordValue(object target, object accessor, MemberInfo memberInfo, Action<object, object> targetErased = null, Action afterRedo = null, Action afterUndo = null);

        void BeginRecordTransform(Transform target, Action<Transform> afterUndo = null);
        void EndRecordTransform(Transform target, Action<Transform> afterRedo = null);

        void BeginRecordTransform(Transform target, Transform parent, int siblingIndex = -1, Action<Transform> afterUndo = null);
        void EndRecordTransform(Transform target, Transform parent, int siblingIndex = -1, Action<Transform> afterRedo = null);

        #region Obsolete
        [Obsolete("Use void Select(IRuntimeSelection selection, UnityObject[] objects, UnityObject activeObject) instead")]
        void Select(UnityObject[] objects, UnityObject activeObject);
        #endregion
    }


    /// <summary>
    /// Class for handling undo and redo operations
    /// </summary>
    public class RuntimeUndo : IRuntimeUndo
    {
        private class SelectionState
        {
            public UnityObject ActiveObject;
            public UnityObject[] Objects;

            public SelectionState(UnityObject[] objects, UnityObject activeObject)
            {
                ActiveObject = activeObject;
                if (objects != null)
                {
                    Objects = objects.ToArray();
                }
                else
                {
                    Objects = null;
                }
            }

            public SelectionState(IMESelection selection)
            {
                ActiveObject = selection.ActiveObject;
                if (selection.Objects != null)
                {
                    Objects = selection.Objects.ToArray();
                }
                else
                {
                    Objects = null;
                }
            }
        }

        public class SetValuesState
        {
            public object Accessor;
            public MemberInfo[] MemberInfo;
            public object[] Values;

            public SetValuesState(object accessor, MemberInfo[] memberInfo, object[] values)
            {
                Accessor = accessor;
                MemberInfo = memberInfo;
                Values = values;
            }
        }

        private class TransformState
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Transform parent;
            public int siblingIndex = -1;
            public bool applyOnRedo;
        }

        public bool Enabled
        {
            get;
            set;
        }

        protected bool Locked
        {
            get;
            private set;
        }

        public bool CanUndo
        {
            get { return _stack.CanPop; }
        }

        public bool CanRedo
        {
            get { return _stack.CanRestore; }
        }

        public bool IsRecording
        {
            get { return _group != null; }
        }

        public event RuntimeUndoEventHandler BeforeUndo;
        public event RuntimeUndoEventHandler UndoCompleted;
        public event RuntimeUndoEventHandler BeforeRedo;
        public event RuntimeUndoEventHandler RedoCompleted;
        public event RuntimeUndoEventHandler StateChanged;

        public const int Limit = 8192;

        private Dictionary<object, Dictionary<MemberInfo, object>> _objToValue;

        private List<Record> _group;
        private UndoStack<Record[]> _stack;
        private Stack<UndoStack<Record[]>> _stacks;
        private List<Record[]> _purgeRecords;
        private List<UndoStack<Record[]>.Node> _purgeNodes;

        private IME _rte;
        public RuntimeUndo(IME rte)
        {
            _rte = rte;
            Reset();
        }

        public void Reset()
        {
            Enabled = true;
            _group = null;
            _stack = new UndoStack<Record[]>(Limit);
            _stacks = new Stack<UndoStack<Record[]>>();
            _purgeRecords = new List<Record[]>();
            _purgeNodes = new List<UndoStack<Record[]>.Node>();
            _objToValue = new Dictionary<object, Dictionary<MemberInfo, object>>();
        }

        public void BeginRecord()
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }

            _group = new List<Record>();
        }

        public void EndRecord()
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }

            if (_group != null)
            {
                _stack.Push(_group.ToArray(), _purgeRecords);

                for (int i = 0; i < _purgeRecords.Count; ++i)
                {
                    Record[] purgeRecords = _purgeRecords[i];
                    if (purgeRecords != null)
                    {
                        for (int j = 0; j < purgeRecords.Length; ++j)
                        {
                            Record record = purgeRecords[j];
                            record.Purge();
                        }
                    }
                }
                _purgeRecords.Clear();
                _markAsDestroyedDuringLastOperation.Clear();

                if (StateChanged != null)
                {
                    StateChanged();
                }
            }
            _group = null;
        }

        public void Redo()
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }

            if (!_stack.CanRestore)
            {
                return;
            }

            try
            {
                Locked = true;
                DoRedo();
            }
            finally
            {
                Locked = false;
            }
        }

        private void DoRedo()
        {
            if (BeforeRedo != null)
            {
                BeforeRedo();
            }

            bool somethingHasChanged;
            do
            {
                somethingHasChanged = false;
                Record[] records = _stack.Restore();
                for (int i = 0; i < records.Length; ++i)
                {
                    Record record = records[i];
                    somethingHasChanged |= record.Redo();
                }
            }
            while (!somethingHasChanged && _stack.CanRestore);

            if (RedoCompleted != null)
            {
                RedoCompleted();
            }
        }

        public void Undo()
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }

            if (!_stack.CanPop)
            {
                return;
            }

            try
            {
                Locked = true;
                DoUndo();
            }
            finally
            {
                Locked = false;
            }
        }

        private void DoUndo()
        {
            if (BeforeUndo != null)
            {
                BeforeUndo();
            }

            bool somethingHasChanged;
            do
            {
                somethingHasChanged = false;
                Record[] records = _stack.Pop();

                for (int i = records.Length - 1; i >= 0; --i)
                {
                    Record record = records[i];
                    somethingHasChanged |= record.Undo();
                }
            }
            while (!somethingHasChanged && _stack.CanPop);

            if (UndoCompleted != null)
            {
                UndoCompleted();
            }
        }

        public void Purge()
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }

            _Purge();

            if (StateChanged != null)
            {
                StateChanged();
            }
        }

        private void _Purge()
        {
            foreach (UndoStack<Record[]>.Node node in _stack)
            {
                if (node.Data != null)
                {
                    for (int i = 0; i < node.Data.Length; ++i)
                    {
                        Record record = node.Data[i];
                        record.Purge();
                    }
                }
            }
            _stack.Clear();
            _group = null;
            if (_objToValue.Count > 0)
            {
                Debug.LogWarning("Unifished RecordValue operations exists.");
                _objToValue = new Dictionary<object, Dictionary<MemberInfo, object>>();
            }
        }

        public void Erase(object oldRef, object newRef, bool ignoreLock = false)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked && !ignoreLock)
            {
                return;
            }

            if (_objToValue.Count > 0)
            {
                Debug.LogWarning("Unifished RecordValue operations exists.");
                _objToValue = new Dictionary<object, Dictionary<MemberInfo, object>>();
            }

            foreach (UndoStack<Record[]>.Node node in _stack)
            {
                if (node.Data != null)
                {
                    int erased = 0;
                    for (int i = 0; i < node.Data.Length; ++i)
                    {
                        Record record = node.Data[i];
                        if (record.Erase(oldRef, newRef))
                        {
                            erased++;
                        }
                    }

                    if (erased > 0 && node.Data.Length == erased)
                    {
                        _purgeNodes.Add(node);
                    }
                }
            }

            for (int i = 0; i < _purgeNodes.Count; ++i)
            {
                UndoStack<Record[]>.Node purgeNode = _purgeNodes[i];
                if (purgeNode != null)
                {
                    for (int j = 0; j < purgeNode.Data.Length; ++j)
                    {
                        purgeNode.Data[j].Purge();
                    }
                }

                _stack.Purge(purgeNode);
            }

            _purgeNodes.Clear();

            if (StateChanged != null)
            {
                StateChanged();
            }
        }

        public void Store()
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }
            _stacks.Push(_stack);
            _stack = new UndoStack<Record[]>(Limit);
            if (StateChanged != null)
            {
                StateChanged();
            }
        }

        public void Restore()
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }

            if (_stacks.Count > 0)
            {
                _Purge();

                _stack = _stacks.Pop();
                if (StateChanged != null)
                {
                    StateChanged();
                }
            }
        }

        public Record CreateRecord(UndoRedoCallback redoCallback, UndoRedoCallback undoCallback, PurgeCallback purgeCallback = null, EraseReferenceCallback eraseCallback = null)
        {
            return CreateRecord(null, null, null, redoCallback, undoCallback, purgeCallback, eraseCallback);
        }

        public Record CreateRecord(object target, object newState, object oldState, UndoRedoCallback redoCallback, UndoRedoCallback undoCallback, PurgeCallback purgeCallback = null, EraseReferenceCallback eraseCallback = null)
        {
            //Debug.Log($"Create Record {target} {newState} {oldState}");

            if (!Enabled)
            {
                return null;
            }
            if (Locked)
            {
                return null;
            }

            if (purgeCallback == null)
            {
                purgeCallback = rec => { };
            }

            Record record = new Record(target, newState, oldState, redoCallback, undoCallback, purgeCallback, eraseCallback);
            if (_group != null)
            {
                _group.Add(record);
            }
            else
            {
                _stack.Push(new[] { record }, _purgeRecords);

                //these lines causes wrong behavior.Remove->undo->remove->undo again and objects are not recovered
                //reason: the same action from different records undone.
                for (int i = 0; i < _purgeRecords.Count; ++i)
                {
                    Record[] purgeItems = _purgeRecords[i];
                    if (purgeItems != null)
                    {
                        for (int j = 0; j < purgeItems.Length; ++j)
                        {
                            purgeItems[j].Purge();
                        }
                    }
                }


                _purgeRecords.Clear();

                if (StateChanged != null)
                {
                    StateChanged();
                }
            }
            return record;
        }

        private static bool HasSelectionChanged(UnityObject[] newObjects, UnityObject newActiveObject, IMESelection selection)
        {
            return HasSelectionChanged(newObjects, newActiveObject, selection.Objects, selection.ActiveObject);
        }

        private static bool HasSelectionChanged(UnityObject[] newObjects, UnityObject newActiveObject, UnityObject[] objects, UnityObject activeObject)
        {
            if (activeObject != newActiveObject)
            {
                return true;
            }

            if (objects == newObjects)
            {
                return false;
            }

            if (objects == null || newObjects == null)
            {
                return true;
            }

            if (objects.Length != newObjects.Length)
            {
                return true;
            }

            for (int i = 0; i < objects.Length; ++i)
            {
                if (objects[i] != newObjects[i])
                {
                    return true;
                }
            }

            return false;
        }

        public void EraseFromSelection(UnityObject[] objects)
        {
            foreach (var node in _stack)
            {
                Record[] records = node.Data;
                foreach (Record record in records)
                {
                    SelectionState oldState = record.OldState as SelectionState;
                    if (oldState != null)
                    {
                        foreach (object obj in objects)
                        {
                            EraseFromSelection(oldState, null, obj);
                        }
                    }

                    SelectionState newState = record.NewState as SelectionState;
                    if (newState != null)
                    {
                        foreach (object obj in objects)
                        {
                            EraseFromSelection(newState, null, obj);
                        }
                    }
                }
            }
        }

        private static void EraseFromSelection(SelectionState state, object newReference, object oldReference)
        {
            if ((object)state.ActiveObject == oldReference)
            {
                state.ActiveObject = newReference as UnityObject;
            }

            bool hasNulls = false;
            if (state.Objects != null)
            {
                for (int i = 0; i < state.Objects.Length; ++i)
                {
                    object reference = state.Objects[i];
                    if (reference == oldReference)
                    {
                        state.Objects[i] = newReference as UnityObject;
                        if (state.Objects[i] == null)
                        {
                            hasNulls = true;
                        }
                    }
                }
            }

            if (hasNulls)
            {
                state.Objects = state.Objects.Where(o => o != null).ToArray();
                if (state.Objects.Length == 0)
                {
                    state.Objects = null;
                }
            }
        }

        private bool ApplySelection(SelectionState state, IMESelection selection)
        {
            bool hasChanged = HasSelectionChanged(state.Objects, state.ActiveObject, selection);

            if (hasChanged)
            {
                selection.Select(state.ActiveObject, state.Objects);
            }

            return hasChanged;
        }

        public void Select(IMESelection selection, UnityObject[] objects, UnityObject activeObject)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }
            if (!HasSelectionChanged(objects, activeObject, selection))
            {
                return;
            }

            Record newRecord = CreateRecord(selection,
                new SelectionState(objects, activeObject),
                new SelectionState(selection),
                record => ApplySelection((SelectionState)record.NewState, (IMESelection)record.Target),
                record => ApplySelection((SelectionState)record.OldState, (IMESelection)record.Target),
                record => { /*do nothing*/ },
                (record, oldReference, newReference) =>
                {
                    SelectionState newState = (SelectionState)record.NewState;
                    SelectionState oldState = (SelectionState)record.OldState;
                    EraseFromSelection(oldState, newReference, oldReference);
                    EraseFromSelection(newState, newReference, oldReference);

                    bool purge = false;
                    if (!HasSelectionChanged(newState.Objects, newState.ActiveObject, oldState.Objects, oldState.ActiveObject))
                    {
                        purge = true;
                    }
                    return purge;
                });

            if (newRecord != null)
            {
                newRecord.Redo();
            }
        }

        private bool MarkAsDestroyed(Record record, bool destroyed)
        {
            ExposeToEditor[] objects = (ExposeToEditor[])record.Target;
            for (int i = 0; i < objects.Length; ++i)
            {
                ExposeToEditor obj = objects[i];
                if (obj != null)
                {
                    obj.MarkAsDestroyed = destroyed;
                }
            }
            return true;
        }

        private void PurgeMarkedAsDestoryed(Record record)
        {
            ExposeToEditor[] objects = (ExposeToEditor[])record.Target;
            for (int i = 0; i < objects.Length; ++i)
            {
                ExposeToEditor obj = objects[i];

                if (obj != null && obj.MarkAsDestroyed)
                {
                    if (!_markAsDestroyedDuringLastOperation.Contains(obj))
                    {
                        UnityObject.DestroyImmediate(obj.gameObject);
                    }
                }
            }
        }

        private static bool EraseMarkedAsDestroyed(Record record, object newReference, object oldReference)
        {
            ExposeToEditor[] objects = (ExposeToEditor[])record.Target;
            bool hasNulls = false;
            for (int i = 0; i < objects.Length; ++i)
            {
                ExposeToEditor obj = objects[i];
                if (obj == null)
                {
                    continue;
                }
                if (oldReference is GameObject)
                {
                    if ((object)obj.gameObject == oldReference)
                    {
                        objects[i] = null;
                        GameObject newRef = newReference as GameObject;
                        if (newRef != null)
                        {
                            objects[i] = newRef.GetComponent<ExposeToEditor>();
                        }
                    }
                }

                if (objects[i] == null)
                {
                    hasNulls = true;
                }
            }

            if (hasNulls)
            {
                objects = objects.Where(obj => obj != null).ToArray();
                record.Target = objects;
            }

            return objects.Length == 0;
        }

        public void RegisterCreatedObjects(ExposeToEditor[] createdObjects, Action afterRedo = null, Action afterUndo = null)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }
            Record newRecord = CreateRecord(createdObjects, false, true,
                record => { bool result = MarkAsDestroyed(record, (bool)record.NewState); afterRedo?.Invoke(); return result; },
                record => { bool result = MarkAsDestroyed(record, (bool)record.OldState); afterUndo?.Invoke(); return result; },
                record => PurgeMarkedAsDestoryed(record),
                (record, oldReference, newReference) => EraseMarkedAsDestroyed(record, newReference, oldReference));

            if (newRecord != null)
            {
                newRecord.Redo();
            }
        }

        //To prevent gameobject from being destroyed during purge operation (in case if they are referenced somewhere in the stack)
        private HashSet<ExposeToEditor> _markAsDestroyedDuringLastOperation = new HashSet<ExposeToEditor>();
        public void DestroyObjects(ExposeToEditor[] destoryedObjects, Action afterRedo = null, Action afterUndo = null)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }

            for (int i = 0; i < destoryedObjects.Length; ++i)
            {
                if (!_markAsDestroyedDuringLastOperation.Contains(destoryedObjects[i]))
                {
                    _markAsDestroyedDuringLastOperation.Add(destoryedObjects[i]);
                }
            }

            Record newRecord = CreateRecord(destoryedObjects, true, false,
               record => { bool result = MarkAsDestroyed(record, (bool)record.NewState); afterRedo?.Invoke(); return result; },
               record => { bool result = MarkAsDestroyed(record, (bool)record.OldState); afterUndo?.Invoke(); return result; },
               record => PurgeMarkedAsDestoryed(record),
               (record, oldReference, newReference) => EraseMarkedAsDestroyed(record, newReference, oldReference));

            if (newRecord != null)
            {
                newRecord.Redo();
            }

            if (!IsRecording)
            {
                _markAsDestroyedDuringLastOperation.Clear();
            }
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }


        private static Array DuplicateArray(Array array)
        {
            Array newArray = (Array)Activator.CreateInstance(array.GetType(), array.Length);
            if (array != null)
            {
                for (int i = 0; i < newArray.Length; ++i)
                {
                    newArray.SetValue(array.GetValue(i), i);
                }
            }

            return array;
        }

        private object GetValue(object accessor, MemberInfo m)
        {
            PropertyInfo p = m as PropertyInfo;
            if (p != null)
            {
                if (accessor == null || (accessor is UnityObject) && null == (UnityObject)accessor)
                {
                    return GetDefault(p.PropertyType);
                }

                object val = p.GetValue(accessor, null);
                if (val is Array)
                {
                    val = DuplicateArray((Array)val);
                }
                return val;
            }

            FieldInfo f = m as FieldInfo;
            if (f != null)
            {
                if (accessor == null || (accessor is UnityObject) && null == (UnityObject)accessor)
                {
                    return GetDefault(f.FieldType);
                }
                object val = f.GetValue(accessor);
                if (val is Array)
                {
                    val = DuplicateArray((Array)val);
                }
                return val;
            }

            if (m is MethodInfo)
            {
                return null;
            }

            throw new ArgumentException("member is not FieldInfo and is not PropertyInfo", "m");
        }

        private object[] GetValues(object accessor, MemberInfo[] memberInfo)
        {
            object[] values = new object[memberInfo.Length];
            for (int i = 0; i < memberInfo.Length; ++i)
            {
                values[i] = GetValue(accessor, memberInfo[i]);
            }
            return values;
        }

        private void AssignValue(object accessor, MemberInfo m, object value)
        {
            if (accessor == null || (accessor is UnityObject) && null == (UnityObject)accessor)
            {
                return;
            }
            PropertyInfo p = m as PropertyInfo;
            if (p != null)
            {
                p.SetValue(accessor, value, null);
                return;
            }

            FieldInfo f = m as FieldInfo;
            if (f != null)
            {
                f.SetValue(accessor, value);
                return;
            }

            if (m is MethodInfo)
            {
                return;
            }

            throw new ArgumentException("member is not FieldInfo and is not PropertyInfo", "m");
        }

        private void AssingValues(object accessor, MemberInfo[] memberInfo, object[] values)
        {
            for (int i = 0; i < memberInfo.Length; ++i)
            {
                AssignValue(accessor, memberInfo[i], values[i]);
            }
        }

        private bool AssignValues(SetValuesState state, Action callback)
        {
            if (state.Accessor == null || (state.Accessor is UnityObject) && null == (UnityObject)state.Accessor)
            {
                return false;
            }

            bool isValueChanged = false;
            for (int i = 0; i < state.Values.Length; ++i)
            {
                object oldValue = GetValue(state.Accessor, state.MemberInfo[i]);
                object newValue = state.Values[i];
                if (IsValueChanged(oldValue, newValue))
                {
                    isValueChanged = true;
                }
                AssignValue(state.Accessor, state.MemberInfo[i], newValue);
            }

            if (callback != null)
            {
                callback();
            }

            return isValueChanged;
        }

        private static bool IsValueChanged(object a, object b)
        {
            if (a == null && b == null)
            {
                return false;
            }

            if (a != null && b != null)
            {
                if (a is Vector3 && b is Vector3)
                {
                    return (Vector3)a != (Vector3)b;
                }
                else if (a is Vector2 && b is Vector2)
                {
                    return (Vector2)a != (Vector2)b;
                }
                else if (a is Vector4 && b is Vector4)
                {
                    return (Vector4)a != (Vector4)b;
                }

                return !a.Equals(b);
            }

            return true;
        }

        private void EraseFromSetValuesState(SetValuesState state, object newReference, object oldReference)
        {
            if (state.Accessor == oldReference)
            {
                state.Accessor = newReference;
            }

            bool hasNulls = false;
            if (state.Values != null)
            {
                for (int i = 0; i < state.Values.Length; ++i)
                {
                    object reference = state.Values[i];
                    if (reference == oldReference)
                    {
                        state.Values[i] = newReference;
                        if (newReference == null)
                        {
                            state.MemberInfo[i] = null;
                            hasNulls = true;
                        }
                    }
                    else if (reference is IList)
                    {
                        IList list = (IList)reference;
                        for (int j = 0; j < list.Count; ++j)
                        {
                            if (list[j] == oldReference)
                            {
                                list[j] = newReference;
                            }
                        }
                    }
                }
            }

            if (hasNulls)
            {
                state.Values = state.Values.Where(o => o != null).ToArray();
                state.MemberInfo = state.MemberInfo.Where(o => o != null).ToArray();
            }
        }

        private void RecordValues(object target, object accessor, MemberInfo[] memberInfo, object[] oldValues, Action<object, object> targetErased, Action afterRedo, Action afterUndo)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }
            Record newRecord = CreateRecord(target,
                new SetValuesState(accessor, memberInfo, GetValues(accessor, memberInfo)),
                new SetValuesState(accessor, memberInfo, oldValues),
                record => AssignValues((SetValuesState)record.NewState, afterRedo),
                record => AssignValues((SetValuesState)record.OldState, afterUndo),
                record => { },
                (record, oldReference, newReference) =>
                {
                    if (record.Target == oldReference)
                    {
                        record.Target = newReference;
                        if (targetErased != null)
                        {
                            targetErased(accessor, record.Target);
                        }
                        if (record.Target == null)
                        {
                            return true;
                        }
                    }

                    SetValuesState newState = (SetValuesState)record.NewState;
                    SetValuesState oldState = (SetValuesState)record.OldState;

                    EraseFromSetValuesState(newState, newReference, oldReference);
                    EraseFromSetValuesState(oldState, newReference, oldReference);

                    if (newState.Values.Length == 0 && oldState.Values.Length == 0)
                    {
                        return true;
                    }

                    if (newState.Accessor == null)
                    {
                        return true;
                    }

                    return false;
                });
        }

        private void RecordValue(object target, object accessor, MemberInfo memberInfo, object oldValue, Action<object, object> targetErased, Action afterRedo, Action afterUndo)
        {
            RecordValues(target, accessor, new[] { memberInfo }, new[] { oldValue }, targetErased, afterRedo, afterUndo);
        }

        public void RecordValue(object target, MemberInfo memberInfo, Action afterRedo, Action afterUndo)
        {
            RecordValue(target, target, memberInfo, GetValue(target, memberInfo), null, afterRedo, afterUndo);
        }

        public void RecordValue(object target, object accessor, MemberInfo memberInfo, Action afterRedo, Action afterUndo)
        {
            RecordValue(target, accessor, memberInfo, GetValue(accessor, memberInfo), null, afterRedo, afterUndo);
        }

        public void BeginRecordValue(object target, MemberInfo memberInfo)
        {
            BeginRecordValue(target, target, memberInfo);
        }

        public void BeginRecordValue(object target, object accessor, MemberInfo memberInfo)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }
            Dictionary<MemberInfo, object> memberInfoToValue;
            if (!_objToValue.TryGetValue(target, out memberInfoToValue))
            {
                memberInfoToValue = new Dictionary<MemberInfo, object>();
                _objToValue.Add(target, memberInfoToValue);
            }

            if (memberInfoToValue.ContainsKey(memberInfo))
            {
                Debug.LogWarning("Unfinished record value operation for " + memberInfo.Name + " exist");
            }

            memberInfoToValue[memberInfo] = GetValue(accessor, memberInfo);
        }

        public void EndRecordValue(object target, MemberInfo memberInfo, Action afterRedo, Action afterUndo)
        {
            EndRecordValue(target, target, memberInfo, null, afterRedo, afterUndo);
        }

        public void EndRecordValue(object target, object accessor, MemberInfo memberInfo, Action<object, object> targetErased, Action afterRedo, Action afterUndo)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }
            Dictionary<MemberInfo, object> memberInfoToValue;
            if (!_objToValue.TryGetValue(target, out memberInfoToValue))
            {
                return;
            }

            object oldValue;
            if (!memberInfoToValue.TryGetValue(memberInfo, out oldValue))
            {
                return;
            }

            memberInfoToValue.Remove(memberInfo);
            if (memberInfoToValue.Count == 0)
            {
                _objToValue.Remove(target);
            }

            RecordValue(target, accessor, memberInfo, oldValue, targetErased, afterRedo, afterUndo);
        }

        public void BeginRecordTransform(Transform target, Action<Transform> afterUndo = null)
        {
            RecordTransform(false, target, null, -1, afterUndo);
        }

        public void EndRecordTransform(Transform target, Action<Transform> afterRedo = null)
        {
            RecordTransform(true, target, null, -1, afterRedo);
        }

        public void BeginRecordTransform(Transform target, Transform parent, int siblingIndex = -1, Action<Transform> afterUndo = null)
        {
            RecordTransform(false, target, parent, siblingIndex, afterUndo);
        }

        public void EndRecordTransform(Transform target, Transform parent, int siblingIndex = -1, Action<Transform> afterRedo = null)
        {
            RecordTransform(true, target, parent, siblingIndex, afterRedo);
        }

        private void RecordTransform(bool applyOnRedo, Transform target, Transform parent = null, int siblingIndex = -1, Action<Transform> callback = null)
        {
            if (!Enabled)
            {
                return;
            }
            if (Locked)
            {
                return;
            }
            TransformState newState = new TransformState { position = target.position, rotation = target.rotation, scale = target.localScale };
            newState.parent = parent;
            newState.siblingIndex = siblingIndex;
            newState.applyOnRedo = applyOnRedo;

            CreateRecord(target, newState, null,
                record => ApplyTransform(record, true, callback),
                record => ApplyTransform(record, false, callback),
                record => { },
                (record, oldReference, newReference) =>
                {
                    return false;
                });
        }

        private static bool ApplyTransform(Record record, bool isRedo, Action<Transform> callback)
        {
            Transform transform = (Transform)record.Target;
            if (!transform)
            {
                return false;
            }

            TransformState state = (TransformState)record.NewState;
            if (state.applyOnRedo != isRedo)
            {
                return false;
            }
            bool hasChanged = transform.position != state.position ||
                              transform.rotation != state.rotation ||
                              transform.localScale != state.scale;

            bool trsOnly = state.siblingIndex == -1;
            if (!trsOnly)
            {
                int siblingIndex = transform.GetSiblingIndex();
                hasChanged = hasChanged || transform.parent != state.parent || siblingIndex != state.siblingIndex;
            }

            if (hasChanged)
            {
                //Transform prevParent = transform.parent;
                if (!trsOnly)
                {
                    transform.SetParent(state.parent, true);
                    transform.SetSiblingIndex(state.siblingIndex);
                }

                transform.position = state.position;
                transform.rotation = state.rotation;
                transform.localScale = state.scale;
            }

            callback?.Invoke(transform);
            return hasChanged;
        }

        private class RectTransformState
        {
            private Vector2 anchorMin;
            private Vector2 anchorMax;
            private Vector2 anchoredPosition;
            private Vector2 pivot;
            private Vector2 sizeDelta;
            private Vector2 offsetMin;
            private Vector2 offsetMax;

            public RectTransformState(RectTransform rt)
            {
                anchorMin = rt.anchorMin;
                anchorMax = rt.anchorMax;
                anchoredPosition = rt.anchoredPosition;
                pivot = rt.pivot;
                sizeDelta = rt.sizeDelta;
                offsetMin = rt.offsetMin;
                offsetMax = rt.offsetMax;
            }

            public void WriteTo(RectTransform rt)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.anchoredPosition = anchoredPosition;
                rt.pivot = pivot;
                rt.sizeDelta = sizeDelta;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
            }
        }

        //TODO: Make it in more generic/extendable way.
        private object OnBeforeAddComponent(GameObject obj, Type componentType)
        {
            if (componentType.IsSubclassOf(typeof(LayoutGroup)))
            {
                RectTransformState[] state = new RectTransformState[obj.transform.childCount];
                int index = 0;
                foreach (RectTransform rt in obj.transform)
                {
                    state[index] = new RectTransformState(rt);
                    index++;
                }
                return state;
            }
            return null;
        }

        private object OnAfterAddComponentUndo(GameObject obj, Type componentType, object gameObjectState)
        {
            if (componentType.IsSubclassOf(typeof(LayoutGroup)))
            {
                RectTransformState[] state = (RectTransformState[])gameObjectState;
                int index = 0;
                foreach (RectTransform rt in obj.transform)
                {
                    state[index].WriteTo(rt);
                    index++;
                }
                return state;
            }
            return null;
        }
        

        #region Obsolete

        [Obsolete("Use void Select(IRuntimeSelection selection, UnityObject[] objects, UnityObject activeObject) instead")]
        public void Select(UnityObject[] objects, UnityObject activeObject)
        {
            Select(_rte.Selection, objects, activeObject);
        }

        #endregion
    }

    public class DisabledUndo : IRuntimeUndo
    {
        public bool Enabled { get { return false; } set { } }

        public bool CanUndo { get { return false; } }

        public bool CanRedo { get { return false; } }

        public bool IsRecording { get { return false; } }

        private void GetRidOfWarnings()
        {
            BeforeUndo();
            UndoCompleted();
            BeforeRedo();
            RedoCompleted();
            StateChanged();
        }

        public void BeginRecord()
        {

        }

        public void EndRecord()
        {

        }

        public void Redo()
        {

        }

        public void Undo()
        {

        }

        public void Purge()
        {

        }

        public void Erase(object oldRef, object newRef, bool ignoreLock)
        {

        }

        public void Store()
        {

        }

        public void Restore()
        {

        }

        public Record CreateRecord(UndoRedoCallback redoCallback, UndoRedoCallback undoCallback, PurgeCallback purgeCallback = null, EraseReferenceCallback eraseCallback = null)
        {
            return CreateRecord(null, null, null, redoCallback, undoCallback, purgeCallback, eraseCallback);
        }

        public Record CreateRecord(object target, object newState, object oldState, UndoRedoCallback redoCallback, UndoRedoCallback undoCallback, PurgeCallback purgeCallback = null, EraseReferenceCallback eraseCallback = null)
        {
            return null;
        }


        public void EraseFromSelection(UnityObject[] objects)
        {
        }

        [Obsolete]
        public void Select(UnityObject[] objects, UnityObject activeObject)
        {

        }

        public void RegisterCreatedObjects(ExposeToEditor[] createdObjects, Action afterRedo = null, Action afterUndo = null)
        {

        }

        public void DestroyObjects(ExposeToEditor[] destoryedObjects, Action afterRedo = null, Action afterUndo = null)
        {
            for (int i = 0; i < destoryedObjects.Length; ++i)
            {
                ExposeToEditor exposed = destoryedObjects[i];

                if (exposed == null || exposed.CanDelete)
                {
                    UnityObject.Destroy(exposed.gameObject);
                }
            }
        }

        public void RecordValue(object target, MemberInfo memberInfo, Action afterRedo, Action afterUndo)
        {

        }

        public void RecordValue(object target, object accessor, MemberInfo memberInfo, Action afterRedo, Action afterUndo)
        {

        }

        public void BeginRecordValue(object target, MemberInfo memberInfo)
        {

        }

        public void BeginRecordValue(object target, object accessor, MemberInfo memberInfo)
        {

        }

        public void EndRecordValue(object target, MemberInfo memberInfo, Action afterRedo, Action afterUndo)
        {

        }

        public void EndRecordValue(object target, object accessor, MemberInfo memberInfo, Action<object, object> targetErased, Action afterRedo, Action afterUndo)
        {

        }

        public void BeginRecordTransform(Transform target, Action<Transform> afterUndo = null)
        {

        }

        public void EndRecordTransform(Transform target, Action<Transform> afterRedo = null)
        {

        }

        public void BeginRecordTransform(Transform target, Transform parent, int siblingIndex = -1, Action<Transform> afterUndo = null)
        {

        }

        public void EndRecordTransform(Transform target, Transform parent, int siblingIndex = -1, Action<Transform> afterRedo = null)
        {

        }
        public void Select(IMESelection selection, UnityObject[] objects, UnityObject activeObject)
        {

        }
        public event RuntimeUndoEventHandler BeforeUndo;
        public event RuntimeUndoEventHandler UndoCompleted;
        public event RuntimeUndoEventHandler BeforeRedo;
        public event RuntimeUndoEventHandler RedoCompleted;
        public event RuntimeUndoEventHandler StateChanged;


    }
}