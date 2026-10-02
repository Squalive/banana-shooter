using System;
using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.MEEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CodingDaniel.MapEditor.UI
{
    public class UVEditorUI : MonoBehaviour
    {
        public static  UVEditorUI Instance { private set; get;}
        [SerializeField] private RectTransform ui;
        private Vector2 _desiredPos;
        public bool DisplayingUI { private set; get; }

        public HoverOnUICheck hoverOnUICheck;

        private ProBuilderTool _tool;

        [SerializeField] private GameObject autoUVPanel;
        [SerializeField] private GameObject notSelectedFaces;
        private void Awake()
        {
            Instance = this;
            _tool = ProBuilderTool.Instance;

            UpdateVisualState();
            _tool.SelectionChanged += OnToolSelectionChanged;
            MEBase.Instance.Selection.SelectionChanged += OnSelectionChanged;

            offsetX.onEndEdit.AddListener(OffsetXChanged);
            offsetY.onEndEdit.AddListener(OffsetYChanged);
            
            tilingY.onEndEdit.AddListener(TilingYChanged);
            tilingX.onEndEdit.AddListener(TilingXChanged);

            rotationSlider.minValue = 0;
            rotationSlider.maxValue = 360;
            
            rotationSlider.onValueChanged.AddListener(RotationChanged);

            worldSpaceToggle.onValueChanged.AddListener(SetWorldSpace);
            flipUToggle.onValueChanged.AddListener(SetFlipU);
            flipVToggle.onValueChanged.AddListener(SetFlipV);
            swapUVToggle.onValueChanged.AddListener(SetSwapUV);
            
            groupFaceBtn.onClick.AddListener(OnGroupFace);
            unGroupFaceBtn.onClick.AddListener(OnUngroupFace);
            selectFaceGroupBtn.onClick.AddListener(OnSelectFaceGroup);
            resetUVBtn.onClick.AddListener(OnResetUV);
            
            fillDropDown.onValueChanged.AddListener(SetFill);
            anchorDropDown.onValueChanged.AddListener(SetAnchor);
        }

        private void OnSelectionChanged(Object[] unselectedobjects)
        {
            UpdateVisualState();
        }


        private void OnDestroy()
        {
            offsetX.onEndEdit.RemoveListener(OffsetXChanged);
            offsetY.onEndEdit.RemoveListener(OffsetYChanged);
            
            tilingY.onEndEdit.RemoveListener(TilingYChanged);
            tilingX.onEndEdit.RemoveListener(TilingXChanged);

            rotationSlider.onValueChanged.RemoveListener(RotationChanged);

            worldSpaceToggle.onValueChanged.RemoveListener(SetWorldSpace);
            flipUToggle.onValueChanged.RemoveListener(SetFlipU);
            flipVToggle.onValueChanged.RemoveListener(SetFlipV);
            swapUVToggle.onValueChanged.RemoveListener(SetSwapUV);
            _tool.SelectionChanged -= OnToolSelectionChanged;
            MEBase.Instance.Selection.SelectionChanged -= OnSelectionChanged;
            
            
            groupFaceBtn.onClick.RemoveListener(OnGroupFace);
            unGroupFaceBtn.onClick.RemoveListener(OnUngroupFace);
            selectFaceGroupBtn.onClick.RemoveListener(OnSelectFaceGroup);
            resetUVBtn.onClick.RemoveListener(OnResetUV);           
            fillDropDown.onValueChanged.RemoveListener(SetFill);
            
            anchorDropDown.onValueChanged.RemoveListener(SetAnchor);

        }

        void UpdateVisualState()
        {
            if (_tool == null) return;

            if (_tool.HasSelectedAutoUVs)
            {
                notSelectedFaces.SetActive(false);
                autoUVPanel.SetActive(true);
            }
            else
            {
                autoUVPanel.SetActive(false);
                notSelectedFaces.SetActive(true);
                
            }
        }

        public void SetDisplayUI(bool fl)
        {
            DisplayingUI = fl;
            _tool.UVEditingMode = fl;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                SetDisplayUI(!DisplayingUI);
            }
            
            _desiredPos = DisplayingUI ? new Vector2(0, 0) : new Vector2(300f,0f);
            ui.anchoredPosition = Vector2.Lerp(ui.anchoredPosition,_desiredPos,Time.deltaTime*15f);
        }
        void OnToolSelectionChanged()
        {
            UpdateVisualState();
            if (_tool.HasSelectedAutoUVs)
            {
                RefreshAll();
            }
        }

        public void RefreshAll()
        {
            UpdateOffset();
            UpdateRotation();
            UpdateTiling();
            UpdateWorldSpace();
            UpdateFlipU();
            UpdateFlipV();
            UpdateSwapUV();
            UpdateFill();
            UpdateAnchor();
        }

        #region Transform

        [SerializeField] private TMP_InputField offsetX, offsetY;

        private bool _changedOffset;
        void UpdateOffset()
        {
            if (_changedOffset) return;
            offsetX.SetTextWithoutNotify(_tool.UV.offset.x.ToString("F2"));
            offsetY.SetTextWithoutNotify(_tool.UV.offset.y.ToString("F2"));
        }

        void OffsetFinishChanged()
        {
            _changedOffset = false;
        }
        void OffsetXChanged(string str)
        {
            if (float.TryParse(str, out var value))
            {
                _changedOffset = true;
                _tool.UV.offset = new Vector2(value, _tool.UV.offset.y);

                Invoke(nameof(OffsetFinishChanged), 0.1f);
            }
            else
            {
                UpdateOffset();
            }
        }
        void OffsetYChanged(string str)
        {
            if (float.TryParse(str, out var value))
            {
                _changedOffset = true;
                _tool.UV.offset = new Vector2(_tool.UV.offset.x, float.Parse(str));

                Invoke(nameof(OffsetFinishChanged), 0.1f);
            }
            else
            {
                UpdateOffset();
            }
        }

        [SerializeField] private Slider rotationSlider;
        [SerializeField] private TextMeshProUGUI rotationText;

        private bool _changedRotation=false;
        void UpdateRotation()
        {
            if (_changedRotation) return;
            rotationSlider.SetValueWithoutNotify(_tool.UV.rotation);
            rotationText.SetText(_tool.UV.rotation.ToString("F0"));
        }
        void RotationFinishChanged()
        {
            _changedRotation = false;
        }
        void RotationChanged(float value)
        {
            _changedRotation = true;
            _tool.UV.rotation = value;
            rotationText.SetText(value.ToString("F0"));
            Invoke(nameof(RotationFinishChanged),0.1f);
        }
        
        [SerializeField] private TMP_InputField tilingX, tilingY;

        private bool _changedTiling;
        void UpdateTiling()
        {
            if (_changedTiling) return;
            tilingX.SetTextWithoutNotify(_tool.UV.scale.x.ToString("F2"));
            tilingY.SetTextWithoutNotify(_tool.UV.scale.y.ToString("F2"));
        }

        void TilingFinishChanged()
        {
            _changedTiling = false;
        }
        void TilingXChanged(string str)
        {
            if (float.TryParse(str, out var value))
            {
                _changedTiling = true;
                _tool.UV.scale = new Vector2(value,_tool.UV.scale.y);
            
                Invoke(nameof(TilingFinishChanged),0.1f);
            }
            else
            {
                UpdateTiling();
            }
        }
        void TilingYChanged(string str)
        {
            if (float.TryParse(str, out var value))
            {
                _changedTiling = true;
                _tool.UV.scale = new Vector2(_tool.UV.scale.x, value);

                Invoke(nameof(TilingFinishChanged), 0.1f);
            }
            else
            {
                UpdateTiling();
                
            }
        }

        #endregion

        #region Special

        #region World Space
        [SerializeField] private Toggle worldSpaceToggle;
        private bool _changedWorldSpace;

        void UpdateWorldSpace()
        {
            if (_changedWorldSpace) return;
            worldSpaceToggle.SetIsOnWithoutNotify(_tool.UV.useWorldSpace);
        }

        void ResetWorldSpace()
        {
            _changedWorldSpace = false;
        }

        void SetWorldSpace(bool value)
        {
            _changedWorldSpace = true;
            _tool.UV.useWorldSpace = value;
            
            Invoke(nameof(ResetWorldSpace), 0.1f);
        }

        

        #endregion

        #region Flip U

        [SerializeField] private Toggle flipUToggle;
        private bool _changedFlipU;

        void UpdateFlipU()
        {
            if (_changedFlipU) return;
            flipUToggle.SetIsOnWithoutNotify(_tool.UV.flipU);
        }

        void ResetFlipU()
        {
            _changedFlipU = false;
        }

        void SetFlipU(bool value)
        {
            _changedFlipU = true;
            _tool.UV.flipU = value;
            
            Invoke(nameof(ResetFlipU), 0.1f);
        }

        #endregion

        #region Flip V

        [SerializeField] private Toggle flipVToggle;
        private bool _changedFlipV;

        void UpdateFlipV()
        {
            if (_changedFlipV) return;
            flipVToggle.SetIsOnWithoutNotify(_tool.UV.flipV);
        }

        void ResetFlipV()
        {
            _changedFlipV = false;
        }

        void SetFlipV(bool value)
        {
            _changedFlipV = true;
            _tool.UV.flipV = value;
            
            Invoke(nameof(ResetFlipV), 0.1f);
        }

        #endregion

        #region Swap UV

        [SerializeField] private Toggle swapUVToggle;
        private bool _changedSwapUV;

        void UpdateSwapUV()
        {
            if (_changedSwapUV) return;
            swapUVToggle.SetIsOnWithoutNotify(_tool.UV.swapUV);
        }

        void ResetSwapUV()
        {
            _changedSwapUV = false;
        }

        void SetSwapUV(bool value)
        {
            _changedSwapUV = true;
            _tool.UV.swapUV = value;
            
            Invoke(nameof(ResetSwapUV), 0.1f);
        }

        #endregion

        #region Group Face

        [SerializeField] private Button groupFaceBtn;
        void OnGroupFace()
        {
            _tool.GroupFaces();
        }

        #endregion
        
        #region Ungroup Face

        [SerializeField] private Button unGroupFaceBtn;
        void OnUngroupFace()
        {
            _tool.UngroupFaces();
        }

        #endregion
        
        #region Select Face Group

        [SerializeField] private Button selectFaceGroupBtn;
        void OnSelectFaceGroup()
        {
            _tool.SelectFaceGroup();
        }

        #endregion

        #region Reset UVs

        [SerializeField] private Button resetUVBtn;
        void OnResetUV()
        {
            _tool.ResetUVs();
        }

        #endregion
        #endregion

        #region Fill

        [SerializeField]private TMP_Dropdown fillDropDown;

        private bool _changedFill;
        void UpdateFill()
        {
            if (_changedFill) return;
            fillDropDown.SetValueWithoutNotify((int) _tool.UV.fill);
        }

        void SetFill(int value)
        {
            _changedFill = true;

            _tool.UV.fill = (PBAutoUnwrapSettings.Fill) value;
            
            Invoke(nameof(ResetFillChanged),0.1f);
        }

        void ResetFillChanged()
        {
            _changedFill = false;
        }

        #endregion
        
        #region Anchor

        [SerializeField]private TMP_Dropdown anchorDropDown;

        private bool _changedAnchor;
        void UpdateAnchor()
        {
            if (_changedAnchor) return;
            anchorDropDown.SetValueWithoutNotify((int) _tool.UV.anchor);
        }

        void SetAnchor(int value)
        {
            _changedAnchor = true;

            _tool.UV.anchor = (PBAutoUnwrapSettings.Anchor) value;
            
            Invoke(nameof(ResetAnchorChanged),0.1f);
        }

        void ResetAnchorChanged()
        {
            _changedAnchor = false;
        }

        #endregion
    }
}
