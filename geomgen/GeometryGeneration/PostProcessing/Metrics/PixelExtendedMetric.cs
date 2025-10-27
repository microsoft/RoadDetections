// <copyright file="PixelExtendedMetric.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using ImageUtils.Images;

    public class PixelExtendedMetric : PrecisionRecall
    {
        private readonly int truePositivesCandidate;
        private readonly int positivesCandidate;
        private readonly int truePositivesGroundTruth;
        private readonly int positivesGroundTruth;

        public PixelExtendedMetric(BlackWhiteImage candidate, BlackWhiteImage groundTruth, int relaxedDistance = 3)
        {
            this.positivesCandidate = 0;
            this.truePositivesCandidate = 0;
            this.positivesGroundTruth = 0;
            this.truePositivesGroundTruth = 0;
            for (int i = 0; i < candidate.Width; i++)
            {
                for (int j = 0; j < candidate.Height; j++)
                {
                    if (candidate.Data[i, j] == 1)
                    {
                        this.positivesCandidate++;
                        if (MatchAny(groundTruth, i, j, relaxedDistance))
                        {
                            this.truePositivesCandidate++;
                        }
                    }

                    if (groundTruth.Data[i, j] == 1)
                    {
                        this.positivesGroundTruth++;
                        if (MatchAny(candidate, i, j, relaxedDistance))
                        {
                            this.truePositivesGroundTruth++;
                        }
                    }
                }
            }
        }

        public override double Precision => (this.positivesCandidate != 0) ? (1.0 * this.truePositivesCandidate / this.positivesCandidate) : double.NaN;

        public override double Recall => (this.positivesGroundTruth != 0) ? (1.0 * this.truePositivesGroundTruth / this.positivesGroundTruth) : double.NaN;

        private static bool MatchAny(BlackWhiteImage img, int centeri, int centerj, int dist)
        {
            for (int di = -dist; di <= dist; di++)
            {
                for (int dj = -dist; dj <= dist; dj++)
                {
                    int i = centeri + di;
                    int j = centerj + dj;
                    if (img.CheckCoordinates(i, j) && img.Data[i, j] > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
