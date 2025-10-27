// <copyright file="Line.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Stitching
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;
    using PostProcessing.Annealing;
    using PostProcessing.Helpers;
    using Utils;

    public class Line
    {
        public Line(Road road, string quadkey, int resolution)
        {
            this.BoundingBoxQuadkey = quadkey;
            this.Width = GenericHelper.Round(GeoHelper.MetersToPixelsDouble(road.WidthMeters, GeoHelper.GetRoadCenter(road.Points), resolution));
            this.Points = new List<Point>();
            foreach (var latlong in road.Points)
            {
                BingMapsTileSystemHelper.LatLongToPixelXY(latlong.Latitude, latlong.Longitude, out var x, out var y, resolution);
                BingMapsTileSystemHelper.GetCoordinatesInChildQuadkey(string.Empty, x, y, quadkey, out var resx, out var resy, resolution);
                this.Points.Add(new Point(resx, resy));
            }
        }

        public Line(List<LatLong> road, string quadkey, int resolution, int width)
        {
            this.BoundingBoxQuadkey = quadkey;
            this.Width = width;
            this.Points = new List<Point>();
            foreach (var p in road)
            {
                BingMapsTileSystemHelper.LatLongToPixelXY(p.Latitude, p.Longitude, out var x, out var y, resolution);
                BingMapsTileSystemHelper.GetCoordinatesInChildQuadkey(string.Empty, x, y, quadkey, out var resx, out var resy, resolution);
                this.Points.Add(new Point(resx, resy));
            }
        }

        public Line(IEnumerable<Point> points, string boundingBoxQuadkey, int width)
        {
            this.BoundingBoxQuadkey = boundingBoxQuadkey;
            this.Width = width;
            this.Points = new List<Point>();
            foreach (var point in points)
            {
                this.Points.Add(new Point(point.X, point.Y));
            }
        }

        public Line(string quadkey, List<int> eidList, List<Edge> edges, ConnectionsGraph graph)
        {
            this.BoundingBoxQuadkey = quadkey;

            // find start
            var pointCount = new SortedDictionary<Tuple<int, int>, List<int>>();
            foreach (int eid in eidList)
            {
                foreach (var point in edges[eid].Points)
                {
                    if (!pointCount.ContainsKey(point))
                    {
                        pointCount.Add(point, new List<int>());
                    }

                    pointCount[point].Add(eid);
                }
            }

            var points = pointCount.Keys.ToHashSet();
            int minCon = pointCount.Values.Min(list => list.Count);
            var startPoint = pointCount.First(kvp => kvp.Value.Count == minCon);
            var secondPoint = edges[startPoint.Value.First()].Points.First(point => point.Item1 != startPoint.Key.Item1 || point.Item2 != startPoint.Key.Item2);

            this.Points = new List<Point>
            {
                new Point(startPoint.Key.Item1, startPoint.Key.Item2),
                new Point(secondPoint.Item1, secondPoint.Item2),
            };

            for (int i = 0; i < eidList.Count - 1; i++)
            {
                try
                {
                    graph.GetFirstNeighbor(this.Points[this.Points.Count - 1].X, this.Points[this.Points.Count - 1].Y, out var x, out var y, this.Points[this.Points.Count - 2].X, this.Points[this.Points.Count - 2].Y, points);
                    this.Points.Add(new Point(x, y));
                }
                catch (Exception exc)
                {
                    var message = "Can't find start of a line while constructing an object";
                    message += "\tnodes = " + graph.RealNodes;
                    message += "\teidlist.Count = " + eidList.Count;
                    message += "\tedges:";
                    foreach (var eid in eidList)
                    {
                        message += " " + eid + " " + "{" + edges[eid] + "}";
                    }

                    message += "|||";
                    foreach (var point in this.Points)
                    {
                        message += "(" + point.X + "," + point.Y + "):";
                        message += string.Join("|", graph.GetNeighbors(point.X, point.Y).Select(tuple => tuple.Item1 + "," + tuple.Item2 + "," + tuple.Item3));
                    }

                    throw new Exception(exc.Message + " failed on " + i + " " + message);
                }
            }

            this.Width = 0;
            foreach (var eid in eidList)
            {
                this.Width = Math.Max(this.Width, edges[eid].Width);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Line"/> class.
        /// Creates a Line from other Line object from points [start, end] of it,
        /// but also some part of [start-1, start] and [end, end+1] can be used in new object if it is inside
        /// of tile with size [imageSize][imageSize] from tile of size [imageSize+2*padding][imageSize+2*padding].
        /// </summary>
        public Line(Line other, int startPoint, int endPoint, int imageSize, int padding)
        {
            this.Points = new List<Point>();
            this.BoundingBoxQuadkey = other.BoundingBoxQuadkey;
            this.Width = other.Width;

            if (startPoint - 1 >= 0)
            {
                var start = GeometryFetchingHelper.FetchPoint(other.Points[startPoint - 1], other.Points[startPoint], imageSize, padding);
                if (start.X != other.Points[startPoint].X || start.Y != other.Points[startPoint].Y)
                {
                    this.Points.Add(start);
                }
            }

            for (int i = startPoint; i <= endPoint; i++)
            {
                this.Points.Add(other.Points[i]);
            }

            if (endPoint + 1 < other.Points.Count)
            {
                var end = GeometryFetchingHelper.FetchPoint(other.Points[endPoint + 1], other.Points[endPoint], imageSize, padding);
                if (end.X != other.Points[endPoint].X || end.Y != other.Points[endPoint].Y)
                {
                    this.Points.Add(end);
                }
            }
        }

        public List<Point> Points { get; set; }

        public int Width { get; set; }

        public double Length
        {
            get
            {
                double res = 0.0;
                for (int i = 0; i + 1 < this.Points.Count; i++)
                {
                    res += GeometryHelper.Distance(this.Points[i].X, this.Points[i].Y, this.Points[i + 1].X, this.Points[i + 1].Y);
                }

                return res;
            }
        }

        public string BoundingBoxQuadkey { get; set; }

        public static Line FromGeoJson(string geojson)
        {
            var boundingBoxQuadkey = JsonUtils.GetProperty(geojson, "BoundingBoxQuadkey");
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

            return new Line(points, boundingBoxQuadkey, width);
        }

        public static string JsonSerialize(Line line)
        {
            return JsonSerialize(new List<Line> { line });
        }

        public static string JsonSerialize(List<Line> lines)
        {
            var res = new List<string>();
            foreach (var line in lines)
            {
                var gj = GeoJsonUtils.ConstructLineStringGeoJson(line.Points.Select(point => Tuple.Create(point.X, point.Y)));
                gj = JsonUtils.AddProperty(gj, "BoundingBoxQuadkey", line.BoundingBoxQuadkey);
                gj = JsonUtils.AddProperty(gj, "Width", line.Width.ToString());

                res.Add(gj);
            }

            return string.Join("|", res);
        }

        public static List<Line> JsonDeserialize(string data)
        {
            var res = new List<Line>();

            if (string.IsNullOrEmpty(data))
            {
                return res;
            }

            foreach (var item in data.Split('|'))
            {
                res.Add(Line.FromGeoJson(item));
            }

            return res;
        }

        public string GetNormalizedCoordinates()
        {
            var line_crds = string.Join("|", this.Points.Select(point => point.X + "," + point.Y));
            var line_crds_rev = string.Join("|", this.Points.Select(point => point.X + "," + point.Y).Reverse());
            return line_crds.CompareTo(line_crds_rev) == -1 ? line_crds : line_crds_rev;
        }

        public Line Add(int dx, int dy)
        {
            for (int i = 0; i < this.Points.Count; i++)
            {
                this.Points[i].X += dx;
                this.Points[i].Y += dy;
            }

            return this;
        }

        public Line RescaleToParent(string parentQuadkey, int resolution)
        {
            for (int i = 0; i < this.Points.Count; i++)
            {
                BingMapsTileSystemHelper.GetCoordinatesInParentQuadkey(this.BoundingBoxQuadkey, this.Points[i].X, this.Points[i].Y, parentQuadkey, out var x, out var y, resolution);
                this.Points[i].X = x;
                this.Points[i].Y = y;
            }

            this.BoundingBoxQuadkey = parentQuadkey;

            return this;
        }

        public bool IsDegenerate()
        {
            return this.Points.Count < 2 || (this.Points.Count == 2 && this.Points[0].Equals(this.Points[1]));
        }

        public override string ToString()
        {
            return this.Points.Count + ":" + string.Join(",", this.Points);
        }
    }
}
