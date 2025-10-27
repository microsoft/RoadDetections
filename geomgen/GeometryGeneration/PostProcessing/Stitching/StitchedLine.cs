// <copyright file="StitchedLine.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Stitching
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;
    using Utils;

    public class StitchedLine
    {
        public StitchedLine(Line line, int resolution)
        {
            this.Width = line.Width;
            this.Points = new List<Point>();
            foreach (var point in line.Points)
            {
                BingMapsTileSystemHelper.GetCoordinatesInParentQuadkey(line.BoundingBoxQuadkey, point.X, point.Y, string.Empty, out var x, out var y, resolution);
                this.Points.Add(new Point(x, y));
            }

            this.ToCanonicalForm();
        }

        public StitchedLine(Road road, int resolution)
        {
            this.Width = GenericHelper.Round(GeoHelper.MetersToPixelsDouble(road.WidthMeters, GeoHelper.GetRoadCenter(road.Points), resolution));
            this.Points = new List<Point>();
            foreach (var latlong in road.Points)
            {
                BingMapsTileSystemHelper.LatLongToPixelXY(latlong.Latitude, latlong.Longitude, out var px, out var py, resolution);
                this.Points.Add(new Point(px, py));
            }
        }

        private StitchedLine(List<Point> points, int width)
        {
            this.Width = width;
            this.Points = points;
        }

        public List<Point> Points { get; private set; }

        public int Width { get; private set; }

        public static string Serialize(StitchedLine line)
        {
            var res = GeoJsonUtils.ConstructLineStringGeoJson(line.Points.Select(point => Tuple.Create(point.X, point.Y)));
            res = JsonUtils.AddProperty(res, "Width", line.Width.ToString());

            return res;
        }

        public static StitchedLine Deserialize(string geojson)
        {
            var width = int.Parse(JsonUtils.GetProperty(geojson, "Width"));
            var points = new List<Point>();
            foreach (var point in GeoJsonUtils.GetCoordinates<int>(geojson))
            {
                if (point.Length != 2)
                {
                    throw new Exception("Expected to find 2 coordfinates per point, found '" + point.Length + "' in this json:" + geojson);
                }

                points.Add(new Point(point[0], point[1]));
            }

            return new StitchedLine(points, width);
        }

        public bool IsDegenerate()
        {
            return this.Points.Count < 2 || this.Points.All(point => point.Equals(this.Points[0]));
        }

        private void ToCanonicalForm()
        {
            var line_crds = string.Join("|", this.Points.Select(point => point.X + "," + point.Y));
            var line_crds_rev = string.Join("|", this.Points.Select(point => point.X + "," + point.Y).Reverse());
            if (line_crds.CompareTo(line_crds_rev) == 1)
            {
                this.Points.Reverse();
            }
        }
    }
}
