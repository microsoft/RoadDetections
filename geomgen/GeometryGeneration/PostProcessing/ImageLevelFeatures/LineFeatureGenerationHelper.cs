// <copyright file="LineFeatureGenerationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.ImageLevelFeatures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using ImageUtils;
    using ImageUtils.Images;
    using PostProcessing.Helpers.LineProjecting;
    using PostProcessing.Stitching;
    using Utils;
    using Pixels = System.Collections.Generic.List<System.Tuple<int, int>>;

    public static class LineFeatureGenerationHelper
    {
        /// <summary>
        /// Generates pixels at given quadkeyLength that correspond to the given line with the given width
        /// Return format is following {[quadkey, {[x, y]}]}.
        /// </summary>
        public static List<Tuple<string, Pixels>> GeneratePixels(StitchedLine stitchedLine, int quadkeyLength, int widthMultiplier, int resolution)
        {
            var labelProjector = new GlobalLineProjector();
            var res = new Dictionary<string, HashSet<Tuple<int, int>>>();
            for (int i = 0; i + 1 < stitchedLine.Points.Count; i++)
            {
                foreach (var point in labelProjector.GetPoints(stitchedLine.Points[i].X, stitchedLine.Points[i].Y, stitchedLine.Points[i + 1].X, stitchedLine.Points[i + 1].Y, widthMultiplier * stitchedLine.Width))
                {
                    var quadkey = BingMapsTileSystemHelper.PixelXYToQuadkey(point.Item1, point.Item2, quadkeyLength, resolution);
                    BingMapsTileSystemHelper.GetCoordinatesInChildQuadkey(string.Empty, point.Item1, point.Item2, quadkey, out var px, out var py, resolution);
                    GenericHelper.AddToDictionary(res, quadkey, Tuple.Create(px, py));
                }
            }

            return res.Select(kvp => Tuple.Create(kvp.Key, kvp.Value.ToList())).ToList();
        }

        /// <summary>
        /// Breaks the given line into segments of length (width * splitWidthFactor) px
        /// Generates pixels at given quadkeyLength that correspond to the given line with the given width
        /// Return format is following {[quadkey, {[segmengid, x, y]}]}.
        /// </summary>
        public static List<Tuple<string, List<Tuple<int, int, int>>>> GeneratePixelsSplit(StitchedLine stitchedLine, int quadkeyLength, int resolution)
        {
            // break line into smaller equal sized segments
            var segments = BreakEvenly(stitchedLine, stitchedLine.Width);

            // expand to the size of the road by width
            var res = new Dictionary<string, HashSet<Tuple<int, int, int>>>();
            for (int i = 0; i < segments.Count; i++)
            {
                foreach (var point in GetPointsAround(segments[i], (stitchedLine.Width - 1) / 2))
                {
                    var quadkey = BingMapsTileSystemHelper.PixelXYToQuadkey(point.Item1, point.Item2, quadkeyLength, resolution);
                    BingMapsTileSystemHelper.GetCoordinatesInChildQuadkey(string.Empty, point.Item1, point.Item2, quadkey, out var px, out var py, resolution);
                    GenericHelper.AddToDictionary(res, quadkey, Tuple.Create(i, px, py));
                }
            }

            return res.Select(kvp => Tuple.Create(kvp.Key, kvp.Value.ToList())).ToList();
        }

        /// <summary>
        /// Generates image-level features (ColorStats).
        /// </summary>
        /// <param name="data">List of pixels, CNN's preditions and rgb image.</param>
        /// <param name="withTileFeatures">If this is set to true features of the whole quadkeys are calculated too.</param>
        /// <returns></returns>
        public static string GetColorStats(List<Tuple<Pixels, string, string, string>> data, bool withTileFeatures)
        {
            // list of colorStat objects with corresponding weights
            var lineCsList = new List<Tuple<ColorStats, int>>();
            var tileCsList = new List<Tuple<ColorStats, int>>();
            foreach (var tuple in data)
            {
                var pixels = tuple.Item1;
                var gry = BlackWhiteImage.CreateImageFromBase64(tuple.Item2);
                var bin = BlackWhiteImage.CreateImageFromBase64(tuple.Item3);
                var rgb = RgbImage.CreateImageFromBase64(tuple.Item4);

                if (withTileFeatures)
                {
                    tileCsList.Add(Tuple.Create(new ColorStats(gry, bin, rgb, null), pixels.Count));
                }

                lineCsList.Add(Tuple.Create(new ColorStats(gry, bin, rgb, pixels), pixels.Count));
            }

            var features = new Dictionary<string, double>();
            if (withTileFeatures)
            {
                new ColorStats(tileCsList).FillFeatures("all", features);
            }

            new ColorStats(lineCsList).FillFeatures(string.Empty, features);

            features.Add("pixels", lineCsList.Sum(cs => cs.Item2));

            return LineFeatureGenerationHelper.SerializeFeatures(features);
        }

        public static string GetColorStats(List<Tuple<ColorStats, int>> lineCsList, List<Tuple<ColorStats, int>> tileCsList, bool withTileFeatures)
        {
            var features = new Dictionary<string, double>();
            if (withTileFeatures)
            {
                new ColorStats(tileCsList).FillFeatures("all", features);
            }

            new ColorStats(lineCsList).FillFeatures(string.Empty, features);

            features.Add("pixels", lineCsList.Sum(cs => cs.Item2));

            return LineFeatureGenerationHelper.SerializeFeatures(features);
        }

        /// <summary>
        /// Given image-level features of segments of the line, contrast features are calculated.
        /// </summary>
        /// <param name="serFeatures">List of image features of segments with segment id.</param>
        /// <returns></returns>
        public static string GetLineContrasts(List<Tuple<int, ColorStats>> serFeatures)
        {
            var features = serFeatures.OrderBy(tuple => tuple.Item1).ToList();

            var diffListRgb = new List<double>();
            var diffListBin = new List<double>();
            var diffListGry = new List<double>();

            for (int i = 0; i + 1 < features.Count; i++)
            {
                diffListRgb.Add(features[i].Item2.GetRGBDiff(features[i + 1].Item2));
                diffListBin.Add(features[i].Item2.GetBinDiff(features[i + 1].Item2));
                diffListGry.Add(features[i].Item2.GetGryDiff(features[i + 1].Item2));
            }

            double avgDiffRgb = diffListRgb.Count > 0 ? diffListRgb.Average() : 0.0;
            double maxDiffRgb = diffListRgb.Count > 0 ? diffListRgb.Max() : 0.0;

            double avgDiffBin = diffListBin.Count > 0 ? diffListBin.Average() : 0.0;
            double maxDiffBin = diffListBin.Count > 0 ? diffListBin.Max() : 0.0;

            double avgDiffGry = diffListGry.Count > 0 ? diffListGry.Average() : 0.0;
            double maxDiffGry = diffListGry.Count > 0 ? diffListGry.Max() : 0.0;

            return LineFeatureGenerationHelper.SerializeFeatures(new Dictionary<string, double>
            {
                { "maxDiffRgb", maxDiffRgb },
                { "avgDiffRgb", avgDiffRgb },
                { "maxDiffBin", maxDiffBin },
                { "avgDiffBin", avgDiffBin },
                { "maxDiffGry", maxDiffGry },
                { "avgDiffGry", avgDiffGry },
            });
        }

        /// <summary>
        /// Expands the line represented by points so that the line's width is 2*r.
        /// </summary>
        /// <param name="points">Points representing a line.</param>
        /// <param name="r">Radius.</param>
        /// <returns></returns>
        private static HashSet<Tuple<int, int>> GetPointsAround(Pixels points, int r)
        {
            var res = new HashSet<Tuple<int, int>>();

            foreach (var point in points)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        res.Add(Tuple.Create(point.Item1 + dx, point.Item2 + dy));
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Given a line, this method splits into points and breaks evenly so that each segment is approximately distance long.
        /// </summary>
        /// <param name="stitchedLine">A line to be broken.</param>
        /// <param name="distance">Length of the resulting segments.</param>
        /// <returns></returns>
        private static List<Pixels> BreakEvenly(StitchedLine stitchedLine, double distance)
        {
            var filledLine = new Pixels();
            for (int i = 0; i + 1 < stitchedLine.Points.Count; i++)
            {
                var oneBlob = Bresenham.GetLinePoints(stitchedLine.Points[i].X, stitchedLine.Points[i].Y, stitchedLine.Points[i + 1].X, stitchedLine.Points[i + 1].Y).ToList();
                oneBlob.RemoveAt(oneBlob.Count - 1);
                filledLine.AddRange(oneBlob);
            }

            filledLine.Add(Tuple.Create(stitchedLine.Points.Last().X, stitchedLine.Points.Last().Y));

            int parts = Math.Max(GenericHelper.Round(filledLine.Count / distance), 1);

            var res = new List<Pixels>();
            for (int i = 0; i < filledLine.Count;)
            {
                int pointsToAdd = GenericHelper.Round((filledLine.Count - i) * 1.0 / (parts - res.Count));
                var cur = new Pixels();
                for (int j = 0; j < pointsToAdd && i + j < filledLine.Count; j++)
                {
                    cur.Add(filledLine[i + j]);
                }

                i += pointsToAdd;
                res.Add(cur);
            }

            return res;
        }

        private static string SerializeFeatures(Dictionary<string, double> container)
        {
            return string.Join(",", container.Select(kvp => kvp.Key + ":" + kvp.Value));
        }
    }
}
