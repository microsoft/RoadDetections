// <copyright file="GeometryHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using Utils;

    public static class GeometryHelper
    {
        public static readonly int[,] DIJ = new int[,]
        {
            { -1, 0 }, // W
            { -1, 1 },
            { 0, 1 }, // S
            { 1, 1 },
            { 1, 0 }, // E
            { 1, -1 },
            { 0, -1 }, // N
            { -1, -1 },
        };

        public static readonly double[] DijDist = new double[]
        {
            1,
            Math.Sqrt(2.0),
            1,
            Math.Sqrt(2.0),
            1,
            Math.Sqrt(2.0),
            1,
            Math.Sqrt(2.0),
        };

        public static double Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)) + ((z1 - z2) * (z1 - z2)));
        }

        public static double Distance(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            return Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)) + ((z1 - z2) * (z1 - z2)));
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
        }

        public static int Distance2(int x1, int y1, int x2, int y2)
        {
            return ((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2));
        }

        /// <summary>
        /// Find angle between vectors p2->p1 and p3->p2.
        /// ..............................
        /// ....(135)......(90)......(45).
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// .(p1)----------(p2)-------(0).
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// ....(135)......(90)......(45).
        /// ..............................
        /// </summary>
        public static double GetAngle(double p1x, double p1y, double p2x, double p2y, double p3x, double p3y)
        {
            double ss = ((p1x - p2x) * (p2x - p3x)) + ((p1y - p2y) * (p2y - p3y));
            ss /= GeometryHelper.Distance(p1x, p1y, p2x, p2y);
            ss /= GeometryHelper.Distance(p2x, p2y, p3x, p3y);

            ss = Math.Min(ss, 1.0);
            ss = Math.Max(ss, -1.0);

            return Math.Acos(ss);
        }

        /// <summary>
        /// Find angle between vectors p2->p1 and p3->p2.
        /// ..............................
        /// ...(-135).....(-90).....(-45).
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// .(p1)----------(p2)-------(0).
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// ................|.............
        /// ....(135)......(90)......(45).
        /// ..............................
        /// </summary>
        public static double GetSignedAngle(double p1x, double p1y, double p2x, double p2y, double p3x, double p3y)
        {
            return GetAngle(p1x, p1y, p2x, p2y, p3x, p3y) * (GetOrientedTriangleAreaSign(p1y, p1x, p2y, p2x, p3y, p3x) == -1 ? -1 : 1);
        }

        /// <summary>
        /// Returns a vector (of length 1) from point [fromX, fromY] towards point [toX, toY].
        /// </summary>
        public static Tuple<double, double> GetVector(double fromX, double fromY, double toX, double toY)
        {
            double dx = toX - fromX;
            double dy = toY - fromY;
            double len = Math.Sqrt((dx * dx) + (dy * dy));
            dx /= len;
            dy /= len;

            return Tuple.Create(dx, dy);
        }

        /// <summary>
        /// Returns a point on a segment {[fromX, fromY], [toX, toY]} closest to a point [x, y].
        /// </summary>
        public static Tuple<double, double> FindClosestPointToSegment(double x, double y, double fromX, double fromY, double toX, double toY)
        {
            var (lineX, lineY) = FindClosestPointToLine(x, y, fromY - toY, toX - fromX, ((toY - fromY) * fromX) + ((fromX - toX) * fromY));

            // check boundingbox
            if (GenericHelper.IsBetween(lineX, Math.Min(fromX, toX), Math.Max(fromX, toX)) && GenericHelper.IsBetween(lineY, Math.Min(fromY, toY), Math.Max(fromY, toY)))
            {
                return Tuple.Create(lineX, lineY);
            }
            else
            {
                if (Distance(x, y, fromX, fromY) < Distance(x, y, toX, toY))
                {
                    return Tuple.Create(fromX, fromY);
                }
                else
                {
                    return Tuple.Create(toX, toY);
                }
            }
        }

        public static LatLong FindClosestPointToSegment(LatLong point, LatLong from, LatLong to)
        {
            var closest = FindClosestPointToSegment(point.Longitude, point.Latitude, from.Longitude, from.Latitude, to.Longitude, to.Latitude);
            return new LatLong(closest.Item2, closest.Item1);
        }

        public static double DistanceSegmentToSegment(double fromX1, double fromY1, double toX1, double toY1, double fromX2, double fromY2, double toX2, double toY2, Func<double, double, double, double, double> distanceFunction = null)
        {
            if (DoSegmentsIntersect(fromX1, fromY1, toX1, toY1, fromX2, fromY2, toX2, toY2))
            {
                return 0.0;
            }

            return Math.Min(DistanceToSegment(fromX1, fromY1, fromX2, fromY2, toX2, toY2, distanceFunction), DistanceToSegment(toX1, toY1, fromX2, fromY2, toX2, toY2, distanceFunction));
        }

        public static double DistanceToSegment(double x, double y, double fromX, double fromY, double toX, double toY, Func<double, double, double, double, double> distanceFunction = null)
        {
            var (lineX, lineY) = FindClosestPointToLine(x, y, fromY - toY, toX - fromX, ((toY - fromY) * fromX) + ((fromX - toX) * fromY));

            // check boundingbox
            if (GenericHelper.IsBetween(lineX, Math.Min(fromX, toX), Math.Max(fromX, toX)) && GenericHelper.IsBetween(lineY, Math.Min(fromY, toY), Math.Max(fromY, toY)))
            {
                return distanceFunction == null ? Distance(x, y, lineX, lineY) : distanceFunction(x, y, lineX, lineY);
            }
            else
            {
                return Math.Min(
                    distanceFunction == null ? Distance(x, y, fromX, fromY) : distanceFunction(x, y, fromX, fromY),
                    distanceFunction == null ? Distance(x, y, toX, toY) : distanceFunction(x, y, toX, toY));
            }
        }

        public static bool DoSegmentsIntersect(double fromX1, double fromY1, double toX1, double toY1, double fromX2, double fromY2, double toX2, double toY2)
        {
            return
                GenericHelper.CompareDouble(Math.Max(Math.Min(fromX1, toX1), Math.Min(fromX2, toX2)), Math.Min(Math.Max(fromX1, toX1), Math.Max(fromX2, toX2))) <= 0 &&
                GenericHelper.CompareDouble(Math.Max(Math.Min(fromY1, toY1), Math.Min(fromY2, toY2)), Math.Min(Math.Max(fromY1, toY1), Math.Max(fromY2, toY2))) <= 0 &&
                GetOrientedTriangleAreaSign(fromX1, fromY1, toX1, toY1, fromX2, fromY2) * GetOrientedTriangleAreaSign(fromX1, fromY1, toX1, toY1, toX2, toY2) <= 0 &&
                GetOrientedTriangleAreaSign(fromX2, fromY2, toX2, toY2, fromX1, fromY1) * GetOrientedTriangleAreaSign(fromX2, fromY2, toX2, toY2, toX1, toY1) <= 0;
        }

        public static double DistanceToLine(int x, int y, int fromX, int fromY, int toX, int toY)
        {
            return Math.Abs(SignedDistanceToLine(x, y, fromX, fromY, toX, toY));
        }

        public static double SignedDistanceToLine(int x, int y, int fromX, int fromY, int toX, int toY)
        {
            double a = fromY - toY;
            double b = toX - fromX;
            double c = ((toY - fromY) * fromX) + ((fromX - toX) * fromY);
            return ((a * x) + (b * y) + c) / Math.Sqrt((a * a) + (b * b));
        }

        public static int GetOrientedTriangleAreaSign(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            return GenericHelper.Sign(((x2 - x1) * (y3 - y1)) - ((y2 - y1) * (x3 - x1)), 2);
        }

        public static double GetOrientedTriangleArea(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            return (((x2 - x1) * (y3 - y1)) - ((y2 - y1) * (x3 - x1))) / 2;
        }

        private static Tuple<double, double> FindClosestPointToLine(double x, double y, double a, double b, double c)
        {
            return Tuple.Create(
                ((b * ((b * x) - (a * y))) - (a * c)) / ((a * a) + (b * b)),
                ((a * ((a * y) - (b * x))) - (b * c)) / ((a * a) + (b * b)));
        }
    }
}
