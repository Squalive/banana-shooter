using System;
using CodingDaniel.MapEditor.Graphics;
using UnityEngine;

namespace CodingDaniel.MapEditor.Interaction.Rendering
{
    /// <summary>
    /// The meshes the transform handles draw with.
    ///
    /// Extracted from MEHandleComponent, which built all of these inline in Initialize and destroyed
    /// them in Cleanup. The arrow and cube meshes bake the palette colours in, so the library is built
    /// from a palette snapshot and rebuilt when the handle scale or Z-axis direction changes.
    /// </summary>
    public sealed class GizmoGeometryLibrary : IDisposable
    {
        public Mesh Axes { get; private set; }
        public Mesh Arrows { get; private set; }
        public Mesh ArrowY { get; private set; }
        public Mesh ArrowX { get; private set; }
        public Mesh ArrowZ { get; private set; }
        public Mesh SelectionArrowY { get; private set; }
        public Mesh SelectionArrowX { get; private set; }
        public Mesh SelectionArrowZ { get; private set; }
        public Mesh DisabledArrowY { get; private set; }
        public Mesh DisabledArrowX { get; private set; }
        public Mesh DisabledArrowZ { get; private set; }
        public Mesh Quads { get; private set; }
        public Mesh WireQuads { get; private set; }
        public Mesh Quad { get; private set; }
        public Mesh SelectionCube { get; private set; }
        public Mesh DisabledCube { get; private set; }
        public Mesh CubeX { get; private set; }
        public Mesh CubeY { get; private set; }
        public Mesh CubeZ { get; private set; }
        public Mesh CubeUniform { get; private set; }
        public Mesh WireCircle { get; private set; }
        public Mesh WireCircle11 { get; private set; }

        public GizmoGeometryLibrary(GizmoPalette palette, float handleScale, bool invertZ)
        {
            Vector3 forward = invertZ ? Vector3.back : Vector3.forward;

            Axes = CreateAxes(forward);

            Mesh selectionArrowMesh = GraphicsUtility.CreateCone(palette.SelectionColor, handleScale);
            Mesh disableArrowMesh = GraphicsUtility.CreateCone(palette.DisabledColor, handleScale);

            CombineInstance yArrow = new CombineInstance();
            yArrow.mesh = selectionArrowMesh;
            yArrow.transform = Matrix4x4.TRS(Vector3.up * handleScale, Quaternion.identity, Vector3.one);
            SelectionArrowY = Combine(yArrow);

            yArrow.mesh = disableArrowMesh;
            yArrow.transform = Matrix4x4.TRS(Vector3.up * handleScale, Quaternion.identity, Vector3.one);
            DisabledArrowY = Combine(yArrow);

            yArrow.mesh = GraphicsUtility.CreateCone(palette.YColor, handleScale);
            yArrow.transform = Matrix4x4.TRS(Vector3.up * handleScale, Quaternion.identity, Vector3.one);
            ArrowY = Combine(yArrow);

            CombineInstance xArrow = new CombineInstance();
            xArrow.mesh = selectionArrowMesh;
            xArrow.transform = Matrix4x4.TRS(Vector3.right * handleScale, Quaternion.AngleAxis(-90, Vector3.forward), Vector3.one);
            SelectionArrowX = Combine(xArrow);

            xArrow.mesh = disableArrowMesh;
            xArrow.transform = Matrix4x4.TRS(Vector3.right * handleScale, Quaternion.AngleAxis(-90, Vector3.forward), Vector3.one);
            DisabledArrowX = Combine(xArrow);

            xArrow.mesh = GraphicsUtility.CreateCone(palette.XColor, handleScale);
            xArrow.transform = Matrix4x4.TRS(Vector3.right * handleScale, Quaternion.AngleAxis(-90, Vector3.forward), Vector3.one);
            ArrowX = Combine(xArrow);

            Vector3 zAxis = forward * handleScale;
            Quaternion zRotation = invertZ ? Quaternion.AngleAxis(-90, Vector3.right) : Quaternion.AngleAxis(90, Vector3.right);
            CombineInstance zArrow = new CombineInstance();
            zArrow.mesh = selectionArrowMesh;
            zArrow.transform = Matrix4x4.TRS(zAxis, zRotation, Vector3.one);
            SelectionArrowZ = Combine(zArrow);

            zArrow.mesh = disableArrowMesh;
            zArrow.transform = Matrix4x4.TRS(zAxis, zRotation, Vector3.one);
            DisabledArrowZ = Combine(zArrow);

            zArrow.mesh = GraphicsUtility.CreateCone(palette.ZColor, handleScale);
            zArrow.transform = Matrix4x4.TRS(zAxis, zRotation, Vector3.one);
            ArrowZ = Combine(zArrow);

            // The three arrows are combined again into the single multi-submesh mesh the axis drawer uses.
            yArrow.mesh = GraphicsUtility.CreateCone(palette.YColor, handleScale);
            xArrow.mesh = GraphicsUtility.CreateCone(palette.XColor, handleScale);
            zArrow.mesh = GraphicsUtility.CreateCone(palette.ZColor, handleScale);
            Arrows = new Mesh();
            Arrows.CombineMeshes(new[] { yArrow, xArrow, zArrow }, true);
            Arrows.RecalculateNormals();

            Quad = GraphicsUtility.CreateWireQuad(0.2f * handleScale, 0.2f * handleScale);
            Quads = CreatePositionHandleQuads();
            WireQuads = CreatePositionHandleWireQuads();

            SelectionCube = GraphicsUtility.CreateCube(palette.SelectionColor, Vector3.zero, handleScale, 0.1f, 0.1f, 0.1f);
            DisabledCube = GraphicsUtility.CreateCube(palette.DisabledColor, Vector3.zero, handleScale, 0.1f, 0.1f, 0.1f);
            CubeX = GraphicsUtility.CreateCube(palette.XColor, Vector3.zero, handleScale, 0.1f, 0.1f, 0.1f);
            CubeY = GraphicsUtility.CreateCube(palette.YColor, Vector3.zero, handleScale, 0.1f, 0.1f, 0.1f);
            CubeZ = GraphicsUtility.CreateCube(palette.ZColor, Vector3.zero, handleScale, 0.1f, 0.1f, 0.1f);
            CubeUniform = GraphicsUtility.CreateCube(palette.AltColor, Vector3.zero, handleScale, 0.1f, 0.1f, 0.1f);

            WireCircle = GraphicsUtility.CreateWireCircle();
            WireCircle11 = GraphicsUtility.CreateWireCircle(1.1f);
        }

