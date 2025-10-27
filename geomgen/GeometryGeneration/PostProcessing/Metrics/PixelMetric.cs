// <copyright file="PixelMetric.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using System.Collections.Generic;
    using ImageUtils.Images;

    public class PixelMetric : PrecisionRecall
    {
        public PixelMetric(string str)
        {
            var tags = str.Split(' ');
            this.TruePositive = int.Parse(tags[2]);
            this.FalsePositive = int.Parse(tags[5]);
            this.FalseNegative = int.Parse(tags[8]);
            this.TrueNegative = int.Parse(tags[11]);
        }

        public PixelMetric(int truePositive, int falsePositive, int falseNegative, int trueNegative)
        {
            this.TruePositive = truePositive;
            this.FalsePositive = falsePositive;
            this.FalseNegative = falseNegative;
            this.TrueNegative = trueNegative;
        }

        public PixelMetric(BinaryImage candidate, BinaryImage groundTruth, int dx, int dy)
        {
            this.TruePositive = 0;
            this.FalsePositive = 0;
            this.FalseNegative = 0;
            this.TrueNegative = 0;
            for (int i = 0; i < groundTruth.Width; i++)
            {
                for (int j = 0; j < groundTruth.Height; j++)
                {
                    if (!candidate.CheckCoordinates(i + dx, j + dy))
                    {
                        continue;
                    }

                    if (!candidate.Data[i + dx, j + dy] && !groundTruth.Data[i, j])
                    {
                        this.TrueNegative++;
                    }

                    if (!candidate.Data[i + dx, j + dy] && groundTruth.Data[i, j])
                    {
                        this.FalseNegative++;
                    }

                    if (candidate.Data[i + dx, j + dy] && !groundTruth.Data[i, j])
                    {
                        this.FalsePositive++;
                    }

                    if (candidate.Data[i + dx, j + dy] && groundTruth.Data[i, j])
                    {
                        this.TruePositive++;
                    }
                }
            }
        }

        public PixelMetric(BinaryImage candidate, BinaryImage groundTruth)
        {
            if (candidate.Width != groundTruth.Width || candidate.Height != groundTruth.Height)
            {
                throw new System.Exception("Images of equal size are expected in PixelMetric constructor. Given: candidate=(" + candidate.Width + "," + candidate.Height + "), groundTruth=(" + groundTruth.Width + "," + groundTruth.Height + ")");
            }

            this.TruePositive = 0;
            this.FalsePositive = 0;
            this.FalseNegative = 0;
            this.TrueNegative = 0;
            for (int i = 0; i < groundTruth.Width; i++)
            {
                for (int j = 0; j < groundTruth.Height; j++)
                {
                    if (!candidate.CheckCoordinates(i, j))
                    {
                        continue;
                    }

                    if (!candidate.Data[i, j] && !groundTruth.Data[i, j])
                    {
                        this.TrueNegative++;
                    }

                    if (!candidate.Data[i, j] && groundTruth.Data[i, j])
                    {
                        this.FalseNegative++;
                    }

                    if (candidate.Data[i, j] && !groundTruth.Data[i, j])
                    {
                        this.FalsePositive++;
                    }

                    if (candidate.Data[i, j] && groundTruth.Data[i, j])
                    {
                        this.TruePositive++;
                    }
                }
            }
        }

        public int TruePositive { get; private set; }

        public int FalsePositive { get; private set; }

        public int FalseNegative { get; private set; }

        public int TrueNegative { get; private set; }

        public override double Precision => (this.TruePositive + this.FalsePositive != 0) ? (1.0 * this.TruePositive / (this.TruePositive + this.FalsePositive)) : double.NaN;

        public override double Recall => (this.TruePositive + this.FalseNegative != 0) ? (1.0 * this.TruePositive / (this.TruePositive + this.FalseNegative)) : double.NaN;

        public override string ToString()
        {
            return "tp = " + this.TruePositive + " fp = " + this.FalsePositive + " fn = " + this.FalseNegative + " tn = " + this.TrueNegative + " pr = " + string.Format("{0:0.000}", this.Precision) + " rc = " + string.Format("{0:0.000}", this.Recall) + " f1 = " + string.Format("{0:0.000}", this.F1);
        }

        internal static PrecisionRecall Average(IEnumerable<PixelMetric> list)
        {
            var res = new PixelMetric(0, 0, 0, 0);
            foreach (var pr in list)
            {
                res.TruePositive += pr.TruePositive;
                res.FalsePositive += pr.FalsePositive;
                res.FalseNegative += pr.FalseNegative;
                res.TrueNegative += pr.TrueNegative;
            }

            return res;
        }
    }
}
