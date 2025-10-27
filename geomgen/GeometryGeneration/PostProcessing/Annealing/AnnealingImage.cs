// <copyright file="AnnealingImage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Linq;
    using ImageUtils;
    using ImageUtils.Images;
    using PostProcessing.Helpers.LineProjecting;
    using PostProcessing.Metrics;
    using Utils;

    /// <summary>
    /// A class that holds information for each pixel if it is black or white
    /// and providing fast way of adding/erasing/moving new layers of edges/nodes
    /// keeps Score based on similarity of pixels in groundTruth image in this one.
    /// </summary>
    public class AnnealingImage
    {
        private readonly int[,] used;
        private int usedValue;

        public AnnealingImage(BlackWhiteImage groundTruth)
        {
            this.GroundTruth = groundTruth;
            this.Width = groundTruth.Width;
            this.Height = groundTruth.Height;
            this.Data = new int[this.Width, this.Height];

            this.usedValue = 0;
            this.used = new int[this.Width, this.Height];

            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.Score += AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[i, j], this.Data[i, j]);
                }
            }
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public double Score { get; protected set; }

        public double MaxScore => AnnealingImageScoreHelper.MaxScore * this.Width * this.Height;

        protected BlackWhiteImage GroundTruth { get; private set; }

        protected int[,] Data { get; set; }

        public PixelMetric GetMatchScore()
        {
            return new PixelMetric(this.ToBinaryImage(), new BinaryImage(this.GroundTruth, ImageUtils.Constants.ImageBinaryThreshold));
        }

        public RgbImage ToHeatmap()
        {
            var res = new RgbImage(this.Width, this.Height);

            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    res.Red[i, j] = this.GroundTruth.Data[i, j];
                    res.Green[i, j] = this.GroundTruth.Data[i, j];
                    res.Blue[i, j] = this.GroundTruth.Data[i, j];
                }
            }

            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    if (this.Data[i, j] == 0)
                    {
                        res.Red[i, j] = 0;
                        res.Green[i, j] = 0;
                        res.Blue[i, j] = this.GroundTruth.Data[i, j];
                    }
                    else
                    {
                        var score = AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[i, j], this.Data[i, j]);
                        var minScore = AnnealingImageScoreHelper.GetScore(0, 1);
                        var maxScore = AnnealingImageScoreHelper.GetScore(255, 1);

                        if (score < 0)
                        {
                            res.Red[i, j] = GenericHelper.Round(score / minScore * 255);
                            res.Green[i, j] = 0;
                            res.Blue[i, j] = 0;
                        }
                        else
                        {
                            res.Red[i, j] = 0;
                            res.Green[i, j] = GenericHelper.Round(score / maxScore * 255);
                            res.Blue[i, j] = 0;
                        }
                    }
                }
            }

            return res;
        }

        public BinaryImage ToBinaryImage()
        {
            var res = new BinaryImage(this.Width, this.Height);
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    res.Data[i, j] = this.Data[i, j] > 0;
                }
            }

            return res;
        }

        public void MoveEdge(int x0, int y0, int xfrom, int yfrom, int xto, int yto, int width, int dw)
        {
            var pts = EdgeMoveAndGrowHelper.GetPoints(x0, y0, xfrom, yfrom, xto, yto, width, dw, out var invY);
            if (pts != null)
            {
                foreach (var (x, y, flag) in pts)
                {
                    int xx = x + x0;
                    int yy = (invY ? -y : y) + y0;
                    if (this.CheckCoordinates(xx, yy))
                    {
                        this.Score -= AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[xx, yy], this.Data[xx, yy]);
                        this.Data[xx, yy] += flag ? 1 : -1;
                        this.Score += AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[xx, yy], this.Data[xx, yy]);
                    }
                }

                return;
            }

            this.EraseEdge(new Edge() { X1 = x0, Y1 = y0, X2 = xfrom, Y2 = yfrom, Width = width });
            this.DrawEdge(new Edge() { X1 = x0, Y1 = y0, X2 = xto, Y2 = yto, Width = width + dw });
        }

        public void DrawEdge(Edge edge)
        {
            if (edge.Width <= 0)
            {
                return;
            }

            this.DrawFatLine(edge, 1);
        }

        public void EraseEdge(Edge edge)
        {
            if (edge.Width <= 0)
            {
                return;
            }

            this.DrawFatLine(edge, -1);
        }

        protected bool CheckCoordinates(int i, int j)
        {
            return i >= 0 && i < this.Width && j >= 0 && j < this.Height;
        }

        private void DrawFatLine(Edge edge, int add)
        {
            this.usedValue++;
            int r = (edge.Width - 1) / 2;
            int prevx = 0, prevy = 0;
            bool first = true;
            foreach (var (x, y) in Bresenham.GetLinePoints(edge.X1, edge.Y1, edge.X2, edge.Y2).ToList())
            {
                if (first)
                {
                    first = false;
                    var len = LineProjectorHelper.CirclePointsLength[r];
                    for (int i = 0; i < len; i += 2)
                    {
                        int xcur = x + LineProjectorHelper.CirclePoints[r][i];
                        int ycur = y + LineProjectorHelper.CirclePoints[r][i + 1];
                        if (this.CheckCoordinates(xcur, ycur) && this.used[xcur, ycur] != this.usedValue)
                        {
                            this.used[xcur, ycur] = this.usedValue;

                            this.Score -= AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[xcur, ycur], this.Data[xcur, ycur]);
                            this.Data[xcur, ycur] += add;
                            this.Score += AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[xcur, ycur], this.Data[xcur, ycur]);
                        }
                    }
                }
                else
                {
                    int di = x - prevx + 1, dj = y - prevy + 1;
                    int len = LineProjectorHelper.Length[r, di, dj];
                    for (int i = 0; i < len; i++)
                    {
                        int xcur = x + LineProjectorHelper.DX[r, di, dj][i];
                        int ycur = y + LineProjectorHelper.DY[r, di, dj][i];
                        if (this.CheckCoordinates(xcur, ycur) && this.used[xcur, ycur] != this.usedValue)
                        {
                            this.used[xcur, ycur] = this.usedValue;

                            this.Score -= AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[xcur, ycur], this.Data[xcur, ycur]);
                            this.Data[xcur, ycur] += add;
                            this.Score += AnnealingImageScoreHelper.GetScore(this.GroundTruth.Data[xcur, ycur], this.Data[xcur, ycur]);
                        }
                    }
                }

                prevx = x;
                prevy = y;
            }
        }
    }
}