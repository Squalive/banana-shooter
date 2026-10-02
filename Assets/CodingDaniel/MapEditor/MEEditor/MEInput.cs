using System;
using System.Collections.Generic;
using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.UI;
using CodingDaniel.MapEditor.UI.AddObject;
using Manager;
using UnityEngine;
using UnityEngine.Rendering.Universal;

using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.MEEditor
{
    public class MEInput : MonoBehaviour
    {
        public static MEInput Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
        }

        private MESelectionComponent _component;

        private void Start()
        {
            _component = MESelectionComponent.Instance;
        }

        private bool _isPointerPressed,_isPointerReleased;

        /// <summary>Reused so the per-frame hover pick allocates nothing.</summary>
        private readonly List<GameObject> _hoverBuffer = new List<GameObject>(1);

        private void Update()
        {
            if (TabHolder.Instance.UsingUI() || MEBase.Instance.IsPlayMode)
            {
                ClearHoveredObject();
                return;
            }

            UpdateHoveredObject();
            
            if (Input.GetMouseButtonDown(0))
            {
                PressPointer();
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                ReleasePointer();
            }

            if (SelectAction())
            {
                SelectGameObject();
            }

            if (Input.GetKeyDown(KeyCode.Delete) && ProBuilderTool.Instance.GetEditor() == null)
            {
                
                foreach (var go in _component.Selection.GameObjects)
                {
                   
                    if (go != null && go.GetComponent<ExposeToEditor>().CanDelete)
                    {
                    
                        Destroy(go);
                    }
                }
                _component.TryToClearSelection();
                
            }
            else if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
            {
                List<GameObject> objs = new List<GameObject>();
                if (_component.Selection.GameObjects == null) return;
                foreach (var go in _component.Selection.GameObjects)
                {
                    for (int i = 0; i < MapSaver.Instance.ObjectsNeedToSave.Count; i++)
                    {
                        var saveObject = MapSaver.Instance.ObjectsNeedToSave[i];
                        if (saveObject.gameObject == go)
                        {
                            bool external = saveObject.objectItem == null;
                            var type = external ? saveObject.type : saveObject.objectItem.type;
                            ExposeToEditor expose;
                            switch (type)
                            {
                                case ObjectType.Mesh:
                                    PBMesh pbMesh = saveObject.GetComponent<PBMesh>();
                                    var transform1 = saveObject.transform;
                                    MapData.MeshObjectData meshObjectData = new MapData.MeshObjectData(saveObject.objectItem.ObjectName,transform1.position,
                                        transform1.rotation, transform1.localScale,pbMesh.Positions,pbMesh.Faces,pbMesh.Textures,pbMesh.GetMaterialIndex());
                                    
                                    GameObject obj = MapSaver.Instance.GenerateMesh(meshObjectData);
                                    
                                    obj.transform.SetParent(MEBase.Instance.EditedObject.transform);

                                    obj.AddComponent<ExposeToEditor>();
                                    obj.AddComponent<PBMesh>();
                                    obj.AddComponent<MapSaveObject>().Init(saveObject.objectItem);
                
                                    MapSaver.Instance.loadedObject.Add(obj);
                                    
                                    objs.Add(obj);
                                    break;
                                case ObjectType.Decal:
                                    var ogDecal = go.GetComponent<DecalProjector>();
                                    expose = AddObjectMenu.CreateDecalExposeToEditor(ogDecal.material);

                                    DecalProjector decalProjector = expose.GetComponent<DecalProjector>();

                                    decalProjector.size = ogDecal.size;

                                    InitializeObject(expose.gameObject, saveObject,external,type);
                                        
                                    objs.Add(expose.gameObject);
                                    break;
                                case ObjectType.Light:
                                    var ogLight = go.GetComponent<Light>();
                                    ExposeToEditor l = AddObjectMenu.CreateLightExposeToEditor(ogLight.type);
                                    Light li = l.GetComponent<Light>();

                                    InitializeObject(l.gameObject, saveObject,external,type);

                                    li.enabled = ogLight.enabled;
                                    li.intensity = ogLight.intensity;
                                    li.range = ogLight.range;
                                    li.spotAngle = ogLight.spotAngle;
                                    li.innerSpotAngle = ogLight.innerSpotAngle;
                                    li.color = ogLight.color;
                                    
                                    objs.Add(l.gameObject);
                                    break;
                                case ObjectType.PlayerSpawnPoint:
                                    ExposeToEditor p = AddObjectMenu.CreatePlayerSpawnPointExposeToEditor();
                
                                    InitializeObject(p.gameObject, saveObject,external,type);
                                    break;
                                case ObjectType.Decoration:
                                    ObjectItem item = saveObject.objectItem;
                                    MEDecoration decoration = go.GetComponent<MEDecoration>();
                                    Transform t = go.transform;
                                    if (external)
                                    {
                                        expose = AddObjectMenu.CreateDecorationExposeToEditor(
                                            (GameObject)AssetBundleManager.Instance.GetAssetObject(saveObject.n),MEBase.Instance.EditedObject.transform,t.position,t.rotation,t.localScale, (int)decoration.type, decoration.enableCollision,true);
                                    }
                                    else
                                    {
                                        expose = AddObjectMenu.CreateDecorationExposeToEditor(item.prefab,MEBase.Instance.EditedObject.transform,t.position,t.rotation,t.localScale, (int)decoration.type, decoration.enableCollision,true); 
                                    }

                                    GameObject o= expose.gameObject;
                                    InitializeObject(o, saveObject,external,type);

                                    objs.Add(o);
                                    break;
                                case ObjectType.AudioSource:
                                    var audioSource = go.GetComponent<AudioSource>();
                                    expose = AddExternalObjectMenu.CreateAudioSourceExposeToEditor(audioSource.clip);

                                    AudioSource source = go.GetComponent<AudioSource>();

                                    source.enabled = audioSource.enabled;
                                    source.loop = audioSource.loop;
                                    source.maxDistance = audioSource.maxDistance;
                                    source.minDistance = audioSource.minDistance;
                                    source.spatialBlend = audioSource.spatialBlend;
                                    source.dopplerLevel = audioSource.dopplerLevel;

                                    InitializeObject(expose.gameObject, saveObject,external,type);
                                    
                                    objs.Add(expose.gameObject);
                                    break;
                            }

                            break;
                        }
                    }
                }

                _component.TryToClearSelection();
                _component.Selection.Objects = objs.ToArray();
            }
        }

        void InitializeObject(GameObject og, MapSaveObject target, bool external=false, ObjectType type=ObjectType.None)
        {
            Transform transform1 = og.transform;
            var transform2 = target.transform;
            transform1.position = transform2.position;
            transform1.rotation = transform2.rotation;
            transform1.localScale = transform2.localScale;
            
            if(external)
                og.AddComponent<MapSaveObject>().Init(type, target.n);
            else
                og.AddComponent<MapSaveObject>().Init(target.objectItem);
            
            MapSaver.Instance.loadedObject.Add(og);
        }

        public void SelectGameObject()
        {
            EditorToolState tools = MEBase.Instance.Tools;
            IMESelection selection = _component.Selection;

            if (tools.ActiveTool != null && tools.ActiveTool !=_component.BoxSelection)
            {
                return;
            }
            
            if (tools.IsViewing)
            {
                return;
            }

            if (!selection.Enabled)
            {
                return;
            }
            
            OnSelectGO();
        }
        protected virtual void OnSelectGO()
        {
            _component.SelectGO(Input.GetKey(KeyCode.LeftControl), false);
        }

        #region Hover outline

        private ExposeToEditor _hoveredObject;

        /// <summary>
        /// The object the pointer is currently over, or null. Exposed for anything that wants to react
        /// to the same pick the outline uses.
        /// </summary>
        public ExposeToEditor HoveredObject
        {
            get { return _hoveredObject; }
        }

        /// <summary>
        /// Outlines whatever the pointer is over. Deliberately mirrors what a left click would select:
        /// the nearest pickable collider resolved up to its ExposeToEditor, so a decoration or a mesh
        /// outlines as a whole rather than per sub-object.
        /// </summary>
        private void UpdateHoveredObject()
        {
            if (OutlineManager.Instance == null || _component == null)
            {
                return;
            }

            if (MEBase.Instance.Tools.IsViewing || IsEditingWithTool() || MEBase.Instance.Pointer == null)
            {
                ClearHoveredObject();
                return;
            }

            Physics.Raycast(MEBase.Instance.Pointer, out RaycastHit hit, float.MaxValue);

            ExposeToEditor hovered = null;
            if (hit.collider != null)
            {
                ExposeToEditor exposed = hit.collider.GetComponentInParent<ExposeToEditor>();
                if (exposed != null && !exposed.MarkAsDestroyed)
                {
                    hovered = exposed;
                }
            }

            // A selected object is already outlined, and two halos on the same silhouette read as one
            // mushy glow. The selection wins; hovering something else still outlines it.
            if (hovered != null && _component.Selection.IsSelected(hovered.gameObject))
            {
                hovered = null;
            }

            if (hovered == _hoveredObject)
            {
                return;
            }

            _hoveredObject = hovered;

            if (hovered == null)
            {
                OutlineManager.Instance.ClearHoveredObjects();
                return;
            }

            _hoverBuffer.Clear();
            _hoverBuffer.Add(hovered.gameObject);
            OutlineManager.Instance.SetHoveredObjects(_hoverBuffer);
        }

        /// <summary>
        /// True while a handle or box-selection drag owns the pointer, in which case the object under
        /// the cursor is not what the user is aiming at.
        /// </summary>
        private bool IsEditingWithTool()
        {
            EditorToolState tools = MEBase.Instance.Tools;
            return tools.ActiveTool != null && tools.ActiveTool != _component.BoxSelection;
        }

        private void ClearHoveredObject()
        {
            if (_hoveredObject == null)
            {
                return;
            }

            _hoveredObject = null;

            if (OutlineManager.Instance != null)
            {
                OutlineManager.Instance.ClearHoveredObjects();
            }
        }

        private void OnDestroy()
        {
            ClearHoveredObject();
        }

        #endregion

        public virtual void SelectAll()
        {
            _component.SelectAll();
        }
        public bool SelectAction()
        {
            bool select = _isPointerPressed && _isPointerReleased;
            if (select)
            {
                _isPointerPressed = false;
                _isPointerReleased = false;
            }
            return select;
        }

        void PressPointer()
        {
            _isPointerPressed = true;
        }
        
        void ReleasePointer()
        {
            _isPointerReleased = true;
        }
    }
}
