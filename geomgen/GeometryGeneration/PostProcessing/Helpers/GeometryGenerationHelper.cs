// <copyright file="GeometryGenerationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using ImageUtils.Images;
    using PostProcessing.Main;
    using PostProcessing.Stitching;
    using Utils;

    public static class GeometryGenerationHelper
    {
        /// <summary>
        /// Generates geometry from prediction of aerial images. Images size should be a power of 2 (256 , 512, 1024, ...)
        /// </summary>
        public static PostProcessorResult Process(List<Tuple<Tuple<int, int>, string, string>> data, int processingLevel, Configuration config, string quadkey, int resolution, string path = "")
        {
            var tileGrayImg = data.Select(tuple => Tuple.Create(tuple.Item1, tuple.Item2));
            var tileBinImg = data.Select(tuple => Tuple.Create(tuple.Item1, tuple.Item3));

            var processingSize = BingMapsTileSystemHelper.GetTileSize(processingLevel, resolution);
            var gryImage = GeometryGenerationHelper.CombineNeighborImages(tileGrayImg, processingSize, config.ProcessingPadding);
            var binImage = GeometryGenerationHelper.CombineNeighborImages(tileBinImg, processingSize, config.ProcessingPadding);

            return GeometryGenerationHelper.Process(gryImage, binImage, config.ProcessingPadding, config, quadkey, resolution, path);
        }

        /// <summary>
        /// Generates geometry from prediction of validation set images.
        /// </summary>
        public static PostProcessorResult Process(string predStr, string predBinStr, Configuration config, string quadkey, int resolution, string path = "")
        {
            var gryImage = BlackWhiteImage.CreateImageFromBase64(predStr);
            var binImage = BlackWhiteImage.CreateImageFromBase64(predBinStr);
            int size = gryImage.Width;
            if (gryImage.Width != size || gryImage.Width != size || binImage.Width != size || binImage.Width != size || size < 1088)
            {
                throw new Exception("Expected images of equal size >= 1088");
            }

            return GeometryGenerationHelper.Process(gryImage, binImage, (size - 1024) / 2, config, quadkey, resolution, path);
        }

        private static BlackWhiteImage CombineNeighborImages(IEnumerable<Tuple<Tuple<int, int>, string>> tiles, int imageSize, int padding)
        {
            var image = new BlackWhiteImage(3 * imageSize, 3 * imageSize);

            if (tiles.Count() == 9 &&
                tiles.Select(tuple => tuple.Item1).Distinct().Count() == 9 &&
                tiles.All(tuple => GenericHelper.IsBetween(tuple.Item1.Item1, -1, 1) && GenericHelper.IsBetween(tuple.Item1.Item2, -1, 1)) &&
                tiles.All(tuple => tuple.Item2 != null))
            {
                foreach (var tile in tiles)
                {
                    var (di, dj) = tile.Item1;
                    var img = BlackWhiteImage.CreateImageFromBase64(tile.Item2);

                    if (img.Width != imageSize || img.Height != imageSize)
                    {
                        throw new Exception("Provided images should be of size " + imageSize + ". Found an image with width=" + img.Width + " and height=" + img.Height);
                    }

                    image.AddWithPadding(img, imageSize + (di * imageSize), imageSize + (dj * imageSize));
                }
            }
            ////else
            ////{
            ////    throw new Exception("Expected to get 9 images to generate geometry, " +
            ////        "got tiles.Count()='" + tiles.Count() + "', " +
            ////        "tiles.Select(tuple => tuple.Item1).Distinct().Count()='" + tiles.Select(tuple => tuple.Item1).Distinct().Count() + "', " +
            ////        "coords = '" + string.Join(",", tiles.Select(tuple => "(" + tuple.Item1.Item1 + "," + tuple.Item1.Item2 + ")")) + "', " +
            ////        "nulls = '" + string.Join(",", tiles.Select(tuple => tuple.Item2 == null ? "null" : "ok")) + "'");
            ////}

            int resImageSize = imageSize + padding + padding;
            image = image.Cut(imageSize - padding, imageSize - padding, resImageSize, resImageSize);
            return image;
        }

        private static PostProcessorResult Process(BlackWhiteImage image, BlackWhiteImage binImage, int padding, Configuration config, string quadkey, int resolution, string path = "")
        {
            if (binImage.IsBlack())
            {
                return new PostProcessorResult { Lines = new List<Line>(), MatchScore = new Metrics.PixelMetric(0, 0, 0, 0), RunnerLog = new RunnerLog() };
            }

            var res = PostProcessor.Run(image, binImage, ref padding, config, quadkey, resolution, path);
            res.Lines = GeometryFetchingHelper.FetchLines(res.Lines, BingMapsTileSystemHelper.GetTileSize(quadkey, resolution), padding, res.RunnerLog);
            for (int i = 0; i < res.Lines.Count; i++)
            {
                for (int j = 0; j < res.Lines[i].Points.Count; j++)
                {
                    res.Lines[i].Points[j] = new Point(res.Lines[i].Points[j].X - padding, res.Lines[i].Points[j].Y - padding);
                    if (!GenericHelper.IsBetween(res.Lines[i].Points[j].X, 0, BingMapsTileSystemHelper.GetTileSize(quadkey, resolution) - 1) ||
                        !GenericHelper.IsBetween(res.Lines[i].Points[j].Y, 0, BingMapsTileSystemHelper.GetTileSize(quadkey, resolution) - 1))
                    {
                        throw new Exception("Point is outside of range: " + res.Lines[i].Points[j]);
                    }
                }
            }

            return res;
        }
    }
}