// <copyright file="BlackWhiteImage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.Images
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using GeoUtils.BingMapsTileSystem;
    using Utils;

    public partial class BlackWhiteImage : BaseImage
    {
        public BlackWhiteImage(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Data = new int[this.Width, this.Height];
        }

        public BlackWhiteImage(int width, int height, int value)
        {
            this.Width = width;
            this.Height = height;
            this.Data = new int[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.Data[i, j] = value;
                }
            }
        }

        public BlackWhiteImage(int size)
        {
            this.Width = size;
            this.Height = size;
            this.Data = new int[this.Width, this.Height];
        }

        public BlackWhiteImage(Bitmap bitmap)
        {
            this.Width = bitmap.Width;
            this.Height = bitmap.Height;
            this.Data = new int[this.Width, this.Height];
            this.Initialize(bitmap);
        }

        public BlackWhiteImage(BlackWhiteImage other)
        {
            this.Width = other.Width;
            this.Height = other.Height;
            this.Data = new int[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.Data[i, j] = other.Data[i, j];
                }
            }
        }

        public BlackWhiteImage(BinaryImage other)
        {
            this.Width = other.Width;
            this.Height = other.Height;
            this.Data = new int[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.Data[i, j] = other.Data[i, j] ? Constants.LabelRescalingFactor : 0;
                }
            }
        }

        private BlackWhiteImage(byte[] data)
        {
            var mat = new Mat();
            CvInvoke.Imdecode(data, ImreadModes.Color, mat);
            var img = mat.ToImage<Bgr, byte>();
            this.Width = img.Cols;
            this.Height = img.Rows;
            this.Data = new int[this.Width, this.Height];
            this.Initialize(img);
        }

        public int[,] Data { get; }

        public static BlackWhiteImage CreateImageFromBase64(string base64Image)
        {
            if (base64Image == null)
            {
                return null;
            }

            byte[] data;
            try
            {
                data = Convert.FromBase64String(base64Image);
            }
            catch (FormatException)
            {
                return null;
            }

            return new BlackWhiteImage(data);
        }

        public static List<Tuple<string, string, string>> BreakIntoStringTiles(string quadkey, string base64Img, string base64Bin, int resQuadkeyLength)
        {
            var img = CreateImageFromBase64(base64Img);
            var bin = CreateImageFromBase64(base64Bin);

            if (img == null || bin == null)
            {
                throw new Exception("Provided base64 strings are null or empty");
            }

            var res = new List<Tuple<string, string, string>>();

            int n = 1 << (resQuadkeyLength - quadkey.Length);
            int srcImagePadding = (img.Width - (1 << GenericHelper.Log2(img.Width))) / 2;
            int resImageSize = (img.Width - (2 * srcImagePadding)) / n;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var curquadkey = QuadkeyOrientationHelper.GetRelativeQuadkey(quadkey, i, j, n);
                    var curImg = new BlackWhiteImage(resImageSize, resImageSize);
                    var curBin = new BlackWhiteImage(resImageSize, resImageSize);

                    for (int ii = 0; ii < resImageSize; ii++)
                    {
                        for (int jj = 0; jj < resImageSize; jj++)
                        {
                            curImg.Data[ii, jj] = img.Data[(j * resImageSize) + ii + srcImagePadding, (i * resImageSize) + jj + srcImagePadding];
                            curBin.Data[ii, jj] = bin.Data[(j * resImageSize) + ii + srcImagePadding, (i * resImageSize) + jj + srcImagePadding];
                        }
                    }

                    res.Add(Tuple.Create(curquadkey, curImg.ToBase64String(), curBin.ToBase64String()));
                }
            }

            return res;
        }

        public BlackWhiteImage Resize(int w, int h)
        {
            var rgb = new RgbImage(this).Resize(w, h);
            var res = new BlackWhiteImage(w, h);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    res.Data[i, j] = GenericHelper.Round((rgb.Red[i, j] + rgb.Green[i, j] + rgb.Blue[i, j]) / 3.0);
                }
            }

            return res;
        }

        protected override void GetRgb(int i, int j, out int r, out int g, out int b)
        {
            r = this.Data[i, j];
            g = this.Data[i, j];
            b = this.Data[i, j];
        }

        protected override void SetRgb(int i, int j, int r, int g, int b)
        {
            this.Data[i, j] = r;
        }

        protected override Color GetPixelColor(int i, int j)
        {
            return Color.FromArgb(this.Data[i, j], this.Data[i, j], this.Data[i, j]);
        }

        protected override void GetOpenCvColor(int i, int j, out byte r, out byte g, out byte b)
        {
            r = (byte)this.Data[i, j];
            g = 0;
            b = 0;
        }

        protected override void SetOpenCvColor(int i, int j, Bgr cv)
        {
            this.Data[i, j] = GenericHelper.Round(cv.Red);
        }

        protected override void SetPixel(int i, int j, Color color)
        {
            if (color.R != color.G || color.R != color.B)
            {
                throw new Exception("Only grayscale colors are allowed to be set to grayscale image");
            }

            this.Data[i, j] = color.R;
        }

        protected override BaseImage Create(int width, int height)
        {
            return new BlackWhiteImage(width, height);
        }
    }
}