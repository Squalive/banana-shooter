using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MECommon;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodingDaniel.MapEditor.Interaction.Rendering
{
    /// <summary>
    /// Draws the move, rotate and scale handles into a command buffer.
    ///
    /// These three drawers were methods on MEHandleComponent. They now take the appearance they read
    /// (palette, handle scale, selection margin, Z direction, arrow-only mode) and the geometry and
    /// material libraries they draw with, instead of reaching for a singleton.
    ///
    /// Drawing maths is unchanged from the original.
    /// </summary>
    public sealed class TransformHandleRenderer
    {
        private readonly IHandleAppearance _appearance;
        private readonly GizmoGeometryLibrary _geometry;
        private readonly GizmoMaterialSet _materials;

        public TransformHandleRenderer(IHandleAppearance appearance, GizmoGeometryLibrary geometry, GizmoMaterialSet materials)
        {
            _appearance = appearance;
            _geometry = geometry;
            _materials = materials;
        }

        private GizmoPalette Palette => _appearance.Palette;
        private float HandleScale => _appearance.HandleScale;
        private bool InvertZAxis => _appearance.InvertZAxis;
        private bool PositionHandleArrowOnly => _appearance.PositionHandleArrowOnly;
        private Vector3 Forward => _appearance.Forward;

        private static float GetScreenScale(Vector3 position, Camera camera)
        {
            return GraphicsUtility.GetScreenScale(position, camera);
        }

        private Mesh Axes => _geometry != null ? _geometry.Axes : null;
        private Mesh Arrows => _geometry != null ? _geometry.Arrows : null;
        private Mesh ArrowY => _geometry != null ? _geometry.ArrowY : null;
        private Mesh ArrowX => _geometry != null ? _geometry.ArrowX : null;
        private Mesh ArrowZ => _geometry != null ? _geometry.ArrowZ : null;
        private Mesh SelectionArrowY => _geometry != null ? _geometry.SelectionArrowY : null;
        private Mesh SelectionArrowX => _geometry != null ? _geometry.SelectionArrowX : null;
        private Mesh SelectionArrowZ => _geometry != null ? _geometry.SelectionArrowZ : null;
        private Mesh DisabledArrowY => _geometry != null ? _geometry.DisabledArrowY : null;
        private Mesh DisabledArrowX => _geometry != null ? _geometry.DisabledArrowX : null;
        private Mesh DisabledArrowZ => _geometry != null ? _geometry.DisabledArrowZ : null;
        private Mesh Quads => _geometry != null ? _geometry.Quads : null;
        private Mesh WireQuads => _geometry != null ? _geometry.WireQuads : null;
        private Mesh Quad => _geometry != null ? _geometry.Quad : null;
        private Mesh SelectionCube => _geometry != null ? _geometry.SelectionCube : null;
        private Mesh DisabledCube => _geometry != null ? _geometry.DisabledCube : null;
        private Mesh CubeX => _geometry != null ? _geometry.CubeX : null;
        private Mesh CubeY => _geometry != null ? _geometry.CubeY : null;
        private Mesh CubeZ => _geometry != null ? _geometry.CubeZ : null;
        private Mesh CubeUniform => _geometry != null ? _geometry.CubeUniform : null;
        private Mesh WireCircle => _geometry != null ? _geometry.WireCircle : null;
        private Mesh WireCircle11 => _geometry != null ? _geometry.WireCircle11 : null;

        private Material _shapesMaterialZTest => _materials != null ? _materials.ShapesZTest : null;
        private Material _shapesMaterialZTest2 => _materials != null ? _materials.ShapesZTest2 : null;
        private Material _shapesMaterialZTest3 => _materials != null ? _materials.ShapesZTest3 : null;
        private Material _shapesMaterialZTest4 => _materials != null ? _materials.ShapesZTest4 : null;
        private Material _shapesMaterialZTestOffset => _materials != null ? _materials.ShapesZTestOffset : null;
        private Material _linesMaterial => _materials != null ? _materials.Lines : null;
        private Material _linesClipMaterial => _materials != null ? _materials.LinesClip : null;
        private Material _linesClipUsingClipPlaneMaterial => _materials != null ? _materials.LinesClipUsingClipPlane : null;
        private Material _linesBillboardMaterial => _materials != null ? _materials.LinesBillboard : null;
        private Material _unlitColorMaterial => _materials != null ? _materials.UnlitColor : null;

        public void DoPositionHandle(CommandBuffer commandBuffer, Camera camera, HandleDrawSettings settings, bool snapMode = false)
        {
            settings.Init(propertyBlocksCount: 11);

            MaterialPropertyBlock[] propertyBlocks = settings.PropertyBlocks;
            LockObject lockObject = settings.LockObject;
            HandleAxis selectedAxis = settings.SelectedAxis;

            bool drawLocked = settings.DrawLocked;
            bool xLocked = lockObject != null && lockObject.PositionX;
            bool yLocked = lockObject != null && lockObject.PositionY;
            bool zLocked = lockObject != null && lockObject.PositionZ;
            
            float screenScale = GetScreenScale(settings.Position, camera);
            Matrix4x4 linesTransform = Matrix4x4.TRS(settings.Position, settings.Rotation, new Vector3(screenScale, screenScale, screenScale) * HandleScale);
            DoAxes(commandBuffer, propertyBlocks, linesTransform, selectedAxis, xLocked, yLocked, zLocked, drawLocked);

            Matrix4x4 transform = Matrix4x4.TRS(settings.Position, settings.Rotation, new Vector3(screenScale, screenScale, screenScale));
            if (snapMode)
            {
                if (selectedAxis == HandleAxis.Snap)
                {
                    propertyBlocks[4].SetColor("_Color", Palette.SelectionColor);
                }
                else
                {
                    propertyBlocks[4].SetColor("_Color", Palette.AltColor);
                }

                commandBuffer.DrawMesh(Quad, transform, _linesBillboardMaterial, 0, 0, propertyBlocks[4]);
            }
            else
            {
                if(!PositionHandleArrowOnly)
                {
                    Vector3 toCam = transform.inverse.MultiplyVector(camera.transform.position - settings.Position);
                    Matrix4x4 quadTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                        new Vector3(
                            Mathf.Sign(Vector3.Dot(toCam, Vector3.right)) * 0.2f, 
                            Mathf.Sign(Vector3.Dot(toCam, Vector3.up)) * 0.2f, 
                            Mathf.Sign(Vector3.Dot(toCam, Vector3.forward)) * 0.2f));

                    Matrix4x4 matrix = linesTransform * quadTransform;

                    if (!xLocked && !yLocked)
                    {
                        Color32 color = Palette.ZColor;
                        color.a = 128;
                        propertyBlocks[5].SetColor("_Color", selectedAxis != HandleAxis.XY ? color : Palette.SelectionColor);
                        commandBuffer.DrawMesh(Quads, matrix, _unlitColorMaterial, 0, 0, propertyBlocks[5]);

                        propertyBlocks[6].SetColor("_Color", selectedAxis != HandleAxis.XY ? Palette.ZColor : Palette.SelectionColor);
                        commandBuffer.DrawMesh(WireQuads, matrix, _linesMaterial, 0, 0, propertyBlocks[6]);
                    }

                    if (!xLocked && !zLocked)
                    {
                        Color32 color = Palette.YColor;
                        color.a = 128;
                        propertyBlocks[7].SetColor("_Color", selectedAxis != HandleAxis.XZ ? color : Palette.SelectionColor);
                        commandBuffer.DrawMesh(Quads, matrix, _unlitColorMaterial, 1, 0, propertyBlocks[7]);

                        propertyBlocks[8].SetColor("_Color", selectedAxis != HandleAxis.XZ ? Palette.YColor : Palette.SelectionColor);
                        commandBuffer.DrawMesh(WireQuads, matrix, _linesMaterial, 1, 0, propertyBlocks[8]);
                    }

                    if (!yLocked && !zLocked)
                    {
                        Color32 color = Palette.XColor;
                        color.a = 128;
                        propertyBlocks[9].SetColor("_Color", selectedAxis != HandleAxis.YZ ? color : Palette.SelectionColor);
                        commandBuffer.DrawMesh(Quads, matrix, _unlitColorMaterial, 2, 0, propertyBlocks[9]);

                        propertyBlocks[10].SetColor("_Color", selectedAxis != HandleAxis.YZ ? Palette.XColor : Palette.SelectionColor);
                        commandBuffer.DrawMesh(WireQuads, matrix, _linesMaterial, 2, 0, propertyBlocks[10]);
                    }
                }
            }

            if (!xLocked && !yLocked && !zLocked)
            {
                commandBuffer.DrawMesh(Arrows, transform, _shapesMaterialZTest, 0, 0);
                if ((selectedAxis & HandleAxis.X) != 0)
                {
                    commandBuffer.DrawMesh(SelectionArrowX, transform, _shapesMaterialZTest, 0, 0);
                }
                if ((selectedAxis & HandleAxis.Y) != 0)
                {
                    commandBuffer.DrawMesh(SelectionArrowY, transform, _shapesMaterialZTest, 0, 0);
                }
                if ((selectedAxis & HandleAxis.Z) != 0)
                {
                    commandBuffer.DrawMesh(SelectionArrowZ, transform, _shapesMaterialZTest, 0, 0);
                }
            }
            else
            {
                if (xLocked)
                {
                    if(drawLocked)
                    {
                        commandBuffer.DrawMesh(DisabledArrowX, transform, _shapesMaterialZTest, 0, 0);
                    }
                }
                else
                {
                    if ((selectedAxis & HandleAxis.X) != 0)
                    {
                        commandBuffer.DrawMesh(SelectionArrowX, transform, _shapesMaterialZTest, 0, 0);
                    }
                    else
                    {
                        commandBuffer.DrawMesh(ArrowX, transform, _shapesMaterialZTest, 0, 0);
                    }
                }

                if (yLocked)
                {
                    if(drawLocked)
                    {
                        commandBuffer.DrawMesh(DisabledArrowY, transform, _shapesMaterialZTest, 0, 0);
                    }
                }
                else 
                {
                    if ((selectedAxis & HandleAxis.Y) != 0)
                    {
                        commandBuffer.DrawMesh(SelectionArrowY, transform, _shapesMaterialZTest, 0, 0); 
                    }
                    else
                    {
                        commandBuffer.DrawMesh(ArrowY, transform, _shapesMaterialZTest, 0, 0);
                    }     
                }

                if (zLocked)
                {
                    if(drawLocked)
                    {
                        commandBuffer.DrawMesh(DisabledArrowZ, transform, _shapesMaterialZTest, 0, 0);
                    }
                }
                else 
                {
                    if ((selectedAxis & HandleAxis.Z) != 0)
                    {
                        commandBuffer.DrawMesh(SelectionArrowZ, transform, _shapesMaterialZTest, 0, 0);
                    }
                    else
                    {
                        commandBuffer.DrawMesh(ArrowZ, transform, _shapesMaterialZTest, 0, 0);
                    }    
                }
            }
        }
        private void DoAxes(CommandBuffer commandBuffer, MaterialPropertyBlock[] propertyBlocks, Matrix4x4 transform, HandleAxis selectedAxis, bool xLocked, bool yLocked, bool zLocked, bool drawLocked)
        {
            if (xLocked)
            {
                if(drawLocked && Palette.DisabledColor.a > 0)
                {
                    propertyBlocks[0].SetColor("_Color", Palette.DisabledColor);
                    commandBuffer.DrawMesh(Axes, transform, _linesMaterial, 0, 0, propertyBlocks[0]);
                }
            }
            else
            {
                propertyBlocks[0].SetColor("_Color", (selectedAxis & HandleAxis.X) == 0 ? Palette.XColor : Palette.SelectionColor);
                commandBuffer.DrawMesh(Axes, transform, _linesMaterial, 0, 0, propertyBlocks[0]);
            }

            if (yLocked)
            {
                if(drawLocked && Palette.DisabledColor.a > 0)
                {
                    propertyBlocks[1].SetColor("_Color", Palette.DisabledColor);
                    commandBuffer.DrawMesh(Axes, transform, _linesMaterial, 1, 0, propertyBlocks[1]);
                }
            }
            else
            {
                propertyBlocks[1].SetColor("_Color", (selectedAxis & HandleAxis.Y) == 0 ? Palette.YColor : Palette.SelectionColor);
                commandBuffer.DrawMesh(Axes, transform, _linesMaterial, 1, 0, propertyBlocks[1]);
            }

            if (zLocked)
            {
                if(drawLocked && Palette.DisabledColor.a > 0)
                {
                    propertyBlocks[2].SetColor("_Color", Palette.DisabledColor);
                    commandBuffer.DrawMesh(Axes, transform, _linesMaterial, 2, 0, propertyBlocks[2]);
                }
            }
            else
            {
                propertyBlocks[2].SetColor("_Color", (selectedAxis & HandleAxis.Z) == 0 ? Palette.ZColor : Palette.SelectionColor);
                commandBuffer.DrawMesh(Axes, transform, _linesMaterial, 2, 0, propertyBlocks[2]);
            }
        }

        public void DoRotationHandle(CommandBuffer commandBuffer, Camera camera, HandleDrawSettings settings, bool cameraFacingBillboardMode = true)
        {
            settings.Init(propertyBlocksCount: 5);

            MaterialPropertyBlock[] propertyBlocks = settings.PropertyBlocks;
            LockObject lockObject = settings.LockObject;
            HandleAxis selectedAxis = settings.SelectedAxis;

            float screenScale = GetScreenScale(settings.Position, camera);
            float radius = HandleScale;
            Vector3 scale = Vector3.Scale(new Vector3(screenScale, screenScale, screenScale) * radius, settings.Scale);
            Matrix4x4 xTranform = Matrix4x4.TRS(Vector3.zero, settings.Rotation * Quaternion.AngleAxis(-90, Vector3.up), Vector3.one);
            Matrix4x4 yTranform = Matrix4x4.TRS(Vector3.zero, settings.Rotation * Quaternion.AngleAxis(-90, Vector3.right), Vector3.one);
            Matrix4x4 zTranform = Matrix4x4.TRS(Vector3.zero, settings.Rotation, Vector3.one);
            Matrix4x4 objToWorld = Matrix4x4.TRS(settings.Position, Quaternion.identity, scale);

            bool drawLocked = settings.DrawLocked;
            bool xLocked = lockObject != null && lockObject.RotationX;
            bool yLocked = lockObject != null && lockObject.RotationY;
            bool zLocked = lockObject != null && lockObject.RotationZ;
            bool freeLocked = lockObject != null && lockObject.RotationFree;
            bool screenLocked = lockObject != null && lockObject.RotationScreen;

            Matrix4x4 matrix;
            Material material;

            if (cameraFacingBillboardMode)
            {
                material = _linesMaterial;
                matrix = Matrix4x4.TRS(settings.Position, Quaternion.LookRotation(camera.transform.position - settings.Position), scale);
            }
            else
            {
                material = _linesBillboardMaterial;
                matrix = objToWorld;
            }

            if (freeLocked)
            {
                if(drawLocked)
                {
                    propertyBlocks[0].SetColor("_Color", Palette.DisabledColor);
                }
            }
            else
            {
                propertyBlocks[0].SetColor("_Color", selectedAxis != HandleAxis.Free ? Palette.AltColor : Palette.SelectionColor);
            }
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, matrix, material, propertyBlocks[0]);

            if (screenLocked)
            {
                if (drawLocked)
                {
                    propertyBlocks[1].SetColor("_Color", Palette.DisabledColor);
                }
            }
            else
            {
                propertyBlocks[1].SetColor("_Color", selectedAxis != HandleAxis.Screen ? Palette.AltColor : Palette.SelectionColor);
            }
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle11, matrix, material, propertyBlocks[1]);

            if(cameraFacingBillboardMode)
            {
                material = _linesClipUsingClipPlaneMaterial;
            }
            else
            {
                material = _linesClipMaterial;
            }
            
            if(xLocked)
            {
                if (drawLocked)
                {
                    propertyBlocks[2].SetColor("_Color", Palette.DisabledColor);
                }
            }
            else
            {
                propertyBlocks[2].SetColor("_Color", selectedAxis != HandleAxis.X ? Palette.XColor : Palette.SelectionColor);
            }   
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, objToWorld * xTranform, material, propertyBlocks[2]);

            if (yLocked)
            {
                if(drawLocked)
                {
                    propertyBlocks[3].SetColor("_Color", Palette.DisabledColor);
                }
            }
            else
            {
                propertyBlocks[3].SetColor("_Color", selectedAxis != HandleAxis.Y ? Palette.YColor : Palette.SelectionColor);
            }
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, objToWorld * yTranform, material, propertyBlocks[3]);
            
            if (zLocked)
            {
                if (drawLocked)
                {
                    propertyBlocks[4].SetColor("_Color", Palette.DisabledColor);
                }
            }
            else
            {
                propertyBlocks[4].SetColor("_Color", selectedAxis != HandleAxis.Z ? Palette.ZColor : Palette.SelectionColor);
            }
            GraphicsUtility.DrawMesh(commandBuffer, WireCircle, objToWorld * zTranform, material, propertyBlocks[4]);
        }

        public void DoScaleHandle(CommandBuffer commandBuffer, Camera camera, HandleDrawSettings settings)
        {
            settings.Init(propertyBlocksCount: 3);

            MaterialPropertyBlock[] propertyBlocks = settings.PropertyBlocks;
            LockObject lockObject = settings.LockObject;
            HandleAxis selectedAxis = settings.SelectedAxis;
            Vector3 position = settings.Position;
            Quaternion rotation = settings.Rotation;
            Vector3 scale = settings.Scale;

            float sScale = GetScreenScale(position, camera);
            Matrix4x4 linesTransform = Matrix4x4.TRS(position, rotation, scale * sScale * HandleScale);

            bool drawLocked = settings.DrawLocked;
            bool xLocked = lockObject != null && lockObject.ScaleX;
            bool yLocked = lockObject != null && lockObject.ScaleY;
            bool zLocked = lockObject != null && lockObject.ScaleZ;
            bool xyzLocked = xLocked && yLocked && zLocked;

            DoAxes(commandBuffer, propertyBlocks, linesTransform, selectedAxis, xLocked, yLocked, zLocked, drawLocked);
                     
            Matrix4x4 rotM = Matrix4x4.TRS(Vector3.zero, rotation, scale);
            Vector3 screenScale = new Vector3(sScale, sScale, sScale);
            Vector3 xOffset = rotM.MultiplyVector(Vector3.right) * sScale * HandleScale;
            Vector3 yOffset = rotM.MultiplyVector(Vector3.up) * sScale * HandleScale;
            Vector3 zOffset = rotM.MultiplyPoint(Forward) * sScale * HandleScale;

            drawLocked = drawLocked && Palette.DisabledColor.a > 0;
            if (selectedAxis == HandleAxis.X)
            {  
                DrawMesh(commandBuffer, drawLocked || !xLocked, xLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position + xOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !yLocked, yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !zLocked, zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !xyzLocked, xyzLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), _shapesMaterialZTest);
            }
            else if (selectedAxis == HandleAxis.Y)
            {
                DrawMesh(commandBuffer, drawLocked || !xLocked, xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !yLocked, yLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position + yOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !zLocked, zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !xyzLocked, xyzLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), _shapesMaterialZTest);
            }
            else if (selectedAxis == HandleAxis.Z)
            {
                DrawMesh(commandBuffer, drawLocked || !xLocked, xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !yLocked, yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !zLocked, zLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position + zOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !xyzLocked, xyzLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), _shapesMaterialZTest);
            }
            else if (selectedAxis == HandleAxis.Free)
            {
                DrawMesh(commandBuffer, drawLocked || !xLocked, xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !yLocked, yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !zLocked, zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !xyzLocked, xyzLocked ? DisabledCube : SelectionCube, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), _shapesMaterialZTest);
            }
            else
            {
                DrawMesh(commandBuffer, drawLocked || !xLocked, xLocked ? DisabledCube : CubeX, Matrix4x4.TRS(position + xOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !yLocked, yLocked ? DisabledCube : CubeY, Matrix4x4.TRS(position + yOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !zLocked, zLocked ? DisabledCube : CubeZ, Matrix4x4.TRS(position + zOffset, rotation, screenScale), _shapesMaterialZTest);
                DrawMesh(commandBuffer, drawLocked || !xyzLocked, xyzLocked ? DisabledCube : CubeUniform, Matrix4x4.TRS(position, rotation, screenScale * 1.35f), _shapesMaterialZTest);
            }
        }
        private void DrawMesh(CommandBuffer commandBuffer, bool draw, Mesh mesh, Matrix4x4 matrix, Material material)
        {
            if(draw)
            {
                commandBuffer.DrawMesh(mesh, matrix, material, 0, 0);
            }
        }
        
    }
}