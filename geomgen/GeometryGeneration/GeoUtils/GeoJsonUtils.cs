// <copyright file="GeoJsonUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NetTopologySuite.Features;
    using NetTopologySuite.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class GeoJsonUtils
    {
        public static readonly GeoJsonReader GeoJsonReader = new GeoJsonReader();
        public static readonly GeoJsonWriter GeoJsonWriter = new GeoJsonWriter();
        public static readonly WKTReader WKTReader = new WKTReader();
        public static readonly WKTWriter WKTWriter = new WKTWriter();

        public static string ToFeatureCollection(params string[] geojsons)
        {
            var res = new FeatureCollection();
            foreach (var geojson in geojsons)
            {
                res.Add(GeoJsonUtils.GeoJsonReader.Read<Feature>(geojson));
            }

            return GeoJsonUtils.GeoJsonWriter.Write(res);
        }

        public static string GetGeometryType(string geoJson)
        {
            dynamic g = JsonConvert.DeserializeObject(geoJson);
            return g.geometry?.type.ToString();
        }

        public static string ConstructMultiLineStringGeoJson(List<string> geojsons)
        {
            var lines = new List<List<LatLong>>();
            foreach (var geojson in geojsons)
            {
                lines.Add(GeoHelper.ParseLineString(geojson, GeometryRepresentation.GeoJson));
            }

            return
                "{" +
                    "\"type\":\"Feature\"," +
                    "\"geometry\":" +
                    "{" +
                        "\"type\":\"MultiLineString\",\"coordinates\":" +
                        "[" +
                            string.Join(",", lines.Select(line => "[" + string.Join(",", line.Select(point => "[" + point.Longitude + "," + point.Latitude + "]")) + "]")) +
                        "]" +
                    "}," +
                    "\"properties\":{}" +
                "}";
        }

        public static string ConstructLineStringGeoJson(params LatLong[] points)
        {
            return ConstructLineStringGeoJson(points.Select(point => Tuple.Create(point.Longitude, point.Latitude)));
        }

        public static string ConstructLineStringGeoJson(IEnumerable<LatLong> points)
        {
            return ConstructLineStringGeoJson(points.Select(point => Tuple.Create(point.Longitude, point.Latitude)));
        }

        public static string ConstructLineStringGeoJson<T>(IEnumerable<Tuple<T, T>> points)
        {
            return
                "{" +
                    "\"type\":\"Feature\"," +
                    "\"geometry\":{\"type\":\"LineString\",\"coordinates\":[" + string.Join(",", points.Select(point => "[" + point.Item1 + "," + point.Item2 + "]")) + "]}," +
                    "\"properties\":{}" +
                "}";
        }

        public static string ConstructPolygonGeoJson(params LatLong[] points)
        {
            return ConstructPolygonGeoJson(points.Select(point => Tuple.Create(point.Longitude, point.Latitude)));
        }

        public static string ConstructPolygonGeoJson(IEnumerable<LatLong> points)
        {
            return ConstructPolygonGeoJson(points.Select(point => Tuple.Create(point.Longitude, point.Latitude)));
        }

        public static string ConstructPolygonGeoJson<T>(IEnumerable<Tuple<T, T>> points)
        {
            return
                "{" +
                    "\"type\":\"Feature\"," +
                    "\"geometry\":{\"type\":\"Polygon\",\"coordinates\":[[" + string.Join(",", points.Select(point => "[" + point.Item1 + "," + point.Item2 + "]")) + "]]}," +
                    "\"properties\":{}" +
                "}";
        }

        public static string ConstructPointGeoJson(LatLong latlong, double z = 0.0)
        {
            return
                "{" +
                    "\"type\":\"Feature\"," +
                    "\"geometry\":{\"type\":\"Point\",\"coordinates\":[" + string.Join(",", latlong.Longitude, latlong.Latitude, z) + "]}," +
                    "\"properties\":{}" +
                "}";
        }

        public static IEnumerable<IEnumerable<T[]>> GetPolygonCoordinates<T>(string geojson)
        {
            dynamic g = JsonConvert.DeserializeObject(geojson);
            return ((JArray)g.geometry.coordinates).Select(polygon => polygon.Select(arr => arr.Values<T>().ToArray()));
        }

        public static IEnumerable<T[]> GetCoordinates<T>(string geojson)
        {
            dynamic g = JsonConvert.DeserializeObject(geojson);
            return ((JArray)g.geometry.coordinates).Select(arr => arr.Values<T>().ToArray());
        }
    }
}