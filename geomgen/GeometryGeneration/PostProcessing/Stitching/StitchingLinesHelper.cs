// <copyright file="StitchingLinesHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Stitching
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using PostProcessing.Main;
    using Utils;

    public static class StitchingLinesHelper
    {
        public static List<Line> ProcessLines(int tileSize, List<Line> lines, Configuration config)
        {
            lines = lines.OrderBy(line => Line.JsonSerialize(line)).ToList();

            // a set of lists of points <lineid, endid>, where a separate list should be merged together
            var mergingPointSets = StitchingLinesHelper.GetMergingPointSets(lines, tileSize, config.MaxPointsDistToStitchLines);

            // change coordinates of lines to coordinates in parent quadkey
            for (int i = 0; i < lines.Count; i++)
            {
                var resolution = BingMapsTileSystemHelper.GetResolution(lines[i].BoundingBoxQuadkey.Length, tileSize);
                lines[i] = lines[i].RescaleToParent(Subquadkey(lines[i].BoundingBoxQuadkey), resolution);
            }

            var pointsToMerge = new List<List<Point>>();
            foreach (var mergePoints in mergingPointSets)
            {
                var list = new List<Point>();
                foreach (var (lineId, lineEnd) in mergePoints)
                {
                    list.Add(lineEnd == 0 ? lines[lineId].Points.First() : lines[lineId].Points.Last());
                }

                pointsToMerge.Add(list);
            }

            var linesDsu = new DSU(lines.Count);
            foreach (var mergePoints in mergingPointSets)
            {
                if (mergePoints.Count == 2)
                {
                    var (lineId0, lineEnd0) = mergePoints[0];
                    var (lineId1, lineEnd1) = mergePoints[1];

                    lineId0 = linesDsu.GetParent(lineId0);
                    lineId1 = linesDsu.GetParent(lineId1);
                    if (lineId0 != lineId1)
                    {
                        var point0 = lineEnd0 == 0 ? lines[lineId0].Points.First() : lines[lineId0].Points.Last();
                        var point1 = lineEnd1 == 0 ? lines[lineId1].Points.First() : lines[lineId1].Points.Last();
                        StitchingLinesHelper.MergeLines(lines, lineId0, point0.X, point0.Y, lineId1, point1.X, point1.Y);
                        linesDsu.Union(lineId0, lineId1);
                    }
                }
            }

            var res = new List<Line>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (linesDsu.GetParent(i) == i)
                {
                    res.Add(lines[i]);
                }
            }

            StitchingLinesHelper.MergePoints(pointsToMerge, ref res);
            StitchingLinesHelper.FixDupLines(ref res);
            StitchingLinesHelper.FixEmptyEdges(ref res);

            return res;
        }

        public static bool IsTouchingTileEnd(int tileSize, Line line)
        {
            return IsTouchingTileEnd(tileSize, line.Points.First()) || IsTouchingTileEnd(tileSize, line.Points.Last());
        }

        private static bool IsTouchingTileEnd(int tileSize, Point point)
        {
            return point.X == 0 || point.X == tileSize - 1 || point.Y == 0 || point.Y == tileSize - 1;
        }

        private static IEnumerable<List<Tuple<int, int>>> GetMergingPointSets(List<Line> lines, int tileSize, int maxPointsDistToStichLines)
        {
            // for each position on a map <quadkey, point> we store a list of 2 integers: lineid and where on that line that point was (0 is first and 1 is last)
            var lineEndsToMerge = new SortedDictionary<Tuple<string, Point>, List<Tuple<int, int>>>();
            for (int i = 0; i < lines.Count; i++)
            {
                GenericHelper.AddToDictionary(lineEndsToMerge, Tuple.Create(lines[i].BoundingBoxQuadkey, lines[i].Points.First()), Tuple.Create(i, 0));
                GenericHelper.AddToDictionary(lineEndsToMerge, Tuple.Create(lines[i].BoundingBoxQuadkey, lines[i].Points.Last()), Tuple.Create(i, 1));
            }

            var dsu = new DSU(2 * lines.Count);
            foreach (var ((quadkey, point), lineEnds) in lineEndsToMerge)
            {
                foreach (var neighbor in StitchingLinesHelper.GetNeighbors(quadkey, point, tileSize, maxPointsDistToStichLines))
                {
                    if (lineEndsToMerge.ContainsKey(neighbor))
                    {
                        foreach (var (lineId1, lineEnd1) in lineEnds)
                        {
                            foreach (var (lineId2, lineEnd2) in lineEndsToMerge[neighbor])
                            {
                                dsu.Union((2 * lineId1) + lineEnd1, (2 * lineId2) + lineEnd2);
                            }
                        }
                    }
                }
            }

            return dsu.GetComponents().Select(list => list.Select(value => Tuple.Create(value / 2, value % 2)).ToList());
        }

        private static void MergeLines(List<Line> lines, int lid0, int x0, int y0, int lid1, int x1, int y1)
        {
            int width = GenericHelper.Round(((lines[lid0].Width * lines[lid0].Length) + (lines[lid1].Width * lines[lid1].Length)) / (lines[lid0].Length + lines[lid1].Length));
            lines[lid0].Width = width;
            lines[lid1].Width = width;

            int pid0, pid1;
            if (lines[lid0].Points.First().Equals(new Point(x0, y0)))
            {
                pid0 = 0;
            }
            else if (lines[lid0].Points.Last().Equals(new Point(x0, y0)))
            {
                pid0 = 1;
            }
            else
            {
                throw new Exception("Merging to non-exsitent point");
            }

            if (lines[lid1].Points.First().Equals(new Point(x1, y1)))
            {
                pid1 = 0;
            }
            else if (lines[lid1].Points.Last().Equals(new Point(x1, y1)))
            {
                pid1 = 1;
            }
            else
            {
                throw new Exception("Merging to non-exsitent point");
            }

            var points = new List<Point>();
            if (pid0 == 0 && pid1 == 0)
            {
                points.AddRange(lines[lid0].Points);
                points.Reverse();
                points.AddRange(lines[lid1].Points);
            }

            if (pid0 == 0 && pid1 == 1)
            {
                points.AddRange(lines[lid1].Points);
                points.AddRange(lines[lid0].Points);
            }

            if (pid0 == 1 && pid1 == 0)
            {
                points.AddRange(lines[lid0].Points);
                points.AddRange(lines[lid1].Points);
            }

            if (pid0 == 1 && pid1 == 1)
            {
                points.AddRange(lines[lid0].Points);
                lines[lid1].Points.Reverse();
                points.AddRange(lines[lid1].Points);
                lines[lid1].Points.Reverse();
            }

            lines[lid0].Points.Clear();
            lines[lid0].Points.AddRange(points);
            lines[lid1].Points.Clear();
            lines[lid1].Points.AddRange(points);
        }

        private static void MergePoints(List<List<Point>> pointsToMerge, ref List<Line> lines)
        {
            var mergeTo = new SortedDictionary<Point, Point>();
            foreach (var cluster in pointsToMerge)
            {
                var resPoint = new Point(GenericHelper.Round(cluster.Average(point => point.X)), GenericHelper.Round(cluster.Average(point => point.Y)));
                foreach (var point in cluster)
                {
                    if (!point.Equals(resPoint))
                    {
                        if (mergeTo.ContainsKey(point))
                        {
                            if (!mergeTo[point].Equals(resPoint))
                            {
                                throw new Exception("Points belong to different clusters. " + string.Join("|||", pointsToMerge.Select(pointCluster => string.Join("|", pointCluster))));
                            }
                        }
                        else
                        {
                            mergeTo.Add(point, resPoint);
                        }
                    }
                }
            }

            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = 0; j < lines[i].Points.Count; j++)
                {
                    if (mergeTo.ContainsKey(lines[i].Points[j]))
                    {
                        lines[i].Points[j] = mergeTo[lines[i].Points[j]];
                    }
                }
            }
        }

        private static void FixDupLines(ref List<Line> lines)
        {
            var res = new Dictionary<string, Line>();
            foreach (var line in lines)
            {
                var key = line.GetNormalizedCoordinates();
                if (res.ContainsKey(key))
                {
                    if (res[key].Width < line.Width)
                    {
                        res[key] = line;
                    }
                }
                else
                {
                    res.Add(key, line);
                }
            }

            lines = res.Values.ToList();
        }

        private static void FixEmptyEdges(ref List<Line> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Points.Count <= 3)
                {
                    continue;
                }

                var points = new List<Point>
                {
                    lines[i].Points[0],
                    lines[i].Points[1],
                };

                for (int j = 2; j + 1 < lines[i].Points.Count; j++)
                {
                    if (!lines[i].Points[j - 1].Equals(lines[i].Points[j]))
                    {
                        points.Add(lines[i].Points[j]);
                    }
                }

                points.Add(lines[i].Points.Last());

                lines[i].Points = points;
            }
        }

        private static IEnumerable<Tuple<string, Point>> GetNeighbors(string quadkey, Point point, int size, int maxPointsDistToStichLines)
        {
            int x = GenericHelper.Floor(Math.Sqrt((maxPointsDistToStichLines * maxPointsDistToStichLines) - 1));
            for (int dist = -x; dist <= x; dist++)
            {
                // top
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.Top(quadkey)) && point.Y == 0)
                {
                    if (GenericHelper.IsBetween(point.X + dist, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.Top(quadkey), new Point(point.X + dist, size - 1));
                    }
                }

                // bot
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.Bot(quadkey)) && point.Y == size - 1)
                {
                    if (GenericHelper.IsBetween(point.X + dist, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.Bot(quadkey), new Point(point.X + dist, 0));
                    }
                }

                // left
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.Left(quadkey)) && point.X == 0)
                {
                    if (GenericHelper.IsBetween(point.Y + dist, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.Left(quadkey), new Point(size - 1, point.Y + dist));
                    }
                }

                // right
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.Right(quadkey)) && point.X == size - 1)
                {
                    if (GenericHelper.IsBetween(point.Y + dist, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.Right(quadkey), new Point(0, point.Y + dist));
                    }
                }

                // top right
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.TopRight(quadkey)))
                {
                    if (point.X == size - 1 && GenericHelper.IsBetween(point.Y + dist + size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.TopRight(quadkey), new Point(0, point.Y + dist + size));
                    }

                    if (point.Y == 0 && GenericHelper.IsBetween(point.X + dist - size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.TopRight(quadkey), new Point(point.X + dist - size, size - 1));
                    }
                }

                // top left
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.TopLeft(quadkey)))
                {
                    if (point.X == 0 && GenericHelper.IsBetween(point.Y + dist + size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.TopLeft(quadkey), new Point(size - 1, point.Y + dist + size));
                    }

                    if (point.Y == 0 && GenericHelper.IsBetween(point.X + dist + size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.TopLeft(quadkey), new Point(point.X + dist + size, size - 1));
                    }
                }

                // bot right
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.BotRight(quadkey)))
                {
                    if (point.X == size - 1 && GenericHelper.IsBetween(point.Y + dist - size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.BotRight(quadkey), new Point(0, point.Y + dist - size));
                    }

                    if (point.Y == size - 1 && GenericHelper.IsBetween(point.X + dist - size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.BotRight(quadkey), new Point(point.X + dist - size, 0));
                    }
                }

                // bot left
                if (Subquadkey(quadkey) == Subquadkey(QuadkeyOrientationHelper.BotLeft(quadkey)))
                {
                    if (point.X == 0 && GenericHelper.IsBetween(point.Y + dist - size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.BotLeft(quadkey), new Point(size - 1, point.Y + dist - size));
                    }

                    if (point.Y == size - 1 && GenericHelper.IsBetween(point.X + dist + size, 0, size - 1))
                    {
                        yield return Tuple.Create(QuadkeyOrientationHelper.BotLeft(quadkey), new Point(point.X + dist + size, 0));
                    }
                }
            }
        }

        private static string Subquadkey(string quadkey)
        {
            return quadkey.Substring(0, quadkey.Length - 1);
        }
    }
}
