// <copyright file="Constants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    using System.Drawing.Imaging;

    public static class Constants
    {
        public const double InfiniteDistance = 1e9;

        public const int MinEdgeWidth = 7; ////-1;

        public const int EdgeAddWidth = 2;

        public const int MaxEdgeWidth = 100;

        public const int StitchingTileSize = 256;

        public const int AnnealingMaxAmplitude = 1;

        public const int LabelRescalingFactor = 254;

        public const PixelFormat LabelPixelFormat = PixelFormat.Format8bppIndexed;

        public const bool Local = false;

        public const bool DebugAnnealling = false;
    }
}
