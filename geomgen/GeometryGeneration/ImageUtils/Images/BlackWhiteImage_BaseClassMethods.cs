// <copyright file="BlackWhiteImage_BaseClassMethods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils.Images
{
    using System.Drawing;

    public partial class BlackWhiteImage
    {
        public new BlackWhiteImage CutAllPadding()
        {
            return (BlackWhiteImage)base.CutAllPadding();
        }

        public new BlackWhiteImage CutPadding(int padding)
        {
            return (BlackWhiteImage)base.CutPadding(padding);
        }

        public new BlackWhiteImage CutPadding(int padding, int paddingRatio)
        {
            return (BlackWhiteImage)base.CutPadding(padding, paddingRatio);
        }

        public new BlackWhiteImage Cut(int starti, int startj, int width, int height)
        {
            return (BlackWhiteImage)base.Cut(starti, startj, width, height);
        }

        public new BlackWhiteImage AddBorder(Color color, int padding)
        {
            return (BlackWhiteImage)base.AddBorder(color, padding);
        }

        public new BlackWhiteImage Subquadkey(string quadkey, string subquadkey, int resPadding = 0)
        {
            return (BlackWhiteImage)base.Subquadkey(quadkey, subquadkey, resPadding);
        }

        public new BlackWhiteImage AddWithPadding(BaseImage img, int paddingI, int paddingJ)
        {
            return (BlackWhiteImage)base.AddWithPadding(img, paddingI, paddingJ);
        }

        public new BlackWhiteImage Enlarge(int times)
        {
            return (BlackWhiteImage)base.Enlarge(times);
        }

        public new BlackWhiteImage Shrink(int times)
        {
            return (BlackWhiteImage)base.Shrink(times);
        }
    }
}