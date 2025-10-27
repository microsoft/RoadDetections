// <copyright file="Road.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Stitching
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;

    public class Road
    {
        public Road(StitchedLine stitchedLine, int resolution)
        {
            this.Points = new List<LatLong>();

            foreach (var point in stitchedLine.Points)
            {
                this.Points.Add(BingMapsTileSystemHelper.PixelXYToLatLong(point.X, point.Y, resolution));
            }

            this.WidthMeters = GeoHelper.PixelsToMeters(stitchedLine.Width, GeoHelper.GetRoadCenter(this.Points), resolution);
        }

        public Road(List<LatLong> points, double widthMeters)
        {
            this.Points = points;
            this.WidthMeters = widthMeters;
        }

        public Road(string geojson)
        {
            this.Points = GeoHelper.ParseLineString(geojson, GeometryRepresentation.GeoJson);

            if (this.Points.Count == 0)
            {
                throw new Exception("Road constructor doesn't support empty geojson");
            }

            this.WidthMeters = double.Parse(JsonUtils.GetProperty(geojson, "WidthMeters"));
        }

        public List<LatLong> Points { get; private set; }

        public double WidthMeters { get; private set; }

        /// <summary>
        /// Calculates similarity score 0.0 is returned for equal roads.
        /// </summary>
        /// <param name="other">Other road to calculate similarity to.</param>
        /// <param name="resolution"></param>
        /// <returns>Similarity score.</returns>
        public double GetSimilarityScore(Road other, int resolution)
        {
            var line1 = new StitchedLine(this, resolution);
            var line2 = new StitchedLine(other, resolution);

            double res = 0.0;
            for (int i = 0; i < line1.Points.Count; i++)
            {
                double minDist = 1e9;
                for (int j = 0; j < line2.Points.Count; j++)
                {
                    minDist = Math.Min(minDist, GeometryHelper.Distance(line1.Points[i].X, line1.Points[i].Y, line2.Points[j].X, line2.Points[j].Y));
                }

                for (int j = 0; j + 1 < line2.Points.Count; j++)
                {
                    minDist = Math.Min(minDist, GeometryHelper.DistanceToSegment(line1.Points[i].X, line1.Points[i].Y, line2.Points[j].X, line2.Points[j].Y, line2.Points[j + 1].X, line2.Points[j + 1].Y));
                }

                res = Math.Max(res, minDist);
            }

            for (int i = 0; i < line2.Points.Count; i++)
            {
                double minDist = 1e9;
                for (int j = 0; j < line1.Points.Count; j++)
                {
                    minDist = Math.Min(minDist, GeometryHelper.Distance(line2.Points[i].X, line2.Points[i].Y, line1.Points[j].X, line1.Points[j].Y));
                }

                for (int j = 0; j + 1 < line1.Points.Count; j++)
                {
                    minDist = Math.Min(minDist, GeometryHelper.DistanceToSegment(line2.Points[i].X, line2.Points[i].Y, line1.Points[j].X, line1.Points[j].Y, line1.Points[j + 1].X, line1.Points[j + 1].Y));
                }

                res = Math.Max(res, minDist);
            }

            return res;
        }

        public List<Tuple<string, Road>> Split(double splitLength)
        {
            var split = GeoHelper.Split(this.Points, splitLength);
            var res = new List<Tuple<string, Road>>();
            for (int i = 0; i < split.Count; i++)
            {
                res.Add(Tuple.Create(i.ToString(), new Road(split[i], this.WidthMeters)));
            }

            return res;
        }

        public string ToGeoJson()
        {
            var res = GeoJsonUtils.ConstructLineStringGeoJson(this.Points.Select(latlong => Tuple.Create(latlong.Longitude, latlong.Latitude)));
            res = JsonUtils.AddProperty(res, "WidthMeters", this.WidthMeters.ToString());

            return res;
        }
    }
}