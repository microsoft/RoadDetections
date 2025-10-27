// <copyright file="BingMapsTileSystemHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils.BingMapsTileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Utils;

    public static class BingMapsTileSystemHelper
    {
        public const int DefaultResolution = 25;
        public const int MinResolution = 1;
        public const int MaxResolution = 30;
        private const double MinLatitude = -85.05112878;
        private const double MaxLatitude = 85.05112878;
        private const double MinLongitude = -180;
        private const double MaxLongitude = 180;

        /// <summary>
        /// Converts a point from latitude/longitude WGS-84 coordinates (in degrees)
        /// into pixel XY coordinates at a specified level of detail.
        /// </summary>
        /// <param name="latitude">Latitude of the point, in degrees.</param>
        /// <param name="longitude">Longitude of the point, in degrees.</param>
        /// <param name="pixelX">Output parameter receiving the X coordinate in pixels.</param>
        /// <param name="pixelY">Output parameter receiving the Y coordinate in pixels.</param>
        /// <param name="resolution">Resolution level. Default is 25.</param>
        public static void LatLongToPixelXY(double latitude, double longitude, out int pixelX, out int pixelY, int resolution)
        {
            latitude = BingMapsTileSystemHelper.Clip(latitude, MinLatitude, MaxLatitude);
            longitude = BingMapsTileSystemHelper.Clip(longitude, MinLongitude, MaxLongitude);

            double x = (longitude + 180) / 360;
            double sinLatitude = Math.Sin(latitude * Math.PI / 180);
            double y = 0.5 - (Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI));

            var mapSize = BingMapsTileSystemHelper.GetTileSize(string.Empty, resolution);
            pixelX = (int)BingMapsTileSystemHelper.Clip((x * mapSize) + 0.5, 0, mapSize - 1);
            pixelY = (int)BingMapsTileSystemHelper.Clip((y * mapSize) + 0.5, 0, mapSize - 1);
        }

        /// <summary>
        /// Converts a Quadkey into tile XY coordinates.
        /// </summary>
        /// <param name="quadkey">Quadkey of the tile.</param>
        /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>
        /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
        public static void QuadkeyToTileXY(string quadkey, out int tileX, out int tileY)
        {
            tileX = tileY = 0;
            for (int i = quadkey.Length; i > 0; i--)
            {
                int mask = 1 << (i - 1);
                switch (quadkey[quadkey.Length - i])
                {
                    case '0':
                        break;
                    case '1':
                        tileX |= mask;
                        break;
                    case '2':
                        tileY |= mask;
                        break;
                    case '3':
                        tileX |= mask;
                        tileY |= mask;
                        break;
                    default:
                        throw new ArgumentException("Invalid quadkey digit sequence.");
                }
            }
        }

        public static string LatLongToQuadkey(double latitude, double longitude, int quadkeyLength)
        {
            BingMapsTileSystemHelper.LatLongToPixelXY(latitude, longitude, out var pixelX, out var pixelY, BingMapsTileSystemHelper.MaxResolution);
            BingMapsTileSystemHelper.PixelXYToTileXY(pixelX, pixelY, out var tileX, out var tileY, BingMapsTileSystemHelper.GetTileSize(quadkeyLength, BingMapsTileSystemHelper.MaxResolution));
            return BingMapsTileSystemHelper.TileXYToQuadkey(tileX, tileY, quadkeyLength);
        }

        public static string PixelXYToQuadkey(int pixelX, int pixelY, int quadkeyLength, int resolution)
        {
            BingMapsTileSystemHelper.PixelXYToTileXY(pixelX, pixelY, out int tx, out int ty, BingMapsTileSystemHelper.GetTileSize(quadkeyLength, resolution));
            return BingMapsTileSystemHelper.TileXYToQuadkey(tx, ty, quadkeyLength);
        }

        public static void GetCoordinatesInChildQuadkey(string quadkey, int x, int y, string childQuadkey, out int resx, out int resy, int resolution)
        {
            resx = x;
            resy = y;
            int parentSize = BingMapsTileSystemHelper.GetTileSize(quadkey, resolution);
            while (quadkey != childQuadkey)
            {
                parentSize /= 2;

                int c = childQuadkey[quadkey.Length] - '0';
                if ((c & 1) != 0)
                {
                    resx -= parentSize;
                }

                if ((c & 2) != 0)
                {
                    resy -= parentSize;
                }

                quadkey += childQuadkey[quadkey.Length];
            }
        }

        public static void GetCoordinatesInParentQuadkey(string quadkey, int x, int y, string parentQuadkey, out int resx, out int resy, int resolution)
        {
            resx = x;
            resy = y;
            int tileSize = BingMapsTileSystemHelper.GetTileSize(quadkey, resolution);
            while (quadkey != parentQuadkey)
            {
                int c = quadkey.Last() - '0';
                if ((c & 1) != 0)
                {
                    resx += tileSize;
                }

                if ((c & 2) != 0)
                {
                    resy += tileSize;
                }

                tileSize *= 2;
                quadkey = quadkey.Substring(0, quadkey.Length - 1);
            }
        }

        public static List<string> GetBoundingBoxQuadkeys(BoundingBox bb, int quadkeyLength, int limit)
        {
            BingMapsTileSystemHelper.LatLongToPixelXY(bb.NorthWest.Latitude, bb.NorthWest.Longitude, out var px1, out var py1, BingMapsTileSystemHelper.MaxResolution);
            BingMapsTileSystemHelper.PixelXYToTileXY(px1, py1, out var tx1, out var ty1, BingMapsTileSystemHelper.GetTileSize(quadkeyLength, BingMapsTileSystemHelper.MaxResolution));

            BingMapsTileSystemHelper.LatLongToPixelXY(bb.SouthEast.Latitude, bb.SouthEast.Longitude, out var px2, out var py2, BingMapsTileSystemHelper.MaxResolution);
            BingMapsTileSystemHelper.PixelXYToTileXY(px2, py2, out var tx2, out var ty2, BingMapsTileSystemHelper.GetTileSize(quadkeyLength, BingMapsTileSystemHelper.MaxResolution));

            var res = new List<string>();

            int fromTx = Math.Min(tx1, tx2), toTx = Math.Max(tx1, tx2);
            int fromTy = Math.Min(ty1, ty2), toTy = Math.Max(ty1, ty2);

            if (limit != -1 && (toTx - fromTx + 1) * (toTy - fromTy + 1) > limit)
            {
                return res;
            }

            for (int x = fromTx; x <= toTx; x++)
            {
                for (int y = fromTy; y <= toTy; y++)
                {
                    res.Add(BingMapsTileSystemHelper.TileXYToQuadkey(x, y, quadkeyLength));
                }
            }

            return res;
        }

        public static int GetTileSize(string quadkey, int resolution)
        {
            return 1 << (resolution - quadkey.Length);
        }

        public static int GetTileSize(int quadkeyLength, int resolution)
        {
            return 1 << (resolution - quadkeyLength);
        }

        public static int GetResolution(int quadkeyLength, int size)
        {
            return quadkeyLength + GenericHelper.Log2(size);
        }

        public static int GetQuadkeyLength(int size, int resolution)
        {
            return resolution - GenericHelper.Log2(size);
        }

        /// <summary>
        /// Converts a pixel from pixel XY coordinates at a specified level of detail into latitude/longitude WGS-84 coordinates (in degrees).
        /// </summary>
        /// <param name="pixelX">X coordinate of the point, in pixels.</param>
        /// <param name="pixelY">Y coordinates of the point, in pixels.</param>
        /// <param name="resolution">Resolution level. Default is 25.</param>
        public static LatLong PixelXYToLatLong(int pixelX, int pixelY, int resolution)
        {
            var mapSize = BingMapsTileSystemHelper.GetTileSize(string.Empty, resolution);
            var x = (BingMapsTileSystemHelper.Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
            var y = 0.5 - (BingMapsTileSystemHelper.Clip(pixelY, 0, mapSize - 1) / mapSize);
            return new LatLong(90 - (360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI), 360 * x);
        }

        /// <summary>
        /// Converts tile XY coordinates into a Quadkey at a specified level of detail.
        /// </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <param name="quadkeyLength">Desired quadkey length.</param>
        /// <returns>A string containing the QuadKey.</returns>
        public static string TileXYToQuadkey(int tileX, int tileY, int quadkeyLength)
        {
            var quadkey = new StringBuilder();
            for (int i = quadkeyLength; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((tileX & mask) != 0)
                {
                    digit++;
                }

                if ((tileY & mask) != 0)
                {
                    digit++;
                    digit++;
                }

                quadkey.Append(digit);
            }

            return quadkey.ToString();
        }

        /// <summary>
        /// Converts pixel XY coordinates into tile XY coordinates of the tile containing the specified pixel.
        /// </summary>
        /// <param name="pixelX">Pixel X coordinate.</param>
        /// <param name="pixelY">Pixel Y coordinate.</param>
        /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>
        /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
        /// <param name="tileSize">tileSize.</param>
        private static void PixelXYToTileXY(int pixelX, int pixelY, out int tileX, out int tileY, int tileSize)
        {
            tileX = pixelX / tileSize;
            tileY = pixelY / tileSize;
        }

        /// <summary>
        /// Clips a number to the specified minimum and maximum values.
        /// </summary>
        /// <param name="x">The number to clip.</param>
        /// <param name="minValue">Minimum allowable value.</param>
        /// <param name="maxValue">Maximum allowable value.</param>
        /// <returns>The clipped value.</returns>
        private static double Clip(double x, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(x, minValue), maxValue);
        }
    }
}