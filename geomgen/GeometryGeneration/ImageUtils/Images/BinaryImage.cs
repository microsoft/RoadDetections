// <copyright file="BinaryImage.cs" company="Microsoft">
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
    using Utils;

    public partial class BinaryImage : BaseImage
    {
        public BinaryImage(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Data = new bool[this.Width, this.Height];
        }

        public BinaryImage(int size)
        {
            this.Width = size;
            this.Height = size;
            this.Data = new bool[this.Width, this.Height];
        }

        public BinaryImage(Bitmap bitmap)
        {
            this.Width = bitmap.Width;
            this.Height = bitmap.Height;
            this.Data = new bool[this.Width, this.Height];
            this.Initialize(bitmap);
        }

        public BinaryImage(BinaryImage other)
        {
            this.Width = other.Width;
            this.Height = other.Height;
            this.Data = new bool[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.Data[i, j] = other.Data[i, j];
                }
            }
        }

        public BinaryImage(BlackWhiteImage img, int threshold = Constants.ImageBinaryThreshold)
        {
            this.Width = img.Width;
            this.Height = img.Height;
            this.Data = new bool[this.Width, this.Height];
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    this.Data[i, j] = img.Data[i, j] >= threshold;
                }
            }
        }

        public BinaryImage(RgbImage rgb)
        {
            this.Width = rgb.Width;
            this.Height = rgb.Height;
            this.Data = new bool[this.Width, this.Height];
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    if (rgb.Red[i, j] == Color.White.R && rgb.Green[i, j] == Color.White.G && rgb.Blue[i, j] == Color.White.B)
                    {
                        this.Data[i, j] = true;
                    }
                    else if (rgb.Red[i, j] == Color.Black.R && rgb.Green[i, j] == Color.Black.G && rgb.Blue[i, j] == Color.Black.B)
                    {
                        this.Data[i, j] = false;
                    }
                    else
                    {
                        throw new Exception("Can't cast non black/white color to Binary image pixel");
                    }
                }
            }
        }

        private BinaryImage(byte[] data)
        {
            var mat = new Mat();
            CvInvoke.Imdecode(data, ImreadModes.Color, mat);
            var img = mat.ToImage<Bgr, byte>();
            this.Width = img.Cols;
            this.Height = img.Rows;
            this.Data = new bool[this.Width, this.Height];
            this.Initialize(img);
        }

        public bool[,] Data { get; }

        public static BinaryImage CreateImageFromBase64(string base64Image)
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

            return new BinaryImage(data);
        }

        public static string Or(List<string> images)
        {
            var res = BinaryImage.CreateImageFromBase64(images[0]);

            for (int i = 0; i < images.Count; i++)
            {
                res = res.Or(BinaryImage.CreateImageFromBase64(images[i]));
            }

            return res.ToBase64String();
        }

        public static BinaryImage Or(params BinaryImage[] images)
        {
            var res = new BinaryImage(images[0]);

            for (int i = 1; i < images.Length; i++)
            {
                res = res.Or(images[i]);
            }

            return res;
        }

        public static string Or(params string[] images)
        {
            var res = BinaryImage.CreateImageFromBase64(images[0]);

            for (int i = 1; i < images.Length; i++)
            {
                res = res.Or(BinaryImage.CreateImageFromBase64(images[i]));
            }

            return res.ToBase64String();
        }

        public static string Or(params List<string>[] images)
        {
            BinaryImage res = null;

            foreach (var list in images)
            {
                foreach (var image in list)
                {
                    if (res == null)
                    {
                        res = BinaryImage.CreateImageFromBase64(image);
                    }
                    else
                    {
                        res = res.Or(BinaryImage.CreateImageFromBase64(image));
                    }
                }
            }

            return res.ToBase64String();
        }

        public void Fill(int startx, int starty)
        {
            var q = new Queue<Tuple<int, int>>();
            q.Enqueue(Tuple.Create(startx, starty));

            while (q.Count != 0)
            {
                var top = q.Dequeue();
                int x = top.Item1;
                int y = top.Item2;
                if (this.Data[x, y])
                {
                    continue;
                }

                this.Data[x, y] = true;

                for (int i = 0; i < 8; i += 2)
                {
                    int nx = x + GeometryHelper.DIJ[i, 0];
                    int ny = y + GeometryHelper.DIJ[i, 1];
                    if (this.CheckCoordinates(nx, ny) && !this.Data[nx, ny])
                    {
                        q.Enqueue(Tuple.Create(nx, ny));
                    }
                }
            }
        }

        public BinaryImage Or(BinaryImage img, bool invert = false)
        {
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    if (!invert)
                    {
                        this.Data[i, j] = this.Data[i, j] | img.Data[i, j];
                    }
                    else
                    {
                        this.Data[i, j] = this.Data[i, j] & (!img.Data[i, j]);
                    }
                }
            }

            return this;
        }

        public int And(BinaryImage image)
        {
            int res = 0;
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    if (this.Data[i, j] && image.Data[i, j])
                    {
                        res++;
                    }
                }
            }

            return res;
        }

        public BinaryImage Resize(int w, int h)
        {
            return new BinaryImage(new BlackWhiteImage(this).Resize(w, h), Constants.ImageBinaryThreshold);
        }

        protected override void GetRgb(int i, int j, out int r, out int g, out int b)
        {
            var value = this.Data[i, j] ? Constants.LabelRescalingFactor : 0;
            r = value;
            g = value;
            b = value;
        }

        protected override void SetRgb(int i, int j, int r, int g, int b)
        {
            if (r == 0)
            {
                this.Data[i, j] = false;
            }
            else if (r == Constants.LabelRescalingFactor)
            {
                this.Data[i, j] = true;
            }
            else
            {
                throw new Exception("A value different from 0 and " + Constants.LabelRescalingFactor + " is sent to binary image");
            }
        }

        protected override Color GetPixelColor(int i, int j)
        {
            return this.Data[i, j] ? Color.White : Color.Black;
        }

        protected override void GetOpenCvColor(int i, int j, out byte r, out byte g, out byte b)
        {
            var value = this.Data[i, j] ? Constants.LabelRescalingFactor : 0;
            r = (byte)value;
            g = 0;
            b = 0;
        }

        protected override void SetOpenCvColor(int i, int j, Bgr cv)
        {
            var value = GenericHelper.Round(cv.Red);
            if (value == 0)
            {
                this.Data[i, j] = false;
            }
            else if (value == Constants.LabelRescalingFactor)
            {
                this.Data[i, j] = true;
            }
            else
            {
                throw new ArgumentException("A value different from 0 and " + Constants.LabelRescalingFactor + " is sent to binary image: " + value);
            }
        }

        protected override void SetPixel(int i, int j, Color color)
        {
            if (color.R == Color.White.R && color.G == Color.White.G && color.B == Color.White.B)
            {
                this.Data[i, j] = true;
            }
            else if (color.R == Color.Black.R && color.G == Color.Black.G && color.B == Color.Black.B)
            {
                this.Data[i, j] = false;
            }
            else
            {
                throw new Exception("Only black and white color can be set in the binary image");
            }
        }

        protected override BaseImage Create(int width, int height)
        {
            return new BinaryImage(width, height);
        }
    }
}