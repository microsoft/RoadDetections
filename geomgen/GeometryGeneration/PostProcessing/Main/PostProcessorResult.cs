// <copyright file="PostProcessorResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    using System.Collections.Generic;
    using ImageUtils.Images;
    using PostProcessing.Metrics;
    using PostProcessing.Stitching;

    public class PostProcessorResult
    {
        public List<Line> Lines { get; set; }

        public BinaryImage Image { get; set; }

        public PixelMetric MatchScore { get; set; }

        public RunnerLog RunnerLog { get; set; }
    }
}
