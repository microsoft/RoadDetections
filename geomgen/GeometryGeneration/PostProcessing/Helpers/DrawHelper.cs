// <copyright file="DrawHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using ImageUtils.Images;
    using PostProcessing.Metrics;
    using PostProcessing.Stitching;

    public static partial class DrawHelper
    {
        public static RgbImage Draw(SparsePlanarGraph graph1, SparsePlanarGraph graph2, IEnumerable<Tuple<int, int>> pairs, int size, int dx, int dy)
        {
            var img = new RgbImage(size + dx, size + dy);
            img = DrawHelper.Draw(graph1, img, 0, 0);
            img = DrawHelper.Draw(graph2, img, dx, dy);
            foreach (var pair in pairs)
            {
                img.DrawLine(graph1.Xs[pair.Item1], graph1.Ys[pair.Item1], graph2.Xs[pair.Item2] + dx, graph2.Ys[pair.Item2] + dy, Color.Green, 1);
            }

            return img;
        }

        public static RgbImage DrawTileEdges(RgbImage image, bool onlyCorners, int quadkeyLevel, int resolution)
        {
            int breakSize = BingMapsTileSystemHelper.GetTileSize(quadkeyLevel, resolution);
            int n = image.Width / breakSize;
            int paddingI = image.Width % breakSize / 2;
            int paddingJ = image.Height % breakSize / 2;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    int starti = paddingI + (breakSize * i);
                    int startj = paddingJ + (breakSize * j);
                    if (onlyCorners)
                    {
                        var color = (i + j) % 2 == 0 ? Color.Blue : Color.Yellow;

                        image.DrawLine(starti, startj, starti + 1, startj, color);
                        image.DrawLine(starti, startj, starti, startj + 1, color);

                        image.DrawLine(starti + breakSize - 1, startj, starti + breakSize - 1 - 1, startj, color);
                        image.DrawLine(starti + breakSize - 1, startj, starti + breakSize - 1, startj + 1, color);

                        image.DrawLine(starti, startj + breakSize - 1, starti + 1, startj + breakSize - 1, color);
                        image.DrawLine(starti, startj + breakSize - 1, starti, startj + breakSize - 1 - 1, color);

                        image.DrawLine(starti + breakSize - 1, startj + breakSize - 1, starti + breakSize - 1 - 1, startj + breakSize - 1, color);
                        image.DrawLine(starti + breakSize - 1, startj + breakSize - 1, starti + breakSize - 1, startj + breakSize - 1 - 1, color);
                    }
                    else
                    {
                        var color = Color.Blue;
                        image.DrawLine(starti, startj, starti + breakSize - 1, startj, color);
                        image.DrawLine(starti, startj, starti, startj + breakSize - 1, color);
                        image.DrawLine(starti + breakSize - 1, startj, starti + breakSize - 1, startj + breakSize - 1, color);
                        image.DrawLine(starti, startj + breakSize - 1, starti + breakSize - 1, startj + breakSize - 1, color);
                    }
                }
            }

            return image;
        }

        public static RgbImage Draw(IEnumerable<Line> lines, RgbImage img, int lineWidth, Color lineColor, int nodesize = -1, int dx = 0, int dy = 0)
        {
            return (RgbImage)Draw(lines, (BaseImage)img, lineWidth, lineColor, nodesize, dx, dy);
        }

        public static BinaryImage Draw(IEnumerable<Line> lines, BinaryImage img, int lineWidth, int nodesize = -1, int dx = 0, int dy = 0)
        {
            return (BinaryImage)Draw(lines, img, lineWidth, Color.White, nodesize, dx, dy);
        }

        public static RgbImage DrawPredAroundQuadkey(string centerQuadkey, int radius, Dictionary<string, string> grayImg, bool drawBorders, int resolution)
        {
            return DrawAroundQuadkey(centerQuadkey, radius, size => new RgbImage(size), quadkey => DrawQuadkey(quadkey), drawBorders, resolution);

            RgbImage DrawQuadkey(string quadkey)
            {
                var cur = new RgbImage(BingMapsTileSystemHelper.GetTileSize(quadkey, resolution));
                if (grayImg.ContainsKey(quadkey.Substring(0, 15)))
                {
                    cur = new RgbImage(BlackWhiteImage.CreateImageFromBase64(grayImg[quadkey.Substring(0, 15)]).Subquadkey(quadkey.Substring(0, 15), quadkey));
                }

                return cur;
            }
        }

        public static RgbImage DrawRgbAroundQuadkey(string centerQuadkey, int radius, Dictionary<string, string> rgbImg, bool drawBorders, int resolution)
        {
            return DrawAroundQuadkey(centerQuadkey, radius, size => new RgbImage(size), quadkey => DrawQuadkey(quadkey), drawBorders, resolution);

            RgbImage DrawQuadkey(string quadkey)
            {
                var cur = new RgbImage(BingMapsTileSystemHelper.GetTileSize(quadkey, resolution));
                if (rgbImg.ContainsKey(quadkey.Substring(0, 15)))
                {
                    cur = RgbImage.CreateImageFromBase64(rgbImg[quadkey.Substring(0, 15)]).Subquadkey(quadkey.Substring(0, 15), quadkey);
                }

                return cur;
            }
        }

        public static RgbImage DrawProcessedAroundQuadkey(string centerQuadkey, int radius, Dictionary<string, List<Line>> lines, int lineWidth, Color lineColor, int nodesize, bool drawBorders, int resolution)
        {
            int linesKeyLength = lines.First().Key.Length;

            return DrawAroundQuadkey(centerQuadkey, radius, size => new RgbImage(size), quadkey => DrawQuadkey(quadkey), drawBorders, resolution);

            RgbImage DrawQuadkey(string quadkey)
            {
                var cur = new RgbImage(BingMapsTileSystemHelper.GetTileSize(quadkey, resolution));
                foreach (var keyQuadkey in QuadkeyOrientationHelper.GetQuadkeys(quadkey, linesKeyLength))
                {
                    if (lines.ContainsKey(keyQuadkey))
                    {
                        QuadkeyOrientationHelper.GetSubquadkeyCoordinates(quadkey, keyQuadkey, out var px, out var py, BingMapsTileSystemHelper.GetTileSize(quadkey, resolution));
                        cur = cur.AddWithPadding(DrawHelper.Draw(lines[keyQuadkey], new RgbImage(cur.Width), lineWidth, lineColor, nodesize), px, py);
                    }
                }

                return cur;
            }
        }

        public static RgbImage DrawStitchedAroundQuadkey(string centerQuadkey, int radius, Dictionary<string, StitchedLine> stitched, bool drawBorders, int resolution)
        {
            return DrawAroundQuadkey(centerQuadkey, radius, size => new RgbImage(size), quadkey => DrawQuadkey(quadkey), drawBorders, resolution);

            RgbImage DrawQuadkey(string quadkey)
            {
                var cur = new RgbImage(BingMapsTileSystemHelper.GetTileSize(quadkey, resolution));
                foreach (var stitchedLine in stitched.Values)
                {
                    var points = new List<Stitching.Point>();
                    foreach (var point in stitchedLine.Points)
                    {
                        BingMapsTileSystemHelper.GetCoordinatesInChildQuadkey(string.Empty, point.X, point.Y, quadkey, out var x, out var y, resolution);
                        points.Add(new Stitching.Point(x, y));
                    }

                    DrawHelper.Draw(new List<Line> { new Line(points, quadkey, stitchedLine.Width) }, cur, 1, Color.White, 3);
                }

                return cur;
            }
        }

        private static BaseImage Draw(IEnumerable<Line> lines, BaseImage img, int lineWidth, Color lineColor, int nodesize = -1, int dx = 0, int dy = 0)
        {
            foreach (var line in lines)
            {
                for (int i = 0; i + 1 < line.Points.Count; i++)
                {
                    img.DrawLine(line.Points[i].X + dx, line.Points[i].Y + dy, line.Points[i + 1].X + dx, line.Points[i + 1].Y + dy, lineColor, lineWidth == -1 ? line.Width : lineWidth);
                }

                if (nodesize != -1)
                {
                    for (int i = 0; i < line.Points.Count; i++)
                    {
                        img.DrawCircle(line.Points[i].X + dx, line.Points[i].Y + dy, nodesize, Color.Blue);
                    }

                    img.DrawCircle(line.Points.First().X + dx, line.Points.First().Y + dy, nodesize, lineColor == Color.Red ? Color.LightGreen : Color.Red);
                    img.DrawCircle(line.Points.Last().X + dx, line.Points.Last().Y + dy, nodesize, lineColor == Color.Red ? Color.LightGreen : Color.Red);
                }
            }

            return img;
        }

        private static RgbImage DrawAroundQuadkey(string centerQuadkey, int radius, Func<int, RgbImage> factory, Func<string, RgbImage> drawQuadkey, bool drawBorders, int resolution)
        {
            var curQuadkey = centerQuadkey;
            for (int i = 0; i < radius; i++)
            {
                curQuadkey = QuadkeyOrientationHelper.TopLeft(curQuadkey);
            }

            int n = (2 * radius) + 1;
            int tileSize = BingMapsTileSystemHelper.GetTileSize(curQuadkey, resolution);
            var res = factory(n * tileSize);
            var iQuadkey = curQuadkey;
            for (int i = 0; i < n; i++)
            {
                var ijQuadkey = iQuadkey;
                for (int j = 0; j < n; j++)
                {
                    var cur = drawQuadkey(ijQuadkey);

                    if (drawBorders)
                    {
                        cur = cur.AddBorder(Color.Blue, 1);
                    }

                    res = res.AddWithPadding(cur, j * tileSize, i * tileSize);

                    ijQuadkey = QuadkeyOrientationHelper.Right(ijQuadkey);
                }

                iQuadkey = QuadkeyOrientationHelper.Bot(iQuadkey);
            }

            return res;
        }

        private static RgbImage Draw(SparsePlanarGraph graph, RgbImage img, int dx, int dy)
        {
            for (int from = 0; from < graph.ConnectionsCount; from++)
            {
                foreach (var to in graph.GetConnections(from))
                {
                    img.DrawLine(graph.Xs[from] + dx, graph.Ys[from] + dy, graph.Xs[to] + dx, graph.Ys[to] + dy, Color.White, 1);
                }
            }

            for (int from = 0; from < graph.ConnectionsCount; from++)
            {
                img.DrawCircle(graph.Xs[from] + dx, graph.Ys[from] + dy, graph.IsControlPoint[from] ? 2 : 1, Color.Red);
            }

            return img;
        }
    }
}
