using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public interface IAutoUVEditor
    {
        void ApplySettings(PBAutoUnwrapSettings settings, MeshSelection selection);
        PBAutoUnwrapSettings GetSettings(MeshSelection selection);

        bool HasAutoUV(MeshSelection selection, bool auto);
        void SetAutoUV(MeshSelection selection, bool auto);
        void ResetUV(MeshSelection selection);

        void GroupFaces(MeshSelection selection);
        void UngroupFaces(MeshSelection selection);
        MeshSelection SelectFaceGroup(MeshSelection currentSelection);
    }
    public class PBAutoUnwrapSettings
    {
        public event Action Changed;

        public enum Anchor
        {
            UpperLeft = 0,
            UpperCenter = 1,
            UpperRight = 2,
            MiddleLeft = 3,
            MiddleCenter = 4,
            MiddleRight = 5,
            LowerLeft = 6,
            LowerCenter = 7,
            LowerRight = 8,
            None = 9
        }
        public enum Fill
        {
            Fit = 0,
            Tile = 1,
            Stretch = 2
        }

        private AutoUnwrapSettings _settings;

        public PBAutoUnwrapSettings()
        {
            _settings = AutoUnwrapSettings.defaultAutoUnwrapSettings;
        }

        public PBAutoUnwrapSettings(AutoUnwrapSettings unwrapSettings)
        {
            _settings = unwrapSettings;
        }
        public PBAutoUnwrapSettings(PBAutoUnwrapSettings unwrapSettings)
        {
            _settings = new AutoUnwrapSettings(unwrapSettings._settings);
        }

        public static PBAutoUnwrapSettings stretch
        {
            get { return new PBAutoUnwrapSettings(AutoUnwrapSettings.stretch); }
        }

        public static PBAutoUnwrapSettings fit
        {
            get { return new PBAutoUnwrapSettings(AutoUnwrapSettings.fit); }
        }

        public static PBAutoUnwrapSettings defaultAutoUnwrapSettings
        {
            get { return new PBAutoUnwrapSettings(AutoUnwrapSettings.defaultAutoUnwrapSettings); }
        }

        public static PBAutoUnwrapSettings tile
        {
            get { return new PBAutoUnwrapSettings(AutoUnwrapSettings.tile); }
        }

        public Anchor anchor
        {
            get { return (Anchor)_settings.anchor; }
            set
            {
                var newValue = (AutoUnwrapSettings.Anchor)value;
                if(newValue != _settings.anchor)
                {
                    _settings.anchor = newValue;
                    RaiseChanged();
                }
            }
        }


        public float rotation
        {
            get { return _settings.rotation;  }
            set
            {
                if(_settings.rotation != value)
                {
                    float clampedValue = value - Mathf.CeilToInt(value / 360f) * 360f;
                    if (clampedValue < 0)
                    {
                        clampedValue += 360f;
                    }

                    _settings.rotation = clampedValue;
                    RaiseChanged();
                }
            }
        }

        public Vector2 offset
        {
            get { return _settings.offset; }
            set
            {
                if(_settings.offset != value)
                {
                    _settings.offset = value;
                    RaiseChanged();
                }
            }
        }

        public Vector2 scale
        {
            get { return _settings.scale; }
            set
            {
                if(_settings.scale != value)
                {
                    _settings.scale = value;
                    RaiseChanged();
                }
            }
        }

        public Fill fill
        {
            get { return (Fill)_settings.fill; }
            set
            {
                var newValue = (AutoUnwrapSettings.Fill)value; 
                if (_settings.fill != newValue)
                {
                    _settings.fill = newValue;
                    RaiseChanged();
                }
            }
        }

        public bool swapUV
        {
            get { return _settings.swapUV; }
            set
            {
                if(_settings.swapUV != value)
                {
                    _settings.swapUV = value;
                    RaiseChanged();
                }
            }
        }

        public bool flipV
        {
            get { return _settings.flipV; }
            set
            {
                if(_settings.flipV != value)
                {
                    _settings.flipV = value;
                    RaiseChanged();
                }
            }
        }

        public bool flipU
        {
            get { return _settings.flipU; }
            set
            {
                if(_settings.flipU != value)
                {
                    _settings.flipU = value;
                    RaiseChanged();
                }
            }
        }

        public bool useWorldSpace
        {
            get { return _settings.useWorldSpace; }
            set
            {
                if(_settings.useWorldSpace != value)
                {
                    _settings.useWorldSpace = value;
                    RaiseChanged();
                }
            }
        }

        public void Reset()
        {
            _settings.Reset();
        }

        private void RaiseChanged()
        {
            if(Changed != null)
            {
                Changed();
            }
        }

        public void CopyFrom(PBAutoUnwrapSettings settings)
        {
            _settings = settings._settings;
        }

        public override string ToString()
        {
            return _settings.ToString();
        }

        public static implicit operator AutoUnwrapSettings(PBAutoUnwrapSettings settings)
        {
            return settings._settings;
        }

        public static implicit operator PBAutoUnwrapSettings(AutoUnwrapSettings settings)
        {
            return new PBAutoUnwrapSettings(settings);
        }
    }
    public class PBAutoUVEditor : MonoBehaviour,IAutoUVEditor
    {
        public bool HasAutoUV(MeshSelection selection, bool auto)
        {
            if (selection == null)
            {
                return false;
            }

            selection = selection.ToFaces(false, false);
            IList<Face> faces = new List<Face>();
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();

                faces.Clear();
                mesh.GetFaces(kvp.Value, faces);
                for (int i = 0; i < faces.Count; ++i)
                {
                    Face face = faces[i];
                    if(auto)
                    {
                        if (!face.manualUV)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (face.manualUV)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void SetAutoUV(MeshSelection selection, bool auto)
        {
            if (selection == null)
            {
                return;
            }

            selection = selection.ToFaces(false, false);

            List<Face> faces = new List<Face>();
            foreach(KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();

                faces.Clear();
                mesh.GetFaces(kvp.Value, faces);

                PBAutoUVConversion.SetAutoUV(mesh, faces.ToArray(), auto);
                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        public void ResetUV(MeshSelection selection)
        {
            if (selection == null)
            {
                return;
            }

            selection = selection.ToFaces(false, false);

            List<Face> faces = new List<Face>();
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();

                faces.Clear();
                mesh.GetFaces(kvp.Value, faces);

                for(int i = 0; i < faces.Count; ++i)
                {
                    faces[i].uv = AutoUnwrapSettings.defaultAutoUnwrapSettings;
                }

                mesh.RefreshUV(faces);


                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        public void ApplySettings(PBAutoUnwrapSettings settings, MeshSelection selection)
        {
            if (selection == null)
            {
                return;
            }

            selection = selection.ToFaces(false, false);

            AutoUnwrapSettings unwrapSettings = settings;
            IList<Face> faces = new List<Face>();
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();

                faces.Clear();
                mesh.GetFaces(kvp.Value, faces);
                for (int i = 0; i < faces.Count; ++i)
                {
                    Face face = faces[i];
                    face.uv = unwrapSettings;
                }

                mesh.RefreshUV(faces);


                mesh.ToMesh();
                mesh.Refresh();
            }    
        }

        public PBAutoUnwrapSettings GetSettings(MeshSelection selection)
        {
            if (selection == null)
            {
                return PBAutoUnwrapSettings.defaultAutoUnwrapSettings;
            }

            selection = selection.ToFaces(false, false);

            PBAutoUnwrapSettings unwrapSettings = PBAutoUnwrapSettings.defaultAutoUnwrapSettings;
            IList<Face> faces = new List<Face>();
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selection.SelectedFaces)
            {
                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                faces.Clear();
                mesh.GetFaces(kvp.Value, faces);
                for (int i = 0; i < faces.Count;)
                {
                    Face face = faces[i];
                    unwrapSettings = face.uv;
                    break;
                }
            }

            return unwrapSettings;
        }

        private int GetUnusedTextureGroup(ProBuilderMesh mesh)
        {
            return mesh.faces.Max(f => f.textureGroup) + 2;
        }

        private void SetTextureGroup(MeshSelection selection, int textureGroup = 0)
        {
            selection = selection.ToFaces(false, false);

            List<Face> faces = new List<Face>();
            Dictionary<GameObject, IList<int>> selectedFaces = selection.SelectedFaces;
            foreach (KeyValuePair<GameObject, IList<int>> kvp in selectedFaces)
            {
                faces.Clear();

                ProBuilderMesh mesh = kvp.Key.GetComponent<ProBuilderMesh>();
                mesh.GetFaces(kvp.Value, faces);

                int texGroup = textureGroup >= 0 ? GetUnusedTextureGroup(mesh) : -1;
                for (int i = 0; i < faces.Count; ++i)
                {
                    Face face = faces[i];
                    face.textureGroup = texGroup;
                }

                mesh.RefreshUV(faces);

                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        public void GroupFaces(MeshSelection selection)
        {
            SetTextureGroup(selection);
        }

        public void UngroupFaces(MeshSelection selection)
        {
            SetTextureGroup(selection, -1);
        }

        public MeshSelection SelectFaceGroup(MeshSelection currentSelection)
        {
            if(currentSelection == null)
            {
                return null;
            }

            currentSelection = currentSelection.ToFaces(false);
            if(!currentSelection.HasFaces)
            {
                return null;
            }

            
            ProBuilderMesh mesh = currentSelection.SelectedFaces.Last().Key.GetComponent<ProBuilderMesh>();
            IList<int> currentlySelectedFaces = currentSelection.SelectedFaces.Last().Value;
            if(currentlySelectedFaces.Count == 0)
            {
                return currentSelection;
            }
            //HashSet<int> facesHs = new HashSet<int>(currentlySelectedFaces);
            int faceIndex = currentlySelectedFaces.Last();
            int textureGroup = mesh.faces[faceIndex].textureGroup;
            if(textureGroup == -1)
            {
                return currentSelection;
            }

            MeshSelection selection = new MeshSelection();
            IList<Face> faces = mesh.faces;
            List<int> selectedFaces = new List<int>();
            for (int i = 0; i < faces.Count; ++i)
            {
                Face face = faces[i];
                if (face.textureGroup == textureGroup /*&& !facesHs.Contains(i)*/)
                {
                    selectedFaces.Add(i);
                }
            }

            selection.SelectedFaces.Add(mesh.gameObject, selectedFaces);
            return selection;
        }
    }
}
