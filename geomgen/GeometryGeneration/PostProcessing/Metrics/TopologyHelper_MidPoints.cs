// <copyright file="TopologyHelper_MidPoints.cs" company="Microsoft">
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
        public static void AddMidPoints(SparsePlanarGraph graph1, SparsePlanarGraph graph2, ICollection<Tuple<int, int>> pairs, string quadkey, int resolution)
        {
            double topologyMidPointPerDistance = GeoHelper.MetersToPixels(50.0, quadkey, resolution);

            var pair1To2 = new Dictionary<int, List<int>>();
            var pair2To1 = new Dictionary<int, List<int>>();

            foreach (var pair in pairs)
            {
                AddToDictionary(pair1To2, pair.Item1, pair.Item2);
                AddToDictionary(pair2To1, pair.Item2, pair.Item1);
            }

            var paths1 = GenerateControlPaths(graph1);
            var paths2 = GenerateControlPaths(graph2);

            foreach (var path1 in paths1)
            {
                bool found = false;
                foreach (var p1 in pair1To2[path1.Key.Item1])
                {
                    foreach (var p2 in pair1To2[path1.Key.Item2])
                    {
                        int from = Math.Min(p1, p2), to = Math.Max(p1, p2);
                        var pairId = Tuple.Create(from, to);
                        if (paths2.ContainsKey(pairId))
                        {
                            found = true;
                            var path2 = paths2[pairId];
                            if (path2.Path.First() != p1)
                            {
                                path2 = path2.Reverse();
                            }

                            int cnt = GenericHelper.Round((path1.Value.Distance + path2.Distance) / 2.0 / topologyMidPointPerDistance) - 1;
                            var points1 = CreateMidPoints(graph1, path1.Value, cnt);
                            var points2 = CreateMidPoints(graph2, path2, cnt);

                            for (int i = 0; i < cnt; i++)
                            {
                                pairs.Add(Tuple.Create(points1[i], points2[i]));
                                AddToDictionary(pair1To2, points1[i], points2[i]);
                                AddToDictionary(pair2To1, points2[i], points1[i]);
                            }
                        }
                    }
                }

                if (!found)
                {
                    int cnt = GenericHelper.Round(path1.Value.Distance / topologyMidPointPerDistance) - 1;
                    var points1 = CreateMidPoints(graph1, path1.Value, cnt);
                    var points2 = CreateFakeMidPoints(graph2, graph1, points1);

                    for (int i = 0; i < cnt; i++)
                    {
                        pairs.Add(Tuple.Create(points1[i], points2[i]));
                        AddToDictionary(pair1To2, points1[i], points2[i]);
                        AddToDictionary(pair2To1, points2[i], points1[i]);
                    }
                }
            }

            foreach (var path2 in paths2)
            {
                bool found = false;
                foreach (var p1 in pair2To1[path2.Key.Item1])
                {
                    foreach (var p2 in pair2To1[path2.Key.Item2])
                    {
                        int from = Math.Min(p1, p2), to = Math.Max(p1, p2);
                        var pairId = Tuple.Create(from, to);
                        if (paths1.ContainsKey(pairId))
                        {
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    int cnt = GenericHelper.Round(path2.Value.Distance / topologyMidPointPerDistance) - 1;
                    var points2 = CreateMidPoints(graph2, path2.Value, cnt);
                    var points1 = CreateFakeMidPoints(graph1, graph2, points2);

                    for (int i = 0; i < cnt; i++)
                    {
                        pairs.Add(Tuple.Create(points1[i], points2[i]));
                        AddToDictionary(pair1To2, points1[i], points2[i]);
                        AddToDictionary(pair2To1, points2[i], points1[i]);
                    }
                }
            }
        }

        private static void AddToDictionary(Dictionary<int, List<int>> dic, int key, int value)
        {
            if (dic.ContainsKey(key))
            {
                dic[key].Add(value);
            }
            else
            {
                dic.Add(key, new List<int> { value });
            }
        }

        private static List<int> CreateMidPoints(SparsePlanarGraph graph, ControlPath path, int pointsCount)
        {
            var res = new List<int>();

            double distBetweenInserts = (path.Distance / (pointsCount + 1)) + 1e-3;
            double distFromPrevSegment = 0.0;
            for (int i = 0; i + 1 < path.Path.Count; i++)
            {
                int from = path.Path[i];
                int to = path.Path[i + 1];
                double dist = GeometryHelper.Distance(graph.Xs[from], graph.Ys[from], graph.Xs[to], graph.Ys[to]);

                int cnt = (int)((distFromPrevSegment + dist) / distBetweenInserts);
                for (int j = 0; j < cnt; j++)
                {
                    double distFromFrom = ((j + 1) * distBetweenInserts) - distFromPrevSegment;
                    var (vx, vy) = GeometryHelper.GetVector(graph.Xs[from], graph.Ys[from], graph.Xs[to], graph.Ys[to]);
                    var x = GenericHelper.Round(graph.Xs[from] + (distFromFrom * vx));
                    var y = GenericHelper.Round(graph.Ys[from] + (distFromFrom * vy));
                    int newNode = graph.SplitEdge(x, y, j == 0 ? from : res.Last(), to);
                    res.Add(newNode);
                }

                distFromPrevSegment = distFromPrevSegment + dist - (cnt * distBetweenInserts);
            }

            return res;
        }

        private static List<int> CreateFakeMidPoints(SparsePlanarGraph fakeGraph, SparsePlanarGraph realGraph, List<int> points)
        {
            var res = new List<int>();
            foreach (var pid in points)
            {
                res.Add(fakeGraph.CreateFakeNode(realGraph.Xs[pid], realGraph.Ys[pid]));
            }

            return res;
        }

        private static Dictionary<Tuple<int, int>, ControlPath> GenerateControlPaths(SparsePlanarGraph graph)
        {
            var res = new Dictionary<Tuple<int, int>, ControlPath>();

            for (int i = 0; i < graph.NodesCount; i++)
            {
                if (graph.IsControlPoint[i])
                {
                    FillControlPaths(i, graph, res, new List<int>() { i }, 0.0, -1);
                }
            }

            return res;
        }

        private static void FillControlPaths(int v, SparsePlanarGraph graph, Dictionary<Tuple<int, int>, ControlPath> res, List<int> path, double distance, int parent)
        {
            if (graph.IsControlPoint[v] && path.Count != 1)
            {
                var cur = new ControlPath(path, distance);
                if (!res.ContainsKey(cur.Id))
                {
                    res.Add(cur.Id, cur);
                }

                return;
            }

            foreach (var to in graph.GetConnections(v))
            {
                if (to != parent)
                {
                    path.Add(to);
                    FillControlPaths(to, graph, res, path, distance + GeometryHelper.Distance(graph.Xs[v], graph.Ys[v], graph.Xs[to], graph.Ys[to]), v);
                    path.RemoveAt(path.Count - 1);
                }
            }
        }

        private class ControlPath
        {
            public ControlPath(List<int> path, double distance)
            {
                this.Path = new List<int>();
                this.Path.AddRange(path);
                this.Distance = distance;
            }

            public List<int> Path { get; set; }

            public double Distance { get; set; }

            public Tuple<int, int> Id => Tuple.Create(Math.Min(this.Path.First(), this.Path.Last()), Math.Max(this.Path.First(), this.Path.Last()));

            public ControlPath Reverse()
            {
                this.Path.Reverse();
                return this;
            }
        }
    }
}
