// <copyright file="GeoHelper_Helper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using Newtonsoft.Json;
    using Utils;

    public static partial class GeoHelper
    {
        public static List<Tuple<string, string>> SplitFeatureCollection(string geojson, string nameProperty)
        {
            dynamic g = JsonConvert.DeserializeObject(geojson);
            if (g.type != "FeatureCollection")
            {
                throw new Exception("Expected to get a FeatureCollection, got a '" + g.type + "'.");
            }

            var res = new List<Tuple<string, string>>();
            foreach (var feature in g.features)
            {
                res.Add(Tuple.Create(feature.properties[nameProperty].ToString(), feature.ToString()));
            }

            return res;
        }

        public static List<Tuple<string, LatLong, string>> GetRoadEnds(string geojson, int quadkeyLength, double bufferMeters)
        {
            var geometry = ParseGeometry(geojson, GeometryRepresentation.GeoJson);
            if (geometry.GeometryType == "LineString" || geometry.GeometryType == "MultiLineString" || geometry.GeometryType == "Point")
            {
                var res = new List<Tuple<string, LatLong, string>>();
                foreach (var (latlon, id) in new[] { Tuple.Create(new LatLong(geometry.Coordinates.First()), "first"), Tuple.Create(new LatLong(geometry.Coordinates.Last()), "last") })
                {
                    foreach (var quadkey in BingMapsTileSystemHelper.GetBoundingBoxQuadkeys(new BoundingBox(latlon).AddMargin(bufferMeters), quadkeyLength, -1))
                    {
                        res.Add(Tuple.Create(quadkey, latlon, id));
                    }
                }

                return res;
            }
            else
            {
                throw new Exception("Unexpected geoemtry type: " + geometry.GeometryType);
            }
        }

        public static List<string> Split(string geojson, double splitLength)
        {
            return Split(ParseLineString(geojson, GeometryRepresentation.GeoJson), splitLength).Select(latlongs => GeoJsonUtils.ConstructLineStringGeoJson(latlongs)).ToList();
        }

        public static List<List<LatLong>> Split(List<LatLong> points, double splitLength)
        {
            double length = GeoHelper.GetRoadLength(points);
            int cnt = GenericHelper.Round(length / splitLength);
            splitLength = length / cnt;

            var res = new List<List<LatLong>>();
            var cur = new List<LatLong> { points[0] };
            double curLength = 0.0;
            for (int i = 1; i < points.Count;)
            {
                var segLen = GeoHelper.Distance(cur.Last(), points[i]);
                if (curLength + segLen > splitLength && res.Count < cnt - 1)
                {
                    var dlat = points[i].Latitude - cur.Last().Latitude;
                    var dlon = points[i].Longitude - cur.Last().Longitude;
                    var addRatio = (splitLength - curLength) / segLen;
                    var lat = cur.Last().Latitude + (dlat * addRatio);
                    var lon = cur.Last().Longitude + (dlon * addRatio);
                    cur.Add(new LatLong(lat, lon));
                    res.Add(cur);
                    cur = new List<LatLong> { new LatLong(lat, lon) };
                    curLength = 0.0;
                    continue;
                }

                cur.Add(points[i]);
                curLength += segLen;
                i++;
            }

            res.Add(cur);

            return res;
        }

        public static List<LatLong> Split(LatLong p1, LatLong p2, double splitLength)
        {
            double length = GeoHelper.Distance(p1, p2);
            int cnt = GenericHelper.Round(length / splitLength);

            var res = new List<LatLong> { p1 };
            for (int i = 1; i < cnt; i++)
            {
                var dlat = p2.Latitude - p1.Latitude;
                var dlon = p2.Longitude - p1.Longitude;
                var lat = p1.Latitude + (dlat * i / cnt);
                var lon = p1.Longitude + (dlon * i / cnt);
                res.Add(new LatLong(lat, lon));
            }

            res.Add(p2);

            return res;
        }
    }
}