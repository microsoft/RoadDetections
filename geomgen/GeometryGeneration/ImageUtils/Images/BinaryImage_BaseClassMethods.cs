// <copyright file="BinaryImage_BaseClassMethods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.Images
{
    using System.Drawing;

    public partial class BinaryImage
    {
        public new BinaryImage CutAllPadding()
        {
            return (BinaryImage)base.CutAllPadding();
        }

        public new BinaryImage CutPadding(int padding)
        {
            return (BinaryImage)base.CutPadding(padding);
        }

        public new BinaryImage CutPadding(int padding, int paddingRatio)
        {
            return (BinaryImage)base.CutPadding(padding, paddingRatio);
        }

        public new BinaryImage Cut(int starti, int startj, int width, int height)
        {
            return (BinaryImage)base.Cut(starti, startj, width, height);
        }

        public new BinaryImage AddBorder(Color color, int padding)
        {
            return (BinaryImage)base.AddBorder(color, padding);
        }

        public new BinaryImage Subquadkey(string quadkey, string subquadkey, int resPadding = 0)
        {
            return (BinaryImage)base.Subquadkey(quadkey, subquadkey, resPadding);
        }

        public new BinaryImage AddWithPadding(BaseImage img, int paddingI, int paddingJ)
        {
            return (BinaryImage)base.AddWithPadding(img, paddingI, paddingJ);
        }

        public new BinaryImage Enlarge(int times)
        {
            return (BinaryImage)base.Enlarge(times);
        }

        public new BinaryImage Shrink(int times)
        {
            return (BinaryImage)base.Shrink(times);
        }
    }
}