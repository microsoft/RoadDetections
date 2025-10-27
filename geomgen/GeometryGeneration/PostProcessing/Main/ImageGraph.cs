// <copyright file="ImageGraph.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Annealing;
    using PostProcessing.Search;
    using PostProcessing.Stitching;

    public class ImageGraph : IPlanarGraph
    {
        private readonly int width;

        private readonly int height;

        private readonly List<List<int>> connections;

        public ImageGraph(BinaryImage img)
        {
            this.connections = new List<List<int>>();
            this.width = img.Width;
            this.height = img.Height;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    this.connections.Add(new List<int>());

                    if (img.Data[i, j])
                    {
                        for (int ii = 0; ii < 8; ii += 2)
                        {
                            int ni = i + GeometryHelper.DIJ[ii, 0], nj = j + GeometryHelper.DIJ[ii, 1];
                            if (img.CheckCoordinates(ni, nj) && img.Data[ni, nj])
                            {
                                this.connections[(i * img.Height) + j].Add((ni * img.Height) + nj);
                            }
                        }

                        for (int ii = 1; ii < 8; ii += 2)
                        {
                            int ni = i + GeometryHelper.DIJ[ii, 0], nj = j + GeometryHelper.DIJ[ii, 1];

                            int prvi = i + GeometryHelper.DIJ[ii - 1, 0], prvj = j + GeometryHelper.DIJ[ii - 1, 1];
                            int nxti = i + GeometryHelper.DIJ[(ii + 1) % 8, 0], nxtj = j + GeometryHelper.DIJ[(ii + 1) % 8, 1];
                            if (img.CheckCoordinates(ni, nj) && img.Data[ni, nj]
                                && img.CheckCoordinates(nxti, nxtj) && !img.Data[nxti, nxtj]
                                && img.CheckCoordinates(prvi, prvj) && !img.Data[prvi, prvj])
                            {
                                this.connections[(i * img.Height) + j].Add((ni * img.Height) + nj);
                            }
                        }
                    }
                }
            }
        }

        public ImageGraph(ImageGraph other)
        {
            this.connections = new List<List<int>>();
            this.width = other.width;
            this.height = other.height;
            for (int i = 0; i < this.width; i++)
            {
                for (int j = 0; j < this.height; j++)
                {
                    this.connections.Add(new List<int>());
                }
            }
        }

        public ImageGraph(List<Edge> edges, int width, int height)
        {
            this.connections = new List<List<int>>();
            this.width = width;
            this.height = height;
            for (int i = 0; i < this.width; i++)
            {
                for (int j = 0; j < this.height; j++)
                {
                    this.connections.Add(new List<int>());
                }
            }

            foreach (var edge in edges)
            {
                int id1 = this.GetId(edge.X1, edge.Y1);
                int id2 = this.GetId(edge.X2, edge.Y2);
                this.AddEdge(id1, id2);
            }
        }

        public ImageGraph(int width, int height)
        {
            this.connections = new List<List<int>>();
            this.width = width;
            this.height = height;
            for (int i = 0; i < this.width; i++)
            {
                for (int j = 0; j < this.height; j++)
                {
                    this.connections.Add(new List<int>());
                }
            }
        }

        public ImageGraph(List<Line> lines, int width, int height)
        {
            this.connections = new List<List<int>>();
            this.width = width;
            this.height = height;
            for (int i = 0; i < this.width; i++)
            {
                for (int j = 0; j < this.height; j++)
                {
                    this.connections.Add(new List<int>());
                }
            }

            foreach (var line in lines)
            {
                for (int i = 0; i + 1 < line.Points.Count; i++)
                {
                    int id1 = this.GetId(line.Points[i].X, line.Points[i].Y);
                    int id2 = this.GetId(line.Points[i + 1].X, line.Points[i + 1].Y);
                    if (id1 != id2)
                    {
                        this.AddEdge(id1, id2);
                    }
                }
            }
        }

        public int NodesCount => this.connections.Count;

        public int ConnectionsCount => this.connections.Count;

        public int RealNodesCount => this.connections.Count(edges => edges.Count != 0);

        public void CleanDupEdges()
        {
            for (int i = 0; i < this.connections.Count; i++)
            {
                this.connections[i] = this.connections[i].Distinct().ToList();
            }
        }

        public IEnumerable<int> GetConnections(int node)
        {
            return this.connections[node];
        }

        public int GetConnectionsCount(int node)
        {
            return this.connections[node].Count;
        }

        public void MoveNode(int fromx, int fromy, int tox, int toy)
        {
            if (fromx == tox && fromy == toy)
            {
                return;
            }

            int from = this.GetId(fromx, fromy);
            int to = this.GetId(tox, toy);
            for (int i = 0; i < this.connections[from].Count; i++)
            {
                int neig = this.connections[from][i];
                this.connections[neig][this.connections[neig].FindIndex(v => v == from)] = to;
            }

            this.connections[to].AddRange(this.connections[from]);
            this.connections[from].Clear();
        }

        public List<Edge> GetEdges()
        {
            var res = new List<Edge>();
            for (int i = 0; i < this.NodesCount; i++)
            {
                foreach (var to in this.connections[i])
                {
                    if (to > i)
                    {
                        this.GetCoordinates(i, out var x1, out var y1);
                        this.GetCoordinates(to, out var x2, out var y2);
                        res.Add(new Edge { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Width = 1 });
                    }
                }
            }

            return res;
        }

        public RgbImage ToImage(Color nodesColor, int nodeRadius = 0, int zoom = 1)
        {
            var img = new RgbImage(zoom * this.width, zoom * this.height);

            for (int i = 0; i < this.NodesCount; i++)
            {
                this.GetCoordinates(i, out var x1, out var y1);
                foreach (int j in this.connections[i])
                {
                    this.GetCoordinates(j, out var x2, out var y2);
                    img.DrawLine(zoom * x1, zoom * y1, zoom * x2, zoom * y2, Color.White);
                }
            }

            for (int i = 0; i < this.NodesCount; i++)
            {
                if (this.connections[i].Count > 0)
                {
                    this.GetCoordinates(i, out var x, out var y);
                    img.DrawCircle(zoom * x, zoom * y, zoom * nodeRadius, nodesColor);
                }
            }

            return img;
        }

        public RgbImage ToImage(RgbImage img, int nodeRadius = 0)
        {
            for (int i = 0; i < this.NodesCount; i++)
            {
                this.GetCoordinates(i, out var x1, out var y1);
                foreach (int j in this.connections[i])
                {
                    this.GetCoordinates(j, out var x2, out var y2);
                    img.DrawLine(x1, y1, x2, y2, Color.Blue);
                }
            }

            for (int i = 0; i < this.NodesCount; i++)
            {
                if (this.connections[i].Count > 0)
                {
                    this.GetCoordinates(i, out var x, out var y);
                    img.DrawCircle(x, y, nodeRadius, Color.Red);
                }
            }

            return img;
        }

        public void GetCoordinates(int id, out int x, out int y)
        {
            x = id / this.height;
            y = id % this.height;
        }

        public int GetId(int x, int y)
        {
            return (x * this.height) + y;
        }

        public int GetId(Tuple<int, int> p)
        {
            return (p.Item1 * this.height) + p.Item2;
        }

        public void AddEdge(int v1, int v2, bool oneway = false)
        {
            this.connections[v1].Add(v2);
            if (!oneway)
            {
                this.connections[v2].Add(v1);
            }
        }

        public bool CheckIJ(int i, int j)
        {
            return i >= 0 && i < this.width && j >= 0 && j < this.height;
        }

        public int CountEdges(int i, int j)
        {
            return this.connections[this.GetId(i, j)].Count;
        }
    }
}