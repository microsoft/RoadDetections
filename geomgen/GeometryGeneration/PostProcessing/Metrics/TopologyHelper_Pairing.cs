// <copyright file="TopologyHelper_Pairing.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using Utils;

    public static partial class TopologyHelper
    {
        public static Tuple<List<Tuple<int, int>>, List<Tuple<int, int>>> GetPairs(SparsePlanarGraph cGraph, SparsePlanarGraph gGraph, double topologyMaxDistToNode, int topologyMaxDist2ToEdge)
        {
            var pairCG = Enumerable.Repeat(-1, cGraph.NodesCount).ToList();
            var pairGC = Enumerable.Repeat(-1, gGraph.NodesCount).ToList();
            var pairCGDist2 = Enumerable.Repeat(0.0, cGraph.NodesCount).ToList();
            var pairGCDist2 = Enumerable.Repeat(0.0, gGraph.NodesCount).ToList();

            for (int i = 0; i < cGraph.NodesCount; i++)
            {
                if (!cGraph.IsControlPoint[i])
                {
                    continue;
                }

                for (int j = 0; j < gGraph.NodesCount; j++)
                {
                    if (!gGraph.IsControlPoint[j])
                    {
                        continue;
                    }

                    var dist = GeometryHelper.Distance(cGraph.Xs[i], cGraph.Ys[i], gGraph.Xs[j], gGraph.Ys[j]);
                    var coeff = SimilarityCoefficient(cGraph, gGraph, i, j);
                    dist /= coeff;
                    if (GenericHelper.CompareDouble(dist, topologyMaxDistToNode) <= 0)
                    {
                        if (pairCG[i] == -1 || dist < pairCGDist2[i])
                        {
                            pairCG[i] = j;
                            pairCGDist2[i] = dist;
                        }

                        if (pairGC[j] == -1 || dist < pairGCDist2[j])
                        {
                            pairGC[j] = i;
                            pairGCDist2[j] = dist;
                        }
                    }
                }
            }

            var pairs = new List<Tuple<int, int, double>>();
            for (int i = 0; i < cGraph.NodesCount; i++)
            {
                if (pairCG[i] != -1)
                {
                    pairs.Add(Tuple.Create(i, pairCG[i], pairCGDist2[i]));
                    pairCG[i] = -1;
                }
            }

            for (int i = 0; i < gGraph.NodesCount; i++)
            {
                if (pairGC[i] != -1)
                {
                    pairs.Add(Tuple.Create(pairGC[i], i, pairGCDist2[i]));
                    pairGC[i] = -1;
                }
            }

            // c, g, dist
            foreach (var tuple in pairs.OrderBy(tuple => tuple.Item3))
            {
                if (pairCG[tuple.Item1] == -1 && pairGC[tuple.Item2] == -1)
                {
                    pairCG[tuple.Item1] = tuple.Item2;
                    pairGC[tuple.Item2] = tuple.Item1;
                }
            }

            FillPairs(cGraph, gGraph, pairCG, pairGC, topologyMaxDist2ToEdge);
            FillPairs(gGraph, cGraph, pairGC, pairCG, topologyMaxDist2ToEdge);

            var res1 = new HashSet<Tuple<int, int>>();
            for (int i = 0; i < pairCG.Count; i++)
            {
                if (pairCG[i] != -1)
                {
                    res1.Add(Tuple.Create(i, pairCG[i]));
                }
            }

            var res2 = new HashSet<Tuple<int, int>>();
            for (int i = 0; i < pairGC.Count; i++)
            {
                if (pairGC[i] != -1)
                {
                    res2.Add(Tuple.Create(pairGC[i], i));
                }
            }

            return Tuple.Create(res1.ToList(), res2.ToList());
        }

        private static double SimilarityCoefficient(SparsePlanarGraph g1, SparsePlanarGraph g2, int v1, int v2)
        {
            bool isEnd1 = g1.GetConnectionsCount(v1) == 1;
            bool isEnd2 = g2.GetConnectionsCount(v2) == 1;
            if (isEnd1 == isEnd2)
            {
                var vec1 = g1.GetAverageVector(v1);
                var vec2 = g2.GetAverageVector(v2);
                if (GenericHelper.CompareDouble(GeometryHelper.Distance(vec1.Item1, vec1.Item2, vec2.Item1, vec2.Item2), 1.0) <= 0)
                {
                    return 3.5;
                }
            }

            return 1.0;
        }

        private static void FillPairs(SparsePlanarGraph mainGraph, SparsePlanarGraph pairGraph, List<int> pairMain, List<int> pairPair, int topologyMaxDist2ToEdge)
        {
            for (int i = 0; i < mainGraph.NodesCount; i++)
            {
                if (!mainGraph.IsControlPoint[i])
                {
                    continue;
                }

                if (pairMain[i] == -1)
                {
                    bool found = pairGraph.TryFindClosestPoint(mainGraph.Xs[i], mainGraph.Ys[i], out var x, out var y, out var from, out var to);
                    int nodeId;
                    if (found && GeometryHelper.Distance2(mainGraph.Xs[i], mainGraph.Ys[i], x, y) <= topologyMaxDist2ToEdge)
                    {
                        nodeId = pairGraph.SplitEdge(x, y, from, to);
                    }
                    else
                    {
                        nodeId = pairGraph.CreateFakeNode(mainGraph.Xs[i], mainGraph.Ys[i]);
                    }

                    pairMain[i] = nodeId;

                    while (pairPair.Count <= nodeId)
                    {
                        pairPair.Add(-1);
                    }

                    pairPair[nodeId] = i;
                }
            }
        }
    }
}