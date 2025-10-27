// <copyright file="ColorSpaceUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers
{
    using System;
    using ImageUtils.Images;
    using Utils;

    public static class ColorSpaceUtils
    {
        public static double GetLuminance(int r, int g, int b)
        {
            return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
        }

        ////////HSV or HSB////////
        public static double GetBrightness(int r, int g, int b)
        {
            return Math.Max(Math.Max(r, g), b);
        }

        public static double GetSaturationHSV(int ri, int gi, int bi)
        {
            double r = ri / 255.0;
            double g = gi / 255.0;
            double b = bi / 255.0;
            double mn = Math.Min(Math.Min(r, g), b);
            double mx = Math.Max(Math.Max(r, g), b);

            if (GenericHelper.CompareDouble(mx, 0.0) == 0)
            {
                return 0;
            }
            else
            {
                return (mx - mn) / mx * 255;
            }
        }

        ////////HSL or HSI////////
        public static double GetIntensity(int r, int g, int b)
        {
            int mn = Math.Min(Math.Min(r, g), b);
            int mx = Math.Max(Math.Max(r, g), b);
            return (mx + mn) / 2.0;
        }

        public static double GetSaturationHSL(int ri, int gi, int bi)
        {
            double r = ri / 255.0;
            double g = gi / 255.0;
            double b = bi / 255.0;
            double mn = Math.Min(Math.Min(r, g), b);
            double mx = Math.Max(Math.Max(r, g), b);
            double delta = mx - mn;

            if (GenericHelper.CompareDouble(delta, 0.0) == 0)
            {
                return 0;
            }
            else
            {
                return (mx - mn) / (1.0 - Math.Abs(mx + mn - 1)) * 255;
            }
        }

        public static string RgbToLuminance(string rgb)
        {
            return ConvertRgb(rgb, GetLuminance);
        }

        public static string RgbToBrightness(string rgb)
        {
            return ConvertRgb(rgb, GetBrightness);
        }

        public static string RgbToSaturationHSV(string rgb)
        {
            return ConvertRgb(rgb, GetSaturationHSV);
        }

        public static string RgbToIntensity(string rgb)
        {
            return ConvertRgb(rgb, GetIntensity);
        }

        public static string RgbToSaturationHSL(string rgb)
        {
            return ConvertRgb(rgb, GetSaturationHSL);
        }

        private static string ConvertRgb(string rgb, Func<int, int, int, double> func)
        {
            var rgbImage = RgbImage.CreateImageFromBase64(rgb);
            var res = new BlackWhiteImage(rgbImage.Width, rgbImage.Height);
            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    var lum = func(rgbImage.Red[i, j], rgbImage.Green[i, j], rgbImage.Blue[i, j]);
                    res.Data[i, j] = Math.Max(0, Math.Min(255, GenericHelper.Round(lum)));
                }
            }

            return res.ToBase64String();
        }
    }
}
