// <copyright file="LocalLineProjector.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers.LineProjecting
{
    using System;
    using System.Collections.Generic;
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;
    using ImageUtils;
    using PostProcessing.Stitching;
    using Utils;

    public class LocalLineProjector : ILineProjector
    {
        private readonly int[,] used;
        private readonly int width;
        private readonly int height;

        private int usedValue;

        public LocalLineProjector(int width, int height)
        {
            this.usedValue = 0;
            this.used = new int[width, height];
            this.width = width;
            this.height = height;
        }

        public IEnumerable<Tuple<int, int>> GetPoints(List<LatLong> road, double? meterWidth, string quadkey, int resolution)
        {
            this.usedValue++;
            int padding = (this.width - BingMapsTileSystemHelper.GetTileSize(quadkey, resolution)) / 2;
            var points = new Line(road, quadkey, resolution, 0).Points;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                int x1 = points[i].X, y1 = points[i].Y;
                int x2 = points[i + 1].X, y2 = points[i + 1].Y;
                var ll1 = road[i];
                var ll2 = road[i + 1];

                foreach (var (x, y) in Bresenham.GetLinePoints(x1, y1, x2, y2))
                {
                    if (!meterWidth.HasValue)
                    {
                        int xcur = x + padding;
                        int ycur = y + padding;
                        if (this.CheckCoordinates(xcur, ycur) && this.used[xcur, ycur] != this.usedValue)
                        {
                            this.used[xcur, ycur] = this.usedValue;
                            yield return Tuple.Create(xcur, ycur);
                        }
                    }
                    else
                    {
                        int r = ((GeoHelper.MetersToPixels(meterWidth.Value, quadkey, resolution) + 1) / 2) + 5;
                        for (int ii = -r; ii <= r; ii++)
                        {
                            for (int jj = -r; jj <= r; jj++)
                            {
                                int xcur = x + ii + padding;
                                int ycur = y + jj + padding;
                                if (this.CheckCoordinates(xcur, ycur) && this.used[xcur, ycur] != this.usedValue)
                                {
                                    BingMapsTileSystemHelper.GetCoordinatesInParentQuadkey(quadkey, x + ii, y + jj, string.Empty, out var xx, out var yy, resolution);

                                    var ll = BingMapsTileSystemHelper.PixelXYToLatLong(xx, yy, resolution);
                                    var dist = GeometryHelper.DistanceToSegment(ll.Latitude, ll.Longitude, ll1.Latitude, ll1.Longitude, ll2.Latitude, ll2.Longitude, (lat1, lon1, lat2, lon2) => GeoHelper.Distance(new LatLong(lat1, lon1), new LatLong(lat2, lon2)));

                                    if (GenericHelper.CompareDouble(dist, meterWidth.Value / 2.0) <= 0)
                                    {
                                        this.used[xcur, ycur] = this.usedValue;
                                        yield return Tuple.Create(xcur, ycur);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<Tuple<int, int>> GetPoints(int x1, int y1, int x2, int y2, int lineWidth)
        {
            if (lineWidth <= 0)
            {
                yield break;
            }

            this.usedValue++;
            int r = (lineWidth - 1) / 2;
            int prevx = 0, prevy = 0;
            bool first = true;
            foreach (var (x, y) in Bresenham.GetLinePoints(x1, y1, x2, y2))
            {
                if (first)
                {
                    first = false;
                    var len = LineProjectorHelper.CirclePointsLength[r];
                    for (int i = 0; i < len; i += 2)
                    {
                        int xcur = x + LineProjectorHelper.CirclePoints[r][i];
                        int ycur = y + LineProjectorHelper.CirclePoints[r][i + 1];
                        if (this.CheckCoordinates(xcur, ycur) && this.used[xcur, ycur] != this.usedValue)
                        {
                            this.used[xcur, ycur] = this.usedValue;
                            yield return Tuple.Create(xcur, ycur);
                        }
                    }
                }
                else
                {
                    int di = x - prevx + 1, dj = y - prevy + 1;
                    int len = LineProjectorHelper.Length[r, di, dj];
                    for (int i = 0; i < len; i++)
                    {
                        int xcur = x + LineProjectorHelper.DX[r, di, dj][i];
                        int ycur = y + LineProjectorHelper.DY[r, di, dj][i];
                        if (this.CheckCoordinates(xcur, ycur) && this.used[xcur, ycur] != this.usedValue)
                        {
                            this.used[xcur, ycur] = this.usedValue;
                            yield return Tuple.Create(xcur, ycur);
                        }
                    }
                }

                prevx = x;
                prevy = y;
            }
        }

        private bool CheckCoordinates(int i, int j)
        {
            return i >= 0 && i < this.width && j >= 0 && j < this.height;
        }
    }
}
