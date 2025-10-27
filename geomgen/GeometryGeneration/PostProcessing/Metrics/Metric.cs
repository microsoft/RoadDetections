// <copyright file="Metric.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using System.Collections.Generic;
    using System.Linq;

    public class Metric : PrecisionRecall
    {
        private readonly double precision;
        private readonly double recall;

        public Metric(double precision, double recall)
        {
            this.precision = precision;
            this.recall = recall;
        }

        public Metric(IEnumerable<PixelExtendedMetric> list)
        {
            this.precision = list.Average(metric => metric.Precision);
            this.recall = list.Average(metric => metric.Recall);
        }

        public Metric(IEnumerable<Metric> list)
        {
            this.precision = list.Average(metric => metric.Precision);
            this.recall = list.Average(metric => metric.Recall);
        }

        public Metric(IEnumerable<TopologyMetric> list)
        {
            var avg = TopologyMetric.Average(list);
            this.precision = avg.Precision;
            this.recall = avg.Recall;
        }

        public Metric(IEnumerable<PixelMetric> list)
        {
            var avg = PixelMetric.Average(list);
            this.precision = avg.Precision;
            this.recall = avg.Recall;
        }

        public override double Precision => this.precision;

        public override double Recall => this.recall;

        public override string ToString()
        {
            return string.Format("{0:N3}", 100.0 * this.Precision) + " " + string.Format("{0:N3}", 100.0 * this.Recall) + " " + string.Format("{0:N3}", 100.0 * this.F1);
        }
    }
}
