using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UObject = UnityEngine.Object;
namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public interface IPBSelectionPickerRenderer
    {
        Dictionary<ProBuilderMesh, HashSet<Face>> PickFacesInRect(
            Camera camera,
            Rect pickerRect,
            IList<ProBuilderMesh> selection,
            int renderTextureWidth = -1,
            int renderTextureHeight = -1);

        Dictionary<ProBuilderMesh, HashSet<int>> PickVerticesInRect(
            Camera camera,
            Rect pickerRect,
            IList<ProBuilderMesh> selection,
            bool doDepthTest,
            int renderTextureWidth = -1,
            int renderTextureHeight = -1);

        Dictionary<ProBuilderMesh, HashSet<Edge>> PickEdgesInRect(
            Camera camera,
            Rect pickerRect,
            IList<ProBuilderMesh> selection,
            bool doDepthTest,
            int renderTextureWidth = -1,
            int renderTextureHeight = -1);
    }
    public class PBSelectionPickerRenderer:IPBSelectionPickerRenderer
    {
        const string FacePickerOcclusionTintUniform = "_Tint";
        static readonly Color Blackf = new Color(0f, 0f, 0f, 1f);
        static readonly Color Whitef = new Color(1f, 1f, 1f, 1f);
        const uint PickerHashNone = 0x00;
        const uint PickerHashMin = 0x1;
        const uint PickerHashMax = 0x00FFFFFF;
        const uint MinEdgePixelsForValidSelection = 1;

        static bool _Initialized;
        static RenderTextureFormat renderTextureFormat
        {
            get
            {
                if (_Initialized)
                    return _RenderTextureFormat;

                _Initialized = true;

                for (int i = 0; i < _PreferredFormats.Length; i++)
                {
                    if (SystemInfo.SupportsRenderTextureFormat(_PreferredFormats[i]))
                    {
                        _RenderTextureFormat = _PreferredFormats[i];
                        break;
                    }
                }

                return _RenderTextureFormat;
            }
        }

        static TextureFormat textureFormat { get { return TextureFormat.ARGB32; } }

        static RenderTextureFormat _RenderTextureFormat = RenderTextureFormat.Default;

        static RenderTextureFormat[] _PreferredFormats = new RenderTextureFormat[]
        {
#if UNITY_5_6
            RenderTextureFormat.ARGBFloat,
            RenderTextureFormat.ARGB32,
#else
            RenderTextureFormat.ARGB32,
            RenderTextureFormat.ARGBFloat,
#endif
        };

        public virtual Dictionary<ProBuilderMesh, HashSet<Face>> PickFacesInRect(
            Camera camera,
            Rect pickerRect,
            IList<ProBuilderMesh> selection,
            int renderTextureWidth = -1,
            int renderTextureHeight = -1)
        {
            Dictionary<uint, SimpleTuple<ProBuilderMesh, Face>> map;
            Texture2D tex = RenderSelectionPickerTexture(camera, selection, out map, renderTextureWidth, renderTextureHeight);

            Color32[] pix = tex.GetPixels32();


#if PB_RENDER_PICKER_TEXTURE
            System.IO.File.WriteAllBytes("Assets/face_scene.png", tex.EncodeToPNG());
#endif
            pickerRect.width /= camera.rect.width;
            pickerRect.height /= camera.rect.height;
            pickerRect.x = (pickerRect.x - camera.pixelRect.x) / camera.rect.width;
            pickerRect.y = (pickerRect.y - (tex.height - (camera.pixelRect.y + camera.pixelRect.height))) / camera.rect.height;

            int ox = System.Math.Max(0, Mathf.FloorToInt(pickerRect.x));
            int oy = System.Math.Max(0, Mathf.FloorToInt((tex.height - pickerRect.y) - pickerRect.height));
            int imageWidth = tex.width;
            int imageHeight = tex.height;
            int width = Mathf.FloorToInt(pickerRect.width);
            int height = Mathf.FloorToInt(pickerRect.height);
            UObject.DestroyImmediate(tex);

            Dictionary<ProBuilderMesh, HashSet<Face>> selected = new Dictionary<ProBuilderMesh, HashSet<Face>>();
            SimpleTuple<ProBuilderMesh, Face> hit;
            HashSet<Face> faces = null;
            HashSet<uint> used = new HashSet<uint>();

#if PB_RENDER_PICKER_TEXTURE
            List<Color> rectImg = new List<Color>();
#endif

            for (int y = oy; y < System.Math.Min(oy + height, imageHeight); y++)
            {
                for (int x = ox; x < System.Math.Min(ox + width, imageWidth); x++)
                {
#if PB_RENDER_PICKER_TEXTURE
                    rectImg.Add(pix[y * imageWidth + x]);
#endif

                    uint v = DecodeRGBA(pix[y * imageWidth + x]);

                    if (used.Add(v) && map.TryGetValue(v, out hit))
                    {
                        if (selected.TryGetValue(hit.item1, out faces))
                            faces.Add(hit.item2);
                        else
                            selected.Add(hit.item1, new HashSet<Face>() { hit.item2 });
                    }
                }
            }

#if PB_RENDER_PICKER_TEXTURE
            if (width > 0 && height > 0)
            {
//              Debug.Log("used: \n" + used.Select(x => string.Format("{0} ({1})", x, EncodeRGBA(x))).ToString("\n"));
                Texture2D img = new Texture2D(width, height);
                img.SetPixels(rectImg.ToArray());
                img.Apply();
                byte[] bytes = img.EncodeToPNG();
                System.IO.File.WriteAllBytes("Assets/rect.png", bytes);
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
                UObject.DestroyImmediate(img);
            }
#endif

            return selected;
        }

        /// <summary>
        /// Decode Color32.RGB values to a 32 bit unsigned int, using the RGB as the little bytes. Discards the hi byte (alpha)
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static uint DecodeRGBA(Color32 color)
        {
            uint r = (uint)color.r;
            uint g = (uint)color.g;
            uint b = (uint)color.b;

            if (System.BitConverter.IsLittleEndian)
                return r << 16 | g << 8 | b;
            else
                return r << 24 | g << 16 | b << 8;
        }

        protected virtual Texture2D RenderSelectionPickerTexture(
            Camera camera,
            IList<ProBuilderMesh> selection,
            out Dictionary<uint, SimpleTuple<ProBuilderMesh, Face>> map,
            int width = -1,
            int height = -1)
        {
            var pickerObjects = GenerateFacePickingObjects(selection, out map);

            PBBuiltinMaterials.facePickerMaterial.SetColor(FacePickerOcclusionTintUniform, Whitef);

            Texture2D tex = RenderFacesWithRepacementShader(camera, PBBuiltinMaterials.selectionPickerShader, "ProBuilderPicker", width, height);

            foreach (GameObject go in pickerObjects)
            {
                UObject.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);
                UObject.DestroyImmediate(go);
            }

            return tex;
        }

        protected virtual GameObject[] GenerateFacePickingObjects(
          IList<ProBuilderMesh> selection,
          out Dictionary<uint, SimpleTuple<ProBuilderMesh, Face>> map)
        {
            int selectionCount = selection.Count;
            GameObject[] pickerObjects = new GameObject[selectionCount];
            map = new Dictionary<uint, SimpleTuple<ProBuilderMesh, Face>>();

            uint index = 0;

            for (int i = 0; i < selectionCount; i++)
            {
                var pb = selection[i];

                GameObject go = EmptyGameObjectWithTransform(pb.transform);
                go.name = pb.name + " (Face Depth Test)";

                Mesh m = new Mesh();
                m.vertices = pb.positions.ToArray();
                m.triangles = pb.faces.SelectMany(x => x.indexes).ToArray();
                Color32[] colors = new Color32[m.vertexCount];

                foreach (Face f in pb.faces)
                {
                    Color32 color = EncodeRGBA(index++);
                    map.Add(DecodeRGBA(color), new SimpleTuple<ProBuilderMesh, Face>(pb, f));

                    IList<int> distinctIndexes = f.distinctIndexes;
                    for (int n = 0; n < distinctIndexes.Count; n++)
                        colors[distinctIndexes[n]] = color;
                }

                m.colors32 = colors;

                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterial = PBBuiltinMaterials.facePickerMaterial;

                pickerObjects[i] = go;
            }

            return pickerObjects;
        }

        public static GameObject EmptyGameObjectWithTransform(Transform t)
        {
            GameObject go = new GameObject();
            go.transform.position = t.position;
            go.transform.localRotation = t.localRotation;
            go.transform.localScale = t.localScale;
            return go;
        }

        protected virtual Texture2D RenderFacesWithRepacementShader( 
            Camera camera,
            Shader shader,
            string tag,
            int width = -1,
            int height = -1)
        {
            return RenderWithReplacementShader(camera, shader, tag, width, height);
        }

        protected virtual Texture2D RenderVerticesWithRepacementShader(
            Camera camera,
            Shader shader,
            string tag,
            int width = -1,
            int height = -1)
        {
            return RenderWithReplacementShader(camera, shader, tag, width, height);
        }

        protected virtual Texture2D RenderEdgesWithRepacementShader(
          Camera camera,
          Shader shader,
          string tag,
          int width = -1,
          int height = -1)
        {
            return RenderWithReplacementShader(camera, shader, tag, width, height);
        }


        protected virtual Texture2D RenderWithReplacementShader(
           Camera camera,
           Shader shader,
           string tag,
           int width = -1,
           int height = -1)
        {
            bool autoSize = width < 0 || height < 0;

            int _width = autoSize ? (int)camera.pixelRect.width : width;
            int _height = autoSize ? (int)camera.pixelRect.height : height;

            GameObject go = new GameObject();
            Camera renderCam = go.AddComponent<Camera>();
            renderCam.CopyFrom(camera);
            renderCam.cullingMask = int.MaxValue;
         
            renderCam.renderingPath = RenderingPath.Forward;
            renderCam.enabled = false;
            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.backgroundColor = Color.white;
#if UNITY_5_6_OR_NEWER
            renderCam.allowHDR = false;
            renderCam.allowMSAA = false;
            renderCam.forceIntoRenderTexture = true;
#endif

#if UNITY_2017_1_OR_NEWER
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor()
            {
                width = _width,
                height = _height,
                colorFormat = renderTextureFormat,
                autoGenerateMips = false,
                depthBufferBits = 16,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                enableRandomWrite = false,
                memoryless = RenderTextureMemoryless.None,
                sRGB = false,
                useMipMap = false,
                volumeDepth = 1,
                msaaSamples = 1,
            };
            RenderTexture rt = RenderTexture.GetTemporary(descriptor);
#else
            RenderTexture rt = RenderTexture.GetTemporary(
                    _width,
                    _height,
                    16,
                    renderTextureFormat,
                    RenderTextureReadWrite.Linear,
                    1);
#endif

            RenderTexture prev = RenderTexture.active;
            renderCam.targetTexture = rt;
            RenderTexture.active = rt;

#if PB_DEBUG
            /* Debug.Log(string.Format("antiAliasing {0}\nautoGenerateMips {1}\ncolorBuffer {2}\ndepth {3}\ndepthBuffer {4}\ndimension {5}\nenableRandomWrite {6}\nformat {7}\nheight {8}\nmemorylessMode {9}\nsRGB {10}\nuseMipMap {11}\nvolumeDepth {12}\nwidth {13}",
                RenderTexture.active.antiAliasing,
                RenderTexture.active.autoGenerateMips,
                RenderTexture.active.colorBuffer,
                RenderTexture.active.depth,
                RenderTexture.active.depthBuffer,
                RenderTexture.active.dimension,
                RenderTexture.active.enableRandomWrite,
                RenderTexture.active.format,
                RenderTexture.active.height,
                RenderTexture.active.memorylessMode,
                RenderTexture.active.sRGB,
                RenderTexture.active.useMipMap,
                RenderTexture.active.volumeDepth,
                RenderTexture.active.width));
                */
#endif

            PrepareCamera(renderCam);
            Render(shader, tag, renderCam);

            Texture2D img = new Texture2D(_width, _height, textureFormat, false, false);
            img.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            img.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            
            UObject.DestroyImmediate(go);

            return img;
        }

        protected virtual void PrepareCamera(Camera renderCamera)
        {
            float aspect = renderCamera.aspect;
            renderCamera.rect = new Rect(Vector2.zero, Vector2.one);
            renderCamera.aspect = aspect;
        }

        protected virtual void Render(Shader shader, string tag, Camera renderCam)
        {
            renderCam.RenderWithShader(shader, tag);
        }

        public static Color32 EncodeRGBA(uint hash)
        {
            // skip using BitConverter.GetBytes since this is super simple
            // bit math, and allocating arrays for each conversion is expensive
            if (System.BitConverter.IsLittleEndian)
                return new Color32(
                    (byte)(hash >> 16 & 0xFF),
                    (byte)(hash >> 8 & 0xFF),
                    (byte)(hash & 0xFF),
                    (byte)(255));
            else
                return new Color32(
                    (byte)(hash >> 24 & 0xFF),
                    (byte)(hash >> 16 & 0xFF),
                    (byte)(hash >> 8 & 0xFF),
                    (byte)(255));
        }

        public const float FltCompareResolution = 1000f;

        static int HashFloat(float f)
        {
            ulong u = (ulong)(f * FltCompareResolution);
            return (int)(u % int.MaxValue);
        }

        public static int GetHashCode(Vector3 v)
        {
            // http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode/263416#263416
            int hash = 27;

            unchecked
            {
                hash = hash * 29 + HashFloat(v.x);
                hash = hash * 29 + HashFloat(v.y);
                hash = hash * 29 + HashFloat(v.z);
            }

            return hash;
        }

        /// <summary>
        /// Select vertex indexes contained within a rect.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="pickerRect"></param>
        /// <param name="selection"></param>
        /// <param name="doDepthTest"></param>
        /// <param name="renderTextureWidth"></param>
        /// <param name="renderTextureHeight"></param>
        /// <returns>A dictionary of pb_Object selected vertex indexes.</returns>
        public virtual Dictionary<ProBuilderMesh, HashSet<int>> PickVerticesInRect(
            Camera camera,
            Rect pickerRect,
            IList<ProBuilderMesh> selection,
            bool doDepthTest,
            int renderTextureWidth = -1,
            int renderTextureHeight = -1)
        {
            Dictionary<uint, SimpleTuple<ProBuilderMesh, int>> map;
            Dictionary<ProBuilderMesh, HashSet<int>> selected = new Dictionary<ProBuilderMesh, HashSet<int>>();

#if PB_RENDER_PICKER_TEXTURE
            List<Color> rectImg = new List<Color>();
#endif
            Texture2D tex = RenderSelectionPickerTexture(camera, selection, doDepthTest, out map, renderTextureWidth, renderTextureHeight);
            Color32[] pix = tex.GetPixels32();

#if PB_RENDER_PICKER_TEXTURE
            System.IO.File.WriteAllBytes("Assets/vertex_scene.png", tex.EncodeToPNG());
#endif
            pickerRect.width /= camera.rect.width;
            pickerRect.height /= camera.rect.height;
            pickerRect.x = (pickerRect.x - camera.pixelRect.x) / camera.rect.width;
            pickerRect.y = (pickerRect.y - (tex.height - (camera.pixelRect.y + camera.pixelRect.height))) / camera.rect.height;

            int ox = System.Math.Max(0, Mathf.FloorToInt(pickerRect.x));
            int oy = System.Math.Max(0, Mathf.FloorToInt((tex.height - pickerRect.y) - pickerRect.height));
            int imageWidth = tex.width;
            int imageHeight = tex.height;
            int width = Mathf.FloorToInt(pickerRect.width);
            int height = Mathf.FloorToInt(pickerRect.height);
            UObject.DestroyImmediate(tex);

            SimpleTuple<ProBuilderMesh, int> hit;
            HashSet<int> indexes = null;
            HashSet<uint> used = new HashSet<uint>();

            for (int y = oy; y < System.Math.Min(oy + height, imageHeight); y++)
            {
                for (int x = ox; x < System.Math.Min(ox + width, imageWidth); x++)
                {
                    uint v = DecodeRGBA(pix[y * imageWidth + x]);

#if PB_RENDER_PICKER_TEXTURE
                    rectImg.Add(pix[y * imageWidth + x]);
#endif

                    if (used.Add(v) && map.TryGetValue(v, out hit))
                    {
                        if (selected.TryGetValue(hit.item1, out indexes))
                            indexes.Add(hit.item2);
                        else
                            selected.Add(hit.item1, new HashSet<int>() { hit.item2 });
                    }
                }
            }

            var coincidentSelection = new Dictionary<ProBuilderMesh, HashSet<int>>();

            // workaround for picking vertices that share a position but are not shared
            foreach (var meshSelection in selected)
            {
                var positions = meshSelection.Key.positions;
                var sharedVertices = meshSelection.Key.sharedVertices;
                var positionHash = new HashSet<int>(meshSelection.Value.Select(x => GetHashCode(positions[sharedVertices[x][0]])));
                var collected = new HashSet<int>();

                for (int i = 0, c = sharedVertices.Count; i < c; i++)
                {
                    var hash = GetHashCode(positions[sharedVertices[i][0]]);
                    if (positionHash.Contains(hash))
                        collected.Add(i);
                }

                coincidentSelection.Add(meshSelection.Key, collected);
            }
            selected = coincidentSelection;

#if PB_RENDER_PICKER_TEXTURE
            if (width > 0 && height > 0)
            {
                Texture2D img = new Texture2D(width, height);
                img.SetPixels(rectImg.ToArray());
                img.Apply();
                System.IO.File.WriteAllBytes("Assets/rect_" + _RenderTextureFormat.ToString() + ".png", img.EncodeToPNG());
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
                UObject.DestroyImmediate(img);
            }
#endif

            return selected;
        }

        protected virtual Texture2D RenderSelectionPickerTexture(
            Camera camera,
            IList<ProBuilderMesh> selection,
            bool doDepthTest,
            out Dictionary<uint, SimpleTuple<ProBuilderMesh, int>> map,
            int width = -1,
            int height = -1)
        {
            GameObject[] depthObjects, pickerObjects;

            GenerateVertexPickingObjects(selection, doDepthTest, out map, out depthObjects, out pickerObjects);

            PBBuiltinMaterials.facePickerMaterial.SetColor(FacePickerOcclusionTintUniform, Blackf);

            Texture2D tex = RenderVerticesWithRepacementShader(camera, PBBuiltinMaterials.selectionPickerShader, "ProBuilderPicker", width, height);

            for (int i = 0, c = pickerObjects.Length; i < c; i++)
            {
                UObject.DestroyImmediate(pickerObjects[i].GetComponent<MeshFilter>().sharedMesh);
                UObject.DestroyImmediate(pickerObjects[i]);
            }

            if (doDepthTest)
            {
                for (int i = 0, c = depthObjects.Length; i < c; i++)
                {
                    UObject.DestroyImmediate(depthObjects[i]);
                }
            }

            return tex;
        }

        protected virtual void GenerateVertexPickingObjects(
          IList<ProBuilderMesh> selection,
          bool doDepthTest,
          out Dictionary<uint, SimpleTuple<ProBuilderMesh, int>> map,
          out GameObject[] depthObjects,
          out GameObject[] pickerObjects)
        {
            map = new Dictionary<uint, SimpleTuple<ProBuilderMesh, int>>();

            // don't start at 0 because that means one vertex would be black, matching
            // the color used to cull hidden vertices.
            uint index = 0x02;
            int selectionCount = selection.Count;
            pickerObjects = new GameObject[selectionCount];

            for (int i = 0; i < selectionCount; i++)
            {
                // build vertex billboards
                var pb = selection[i];
                GameObject go = EmptyGameObjectWithTransform(pb.transform);
                go.name = pb.name + "  (Vertex Billboards)";
                go.AddComponent<MeshFilter>().sharedMesh = BuildVertexMesh(pb, map, ref index);
                go.AddComponent<MeshRenderer>().sharedMaterial = PBBuiltinMaterials.vertexPickerMaterial;
                pickerObjects[i] = go;
            }

            if (doDepthTest)
            {
                depthObjects = new GameObject[selectionCount];

                // copy the select gameobject just for z-write
                for (int i = 0; i < selectionCount; i++)
                {
                    var pb = selection[i];
                    GameObject go = EmptyGameObjectWithTransform(pb.transform);
                    go.name = pb.name + "  (Depth Mask)";
                    go.AddComponent<MeshFilter>().sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = PBBuiltinMaterials.facePickerMaterial;
                    depthObjects[i] = go;
                }
            }
            else
            {
                depthObjects = null;
            }
        }

        static Mesh BuildVertexMesh(ProBuilderMesh pb, Dictionary<uint, SimpleTuple<ProBuilderMesh, int>> map, ref uint index)
        {
            int length = System.Math.Min(pb.sharedVertices.Count, ushort.MaxValue / 4 - 1);

            Vector3[] t_billboards = new Vector3[length * 4];
            Vector2[] t_uvs = new Vector2[length * 4];
            Vector2[] t_uv2 = new Vector2[length * 4];
            Color[] t_col = new Color[length * 4];
            int[] t_tris = new int[length * 6];

            int n = 0;
            int t = 0;

            Vector3 up = Vector3.up;
            Vector3 right = Vector3.right;

            IList<Vector3> positions = pb.positions;
            IList<SharedVertex> shaderdVertices = pb.sharedVertices;
            for (int i = 0; i < length; i++)
            {
                Vector3 v = positions[pb.sharedVertices[i][0]];

                t_billboards[t + 0] = v;
                t_billboards[t + 1] = v;
                t_billboards[t + 2] = v;
                t_billboards[t + 3] = v;

                t_uvs[t + 0] = Vector3.zero;
                t_uvs[t + 1] = Vector3.right;
                t_uvs[t + 2] = Vector3.up;
                t_uvs[t + 3] = Vector3.one;

                t_uv2[t + 0] = -up - right;
                t_uv2[t + 1] = -up + right;
                t_uv2[t + 2] = up - right;
                t_uv2[t + 3] = up + right;

                t_tris[n + 0] = t + 0;
                t_tris[n + 1] = t + 1;
                t_tris[n + 2] = t + 2;
                t_tris[n + 3] = t + 1;
                t_tris[n + 4] = t + 3;
                t_tris[n + 5] = t + 2;

                Color32 color = EncodeRGBA(index);
                map.Add(index++, new SimpleTuple<ProBuilderMesh, int>(pb, i));

                t_col[t + 0] = color;
                t_col[t + 1] = color;
                t_col[t + 2] = color;
                t_col[t + 3] = color;

                t += 4;
                n += 6;
            }

            Mesh mesh = new Mesh();
            mesh.name = "Vertex Billboard";
            mesh.vertices = t_billboards;
            mesh.uv = t_uvs;
            mesh.uv2 = t_uv2;
            mesh.colors = t_col;
            mesh.triangles = t_tris;

            return mesh;
        }

        public virtual Dictionary<ProBuilderMesh, HashSet<Edge>> PickEdgesInRect(
            Camera camera,
            Rect pickerRect,
            IList<ProBuilderMesh> selection,
            bool doDepthTest,
            int renderTextureWidth = -1,
            int renderTextureHeight = -1)
        {
            var selected = new Dictionary<ProBuilderMesh, HashSet<Edge>>();

#if PB_RENDER_PICKER_TEXTURE
            List<Color> rectImg = new List<Color>();
#endif
            Dictionary<uint, SimpleTuple<ProBuilderMesh, Edge>> map;
            Texture2D tex = RenderSelectionPickerTexture(camera, selection, doDepthTest, out map, renderTextureWidth, renderTextureHeight);
            Color32[] pix = tex.GetPixels32();

#if PB_RENDER_PICKER_TEXTURE
            System.IO.File.WriteAllBytes("Assets/edge_scene.png", tex.EncodeToPNG());
#endif
            pickerRect.width /= camera.rect.width;
            pickerRect.height /= camera.rect.height;
            pickerRect.x = (pickerRect.x - camera.pixelRect.x) / camera.rect.width;
            pickerRect.y = (pickerRect.y - (tex.height - (camera.pixelRect.y + camera.pixelRect.height))) / camera.rect.height;

            int ox = System.Math.Max(0, Mathf.FloorToInt(pickerRect.x));
            int oy = System.Math.Max(0, Mathf.FloorToInt((tex.height - pickerRect.y) - pickerRect.height));
            int imageWidth = tex.width;
            int imageHeight = tex.height;
            int width = Mathf.FloorToInt(pickerRect.width);
            int height = Mathf.FloorToInt(pickerRect.height);
            UObject.DestroyImmediate(tex);

            var pixelCount = new Dictionary<uint, uint>();

            for (int y = oy; y < System.Math.Min(oy + height, imageHeight); y++)
            {
                for (int x = ox; x < System.Math.Min(ox + width, imageWidth); x++)
                {
#if PB_RENDER_PICKER_TEXTURE
                    rectImg.Add(pix[y * imageWidth + x]);
#endif
                    uint v = DecodeRGBA(pix[y * imageWidth + x]);

                    if (v == PickerHashNone || v == PickerHashMax)
                        continue;

                    if (!pixelCount.ContainsKey(v))
                        pixelCount.Add(v, 1);
                    else
                        pixelCount[v] = pixelCount[v] + 1;
                }
            }

            foreach (var kvp in pixelCount)
            {
                SimpleTuple<ProBuilderMesh, Edge> hit;

                if (kvp.Value > MinEdgePixelsForValidSelection && map.TryGetValue(kvp.Key, out hit))
                {
                    HashSet<Edge> edges = null;

                    if (selected.TryGetValue(hit.item1, out edges))
                        edges.Add(hit.item2);
                    else
                        selected.Add(hit.item1, new HashSet<Edge>() { hit.item2 });
                }
            }

#if PB_RENDER_PICKER_TEXTURE
            if (width > 0 && height > 0)
            {
                Texture2D img = new Texture2D(width, height);
                img.SetPixels(rectImg.ToArray());
                img.Apply();
                System.IO.File.WriteAllBytes("Assets/edge_rect_" + _RenderTextureFormat.ToString() + ".png", img.EncodeToPNG());
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
                UObject.DestroyImmediate(img);
            }
#endif

            return selected;
        }

        protected virtual Texture2D RenderSelectionPickerTexture(
            Camera camera,
            IList<ProBuilderMesh> selection,
            bool doDepthTest,
            out Dictionary<uint, SimpleTuple<ProBuilderMesh, Edge>> map,
            int width = -1,
            int height = -1)
        {
            GameObject[] depthObjects, pickerObjects;
            GenerateEdgePickingObjects(selection, doDepthTest, out map, out depthObjects, out pickerObjects);

            PBBuiltinMaterials.facePickerMaterial.SetColor(FacePickerOcclusionTintUniform, Blackf);

            Texture2D tex = RenderEdgesWithRepacementShader(camera, PBBuiltinMaterials.selectionPickerShader, "ProBuilderPicker", width, height);

            for (int i = 0, c = pickerObjects.Length; i < c; i++)
            {
                UObject.DestroyImmediate(pickerObjects[i].GetComponent<MeshFilter>().sharedMesh);
                UObject.DestroyImmediate(pickerObjects[i]);
            }

            if (doDepthTest)
            {
                for (int i = 0, c = depthObjects.Length; i < c; i++)
                {
                    UObject.DestroyImmediate(depthObjects[i]);
                }
            }
            return tex;
        }

        protected virtual void GenerateEdgePickingObjects(
            IList<ProBuilderMesh> selection,
            bool doDepthTest,
            out Dictionary<uint, SimpleTuple<ProBuilderMesh, Edge>> map,
            out GameObject[] depthObjects,
            out GameObject[] pickerObjects)
        {
            map = new Dictionary<uint, SimpleTuple<ProBuilderMesh, Edge>>();

            uint index = 0x2;
            int selectionCount = selection.Count;
            pickerObjects = new GameObject[selectionCount];

            for (int i = 0; i < selectionCount; i++)
            {
                // build edge billboards
                var pb = selection[i];
                GameObject go = EmptyGameObjectWithTransform(pb.transform);
                go.name = pb.name + "  (Edge Billboards)";
                go.AddComponent<MeshFilter>().sharedMesh = BuildEdgeMesh(pb, map, ref index);
                go.AddComponent<MeshRenderer>().sharedMaterial = PBBuiltinMaterials.edgePickerMaterial;
                pickerObjects[i] = go;
            }

            if (doDepthTest)
            {
                depthObjects = new GameObject[selectionCount];

                for (int i = 0; i < selectionCount; i++)
                {
                    var pb = selection[i];
                    // copy the select gameobject just for z-write
                    GameObject go = EmptyGameObjectWithTransform(pb.transform);
                    go.name = pb.name + "  (Depth Mask)";
                    go.AddComponent<MeshFilter>().sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = PBBuiltinMaterials.facePickerMaterial;
                    depthObjects[i] = go;
                }
            }
            else
            {
                depthObjects = null;
            }
        }

        static Mesh BuildEdgeMesh(ProBuilderMesh pb, Dictionary<uint, SimpleTuple<ProBuilderMesh, Edge>> map, ref uint index)
        {
            int edgeCount = 0;
            int faceCount = pb.faceCount;

            IList<Face> faces = pb.faces;

            for (int i = 0; i < faceCount; i++)
                edgeCount += faces[i].edges.Count;

            int elementCount = System.Math.Min(edgeCount, ushort.MaxValue / 2 - 1);

            Vector3[] positions = new Vector3[elementCount * 2];
            Color32[] color = new Color32[elementCount * 2];
            int[] tris = new int[elementCount * 2];

            int edgeIndex = 0;

            IList<Vector3> pbPositions = pb.positions;
            for (int i = 0; i < faceCount && edgeIndex < elementCount; i++)
            {
                for (int n = 0; n < faces[i].edges.Count && edgeIndex < elementCount; n++)
                {
                    var edge = faces[i].edges[n];

                    Vector3 a = pbPositions[edge.a];
                    Vector3 b = pbPositions[edge.b];
                    int positionIndex = edgeIndex * 2;

                    positions[positionIndex + 0] = a;
                    positions[positionIndex + 1] = b;

                    Color32 c = EncodeRGBA(index);

                    map.Add(index++, new SimpleTuple<ProBuilderMesh, Edge>(pb, edge));

                    color[positionIndex + 0] = c;
                    color[positionIndex + 1] = c;

                    tris[positionIndex + 0] = positionIndex + 0;
                    tris[positionIndex + 1] = positionIndex + 1;

                    edgeIndex++;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "Edge Billboard";
            mesh.vertices = positions;
            mesh.colors32 = color;
            mesh.subMeshCount = 1;
            mesh.SetIndices(tris, MeshTopology.Lines, 0);

            return mesh;
        }
    }
}