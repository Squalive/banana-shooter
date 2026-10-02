using System.Collections.Generic;
using CodingDaniel.MapEditor.MECommon;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CodingDaniel.MapEditor.Interaction.ObjectGizmos
{
    /// <summary>
    /// The built-in object gizmos.
    ///
    /// Every entry here replaces a MonoBehaviour that used to exist for it:
    /// light.point (PointLightGizmo), light.spot (SpotlightGizmo), light.directional
    /// (DirectionalLightGizmo), decal.projector (DecalGizmo) and audio.source (AudioSourceGizmo).
    /// The LightGizmo dispatcher is replaced by resolving the definition from Light.type.
    /// </summary>
    public static class ObjectGizmoRegistry
    {
        // Order is the resolution priority for a GameObject that carries several gizmo components;
        // it mirrors the branch order ComponentMenu used (light, then decal, then audio).
        private static readonly ObjectGizmoDefinition[] _definitions =
        {
            PointLight(),
            SpotLight(),
            DirectionalLight(),
            Decal(),
            AudioSource(),
        };

        public static IReadOnlyList<ObjectGizmoDefinition> Definitions
        {
            get { return _definitions; }
        }

        /// <summary>Resolves the definition for a specific component, or null if nothing applies.</summary>
        public static ObjectGizmoDefinition Resolve(Component component)
        {
            if (component == null)
            {
                return null;
            }

            for (int i = 0; i < _definitions.Length; ++i)
            {
                ObjectGizmoDefinition definition = _definitions[i];
                if (definition.ComponentType == null || !definition.ComponentType.IsInstanceOfType(component))
                {
                    continue;
                }

                if (definition.Applies != null && !definition.Applies(component))
                {
                    continue;
                }

                return definition;
            }

            return null;
        }

        public static ObjectGizmoDefinition ResolveById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < _definitions.Length; ++i)
            {
                if (_definitions[i].Id == id)
                {
                    return _definitions[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the component on a GameObject that a gizmo should attach to. Used when the behaviour
        /// is added without going through <see cref="ObjectGizmoBehaviour.Attach"/>.
        /// </summary>
        public static Component FindComponent(GameObject gameObject, out ObjectGizmoDefinition definition)
        {
            definition = null;
            if (gameObject == null)
            {
                return null;
            }

            for (int i = 0; i < _definitions.Length; ++i)
            {
                ObjectGizmoDefinition candidate = _definitions[i];
                if (candidate.ComponentType == null)
                {
                    continue;
                }

                Component component = gameObject.GetComponent(candidate.ComponentType);
                if (component == null)
                {
                    continue;
                }

                if (candidate.Applies != null && !candidate.Applies(component))
                {
                    continue;
                }

                definition = candidate;
                return component;
            }

            return null;
        }

        // -----------------------------------------------------------------------------------------
        // Definitions
        // -----------------------------------------------------------------------------------------

        private static ObjectGizmoDefinition PointLight()
        {
            return new ObjectGizmoDefinition
            {
                Id = "light.point",
                ComponentType = typeof(Light),
                Applies = c => ((Light)c).type == LightType.Point,
                Geometry = ObjectGizmoGeometry.Sphere,
                RefreshOnCameraChanged = true,
                Colors = GizmoColors.Warm,
                Radius = c => ((Light)c).range,
                ReadPrimary = c => ((Light)c).range,
                WritePrimary = (c, v) => ((Light)c).range = v,
                UndoMembers = _ => new[] { Strong.PropertyInfo((Light x) => x.range, "range") },
                UiBindings = new[] { Binding("range", c => ((Light)c).range) },
            };
        }

        private static ObjectGizmoDefinition SpotLight()
        {
            return new ObjectGizmoDefinition
            {
                Id = "light.spot",
                ComponentType = typeof(Light),
                Applies = c => ((Light)c).type == LightType.Spot,
                Geometry = ObjectGizmoGeometry.Cone,
                RefreshOnCameraChanged = true,
                Colors = GizmoColors.Warm,
                Height = c => ((Light)c).range,
                Radius = c => SpotRadius((Light)c),
                ReadPrimary = c => ((Light)c).range,
                WritePrimary = (c, v) => ((Light)c).range = v,
                ReadSecondary = c => SpotRadius((Light)c),
                WriteSecondary = (c, radius) =>
                {
                    Light light = (Light)c;
                    light.spotAngle = Mathf.Atan2(radius, light.range) * Mathf.Rad2Deg * 2f;
                    light.innerSpotAngle = light.spotAngle * 0.8f;
                },
                // The radius writer also changes innerSpotAngle, so all three are recorded; the old
                // SpotlightGizmo recorded only range and spotAngle and could not undo the inner angle.
                UndoMembers = _ => new[]
                {
                    Strong.PropertyInfo((Light x) => x.range, "range"),
                    Strong.PropertyInfo((Light x) => x.spotAngle, "spotAngle"),
                    Strong.PropertyInfo((Light x) => x.innerSpotAngle, "innerSpotAngle"),
                },
                UiBindings = new[]
                {
                    Binding("range", c => ((Light)c).range),
                    Binding("spotangle", c => ((Light)c).spotAngle),
                    Binding("innerspotangle", c => ((Light)c).innerSpotAngle),
                },
            };
        }

        private static ObjectGizmoDefinition DirectionalLight()
        {
            return new ObjectGizmoDefinition
            {
                Id = "light.directional",
                ComponentType = typeof(Light),
                Applies = c => ((Light)c).type == LightType.Directional,
                Geometry = ObjectGizmoGeometry.DirectionalLight,
                RefreshOnCameraChanged = true,
                Colors = GizmoColors.Warm,
                // The old gizmo accepted a drag and then did nothing with it, while still opening an
                // undo record. Display-only is the honest description.
                AllowDrag = false,
            };
        }

        private static ObjectGizmoDefinition Decal()
        {
            return new ObjectGizmoDefinition
            {
                Id = "decal.projector",
                ComponentType = typeof(DecalProjector),
                Geometry = ObjectGizmoGeometry.Box,
                Colors = GizmoColors.Neutral,
                // DecalGizmo never wired input and had no drag path; the box is sized from the panel.
                AllowDrag = false,
                LocalBounds = c =>
                {
                    DecalProjector decal = (DecalProjector)c;
                    return new Bounds(decal.pivot, decal.size);
                },
            };
        }

        private static ObjectGizmoDefinition AudioSource()
        {
            return new ObjectGizmoDefinition
            {
                Id = "audio.source",
                ComponentType = typeof(AudioSource),
                Geometry = ObjectGizmoGeometry.Sphere,
                RefreshOnCameraChanged = true,
                Colors = GizmoColors.Audio,
                Radius = c => ((AudioSource)c).minDistance,
                ReadPrimary = c => ((AudioSource)c).minDistance,
                WritePrimary = (c, v) =>
                {
                    AudioSource source = (AudioSource)c;
                    source.minDistance = v;
                    source.maxDistance = v * 3f;
                },
                // maxDistance is written above, so it is recorded too; the old AudioSourceGizmo
                // recorded only minDistance.
                UndoMembers = _ => new[]
                {
                    Strong.PropertyInfo((AudioSource x) => x.minDistance, "minDistance"),
                    Strong.PropertyInfo((AudioSource x) => x.maxDistance, "maxDistance"),
                },
                UiBindings = new[]
                {
                    Binding("minDistance", c => ((AudioSource)c).minDistance),
                    Binding("maxDistance", c => ((AudioSource)c).maxDistance),
                },
            };
        }

        private static float SpotRadius(Light light)
        {
            return light.range * Mathf.Tan(Mathf.Deg2Rad * light.spotAngle / 2f);
        }

        private static ObjectGizmoDefinition.UiBinding Binding(string key, System.Func<Component, float> current)
        {
            return new ObjectGizmoDefinition.UiBinding { Key = key, Current = current };
        }
    }
}
