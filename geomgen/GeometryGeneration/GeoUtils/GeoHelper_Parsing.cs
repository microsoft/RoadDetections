// <copyright file="GeoHelper_Parsing.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NetTopologySuite.Features;
    using NetTopologySuite.Geometries;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static partial class GeoHelper
    {
        public static LatLong ParsePoint(string input, GeometryRepresentation geometryRepresentation)
        {
            var geometry = GeoHelper.ParseGeometry(input, geometryRepresentation);
            if (geometry.GeometryType == "Point")
            {
                return new LatLong(geometry.Coordinate);
            }
            else
            {
                throw new Exception("Not expected this geometry type: " + geometry.GeometryType + " in input=" + input);
            }
        }

        public static List<LatLong> ParseLineString(string input, GeometryRepresentation geometryRepresentation)
        {
            var geometry = GeoHelper.ParseGeometry(input, geometryRepresentation);
            if (geometry.GeometryType == "LineString")
            {
                return geometry.Coordinates.Select(coord => new LatLong(coord)).ToList();
            }
            else
            {
                throw new Exception("Not expected this geometry type: " + geometry.GeometryType + " in input=" + input);
            }
        }

        public static List<List<LatLong>> ParseMultiLineString(string input, GeometryRepresentation geometryRepresentation)
        {
            var roads = new List<List<LatLong>>();
            var geometry = GeoHelper.ParseGeometry(input, geometryRepresentation);
            if (geometry.GeometryType == "LineString" || geometry.GeometryType == "MultiLineString")
            {
                for (var i = 0; i < geometry.NumGeometries; i++)
                {
                    roads.Add(geometry.GetGeometryN(i).Coordinates.Select(coord => new LatLong(coord)).ToList());
                }
            }
            else
            {
                if (geometryRepresentation == GeometryRepresentation.GeoJson && GeoJsonUtils.GetGeometryType(input) == "LineString")
                {
                    roads.Add(GeoJsonUtils.GetCoordinates<double>(input).Select(arr => new LatLong(arr[0], arr[1])).ToList());
                }
                else
                {
                    throw new Exception("Not expected this geometry type: " + geometry.GeometryType + " in input=" + input);
                }
            }

            return roads;
        }

        public static Geometry ParseGeometry(string input, GeometryRepresentation geometryRepresentation)
        {
            Geometry res = null;
            if (geometryRepresentation == GeometryRepresentation.GeoJson)
            {
                res = GeoJsonUtils.GeoJsonReader.Read<Feature>(input).Geometry;
            }
            else if (geometryRepresentation == GeometryRepresentation.JefGeoJson)
            {
                res = GeoJsonUtils.WKTReader.Read(JefGeoJsonToWkt(input));
            }
            else if (geometryRepresentation == GeometryRepresentation.Wkt)
            {
                res = GeoJsonUtils.WKTReader.Read(input);
            }
            else if (geometryRepresentation == GeometryRepresentation.Jef)
            {
                var entity = (JObject)JsonConvert.DeserializeObject(input);
                foreach (var primitive in entity["Primitives"])
                {
                    if (res == null)
                    {
                        res = GeoJsonUtils.WKTReader.Read((string)primitive["Geometry"]);
                    }
                    else
                    {
                        res = res.Union(GeoJsonUtils.WKTReader.Read((string)primitive["Geometry"]));
                    }
                }
            }
            else
            {
                throw new Exception("Unexpected GeometryRepresentation: " + geometryRepresentation);
            }

            if (res.IsValid == false)
            {
                res = res.Buffer(0);
            }

            return res;
        }

        public static string WktToGeoJson(string wkt)
        {
            return GeoJsonUtils.GeoJsonWriter.Write(new Feature(GeoJsonUtils.WKTReader.Read(wkt), new AttributesTable()));
        }

        public static string GeoJsonToWkt(string geoJson)
        {
            return GeoJsonUtils.WKTWriter.Write(GeoJsonUtils.GeoJsonReader.Read<Feature>(geoJson).Geometry);
        }

        private static string JefGeoJsonToWkt(string jefGeojson)
        {
            var geomType = GeoJsonUtils.GetGeometryType(jefGeojson);
            if (geomType == "Polygon" || jefGeojson == "MultiPolygon")
            {
                var res = new List<List<List<LatLong>>>();
                foreach (var polygonCrds in GeoJsonUtils.GetPolygonCoordinates<double>(jefGeojson))
                {
                    var polygon = new List<List<LatLong>>();
                    var cur = new List<LatLong>();
                    foreach (var crd in polygonCrds)
                    {
                        var latlon = new LatLong(crd[1], crd[0]);
                        cur.Add(latlon);

                        if (cur.Count > 1 && cur.First().Equals(latlon))
                        {
                            polygon.Add(cur);
                            cur = new List<LatLong>();
                        }
                    }

                    if (cur.Count > 0)
                    {
                        throw new Exception("Points must form a closed linestring in geojson:" + jefGeojson);
                    }

                    res.Add(polygon);
                }

                if (res.Count == 1)
                {
                    return "POLYGON " + ToWkt(res.First());
                }
                else
                {
                    return "MULTIPOLYGON " + ToWkt(res);
                }
            }
            else
            {
                return jefGeojson;
            }
        }

        private static string ToWkt(List<List<List<LatLong>>> list)
        {
            return "(" + string.Join(", ", list.Select(item => ToWkt(item))) + ")";
        }

        private static string ToWkt(List<List<LatLong>> list)
        {
            return "(" + string.Join(", ", list.Select(item => ToWkt(item))) + ")";
        }

        private static string ToWkt(List<LatLong> list)
        {
            return "(" + string.Join(", ", list.Select(latlon => latlon.Longitude + " " + latlon.Latitude)) + ")";
        }
    }
}
