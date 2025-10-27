// <copyright file="SparsePlanarGraph.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using PostProcessing.Main;
    using PostProcessing.Search;
    using Utils;

    public class SparsePlanarGraph : IPlanarGraph
    {
        public SparsePlanarGraph(ImageGraph g)
        {
            var oldIdToNewId = Enumerable.Repeat(-1, g.NodesCount).ToArray();
            this.NodesCount = g.RealNodesCount;
            this.Xs = Enumerable.Repeat(-1, this.NodesCount).ToList();
            this.Ys = Enumerable.Repeat(-1, this.NodesCount).ToList();
            this.Connections = new List<List<int>>();
            this.IsControlPoint = new List<bool>();
            for (int i = 0; i < this.NodesCount; i++)
            {
                this.Connections.Add(new List<int>());
            }

            int nodes = 0;
            for (int i = 0; i < g.ConnectionsCount; i++)
            {
                foreach (var to in g.GetConnections(i))
                {
                    if (oldIdToNewId[i] == -1)
                    {
                        oldIdToNewId[i] = nodes++;
                    }

                    if (oldIdToNewId[to] == -1)
                    {
                        oldIdToNewId[to] = nodes++;
                    }

                    int a = oldIdToNewId[i];
                    int b = oldIdToNewId[to];
                    this.Connections[a].Add(b);

                    g.GetCoordinates(i, out var x, out var y);
                    this.Xs[a] = x;
                    this.Ys[a] = y;
                }
            }

            for (int i = 0; i < this.NodesCount; i++)
            {
                this.IsControlPoint.Add(this.Connections[i].Count != 2);
            }
        }

        public int ConnectionsCount => this.Connections.Count;

        public int NodesCount { get; private set; }

        public List<int> Xs { get; private set; }

        public List<int> Ys { get; private set; }

        public List<bool> IsControlPoint { get; private set; }

        private List<List<int>> Connections { get; set; }

        public IEnumerable<int> GetConnections(int node)
        {
            return this.Connections[node];
        }

        public int GetConnectionsCount(int node)
        {
            return this.Connections[node].Count;
        }

        public void GetCoordinates(int id, out int x, out int y)
        {
            x = this.Xs[id];
            y = this.Ys[id];
        }

        public bool TryFindClosestPoint(int x, int y, out int resX, out int resY, out int from, out int to)
        {
            var found = false;
            resX = resY = from = to = 0;
            var bestDist = 0.0;
            for (int i = 0; i < this.NodesCount; i++)
            {
                foreach (var j in this.Connections[i])
                {
                    var (curX, curY) = GeometryHelper.FindClosestPointToSegment(x, y, this.Xs[i], this.Ys[i], this.Xs[j], this.Ys[j]);
                    var curDist = GeometryHelper.Distance(x, y, curX, curY);
                    if (!found || curDist < bestDist)
                    {
                        found = true;
                        bestDist = curDist;
                        resX = GenericHelper.Round(curX);
                        resY = GenericHelper.Round(curY);
                        (from, to) = (i, j);
                    }
                }
            }

            return found;
        }

        public int CreateFakeNode(int x, int y)
        {
            this.NodesCount++;
            this.Xs.Add(x);
            this.Ys.Add(y);
            this.Connections.Add(new List<int>());
            this.IsControlPoint.Add(false);

            return this.NodesCount - 1;
        }

        public int SplitEdge(int x, int y, int from, int to)
        {
            if (this.Xs[from] == x && this.Ys[from] == y)
            {
                return from;
            }

            if (this.Xs[to] == x && this.Ys[to] == y)
            {
                return to;
            }

            int nodeId = this.CreateFakeNode(x, y);

            // change edge {from, to} to {from, nodeId}
            for (int i = 0; i < this.Connections[from].Count; i++)
            {
                if (this.Connections[from][i] == to)
                {
                    this.Connections[from][i] = nodeId;
                }
            }

            // change edge {to, from} to {to, nodeId}
            for (int i = 0; i < this.Connections[to].Count; i++)
            {
                if (this.Connections[to][i] == from)
                {
                    this.Connections[to][i] = nodeId;
                }
            }

            this.Connections[nodeId].Add(from);
            this.Connections[nodeId].Add(to);

            return nodeId;
        }

        public Tuple<double, double> GetAverageVector(int v)
        {
            var xRes = 0.0;
            var yRes = 0.0;
            int cnt = 0;
            foreach (var to in this.Connections[v])
            {
                var (vx, vy) = GeometryHelper.GetVector(this.Xs[v], this.Ys[v], this.Xs[to], this.Ys[to]);
                xRes += vx;
                yRes += vy;
                cnt++;
            }

            return Tuple.Create(xRes / cnt, yRes / cnt);
        }
    }
}
