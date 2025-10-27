// <copyright file="RgbImage.cs" company="Microsoft">
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
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;
    using Utils;

    public partial class RgbImage : BaseImage
    {
        public RgbImage(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Red = new int[this.Width, this.Height];
            this.Green = new int[this.Width, this.Height];
            this.Blue = new int[this.Width, this.Height];
        }

        public RgbImage(int size)
        {
            this.Width = size;
            this.Height = size;
            this.Red = new int[this.Width, this.Height];
            this.Green = new int[this.Width, this.Height];
            this.Blue = new int[this.Width, this.Height];
        }

        public RgbImage(Bitmap bitmap)
        {
            this.Width = bitmap.Width;
            this.Height = bitmap.Height;
            this.Red = new int[this.Width, this.Height];
            this.Green = new int[this.Width, this.Height];
            this.Blue = new int[this.Width, this.Height];
            this.Initialize(bitmap);
        }

        public RgbImage(RgbImage other)
        {
            this.Width = other.Width;
            this.Height = other.Height;
            this.Red = new int[this.Width, this.Height];
            this.Green = new int[this.Width, this.Height];
            this.Blue = new int[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.Red[i, j] = other.Red[i, j];
                    this.Green[i, j] = other.Green[i, j];
                    this.Blue[i, j] = other.Blue[i, j];
                }
            }
        }

        public RgbImage(BlackWhiteImage other)
        {
            this.Width = other.Width;
            this.Height = other.Height;
            this.Red = new int[this.Width, this.Height];
            this.Green = new int[this.Width, this.Height];
            this.Blue = new int[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.Red[i, j] = other.Data[i, j];
                    this.Green[i, j] = other.Data[i, j];
                    this.Blue[i, j] = other.Data[i, j];
                }
            }
        }

        public RgbImage(BinaryImage other)
        {
            this.Width = other.Width;
            this.Height = other.Height;
            this.Red = new int[this.Width, this.Height];
            this.Green = new int[this.Width, this.Height];
            this.Blue = new int[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.SetPixel(i, j, other.Data[i, j] ? Color.White : Color.Black);
                }
            }
        }

        public RgbImage(byte[] data)
        {
            Mat image = new Mat();
            CvInvoke.Imdecode(data, ImreadModes.Color, image);
            Image<Bgr, byte> img = image.ToImage<Bgr, byte>();
            this.Width = img.Cols;
            this.Height = img.Rows;
            this.Red = new int[this.Width, this.Height];
            this.Green = new int[this.Width, this.Height];
            this.Blue = new int[this.Width, this.Height];
            this.Initialize(img);
        }

        public int[,] Red { get; }

        public int[,] Green { get; }

        public int[,] Blue { get; }

        public static RgbImage CreateImageFromBase64(string base64Image)
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

            return new RgbImage(data);
        }

        /// <summary>
        /// Breaks an image represented by base64Img into images of quadkey level resQuadkeyLength under given resolution.
        /// </summary>
        public static List<Tuple<string, string>> BreakIntoStringTiles(string quadkey, string base64Img, int resQuadkeyLength)
        {
            var img = CreateImageFromBase64(base64Img);

            if (img == null)
            {
                throw new Exception("Provided base64 string is null or empty");
            }

            var res = new List<Tuple<string, string>>();

            int n = 1 << (resQuadkeyLength - quadkey.Length);
            int srcImagePadding = (img.Width - (1 << GenericHelper.Log2(img.Width))) / 2;
            int resImageSize = (img.Width - (2 * srcImagePadding)) / n;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var curquadkey = QuadkeyOrientationHelper.GetRelativeQuadkey(quadkey, i, j, n);
                    var curImg = new RgbImage(resImageSize, resImageSize);
                    int di = srcImagePadding + (j * resImageSize);
                    int dj = srcImagePadding + (i * resImageSize);

                    for (int ii = 0; ii < resImageSize; ii++)
                    {
                        for (int jj = 0; jj < resImageSize; jj++)
                        {
                            curImg.Red[ii, jj] = img.Red[di + ii, dj + jj];
                            curImg.Green[ii, jj] = img.Green[di + ii, dj + jj];
                            curImg.Blue[ii, jj] = img.Blue[di + ii, dj + jj];
                        }
                    }

                    res.Add(Tuple.Create(curquadkey, curImg.ToBase64String()));
                }
            }

            return res;
        }

        public static RgbImage CombineWithGray(RgbImage img, BlackWhiteImage gray)
        {
            var res = new RgbImage(gray);

            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    if (!img.IsTolerateColorDiff(i, j, Color.Black, 0))
                    {
                        if (img.IsTolerateColorDiff(i, j, Color.White, 0))
                        {
                            res.SetPixel(i, j, Color.Blue);
                        }
                        else
                        {
                            img.GetRgb(i, j, out var r, out var g, out var b);
                            res.SetRgb(i, j, r, g, b);
                        }
                    }
                }
            }

            return res;
        }

        public RgbImage AddAnImageOver(RgbImage bw, bool invertblue = false)
        {
            if (bw.Width != this.Width || bw.Height != this.Height)
            {
                throw new Exception("Images should have same size");
            }

            for (int i = 0; i < bw.Width; i++)
            {
                for (int j = 0; j < bw.Height; j++)
                {
                    var color = bw.GetPixelColor(i, j);

                    if (invertblue && bw.IsTolerateColorDiff(i, j, Color.Blue, 0))
                    {
                        this.GetRgb(i, j, out var r, out var g, out var b);
                        this.SetPixel(i, j, Color.FromArgb(255 - r, 255 - g, 255 - b));
                    }
                    else if (!bw.IsTolerateColorDiff(i, j, Color.Black, 0))
                    {
                        this.SetPixel(i, j, color);
                    }
                }
            }

            return this;
        }

        public RgbImage AddLabel(BinaryImage binaryImage, Color labelColor, int padding = 0, bool sprase = false)
        {
            return this.AddLabel(binaryImage, labelColor, padding, padding, sprase);
        }

        public RgbImage AddLabel(BinaryImage binaryImage, Color labelColor, int paddingI, int paddingJ, bool sprase = false)
        {
            var rand = new Random();

            for (int i = 0; i < binaryImage.Width; i++)
            {
                for (int j = 0; j < binaryImage.Height; j++)
                {
                    bool mark = false;
                    if (binaryImage.Data[i, j])
                    {
                        for (int ii = 0; ii < 8; ii++)
                        {
                            int ni = i + GeometryHelper.DIJ[ii, 0];
                            int nj = j + GeometryHelper.DIJ[ii, 1];
                            if (!binaryImage.CheckCoordinates(ni, nj) || !binaryImage.Data[ni, nj])
                            {
                                mark = true;
                            }
                        }
                    }

                    if (mark)
                    {
                        if (!sprase || rand.NextDouble() < 0.5)
                        {
                            this.SetPixel(i + paddingI, j + paddingJ, labelColor);
                        }
                    }
                }
            }

            return this;
        }

        public RgbImage Resize(int w, int h)
        {
            if (w == this.Width && h == this.Height)
            {
                return new RgbImage(this);
            }

            return new RgbImage(new Bitmap(this.ToBitmap(), new Size(w, h)));
        }

        protected override void GetRgb(int i, int j, out int r, out int g, out int b)
        {
            r = this.Red[i, j];
            g = this.Green[i, j];
            b = this.Blue[i, j];
        }

        protected override void SetRgb(int i, int j, int r, int g, int b)
        {
            this.Red[i, j] = r;
            this.Green[i, j] = g;
            this.Blue[i, j] = b;
        }

        protected override Color GetPixelColor(int i, int j)
        {
            return Color.FromArgb(this.Red[i, j], this.Green[i, j], this.Blue[i, j]);
        }

        protected override void GetOpenCvColor(int i, int j, out byte r, out byte g, out byte b)
        {
            r = (byte)this.Red[i, j];
            g = (byte)this.Green[i, j];
            b = (byte)this.Blue[i, j];
        }

        protected override void SetOpenCvColor(int i, int j, Bgr cv)
        {
            this.Red[i, j] = GenericHelper.Round(cv.Red);
            this.Green[i, j] = GenericHelper.Round(cv.Green);
            this.Blue[i, j] = GenericHelper.Round(cv.Blue);
        }

        protected override void SetPixel(int i, int j, Color color)
        {
            this.Red[i, j] = color.R;
            this.Green[i, j] = color.G;
            this.Blue[i, j] = color.B;
        }

        protected override BaseImage Create(int width, int height)
        {
            return new RgbImage(width, height);
        }
    }
}