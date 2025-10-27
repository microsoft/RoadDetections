// <copyright file="LocalRunner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace RoadExtractor.Main
{
    using GeoUtils.BingMapsTileSystem;
    using PostProcessing.Main;
    using PostProcessing.Stitching;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utils;

    internal static class LocalStitching
    {
        internal static List<StitchedLine> Run(Dictionary<string, List<Line>> allLines, int geometryProcessingLevel, Configuration config, int resolution)
        {
            var data = new List<Tuple<string, int, string, bool>>();
            foreach (var (bb, lines) in allLines)
            {
                foreach (var line in lines)
                {
                    var tileSize = BingMapsTileSystemHelper.GetTileSize(bb, resolution);
                    var isTouchingTileEnd = StitchingLinesHelper.IsTouchingTileEnd(tileSize, line);
                    data.Add(Tuple.Create(bb, tileSize, Line.JsonSerialize(line), isTouchingTileEnd));
                }
            }

            var res = data.Where(tuple => tuple.Item4 == false).Select(tuple => tuple.Item3).ToList();

            for (int i = 0; i < geometryProcessingLevel; i++)
            {
                var curData = new Dictionary<Tuple<string, int>, List<string>>();
                foreach (var (quadkey, tileSize, line, touchingTileEnd) in data.Where(tuple => tuple.Item4 == true))
                {
                    var subquadkey = quadkey.Substring(0, quadkey.Length - 1);
                    GenericHelper.AddToDictionary(curData, Tuple.Create(subquadkey, tileSize), line);
                }

                data.Clear();

                foreach (var ((subquadkey, tileSize), lineGeoJsons) in curData)
                {
                    var lines = lineGeoJsons.Select(line => Line.FromGeoJson(line)).ToList();
                    foreach (var line in StitchingLinesHelper.ProcessLines(tileSize, lines, config))
                    {
                        data.Add(Tuple.Create(subquadkey, 2 * tileSize, Line.JsonSerialize(line), StitchingLinesHelper.IsTouchingTileEnd(2 * tileSize, line)));
                    }
                }

                res.AddRange(data.Where(tuple => tuple.Item4 == false).Select(tuple => tuple.Item3));
            }

            return res.Select(lineGeoJson => new StitchedLine(Line.FromGeoJson(lineGeoJson), resolution))
                      .Where(stitchedLine => stitchedLine.IsDegenerate() == false).ToList();
        }
    }
}
