// <copyright file="AnnealingLinesImage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Collections.Generic;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Helpers.LineProjecting;
    using PostProcessing.Stitching;

    public class AnnealingLinesImage : AnnealingImage
    {
        private readonly ILineProjector pointsProjector;

        public AnnealingLinesImage(BlackWhiteImage groundTruth, ILineProjector pointsProjector)
            : base(groundTruth)
        {
            this.pointsProjector = pointsProjector;
        }

        public void DrawLine(List<LatLong> road, double? meterWidth, string quadkey, int resolution)
        {
            this.DrawFatLine(this.pointsProjector.GetPoints(road, meterWidth, quadkey, resolution), 1);
        }

        public void DrawLine(List<Point> points, int width)
        {
            for (int i = 0; i + 1 < points.Count; i++)
            {
                this.DrawEdge(new Edge { X1 = points[i].X, Y1 = points[i].Y, X2 = points[i + 1].X, Y2 = points[i + 1].Y, Width = width });
            }
        }

        public void EraseLine(List<Point> points, int width)
        {
            for (int i = 0; i + 1 < points.Count; i++)
            {
                this.EraseEdge(new Edge { X1 = points[i].X, Y1 = points[i].Y, X2 = points[i + 1].X, Y2 = points[i + 1].Y, Width = width });
            }
        }

        private void DrawFatLine(IEnumerable<Tuple<int, int>> points, int add)
        {
            foreach (var (x, y) in points)
            {
                this.Score -= AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[x, y], this.Data[x, y]);
                this.Data[x, y] += add;
                this.Score += AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[x, y], this.Data[x, y]);
            }
        }
    }
}