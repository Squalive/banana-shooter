
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ProBuilder;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public static class PBElementSelection
    {
        const int MaxHoleIterations = 2048;

        public static List<List<Edge>> FindHoles(ProBuilderMesh mesh, HashSet<int> common)
        {
            List<List<Edge>> holes = new List<List<Edge>>();
            List<WingedEdge> wings = WingedEdge.GetWingedEdges(mesh);

            foreach (List<WingedEdge> hole in FindHoles(wings, common))
                holes.Add(hole.Select(x => x.edge.local).ToList());

            return holes;
        }

        internal static List<List<WingedEdge>> FindHoles(List<WingedEdge> wings, HashSet<int> common)
        {
            HashSet<WingedEdge> used = new HashSet<WingedEdge>();
            List<List<WingedEdge>> holes = new List<List<WingedEdge>>();

            for (int i = 0; i < wings.Count; i++)
            {
                WingedEdge c = wings[i];

                // if this edge has been added to a hole already, or the edge isn't in the approved list of indexes,
                // or if there's an opposite face, this edge doesn't belong to a hole.  move along.
                if (c.opposite != null || used.Contains(c) || !(common.Contains(c.edge.common.a) || common.Contains(c.edge.common.b)))
                    continue;

                List<WingedEdge> hole = new List<WingedEdge>();
                WingedEdge it = c;
                int ind = it.edge.common.a;

                int counter = 0;

                while (it != null && counter++ < MaxHoleIterations)
                {
                    used.Add(it);
                    hole.Add(it);

                    ind = it.edge.common.a == ind ? it.edge.common.b : it.edge.common.a;
                    it = FindNextEdgeInHole(it, ind);

                    if (it == c)
                        break;
                }

                List<SimpleTuple<int, int>> splits = new List<SimpleTuple<int, int>>();

                // check previous wings for y == x (closed loop).
                for (int n = 0; n < hole.Count; n++)
                {
                    WingedEdge wing = hole[n];

                    for (int p = n - 1; p > -1; p--)
                    {
                        if (wing.edge.common.b == hole[p].edge.common.a)
                        {
                            splits.Add(new SimpleTuple<int, int>(p, n));
                            break;
                        }
                    }
                }

                // create new lists from each segment
                // holes paths are nested, with holes
                // possibly split between multiple nested
                // holes
                //
                //  [2, 0]                                     [5, 3]
                //      [0, 9]                                     [3, 11]
                //      [9, 10]                                    [11, 10]
                //              [10, 7]                                    [10, 2]
                //                      [7, 6]             or with split            [2, 0]
                //                      [6, 1]             nesting ->               [0, 9]
                //                      [1, 4]                                      [9, 10]
                //                      [4, 7]  <- (y == x)                [10, 7]
                //              [7, 8]                                      [7, 6]
                //              [8, 5]                                      [6, 1]
                //              [5, 3]                                      [1, 4]
                //              [3, 11]                                     [4, 7]
                //              [11, 10]    <- (y == x)                [7, 8]
                // [10, 2]                      <- (y == x)                [8, 5]
                //
                // paths may also contain multiple segments non-tiered

                int splitCount = splits.Count;

                splits.Sort((x, y) => x.item1.CompareTo(y.item1));

                int[] shift = new int[splitCount];

                // Debug.Log(hole.ToString("\n") + "\n" + splits.ToString("\n"));

                for (int n = splitCount - 1; n > -1; n--)
                {
                    int x = splits[n].item1, y = splits[n].item2 - shift[n];
                    int range = (y - x) + 1;

                    List<WingedEdge> section = hole.GetRange(x, range);

                    hole.RemoveRange(x, range);

                    for (int m = n - 1; m > -1; m--)
                        if (splits[m].item2 > splits[n].item2)
                            shift[m] += range;

                    // verify that this path has at least one index that was asked for
                    if (splitCount < 2 || section.Any(w => common.Contains(w.edge.common.a)) || section.Any(w => common.Contains(w.edge.common.b)))
                        holes.Add(section);
                }
            }

            return holes;
        }

        static WingedEdge FindNextEdgeInHole(WingedEdge wing, int common)
        {
            WingedEdge next = wing.GetAdjacentEdgeWithCommonIndex(common);
            int counter = 0;
            while (next != null && next != wing && counter++ < MaxHoleIterations)
            {
                if (next.opposite == null)
                    return next;

                next = next.opposite.GetAdjacentEdgeWithCommonIndex(common);
            }

            return null;
        }
    }
}