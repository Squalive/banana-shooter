using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public struct MeshAndFace
    {
        public ProBuilderMesh mesh;
        public Face face;
    }

    public struct VertexPickerEntry
    {
        public ProBuilderMesh mesh;
        public float screenDistance;
        public Vector3 worldPosition;
        public int vertex;
    }

    public struct FaceRaycastHit
    {
        public float distance;
        public Vector3 point;
        public Vector3 normal;
        public int face;

        public FaceRaycastHit(float d, Vector3 p, Vector3 n, int f)
        {
            distance = d;
            point = p;
            normal = n;
            face = f;
        }
    }

    public static class PBUtility
    {
        private static readonly List<VertexPickerEntry> _nearestVertices = new List<VertexPickerEntry>();
        private static readonly List<Edge> _edges = new List<Edge>();

        public static GameObject PickObject(Camera camera, Vector2 mousePosition)
        {
            var ray = camera.ScreenPointToRay(mousePosition);
            RaycastHit result = Physics.RaycastAll(ray).OrderBy(hit => hit.distance).Where(hit => hit.collider.GetComponent<ProBuilderMesh>() != null).FirstOrDefault();
            if(result.collider == null)
            {
                return null;
            }

            return result.collider.gameObject;
        }

        public static MeshAndFace PickFace(Camera camera, Vector3 mousePosition)
        {
            MeshAndFace res = new MeshAndFace();
            GameObject go = PickObject(camera, mousePosition);

            if (go == null || !(res.mesh = go.GetComponent<ProBuilderMesh>()))
            {
                return res;
            }
                
            res.face = SelectionPicker.PickFace(camera, mousePosition, res.mesh);
            return res;
        }

        public static Dictionary<ProBuilderMesh, HashSet<Face>> PickFaces(Camera camera, Rect rect, Rect uiRect, GameObject[] gameObjects, bool depthTest)
        {
            try
            {
                if (depthTest)
                {
                    return PBSelectionPicker.Renderer.PickFacesInRect(camera, rect, gameObjects.Select(g => g.GetComponent<ProBuilderMesh>()).Where(pbm => pbm != null).ToArray(), Mathf.RoundToInt(uiRect.width), Mathf.RoundToInt(uiRect.height));
                }

                return SelectionPicker.PickFacesInRect(camera, rect, gameObjects.Select(g => g.GetComponent<ProBuilderMesh>()).Where(pbm => pbm != null).ToArray(), new PickerOptions { rectSelectMode = RectSelectMode.Partial, depthTest = false }, 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                return new Dictionary<ProBuilderMesh, HashSet<Face>>();
            }
        }

        public static Dictionary<ProBuilderMesh, HashSet<Edge>> PickEdges(Camera camera, Rect rect, Rect uiRootRect, GameObject[] gameObjects, bool depthTest)
        {
            return PBSelectionPicker.PickEdgesInRect(camera, rect, uiRootRect, gameObjects.Select(g => g.GetComponent<ProBuilderMesh>()).Where(pbm => pbm != null).ToArray(), new PickerOptions { rectSelectMode = RectSelectMode.Partial, depthTest = depthTest });
        }

        public static float PickEdge(Camera camera, Vector3 mousePosition, float maxDistance, GameObject pickedObject, IEnumerable<ProBuilderMesh> meshes, bool depthTest, ref SceneSelection selection)
        {
            selection.Clear();
            selection.gameObject = pickedObject;
            ProBuilderMesh hoveredMesh = selection.gameObject != null ? selection.gameObject.GetComponent<ProBuilderMesh>() : null;

            float bestDistance = maxDistance;
            float unselectedBestDistance = maxDistance;
            
            if(hoveredMesh != null)
            {
                EdgeAndDistance tup = GetNearestEdgeOnMesh(camera, hoveredMesh, mousePosition);

                if (tup.edge.IsValid() && tup.distance < maxDistance)
                {
                    selection.gameObject = hoveredMesh.gameObject;
                    selection.mesh = hoveredMesh;
#if PROBUILDER_4_4_0_OR_NEWER
                    selection.SetSingleEdge(tup.edge);
#else
                    selection.edge = tup.edge;
#endif
                    unselectedBestDistance = tup.distance;

                    // if it's in the selection, it automatically wins as best. if not, treat this is a fallback.
                    //if (hoveredIsInSelection)
                    {
                        return tup.distance;
                    }
                }
            }

            ProBuilderMesh pickedMesh = pickedObject != null ? pickedObject.GetComponent<ProBuilderMesh>() : null;
            HashSet<ProBuilderMesh> hs = new HashSet<ProBuilderMesh>();
            foreach (ProBuilderMesh mesh in meshes)
            {
                if(!hs.Contains(mesh))
                {
                    hs.Add(mesh);
                }
            }

            if(pickedMesh != null && !hs.Contains(pickedMesh))
            {
                hs.Add(pickedMesh);
            }

            foreach (ProBuilderMesh mesh in hs)
            {
                Transform trs = mesh.transform;
                IList<Vector3> positions = mesh.positions;
                _edges.Clear();

                const float distMultiplier = 1.0f;

                foreach (Face face in mesh.faces)
                {
                    foreach (Edge edge in face.edges)
                    {
                        int x = edge.a;
                        int y = edge.b;

                        Vector3 projectedPoint;
                        Vector3 p0 = trs.TransformPoint(positions[x]);
                        Vector3 p1 = trs.TransformPoint(positions[y]);
                        float d = MathHelper.DistanceToLine(camera, mousePosition,
                                p0,
                                p1, out projectedPoint);

                        if(depthTest)
                        {
                            Ray ray = camera.ScreenPointToRay(projectedPoint);
                            Vector3 cpl0;
                            Vector3 cpl1;

                            if(MathHelper.ClosestPointsOnTwoLines(out cpl0, out cpl1, ray.origin, ray.direction, p0, p1 - p0))
                            {
                                if(PointIsOccluded(camera, mesh, cpl1))
                                {
                                    continue;
                                }
                            }
                        }
                       
                        d *= distMultiplier;

                        if (d == bestDistance)
                        {
                            _edges.Add(new Edge(x, y));
                        }
                        else if (d < bestDistance)
                        {
                            _edges.Clear();
                            _edges.Add(new Edge(x, y));

                            selection.gameObject = mesh.gameObject;
                            selection.mesh = mesh;
#if PROBUILDER_4_4_0_OR_NEWER
                            selection.SetSingleEdge(new Edge(x, y));
#else
                            selection.edge = new Edge(x, y);
#endif
                            bestDistance = d;
                        }
                    }
                }

                //If more than 1 edge is closest, the closest is one of the vertex.
                //Get closest edge to the camera.
                if (_edges.Count > 1)
                {
#if PROBUILDER_4_4_0_OR_NEWER
                    selection.SetSingleEdge(GetClosestEdgeToCamera(camera, mousePosition, positions, _edges));
#else
                    selection.edge = GetClosestEdgeToCamera(camera, mousePosition, positions, _edges);
#endif
                }
            }

            if (selection.gameObject != null)
            {
                if (bestDistance < maxDistance)
                {
                    return bestDistance;
                }

                return Mathf.Infinity;// unselectedBestDistance;
            }

            return Mathf.Infinity;
        }

        private static Edge GetClosestEdgeToCamera(Camera camera, Vector3 mousePosition, IList<Vector3> positions, IEnumerable<Edge> edges)
        {
            Vector3 camPos = camera.transform.position;
            float closestDistToScreen = Mathf.Infinity;
            Edge closest = default;

            foreach (Edge edge in edges)
            {
                var a = positions[edge.a];
                var b = positions[edge.b];
                var dir = (b - a).normalized * 0.01f;

                //Use a point that is close to the vertex on the edge but not on it,
                //otherwise we will have the same issue with every edge having the same distance to screen
                float dToScreen = Mathf.Min(
                    Vector3.Distance(camPos, a + dir),
                    Vector3.Distance(camPos, b - dir));

                if (dToScreen < closestDistToScreen)
                {
                    closestDistToScreen = dToScreen;
                    closest = edge;
                }
            }

            return closest;
        }

        private struct EdgeAndDistance
        {
            public Edge edge;
            public float distance;
        }

        private static EdgeAndDistance GetNearestEdgeOnMesh(Camera camera, ProBuilderMesh mesh, Vector3 mousePosition)
        {
            Ray ray = camera.ScreenPointToRay(mousePosition);

            var res = new EdgeAndDistance()
            {
                edge = Edge.Empty,
                distance = Mathf.Infinity
            };

            SimpleTuple<Face, Vector3> dualCullModeRaycastBackFace = new SimpleTuple<Face, Vector3>();
            SimpleTuple<Face, Vector3> dualCullModeRaycastFrontFace = new SimpleTuple<Face, Vector3>();

            // get the nearest hit face and point for both cull mode front and back, then prefer the result that is nearest the camera.
            if (FaceRaycastBothCullModes(ray, mesh, ref dualCullModeRaycastBackFace, ref dualCullModeRaycastFrontFace))
            {
                IList<Vector3> v = mesh.positions;

                if (dualCullModeRaycastBackFace.item1 != null)
                {
                    foreach (var edge in dualCullModeRaycastBackFace.item1.edges)
                    {
                        float d = MathHelper.DistanceToLine(dualCullModeRaycastBackFace.item2, v[edge.a], v[edge.b]);

                        if (d < res.distance)
                        {
                            res.edge = edge;
                            res.distance = d;
                        }
                    }
                }

                if (dualCullModeRaycastFrontFace.item1 != null)
                {
                    Vector3 a = mesh.transform.TransformPoint(dualCullModeRaycastBackFace.item2);
                    Vector3 b = mesh.transform.TransformPoint(dualCullModeRaycastFrontFace.item2);
                    Vector3 c = camera.transform.position;

                    if (Vector3.Distance(c, b) < Vector3.Distance(c, a))
                    {
                        foreach (var edge in dualCullModeRaycastFrontFace.item1.edges)
                        {
                            float d = MathHelper.DistanceToLine(dualCullModeRaycastFrontFace.item2, v[edge.a], v[edge.b]);

                            if (d < res.distance)
                            {
                                res.edge = edge;
                                res.distance = d;
                            }
                        }
                    }
                }

                if (res.edge.IsValid())
                {
                    res.distance = MathHelper.DistanceToLine(camera, mousePosition,
                        mesh.transform.TransformPoint(v[res.edge.a]),
                        mesh.transform.TransformPoint(v[res.edge.b]));
                }
            }

            return res;
        }

        private static bool FaceRaycastBothCullModes(Ray worldRay, ProBuilderMesh mesh, ref SimpleTuple<Face, Vector3> back, ref SimpleTuple<Face, Vector3> front)
        {
            // Transform ray into model space
            worldRay.origin -= mesh.transform.position; // Why doesn't worldToLocalMatrix apply translation?
            worldRay.origin = mesh.transform.worldToLocalMatrix * worldRay.origin;
            worldRay.direction = mesh.transform.worldToLocalMatrix * worldRay.direction;

            IList<Vector3> positions = mesh.positions;
            IList<Face> faces = mesh.faces;

            back.item1 = null;
            front.item1 = null;

            float backDistance = Mathf.Infinity;
            float frontDistance = Mathf.Infinity;

            // Iterate faces, testing for nearest hit to ray origin. Optionally ignores backfaces.
            for (int i = 0, fc = faces.Count; i < fc; ++i)
            {
                ReadOnlyCollection<int> indexes = faces[i].indexes;

                for (int j = 0, ic = indexes.Count; j < ic; j += 3)
                {
                    Vector3 a = positions[indexes[j + 0]];
                    Vector3 b = positions[indexes[j + 1]];
                    Vector3 c = positions[indexes[j + 2]];

                    float dist;
                    Vector3 point;

                    if (Math.RayIntersectsTriangle(worldRay, a, b, c, out dist, out point))
                    {
                        if (dist < backDistance || dist < frontDistance)
                        {
                            Vector3 nrm = Vector3.Cross(b - a, c - a);
                            float dot = Vector3.Dot(worldRay.direction, nrm);

                            if (dot < 0f)
                            {
                                if (dist < backDistance)
                                {
                                    backDistance = dist;
                                    back.item1 = faces[i];
                                }
                            }
                            else
                            {
                                if (dist < frontDistance)
                                {
                                    frontDistance = dist;
                                    front.item1 = faces[i];
                                }
                            }
                        }
                    }
                }
            }

            if (back.item1 != null)
                back.item2 = worldRay.GetPoint(backDistance);

            if (front.item1 != null)
                front.item2 = worldRay.GetPoint(frontDistance);

            return back.item1 != null || front.item1 != null;
        }

        public static Dictionary<ProBuilderMesh, HashSet<int>> PickVertices(Camera camera, Rect rect, Rect uiRootRect, GameObject[] gameObjects, bool depthTest)
        {
            return PBSelectionPicker.PickVerticesInRect(camera, rect, uiRootRect, gameObjects.Select(g => g.GetComponent<ProBuilderMesh>()).Where(pbm => pbm != null).ToArray(), new PickerOptions { rectSelectMode = RectSelectMode.Partial, depthTest = depthTest });
        }

        public static float PickVertex(Camera camera, Vector3 mousePosition, float maxDistance, GameObject pickedObject, IEnumerable<ProBuilderMesh> meshes, bool depthTest, ref SceneSelection selection)
        {
          //  selection.Clear();
            _nearestVertices.Clear();

            maxDistance = maxDistance * maxDistance;

            if(pickedObject != null)
            {
                ProBuilderMesh mesh = pickedObject.GetComponent<ProBuilderMesh>();
                if(mesh != null)
                {
                    GetNearestVertices(camera, mesh, mousePosition, _nearestVertices, maxDistance, 1.0f);
                }
            }

            foreach (ProBuilderMesh mesh in meshes)
            {
                if (!mesh.selectable)
                {
                    continue;
                }
                    
                GetNearestVertices(camera, mesh, mousePosition, _nearestVertices, maxDistance, 1.0f);
            }

            _nearestVertices.Sort((x, y) => x.screenDistance.CompareTo(y.screenDistance));

            ProBuilderMesh selectedMesh = selection.mesh;
#if PROBUILDER_4_4_0_OR_NEWER
            int selectedVertex = selection.vertexes.Count > 0 ? selection.vertexes[0] : -1;
#else
            int selectedVertex = selection.vertex;
#endif
            selection.Clear();

            int startIndex = 0;

            for (int i = 0; i < _nearestVertices.Count; i++)
            {
                VertexPickerEntry pickerEntry = _nearestVertices[i];
                if (pickerEntry.mesh == selectedMesh && pickerEntry.vertex == selectedVertex)
                {
                    startIndex = i + 1;
                    startIndex %= _nearestVertices.Count;
                }
            }
            
            for (int i = startIndex; i < _nearestVertices.Count; i++)
            {
                if (!depthTest || !PointIsOccluded(camera, _nearestVertices[i].mesh, _nearestVertices[i].worldPosition))
                {
                    selection.gameObject = _nearestVertices[i].mesh.gameObject;
                    selection.mesh = _nearestVertices[i].mesh;
#if PROBUILDER_4_4_0_OR_NEWER
                    selection.SetSingleVertex(_nearestVertices[i].vertex);
#else
                    selection.vertex = _nearestVertices[i].vertex;
#endif
                    return Mathf.Sqrt(_nearestVertices[i].screenDistance);
                }
            }

            return Mathf.Infinity;
        }

        private static int GetNearestVertices(Camera camera, ProBuilderMesh mesh, Vector3 mousePosition, List<VertexPickerEntry> list, float maxDistance, float distModifier)
        {
            IList<Vector3> positions = mesh.positions;
            IList<SharedVertex> common = mesh.sharedVertices;
            int matches = 0;

            for (int n = 0, c = common.Count; n < c; n++)
            {
                int index = common[n][0];
                Vector3 v = mesh.transform.TransformPoint(positions[index]);
                Vector3 p = camera.WorldToScreenPoint(v);
                p.z = mousePosition.z;

                float dist = (p - mousePosition).sqrMagnitude * distModifier;

                if (dist < maxDistance)
                {
                    list.Add(new VertexPickerEntry
                    {
                        mesh = mesh,
                        screenDistance = dist,
                        worldPosition = v,
                        vertex = index
                    });

                    matches++;
                }
            }

            return matches;
        }

        public static float PickVertex(Camera camera, Vector3 mousePosition, float maxDistance, Transform transform, IList<Vector3> positions, ref SceneSelection selection)
        {
            selection.Clear();
            _nearestVertices.Clear();

            maxDistance = maxDistance * maxDistance;

            GetNearestVertices(camera, transform, positions, mousePosition, _nearestVertices, maxDistance, 1.0f);
           
            _nearestVertices.Sort((x, y) => x.screenDistance.CompareTo(y.screenDistance));

            for (int i = 0; i < _nearestVertices.Count;)
            {
#if PROBUILDER_4_4_0_OR_NEWER
                selection.SetSingleVertex(_nearestVertices[i].vertex);
#else
                selection.vertex = _nearestVertices[i].vertex;
#endif
                return Mathf.Sqrt(_nearestVertices[i].screenDistance);
            }

            return Mathf.Infinity;
        }


        private static int GetNearestVertices(Camera camera, Transform transform, IList<Vector3> positions, Vector3 mousePosition, List<VertexPickerEntry> list, float maxDistance, float distModifier)
        {
            int matches = 0;

            for (int i = 0; i < positions.Count; ++i)
            {
                Vector3 v = transform.TransformPoint(positions[i]);
                Vector3 p = camera.WorldToScreenPoint(v);
                p.z = mousePosition.z;

                float dist = (p - mousePosition).sqrMagnitude * distModifier;

                if (dist < maxDistance)
                {
                    list.Add(new VertexPickerEntry
                    {
                        mesh = null,
                        screenDistance = dist,
                        worldPosition = v,
                        vertex = i
                    });

                    matches++;
                }
            }

            return matches;
        }

        public static bool PointIsOccluded(Camera cam, ProBuilderMesh pb, Vector3 worldPoint)
        {
            Vector3 dir = (cam.transform.position - worldPoint).normalized;

            // move the point slightly towards the camera to avoid colliding with its own triangle
            Ray ray = new Ray(worldPoint + dir * .0001f, dir);

            FaceRaycastHit hit;
            return FaceRaycast(ray, pb, out hit, Vector3.Distance(cam.transform.position, worldPoint), CullingMode.Front);
        }

        private static bool FaceRaycast(Ray worldRay, ProBuilderMesh mesh, out FaceRaycastHit hit, float distance, CullingMode cullingMode, HashSet<Face> ignore = null)
        {
            // Transform ray into model space
            worldRay.origin -= mesh.transform.position; // Why doesn't worldToLocalMatrix apply translation?
            worldRay.origin = mesh.transform.worldToLocalMatrix * worldRay.origin;
            worldRay.direction = mesh.transform.worldToLocalMatrix * worldRay.direction;

            IList<Vector3> positions = mesh.positions;
            IList<Face> faces = mesh.faces;

            float outHitPoint = Mathf.Infinity;
            int outHitFace = -1;
            Vector3 outNrm = Vector3.zero;

            // Iterate faces, testing for nearest hit to ray origin. Optionally ignores backfaces.
            for (int i = 0, fc = faces.Count; i < fc; ++i)
            {
                if (ignore != null && ignore.Contains(faces[i]))
                {
                    continue;
                }
                    
                ReadOnlyCollection<int> indexes = mesh.faces[i].indexes;

                for (int j = 0, ic = indexes.Count; j < ic; j += 3)
                {
                    Vector3 a = positions[indexes[j + 0]];
                    Vector3 b = positions[indexes[j + 1]];
                    Vector3 c = positions[indexes[j + 2]];

                    Vector3 nrm = Vector3.Cross(b - a, c - a);
                    float dot = Vector3.Dot(worldRay.direction, nrm);

                    bool skip = false;

                    switch (cullingMode)
                    {
                        case CullingMode.Front:
                            {
                                if (dot < 0f)
                                {
                                    skip = true;
                                }
                            }
                            break;

                        case CullingMode.Back:
                            {
                                if (dot > 0f)
                                {
                                    skip = true;
                                }
                            }
                            break;
                    }

                    float dist = 0f;
                    Vector3 point;
                    if (!skip && Math.RayIntersectsTriangle(worldRay, a, b, c, out dist, out point))
                    {
                        if (dist > outHitPoint || dist > distance)
                        {
                            continue;
                        }
                            
                        outNrm = nrm;
                        outHitFace = i;
                        outHitPoint = dist;
                    }
                }
            }

            hit = new FaceRaycastHit( outHitPoint,
                worldRay.GetPoint(outHitPoint),
                outNrm,
                outHitFace);

            return outHitFace > -1;
        }

        public static void BuildVertexMeshLegacy(IList<Vector3> positions, Color color, Mesh target, IList<int> indexes)
        {
            const int MaxPointCount = int.MaxValue / 4;

            bool update = false;
            if (indexes == null)
            {
                update = true;
                indexes = new int[positions.Count];
                for (int i = 0; i < indexes.Count; ++i)
                {
                    indexes[i] = i;
                }
            }

            int billboardCount = indexes == null ? positions.Count : indexes.Count;

            if (billboardCount > MaxPointCount)
            {
                billboardCount = MaxPointCount;
            }

            Vector3[] billboards = new Vector3[billboardCount * 4];
            Vector2[] uvs = new Vector2[billboardCount * 4];
            Vector2[] uv2 = new Vector2[billboardCount * 4];
            Color[] colors = new Color[billboardCount * 4];
            int[] tris = new int[billboardCount * 6];

            int n = 0;
            int t = 0;

            Vector3 up = Vector3.up;
            Vector3 right = Vector3.right;


            for (int i = 0; i < billboardCount; i++)
            {
                billboards[t + 0] = positions[indexes[i]];
                billboards[t + 1] = positions[indexes[i]];
                billboards[t + 2] = positions[indexes[i]];
                billboards[t + 3] = positions[indexes[i]];

                uvs[t + 0] = Vector3.zero;
                uvs[t + 1] = Vector3.right;
                uvs[t + 2] = Vector3.up;
                uvs[t + 3] = Vector3.one;

                uv2[t + 0] = -up - right;
                uv2[t + 1] = -up + right;
                uv2[t + 2] = up - right;
                uv2[t + 3] = up + right;

                if(!update)
                {
                    colors[t + 0] = color;
                    colors[t + 1] = color;
                    colors[t + 2] = color;
                    colors[t + 3] = color;
                }
              

                tris[n + 0] = t + 0;
                tris[n + 1] = t + 1;
                tris[n + 2] = t + 2;
                tris[n + 3] = t + 1;
                tris[n + 4] = t + 3;
                tris[n + 5] = t + 2;

                t += 4;
                n += 6;
            }

            if(!update)
            {
                target.Clear();
            }
            target.vertices = billboards;
            target.uv = uvs;
            target.uv2 = uv2;
            if (!update)
            {
                target.colors = colors;
            }
            target.triangles = tris;

        }

        public static void BuildVertexMeshNew(IList<Vector3> positions, Color color, Mesh target, IEnumerable<int> indexes)
        {
            if (indexes != null)
            {
                target.Clear();
            }

            target.vertices = positions.ToArray();

            if (indexes != null)
            {
                Color[] colors = new Color[target.vertexCount];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = color;
                }
                target.colors = colors;
                target.subMeshCount = 1;
                target.SetIndices(indexes as int[] ?? indexes.ToArray(), MeshTopology.Points, 0);
            }
        }

        public static void BuildVertexMesh(IList<Vector3> positions, Color color, Mesh target, IList<int> indexes)
        {
            if (PBBuiltinMaterials.geometryShadersSupported)
            {
                BuildVertexMeshNew(positions, color, target, indexes);
            }
            else
            {
                BuildVertexMeshLegacy(positions, color, target, indexes);
            }
        }

        public static void BuildVertexMesh(IList<Vector3> positions, Color color, Mesh target)
        {
            int[] indexes = new int[positions.Count];
            for (int i = 0; i < indexes.Length; ++i)
            {
                indexes[i] = i;
            }
            if (PBBuiltinMaterials.geometryShadersSupported)
            {
                BuildVertexMeshNew(positions, color, target, indexes);
            }
            else
            {
                BuildVertexMeshLegacy(positions, color, target, indexes);
            }
        }

        public static Face GetFace(ProBuilderMesh mesh, Edge edge)
        {
            Face res = null;

            foreach (var face in mesh.faces)
            {
                var edges = face.edges;

                for (int i = 0, c = edges.Count; i < c; i++)
                {
                    if (edge.Equals(edges[i]))
                        return face;

                    if (edges.Contains(edges[i]))
                        res = face;
                }
            }

            return res;
        }
    }
}
