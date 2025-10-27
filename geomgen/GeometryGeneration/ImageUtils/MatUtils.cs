// <copyright file="MatUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    public class MatUtils
    {
        public enum ImageFileFormat
        {
            PNG,
            JPEG,
        }

        public static byte[] ImageToBytes(Mat image, ImageFileFormat fileFormat)
        {
            VectorOfByte buffer = new VectorOfByte();
            CvInvoke.Imencode(fileFormat == ImageFileFormat.PNG ? ".png" : ".jpg", image, buffer);
            return buffer.ToArray();
        }

        public static Mat ImageFromBytes(byte[] data)
        {
            Mat image = new Mat();
            CvInvoke.Imdecode(data, ImreadModes.Color, image);
            return image;
        }

        public static byte[] Resize(Mat image, int width, int height)
        {
            var bitmap = new Bitmap(image.ToBitmap(), new Size(width, height));
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                return memoryStream.GetBuffer();
            }
        }

        public static Mat CreateImage(int rows, int cols, int channels = 3, DepthType type = DepthType.Cv8U, double value = 0.0)
        {
            Mat image = new Mat(rows, cols, type, channels);
            image.SetTo(new MCvScalar(value));
            return image;
        }
    }
}