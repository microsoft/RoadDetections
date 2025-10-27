// <copyright file="AnnealingImageScoreHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using Utils;

    /// <summary>
    /// A class that caches redrawings of edges over SmartImage object.
    /// </summary>
    public class AnnealingImageScoreHelper
    {
        public static readonly int MaxScore = 2;
        private static readonly double[] Score = new double[256];

        static AnnealingImageScoreHelper()
        {
            for (int pred = 0; pred < Score.Length; pred++)
            {
                /*var points = new int[][]
                {
                    new[] { 0, -10 },
                    new[] { Constants.ImageBinaryThreshold / 2, -2 },
                    new[] { Constants.ImageBinaryThreshold * 3 / 4, 0 },
                    new[] { Constants.ImageBinaryThreshold, 1 },
                    new[] { 255, 2 }
                };*/

                ////var points = new int[][]
                ////{
                ////    new[] { Constants.ImageBinaryThreshold * 0 / 8, -15 },
                ////    new[] { Constants.ImageBinaryThreshold * 2 / 8, -7 },
                ////    new[] { Constants.ImageBinaryThreshold * 4 / 8, -3 },
                ////    new[] { Constants.ImageBinaryThreshold * 6 / 8, -1 },
                ////    new[] { Constants.ImageBinaryThreshold * 8 / 8, 0 },
                ////    new[] { 255, MaxScore }
                ////};

                ////double ratio = 1.25;
                ////var points = new int[][]
                ////{
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 0 / 8, GenericHelper.Round(-34 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 1 / 8, GenericHelper.Round(-21 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 2 / 8, GenericHelper.Round(-13 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 3 / 8, GenericHelper.Round(-8 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 4 / 8, GenericHelper.Round(-5 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 5 / 8, GenericHelper.Round(-3 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 6 / 8, GenericHelper.Round(-2 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 7 / 8, GenericHelper.Round(-1 * ratio) },
                ////    new[] { ImageUtils.Constants.ImageBinaryThreshold * 8 / 8, GenericHelper.Round(0 * ratio) },
                ////    new[] { 255, MaxScore },
                ////};

                var points = new int[][]
                {
                    new[] { 0, -8 },
                    new[] { ImageUtils.Constants.ImageBinaryThreshold, 0 },
                    new[] { 255, MaxScore },
                };

                for (int i = 0; i + 1 < points.Length; i++)
                {
                    if (GenericHelper.IsBetween(pred, points[i][0], points[i + 1][0]))
                    {
                        Score[pred] = Scale(points[i][0], points[i + 1][0], points[i][1], points[i + 1][1], pred);
                        break;
                    }
                }
            }
        }

        public static double GetScore(int groundTruth, int data)
        {
            return data == 0 ? 0 : Score[groundTruth];
        }

        private static double Scale(double x1, double x2, double y1, double y2, double x)
        {
            return ((x - x1) / (x2 - x1) * (y2 - y1)) + y1;
        }
    }
}
