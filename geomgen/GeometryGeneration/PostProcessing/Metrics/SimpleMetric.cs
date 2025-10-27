// <copyright file="SimpleMetric.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    /// <summary>
    /// Simple Metric class that has TP, FP, FN, TN, Precision, Recall, F1.
    /// </summary>
    public class SimpleMetric : PrecisionRecall
    {
        public SimpleMetric()
        {
            this.TruePositive = 0;
            this.FalsePositive = 0;
            this.FalseNegative = 0;
            this.TrueNegative = 0;
        }

        public SimpleMetric(double tp, double fp, double fn, double tn)
        {
            this.TruePositive = tp;
            this.FalsePositive = fp;
            this.FalseNegative = fn;
            this.TrueNegative = tn;
        }

        public double TruePositive { get; set; }

        public double FalsePositive { get; set; }

        public double FalseNegative { get; set; }

        public double TrueNegative { get; set; }

        public override double Precision => (this.TruePositive + this.FalsePositive != 0) ? (1.0 * this.TruePositive / (this.TruePositive + this.FalsePositive)) : double.NaN;

        public override double Recall => (this.TruePositive + this.FalseNegative != 0) ? (1.0 * this.TruePositive / (this.TruePositive + this.FalseNegative)) : double.NaN;

        public double Count => this.TrueNegative + this.TruePositive + this.FalsePositive + this.FalseNegative;

        public double FalseRate => (this.FalsePositive + this.FalseNegative) / this.Count;

        public double PredictRate => (this.TruePositive + this.FalsePositive) / this.Count;

        public double Accuracy => (this.TruePositive + this.TrueNegative) / this.Count;

        public override string ToString()
        {
            return "tp = " + this.TruePositive + " fp = " + this.FalsePositive + " fn = " + this.FalseNegative + " tn = " + this.TrueNegative + " cnt = " + this.Count + " pr = " + string.Format("{0:0.000}", this.Precision) + " rc = " + string.Format("{0:0.000}", this.Recall) + " f1 = " + string.Format("{0:0.000}", this.F1);
        }

        public string GetCFPRF(bool percents = false)
        {
            return "c = " + this.Count + " fr = " + string.Format("{0:0.000}", this.FalseRate * (percents ? 100 : 1)) + " pr = " + string.Format("{0:0.000}", this.Precision * (percents ? 100 : 1)) + " rc = " + string.Format("{0:0.000}", this.Recall * (percents ? 100 : 1)) + " f1 = " + string.Format("{0:0.000}", this.F1 * (percents ? 100 : 1));
        }

        public SimpleMetric Update(bool isPredicted, bool isCorrect, double weight)
        {
            if (isPredicted)
            {
                if (isCorrect)
                {
                    this.TruePositive += weight;
                }
                else
                {
                    this.FalsePositive += weight;
                }
            }
            else
            {
                if (isCorrect)
                {
                    this.FalseNegative += weight;
                }
                else
                {
                    this.TrueNegative += weight;
                }
            }

            return this;
        }
    }
}
