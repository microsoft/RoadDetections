// <copyright file="DownloadedImage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.ImageDownload
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Net;
    using System.Net.Http.Headers;
    using Emgu.CV;
    using Utils;

    internal class DownloadedImage
    {
        private readonly Dictionary<string, WebHeaderCollection> headers;

        private Mat image;

        internal DownloadedImage(int width, int height)
        {
            this.image = MatUtils.CreateImage(height, width);
            this.headers = new Dictionary<string, WebHeaderCollection>();
        }

        internal DownloadedImage(string quadkey, HttpResponseHeaders httpHeaders)
        {
            this.image = null;
            var headers = new WebHeaderCollection();
            foreach (var (key, value) in httpHeaders)
            {
                headers.Set(key, string.Join("|||", value));
            }

            this.headers = new Dictionary<string, WebHeaderCollection> { { quadkey, headers } };
        }

        internal DownloadedImage(string quadkey, byte[] imageBytes, WebHeaderCollection headers)
        {
            this.image = MatUtils.ImageFromBytes(imageBytes);
            this.headers = new Dictionary<string, WebHeaderCollection> { { quadkey, headers } };
        }

        internal DownloadedImage(byte[] imageBytes, string serializedHeader)
        {
            this.image = MatUtils.ImageFromBytes(imageBytes);
            this.headers = ImageMetadataHelper.GetDeserializedHeader(serializedHeader);
        }

        internal DownloadedImage(DownloadedImage another, Tuple<string, string> pcQuadkey)
        {
            this.image = another.image;
            var (parentQuadkey, childQuadkey) = pcQuadkey;
            int parentSize = this.image.Cols;
            int childSize = parentSize >> (childQuadkey.Length - parentQuadkey.Length);
            GeoUtils.BingMapsTileSystem.QuadkeyOrientationHelper.GetSubquadkeyCoordinates(parentQuadkey, childQuadkey, out var x, out var y, parentSize);
            this.image = new Mat(another.image, new Rectangle(x, y, childSize, childSize));

            this.headers = new Dictionary<string, WebHeaderCollection>();
            this.AddHeaders(another.headers);
        }

        public int Width => this.image.Width;

        public int Height => this.image.Height;

        internal AerialImage ToAerialImage(int width, int height)
        {
            byte[] resImage = null;
            if (this.image != null)
            {
                if (this.image.Width != width || this.image.Height != height)
                {
                    resImage = MatUtils.Resize(this.image, width, height);
                }
                else
                {
                    resImage = MatUtils.ImageToBytes(this.image, MatUtils.ImageFileFormat.PNG);
                }
            }

            return new AerialImage(resImage, ImageMetadataHelper.GetSerializedHeader(this.headers));
        }

        internal DownloadedImage CombineHorizontally(DownloadedImage another)
        {
            if (this.image == null)
            {
                this.image = another.image;
            }
            else
            {
                CvInvoke.HConcat(this.image, another.image, this.image);
            }

            this.AddHeaders(another.headers);
            return this;
        }

        internal DownloadedImage CombineVertically(DownloadedImage another, Tuple<string, string> pcQuadkey)
        {
            var (parentQuadkey, childQuadkey) = pcQuadkey;
            int parentSize = another.image.Cols;
            int childSize = parentSize >> (childQuadkey.Length - parentQuadkey.Length);
            GeoUtils.BingMapsTileSystem.QuadkeyOrientationHelper.GetSubquadkeyCoordinates(parentQuadkey, childQuadkey, out var x, out var y, parentSize);
            var image = new Mat(another.image, new Rectangle(x, y, childSize, childSize));

            if (this.image == null)
            {
                this.image = image;
            }
            else
            {
                CvInvoke.VConcat(this.image, image, this.image);
            }

            this.AddHeaders(another.headers);
            return this;
        }

        internal DownloadedImage Cut(Rectangle rectangle)
        {
            this.image = new Mat(this.image, rectangle);
            return this;
        }

        internal DownloadedImage Add(DownloadedImage another, Rectangle anotherBox, Rectangle thisBox)
        {
            new Mat(another.image, anotherBox).CopyTo(new Mat(this.image, thisBox));
            this.AddHeaders(another.headers);
            return this;
        }

        private void AddHeaders(Dictionary<string, WebHeaderCollection> headers)
        {
            foreach (var kvp in headers)
            {
                if (this.headers.ContainsKey(kvp.Key) == false)
                {
                    this.headers.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
