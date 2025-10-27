// <copyright file="ShiftImageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Metrics;
    using PostProcessing.Stitching;

    public static class ShiftImageHelper
    {
        public static Tuple<SimpleMetric, SimpleMetric, int, int> FindBestShift(string predStr, string lblExtStr, int cut = 0)
        {
            var pred = BlackWhiteImage.CreateImageFromBase64(predStr);
            var lbl_ext = BinaryImage.CreateImageFromBase64(lblExtStr);

            if (cut != 0)
            {
                pred = pred.CutPadding(cut);
            }

            int dd = lbl_ext.Width - pred.Width;

            SimpleMetric metric0 = null;
            SimpleMetric res = null;
            int resi = 0, resj = 0;
            Parallel.For(0, dd + 1, starti =>
            {
                for (int startj = 0; startj <= dd; startj++)
                {
                    var metric = new SimpleMetric();
                    for (int i = 0; i < pred.Width; i++)
                    {
                        for (int j = 0; j < pred.Height; j++)
                        {
                            if (lbl_ext.Data[starti + i, startj + j])
                            {
                                metric.TruePositive += pred.Data[i, j] / 255.0;
                            }
                        }
                    }

                    if (starti == dd / 2 && startj == dd / 2)
                    {
                        metric0 = metric;
                    }

                    lock (metric)
                    {
                        if (res == null || metric.TruePositive > res.TruePositive)
                        {
                            res = metric;
                            resi = starti;
                            resj = startj;
                        }
                    }
                }
            });

            return Tuple.Create(metric0, res, resi, resj);
        }

        public static string PerformShift(string label, int shiftX, int shiftY, int shiftPadding)
        {
            var image = BlackWhiteImage.CreateImageFromBase64(label);

            return new BlackWhiteImage(image.Width, image.Height)
                .AddWithPadding(image, -(shiftX - shiftPadding), -(shiftY - shiftPadding))
                .CutPadding(shiftPadding)
                .ToBase64String();
        }

        public static void PerformShift(List<LatLong> latlongs, int shiftX, int shiftY, int shiftPadding, int resolution)
        {
            for (int i = 0; i < latlongs.Count; i++)
            {
                var dx = GeoHelper.PixelsToMeters(shiftX - shiftPadding, latlongs[i], resolution);
                var dy = GeoHelper.PixelsToMeters(shiftY - shiftPadding, latlongs[i], resolution);
                latlongs[i] = GeoHelper.Step(latlongs[i], dx, dy);
            }
        }

        public static void PerformShift(Line line, int shiftX, int shiftY, int shiftPadding)
        {
            line.Add(-(shiftX - shiftPadding), -(shiftY - shiftPadding));
        }
    }
}