// <copyright file="Dijkstra.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Search
{
    using System.Collections.Generic;
    using GeoUtils;
    using PostProcessing.Main;
    using Utils;

    public static class Dijkstra
    {
        public static PathsSearchResult Run(IPlanarGraph graph, int start)
        {
            return Run(graph, new List<int>() { start });
        }

        public static PathsSearchResult Run(IPlanarGraph graph, List<int> starts, double maxDist = 1e9)
        {
            var res = new PathsSearchResult(graph.NodesCount);
            for (int i = 0; i < res.Dist.Length; i++)
            {
                res.Dist[i] = Constants.InfiniteDistance;
                res.From[i] = -1;
            }

            var q = new Heap<DistInfo>();
            foreach (var start in starts)
            {
                res.Dist[start] = 0;
                q.Add(new DistInfo { Id = start, Dist = res.Dist[start] });
            }

            while (q.Count != 0)
            {
                var top = q.Top();
                q.Pop();
                if (GenericHelper.CompareDouble(top.Dist, res.Dist[top.Id]) == 1)
                {
                    continue;
                }

                graph.GetCoordinates(top.Id, out var x1, out var y1);
                foreach (var to in graph.GetConnections(top.Id))
                {
                    graph.GetCoordinates(to, out var x2, out var y2);
                    var len = GeometryHelper.Distance(x1, y1, x2, y2);
                    var newDist = res.Dist[top.Id] + len;

                    if (newDist < maxDist && GenericHelper.CompareDouble(newDist, res.Dist[to]) == -1)
                    {
                        res.Dist[to] = newDist;
                        res.From[to] = top.Id;
                        q.Add(new DistInfo { Id = to, Dist = res.Dist[to] });
                    }
                }
            }

            return res;
        }

        public static bool IsReachable(double dist)
        {
            return dist < Constants.InfiniteDistance - 1;
        }

        public static int[] GetPath(PathsSearchResult dists, int i)
        {
            var res = new List<int>();
            while (i != -1)
            {
                res.Add(i);
                i = dists.From[i];
            }

            return res.ToArray();
        }
    }
}
