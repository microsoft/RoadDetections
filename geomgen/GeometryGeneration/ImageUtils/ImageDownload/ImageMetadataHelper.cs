// <copyright file="ImageMetadataHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.ImageDownload
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using GeoUtils;
    using Utils;

    public static class ImageMetadataHelper
    {
        private static readonly string[] RestrictedImagerySources = new string[] { };

        public enum AccessLevel
        {
            Allowed,
            Restricted,
        }

        public static int GetDownloadLevel(string quadkey, string serializedHeaders)
        {
            var downloadLevels = new List<int>();
            foreach (var imageQuadkey in ImageMetadataHelper.GetDeserializedHeader(serializedHeaders).Keys)
            {
                if (imageQuadkey.StartsWith(quadkey) || quadkey.StartsWith(imageQuadkey))
                {
                    downloadLevels.Add(imageQuadkey.Length);
                }
            }

            return downloadLevels.Min();
        }

        public static List<Tuple<string, string>> GetImagerySources(string quadkey, string serializedHeaders)
        {
            var res = new List<Tuple<string, string>>();
            foreach (var (imageQuadkey, imageHeaders) in ImageMetadataHelper.GetDeserializedHeader(serializedHeaders))
            {
                if (imageQuadkey.StartsWith(quadkey) || quadkey.StartsWith(imageQuadkey))
                {
                    foreach (var key in imageHeaders.AllKeys)
                    {
                        if (key == "X-VE-TILEMETA-Product-IDs")
                        {
                            var access = AccessLevel.Allowed;
                            foreach (var imgsrc in imageHeaders[key].Split(','))
                            {
                                if (ImageMetadataHelper.RestrictedImagerySources.Contains(imgsrc))
                                {
                                    access = AccessLevel.Restricted;
                                    break;
                                }
                            }

                            res.Add(Tuple.Create(string.Join(",", imageHeaders[key].Split(',').Select(src => int.Parse(src)).OrderBy(src => src)), access.ToString()));
                        }
                    }
                }
            }

            return res;
        }

        public static AccessLevel GetAccessLevel(string quadkey, string serializedHeaders)
        {
            foreach (var (imageQuadkey, imageHeaders) in ImageMetadataHelper.GetDeserializedHeader(serializedHeaders))
            {
                if (imageQuadkey.StartsWith(quadkey) || quadkey.StartsWith(imageQuadkey))
                {
                    foreach (var key in imageHeaders.AllKeys)
                    {
                        if (key == "X-VE-TILEMETA-Product-IDs")
                        {
                            foreach (var imgsrc in imageHeaders[key].Split(','))
                            {
                                if (ImageMetadataHelper.RestrictedImagerySources.Contains(imgsrc))
                                {
                                    return AccessLevel.Restricted;
                                }
                            }
                        }
                    }
                }
            }

            return AccessLevel.Allowed;
        }

        internal static string GetSerializedHeader(Dictionary<string, WebHeaderCollection> headers)
        {
            var serializedHeaders = new Dictionary<string, string>();
            foreach (var (imageQuadkey, imageHeaders) in headers)
            {
                var dictionary = new Dictionary<string, string>();
                foreach (string key in imageHeaders.AllKeys)
                {
                    dictionary[key] = imageHeaders[key];
                }

                serializedHeaders.Add(imageQuadkey, JsonUtils.SerializeDictionary(dictionary));
            }

            return JsonUtils.SerializeDictionary(serializedHeaders);
        }

        internal static Dictionary<string, WebHeaderCollection> GetDeserializedHeader(string serializedHeaders)
        {
            var res = new Dictionary<string, WebHeaderCollection>();
            foreach (var (imageQuadkey, imageHeaders) in JsonUtils.DeserializeDictionary(serializedHeaders))
            {
                var headers = new WebHeaderCollection();
                foreach (var (key, value) in JsonUtils.DeserializeDictionary(imageHeaders))
                {
                    headers.Add(key, value);
                }

                res.Add(imageQuadkey, headers);
            }

            return res;
        }
    }
}
