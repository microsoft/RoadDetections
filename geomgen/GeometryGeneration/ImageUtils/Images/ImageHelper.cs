// <copyright file="ImageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.Images
{
    using System;
    using Utils;

    public static class ImageHelper
    {
        public static Tuple<string, string> RescalePrediction(string prediction, string binPrediction, double cft)
        {
            if (GenericHelper.CompareDouble(cft, 1.0) == 0)
            {
                return Tuple.Create(prediction, binPrediction);
            }

            var pred = BlackWhiteImage.CreateImageFromBase64(prediction);
            for (int i = 0; i < pred.Width; i++)
            {
                for (int j = 0; j < pred.Height; j++)
                {
                    pred.Data[i, j] = GenericHelper.Round(pred.Data[i, j] * cft);
                    pred.Data[i, j] = Math.Max(pred.Data[i, j], 0);
                    pred.Data[i, j] = Math.Min(pred.Data[i, j], 255);
                }
            }

            return Tuple.Create(pred.ToBase64String(), new BinaryImage(pred, ImageUtils.Constants.ImageBinaryThreshold).ToBase64String());
        }

        public static double GetRoadDensity(string predImg)
        {
            var bw = BlackWhiteImage.CreateImageFromBase64(predImg);
            double res = 0;
            for (int i = 0; i < bw.Width; i++)
            {
                for (int j = 0; j < bw.Height; j++)
                {
                    res += bw.Data[i, j];
                }
            }

            return res / bw.Width / bw.Height;
        }
    }
}