        private static Mesh Combine(CombineInstance instance)
        {
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(new[] { instance }, true);
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateAxes(Vector3 forward)
        {
            Vector3 x = Vector3.right * 0.95f;
            Vector3 y = Vector3.up * 0.95f;
            Vector3 z = forward * 0.95f;

            Mesh mesh = new Mesh();
            mesh.subMeshCount = 3;
            mesh.vertices = new[]
            {
                Vector3.zero,
                x,
                Vector3.zero,
                y,
                Vector3.zero,
                z
            };
            mesh.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0);
            mesh.SetIndices(new[] { 2, 3 }, MeshTopology.Lines, 1);
            mesh.SetIndices(new[] { 4, 5 }, MeshTopology.Lines, 2);
            return mesh;
        }

        private static Mesh CreatePositionHandleWireQuads()
        {
            Vector3 x = Vector3.right;
            Vector3 y = Vector3.up;
            Vector3 z = Vector3.forward;

            Vector3 xy = x + y;
            Vector3 xz = x + z;
            Vector3 yz = y + z;

            Mesh mesh = new Mesh();
            mesh.subMeshCount = 3;
            mesh.vertices = new[]
            {
                Vector3.zero,
                x,
                y,
                z,
                xy,
                xz,
                yz,
            };
            mesh.SetIndices(new[] { 0, 2, 2, 4, 4, 1, 1, 0 }, MeshTopology.Lines, 0);
            mesh.SetIndices(new[] { 0, 1, 1, 5, 5, 3, 3, 0 }, MeshTopology.Lines, 1);
            mesh.SetIndices(new[] { 0, 3, 3, 6, 6, 2, 2, 0 }, MeshTopology.Lines, 2);

            return mesh;
        }

        private static Mesh CreatePositionHandleQuads()
        {
            Vector3 x = Vector3.right;
            Vector3 y = Vector3.up;
            Vector3 z = Vector3.forward;

            Vector3 xy = x + y;
            Vector3 xz = x + z;
            Vector3 yz = y + z;

            Mesh mesh = new Mesh();
            mesh.subMeshCount = 3;
            mesh.vertices = new[]
            {
                Vector3.zero,
                x,
                y,
                z,
                xy,
                xz,
                yz,
            };
            mesh.SetIndices(new[] { 0, 2, 4, 1 }, MeshTopology.Quads, 0);
            mesh.SetIndices(new[] { 0, 1, 5, 3 }, MeshTopology.Quads, 1);
            mesh.SetIndices(new[] { 0, 3, 6, 2 }, MeshTopology.Quads, 2);

            return mesh;
        }

        public void Dispose()
        {
            Destroy(Axes);
            Destroy(Arrows);
            Destroy(ArrowY);
            Destroy(ArrowX);
            Destroy(ArrowZ);
            Destroy(SelectionArrowY);
            Destroy(SelectionArrowX);
            Destroy(SelectionArrowZ);
            Destroy(DisabledArrowY);
            Destroy(DisabledArrowX);
            Destroy(DisabledArrowZ);
            Destroy(Quads);
            Destroy(WireQuads);
            Destroy(Quad);
            Destroy(SelectionCube);
            Destroy(DisabledCube);
            Destroy(CubeX);
            Destroy(CubeY);
            Destroy(CubeZ);
            Destroy(CubeUniform);
            Destroy(WireCircle);
            Destroy(WireCircle11);

            Axes = null;
            Arrows = null;
            ArrowY = null;
            ArrowX = null;
            ArrowZ = null;
            SelectionArrowY = null;
            SelectionArrowX = null;
            SelectionArrowZ = null;
            DisabledArrowY = null;
            DisabledArrowX = null;
            DisabledArrowZ = null;
            Quads = null;
            WireQuads = null;
            Quad = null;
            SelectionCube = null;
            DisabledCube = null;
            CubeX = null;
            CubeY = null;
            CubeZ = null;
            CubeUniform = null;
            WireCircle = null;
            WireCircle11 = null;
        }

        private static void Destroy(Mesh mesh)
        {
            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
            }
        }
    }
}
