// <copyright file="BingMapsTileSystemHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeometryGeneration
{
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;
    using PostProcessing.Main;
    using PostProcessing.Stitching;
    using RoadExtractor.Main;
    using System.IO;
    using System.Linq;

    internal class Program
    {
        static void Main()
        {
            var rootdir = @"..\..\..\..\..\test\";
            var quadkeysToProcess = new[] { "032000222133032", "030222012212210", "023102203203200" };

            var config = Configuration.Default;
            var resolution = BingMapsTileSystemHelper.DefaultResolution;
            var lod = 15;

            // geometry generation
            var geoms = LocalGeometryGeneration.Run(rootdir + "pred.txt", rootdir + "rgbs.txt", config, resolution, quadkeysToProcess, rootdir);

            // stitching
            var stitchedLines = LocalStitching.Run(geoms.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Lines), lod, config, resolution);

            // converting to geojsons
            var geoJsons = stitchedLines.Select(stitchedLine => new Road(stitchedLine, resolution).ToGeoJson()).ToList();

            File.WriteAllLines(rootdir + "geojsons.tsv", geoJsons);
            File.WriteAllText(rootdir + "geojsons.geojson", GeoJsonUtils.ToFeatureCollection(geoJsons.ToArray()));
        }
    }
}
