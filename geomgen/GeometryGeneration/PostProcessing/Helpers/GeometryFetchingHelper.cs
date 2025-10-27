// <copyright file="GeometryFetchingHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using ImageUtils;
    using PostProcessing.Main;
    using PostProcessing.Stitching;

    public static class GeometryFetchingHelper
    {
        /// <summary>
        /// Method gets a list of lines (geometry info about a tile of size [imageSize+padding]X[imageSize+padding]
        /// Shortens lines so that they fit into tile of size [imageSize]X[imageSize].
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="imageSize"></param>
        /// <param name="padding"></param>
        /// <param name="runnerLog"></param>
        /// <returns></returns>
        public static List<Line> FetchLines(List<Line> lines, int imageSize, int padding, RunnerLog runnerLog)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var res = new List<Line>();
            foreach (var line in lines)
            {
                int start = -1, end = -1;
                for (int i = 0; i < line.Points.Count; i++)
                {
                    if (IsInsideOriginalImage(line.Points[i].X, line.Points[i].Y, imageSize, padding))
                    {
                        if (start == -1)
                        {
                            start = i;
                        }

                        end = i;
                    }
                    else
                    {
                        if (start != -1 && end != -1)
                        {
                            res.Add(new Line(line, start, end, imageSize, padding));
                        }
                        else if (i != 0)
                        {
                            var p1 = FetchPoint(line.Points[i - 1], line.Points[i], imageSize, padding);
                            var p2 = FetchPoint(line.Points[i], line.Points[i - 1], imageSize, padding);

                            if (!(p1.Equals(line.Points[i]) || p2.Equals(line.Points[i - 1])))
                            {
                                res.Add(new Line(new List<Point> { p1, p2 }, line.BoundingBoxQuadkey, line.Width));
                            }
                        }

                        start = end = -1;
                    }
                }

                if (start != -1 && end != -1)
                {
                    res.Add(new Line(line, start, end, imageSize, padding));
                }
            }

            stopwatch.Stop();
            runnerLog.Add("GeometryFetchingHelper.FetchLines", stopwatch);

            return res;
        }

        /// <summary>
        /// Moves p1 to p2 until it is inside of original tile. If whole segment is outside of original tile p2 is returned.
        /// </summary>
        public static Point FetchPoint(Point p1, Point p2, int imageSize, int padding)
        {
            foreach (var (x, y) in Bresenham.GetLinePoints(p1.X, p1.Y, p2.X, p2.Y))
            {
                if (IsInsideOriginalImage(x, y, imageSize, padding))
                {
                    return new Point(x, y);
                }
            }

            return new Point(p2.X, p2.Y);
        }

        private static bool IsInsideOriginalImage(int x, int y, int imageSize, int padding)
        {
            return padding <= x && x < imageSize + padding && padding <= y && y < imageSize + padding;
        }
    }
}