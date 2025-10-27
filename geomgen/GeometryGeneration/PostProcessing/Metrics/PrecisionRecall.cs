// <copyright file="PrecisionRecall.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using Utils;

    public abstract class PrecisionRecall
    {
        public abstract double Precision { get; }

        public abstract double Recall { get; }

        public double F1
        {
            get
            {
                if (double.IsNaN(this.Precision) || double.IsNaN(this.Recall))
                {
                    return double.NaN;
                }

                if (GenericHelper.CompareDouble(this.Precision + this.Recall, 0) == 0)
                {
                    return double.NaN;
                }

                return 2.0 * this.Precision * this.Recall / (this.Precision + this.Recall);
            }
        }

        public override string ToString()
        {
            return string.Format("{0:N3}", 100.0 * this.Precision) + " " + string.Format("{0:N3}", 100.0 * this.Recall) + " " + string.Format("{0:N3}", 100.0 * this.F1);
        }
    }
}
