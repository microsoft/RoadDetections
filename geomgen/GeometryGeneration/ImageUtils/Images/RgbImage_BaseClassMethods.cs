// <copyright file="RgbImage_BaseClassMethods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.Images
{
    using System.Drawing;

    public partial class RgbImage
    {
        public new RgbImage CutAllPadding()
        {
            return (RgbImage)base.CutAllPadding();
        }

        public new RgbImage CutPadding(int padding)
        {
            return (RgbImage)base.CutPadding(padding);
        }

        public new RgbImage CutPadding(int padding, int paddingRatio)
        {
            return (RgbImage)base.CutPadding(padding, paddingRatio);
        }

        public new RgbImage Cut(int starti, int startj, int width, int height)
        {
            return (RgbImage)base.Cut(starti, startj, width, height);
        }

        public new RgbImage AddBorder(Color color, int padding)
        {
            return (RgbImage)base.AddBorder(color, padding);
        }

        public new RgbImage Subquadkey(string quadkey, string subquadkey, int resPadding = 0)
        {
            return (RgbImage)base.Subquadkey(quadkey, subquadkey, resPadding);
        }

        public new RgbImage AddWithPadding(BaseImage img, int paddingI, int paddingJ)
        {
            return (RgbImage)base.AddWithPadding(img, paddingI, paddingJ);
        }

        public new RgbImage Enlarge(int times)
        {
            return (RgbImage)base.Enlarge(times);
        }

        public new RgbImage Shrink(int times)
        {
            return (RgbImage)base.Shrink(times);
        }
    }
}