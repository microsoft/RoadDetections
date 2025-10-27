// <copyright file="ColorStats.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.ImageLevelFeatures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ImageUtils.Images;

    public class ColorStats
    {
        private readonly double gryAvg;
        private readonly double binAvg;
        private readonly double rAvg;
        private readonly double gAvg;
        private readonly double bAvg;
        private readonly double gryErr;
        private readonly double binErr;
        private readonly double rErr;
        private readonly double gErr;
        private readonly double bErr;

        public ColorStats(string serialized)
        {
            var tags = serialized.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            this.gryAvg = double.Parse(tags[0]);
            this.binAvg = double.Parse(tags[1]);
            this.rAvg = double.Parse(tags[2]);
            this.gAvg = double.Parse(tags[3]);
            this.bAvg = double.Parse(tags[4]);
            this.gryErr = double.Parse(tags[5]);
            this.binErr = double.Parse(tags[6]);
            this.rErr = double.Parse(tags[7]);
            this.gErr = double.Parse(tags[8]);
            this.bErr = double.Parse(tags[9]);
        }

        public ColorStats(Dictionary<string, double> dic)
        {
            this.gryAvg = dic["gryAvg"];
            this.binAvg = dic["binAvg"];
            this.rAvg = dic["rAvg"];
            this.gAvg = dic["gAvg"];
            this.bAvg = dic["bAvg"];

            this.gryErr = dic["gryErr"];
            this.binErr = dic["binErr"];
            this.rErr = dic["rErr"];
            this.gErr = dic["gErr"];
            this.bErr = dic["bErr"];
        }

        public ColorStats(List<Tuple<ColorStats, int>> list)
        {
            double allWeight = list.Sum(cs => cs.Item2);

            this.gryAvg = list.Sum(cs => cs.Item1.gryAvg * cs.Item2) / allWeight;
            this.binAvg = list.Sum(cs => cs.Item1.binAvg * cs.Item2) / allWeight;
            this.rAvg = list.Sum(cs => cs.Item1.rAvg * cs.Item2) / allWeight;
            this.gAvg = list.Sum(cs => cs.Item1.gAvg * cs.Item2) / allWeight;
            this.bAvg = list.Sum(cs => cs.Item1.bAvg * cs.Item2) / allWeight;

            this.gryErr = list.Sum(cs => cs.Item1.gryErr * cs.Item2) / allWeight;
            this.binErr = list.Sum(cs => cs.Item1.binErr * cs.Item2) / allWeight;
            this.rErr = list.Sum(cs => cs.Item1.rErr * cs.Item2) / allWeight;
            this.gErr = list.Sum(cs => cs.Item1.gErr * cs.Item2) / allWeight;
            this.bErr = list.Sum(cs => cs.Item1.bErr * cs.Item2) / allWeight;
        }

        public ColorStats(BlackWhiteImage gry, BlackWhiteImage bin, RgbImage rgb, IEnumerable<Tuple<int, int>> pixels)
        {
            var cnt = 0.0;

            this.gryAvg = 0.0;
            this.binAvg = 0.0;
            this.rAvg = 0.0;
            this.gAvg = 0.0;
            this.bAvg = 0.0;

            if (pixels == null)
            {
                for (int i = 0; i < gry.Width; i++)
                {
                    for (int j = 0; j < gry.Height; j++)
                    {
                        this.gryAvg += gry.Data[i, j];
                        this.binAvg += bin.Data[i, j];
                        this.rAvg += rgb.Red[i, j];
                        this.gAvg += rgb.Green[i, j];
                        this.bAvg += rgb.Blue[i, j];
                        cnt++;
                    }
                }
            }
            else
            {
                foreach (var pixel in pixels)
                {
                    int i = pixel.Item1, j = pixel.Item2;
                    this.gryAvg += gry.Data[i, j];
                    this.binAvg += bin.Data[i, j];
                    this.rAvg += rgb.Red[i, j];
                    this.gAvg += rgb.Green[i, j];
                    this.bAvg += rgb.Blue[i, j];
                    cnt++;
                }
            }

            this.gryAvg /= cnt;
            this.binAvg /= cnt;
            this.rAvg /= cnt;
            this.gAvg /= cnt;
            this.bAvg /= cnt;

            this.gryErr = 0.0;
            this.binErr = 0.0;
            this.rErr = 0.0;
            this.gErr = 0.0;
            this.bErr = 0.0;

            if (pixels == null)
            {
                for (int i = 0; i < gry.Width; i++)
                {
                    for (int j = 0; j < gry.Height; j++)
                    {
                        this.gryErr += (this.gryAvg - gry.Data[i, j]) * (this.gryAvg - gry.Data[i, j]);
                        this.binErr += (this.binAvg - bin.Data[i, j]) * (this.binAvg - bin.Data[i, j]);
                        this.rErr += (this.rAvg - rgb.Red[i, j]) * (this.rAvg - rgb.Red[i, j]);
                        this.gErr += (this.gAvg - rgb.Green[i, j]) * (this.gAvg - rgb.Green[i, j]);
                        this.bErr += (this.bAvg - rgb.Blue[i, j]) * (this.bAvg - rgb.Blue[i, j]);
                    }
                }
            }
            else
            {
                foreach (var pixel in pixels)
                {
                    int i = pixel.Item1, j = pixel.Item2;
                    this.gryErr += (this.gryAvg - gry.Data[i, j]) * (this.gryAvg - gry.Data[i, j]);
                    this.binErr += (this.binAvg - bin.Data[i, j]) * (this.binAvg - bin.Data[i, j]);
                    this.rErr += (this.rAvg - rgb.Red[i, j]) * (this.rAvg - rgb.Red[i, j]);
                    this.gErr += (this.gAvg - rgb.Green[i, j]) * (this.gAvg - rgb.Green[i, j]);
                    this.bErr += (this.bAvg - rgb.Blue[i, j]) * (this.bAvg - rgb.Blue[i, j]);
                }
            }

            this.gryErr /= cnt;
            this.binErr /= cnt;
            this.rErr /= cnt;
            this.gErr /= cnt;
            this.bErr /= cnt;
        }

        public string Serialize()
        {
            return string.Join(
                ", ",
                this.gryAvg,
                this.binAvg,
                this.rAvg,
                this.gAvg,
                this.bAvg,
                this.gryErr,
                this.binErr,
                this.rErr,
                this.gErr,
                this.bErr);
        }

        public double GetRGBDiff(ColorStats other)
        {
            return Math.Sqrt(Math.Pow(this.rAvg - other.rAvg, 2)
                            + Math.Pow(this.gAvg - other.gAvg, 2)
                            + Math.Pow(this.bAvg - other.bAvg, 2));
        }

        public double GetBinDiff(ColorStats other)
        {
            return Math.Abs(this.binAvg - other.binAvg);
        }

        public double GetGryDiff(ColorStats other)
        {
            return Math.Abs(this.gryAvg - other.gryAvg);
        }

        public void FillFeatures(string prefix, Dictionary<string, double> features)
        {
            features.Add(prefix + (prefix == string.Empty ? "gryAvg" : "GryAvg"), this.gryAvg);
            features.Add(prefix + (prefix == string.Empty ? "binAvg" : "BinAvg"), this.binAvg);
            features.Add(prefix + (prefix == string.Empty ? "rAvg" : "RAvg"), this.rAvg);
            features.Add(prefix + (prefix == string.Empty ? "gAvg" : "GAvg"), this.gAvg);
            features.Add(prefix + (prefix == string.Empty ? "bAvg" : "BAvg"), this.bAvg);
            features.Add(prefix + (prefix == string.Empty ? "gryErr" : "GryErr"), this.gryErr);
            features.Add(prefix + (prefix == string.Empty ? "binErr" : "BinErr"), this.binErr);
            features.Add(prefix + (prefix == string.Empty ? "rErr" : "RErr"), this.rErr);
            features.Add(prefix + (prefix == string.Empty ? "gErr" : "GErr"), this.gErr);
            features.Add(prefix + (prefix == string.Empty ? "bErr" : "BErr"), this.bErr);
        }
    }
}
