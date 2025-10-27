// <copyright file="RamerDouglasPeucker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    using System;
    using System.Collections.Generic;
    using GeoUtils;

    public class RamerDouglasPeucker
    {
        private readonly ImageGraph res;

        private readonly ImageGraph graph;

        private readonly bool[] used;

        private readonly int minSegmentDist;

        private readonly double minCurviness;

        private readonly int resolution;

        private readonly string quadkey;

        private readonly List<int> path;

        public RamerDouglasPeucker(ImageGraph g, int minSegmentDist, double minCurviness, string quadkey, int resolution)
        {
            this.graph = g;
            this.used = new bool[g.NodesCount];
            this.res = new ImageGraph(g);
            this.minCurviness = minCurviness;
            this.minSegmentDist = minSegmentDist;
            this.resolution = resolution;
            this.quadkey = quadkey;
            this.path = new List<int>();
        }

        public ImageGraph Run()
        {
            int runs = 0, cells = 0;

            var cs = new int[10];
            for (int i = 0; i < this.graph.NodesCount; i++)
            {
                var connectionsCount = this.graph.GetConnectionsCount(i);
                if (connectionsCount > 0)
                {
                    cells++;
                }

                cs[connectionsCount]++;

                if (connectionsCount > 0 && connectionsCount != 2)
                {
                    runs++;
                    this.FindPaths(i, -1);
                    if (this.path.Count != 0)
                    {
                        throw new Exception("Path should have been cleared");
                    }
                }
            }

            if (runs == 0 && cells > 0)
            {
                for (int i = 0; i < this.graph.NodesCount; i++)
                {
                    if (this.graph.GetConnectionsCount(i) > 0 && !this.used[i])
                    {
                        runs++;
                        this.FindCircle(i, -1);
                        if (this.path.Count != 0)
                        {
                            throw new Exception("Path should have been cleared");
                        }
                    }
                }
            }

            this.res.CleanDupEdges();

            return this.res;
        }

        private void FindCircle(int node, int prev)
        {
            if (this.path.Count != 0 && node == this.path[0])
            {
                this.path.Add(node);

                this.ApplyAlgorithm(0, this.path.Count - 1);

                this.path.RemoveAt(this.path.Count - 1);
                return;
            }

            if (this.used[node])
            {
                return;
            }

            this.used[node] = true;
            this.path.Add(node);

            foreach (var i in this.graph.GetConnections(node))
            {
                if (i != prev)
                {
                    this.FindCircle(i, node);
                }

                if (prev == -1)
                {
                    break;
                }
            }

            this.path.RemoveAt(this.path.Count - 1);
            return;
        }

        private void FindPaths(int node, int prev)
        {
            if (this.path.Count != 0 && this.graph.GetConnectionsCount(node) != 2)
            {
                this.path.Add(node);

                this.ApplyAlgorithm(0, this.path.Count - 1);

                this.path.RemoveAt(this.path.Count - 1);
                return;
            }

            if (this.used[node])
            {
                return;
            }

            this.used[node] = true;
            this.path.Add(node);

            foreach (var i in this.graph.GetConnections(node))
            {
                if (i != prev)
                {
                    this.FindPaths(i, node);
                }
            }

            this.path.RemoveAt(this.path.Count - 1);
            return;
        }

        /// <summary>
        /// Actual recursive algorithm is here.
        /// </summary>
        private void ApplyAlgorithm(int from, int to)
        {
            this.graph.GetCoordinates(this.path[from], out var x1, out var y1);
            this.graph.GetCoordinates(this.path[to], out var x2, out var y2);

            double maxDist = -1;
            int distPoint = -1;
            for (int i = from + 1; i < to - 1; i++)
            {
                this.graph.GetCoordinates(this.path[i], out var x0, out var y0);

                var dist = (x1 == x2 && y1 == y2) ? GeometryHelper.Distance(x1, y1, x0, y0) : GeometryHelper.DistanceToLine(x0, y0, x1, y1, x2, y2);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    distPoint = i;
                }
            }

            if (to - from + 1 < this.minSegmentDist || GeoHelper.PixelsToMeters(maxDist, this.quadkey, this.resolution) < this.minCurviness)
            {
                this.res.AddEdge(this.path[from], this.path[to]);
            }
            else
            {
                this.ApplyAlgorithm(from, distPoint);
                this.ApplyAlgorithm(distPoint, to);
            }
        }
    }
}
