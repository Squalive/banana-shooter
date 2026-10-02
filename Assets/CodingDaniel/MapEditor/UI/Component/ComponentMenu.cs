using System.Collections.Generic;
using CodingDaniel.MapEditor.Interaction.ObjectGizmos;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace CodingDaniel.MapEditor.UI.Component
{
    public class ComponentMenu : MonoBehaviour
    {
        public static ComponentMenu Instance { private set; get; }
        [SerializeField] private HoverOnUICheck check;
        [SerializeField] private LocalizeStringEvent nameText;
        [SerializeField] private RawImage iconImg;

        [SerializeField] private RectTransform rectTransform;
        private Vector2 _desiredPos;

        private IMESelection _selection;

        [SerializeField] private Texture2D lightingTexture,decalTexture,audioTexture;

        private void Awake()
        {
            Instance = this;
        }

        private int screenWidth = 0;
        private void Start()
        {
            ClosePanel();
            
            _selection=MEBase.Instance.Selection;

            _selection.SelectionChanged += OnSelectionChanged;
            
            InvokeRepeating(nameof(RefreshWidth),0,5f);

            _camera = MEBase.Instance.Graphics.GetOrCreateCamera(MEBase.Instance.Camera, CameraEvent.AfterImageEffectsOpaque);
        }

        void RefreshWidth()
        {
            screenWidth = Screen.width;
            
        }

        private void OnDestroy()
        {
            _selection.SelectionChanged -= OnSelectionChanged;
        }

        private void Update()
        {
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition,_desiredPos,Time.deltaTime*15f);
        }

        void ClosePanel()
        {
            _desiredPos = new Vector2(-300, 0);

            foreach (var c in _gizmos.Values)
            {
                DestroyImmediate(c);
            }
            _gizmos.Clear();
        }

        void OpenPanel()
        {
            _desiredPos = Vector2.zero;

            foreach (var property in properties.Values)
            {
                foreach (var p in property.Values)
                {
                    Destroy(p.gameObject);
                }
                
            }
            
            properties.Clear();
        }

        [Tooltip("The Properties father")]
        [SerializeField] private Transform content;
        [SerializeField] private BoolPropertyUI boolPrefab;
        [SerializeField] private FloatPropertyUI floatPrefab;
        [SerializeField] private EnumPropertyUI enumPrefab;
        [SerializeField] private ColorPropertyUI colorPrefab;

        public readonly Dictionary<object,Dictionary<string,BaseProperty>> properties = new Dictionary<object,Dictionary<string,BaseProperty>>();

        private readonly Dictionary<object, UnityEngine.Component> _gizmos = new Dictionary<object, UnityEngine.Component>();

        private IMECamera _camera;
        private void OnSelectionChanged(Object[] unselectedobjects)
        {
            if (unselectedobjects != null)
            {
                foreach (var obj in unselectedobjects)
                {
                    var go = obj as GameObject;
                    
                    Light l = go.GetComponent<Light>();
                    DecalProjector decal = go.GetComponent<DecalProjector>();
                    AudioSource au = go.GetComponent<AudioSource>();

                    if (l != null)
                    {
                        if (_gizmos.TryGetValue(l, out var c))
                        {
                            if (c != null)
                            {
                                Destroy(c);
                            }

                            _gizmos.Remove(l);

                        }
                    }
                    else if (decal != null)
                    {
                        if (_gizmos.TryGetValue(decal, out var c))
                        {
                            if (c != null)
                            {
                                Destroy(c);
                            }
                            _gizmos.Remove(decal);

                        }
                    }
                    else if (au != null)
                    {
                        au.Stop();
                        if (_gizmos.TryGetValue(au, out var c))
                        {
                            if (c != null)
                            {
                                Destroy(c);
                            }
                            _gizmos.Remove(au);

                        }
                    }
                }
            }

            if (_selection==null||_selection.ActiveGameObject==null||_selection.GameObjects==null || _selection.GameObjects.Length > 1)
            {
                ClosePanel();
                return;
            }

            Light light = _selection.ActiveGameObject.GetComponent<Light>();
            DecalProjector decalProjector = _selection.ActiveGameObject.GetComponent<DecalProjector>();
            AudioSource audioSource = _selection.ActiveGameObject.GetComponent<AudioSource>();
            MEDecoration decoration = _selection.ActiveGameObject.GetComponent<MEDecoration>();

            if (light != null && !_gizmos.ContainsKey(light))
            {
                var exp = light.GetComponent<ExposeToEditor>();
                bool canEdit = exp != null && exp.CanDelete;
                ObjectGizmoBehaviour gizmo = ObjectGizmoBehaviour.Attach(light);
                _gizmos.Add(light,gizmo);
                
                OpenPanel();
                //Set the name
                nameText.SetEntry("Lighting");
                //Set the texture
                iconImg.texture = lightingTexture;
                
                //Properties

                Dictionary<string,BaseProperty> pr = new Dictionary<string,BaseProperty>();

                //1.Bool -- enable 
                BoolPropertyUI enableUI = Instantiate(boolPrefab, content);
                enableUI.Init("Enable",light.enabled,canEdit);
                
                enableUI.onValueSet += (o =>
                {
                    light.enabled = (bool)o;
                });
                pr.Add("enable",enableUI);
                
                //2. enum -- mode
                EnumPropertyUI type = Instantiate(enumPrefab, content);
                List<string> str = new List<string>();
                str.Add(LightType.Spot.ToString());
                str.Add(LightType.Directional.ToString());
                str.Add(LightType.Point.ToString());
                type.SetDropdown(str);
                type.Init("Type",(int)light.type,canEdit);

                type.onValueSet += (o =>
                {
                    light.type = (LightType) o;
                    
                    _selection.Select(null,null);
                    _selection.Select(light.gameObject,new Object[]{light.gameObject});
                });
                pr.Add("type",type);
                
                //3.float -- Intensity
                FloatPropertyUI intensityPropertyUI = Instantiate(floatPrefab, content);
                intensityPropertyUI.Init("Intensity",light.intensity,true);

                intensityPropertyUI.onValueSet += (o =>
                {
                    light.intensity = float.Parse(o.ToString());
                });
                
                pr.Add("intensity",intensityPropertyUI);

                if (light.type != LightType.Directional)
                {
                    //4.float -- Range
                    FloatPropertyUI rangePropertyUI = Instantiate(floatPrefab, content);
                    rangePropertyUI.Init("Range",light.range,true);

                    rangePropertyUI.onValueSet += (o =>
                    {
                        if (float.TryParse(o.ToString(), out float result))
                        {
                            light.range = result;
                        }
                        
                        _camera.RefreshCommandBuffer();
                    });
                
                    pr.Add("range",rangePropertyUI);
                }
                
                //Color -- color
                ColorPropertyUI colorPropertyUI = Instantiate(colorPrefab, content);
                colorPropertyUI.Init("Color",light.color,true);

                colorPropertyUI.onValueSet += (color =>
                {
                    light.color = (Color)color;
                });
                
                pr.Add("color",colorPropertyUI);
                
                if (light.type == LightType.Spot)
                {
                    //4.float -- Spot angle out
                    FloatPropertyUI rangePropertyUI = Instantiate(floatPrefab, content);
                    rangePropertyUI.Init("Spot Angle",light.spotAngle,true);

                    rangePropertyUI.onValueSet += (o =>
                    {
                        light.spotAngle = float.Parse(o.ToString());
                        light.innerSpotAngle = light.spotAngle *.8f;
                        _camera.RefreshCommandBuffer();
                    });
                
                    pr.Add("spotangle",rangePropertyUI);
                    
                    //5.float -- Spot angle inner
                    FloatPropertyUI innerPropertyUI = Instantiate(floatPrefab, content);
                    innerPropertyUI.Init("Inner Spot Angle",light.innerSpotAngle,true);

                    innerPropertyUI.onValueSet += (o =>
                    {
                        light.innerSpotAngle = float.Parse(o.ToString());
                        
                    });
                
                    pr.Add("innerspotangle",innerPropertyUI);
                }
                
                properties.Add(light,pr);
            }
            else if (decalProjector!=null && !_gizmos.ContainsKey(decalProjector))
            {
                ObjectGizmoBehaviour gizmo = ObjectGizmoBehaviour.Attach(decalProjector);
                _gizmos.Add(decalProjector,gizmo);
                
                OpenPanel();
                //Set the name
                nameText.SetEntry("Decal");
                //Set the texture
                iconImg.texture = decalTexture;
                
                Dictionary<string,BaseProperty> pr = new Dictionary<string,BaseProperty>();
                
                //1.Bool -- enable 
                BoolPropertyUI enableUI = Instantiate(boolPrefab, content);
                enableUI.Init("Enable",decalProjector.enabled,true);
                
                enableUI.onValueSet += (o =>
                {
                    decalProjector.enabled = (bool)o;
                });
                pr.Add("enable",enableUI);
                
                //2.float -- Width
                FloatPropertyUI widthPropertyUI = Instantiate(floatPrefab, content);
                widthPropertyUI.Init("Width",decalProjector.size.x,true);

                widthPropertyUI.onValueSet += (o =>
                {
                    if (float.TryParse(o.ToString(), out var value))
                    {
                        decalProjector.size = new Vector3(value, decalProjector.size.y, decalProjector.size.z);
                    }
                    
                    gizmo.Refresh();
                });
                
                pr.Add("width",widthPropertyUI);

                //3.float -- geight
                FloatPropertyUI heightPropertyUI = Instantiate(floatPrefab, content);
                heightPropertyUI.Init("Height",decalProjector.size.y,true);

                heightPropertyUI.onValueSet += (o =>
                {
                    if (float.TryParse(o.ToString(), out var value))
                    {
                        decalProjector.size = new Vector3(decalProjector.size.x, value, decalProjector.size.z);
                    }
                    gizmo.Refresh();
                });
                
                pr.Add("height",heightPropertyUI);
                //4.float -- depth
                FloatPropertyUI depthPropertyUI = Instantiate(floatPrefab, content);
                depthPropertyUI.Init("Depth",decalProjector.size.z,true);

                depthPropertyUI.onValueSet += (o =>
                {
                    if (float.TryParse(o.ToString(), out var value))
                    {
                        decalProjector.size = new Vector3(decalProjector.size.x, decalProjector.size.y,value);
                    }
                    gizmo.Refresh();
                });
                
                pr.Add("depth",depthPropertyUI);
                properties.Add(decalProjector,pr);
            }
            else if (audioSource != null && !_gizmos.ContainsKey(audioSource))
            {
                ObjectGizmoBehaviour gizmo = ObjectGizmoBehaviour.Attach(audioSource);

                _gizmos.Add(audioSource,gizmo);
                
                audioSource.Play();
                
                OpenPanel();
                
                //Set the name
                nameText.SetEntry("AudioSource");
                //Set the texture
                iconImg.texture = audioTexture;
                
                Dictionary<string,BaseProperty> pr = new Dictionary<string,BaseProperty>();
                
                //1.Bool -- enable 
                BoolPropertyUI enableUI = Instantiate(boolPrefab, content);
                enableUI.Init("Enable",audioSource.enabled,true);
                
                enableUI.onValueSet += (o =>
                {
                    audioSource.enabled = (bool)o;
                    if(audioSource.enabled)
                        audioSource.Play();
                });
                pr.Add("enable",enableUI);
                
                BoolPropertyUI loopUI = Instantiate(boolPrefab, content);
                loopUI.Init("Loop",audioSource.loop,true);
                
                loopUI.onValueSet += (o =>
                {
                    audioSource.loop = (bool)o;
                });
                pr.Add("loop",loopUI);
                
                //2.float -- audioSource.maxDistance
                FloatPropertyUI maxDistancePrr = Instantiate(floatPrefab, content);
                maxDistancePrr.Init("Max Distance",audioSource.maxDistance,true);

                maxDistancePrr.onValueSet += (o =>
                {
                    if (float.TryParse(o.ToString(), out var value))
                    {
                        audioSource.maxDistance = value;
                    }
                    
                    gizmo.Refresh();
                });
                
                pr.Add("maxDistance",maxDistancePrr);
                
                //2.float -- audioSource.maxDistance
                FloatPropertyUI minDistancePrr = Instantiate(floatPrefab, content);
                minDistancePrr.Init("Min Distance",audioSource.minDistance,true);

                minDistancePrr.onValueSet += (o =>
                {
                    if (float.TryParse(o.ToString(), out var value))
                    {
                        audioSource.minDistance = value;
                    }
                    
                    gizmo.Refresh();
                });
                
                pr.Add("minDistance",minDistancePrr);
                
                FloatPropertyUI spatialBlendPr = Instantiate(floatPrefab, content);
                spatialBlendPr.Init("Spatial Blend",audioSource.spatialBlend,true);

                spatialBlendPr.onValueSet += (o =>
                {
                    if (float.TryParse(o.ToString(), out var value))
                    {
                        audioSource.spatialBlend = value;
                    }
                    
                });
                
                pr.Add("spatialBlend",spatialBlendPr);
                
                FloatPropertyUI dopplerLevelPr = Instantiate(floatPrefab, content);
                dopplerLevelPr.Init("Doppler Level",audioSource.dopplerLevel,true);

                dopplerLevelPr.onValueSet += (o =>
                {
                    if (float.TryParse(o.ToString(), out var value))
                    {
                        audioSource.dopplerLevel = Mathf.Clamp(value,0,1f);
                    }
                    
                });
                
                pr.Add("dopplerLevel",dopplerLevelPr);
                
                properties.Add(audioSource,pr);
            }
            else if (decoration != null)
            {
                OpenPanel();
                
                //Set the name
                nameText.SetEntry("Model");
                //Set the texture
                iconImg.texture = audioTexture;
                
                Dictionary<string,BaseProperty> pr = new Dictionary<string,BaseProperty>();
                
                BoolPropertyUI enableUI = Instantiate(boolPrefab, content);
                enableUI.Init("EnableCollision",decoration.enableCollision,true);
                
                enableUI.onValueSet += (o =>
                {
                    decoration.enableCollision = (bool)o;
                });
                pr.Add("enablecollistion",enableUI);
                
                EnumPropertyUI type = Instantiate(enumPrefab, content);
                List<string> str = new List<string>();
                str.Add(MEDecoration.EDecorationType.None.ToString());
                str.Add(MEDecoration.EDecorationType.Obstacle.ToString());
                str.Add(MEDecoration.EDecorationType.Water.ToString());
                str.Add(MEDecoration.EDecorationType.GrapplePoint.ToString());
                type.SetDropdown(str);
                type.Init("Type",(int)decoration.type,true);

                type.onValueSet += (o =>
                {
                    decoration.type = (MEDecoration.EDecorationType) o;
                });
                pr.Add("type",type);

                properties.Add(decoration,pr);
            }
            else
            {
                ClosePanel();
            }
        }

        public bool IsHoverOn()
        {
            return check.Hover || (screenWidth*7f/8f <Input.mousePosition.y);
        }
    }
}
