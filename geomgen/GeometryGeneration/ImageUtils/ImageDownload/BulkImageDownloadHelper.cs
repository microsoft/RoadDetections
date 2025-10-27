// <copyright file="BulkImageDownloadHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.ImageDownload
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;
    using ImageUtils.Images;
    using Utils;

    public static class BulkImageDownloadHelper
    {
        public static RgbImage GetAnImage(BoundingBox bbox, int resolution)
        {
            var quadkeysToDownload = BulkImageDownloadHelper.GetQuadkeysForBoundingBoxImageDownload(bbox.NorthWest.Latitude, bbox.NorthWest.Longitude, bbox.SouthEast.Latitude, bbox.SouthEast.Longitude, resolution);

            var images = new List<Tuple<string, byte[], string>>();
            foreach (var quadkey in quadkeysToDownload)
            {
                var imageSize = BingMapsTileSystemHelper.GetTileSize(quadkey.Length, resolution);
                var downloaded = DownloadImageUtils.GetCombinedImage(quadkey, imageSize);
                images.Add(Tuple.Create(quadkey, downloaded.Image, downloaded.Headers));
            }

            return new RgbImage(BulkImageDownloadHelper.CombineImagesByBoundingBox(bbox, -1, resolution, images).Image);
        }

        public static AerialImage GetAnImage(string quadkey)
        {
            return DownloadImageUtils.GetCombinedImage(quadkey, Constants.ImageDownloadSize);
        }

        public static List<string> GetQuadkeysForQuadkeyImageDownload(string quadkey, int imageSize, int padding, bool downloadAtHigherResolution)
        {
            imageSize *= downloadAtHigherResolution ? 2 : 1;
            padding *= downloadAtHigherResolution ? 2 : 1;

            int imageBlockSize = Math.Min(imageSize, Constants.ImageDownloadSize);
            int paddingShift = (padding + imageBlockSize - 1) / imageBlockSize; // Ceil(padding / imageBlockSize);
            string topleftQuadkey = quadkey + new string('0', GenericHelper.Log2(imageSize / Constants.ImageDownloadSize));
            BingMapsTileSystemHelper.QuadkeyToTileXY(topleftQuadkey, out var tx, out var ty);
            int x0 = tx - paddingShift, x1 = tx + (imageSize / imageBlockSize) + paddingShift;
            int y0 = ty - paddingShift, y1 = ty + (imageSize / imageBlockSize) + paddingShift;

            var res = new List<string>();
            for (int x = x0; x < x1; x++)
            {
                for (int y = y0; y < y1; y++)
                {
                    int downloadLevel = quadkey.Length + GenericHelper.Log2(imageSize) - GenericHelper.Log2(Constants.ImageDownloadSize);
                    var needQuadkey = BingMapsTileSystemHelper.TileXYToQuadkey(x, y, topleftQuadkey.Length);
                    var downloadQuadkey = needQuadkey.Substring(0, downloadLevel);
                    res.Add(downloadQuadkey);
                }
            }

            return res.Distinct().ToList();
        }

        public static List<string> GetQuadkeysForLatLongImageDownload(double lat, double lon, double imageSizeMeters, int preferredDownloadResolution)
        {
            int quadkeyLength = BingMapsTileSystemHelper.GetQuadkeyLength(Constants.ImageDownloadSize, preferredDownloadResolution);
            return BingMapsTileSystemHelper.GetBoundingBoxQuadkeys(new BoundingBox(new LatLong(lat, lon), new LatLong(lat, lon)).AddMargin(imageSizeMeters / 2), quadkeyLength, -1);
        }

        public static List<string> GetQuadkeysForBoundingBoxImageDownload(double lat1, double lon1, double lat2, double lon2, int preferredDownloadResolution)
        {
            int quadkeyLength = BingMapsTileSystemHelper.GetQuadkeyLength(Constants.ImageDownloadSize, preferredDownloadResolution);
            return BingMapsTileSystemHelper.GetBoundingBoxQuadkeys(new BoundingBox(new LatLong(lat1, lon1), new LatLong(lat2, lon2)), quadkeyLength, -1);
        }

        public static AerialImage CombineImagesByQuadkey(string quadkey, int imageSize, int padding, bool downloadAtHigherResolution, List<Tuple<string, byte[], string>> downloadedImages)
        {
            var imagesDic = new Dictionary<string, DownloadedImage>();
            foreach (var (imageQuadkey, imageData, imageHeaders) in downloadedImages)
            {
                imagesDic.Add(imageQuadkey, new DownloadedImage(imageData, imageHeaders));
            }

            imageSize *= downloadAtHigherResolution ? 2 : 1;
            padding *= downloadAtHigherResolution ? 2 : 1;

            int imageBlockSize = Math.Min(imageSize, Constants.ImageDownloadSize);
            int paddingShift = (padding + imageBlockSize - 1) / imageBlockSize; // Ceil(padding / imageBlockSize);
            string topleftQuadkey = quadkey + new string('0', GenericHelper.Log2(imageSize / Constants.ImageDownloadSize));
            BingMapsTileSystemHelper.QuadkeyToTileXY(topleftQuadkey, out var tx, out var ty);
            int x0 = tx - paddingShift, x1 = tx + (imageSize / imageBlockSize) + paddingShift;
            int y0 = ty - paddingShift, y1 = ty + (imageSize / imageBlockSize) + paddingShift;

            var res = new DownloadedImage((x1 - x0) * imageBlockSize, (y1 - y0) * imageBlockSize);
            for (int x = x0; x < x1; x++)
            {
                for (int y = y0; y < y1; y++)
                {
                    int downloadLevel = quadkey.Length + GenericHelper.Log2(imageSize) - GenericHelper.Log2(Constants.ImageDownloadSize);
                    var needQuadkey = BingMapsTileSystemHelper.TileXYToQuadkey(x, y, topleftQuadkey.Length);
                    var downloadQuadkey = needQuadkey.Substring(0, downloadLevel);

                    var cur = imagesDic[downloadQuadkey];
                    int parentSize = cur.Width;
                    int childSize = parentSize >> (needQuadkey.Length - downloadQuadkey.Length);
                    QuadkeyOrientationHelper.GetSubquadkeyCoordinates(downloadQuadkey, needQuadkey, out var nx, out var ny, parentSize);

                    int i = (x - x0) * childSize;
                    int j = (y - y0) * childSize;

                    res.Add(cur, new Rectangle(nx, ny, childSize, childSize), new Rectangle(i, j, childSize, childSize));
                }
            }

            int cutx = (res.Width - imageSize - (2 * padding)) / 2;
            int cuty = (res.Height - imageSize - (2 * padding)) / 2;

            if (cutx > 0 || cuty > 0)
            {
                res = res.Cut(new Rectangle(cutx, cuty, imageSize + (2 * padding), imageSize + (2 * padding)));
            }

            imageSize /= downloadAtHigherResolution ? 2 : 1;
            padding /= downloadAtHigherResolution ? 2 : 1;

            return res.ToAerialImage(imageSize + (2 * padding), imageSize + (2 * padding));
        }

        public static AerialImage CombineImagesByLatLong(double lat, double lon, double imageSizeMeters, int finalImageSize, int preferredDownloadResolution, List<Tuple<string, byte[], string>> downloadedImages)
        {
            var boundingBox = new BoundingBox(new LatLong(lat, lon)).AddMargin(imageSizeMeters / 2);
            return BulkImageDownloadHelper.CombineImagesByBoundingBox(boundingBox, finalImageSize, preferredDownloadResolution, downloadedImages);
        }

        public static AerialImage CombineImagesByBoundingBox(double lat1, double lon1, double lat2, double lon2, int finalImageSize, int preferredDownloadResolution, List<Tuple<string, byte[], string>> downloadedImages)
        {
            var boundingBox = new BoundingBox(new LatLong(lat1, lon1), new LatLong(lat2, lon2));
            return CombineImagesByBoundingBox(boundingBox, finalImageSize, preferredDownloadResolution, downloadedImages);
        }

        public static AerialImage CombineImagesByBoundingBox(BoundingBox boundingBox, int finalImageSize, int preferredDownloadResolution, List<Tuple<string, byte[], string>> downloadedImages)
        {
            BingMapsTileSystemHelper.LatLongToPixelXY(boundingBox.NorthWest.Latitude, boundingBox.NorthWest.Longitude, out var p1x, out var p1y, preferredDownloadResolution);
            BingMapsTileSystemHelper.LatLongToPixelXY(boundingBox.SouthEast.Latitude, boundingBox.SouthEast.Longitude, out var p2x, out var p2y, preferredDownloadResolution);

            var res = new DownloadedImage(p2x - p1x + 1, p2y - p1y + 1);
            foreach (var (imageQuadkey, imageData, imageHeaders) in downloadedImages)
            {
                var cur = new DownloadedImage(imageData, imageHeaders);
                BingMapsTileSystemHelper.QuadkeyToTileXY(imageQuadkey, out var tx, out var ty);
                var qx0 = tx * BingMapsTileSystemHelper.GetTileSize(imageQuadkey.Length, preferredDownloadResolution);
                var qy0 = ty * BingMapsTileSystemHelper.GetTileSize(imageQuadkey.Length, preferredDownloadResolution);

                // cut cur if needed
                int i0 = Math.Max(qx0, p1x) - qx0;
                int j0 = Math.Max(qy0, p1y) - qy0;
                int i1 = Math.Min(qx0 + cur.Height - 1, p2x) - qx0;
                int j1 = Math.Min(qy0 + cur.Width - 1, p2y) - qy0;
                int rows = i1 - i0 + 1;
                int cols = j1 - j0 + 1;

                res.Add(cur, new Rectangle(i0, j0, rows, cols), new Rectangle(Math.Max(qx0, p1x) - p1x, Math.Max(qy0, p1y) - p1y, rows, cols));
            }

            if (finalImageSize == -1)
            {
                return res.ToAerialImage(res.Width, res.Height);
            }
            else
            {
                return res.ToAerialImage(finalImageSize, finalImageSize);
            }
        }
    }
}
