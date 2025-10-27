// <copyright file="GeoHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using GeoUtils.Geometries;
    using NetTopologySuite.Geometries;

    public static partial class GeoHelper
    {
        public static readonly GeometryFactory GeometryFactory = new GeometryFactory();

        private const double RadiusEarth = 6371e3; // meters

        public static double Distance(LatLong l1, LatLong l2)
        {
            var p1 = l1.Latitude * Math.PI / 180.0;
            var p2 = l2.Latitude * Math.PI / 180.0;
            var dlat = (l2.Latitude - l1.Latitude) * Math.PI / 180.0;
            var dlon = (l2.Longitude - l1.Longitude) * Math.PI / 180.0;

            var a = (Math.Sin(dlat / 2) * Math.Sin(dlat / 2)) +
                    ((Math.Cos(p1) * Math.Cos(p2)) *
                    (Math.Sin(dlon / 2) * Math.Sin(dlon / 2)));
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return RadiusEarth * c;
        }

        /// <summary>
        /// Returns a point dx meters to the west and dy meters to the north of the start point.
        /// </summary>
        public static LatLong Step(LatLong start, double dx, double dy)
        {
            var new_latitude = start.Latitude + (dy / RadiusEarth * 180 / Math.PI);
            var new_longitude = start.Longitude + (-dx / RadiusEarth * 180 / Math.PI / Math.Cos(start.Latitude * Math.PI / 180));

            return new LatLong(new_latitude, new_longitude);
        }

        public static double Distance(List<LatLong> road, LatLong point)
        {
            if (road.Count < 2)
            {
                throw new Exception("A list with more that 1 points expected");
            }

            var res = 1e9;
            for (int i = 0; i + 1 < road.Count; i++)
            {
                var distanceToSegment = GeometryHelper.DistanceToSegment(
                    point.Latitude,
                    point.Longitude,
                    road[i].Latitude,
                    road[i].Longitude,
                    road[i + 1].Latitude,
                    road[i + 1].Longitude,
                    (lat1, lon1, lat2, lon2) => GeoHelper.Distance(new LatLong(lat1, lon1), new LatLong(lat2, lon2)));

                res = Math.Min(res, distanceToSegment);
            }

            return res;
        }

        public static double Distance(List<LatLong> road1, List<LatLong> road2)
        {
            if (road1.Count < 2 || road2.Count < 2)
            {
                throw new Exception("A list with more that 1 points expected");
            }

            var res = 1e9;
            for (int i = 0; i + 1 < road1.Count; i++)
            {
                for (int j = 0; j + 1 < road2.Count; j++)
                {
                    var distanceToSegment = GeometryHelper.DistanceSegmentToSegment(
                    road1[i].Latitude,
                    road1[i].Longitude,
                    road1[i + 1].Latitude,
                    road1[i + 1].Longitude,
                    road2[j].Latitude,
                    road2[j].Longitude,
                    road2[j + 1].Latitude,
                    road2[j + 1].Longitude,
                    (lat1, lon1, lat2, lon2) => GeoHelper.Distance(new LatLong(lat1, lon1), new LatLong(lat2, lon2)));

                    res = Math.Min(res, distanceToSegment);
                }
            }

            return res;
        }

        public static double GetRoadLength(List<LatLong> points)
        {
            double res = 0.0;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                res += GeoHelper.Distance(points[i], points[i + 1]);
            }

            return res;
        }

        public static double GetRoadLength(string geojson)
        {
            return ParseMultiLineString(geojson, GeometryRepresentation.GeoJson).Sum(line => GetRoadLength(line));
        }

        public static LatLong GetRoadCenter(List<LatLong> points)
        {
            if (points.Count == 1)
            {
                return new LatLong(points[0].Latitude, points[0].Longitude);
            }

            var left = GeoHelper.GetRoadLength(points) / 2;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                var len = GeoHelper.Distance(points[i], points[i + 1]);
                if (left > len)
                {
                    left -= len;
                }
                else
                {
                    var dlat = points[i + 1].Latitude - points[i].Latitude;
                    var dlon = points[i + 1].Longitude - points[i].Longitude;

                    return new LatLong(points[i].Latitude + (dlat * left / len), points[i].Longitude + (dlon * left / len));
                }
            }

            throw new Exception("Couldn't get the center");
        }

        public static double GetPolygonArea(string geojson)
        {
            return GetPolygonArea(GeoHelper.ParseGeometry(geojson, GeometryRepresentation.GeoJson));
        }

        public static double MetersToPixelsDouble(double meters, LatLong latlong, int resolution)
        {
            var quadkey = BingMapsTileSystemHelper.LatLongToQuadkey(latlong.Latitude, latlong.Longitude, 17);
            return BingMapsTileSystemHelper.GetTileSize(quadkey, resolution) / new BoundingBox(quadkey).Size * meters;
        }

        public static double MetersToPixelsDouble(double meters, string quadkey, int resolution)
        {
            return BingMapsTileSystemHelper.GetTileSize(quadkey, resolution) / new BoundingBox(quadkey).Size * meters;
        }

        public static int MetersToPixels(double meters, LatLong latlong, int resolution)
        {
            var quadkey = BingMapsTileSystemHelper.LatLongToQuadkey(latlong.Latitude, latlong.Longitude, 17);
            return (int)Math.Ceiling(MetersToPixelsDouble(meters, quadkey, resolution));
        }

        public static int MetersToPixels(double meters, string quadkey, int resolution)
        {
            return (int)Math.Ceiling(MetersToPixelsDouble(meters, quadkey, resolution));
        }

        public static double PixelsToMeters(double pixels, LatLong latlong, int resolution)
        {
            var quadkey = BingMapsTileSystemHelper.LatLongToQuadkey(latlong.Latitude, latlong.Longitude, 17);
            return PixelsToMeters(pixels, quadkey, resolution);
        }

        public static double PixelsToMeters(double pixels, string quadkey, int resolution)
        {
            return new BoundingBox(quadkey).Size * pixels / BingMapsTileSystemHelper.GetTileSize(quadkey, resolution);
        }

        internal static double GetPolygonArea(Geometry geometry)
        {
            if (geometry.GeometryType == "Polygon" || geometry.GeometryType == "MultiPolygon")
            {
                if (geometry.IsEmpty)
                {
                    return 0.0;
                }

                var pixelGeometryArea = new PixelGeometry(geometry, BingMapsTileSystemHelper.MaxResolution).Area;
                return Math.Pow(PixelsToMeters(Math.Sqrt(pixelGeometryArea), new LatLong(geometry.Centroid.Coordinate), BingMapsTileSystemHelper.MaxResolution), 2);
            }
            else if (geometry.GeometryType == "Point")
            {
                return 0.0;
            }
            else
            {
                throw new Exception("Not expected this geometry type: " + geometry.GeometryType);
            }
        }
    }
}