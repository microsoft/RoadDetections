// <copyright file="GeometryQuadkeysGenerationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using NetTopologySuite.Geometries;
    using Utils;

    public static class GeometryQuadkeysGenerationHelper
    {
        private const double LineStringQuadkeysBase = 0.1;

        public static List<string> GetRoadQuadkeys(string input, int quadkeyLength, double bufferDistance, GeometryRepresentation geometryRepresentation)
        {
            var res = new HashSet<string>();
            foreach (var road in GeoHelper.ParseMultiLineString(input, geometryRepresentation))
            {
                var quadkeySize = GeoHelper.PixelsToMeters(BingMapsTileSystemHelper.GetTileSize(quadkeyLength, BingMapsTileSystemHelper.MaxResolution), road.First(), BingMapsTileSystemHelper.MaxResolution);
                for (int i = 0; i + 1 < road.Count; i++)
                {
                    var bb1 = new BoundingBox(road[i]).AddMargin(bufferDistance);
                    var bb2 = new BoundingBox(road[i + 1]).AddMargin(bufferDistance);
                    LineStringQuadkeys(bb1.NorthWest, bb2.NorthWest, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                    LineStringQuadkeys(bb1.NorthEast, bb2.NorthEast, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                    LineStringQuadkeys(bb1.SouthWest, bb2.SouthWest, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                    LineStringQuadkeys(bb1.SouthEast, bb2.SouthEast, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));

                    if (quadkeySize < 2.25 * bufferDistance)
                    {
                        LineStringQuadkeys(road[i], road[i + 1], quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                    }

                    if (quadkeySize < 1.25 * bufferDistance)
                    {
                        LineStringQuadkeys(bb1.North, bb2.North, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                        LineStringQuadkeys(bb1.South, bb2.South, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                        LineStringQuadkeys(bb1.West, bb2.West, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                        LineStringQuadkeys(bb1.East, bb2.East, quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                    }

                    if (quadkeySize < 0.6 * bufferDistance)
                    {
                        throw new Exception("This method cannot handle this combination of quadkeyLength=" + quadkeyLength + " and bufferDistance=" + bufferDistance);
                    }
                }
            }

            return res.ToList();
        }

        public static Dictionary<string, double> GetRoadQuadkeysDistribution(string input, int quadkeyLength, GeometryRepresentation geometryRepresentation)
        {
            var res = new Dictionary<string, double>();
            foreach (var road in GeoHelper.ParseMultiLineString(input, geometryRepresentation))
            {
                for (int i = 0; i + 1 < road.Count; i++)
                {
                    LineStringQuadkeys(road[i], road[i + 1], quadkeyLength, res, GeoHelper.Distance(road[i], road[i + 1]));
                }
            }

            return res;
        }

        public static List<string> GetPolygonQuadkeys(string input, int quadkeyLength, double bufferDistance, GeometryRepresentation geometryRepresentation)
        {
            var res = new HashSet<string>();
            foreach (var split in SplitGeometry(ParsePolygons(input, geometryRepresentation), 1e7))
            {
                PolygonQuadkeys(split, string.Empty, quadkeyLength, bufferDistance, res);
            }

            return res.ToList();
        }

        public static List<Tuple<string, double>> GetPolygonQuadkeysDistribution(string input, int quadkeyLength, GeometryRepresentation geometryRepresentation)
        {
            var res = new Dictionary<string, double>();
            foreach (var split in SplitGeometry(ParsePolygons(input, geometryRepresentation), 1e7))
            {
                PolygonQuadkeys(split, string.Empty, quadkeyLength, 0, res);
            }

            return res.Select(kvp => Tuple.Create(kvp.Key, kvp.Value)).ToList();
        }

        private static void LineStringQuadkeys(LatLong p1, LatLong p2, int quadkeyLength, HashSet<string> res, double length)
        {
            var quadkey1 = BingMapsTileSystemHelper.LatLongToQuadkey(p1.Latitude, p1.Longitude, quadkeyLength);
            var quadkey2 = BingMapsTileSystemHelper.LatLongToQuadkey(p2.Latitude, p2.Longitude, quadkeyLength);

            if (quadkey1 == quadkey2)
            {
                res.Add(quadkey1);
            }
            else if (length < GeometryQuadkeysGenerationHelper.LineStringQuadkeysBase)
            {
                res.Add(quadkey1);
                res.Add(quadkey2);
            }
            else
            {
                var mid = new LatLong(p1, p2);
                LineStringQuadkeys(p1, mid, quadkeyLength, res, length / 2);
                LineStringQuadkeys(mid, p2, quadkeyLength, res, length / 2);
            }
        }

        private static void LineStringQuadkeys(LatLong p1, LatLong p2, int quadkeyLength, Dictionary<string, double> res, double length)
        {
            var quadkey1 = BingMapsTileSystemHelper.LatLongToQuadkey(p1.Latitude, p1.Longitude, quadkeyLength);
            var quadkey2 = BingMapsTileSystemHelper.LatLongToQuadkey(p2.Latitude, p2.Longitude, quadkeyLength);

            if (quadkey1 == quadkey2)
            {
                GenericHelper.AddToDictionary(res, quadkey1, GeoHelper.Distance(p1, p2));
            }
            else if (length < GeometryQuadkeysGenerationHelper.LineStringQuadkeysBase)
            {
                GenericHelper.AddToDictionary(res, quadkey1, GeoHelper.Distance(p1, p2) / 2.0);
                GenericHelper.AddToDictionary(res, quadkey2, GeoHelper.Distance(p1, p2) / 2.0);
            }
            else
            {
                var mid = new LatLong(p1, p2);
                LineStringQuadkeys(p1, mid, quadkeyLength, res, length / 2);
                LineStringQuadkeys(mid, p2, quadkeyLength, res, length / 2);
            }
        }

        private static void PolygonQuadkeys(Geometry polygon, string quadkey, int quadkeyLength, double bufferDistance, HashSet<string> res)
        {
            var quadkeyPolygon = new BoundingBox(quadkey).AddMargin(bufferDistance).ToPolygon();
            if (polygon.Contains(quadkeyPolygon))
            {
                foreach (var q in QuadkeyOrientationHelper.GetQuadkeys(quadkey, quadkeyLength))
                {
                    res.Add(q);
                }
            }
            else if (polygon.Intersects(quadkeyPolygon))
            {
                if (quadkey.Length == quadkeyLength)
                {
                    res.Add(quadkey);
                }
                else
                {
                    PolygonQuadkeys(polygon, quadkey + "0", quadkeyLength, bufferDistance, res);
                    PolygonQuadkeys(polygon, quadkey + "1", quadkeyLength, bufferDistance, res);
                    PolygonQuadkeys(polygon, quadkey + "2", quadkeyLength, bufferDistance, res);
                    PolygonQuadkeys(polygon, quadkey + "3", quadkeyLength, bufferDistance, res);
                }
            }
        }

        private static void PolygonQuadkeys(Geometry polygon, string quadkey, int quadkeyLength, double bufferDistance, Dictionary<string, double> res)
        {
            var quadkeyPolygon = new BoundingBox(quadkey).AddMargin(bufferDistance).ToPolygon();
            if (polygon.Contains(quadkeyPolygon))
            {
                foreach (var q in QuadkeyOrientationHelper.GetQuadkeys(quadkey, quadkeyLength))
                {
                    GenericHelper.AddToDictionary(res, q, 1.0);
                }
            }
            else if (polygon.Intersects(quadkeyPolygon))
            {
                if (quadkey.Length == quadkeyLength)
                {
                    if (polygon.IsValid == false)
                    {
                        polygon = polygon.Buffer(0);
                    }

                    GenericHelper.AddToDictionary(res, quadkey, polygon.Intersection(quadkeyPolygon).Area / quadkeyPolygon.Area);
                }
                else
                {
                    PolygonQuadkeys(polygon, quadkey + "0", quadkeyLength, bufferDistance, res);
                    PolygonQuadkeys(polygon, quadkey + "1", quadkeyLength, bufferDistance, res);
                    PolygonQuadkeys(polygon, quadkey + "2", quadkeyLength, bufferDistance, res);
                    PolygonQuadkeys(polygon, quadkey + "3", quadkeyLength, bufferDistance, res);
                }
            }
        }

        private static Geometry ParsePolygons(string input, GeometryRepresentation geometryRepresentation)
        {
            var geometry = GeoHelper.ParseGeometry(input, geometryRepresentation);
            if (geometry.GeometryType == "Polygon" || geometry.GeometryType == "MultiPolygon")
            {
                return geometry;
            }
            else
            {
                throw new Exception("Not expected this geometry type: " + geometry.GeometryType + " in input=" + input);
            }
        }

        private static List<Geometry> SplitGeometry(Geometry geom, double areaThresholdMetersSquared)
        {
            if (geom.IsEmpty)
            {
                return new List<Geometry>();
            }

            geom = geom.Buffer(0);

            if (GeoHelper.GetPolygonArea(geom) < areaThresholdMetersSquared)
            {
                return new List<Geometry> { geom };
            }

            var center = geom.Centroid;
            double step = 1000;
            var coords = new Coordinate[][]
            {
                new[]
                {
                    new Coordinate(center.X, center.Y),
                    new Coordinate(center.X - step, center.Y),
                    new Coordinate(center.X - step, center.Y + step),
                    new Coordinate(center.X, center.Y + step),
                    new Coordinate(center.X, center.Y),
                },
                new[]
                {
                    new Coordinate(center.X, center.Y),
                    new Coordinate(center.X + step, center.Y),
                    new Coordinate(center.X + step, center.Y + step),
                    new Coordinate(center.X, center.Y + step),
                    new Coordinate(center.X, center.Y),
                },
                new[]
                {
                    new Coordinate(center.X, center.Y),
                    new Coordinate(center.X - step, center.Y),
                    new Coordinate(center.X - step, center.Y - step),
                    new Coordinate(center.X, center.Y - step),
                    new Coordinate(center.X, center.Y),
                },
                new[]
                {
                    new Coordinate(center.X, center.Y),
                    new Coordinate(center.X + step, center.Y),
                    new Coordinate(center.X + step, center.Y - step),
                    new Coordinate(center.X, center.Y - step),
                    new Coordinate(center.X, center.Y),
                },
            };

            var res = new List<Geometry>();
            foreach (var coord in coords)
            {
                res.AddRange(SplitGeometry(geom.Intersection(GeoHelper.GeometryFactory.CreatePolygon(coord)), areaThresholdMetersSquared));
            }

            return res;
        }
    }
}
