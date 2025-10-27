// <copyright file="ConnectionsGraph.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Main;
    using Utils;

    /// <summary>
    /// graph stores graph on image with margin
    /// also provides functionalities to change edges based on information
    /// stored as neighbors for each node.
    /// </summary>
    public partial class ConnectionsGraph
    {
        private readonly int margin;

        private readonly Configuration config;
        private readonly int padding;

        // node id -> node id and edge id
        private readonly List<Neighbor>[] connections;

        private readonly BlackWhiteImage sourceImage;

        public ConnectionsGraph(List<Edge> edges, BlackWhiteImage sourceImage, int margin, int padding, Configuration config)
        {
            this.margin = margin;
            this.config = config;
            this.sourceImage = sourceImage;
            this.padding = padding + margin;
            this.connections = new List<Neighbor>[(margin + sourceImage.Width + margin) * (margin + sourceImage.Height + margin)];
            for (int i = 0; i < this.connections.Length; i++)
            {
                this.connections[i] = new List<Neighbor>();
            }

            for (int eid = 0; eid < edges.Count; eid++)
            {
                int i = this.GetId(edges[eid].X1, edges[eid].Y1);
                int j = this.GetId(edges[eid].X2, edges[eid].Y2);
                this.connections[i].Add(new Neighbor { Id = j, EdgeId = eid });
                this.connections[j].Add(new Neighbor { Id = i, EdgeId = eid });
            }

            for (int i = 0; i < this.connections.Length; i++)
            {
                this.AngleScore += this.CalculateAngleScore(i, false);
            }
        }

        public double AngleScore { get; set; }

        public int RealNodes => this.connections.Count(list => list.Count != 0);

        public int CountEdges(int x, int y)
        {
            return this.connections[this.GetId(x, y)].Count;
        }

        public void DeleteEdge(int edgeId, List<Edge> edges, AnnealingImage image)
        {
            int from = this.GetId(edges[edgeId].X1, edges[edgeId].Y1);
            int to = this.GetId(edges[edgeId].X2, edges[edgeId].Y2);

            this.AngleScore -= this.CalculateAngleScore(from, true);
            this.AngleScore -= this.CalculateAngleScore(to, true);

            image.EraseEdge(edges[edgeId]);

            this.connections[from].RemoveAll(neigh => neigh.EdgeId == edgeId);
            this.connections[to].RemoveAll(neigh => neigh.EdgeId == edgeId);

            edges[edgeId] = null;

            this.AngleScore += this.CalculateAngleScore(from, true);
            this.AngleScore += this.CalculateAngleScore(to, true);
        }

        public void MoveNode(int xfrom, int yfrom, int xto, int yto, List<Edge> edges, AnnealingImage image, int widthChange = 0, bool setRevertable = false, bool revert = false)
        {
            int from = this.GetId(xfrom, yfrom);
            int to = this.GetId(xto, yto);

            this.AngleScore -= this.CalculateAngleScore(from, true);

            // move point [from] to point [to]
            foreach (var neig in this.connections[from])
            {
                this.connections[to].Add(neig);
                this.connections[neig.Id][this.connections[neig.Id].FindIndex(n => n.Id == from)].Id = to;
                int dw = widthChange == 0 ? 0 : this.ApplyWidthChange(edges, edges[neig.EdgeId], revert, setRevertable, widthChange);

                if (edges[neig.EdgeId].X1 == xfrom && edges[neig.EdgeId].Y1 == yfrom)
                {
                    image.MoveEdge(edges[neig.EdgeId].X2, edges[neig.EdgeId].Y2, xfrom, yfrom, xto, yto, edges[neig.EdgeId].Width, dw);

                    edges[neig.EdgeId].Width += dw;
                    edges[neig.EdgeId].X1 = xto;
                    edges[neig.EdgeId].Y1 = yto;
                }

                if (edges[neig.EdgeId].X2 == xfrom && edges[neig.EdgeId].Y2 == yfrom)
                {
                    image.MoveEdge(edges[neig.EdgeId].X1, edges[neig.EdgeId].Y1, xfrom, yfrom, xto, yto, edges[neig.EdgeId].Width, dw);

                    edges[neig.EdgeId].Width += dw;
                    edges[neig.EdgeId].X2 = xto;
                    edges[neig.EdgeId].Y2 = yto;
                }
            }

            this.connections[from].Clear();

            this.AngleScore += this.CalculateAngleScore(to, true);
        }

        public void MergeNodes(Edge edge, List<Edge> edges, AnnealingImage image)
        {
            int x, y;
            List<Tuple<int, int, int>> ns;
            if (RandomHelper.GetRandomBool())
            {
                x = edge.X1;
                y = edge.Y1;
                ns = this.GetNeighbors(edge.X2, edge.Y2);
            }
            else
            {
                x = edge.X2;
                y = edge.Y2;
                ns = this.GetNeighbors(edge.X1, edge.Y1);
            }

            for (int nid = 0; nid < ns.Count; nid++)
            {
                if (edges[ns[nid].Item3] != null)
                {
                    int width = edges[ns[nid].Item3].Width;
                    this.DeleteEdge(ns[nid].Item3, edges, image);

                    if (x != ns[nid].Item1 || y != ns[nid].Item2)
                    {
                        this.AddEdge(x, y, ns[nid].Item1, ns[nid].Item2, width, edges, image);
                    }
                }
            }
        }

        public void RemoveNeedlessNode(List<Tuple<int, int, int>> neighs, List<Edge> edges, AnnealingImage image)
        {
            int p1x = neighs[0].Item1;
            int p1y = neighs[0].Item2;
            int p2x = neighs[1].Item1;
            int p2y = neighs[1].Item2;

            int eid1 = neighs[0].Item3;
            int eid2 = neighs[1].Item3;
            int width = (edges[eid1].Width + edges[eid2].Width) / 2;

            this.DeleteEdge(eid1, edges, image);
            this.DeleteEdge(eid2, edges, image);

            this.AddEdge(p1x, p1y, p2x, p2y, width, edges, image);
        }

        public bool CheckIfEdgeWidthFits(Edge edge, int width, List<Edge> edges)
        {
            if (!GenericHelper.IsBetween(width, Main.Constants.MinEdgeWidth, Main.Constants.MaxEdgeWidth))
            {
                return false;
            }

            if (!this.config.AnnealingCheckEdgeWidthBalance)
            {
                return true;
            }

            int p1 = this.GetId(edge.X1, edge.Y1), p2 = this.GetId(edge.X2, edge.Y2);
            var q = new Queue<Tuple<int, int>>(new Tuple<int, int>[] { Tuple.Create(p1, p2), Tuple.Create(p2, p1) });
            while (q.Count > 0)
            {
                var (node, prev) = q.Dequeue();
                if (this.connections[node].Count > 2)
                {
                    continue;
                }

                this.GetCoordinates(node, out var nodeX, out var nodeY);

                if (this.config.AnnealingLockPadding && !AnnealingHelper.IsInInnerArea(nodeX, nodeY, this.sourceImage.Width, this.sourceImage.Height, this.padding))
                {
                    continue;
                }

                foreach (var ng in this.connections[node])
                {
                    if (ng.Id != p1 && ng.Id != p2 && ng.Id != prev)
                    {
                        if (!GenericHelper.IsBetween(width, edges[ng.EdgeId].Width - Main.Constants.EdgeAddWidth, edges[ng.EdgeId].Width + Main.Constants.EdgeAddWidth))
                        {
                            return false;
                        }

                        if (this.config.AnnealingWholeLineSameWidth)
                        {
                            q.Enqueue(Tuple.Create(ng.Id, node));
                        }
                    }
                }
            }

            return true;
        }

        public void GetFirstNeighbor(int x, int y, out int outx, out int outy, int exceptX, int exceptY, HashSet<Tuple<int, int>> points)
        {
            foreach (var neigh in this.connections[this.GetId(x, y)])
            {
                this.GetCoordinates(neigh.Id, out var nx, out var ny);
                if ((nx != exceptX || ny != exceptY) && points.Any(point => point.Item1 == nx && point.Item2 == ny))
                {
                    outx = nx;
                    outy = ny;
                    return;
                }
            }

            string message = x + " " + y + " " + exceptX + " " + exceptY + " ";
            message += "connections=";
            foreach (var neigh in this.connections[this.GetId(x, y)])
            {
                this.GetCoordinates(neigh.Id, out var nx, out var ny);
                message += "(" + nx + "," + ny + ")";
            }

            message += " points=";
            foreach (var point in points)
            {
                message += "(" + point.Item1 + "," + point.Item2 + ")";
            }

            throw new Exception("Cant find a neibor with these filters {" + message + "}");
        }

        public List<Tuple<int, int, int>> GetNeighbors(int x, int y)
        {
            var res = new List<Tuple<int, int, int>>();
            foreach (var neigh in this.connections[this.GetId(x, y)])
            {
                this.GetCoordinates(neigh.Id, out var nx, out var ny);
                res.Add(Tuple.Create(nx, ny, neigh.EdgeId));
            }

            return res;
        }

        public int GetFirstEdge(int x, int y, int except = -1)
        {
            int id = this.GetId(x, y);
            foreach (var to in this.connections[id])
            {
                if (to.EdgeId != except)
                {
                    return to.EdgeId;
                }
            }

            throw new Exception("No edges fit to this filters");
        }

        public bool CheckAngle(int xfrom, int yfrom, int xto, int yto)
        {
            int i = this.GetId(xfrom, yfrom);
            foreach (var neigh1 in this.connections[i])
            {
                this.GetCoordinates(neigh1.Id, out var n1x, out var n1y);
                foreach (var neigh2 in this.connections[neigh1.Id])
                {
                    if (neigh2.Id != i)
                    {
                        this.GetCoordinates(neigh2.Id, out var n2x, out var n2y);
                        var newAnglePi = GeometryHelper.GetAngle(xto, yto, n1x, n1y, n2x, n2y);
                        var oldAnglePi = GeometryHelper.GetAngle(xfrom, yfrom, n1x, n1y, n2x, n2y);
                        if (newAnglePi / Math.PI * 180.0 > AnnealingHelper.GetAngleRestriction(xfrom, yfrom, n1x, n1y, this.connections[i].Count, this.config) && GenericHelper.CompareDouble(newAnglePi, oldAnglePi) > 0)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public double GetMaxAngleScore()
        {
            double res = 0.0;
            for (int i = 0; i < this.connections.Length; i++)
            {
                if (this.connections[i].Count == 2)
                {
                    res += this.config.Junction2AngleWeight;
                }

                if (this.connections[i].Count == 3)
                {
                    res += 3 * this.config.Junction3AngleWeight;
                }
            }

            return res;
        }

        /// <summary>
        /// 1) Fit edges to original image
        /// 2) remove self-loops
        /// 3) remove duplicate edges.
        /// </summary>
        public List<Edge> FitEdges(List<Edge> edges, AnnealingImage image)
        {
            for (int eid = 0; eid < edges.Count; eid++)
            {
                if (edges[eid] != null && !this.IsInsideOriginalImage(edges[eid].X1, edges[eid].Y1))
                {
                    this.FitEdge(edges[eid].X1, edges[eid].Y1, edges[eid].X2, edges[eid].Y2, eid, edges, image);
                }

                if (edges[eid] != null && !this.IsInsideOriginalImage(edges[eid].X2, edges[eid].Y2))
                {
                    this.FitEdge(edges[eid].X2, edges[eid].Y2, edges[eid].X1, edges[eid].Y1, eid, edges, image);
                }
            }

            var res = new List<Edge>();
            for (int eid = 0; eid < edges.Count; eid++)
            {
                bool keepEdge = true;

                if (edges[eid] == null)
                {
                    continue;
                }

                // if edge is still outside of original image
                if (!this.IsInsideOriginalImage(edges[eid].X1, edges[eid].Y1) || !this.IsInsideOriginalImage(edges[eid].X2, edges[eid].Y2))
                {
                    keepEdge = false;
                }

                // if edge is a self-loop
                if (edges[eid].X1 == edges[eid].X2 && edges[eid].Y1 == edges[eid].Y2)
                {
                    keepEdge = false;
                }

                // if edge is a duplicate
                if (res.Any(resEdge => resEdge.IsSame(edges[eid])))
                {
                    keepEdge = false;
                }

                int from = this.GetId(edges[eid].X1, edges[eid].Y1);
                int to = this.GetId(edges[eid].X2, edges[eid].Y2);
                this.connections[from].RemoveAll(neigh => neigh.EdgeId == eid);
                this.connections[to].RemoveAll(neigh => neigh.EdgeId == eid);

                if (keepEdge)
                {
                    res.Add(edges[eid]);

                    this.connections[from].Add(new Neighbor() { EdgeId = res.Count - 1, Id = to });
                    this.connections[to].Add(new Neighbor() { EdgeId = res.Count - 1, Id = from });
                }
                else
                {
                    image.EraseEdge(edges[eid]);
                }
            }

            return res;
        }

        private int ApplyWidthChange(List<Edge> edges, Edge edge, bool revert, bool setRevertable, int widthChange)
        {
            if (revert)
            {
                if (edge.IsRevertable)
                {
                    if (setRevertable)
                    {
                        edge.IsRevertable = false;
                    }

                    return widthChange;
                }
            }
            else
            {
                if (this.CheckIfEdgeWidthFits(edge, edge.Width + widthChange, edges))
                {
                    if (setRevertable)
                    {
                        edge.IsRevertable = true;
                    }

                    return widthChange;
                }
            }

            return 0;
        }
    }
}
