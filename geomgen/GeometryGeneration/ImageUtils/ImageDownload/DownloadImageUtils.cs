// <copyright file="DownloadImageUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.ImageDownload
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using GeoUtils.BingMapsTileSystem;
    using Utils;

    public static class DownloadImageUtils
    {
        public enum ImageryUpdateTimeSpan
        {
            Days30,
            Days90,
            Days180,
            Days365,
        }

        /// <summary>
        /// Downloads an aerial image of a given quadkey at given resolution with given padding. Method will try to download imagery from
        /// a higher resolution if downloadAtHigherResolution flag is set. If an image at that resolution is unavailable, method will try to
        /// download the image at a lower resolution. In addition to the imagery itself, method will return headers that were received when
        /// downloading the imagery for each quadkey. Headers usually have information about CaptureDate, ImagerySource, etc.
        /// </summary>
        public static AerialImage GetCombinedImage(string quadkey, int imageSize, int padding = 0, bool downloadAtHigherResolution = false)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            int sizeCoefficient = downloadAtHigherResolution ? 2 : 1;
            for (int div = 1; (imageSize * sizeCoefficient) % div == 0 && (padding * sizeCoefficient) % div == 0; div *= 2)
            {
                var downloadedImage = DownloadImageUtils.DownloadAndCombinedImage(quadkey, (imageSize * sizeCoefficient) / div, (padding * sizeCoefficient) / div).Result;
                if (downloadedImage != null)
                {
                    return downloadedImage.ToAerialImage(imageSize + (2 * padding), imageSize + (2 * padding));
                }
            }

            throw new Exception("No Bing image can be found in this quadkey: " + quadkey);
        }

        public static string GetAerialImageryUpdates(ImageryUpdateTimeSpan timeSpan)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string timeSpanDays;
            switch (timeSpan)
            {
                case ImageryUpdateTimeSpan.Days30:
                    timeSpanDays = "30";
                    break;
                case ImageryUpdateTimeSpan.Days90:
                    timeSpanDays = "90";
                    break;
                case ImageryUpdateTimeSpan.Days180:
                    timeSpanDays = "180";
                    break;
                case ImageryUpdateTimeSpan.Days365:
                    timeSpanDays = "365";
                    break;
                default:
                    throw new Exception("Unexpected value of ImageryUpdateTimeSpan: " + timeSpan);
            }

            string url = @"https://ipfviewbackup.z20.web.core.windows.net/coveragejson/Coverage_Aerial_Last" + timeSpanDays + @"Days.json";
            for (int attempt = 1; attempt <= Constants.DownloadImageAttempts; attempt++)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        return client.DownloadString(url);
                    }
                }
                catch (Exception e)
                {
                    if (attempt == Constants.DownloadImageAttempts)
                    {
                        throw e;
                    }
                }
            }

            return null;
        }

        private static async Task<DownloadedImage> DownloadAndCombinedImage(string quadkey, int imageSize, int padding)
        {
            try
            {
                int imageBlockSize = Math.Min(imageSize, Constants.ImageDownloadSize);
                int paddingShift = (padding + imageBlockSize - 1) / imageBlockSize; // Ceil(padding / imageBlockSize);
                string topleftQuadkey = quadkey + new string('0', GenericHelper.Log2(imageSize / Constants.ImageDownloadSize));
                BingMapsTileSystemHelper.QuadkeyToTileXY(topleftQuadkey, out var tx, out var ty);
                int x0 = tx - paddingShift, x1 = tx + (imageSize / imageBlockSize) + paddingShift;
                int y0 = ty - paddingShift, y1 = ty + (imageSize / imageBlockSize) + paddingShift;
                var images = new Task<DownloadedImage>[x1 - x0, y1 - y0];
                var tasks = new Dictionary<string, Task<DownloadedImage>>();
                for (int x = x0; x < x1; x++)
                {
                    for (int y = y0; y < y1; y++)
                    {
                        Task<DownloadedImage> task = null;
                        int downloadLevel = quadkey.Length + GenericHelper.Log2(imageSize) - GenericHelper.Log2(Constants.ImageDownloadSize);
                        var needQuadkey = BingMapsTileSystemHelper.TileXYToQuadkey(x, y, topleftQuadkey.Length);
                        var downloadQuadkey = needQuadkey.Substring(0, downloadLevel);

                        if (tasks.ContainsKey(downloadQuadkey))
                        {
                            task = tasks[downloadQuadkey];
                        }
                        else
                        {
                            task = DownloadData(downloadQuadkey);
                            tasks.Add(downloadQuadkey, task);
                        }

                        images[x - x0, y - y0] = task;
                    }
                }

                await Task.WhenAll(tasks.Values);

                DownloadedImage res = null;
                for (int x = x0; x < x1; x++)
                {
                    DownloadedImage cur = null;
                    for (int y = y0; y < y1; y++)
                    {
                        int downloadLevel = quadkey.Length + GenericHelper.Log2(imageSize) - GenericHelper.Log2(Constants.ImageDownloadSize);
                        var needQuadkey = BingMapsTileSystemHelper.TileXYToQuadkey(x, y, topleftQuadkey.Length);
                        var downloadQuadkey = needQuadkey.Substring(0, downloadLevel);
                        var cutTuple = Tuple.Create(downloadQuadkey, needQuadkey);

                        if (cur == null)
                        {
                            cur = new DownloadedImage(await images[x - x0, y - y0], cutTuple);
                        }
                        else
                        {
                            cur = cur.CombineVertically(await images[x - x0, y - y0], cutTuple);
                        }
                    }

                    if (res == null)
                    {
                        res = cur;
                    }
                    else
                    {
                        res = res.CombineHorizontally(cur);
                    }
                }

                int cutx = (res.Width - imageSize - (2 * padding)) / 2;
                int cuty = (res.Height - imageSize - (2 * padding)) / 2;

                if (cutx > 0 || cuty > 0)
                {
                    res.Cut(new Rectangle(cutx, cuty, imageSize + (2 * padding), imageSize + (2 * padding)));
                }

                return res;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        private static async Task<DownloadedImage> DownloadData(string quadkey)
        {
            string url = AerialImageUrl(quadkey);
            bool bingNonExistingOrBrokenImage = false;
            for (int attempt = 1; attempt <= Constants.DownloadImageAttempts; attempt++)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var image = await client.DownloadDataTaskAsync(url);

                        if (client.ResponseHeaders.Get("x-ve-tile-info") == "no-tile")
                        {
                            bingNonExistingOrBrokenImage = true;
                            break;
                        }

                        return new DownloadedImage(quadkey, image, client.ResponseHeaders);
                    }
                }
                catch (Exception e)
                {
                    if (attempt == Constants.DownloadImageAttempts)
                    {
                        throw e;
                    }
                }
            }

            if (bingNonExistingOrBrokenImage)
            {
                throw new ArgumentException("A BingNonExistingImageBase64 or BingBrokenImageBase64 found at " + quadkey);
            }
            else
            {
                throw new ArgumentException("Imagery download attempts failed at " + quadkey);
            }
        }

        private static string AerialImageUrl(string q)
        {
            return "http://ecn.t" + q.Last() + ".tiles.virtualearth.net/tiles/a" + q + ".jpeg?g=1";
        }
    }
}