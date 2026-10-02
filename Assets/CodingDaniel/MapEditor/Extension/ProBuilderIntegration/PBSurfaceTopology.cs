
using System.Collections.ObjectModel;
using UnityEngine.ProBuilder;
namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public static class PBSurfaceTopology
    {
        public static bool? ConformOppositeNormal(WingedEdge source)
        {
            if (source == null || source.opposite == null)
                return false;

            Edge cea = GetCommonEdgeInWindingOrder(source);
            Edge ceb = GetCommonEdgeInWindingOrder(source.opposite);

            if (cea.a == ceb.a)
            {
                source.opposite.face.Reverse();

                return true;
            }

            return null; //no change
        }

        /// <summary>
        /// Iterate a face and return a new common edge where the edge indexes are true to the triangle winding order.
        /// </summary>
        /// <param name="wing"></param>
        /// <returns></returns>
        static Edge GetCommonEdgeInWindingOrder(WingedEdge wing)
        {
            ReadOnlyCollection<int> indexes = wing.face.indexes;
            int len = indexes.Count;

            for (int i = 0; i < len; i += 3)
            {
                Edge e = wing.edge.local;
                int a = indexes[i], b = indexes[i + 1], c = indexes[i + 2];

                if (e.a == a && e.b == b)
                    return wing.edge.common;
                else if (e.a == b && e.b == a)
                    return new Edge(wing.edge.common.b, wing.edge.common.a);
                else if (e.a == b && e.b == c)
                    return wing.edge.common;
                else if (e.a == c && e.b == b)
                    return new Edge(wing.edge.common.b, wing.edge.common.a);
                else if (e.a == c && e.b == a)
                    return wing.edge.common;
                else if (e.a == a && e.b == c)
                    return new Edge(wing.edge.common.b, wing.edge.common.a);
            }

            return Edge.Empty;
        }
    }
}