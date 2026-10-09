
using System.Collections.Generic;

using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI.AddObject
{
    public class AddObjectMenu : MonoBehaviour
    {
        public static AddObjectMenu Instance { private set; get; }

        [SerializeField] private GameObject window;

        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;

        [SerializeField] private List<float> offsets = new List<float>();
        [SerializeField] private List<Toggle> toggles = new List<Toggle>();

        [SerializeField] private Button createBtn;

        private IME _me;
        private IMESelectionComponent _selectionComponent;
        [SerializeField] private ObjectItemUI prefab;

        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform maskRect;

        [SerializeField] private Transform meshItemsParent, decorationItemsParent, lightItemsParent, decalItemsParent, specialItemsParent;

        [SerializeField] private List<MeshObjectItem> meshItems = new List<MeshObjectItem>();
        [SerializeField] private List<ObjectItem> decorationItems = new List<ObjectItem>();
        [SerializeField] private List<LightObjectItem> lightItems = new List<LightObjectItem>();
        [SerializeField] private List<DecalObjectItem> decalItems = new List<DecalObjectItem>();
        [SerializeField] private List<ObjectItem> specialItems = new List<ObjectItem>();

        [SerializeField] private Mesh capsuleMesh;
        private Material _playerSpawnPointMaterial;

        [Header("Add External Objects Buttns")]
        [SerializeField] private Transform[] staysTopObjects;

        private void Awake()
        {
            Instance = this;

            _playerSpawnPointMaterial = Resources.Load<Material>("player_spawn_point");

            _me = MEBase.Instance;
            _selectionComponent = MESelectionComponent.Instance;
            createBtn.interactable = false;

            foreach (var item in meshItems)
            {
                ObjectItemUI ui = Instantiate(prefab, meshItemsParent);

                ui.Init(item.type, item.ObjectName, item.icon, 0, rootCanvas, maskRect, true, item);
            }

            foreach (var item in decorationItems)
            {
                ObjectItemUI ui = Instantiate(prefab, decorationItemsParent);

                ui.Init(item.type, item.ObjectName, item.icon, 0, rootCanvas, maskRect, true, item);
            }

            foreach (var item in lightItems)
            {
                ObjectItemUI ui = Instantiate(prefab, lightItemsParent);

                ui.Init(item.type, item.ObjectName, item.icon, 0, rootCanvas, maskRect, true, item);
            }

            foreach (var item in decalItems)
            {
                ObjectItemUI ui = Instantiate(prefab, decalItemsParent);

                ui.Init(item.type, item.ObjectName, item.icon, 0, rootCanvas, maskRect, true, item);
            }

            foreach (var item in specialItems)
            {
                ObjectItemUI ui = Instantiate(prefab, specialItemsParent);

                ui.Init(item.type, item.ObjectName, item.icon, 0, rootCanvas, maskRect, true, item);
            }

            foreach (var t in staysTopObjects)
            {
                t.SetAsLastSibling();
            }

            scroll.onValueChanged.AddListener(OnScrollChanged);
            createBtn.onClick.AddListener(Create);
        }

        private void OnDestroy()
        {
            scroll.onValueChanged.RemoveListener(OnScrollChanged);
            createBtn.onClick.RemoveListener(Create);
        }



        public void MoveTo(int index)
        {
            content.anchoredPosition = new Vector2(0, offsets[index]);
        }
        private void OnScrollChanged(Vector2 arg0)
        {
            float y = scroll.content.anchoredPosition.y;
            if (Mathf.Abs(y) < 0.1f) y = 0;
            for (int i = 0; i < offsets.Count; i++)
            {
                if (y <= offsets[i])
                {
                    toggles[i].SetIsOnWithoutNotify(true);
                    break;
                }
            }

            AddExternalObjectMenu.Instance.CloseEditBar();
        }


        public void OpenWindow()
        {
            // _selectedItem = null;
            window.SetActive(true);
        }

        private ObjectItem _selectedItem = null;

        public ObjectItem SelectedItem
        {
            get => _selectedItem;
            set => _selectedItem = value;
        }
        public void SelectItem(ObjectItem item)
        {
            AddExternalObjectMenu.Instance.CloseEditBar();
            _selectedItem = item;

            createBtn.interactable = true;

            AddExternalObjectMenu.Instance.SelectItem = null;
        }

        void Create()
        {
            if (_selectedItem != null)
            {
                ExposeToEditor exposeToEditor;
                IMESelectionComponent selectionComponent = _selectionComponent;
                switch (_selectedItem.type)
                {
                    case ObjectType.Mesh:
                        ProBuilderTool.Instance.CreateNewShapeAndRecord((int)((MeshObjectItem)_selectedItem).shapeType, _selectedItem);
                        break;
                    case ObjectType.Light:
                        exposeToEditor = CreateLightExposeToEditor(((LightObjectItem)(_selectedItem)).lightType);

                        exposeToEditor.transform.SetParent(_me.EditedObject.transform);

                        exposeToEditor.gameObject.AddComponent<MapSaveObject>().Init(_selectedItem);

                        _me.Undo.BeginRecord();
                        if (selectionComponent == null || selectionComponent.CanSelect)
                        {
                            _me.Selection.ActiveGameObject = exposeToEditor.gameObject;
                        }

                        _me.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
                        _me.Undo.EndRecord();
                        break;
                    case ObjectType.PlayerSpawnPoint:
                        exposeToEditor = CreatePlayerSpawnPointExposeToEditor();

                        exposeToEditor.transform.SetParent(_me.EditedObject.transform);

                        exposeToEditor.gameObject.AddComponent<MapSaveObject>().Init(_selectedItem);


                        _me.Undo.BeginRecord();
                        if (selectionComponent == null || selectionComponent.CanSelect)
                        {
                            _me.Selection.ActiveGameObject = exposeToEditor.gameObject;
                        }

                        _me.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
                        _me.Undo.EndRecord();
                        break;
                    case ObjectType.Decal:
                        exposeToEditor = CreateDecalExposeToEditor(((DecalObjectItem)_selectedItem).material);

                        exposeToEditor.transform.SetParent(_me.EditedObject.transform);

                        exposeToEditor.gameObject.AddComponent<MapSaveObject>().Init(_selectedItem);


                        _me.Undo.BeginRecord();
                        if (selectionComponent == null || selectionComponent.CanSelect)
                        {
                            _me.Selection.ActiveGameObject = exposeToEditor.gameObject;
                        }

                        _me.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
                        _me.Undo.EndRecord();
                        break;
                    case ObjectType.Decoration:
                        Transform t = _selectedItem.prefab.transform;
                        exposeToEditor = CreateDecorationExposeToEditor(_selectedItem.prefab, _me.EditedObject.transform, t.position, t.rotation, t.localScale,
                           (int)((DecorationObjectItem)_selectedItem).eDecorationType, true, true);

                        exposeToEditor.gameObject.AddComponent<MapSaveObject>().Init(_selectedItem);

                        _me.Undo.BeginRecord();
                        if (selectionComponent == null || selectionComponent.CanSelect)
                        {
                            _me.Selection.ActiveGameObject = exposeToEditor.gameObject;
                        }

                        _me.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
                        _me.Undo.EndRecord();
                        break;
                }
            }

            window.SetActive(false);
        }

        #region Light

        public static ExposeToEditor CreateLightExposeToEditor(LightType lightType)
        {
            ExposeToEditor r = CreateLight(lightType).AddComponent<ExposeToEditor>();

            r.transform.position = GetPosition();

            return r;
        }

        public static GameObject CreateLight(LightType lightType)
        {
            GameObject obj = new GameObject();
            Light l = obj.AddComponent<Light>();

            l.type = lightType;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.9f;
            l.color = Color.white;


            return obj;
        }

        #endregion

        #region Special

        public static ExposeToEditor CreatePlayerSpawnPointExposeToEditor()
        {
            ExposeToEditor r = CreatePlayerSpawnPoint().AddComponent<ExposeToEditor>();

            r.transform.position = GetPosition();

            return r;
        }
        public static GameObject CreatePlayerSpawnPoint()
        {
            GameObject obj = new GameObject();

            obj.AddComponent<PlayerSpawnPoint>().Init(Instance.capsuleMesh, Instance._playerSpawnPointMaterial);

            return obj;
        }


        #endregion

        #region Decal

        public static GameObject CreateDecal(Material material)
        {
            GameObject obj = new GameObject();

            obj.name = "decal ";
            DecalProjector decalProjector = obj.AddComponent<DecalProjector>();

            decalProjector.material = material;

            return obj;
        }

        public static ExposeToEditor CreateDecalExposeToEditor(Material material)
        {
            var r = CreateDecal(material).AddComponent<ExposeToEditor>();
            r.transform.position = GetPosition();
            return r;
        }

        #endregion

        #region Decoration

        public static ExposeToEditor CreateDecorationExposeToEditor(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, int type, bool enableCollision, bool create = false, bool editor = true)
        {
            if (create)
                pos = GetPosition();
            var r = CreateDecoration(prefab, parent, pos, rot, scale, type, enableCollision, editor).AddComponent<ExposeToEditor>();
            return r;
        }

        public static GameObject CreateDecoration(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, int type, bool enableCollision, bool editor = false)
        {
            GameObject go = Instantiate(prefab, parent);

            Transform t = go.transform;

            t.position = pos;
            t.rotation = rot;
            t.localScale = scale;

            var decoration = go.AddComponent<MEDecoration>();

            go.tag = "MECollide";

            decoration.enableCollision = enableCollision;
            decoration.type = (MEDecoration.EDecorationType)type;

            if (!editor)
            {
                Collider col = go.GetComponentInChildren<Collider>();
                if (col != null)
                {
                    switch (decoration.type)
                    {
                        case MEDecoration.EDecorationType.Obstacle:
                            SetupColliders(go, decoration.enableCollision, LayerMask.NameToLayer("Ground"));
                            break;
                        case MEDecoration.EDecorationType.GrapplePoint:
                            SetupColliders(go, decoration.enableCollision, LayerMask.NameToLayer("Grapple"));
                            break;
                        case MEDecoration.EDecorationType.Water:
                            col.gameObject.AddComponent<Water>();
                            col.gameObject.layer = LayerMask.NameToLayer("Water");
                            col.isTrigger = true;

                            if (col is BoxCollider waterCol)
                            {
                                GameObject water = new GameObject();

                                Transform waterTransform = water.transform;

                                waterTransform.position = go.transform.position;
                                waterTransform.rotation = go.transform.rotation;
                                waterTransform.localScale = go.transform.localScale;
                                waterTransform.parent = parent;

                                BoxCollider newProcessingTrigger = water.AddComponent<BoxCollider>();

                                newProcessingTrigger.size = waterCol.size;
                                newProcessingTrigger.center = waterCol.center;
                                newProcessingTrigger.isTrigger = true;

                                Volume volume = water.AddComponent<Volume>();

                                volume.isGlobal = false;
                                volume.weight = 1f;
                                volume.blendDistance = 1f;
                                volume.sharedProfile = PrefabManager.Instance.waterVolume;
                            }

                            break;
                    }
                }
            }

            return go;
        }

        static void SetupColliders(GameObject go, bool enabled, int layer)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>())
            {
                col.enabled = enabled;
                col.gameObject.layer = layer;
            }
        }

        #endregion

        public static Vector3 GetPosition()
        {
            Vector3 position = Vector3.zero;
            if (TabHolder.Instance != null)
            {
                Transform camera = MEBase.Instance.Camera.transform;

                position = camera.position + camera.forward * 5f;
            }

            return position;
        }
    }
}
