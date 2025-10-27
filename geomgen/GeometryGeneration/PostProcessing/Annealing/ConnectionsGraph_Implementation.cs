// <copyright file="ConnectionsGraph_Implementation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using GeoUtils;
    using ImageUtils;
    using ImageUtils.Images;

    public partial class ConnectionsGraph
    {
        public BaseImage Draw(IEnumerable<Edge> edges)
        {
            var lbl = new BinaryImage(this.sourceImage.Width + (2 * this.margin), this.sourceImage.Height + (2 * this.margin));
            foreach (var edge in edges)
            {
                if (edge != null)
                {
                    lbl.DrawLine(edge.X1 + this.margin, edge.Y1 + this.margin, edge.X2 + this.margin, edge.Y2 + this.margin, Color.White, edge.Width);
                }
            }

            var res = new RgbImage(this.sourceImage.Width + (2 * this.margin), this.sourceImage.Height + (2 * this.margin));
            res = res.AddWithPadding(this.sourceImage, this.margin, this.margin);
            res = res.AddLabel(lbl, Color.Red);

            foreach (var edge in edges)
            {
                if (edge != null)
                {
                    res.DrawLine(edge.X1 + this.margin, edge.Y1 + this.margin, edge.X2 + this.margin, edge.Y2 + this.margin, Color.Green, 3);
                }
            }

            foreach (var edge in edges)
            {
                if (edge != null)
                {
                    res.DrawCircle(edge.X1 + this.margin, edge.Y1 + this.margin, 3, Color.Blue);
                    res.DrawCircle(edge.X2 + this.margin, edge.Y2 + this.margin, 3, Color.Blue);
                }
            }

            return res;
        }

        private void AddEdge(int x1, int y1, int x2, int y2, int width, List<Edge> edges, AnnealingImage image)
        {
            if (x1 == x2 && y1 == y2)
            {
                return;
            }

            int from = this.GetId(x1, y1);
            int to = this.GetId(x2, y2);

            this.AngleScore -= this.CalculateAngleScore(from, true);
            this.AngleScore -= this.CalculateAngleScore(to, true);

            edges.Add(new Edge { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Width = width });
            int edgeId = edges.Count - 1;

            image.DrawEdge(edges[edgeId]);

            this.connections[from].Add(new Neighbor { Id = to, EdgeId = edgeId });
            this.connections[to].Add(new Neighbor { Id = from, EdgeId = edgeId });

            this.AngleScore += this.CalculateAngleScore(from, true);
            this.AngleScore += this.CalculateAngleScore(to, true);
        }

        /// <summary>
        /// Given edge [x1,y1] - [x2,y2],
        /// Moves point [x1,y1] until it gets inside of original image
        /// If edge is fully outside of original image, edge is unchanged.
        /// </summary>
        private void FitEdge(int x1, int y1, int x2, int y2, int eid, List<Edge> edges, AnnealingImage image)
        {
            foreach (var (x, y) in Bresenham.GetLinePoints(x1, y1, x2, y2))
            {
                if (this.IsInsideOriginalImage(x, y))
                {
                    int w = edges[eid].Width;
                    this.DeleteEdge(eid, edges, image);
                    this.AddEdge(x2, y2, x, y, w, edges, image);
                    break;
                }
            }
        }

        private double CalculateAngleScore(int nodeId, bool includeNeighbors)
        {
            double res = 0.0;

            if (this.connections[nodeId].Count == 2)
            {
                this.GetCoordinates(this.connections[nodeId][0].Id, out var p1x, out var p1y);
                this.GetCoordinates(nodeId, out var p2x, out var p2y);
                this.GetCoordinates(this.connections[nodeId][1].Id, out var p3x, out var p3y);
                double angle = 180 - (GeometryHelper.GetAngle(p1x, p1y, p2x, p2y, p3x, p3y) / Math.PI * 180.0);
                res += this.config.Junction2AngleWeight * Math.Pow(angle / 180, this.config.Junction2AngleCurviness);
            }

            if (this.connections[nodeId].Count == 3)
            {
                this.GetCoordinates(nodeId, out var px, out var py);
                this.GetCoordinates(this.connections[nodeId][0].Id, out var p1x, out var p1y);
                this.GetCoordinates(this.connections[nodeId][1].Id, out var p2x, out var p2y);
                this.GetCoordinates(this.connections[nodeId][2].Id, out var p3x, out var p3y);

                double a1 = 180 - (GeometryHelper.GetAngle(p1x, p1y, px, py, p2x, p2y) / Math.PI * 180.0);
                double a2 = 180 - (GeometryHelper.GetAngle(p2x, p2y, px, py, p3x, p3y) / Math.PI * 180.0);
                double a3 = 180 - (GeometryHelper.GetAngle(p3x, p3y, px, py, p1x, p1y) / Math.PI * 180.0);

                double f1 = this.config.Junction3AngleWeight * this.GetAngleScore(a1) * a1 / 180;
                double f2 = this.config.Junction3AngleWeight * this.GetAngleScore(a2) * a2 / 180;
                double f3 = this.config.Junction3AngleWeight * this.GetAngleScore(a3) * a3 / 180;

                res += f1 + f2 + f3;
            }

            if (includeNeighbors)
            {
                foreach (var neigh in this.connections[nodeId])
                {
                    res += this.CalculateAngleScore(neigh.Id, false);
                }
            }

            return res;
        }

        /// <summary>
        /// Receives angle in range[0, 180] and returns score for a given angle.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private double GetAngleScore(double angle)
        {
            double x = Math.Abs(angle - 90) / 90;
            double ea = Math.Exp(this.config.Junction3AngleCurviness);
            double eax = Math.Exp(this.config.Junction3AngleCurviness * x);
            double res = (1 - eax) * (1 - (ea / eax)) / (ea - 1);

            return 1 - res;
        }

        private bool IsInsideOriginalImage(int x, int y)
        {
            return x >= 0 && x < this.sourceImage.Width && y >= 0 && y < this.sourceImage.Height;
        }

        private int GetId(int x, int y)
        {
            return ((x + this.margin) * (this.margin + this.sourceImage.Height + this.margin)) + y + this.margin;
        }

        private void GetCoordinates(int id, out int x, out int y)
        {
            x = (id / (this.margin + this.sourceImage.Height + this.margin)) - this.margin;
            y = (id % (this.margin + this.sourceImage.Height + this.margin)) - this.margin;
        }

        private class Neighbor
        {
            internal int Id { get; set; }

            internal int EdgeId { get; set; }
        }
    }
}
