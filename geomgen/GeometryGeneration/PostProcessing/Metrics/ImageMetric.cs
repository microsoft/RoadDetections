// <copyright file="ImageMetric.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using System;
    using System.Collections.Generic;
    using ImageUtils.Images;
    using Utils;

    public class ImageMetric
    {
        private readonly int size;
        private readonly int[,] truePositive;
        private readonly int[,] falsePositive;
        private readonly int[,] falseNegative;
        private readonly int[,] trueNegative;

        public ImageMetric(int size)
        {
            this.size = size;
            this.truePositive = new int[this.size, this.size];
            this.falsePositive = new int[this.size, this.size];
            this.falseNegative = new int[this.size, this.size];
            this.trueNegative = new int[this.size, this.size];
        }

        public ImageMetric(BinaryImage candidate, BinaryImage groundTruth)
        {
            if (candidate.Width != groundTruth.Width)
            {
                throw new Exception("Sizes do not match: " + candidate.Width + " and " + groundTruth.Width);
            }

            this.size = candidate.Width;
            this.truePositive = new int[this.size, this.size];
            this.falsePositive = new int[this.size, this.size];
            this.falseNegative = new int[this.size, this.size];
            this.trueNegative = new int[this.size, this.size];
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    if (!candidate.Data[i, j] && !groundTruth.Data[i, j])
                    {
                        this.trueNegative[i, j]++;
                    }

                    if (!candidate.Data[i, j] && groundTruth.Data[i, j])
                    {
                        this.falseNegative[i, j]++;
                    }

                    if (candidate.Data[i, j] && !groundTruth.Data[i, j])
                    {
                        this.falsePositive[i, j]++;
                    }

                    if (candidate.Data[i, j] && groundTruth.Data[i, j])
                    {
                        this.truePositive[i, j]++;
                    }
                }
            }
        }

        public ImageMetric(ImageMetric other)
        {
            this.size = other.size;
            this.truePositive = new int[this.size, this.size];
            this.falsePositive = new int[this.size, this.size];
            this.falseNegative = new int[this.size, this.size];
            this.trueNegative = new int[this.size, this.size];
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    this.trueNegative[i, j] = other.trueNegative[i, j];
                    this.falseNegative[i, j] = other.falseNegative[i, j];
                    this.falsePositive[i, j] = other.falsePositive[i, j];
                    this.truePositive[i, j] = other.truePositive[i, j];
                }
            }
        }

        public int TP => Sum(this.truePositive, this.size);

        public int FP => Sum(this.falsePositive, this.size);

        public int FN => Sum(this.falseNegative, this.size);

        public int TN => Sum(this.trueNegative, this.size);

        public static ImageMetric Average(IEnumerable<ImageMetric> list)
        {
            ImageMetric res = null;
            foreach (var pr in list)
            {
                if (res == null)
                {
                    res = new ImageMetric(pr);
                }
                else
                {
                    for (int i = 0; i < res.size; i++)
                    {
                        for (int j = 0; j < res.size; j++)
                        {
                            res.trueNegative[i, j] += pr.trueNegative[i, j];
                            res.falseNegative[i, j] += pr.falseNegative[i, j];
                            res.falsePositive[i, j] += pr.falsePositive[i, j];
                            res.truePositive[i, j] += pr.truePositive[i, j];
                        }
                    }
                }
            }

            return res;
        }

        public ImageMetric Add(ImageMetric other)
        {
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    this.trueNegative[i, j] += other.trueNegative[i, j];
                    this.falseNegative[i, j] += other.falseNegative[i, j];
                    this.falsePositive[i, j] += other.falsePositive[i, j];
                    this.truePositive[i, j] += other.truePositive[i, j];
                }
            }

            return this;
        }

        public BlackWhiteImage Precision(int d)
        {
            var res = new BlackWhiteImage(this.size, this.size);
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    double precision = 0.0;
                    int cnt = 0;
                    for (int di = -d; di <= d; di++)
                    {
                        for (int dj = -d; dj <= d; dj++)
                        {
                            int ii = i + di;
                            int jj = j + dj;
                            if (!res.CheckCoordinates(ii, jj))
                            {
                                continue;
                            }

                            if (this.truePositive[ii, jj] + this.falsePositive[ii, jj] != 0)
                            {
                                precision += 1.0 * this.truePositive[ii, jj] / (this.truePositive[ii, jj] + this.falsePositive[ii, jj]);
                                cnt++;
                            }
                        }
                    }

                    res.Data[i, j] = GenericHelper.Round(precision / cnt * 255.0);
                }
            }

            double min = res.Data[0, 0], max = res.Data[0, 0];
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    min = Math.Min(min, res.Data[i, j]);
                    max = Math.Max(max, res.Data[i, j]);
                }
            }

            if (min == max)
            {
                return res;
            }

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    res.Data[i, j] = GenericHelper.Round((res.Data[i, j] - min) / (max - min) * 255.0);
                }
            }

            return res;
        }

        public BlackWhiteImage Recall(int d)
        {
            var res = new BlackWhiteImage(this.size, this.size);
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    double recall = 0.0;
                    int cnt = 0;
                    for (int di = -d; di <= d; di++)
                    {
                        for (int dj = -d; dj <= d; dj++)
                        {
                            int ii = i + di;
                            int jj = j + dj;
                            if (!res.CheckCoordinates(ii, jj))
                            {
                                continue;
                            }

                            if (this.truePositive[ii, jj] + this.falseNegative[ii, jj] != 0)
                            {
                                recall += 1.0 * this.truePositive[ii, jj] / (this.truePositive[ii, jj] + this.falseNegative[ii, jj]);
                                cnt++;
                            }
                        }
                    }

                    res.Data[i, j] = GenericHelper.Round(recall / cnt * 255.0);
                }
            }

            double min = res.Data[0, 0], max = res.Data[0, 0];
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    min = Math.Min(min, res.Data[i, j]);
                    max = Math.Max(max, res.Data[i, j]);
                }
            }

            if (min == max)
            {
                return res;
            }

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    res.Data[i, j] = GenericHelper.Round((res.Data[i, j] - min) / (max - min) * 255.0);
                }
            }

            return res;
        }

        public BlackWhiteImage F1(int d)
        {
            var res = new BlackWhiteImage(this.size, this.size);
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    double f1score = 0.0;
                    int cnt = 0;
                    for (int di = -d; di <= d; di++)
                    {
                        for (int dj = -d; dj <= d; dj++)
                        {
                            int ii = i + di;
                            int jj = j + dj;
                            if (!res.CheckCoordinates(ii, jj))
                            {
                                continue;
                            }

                            if ((2 * this.truePositive[ii, jj]) + this.falseNegative[ii, jj] + this.falsePositive[ii, jj] != 0)
                            {
                                f1score += 2.0 * this.truePositive[ii, jj] / ((2 * this.truePositive[ii, jj]) + this.falseNegative[ii, jj] + this.falsePositive[ii, jj]);
                                cnt++;
                            }
                        }
                    }

                    res.Data[i, j] = GenericHelper.Round(f1score / cnt * 255.0);
                }
            }

            double min = res.Data[0, 0], max = res.Data[0, 0];
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    min = Math.Min(min, res.Data[i, j]);
                    max = Math.Max(max, res.Data[i, j]);
                }
            }

            if (min == max)
            {
                return res;
            }

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    res.Data[i, j] = GenericHelper.Round((res.Data[i, j] - min) / (max - min) * 255.0);
                }
            }

            return res;
        }

        public BlackWhiteImage FreqP(int d3)
        {
            var res = new BlackWhiteImage(this.size, this.size);

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    double freq = 0.0;
                    int cnt = 0;
                    for (int di = -d3; di <= d3; di++)
                    {
                        for (int dj = -d3; dj <= d3; dj++)
                        {
                            int ii = i + di;
                            int jj = j + dj;
                            if (!res.CheckCoordinates(ii, jj))
                            {
                                continue;
                            }

                            double d1 = this.truePositive[ii, jj] + this.falsePositive[ii, jj];
                            double d2 = this.truePositive[ii, jj] + this.falseNegative[ii, jj] + this.falsePositive[ii, jj] + this.trueNegative[ii, jj];
                            double d = d1 / d2;

                            freq += d;
                            cnt++;
                        }
                    }

                    res.Data[i, j] = GenericHelper.Round(freq / cnt * 255.0);
                }
            }

            double min = 255, max = 0.0;
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    min = Math.Min(min, res.Data[i, j]);
                    max = Math.Max(max, res.Data[i, j]);
                }
            }

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    res.Data[i, j] = GenericHelper.Round((res.Data[i, j] - min) / (max - min) * 255.0);
                }
            }

            return res;
        }

        public BlackWhiteImage FreqL(int d3)
        {
            var res = new BlackWhiteImage(this.size, this.size);

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    double freq = 0.0;
                    int cnt = 0;
                    for (int di = -d3; di <= d3; di++)
                    {
                        for (int dj = -d3; dj <= d3; dj++)
                        {
                            int ii = i + di;
                            int jj = j + dj;
                            if (!res.CheckCoordinates(ii, jj))
                            {
                                continue;
                            }

                            double d1 = this.truePositive[ii, jj] + this.falseNegative[ii, jj];
                            double d2 = this.truePositive[ii, jj] + this.falseNegative[ii, jj] + this.falsePositive[ii, jj] + this.trueNegative[ii, jj];
                            double d = d1 / d2;

                            freq += d;
                            cnt++;
                        }
                    }

                    res.Data[i, j] = GenericHelper.Round(freq / cnt * 255.0);
                }
            }

            double min = 255, max = 0.0;
            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    min = Math.Min(min, res.Data[i, j]);
                    max = Math.Max(max, res.Data[i, j]);
                }
            }

            for (int i = 0; i < this.size; i++)
            {
                for (int j = 0; j < this.size; j++)
                {
                    res.Data[i, j] = GenericHelper.Round((res.Data[i, j] - min) / (max - min) * 255.0);
                }
            }

            return res;
        }

        private static int Sum(int[,] array, int size)
        {
            int res = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    res += array[i, j];
                }
            }

            return res;
        }
    }
}
