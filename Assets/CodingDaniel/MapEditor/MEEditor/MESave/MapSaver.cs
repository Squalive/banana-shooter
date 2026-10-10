using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.UI;
using CodingDaniel.MapEditor.UI.AddObject;
using CodingDaniel.SaveAudio;
using Extensions;
using Manager;
using Newtonsoft.Json;
using Save;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering.Universal;
using Utils;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace CodingDaniel.MapEditor.MEEditor.MESave
{
    [Serializable]
    public class MapData
    {
        [Serializable]
        public class ObjectData
        {
            public string name;
            public MyVector3 position;
            public Quaternion rotation;
            public MyVector3 scale;

            public ObjectData(string n, Vector3 pos, Quaternion rot, Vector3 scale)
            {
                name = n;
                position = pos.ToMyVector3();
                rotation = rot;
                this.scale = new MyVector3(scale);
            }

            public ObjectData()
            {

            }
        }

        [Serializable]
        public class MeshObjectData
        {
            public ObjectData data;
            public List<MyVector3> positions;
            public List<PBFace> faces;
            public List<Vector2> textures;
            public List<MaterialIndex> materialIndexes;

            [Serializable]
            public class MaterialIndex
            {
                public bool isExternal;
                public int index;

                public MaterialIndex(bool isExternal, int index)
                {
                    this.isExternal = isExternal;
                    this.index = index;
                }

                public MaterialIndex()
                {

                }
            }

            public MeshObjectData(string n, Vector3 pos, Quaternion rot, Vector3 scale, IList<Vector3> positions, IList<PBFace> faces, IList<Vector2> textures, List<MaterialIndex> materialIndexes)
            {
                data = new ObjectData(n, pos, rot, scale);
                this.positions = positions.ToList().ToMyVector3List();
                this.faces = faces.ToList();
                this.textures = textures.ToList();
                this.materialIndexes = materialIndexes;
            }

            public MeshObjectData()
            {

            }
        }
        [Serializable]
        public class LightObjectData
        {
            public ObjectData data;
            public LightType type;
            public float intensity;
            public float range;
            public bool enable;
            public float spotAngle;
            public float innerSpotAngle;
            public MyColor color = new MyColor(Color.white);

            public LightObjectData(string n, Vector3 pos, Quaternion rot, Vector3 scale, LightType type, float intensity, float range, bool enable, float spotAngle, float innerSpotAngle, Color color)
            {
                data = new ObjectData(n, pos, rot, scale);
                this.type = type;
                this.intensity = intensity;
                this.range = range;
                this.enable = enable;
                this.spotAngle = spotAngle;
                this.innerSpotAngle = innerSpotAngle;
                this.color = new MyColor(color);
            }

            public LightObjectData()
            {

            }
        }
        [Serializable]
        public class MyColor
        {
            public float r, g, b;

            public MyColor(Color color)
            {
                r = color.r;
                g = color.g;
                b = color.b;
            }

            public MyColor()
            {

            }

            public Color ToColor()
            {
                return new Color(r, g, b);
            }
        }

        [Serializable]
        public class ExternalMaterial
        {
            public string name;

            public float smoothness = .5f;
            public float metallic = 0f;
            public float bumpScale = 1f;
            public float parallax = 0.005f;

            public ExternalMaterial(string name, float smoothness, float metallic, float bumpScale, float parallax)
            {
                this.name = name;
                this.smoothness = smoothness;
                this.metallic = metallic;
                this.bumpScale = bumpScale;
                this.parallax = parallax;
            }

            public ExternalMaterial()
            {

            }
        }

        [Serializable]
        public class DecalObjectData
        {
            public ObjectData data;
            public int materialIndex = -1;
            public MyVector3 size;
            public bool external = false;

            public DecalObjectData(string n, Vector3 pos, Quaternion rot, Vector3 scale, int materialIndex, Vector3 size, bool external)
            {
                data = new ObjectData(n, pos, rot, scale);
                this.materialIndex = materialIndex;
                this.size = new MyVector3(size);
                this.external = external;
            }

            public DecalObjectData()
            {

            }
        }

        [Serializable]
        public class ExternalData
        {
            public string name;

            public ExternalData(string name)
            {
                this.name = name;
            }
        }

        [Serializable]
        public class ExternalModelConfig
        {
            public string name = String.Empty;

            [JsonIgnore]
            public Texture2D previewImage = new(512, 512, TextureFormat.RGB24, false);

            [JsonIgnore] public AssetBundleContainer AssetBundle;
        }

        [Serializable]
        public class DecorationObjectData
        {
            public ObjectData data;
            /// <summary>
            /// 0 -- Obstacle
            /// 1 -- Water
            /// </summary>
            public int type = 0;
            public bool enableCollision = true;
            public bool external = false;

            public DecorationObjectData(string n, Vector3 pos, Quaternion rot, Vector3 scale, int type, bool enableCollider, bool external)
            {
                data = new ObjectData(n, pos, rot, scale);
                this.enableCollision = enableCollider;
                this.external = external;
                this.type = type;
            }

            public DecorationObjectData()
            {

            }
        }

        [Serializable]
        public class AudioObjectData
        {
            public ObjectData data;

            public int index = -1;

            public bool enabled = true;
            public bool loop = true;
            public float maxDistance = 500;
            public float minDistance = 1;
            public float spatialBlend = 1;
            public float dopplerLevel = 1;

            public AudioObjectData(string n, Vector3 pos, Quaternion rot, Vector3 scale, int index, bool enabled, bool loop, float maxDistance, float minDistance, float spatialBlend, float dopplerLevel)
            {
                data = new ObjectData(n, pos, rot, scale);
                this.index = index;
                this.enabled = enabled;
                this.loop = loop;
                this.maxDistance = maxDistance;
                this.minDistance = minDistance;
                this.spatialBlend = spatialBlend;
                this.dopplerLevel = dopplerLevel;
            }

            public AudioObjectData()
            {

            }
        }


        public string name;
        public string description;
        public bool isPublished = false;
        public PublishedFileId_t fileId;
        public string saveKey;

        [JsonIgnore]
        public string path = "";
        public string version;

        public int skyboxIndex;
        public bool useDirectionalLight;
        public MyColor ambientColor;

        public MyVector3 bound = new MyVector3(20, 10, 20);
        public MyVector3 boundCenter = new MyVector3(0, 0, 0);

        public List<MeshObjectData> meshDatas = new List<MeshObjectData>();
        public List<LightObjectData> lightDatas = new List<LightObjectData>();
        public List<MyVector3> spawnPos = new List<MyVector3>();
        public List<ExternalMaterial> externalMaterials = new List<ExternalMaterial>();
        public List<DecalObjectData> decalDatas = new List<DecalObjectData>();
        public List<ExternalData> externalDecalDatas = new List<ExternalData>();
        public List<DecorationObjectData> decorationDatas = new List<DecorationObjectData>();

        public List<ExternalData> externalAudioDatas = new List<ExternalData>();
        public List<AudioObjectData> audioDatas = new List<AudioObjectData>();


        public MapData(string name, string description, string v, string saveKey, int skyboxIndex, bool useDirectionalLight, MyColor ambientColor,
            List<MapSaveObject> objectsNeedToSave, List<Tuple<string, string, string, Material>> externalMat, List<Tuple<string, Material>> externalDecalMats, List<Tuple<string, AudioClip>> externalAudioClips,
            bool saved, string defaultPath, Vector3 bound, Vector3 boundCenter,
            bool isPublished = false, PublishedFileId_t fileId = new())
        {
            this.name = name;
            this.bound = new MyVector3(bound);
            this.boundCenter = new MyVector3(boundCenter);
            this.path = defaultPath;
            this.description = description;
            version = v;
            this.saveKey = saveKey;
            this.skyboxIndex = skyboxIndex;
            this.useDirectionalLight = useDirectionalLight;
            this.ambientColor = ambientColor;

            this.isPublished = isPublished;
            this.fileId = fileId;

            foreach (var saveObject in objectsNeedToSave)
            {
                var transform = saveObject.transform;
                if (saveObject.objectItem != null)
                {
                    switch (saveObject.objectItem.type)
                    {
                        case ObjectType.Mesh:
                            PBMesh pbMesh = saveObject.GetComponent<PBMesh>();
                            // Debug.Log(pbMesh.Positions.Length);
                            MeshObjectData meshObjectData = new MeshObjectData(saveObject.objectItem.ObjectName, transform.position,
                                transform.rotation, transform.localScale, pbMesh.Positions, pbMesh.Faces, pbMesh.Textures, pbMesh.GetMaterialIndex());

                            meshDatas.Add(meshObjectData);
                            break;
                        case ObjectType.Light:
                            Light light = saveObject.GetComponent<Light>();
                            LightObjectData lightObjectData = new LightObjectData(saveObject.objectItem.ObjectName,
                                transform.position,
                                transform.rotation, transform.localScale, light.type, light.intensity, light.range,
                                light.enabled, light.spotAngle, light.innerSpotAngle, light.color);

                            lightDatas.Add(lightObjectData);
                            break;
                        case ObjectType.PlayerSpawnPoint:
                            spawnPos.Add(new MyVector3(saveObject.transform.position));
                            break;
                        case ObjectType.Decal:
                            DecalProjector decalProjector = saveObject.GetComponent<DecalProjector>();

                            DecalObjectData decalObjectData =
                                new DecalObjectData(saveObject.objectItem.ObjectName, transform.position,
                                    transform.rotation, transform.localScale,
                                    MapSaver.Instance.GetDecalMaterialIndex(decalProjector.material, false),
                                    decalProjector.size, false);

                            decalDatas.Add(decalObjectData);
                            break;
                        case ObjectType.Decoration:
                            MEDecoration collider = saveObject.GetComponent<MEDecoration>();

                            bool enableCollision = collider.enableCollision;

                            DecorationObjectData decorationObjectData = new DecorationObjectData(
                                saveObject.objectItem.ObjectName, transform.position,
                                transform.rotation, transform.localScale, (int)collider.type, enableCollision, false);

                            decorationDatas.Add(decorationObjectData);
                            break;
                    }
                }
                else
                {
                    switch (saveObject.type)
                    {
                        case ObjectType.Decal:
                            DecalProjector decalProjector = saveObject.GetComponent<DecalProjector>();

                            DecalObjectData decalObjectData =
                                new DecalObjectData(saveObject.n, transform.position,
                                    transform.rotation, transform.localScale,
                                    MapSaver.Instance.GetDecalMaterialIndex(decalProjector.material, true),
                                    decalProjector.size, true);

                            decalDatas.Add(decalObjectData);
                            break;

                        case ObjectType.AudioSource:
                            AudioSource audioSource = saveObject.GetComponent<AudioSource>();

                            AudioObjectData audioObjectData = new AudioObjectData(saveObject.n, transform.position,
                                transform.rotation, transform.localScale,
                                MapSaver.Instance.GetAudioIndex(audioSource.clip), audioSource.enabled,
                                audioSource.loop, audioSource.maxDistance, audioSource.minDistance,
                                audioSource.spatialBlend, audioSource.dopplerLevel);

                            audioDatas.Add(audioObjectData);
                            break;
                        case ObjectType.Decoration:
                            MEDecoration collider = saveObject.GetComponent<MEDecoration>();

                            bool enableCollision = collider.enableCollision;

                            DecorationObjectData decorationObjectData = new DecorationObjectData(
                                saveObject.n, transform.position,
                                transform.rotation, transform.localScale, (int)collider.type, enableCollision, true);

                            decorationDatas.Add(decorationObjectData);
                            break;
                    }
                }
            }

            #region Texture

            string p = GetTexturePath();

            if (saved)
            {
                if (!Directory.Exists(p))
                {
                    if (externalMat.Count > 0)
                        Directory.CreateDirectory(p);
                }
                else
                {
                    Directory.Delete(p, true);
                    if (externalMat.Count > 0)
                        Directory.CreateDirectory(p);
                }
            }

            foreach (var tuple in externalMat)
            {
                var external = tuple.Item4;
                ExternalMaterial externalMaterial = new ExternalMaterial(external.name, external.GetFloat(MapSaver.Smoothness), external.GetFloat(MapSaver.Metallic),
                    external.GetFloat(MapSaver.BumpScale), external.GetFloat(MapSaver.Parallax));

                if (saved)
                {
                    string basePath = p + "/" + external.name + "_base.png";
                    if (!File.Exists(basePath) || tuple.Item1 != basePath)
                    {
                        Texture2D baseTexture = (Texture2D)external.mainTexture;

                        if (baseTexture != null)
                        {
                            byte[] bytes = baseTexture.EncodeToPNG();

                            SaveSystem.WriteToFileAsyncThread(basePath, bytes, () => { });
                        }
                    }

                    string heightPath = p + "/" + external.name + "_height.png";
                    if (!File.Exists(heightPath) || tuple.Item2 != heightPath)
                    {
                        Texture2D heightTexture = (Texture2D)external.GetTexture(MapSaver.ParallaxMap);
                        if (heightTexture != null)
                        {
                            byte[] bytes = heightTexture.EncodeToPNG();

                            SaveSystem.WriteToFileAsyncThread(heightPath, bytes, () => { });
                        }
                    }

                    string normalPath = p + "/" + external.name + "_normal.png";

                    if (!File.Exists(normalPath) || tuple.Item3 != normalPath)
                    {
                        Texture2D normalTexture = (Texture2D)external.GetTexture(MapSaver.BumpMap);

                        if (normalTexture != null)
                        {
                            byte[] bytes = normalTexture.EncodeToPNG();

                            SaveSystem.WriteToFileAsyncThread(normalPath, bytes, () => { });
                        }
                    }
                }


                externalMaterials.Add(externalMaterial);
            }

            #endregion

            #region Decal

            p = GetDecalTexturePath();

            if (saved)
            {
                if (!Directory.Exists(p))
                {
                    if (externalDecalMats.Count > 0)
                        Directory.CreateDirectory(p);
                }
                else
                {
                    Directory.Delete(p, true);
                    if (externalDecalMats.Count > 0)
                        Directory.CreateDirectory(p);
                }
            }

            foreach (var tuple in externalDecalMats)
            {
                var externalDecalMat = tuple.Item2;
                ExternalData externalDecalData = new ExternalData(externalDecalMat.name);

                if (saved)
                {
                    string basePath = p + "/" + externalDecalMat.name + "_base.png";
                    if (!File.Exists(basePath) || basePath != tuple.Item1)
                    {
                        Texture2D baseTexture = (Texture2D)externalDecalMat.GetTexture(MapSaver.BaseMap);

                        if (baseTexture != null)
                        {
                            byte[] bytes = baseTexture.EncodeToPNG();

                            SaveSystem.WriteToFileAsyncThread(basePath, bytes, () =>
                            {
                            });
                        }

                    }
                }

                externalDecalDatas.Add(externalDecalData);
            }

            #endregion

            #region Audio

            p = GetAudioPath();

            if (saved)
            {
                if (!Directory.Exists(p))
                {
                    if (externalAudioClips.Count > 0)
                        Directory.CreateDirectory(p);
                }
                else
                {
                    Directory.Delete(p, true);
                    if (externalAudioClips.Count > 0)
                        Directory.CreateDirectory(p);
                }
            }

            foreach (var tuple in externalAudioClips)
            {
                var audioClip = tuple.Item2;
                ExternalData externalData = new ExternalData(audioClip.name);
                if (saved)
                {
                    string basePath = p + "/" + audioClip.name + tuple.Item1.Substring(tuple.Item1.Length - 4).ToLowerInvariant();
                    Debug.Log(basePath);
                    if (!File.Exists(basePath) || basePath != tuple.Item1)
                    {
                        if (File.Exists(tuple.Item1))
                            File.Copy(tuple.Item1, basePath);
                    }
                }
                externalAudioDatas.Add(externalData);
            }

            p = GetModelPath();

            if (saved)
            {
                if (!Directory.Exists(p))
                {
                    Directory.CreateDirectory(p);
                }
            }

            #endregion

        }

        public string GetTexturePath()
        {
            return path + GetNameString() + "_texture";
        }
        public string GetDecalTexturePath()
        {
            return path + GetNameString() + "_decal_texture";
        }
        public string GetAudioPath()
        {
            return path + GetNameString() + "_audio";
        }

        public string GetModelPath()
        {
            return path + GetNameString() + "_model";
        }

        public string GetNameString()
        {
            return name + saveKey;
        }
        public MapData()
        {

        }
    }


    public class MapMetadata
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("SaveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("IsPublished")]
        public bool IsPublished { get; set; }

        [JsonProperty("FileId")]
        public PublishedFileId_t FileId { get; set; }

        public string GetBasePath()
        {
            return MapSaver.path + Name + SaveKey;
        }

        public string GetTexturePath()
        {
            return GetBasePath() + "_texture";
        }

        public MapMetadata(string name, string saveKey, string description, bool isPublished, PublishedFileId_t fileId)
        {
            Name = name;
            SaveKey = saveKey;
            Description = description;
            IsPublished = isPublished;
            FileId = fileId;
        }

        public MapMetadata()
        {

        }
    }

    [DefaultExecutionOrder(-99)]
    public class MapSaver : MonoBehaviour
    {
        private static MapSaver _instance;
        public static MapSaver Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                {
                    _instance = value;
                    DontDestroyOnLoad(value.gameObject);
                }
                else if (_instance != value)
                {
                    Debug.Log($"{nameof(MapSaver)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        public static bool Initialized { private set; get; } = false;

        public static readonly int ParallaxMap = Shader.PropertyToID("_ParallaxMap");
        public static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        public static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        public static readonly int Metallic = Shader.PropertyToID("_Metallic");
        public static readonly int BumpScale = Shader.PropertyToID("_BumpScale");
        public static readonly int Parallax = Shader.PropertyToID("_Parallax");
        public static readonly int BaseMap = Shader.PropertyToID("Base_Map");
        public List<MapSaveObject> ObjectsNeedToSave { get; } = new List<MapSaveObject>();

        public List<Material> materials = new List<Material>();
        public List<Tuple<string, string, string, Material>> externalMaterials = new();
        public List<Material> skyboxes = new List<Material>();

        public List<Material> decalMaterials = new List<Material>();
        public List<Tuple<string, Material>> externalDecalMaterials = new();

        public List<Tuple<string, AudioClip>> externalAudioClips = new();

        public Dictionary<string, MapData.ExternalModelConfig> ModelConfigs = new();

        public List<ObjectItem> objectItems = new List<ObjectItem>();

        [SerializeField] private PhysicMaterial groundMat;

        public List<GameObject> loadedObject = new List<GameObject>();
        public List<GameObject> playModeLoadedObject = new List<GameObject>();

        public static MapData CurrentMap;

        public static FileInfo CurrentMapFile = null;

        public List<FileInfo> EditingMaps = new();
        public static Dictionary<FileInfo, MapMetadata> EditingMapMetadatas = new Dictionary<FileInfo, MapMetadata>();

        public static Dictionary<PublishedFileId_t, FileInfo> WorkshopMaps = new Dictionary<PublishedFileId_t, FileInfo>();

        public Material decalDummyMat;
        private void Awake()
        {
            Instance = this;
        }

        public static string SteamTemp;
        private IEnumerator Start()
        {
            path = Application.dataPath + "/maps/";
            SteamTemp = Application.dataPath + "/SteamWorkshopTemp/";


            if (Directory.Exists(path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                FileInfo[] info = directoryInfo.GetFiles("*.bsm");
                // Preload.IncreaseTotalStep(info.Length);

                foreach (var f in info)
                {
                    EditingMaps.Add(f);

                    string n = f.FullName.Substring(0, f.FullName.Length - 4) + ".metadata";

                    if (File.Exists(n))
                    {
                        CoroutineWithData cd = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(n));

                        yield return cd.coroutine;

                        string content = cd.result.ToString();
                        if (string.IsNullOrEmpty(content) || content == "{}" || (!content.Contains("{") || !content.Contains("}")))
                        {
                            continue;
                        }
                        MapMetadata data = JsonConvert.DeserializeObject<MapMetadata>(content);

                        if (data != null)
                        {
                            EditingMapMetadatas.Add(f, data);
                        }
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(path);
            }



            if (!Directory.Exists(SteamTemp))
            {
                Directory.CreateDirectory(SteamTemp);
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(SteamTemp);
                FileInfo[] info = directoryInfo.GetFiles("*.*");

                foreach (var file in info)
                {
                    file.Delete();
                }
            }

            Initialized = true;
        }

        public bool LoadWorkshopMap(string p, PublishedFileId_t fileId)
        {
            if (WorkshopMaps.ContainsKey(fileId)) return true;
            p += "/";
            if (Directory.Exists(p))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(p);
                FileInfo[] info = directoryInfo.GetFiles("*.bsm");

                foreach (var f in info)
                {
                    WorkshopMaps[fileId] = f;
                    return true;
                }
            }

            return false;
        }

        public int GetDecalMaterialIndex(Material material, bool external)
        {
            if (external)
            {
                for (int i = 0; i < externalDecalMaterials.Count; i++)
                {
                    if (material.GetInstanceID() == externalDecalMaterials[i].Item2.GetInstanceID())
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = 0; i < decalMaterials.Count; i++)
                {
                    if (material.GetInstanceID() == decalMaterials[i].GetInstanceID())
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static string path = "";

        // [SerializeField]
        // private string saved;

        public static readonly string Characters = "akshdkashkngsjahgyoqopweiup12903890124snka";

        public bool IsSaving
        {
            private set;
            get;
        }


        public bool Save(string mapName, string description, Texture2D texture2D, bool saveAs)
        {
            PublishMenu.Instance.nameInput.SetTextWithoutNotify(mapName);
            PublishMenu.Instance.descriptionInput.SetTextWithoutNotify(description);
            if (string.IsNullOrEmpty(mapName))
            {
                return false;
            }

            IsSaving = true;

            EditorMenu.Instance.SavingMenu.SetActive(true);

            try
            {
                SetMapData(mapName, description, saveAs, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to prepare map {mapName} for saving: {e}");
                IsSaving = false;
                EditorMenu.Instance.SavingMenu.SetActive(false);
                return false;
            }

            MapMetadata md = new MapMetadata(mapName, CurrentMap.saveKey, description, CurrentMap.isPublished,
                CurrentMap.fileId);

            SaveToFile(texture2D, md);

            return true;
        }

        public void SetMapData(string mapName, string description, bool saveAs, bool save)
        {
            bool isPublished = false;
            PublishedFileId_t fileId = new PublishedFileId_t();
            if (CurrentMap != null)
            {
                isPublished = CurrentMap.isPublished;

                if (isPublished)
                {
                    fileId = CurrentMap.fileId;
                }
            }
            else
            {
                saveAs = true;
            }

            StringBuilder stringBuilder = saveAs ? new StringBuilder() : new StringBuilder(CurrentMap.saveKey);

            if (saveAs)
            {
                for (int i = 0; i < 15; i++)
                {
                    stringBuilder.Append(Characters[Random.Range(0, Characters.Length)]);
                }
            }

            int skyboxIndex = -1;
            if (RenderSettings.skybox != null)
            {
                for (int i = 0; i < skyboxes.Count; i++)
                {
                    if (skyboxes[i] == RenderSettings.skybox)
                    {
                        skyboxIndex = i;
                        break;
                    }
                }
            }

            Bounds bounds = MapBoundVisual.Instance.Bounds;

            CurrentMap = new MapData(mapName, description, "0.1", stringBuilder.ToString(), skyboxIndex,
                MapSettingMenu.Instance.GetDirectionalLightEnable(), new MapData.MyColor(RenderSettings.ambientLight),
                ObjectsNeedToSave, externalMaterials, externalDecalMaterials, externalAudioClips, save, path, bounds.extents * 2f, bounds.center, isPublished, fileId);
        }

        async void SaveToFile(Texture2D texture2D, MapMetadata metadata)
        {
            float time = Time.time;

            try
            {
                string map = await Task.Run(() => JsonConvert.SerializeObject(CurrentMap, Formatting.None,
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    }));

                byte[] previewImg = texture2D.EncodeToJPG();

                await SaveSystem.WriteToFileAsyncThread(path + CurrentMap.GetNameString() + ".bsm", map);

                await SaveSystem.WriteToFileAsyncThread(path + CurrentMap.GetNameString() + ".jpg", previewImg);

                string md = await Task.Run(() => JsonConvert.SerializeObject(metadata, Formatting.None,
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    }));

                await SaveSystem.WriteToFileAsyncThread(path + CurrentMap.GetNameString() + ".metadata", md);

                Debug.Log("save complete in: " + (Time.time - time));
                MEBase.Instance.HasChanged = false;

                CurrentMapFile ??= new FileInfo(path + CurrentMap.GetNameString() + ".bsm");

                if (!EditingMaps.Contains(CurrentMapFile))
                {
                    EditingMaps.Add(CurrentMapFile);
                }

                EditingMapMetadatas[CurrentMapFile] = metadata;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save map {CurrentMap?.name}: {e}");
            }
            finally
            {
                IsSaving = false;
                EditorMenu.Instance.SavingMenu.SetActive(false);
            }
        }

        public async void SaveBsmFileOnly()
        {
            try
            {
                string map = JsonConvert.SerializeObject(CurrentMap, Formatting.None, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                });

                MapMetadata metadata = new MapMetadata(CurrentMap.name, CurrentMap.saveKey, CurrentMap.description,
                    CurrentMap.isPublished, CurrentMap.fileId);

                float time = await SaveSystem.WriteToFileAsyncThread(path + CurrentMap.GetNameString() + ".bsm", map);

                string md = JsonConvert.SerializeObject(metadata, Formatting.None, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                });

                time += await SaveSystem.WriteToFileAsyncThread(path + CurrentMap.GetNameString() + ".metadata", md);

                CurrentMapFile ??= new FileInfo(path + CurrentMap.GetNameString() + ".bsm");

                Debug.Log("save complete in: " + time);

                if (!EditingMaps.Contains(CurrentMapFile))
                {
                    EditingMaps.Add(CurrentMapFile);
                }

                EditingMapMetadatas[CurrentMapFile] = metadata;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save map {CurrentMap?.name}: {e}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        public Material dummyMat;
        async Task InitializeLoad(bool loadExternal, bool workshopMap)
        {
            var data = CurrentMap;
            #region Envirnment

            RenderSettings.skybox = data.skyboxIndex == -1 ? null : skyboxes[data.skyboxIndex];
            RenderSettings.ambientLight = data.ambientColor.ToColor();

            if (workshopMap)
            {
                MEMap.Instance.GetDirectionalLight().enabled = data.useDirectionalLight;
            }
            else
            {
                MapSettingMenu.Instance.skyboxImg.texture = RenderSettings.skybox == null ? null : SourceUI.GetSkyboxTexture(
                    RenderSettings.skybox);

                MapSettingMenu.Instance.skyboxText.SetText(RenderSettings.skybox == null ? "None" : RenderSettings.skybox.name);

                MapSettingMenu.Instance.enableDirectionalLighting.SetIsOnWithoutNotify(data.useDirectionalLight);
                MapSettingMenu.Instance.skyboxColorImg.color = data.ambientColor.ToColor();

                MapSettingMenu.Instance.GetDirectionalLight().enabled = data.useDirectionalLight;

                MapSettingMenu.Instance.SetBound(data.bound.ToVector3(), data.boundCenter.ToVector3());
            }


            #endregion

            if (loadExternal)
            {
                AssetBundleManager.Instance.UnloadAllBundles();
                externalMaterials.Clear();
                externalDecalMaterials.Clear();
                externalAudioClips.Clear();
                ModelConfigs.Clear();

                foreach (var externalMaterial in data.externalMaterials)
                {
                    Material material = new Material(dummyMat);

                    material.EnableKeyword("_METALLICGLOSSMAP");
                    material.EnableKeyword("_PARALLAXMAP");

                    material.SetFloat(Smoothness, externalMaterial.smoothness);
                    material.SetFloat(Metallic, externalMaterial.metallic);

                    string p = data.GetTexturePath();
                    string n = externalMaterial.name;

                    material.name = n;

                    string baseMap = $"{p}/{n}_base.png";
                    if (File.Exists(baseMap))
                    {
                        Texture2D texture2D = new Texture2D(512, 512, TextureFormat.RGB24, false);
                        byte[] bytes = await SaveSystem.ReadByteFromFileAsync(baseMap);
                        texture2D.LoadImage(bytes);

                        material.mainTexture = texture2D;
                    }

                    string heightMap = $"{p}/{n}_height.png";
                    if (File.Exists(heightMap))
                    {
                        Texture2D texture2D = new Texture2D(512, 512, TextureFormat.RGB24, false);
                        byte[] bytes = await SaveSystem.ReadByteFromFileAsync(heightMap);
                        texture2D.LoadImage(bytes);

                        material.SetTexture(ParallaxMap, texture2D);

                        material.SetFloat(Parallax, externalMaterial.parallax);
                    }
                    else material.SetTexture(ParallaxMap, null);

                    string normalMap = $"{p}/{n}_normal.png";
                    if (File.Exists(normalMap))
                    {
                        Texture2D texture2D = new Texture2D(512, 512, TextureFormat.RGB24, false);
                        byte[] bytes = await SaveSystem.ReadByteFromFileAsync(normalMap);
                        texture2D.LoadImage(bytes);

                        material.SetTexture(BumpMap, texture2D);
                        material.EnableKeyword("_NORMALMAP");


                        material.SetFloat(BumpScale, externalMaterial.bumpScale);
                    }
                    else material.SetTexture(BumpMap, null);

                    Tuple<string, string, string, Material> tuple = new Tuple<string, string, string, Material>(baseMap, heightMap, normalMap, material);
                    externalMaterials.Add(tuple);
                }

                foreach (var decalData in data.externalDecalDatas)
                {
                    Material material = new Material(decalDummyMat);

                    string p = data.GetDecalTexturePath();
                    string n = decalData.name;

                    material.name = n;

                    string baseMap = $"{p}/{n}_base.png";
                    if (File.Exists(baseMap))
                    {
                        Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
                        byte[] bytes = await SaveSystem.ReadByteFromFileAsync(baseMap);
                        texture2D.LoadImage(bytes);

                        material.SetTexture(BaseMap, texture2D);
                    }

                    externalDecalMaterials.Add(new Tuple<string, Material>(baseMap, material));
                }

                foreach (var audioData in data.externalAudioDatas)
                {
                    string p = data.GetAudioPath();
                    string n = audioData.name;
                    Debug.Log(n);

                    string audioPath = FindFile(p, $"{n}.wav");
                    // Debug.Log(audioPath + "Path exist: " + File.Exists(audioPath));
                    if (File.Exists(audioPath))
                    {
                        AudioClip clip = await LoadAudio(audioPath);
                        clip.name = n;

                        externalAudioClips.Add(new Tuple<string, AudioClip>(audioPath, clip));
                        continue;
                    }

                    audioPath = FindFile(p, $"{n}.mp3");
                    // Debug.Log(audioPath + "Path exist: " + File.Exists(audioPath));
                    if (File.Exists(audioPath))
                    {
                        AudioClip clip = await LoadAudio(audioPath);
                        clip.name = n;

                        externalAudioClips.Add(new Tuple<string, AudioClip>(audioPath, clip));
                    }
                }

                if (Directory.Exists(data.GetModelPath()))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(data.GetModelPath());

                    var infos = directoryInfo.GetDirectories();

                    foreach (var info in infos)
                    {
                        string configPath = Path.Combine(info.FullName, "config.json");
                        string previewImage = Path.Combine(info.FullName, "preview.png");
                        string sourcePath = Path.Combine(info.FullName, "source.unity3d");
                        if (File.Exists(configPath))
                        {
                            MapData.ExternalModelConfig config =
                                JsonConvert.DeserializeObject<MapData.ExternalModelConfig>(
                                    SaveSystem.ReadFileNormally(configPath));

                            if (File.Exists(previewImage))
                                config.previewImage.LoadImage(await SaveSystem.ReadByteFromFileAsync(previewImage));

                            if (File.Exists(sourcePath))
                            {
                                await TryToDownloadAssetBundle(config, sourcePath);
                            }
                            ModelConfigs.Add(config.name, config);
                        }
                    }
                }
            }
        }

        async Task TryToDownloadAssetBundle(MapData.ExternalModelConfig bundle, string p)
        {
            AssetBundleContainer container = AssetBundleManager.Instance.GetAssetBundle(bundle.name);

            if (container == null)
            {
                var bundleRequest = AssetBundle.LoadFromFileAsync(p);

                while (!bundleRequest.isDone)
                {
                    await Task.Delay(10);
                }

                var loadedAssetBundle = bundleRequest.assetBundle;

                if (loadedAssetBundle == null)
                {
                    Debug.Log($"Failed to load {bundle.name} AssetBundle!");
                    return;
                }

                var assetRequest = loadedAssetBundle.LoadAssetAsync<GameObject>("model");

                while (!assetRequest.isDone)
                {
                    await Task.Delay(10);
                }

                if (assetRequest.asset is not GameObject go)
                {
                    Debug.Log($"Failed to load {bundle.name} Asset!");
                    return;
                }

                Sanitize(go);

                bundle.AssetBundle = AssetBundleManager.Instance.AddBundle(bundle.name, bundleRequest.assetBundle, assetRequest.asset);
            }
        }

        static readonly HashSet<Type> Allowed = new()
        {
            typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SkinnedMeshRenderer),
            typeof(BoxCollider), typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider),
            typeof(LODGroup), typeof(TerrainCollider),
        };

        static void Sanitize(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;      // cannot remove Transform
                if (!Allowed.Contains(c.GetType()))
                {
                    // Debug.LogWarning($"[Workshop] stripping {c.GetType().FullName}");
                    DestroyImmediate(c, true);                            // immediate, on the asset
                }
            }
        }

        public void Cleanup()
        {
            foreach (var external in externalMaterials)
            {
                Destroy(external.Item4);
            }

            foreach (var external in externalAudioClips)
            {
                Destroy(external.Item2);
            }

            foreach (var external in externalDecalMaterials)
            {
                Destroy(external.Item2);
            }

            foreach (var obj in loadedObject)
            {
                Destroy(obj);
            }

            foreach (var obj in playModeLoadedObject)
            {
                Destroy(obj);
            }

            externalMaterials.Clear();
            externalAudioClips.Clear();
            externalDecalMaterials.Clear();
            loadedObject.Clear();
            playModeLoadedObject.Clear();

            AssetBundleManager.Instance.UnloadAllBundles();
        }

        static string FindFile(string folder, string fileName)
        {
            string exact = Path.Combine(folder, fileName);
            if (File.Exists(exact) || !Directory.Exists(folder)) return exact;

            foreach (var file in Directory.GetFiles(folder))
            {
                if (string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase))
                    return file;
            }

            return exact;
        }

        async Task<AudioClip> LoadAudio(string p)
        {
            if (string.IsNullOrEmpty(p))
            {
                return null;
            }
            var audioType = p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? AudioType.WAV : AudioType.MPEG;
            using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(SaveSystem.ToFileUri(p), audioType))
            {
                // download the audio data using DownloadHandlerAudioClip
                DownloadHandlerAudioClip handler = new DownloadHandlerAudioClip(webRequest.url, audioType);
                webRequest.downloadHandler = handler;

                var q = webRequest.SendWebRequest();

                while (!q.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }

                return handler.audioClip;
            }
        }

        public Action OnMapLoaded;

        public IEnumerator LoadEditorMap()
        {
            if (CurrentMapFile == null)
            {
                CurrentMap = null;

                externalMaterials.Clear();
                externalDecalMaterials.Clear();
                externalAudioClips.Clear();
                yield break;
            }
            MEBase.Instance.HasChanged = false;

            CoroutineWithData cd = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(CurrentMapFile.FullName));
            yield return cd.coroutine;
            string content = cd.result.ToString();
            if (string.IsNullOrEmpty(content) || content == "{}" || (!content.Contains("{") || !content.Contains("}")))
            {
                CurrentMapFile = null;
                CurrentMap = null;

                externalMaterials.Clear();
                externalDecalMaterials.Clear();
                externalAudioClips.Clear();

                yield break;
            }
            MapData data = JsonConvert.DeserializeObject<MapData>(content);

            if (data == null)
            {
                yield break;
            }
            data.path = path;

            CurrentMap = data;

            #region Setting

            TabHolder.Instance.mapNameInput.SetTextWithoutNotify(data.name);
            TabHolder.Instance.mapDescriptionInput.SetTextWithoutNotify(data.description);

            PublishMenu.Instance.descriptionInput.SetTextWithoutNotify(data.description);
            PublishMenu.Instance.nameInput.SetTextWithoutNotify(data.name);

            #endregion

            var task = InitializeLoad(true, false);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            OnMapLoaded?.Invoke();

            // MaterialPaletteUI.Instance.InitExternalMat();

            #region Object

            foreach (var game in loadedObject)
            {
                Destroy(game);
            }
            loadedObject.Clear();


            foreach (var objectData in data.meshDatas)
            {
                GameObject go = GenerateMesh(objectData);

                go.transform.SetParent(MEBase.Instance.EditedObject.transform);

                go.AddComponent<ExposeToEditor>();
                go.AddComponent<PBMesh>();
                go.AddComponent<MapSaveObject>().Init(GetObjectItem(objectData.data.name));

                loadedObject.Add(go);
            }

            foreach (var lightData in data.lightDatas)
            {
                ExposeToEditor l = AddObjectMenu.CreateLightExposeToEditor(lightData.type);
                Light li = l.GetComponent<Light>();

                GameObject o;
                (o = l.gameObject).AddComponent<MapSaveObject>().Init(GetObjectItem(lightData.data.name));

                o.transform.SetParent(MEBase.Instance.EditedObject.transform);
                o.name = lightData.data.name;
                o.transform.position = lightData.data.position.ToVector3();
                o.transform.rotation = lightData.data.rotation;
                o.transform.localScale = lightData.data.scale.ToVector3();

                li.enabled = lightData.enable;
                li.intensity = lightData.intensity;
                li.range = lightData.range;
                li.spotAngle = lightData.spotAngle;
                li.innerSpotAngle = lightData.innerSpotAngle;
                li.color = lightData.color.ToColor();

                loadedObject.Add(o);
            }

            foreach (var pos in data.spawnPos)
            {
                ExposeToEditor p = AddObjectMenu.CreatePlayerSpawnPointExposeToEditor();

                p.gameObject.AddComponent<MapSaveObject>().Init(playerSpawn);

                Transform transform1;
                (transform1 = p.transform).SetParent(MEBase.Instance.EditedObject.transform);
                transform1.position = pos.ToVector3();

                loadedObject.Add(p.gameObject);
            }

            foreach (var decalData in data.decalDatas)
            {
                ExposeToEditor go = AddObjectMenu.CreateDecalExposeToEditor(decalData.external ? externalDecalMaterials[decalData.materialIndex].Item2 : decalMaterials[decalData.materialIndex]);

                Transform transform1;
                (transform1 = go.transform).SetParent(MEBase.Instance.EditedObject.transform);
                transform1.position = decalData.data.position.ToVector3();
                transform1.rotation = decalData.data.rotation;
                transform1.localScale = decalData.data.scale.ToVector3();


                DecalProjector decalProjector = go.GetComponent<DecalProjector>();

                decalProjector.size = decalData.size.ToVector3();

                if (decalData.external)
                    go.gameObject.AddComponent<MapSaveObject>().Init(ObjectType.Decal, decalData.data.name);
                else
                    go.gameObject.AddComponent<MapSaveObject>().Init(GetObjectItem(decalData.data.name));

                loadedObject.Add(go.gameObject);
            }

            foreach (var decoration in data.decorationDatas)
            {
                ObjectItem item = GetObjectItem(decoration.data.name);
                var go = decoration.external ? AddObjectMenu.CreateDecorationExposeToEditor((GameObject)AssetBundleManager.Instance.GetAssetObject(decoration.data.name), MEBase.Instance.EditedObject.transform, decoration.data.position.ToVector3()
                    , decoration.data.rotation, decoration.data.scale.ToVector3(), decoration.type, decoration.enableCollision) : AddObjectMenu.CreateDecorationExposeToEditor(item.prefab, MEBase.Instance.EditedObject.transform, decoration.data.position.ToVector3(),
                    decoration.data.rotation, decoration.data.scale.ToVector3(), decoration.type, decoration.enableCollision);

                GameObject o = go.gameObject;
                if (decoration.external)
                {
                    o.AddComponent<MapSaveObject>().Init(ObjectType.Decoration, decoration.data.name);
                }
                else
                {
                    o.AddComponent<MapSaveObject>().Init(item);
                }

                loadedObject.Add(o);
            }

            foreach (var audioData in data.audioDatas)
            {
                if (externalAudioClips.Count > audioData.index)
                {
                    ExposeToEditor go = AddExternalObjectMenu.CreateAudioSourceExposeToEditor(externalAudioClips[audioData.index].Item2);

                    Transform transform1;
                    (transform1 = go.transform).SetParent(MEBase.Instance.EditedObject.transform);
                    transform1.position = audioData.data.position.ToVector3();
                    transform1.rotation = audioData.data.rotation;
                    transform1.localScale = audioData.data.scale.ToVector3();

                    AudioSource source = go.GetComponent<AudioSource>();

                    source.enabled = audioData.enabled;
                    source.loop = audioData.loop;
                    source.maxDistance = audioData.maxDistance;
                    source.minDistance = audioData.minDistance;
                    source.spatialBlend = audioData.spatialBlend;
                    source.dopplerLevel = audioData.dopplerLevel;

                    GameObject o;
                    (o = go.gameObject).AddComponent<MapSaveObject>().Init(ObjectType.AudioSource, audioData.data.name);

                    loadedObject.Add(o);
                }
            }
            #endregion

        }

        [SerializeField] public ObjectItem playerSpawn;
        public async void LoadPlayModeMap()
        {
            if (CurrentMap == null) return;
            MapData data = CurrentMap;


            await InitializeLoad(false, false);
            #region Object

            foreach (var game in playModeLoadedObject)
            {
                Destroy(game);
            }
            playModeLoadedObject.Clear();


            foreach (var objectData in data.meshDatas)
            {
                GameObject go = GenerateMesh(objectData);
                go.layer = LayerMask.NameToLayer("Ground");
                go.transform.SetParent(MEBase.Instance.PlayModeObject.transform);

                go.AddComponent<PBMesh>();

                playModeLoadedObject.Add(go);
            }

            foreach (var lightData in data.lightDatas)
            {
                GameObject l = AddObjectMenu.CreateLight(lightData.type);
                Light li = l.GetComponent<Light>();

                l.transform.SetParent(MEBase.Instance.PlayModeObject.transform);
                l.name = lightData.data.name;
                l.transform.position = lightData.data.position.ToVector3();
                l.transform.rotation = lightData.data.rotation;
                l.transform.localScale = lightData.data.scale.ToVector3();

                li.enabled = lightData.enable;
                li.intensity = lightData.intensity;
                li.range = lightData.range;
                li.spotAngle = lightData.spotAngle;
                li.innerSpotAngle = lightData.innerSpotAngle;
                li.color = lightData.color.ToColor();

                playModeLoadedObject.Add(l);
            }
            foreach (var decalData in data.decalDatas)
            {
                GameObject go = AddObjectMenu.CreateDecal(decalData.external ? externalDecalMaterials[decalData.materialIndex].Item2 : decalMaterials[decalData.materialIndex]);

                go.transform.SetParent(MEBase.Instance.PlayModeObject.transform);
                go.transform.position = decalData.data.position.ToVector3();
                go.transform.rotation = decalData.data.rotation;
                go.transform.localScale = decalData.data.scale.ToVector3();

                DecalProjector decalProjector = go.GetComponent<DecalProjector>();

                decalProjector.size = decalData.size.ToVector3();

                playModeLoadedObject.Add(go.gameObject);
            }

            foreach (var decoration in data.decorationDatas)
            {
                GameObject go;
                if (decoration.external)
                {
                    go = AddObjectMenu.CreateDecoration(
                        (GameObject)AssetBundleManager.Instance.GetAssetObject(decoration.data.name), MEBase.Instance.PlayModeObject.transform, decoration.data.position.ToVector3(), decoration.data.rotation
                        , decoration.data.scale.ToVector3(), decoration.type, decoration.enableCollision);
                }
                else
                {
                    ObjectItem item = GetObjectItem(decoration.data.name);
                    go = AddObjectMenu.CreateDecoration(item.prefab, MEBase.Instance.PlayModeObject.transform, decoration.data.position.ToVector3(), decoration.data.rotation, decoration.data.scale.ToVector3(),
                        decoration.type, decoration.enableCollision);
                }

                playModeLoadedObject.Add(go);
            }

            foreach (var audioData in data.audioDatas)
            {
                AudioSource source = AddExternalObjectMenu.CreateAudioSource(externalAudioClips[audioData.index].Item2);

                Transform transform1;
                (transform1 = source.transform).SetParent(MEBase.Instance.PlayModeObject.transform);
                transform1.position = audioData.data.position.ToVector3();
                transform1.rotation = audioData.data.rotation;
                transform1.localScale = audioData.data.scale.ToVector3();

                source.enabled = audioData.enabled;
                source.loop = audioData.loop;
                source.maxDistance = audioData.maxDistance;
                source.minDistance = audioData.minDistance;
                source.spatialBlend = audioData.spatialBlend;
                source.dopplerLevel = audioData.dopplerLevel;

                source.Play();

                playModeLoadedObject.Add(source.gameObject);
            }
            #endregion

        }

        public async Task<bool> LoadWorkshopMap(PublishedFileId_t fileIdT, MapData data)
        {
            if (WorkshopMaps.TryGetValue(fileIdT, out CurrentMapFile))
                Debug.Log("Find map " + data.name);
            else
            {
                Debug.Log("Cant Find map " + data.name);
                return false;
            }

            CurrentMap = data;

            await InitializeLoad(true, true);
            #region Object
            foreach (var objectData in CurrentMap.meshDatas)
            {
                GameObject go = GenerateMesh(objectData);
                go.isStatic = true;
                go.layer = LayerMask.NameToLayer("Ground");
                go.transform.SetParent(MEMap.Instance.GetGroundRoot());

                go.AddComponent<PBMesh>();
            }

            foreach (var lightData in CurrentMap.lightDatas)
            {
                GameObject l = AddObjectMenu.CreateLight(lightData.type);
                Light li = l.GetComponent<Light>();

                l.transform.SetParent(MEMap.Instance.GetLightRoot());
                l.name = lightData.data.name;
                l.transform.position = lightData.data.position.ToVector3();
                l.transform.rotation = lightData.data.rotation;
                l.transform.localScale = lightData.data.scale.ToVector3();

                li.enabled = lightData.enable;
                li.intensity = lightData.intensity;
                li.range = lightData.range;
                li.spotAngle = lightData.spotAngle;
                li.innerSpotAngle = lightData.innerSpotAngle;
                li.color = lightData.color.ToColor();
                l.isStatic = true;
            }
            foreach (var decalData in CurrentMap.decalDatas)
            {
                GameObject go = AddObjectMenu.CreateDecal(decalData.external ? externalDecalMaterials[decalData.materialIndex].Item2 : decalMaterials[decalData.materialIndex]);

                go.transform.SetParent(MEMap.Instance.GetGroundRoot());
                go.transform.position = decalData.data.position.ToVector3();
                go.transform.rotation = decalData.data.rotation;
                go.transform.localScale = decalData.data.scale.ToVector3();

                DecalProjector decalProjector = go.GetComponent<DecalProjector>();

                decalProjector.size = decalData.size.ToVector3();
                go.isStatic = true;
            }

            foreach (var decoration in CurrentMap.decorationDatas)
            {
                if (decoration.external)
                {
                    AddObjectMenu.CreateDecoration(
                        (GameObject)AssetBundleManager.Instance.GetAssetObject(decoration.data.name), MEMap.Instance.GetGroundRoot(), decoration.data.position.ToVector3(), decoration.data.rotation, decoration.data.scale.ToVector3(),
                        decoration.type, decoration.enableCollision);
                }
                else
                {
                    ObjectItem item = GetObjectItem(decoration.data.name);
                    AddObjectMenu.CreateDecoration(item.prefab, MEMap.Instance.GetGroundRoot(), decoration.data.position.ToVector3(), decoration.data.rotation, decoration.data.scale.ToVector3(),
                        decoration.type, decoration.enableCollision);
                }
            }
            foreach (var audioData in CurrentMap.audioDatas)
            {
                AudioSource source = AddExternalObjectMenu.CreateAudioSource(externalAudioClips[audioData.index].Item2);

                Transform transform1;
                (transform1 = source.transform).SetParent(MEMap.Instance.GetGroundRoot());
                transform1.position = audioData.data.position.ToVector3();
                transform1.rotation = audioData.data.rotation;
                transform1.localScale = audioData.data.scale.ToVector3();

                source.enabled = audioData.enabled;
                source.loop = audioData.loop;
                source.maxDistance = audioData.maxDistance;
                source.minDistance = audioData.minDistance;
                source.spatialBlend = audioData.spatialBlend;
                source.dopplerLevel = audioData.dopplerLevel;

                source.Play();
            }
            #endregion

            return true;
        }
        ObjectItem GetObjectItem(string n)
        {
            foreach (var item in objectItems)
            {
                if (item.name == n)
                {
                    return item;
                }
            }
            return null;
        }

        public GameObject GenerateMesh(MapData.MeshObjectData meshObjectData)
        {
            GameObject go = new GameObject(meshObjectData.data.name);
            go.transform.SetPositionAndRotation(meshObjectData.data.position.ToVector3(), meshObjectData.data.rotation);
            go.transform.localScale = meshObjectData.data.scale.ToVector3();

            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

            Material[] mat = new Material[meshObjectData.materialIndexes.Count];

            for (int i = 0; i < meshObjectData.materialIndexes.Count; i++)
            {
                var tuple = meshObjectData.materialIndexes[i];
                if (tuple.isExternal)
                {
                    mat[i] = externalMaterials[tuple.index].Item4;
                }
                else
                {
                    mat[i] = materials[tuple.index];
                }

            }

            meshRenderer.materials = mat;
            ProBuilderMesh mesh = go.AddComponent<ProBuilderMesh>();

            if (meshObjectData.positions != null)
            {
                Face[] faces = meshObjectData.faces.Select(f => f.ToFace()).ToArray();
                mesh.Rebuild(meshObjectData.positions.ToVector3List(), faces, meshObjectData.textures);

                IList<Face> actualFaces = mesh.faces;
                for (int i = 0; i < actualFaces.Count; ++i)
                {
                    actualFaces[i].submeshIndex = meshObjectData.faces[i].SubmeshIndex;
                }


                mesh.ToMesh();
                mesh.Refresh();

                // mesh.
            }

            MeshCollider meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            meshCollider.sharedMaterial = groundMat;

            return go;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (Instance == null)
            {
                Instantiate(Resources.Load<GameObject>("Map Saver"));
            }
        }

        public int GetAudioIndex(AudioClip clip)
        {
            for (int i = 0; i < externalAudioClips.Count; i++)
            {
                if (externalAudioClips[i].Item2 == clip) return i;
            }

            return -1;
        }

    }
}
