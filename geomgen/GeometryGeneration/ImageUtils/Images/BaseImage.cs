// <copyright file="BaseImage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.Images
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using GeoUtils;
    using GeoUtils.BingMapsTileSystem;
    using Utils;

    public abstract class BaseImage
    {
        public int Width
        {
            get; protected set;
        }

        public int Height
        {
            get; protected set;
        }

        /// <summary>
        /// Use Diff(rgb, 4) with threshold 575000 to identify if rgb image changed or not.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="power"></param>
        /// <returns></returns>
        public double Diff(BaseImage other, int power = 2)
        {
            if (this.Width != other.Width || this.Height != other.Height)
            {
                throw new Exception("Images must have same size");
            }

            double res = 0;
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.GetRgb(i, j, out var r1, out var g1, out var b1);
                    other.GetRgb(i, j, out var r2, out var g2, out var b2);
                    var dr = Math.Pow(Math.Abs(r1 - r2), power);
                    var dg = Math.Pow(Math.Abs(g1 - g2), power);
                    var db = Math.Pow(Math.Abs(b1 - b2), power);

                    res += (dr + dg + db) / 3.0;
                }
            }

            return Math.Pow(res, 1.0 / power);
        }

        public BlackWhiteImage DiffImage(BaseImage other)
        {
            if (this.Width != other.Width || this.Height != other.Height)
            {
                throw new Exception("Images must have same size");
            }

            var res = new BlackWhiteImage(this.Width, this.Height);
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.GetRgb(i, j, out var r1, out var g1, out var b1);
                    other.GetRgb(i, j, out var r2, out var g2, out var b2);
                    var dr = Math.Abs(r1 - r2);
                    var dg = Math.Abs(g1 - g2);
                    var db = Math.Abs(b1 - b2);
                    res.Data[i, j] = GenericHelper.Round((dr + dg + db) / 3.0);
                }
            }

            return res;
        }

        public string ToBase64String()
        {
            var mat = MatUtils.CreateImage(this.Height, this.Width);
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.GetOpenCvColor(i, j, out var r, out var g, out var b);
                    Marshal.Copy(new byte[] { b, g, r }, 0, mat.DataPointer + (((j * mat.Cols) + i) * mat.ElementSize), 3);
                }
            }

            var bytes = MatUtils.ImageToBytes(mat, MatUtils.ImageFileFormat.PNG);

            return Convert.ToBase64String(bytes);
        }

        public void SaveAsMat(string path)
        {
            var mat = new Mat(this.Height, this.Width, DepthType.Cv8U, 3);
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.GetOpenCvColor(i, j, out var r, out var g, out var b);
                    Marshal.Copy(new byte[] { b, g, r }, 0, mat.DataPointer + (((j * mat.Cols) + i) * mat.ElementSize), 3);
                }
            }

            mat.Save(path);
        }

        public bool CheckCoordinates(int i, int j)
        {
            return i >= 0 && i < this.Width && j >= 0 && j < this.Height;
        }

        public BlackWhiteImage Extract(Color color, int tolerance)
        {
            var res = new BlackWhiteImage(this.Width, this.Height);

            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    if (this.IsTolerateColorDiff(i, j, color, tolerance))
                    {
                        res.Data[i, j] = 1;
                    }
                }
            }

            return res;
        }

        public void DrawCircle(int x, int y, int r, Color color, bool fill = true)
        {
            if (r < 0)
            {
                return;
            }

            if (r == 0)
            {
                if (this.CheckCoordinates(x, y))
                {
                    this.SetPixel(x, y, color);
                }

                return;
            }

            for (int i = x - r; i <= x + r; i++)
            {
                for (int j = y - r; j <= y + r; j++)
                {
                    if (this.CheckCoordinates(i, j) && GeometryHelper.Distance2(x, y, i, j) <= r * r)
                    {
                        if (fill || GeometryHelper.Distance2(x, y, i, j) >= (r - 1) * (r - 1))
                        {
                            this.SetPixel(i, j, color);
                        }
                    }
                }
            }
        }

        public void DrawLine(int x1, int y1, int x2, int y2, Color color, int width = 1)
        {
            foreach (var (x, y) in Bresenham.GetLinePoints(x1, y1, x2, y2))
            {
                this.DrawCircle(x, y, (width - 1) / 2, color);
            }
        }

        public bool IsBlack()
        {
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    if (!this.IsTolerateColorDiff(i, j, Color.Black, 0))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public BaseImage AddWithPadding(BaseImage img, int paddingI, int paddingJ)
        {
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    if (this.CheckCoordinates(i + paddingI, j + paddingJ))
                    {
                        img.GetRgb(i, j, out var r, out var g, out var b);
                        this.SetRgb(i + paddingI, j + paddingJ, r, g, b);
                    }
                }
            }

            return this;
        }

        public BaseImage CutAllPadding()
        {
            return this.CutPadding((this.Width - (1 << GenericHelper.Log2(this.Width))) / 2);
        }

        public BaseImage CutPadding(int padding)
        {
            return this.Cut(padding, padding, this.Width - (2 * padding), this.Height - (2 * padding));
        }

        public BaseImage CutPadding(int padding, int paddingRatio)
        {
            int cut = padding - (padding / paddingRatio);

            return this.Cut(cut, cut, this.Width - cut - cut, this.Height - cut - cut);
        }

        public BaseImage Cut(int starti, int startj, int width, int height)
        {
            var res = this.Create(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    this.GetRgb(starti + i, startj + j, out var r, out var g, out var b);
                    res.SetRgb(i, j, r, g, b);
                }
            }

            return res;
        }

        public BaseImage FlipHorizontal()
        {
            for (int i = 0; i < this.Width / 2; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.GetRgb(i, j, out var r1, out var g1, out var b1);
                    this.GetRgb(this.Width - i - 1, j, out var r2, out var g2, out var b2);

                    this.SetRgb(i, j, r2, g2, b2);
                    this.SetRgb(this.Width - i - 1, j, r1, g1, b1);
                }
            }

            return this;
        }

        public BaseImage FlipVertical()
        {
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height / 2; j++)
                {
                    this.GetRgb(i, j, out var r1, out var g1, out var b1);
                    this.GetRgb(i, this.Height - j - 1, out var r2, out var g2, out var b2);

                    this.SetRgb(i, j, r2, g2, b2);
                    this.SetRgb(i, this.Height - j - 1, r1, g1, b1);
                }
            }

            return this;
        }

        /// <summary>
        /// For simple roatations 90, 180, 270 degrees.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public BaseImage Rotate(int degrees)
        {
            if (degrees == 0)
            {
                var res = this.Create(this.Width, this.Height);
                for (int i = 0; i < this.Width; i++)
                {
                    for (int j = 0; j < this.Height; j++)
                    {
                        this.GetRgb(i, j, out var r, out var g, out var b);
                        res.SetRgb(i, j, r, g, b);
                    }
                }

                return res;
            }
            else if (degrees == 90)
            {
                var res = this.Create(this.Height, this.Width);
                for (int i = 0; i < this.Width; i++)
                {
                    for (int j = 0; j < this.Height; j++)
                    {
                        this.GetRgb(i, j, out var r, out var g, out var b);
                        res.SetRgb(this.Height - j - 1, i, r, g, b);
                    }
                }

                return res;
            }
            else if (degrees == 180)
            {
                var res = this.Create(this.Width, this.Height);
                for (int i = 0; i < this.Width; i++)
                {
                    for (int j = 0; j < this.Height; j++)
                    {
                        this.GetRgb(i, j, out var r, out var g, out var b);
                        res.SetRgb(this.Width - i - 1, this.Height - j - 1, r, g, b);
                    }
                }

                return res;
            }
            else if (degrees == 270)
            {
                var res = this.Create(this.Height, this.Width);
                for (int i = 0; i < this.Width; i++)
                {
                    for (int j = 0; j < this.Height; j++)
                    {
                        this.GetRgb(i, j, out var r, out var g, out var b);
                        res.SetRgb(j, this.Width - i - 1, r, g, b);
                    }
                }

                return res;
            }
            else
            {
                throw new Exception("Only 90, 180 or 270 degrees are expected in this method, found '" + degrees + "'");
            }
        }

        public BaseImage Rotate(float angle)
        {
            var bm = this.ToBitmap();

            // Make a Matrix to represent rotation
            // by this angle.
            Matrix rotate_at_origin = new Matrix();
            rotate_at_origin.Rotate(angle);

            // Rotate the image's corners to see how big
            // it will be after rotation.
            PointF[] points =
            {
                new PointF(0, 0),
                new PointF(bm.Width, 0),
                new PointF(bm.Width, bm.Height),
                new PointF(0, bm.Height),
            };
            rotate_at_origin.TransformPoints(points);
            var xmin = points.Min(point => point.X);
            var xmax = points.Max(point => point.X);
            var ymin = points.Min(point => point.Y);
            var ymax = points.Max(point => point.Y);

            // Make a bitmap to hold the rotated result.
            int wid = (int)Math.Round(xmax - xmin);
            int hgt = (int)Math.Round(ymax - ymin);
            Bitmap resultBm = new Bitmap(wid, hgt);

            // Create the real rotation transformation.
            Matrix rotate_at_center = new Matrix();
            rotate_at_center.RotateAt(angle, new PointF(wid / 2f, hgt / 2f));

            // Draw the image onto the new bitmap rotated.
            using (Graphics gr = Graphics.FromImage(resultBm))
            {
                // Use smooth image interpolation.
                gr.InterpolationMode = InterpolationMode.High;

                // Clear with the color in the image's upper left corner.
                gr.Clear(bm.GetPixel(0, 0));

                // Set up the transformation to rotate.
                gr.Transform = rotate_at_center;

                // Draw the image centered on the bitmap.
                int x = (wid - bm.Width) / 2;
                int y = (hgt - bm.Height) / 2;
                gr.DrawImage(bm, x, y);
            }

            var res = this.Create(resultBm.Width, resultBm.Height);
            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    res.SetRgb(i, j, resultBm.GetPixel(i, j).R, resultBm.GetPixel(i, j).G, resultBm.GetPixel(i, j).B);
                }
            }

            return res;
        }

        public void Save(string filepath)
        {
            var bitmap = new Bitmap(this.Width, this.Height);
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    bitmap.SetPixel(i, j, this.GetPixelColor(i, j));
                }
            }

            bitmap.Save(filepath, ImageFormat.Png);
        }

        protected void Initialize(Image<Bgr, byte> img)
        {
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.SetOpenCvColor(i, j, img[j, i]);
                }
            }
        }

        protected void Initialize(Bitmap img)
        {
            for (int i = 0; i < this.Width; i++)
            {
                for (int j = 0; j < this.Height; j++)
                {
                    this.SetPixel(i, j, img.GetPixel(i, j));
                }
            }
        }

        protected BaseImage Enlarge(int times)
        {
            var res = this.Create(this.Width * times, this.Height * times);

            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    this.GetRgb(i / times, j / times, out var r, out var g, out var b);
                    res.SetRgb(i, j, r, g, b);
                }
            }

            return res;
        }

        protected BaseImage Shrink(int times)
        {
            var res = this.Create(this.Width / times, this.Height / times);

            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    int rr = 0, gg = 0, bb = 0;
                    for (int ii = 0; ii < times; ii++)
                    {
                        for (int jj = 0; jj < times; jj++)
                        {
                            this.GetRgb((i * times) + ii, (j * times) + jj, out var r, out var g, out var b);
                            rr += r;
                            gg += g;
                            bb += b;
                        }
                    }

                    res.SetRgb(i, j, GenericHelper.Round(rr * 1.0 / times / times), GenericHelper.Round(gg * 1.0 / times / times), GenericHelper.Round(bb * 1.0 / times / times));
                }
            }

            return res;
        }

        protected BaseImage AddBorder(Color color, int padding)
        {
            for (int j = 0; j < this.Height; j++)
            {
                foreach (var (ii, jj) in new[]
                {
                    Tuple.Create(padding - 1, j),
                    Tuple.Create(padding, j),
                    Tuple.Create(this.Width - 1 - padding, j),
                    Tuple.Create(this.Width - padding, j),
                })
                {
                    if (this.CheckCoordinates(ii, jj))
                    {
                        this.SetPixel(ii, jj, color);
                    }
                }
            }

            for (int i = 0; i < this.Width; i++)
            {
                foreach (var (ii, jj) in new[]
                {
                    Tuple.Create(i, padding - 1),
                    Tuple.Create(i, padding),
                    Tuple.Create(i, this.Height - 1 - padding),
                    Tuple.Create(i, this.Height - padding),
                })
                {
                    if (this.CheckCoordinates(ii, jj))
                    {
                        this.SetPixel(ii, jj, color);
                    }
                }
            }

            return this;
        }

        protected BaseImage Subquadkey(string quadkey, string subquadkey, int resPadding)
        {
            int n = 1 << GenericHelper.Log2(this.Width);
            int sn = n >> (subquadkey.Length - quadkey.Length);
            int padding = (this.Width - n) / 2;
            QuadkeyOrientationHelper.GetSubquadkeyCoordinates(quadkey, subquadkey, out var starti, out var startj, n);
            starti += padding;
            startj += padding;

            starti -= resPadding;
            startj -= resPadding;

            var res = this.Create(sn + (2 * resPadding), sn + (2 * resPadding));
            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    this.GetRgb(starti + i, startj + j, out var r, out var g, out var b);
                    res.SetRgb(i, j, r, g, b);
                }
            }

            return res;
        }

        protected bool IsTolerateColorDiff(int i, int j, Color color, int tolerance)
        {
            this.GetRgb(i, j, out var r, out var g, out var b);
            return GeometryHelper.Distance(r, g, b, color.R, color.G, color.B) <= tolerance;
        }

        protected Bitmap ToBitmap()
        {
            byte[] byteBuffer = Convert.FromBase64String(this.ToBase64String());
            using (MemoryStream memoryStream = new MemoryStream(byteBuffer))
            {
                memoryStream.Position = 0;
                return (Bitmap)Image.FromStream(memoryStream);
            }
        }

        protected abstract void SetPixel(int i, int j, Color color);

        protected abstract void GetRgb(int i, int j, out int r, out int g, out int b);

        protected abstract void SetRgb(int i, int j, int r, int g, int b);

        protected abstract Color GetPixelColor(int i, int j);

        protected abstract void GetOpenCvColor(int i, int j, out byte r, out byte g, out byte b);

        protected abstract void SetOpenCvColor(int i, int j, Bgr cv);

        protected abstract BaseImage Create(int width, int height);
    }
}
