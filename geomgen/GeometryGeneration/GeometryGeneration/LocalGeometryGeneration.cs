// <copyright file="LocalRunner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace RoadExtractor.Main
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using GeoUtils.BingMapsTileSystem;
    using ImageUtils.Images;
    using PostProcessing.Helpers;
    using PostProcessing.Main;
    using Utils;

    internal static class LocalGeometryGeneration
    {
        /// <summary>
        /// Running geometry generation pipeline for specified quadkeys.
        /// this.pred and this.rgb are expected to have both each quadkey from quadkeysToProcess as well as all surrounding quadkeys of those
        /// </summary>
        /// <param name="predPath">A path to a file produced by NN (schema [quadkey15, base64graypred, base64binpred]).</param>
        /// <param name="rgbPath">A path to a file used by NN (schema [quadkey15, base64rgbimage]).</param>
        /// <param name="config">configuration.</param>
        /// <param name="resolution">resolution.</param>
        /// <param name="quadkeysToProcess">quadkeys to process (LOD15).</param>
        /// <param name="outputDir">ouput dir.</param>
        /// <returns></returns>
        internal static Dictionary<string, PostProcessorResult> Run(string predPath, string rgbPath, Configuration config, int resolution, string[] quadkeysToProcess, string outputDir)
        {
            var pred = new Dictionary<string, Tuple<string, string>>();
            foreach (var line in File.ReadLines(predPath))
            {
                var (quadkey, img, bin) = line.Split('\t');
                pred.Add(quadkey, Tuple.Create(img, bin));
            }

            var rgbs = new Dictionary<string, string>();
            foreach (var line in File.ReadLines(rgbPath))
            {
                var (quadkey, rgb) = line.Split('\t');
                rgbs.Add(quadkey, rgb);
            }

            var dataset = new Dictionary<string, Tuple<string, string>>();
            foreach (var (quadkey, (img, bin)) in pred)
            {
                var imgTrimmed = BlackWhiteImage.CreateImageFromBase64(img).CutAllPadding().ToBase64String();
                var binTrimmed = BinaryImage.CreateImageFromBase64(bin).CutAllPadding().ToBase64String();

                dataset.Add(quadkey, Tuple.Create(imgTrimmed, binTrimmed));
            }

            var res = new Dictionary<string, PostProcessorResult>();
            foreach (var quadkeyToProcess in quadkeysToProcess)
            {
                res.Add(quadkeyToProcess, RunOne(quadkeyToProcess, dataset, config, resolution, outputDir));

                // visualization
                var rgb = RgbImage.CreateImageFromBase64(rgbs[quadkeyToProcess]);
                rgb = DrawHelper.DrawTileEdges(rgb, false, quadkeyToProcess.Length, resolution);
                var padding = (rgb.Width - (1 << GenericHelper.Log2(rgb.Width))) / 2;
                var tileSize = BingMapsTileSystemHelper.GetTileSize(quadkeyToProcess, resolution);
                rgb.AddLabel(DrawHelper.Draw(res[quadkeyToProcess].Lines, new BinaryImage(tileSize), -1), Color.Red, padding).
                    Save(outputDir + quadkeyToProcess + "-rgb-res.png");
            }

            return res;
        }

        private static PostProcessorResult RunOne(string quadkeyToProcess, Dictionary<string, Tuple<string, string>> pred, Configuration config, int resolution, string outputDir)
        {
            var watch = new Stopwatch();
            watch.Start();
            var tiles = new List<Tuple<Tuple<int, int>, string, string>>();
            foreach (var (quadkey15, coords) in QuadkeyOrientationHelper.GetNeighbouringQuadkeys(quadkeyToProcess))
            {
                tiles.Add(Tuple.Create(coords, pred[quadkey15].Item1, pred[quadkey15].Item2));
            }

            Console.WriteLine("Start " + quadkeyToProcess);
            var processed = GeometryGenerationHelper.Process(tiles, quadkeyToProcess.Length, config, quadkeyToProcess, resolution, outputDir + quadkeyToProcess + "-");

            watch.Stop();
            Console.WriteLine(quadkeyToProcess + "\tTook " + watch.ElapsedMilliseconds + " ms");
            Console.WriteLine("Done");

            return processed;
        }
    }
}
