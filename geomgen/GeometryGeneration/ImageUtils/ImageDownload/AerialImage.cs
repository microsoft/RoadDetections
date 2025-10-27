// <copyright file="AerialImage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.ImageDownload
{
    public class AerialImage
    {
        internal AerialImage(byte[] image, string headers)
        {
            this.Image = image;
            this.Headers = headers;
        }

        public byte[] Image { get; }

        public string Headers { get; }
    }
}
