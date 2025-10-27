// <copyright file="Thinning.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.ImageProcessing
{
    using GeoUtils;
    using ImageUtils.Images;

    public static class Thinning
    {
        /// <summary>
        /// Zhan-Suen algorithm http://agcggs680.pbworks.com/f/Zhan-Suen_algorithm.pdf .
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static BinaryImage Apply(BinaryImage img)
        {
            int thinningPadding = 64;
            var res = new BinaryImage(img.Width + (2 * thinningPadding), img.Height + (2 * thinningPadding)).AddWithPadding(img, thinningPadding, thinningPadding);
            int changedCount1, changedCount2;

            do
            {
                res = Subiteration1(res, out changedCount1);
                res = Subiteration2(res, out changedCount2);
            }
            while (changedCount1 > 0 || changedCount2 > 0);

            return res.CutPadding(thinningPadding);
        }

        private static int CountNeighbors(BinaryImage img, int starti, int startj, params int[] ids)
        {
            int res = 0;
            foreach (int i in ids)
            {
                if (img.CheckCoordinates(starti + GeometryHelper.DIJ[i, 0], startj + GeometryHelper.DIJ[i, 1]) && img.Data[starti + GeometryHelper.DIJ[i, 0], startj + GeometryHelper.DIJ[i, 1]])
                {
                    res++;
                }
            }

            return res;
        }

        private static BinaryImage Subiteration1(BinaryImage img, out int changedCount)
        {
            BinaryImage res = new BinaryImage(img.Width, img.Height);
            changedCount = 0;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    res.Data[i, j] = img.Data[i, j];
                    if (img.Data[i, j])
                    {
                        int ns = CountNeighbors(img, i, j, 0, 1, 2, 3, 4, 5, 6, 7);
                        if (ns >= 2 && ns <= 6)
                        {
                            if (Count01Pattern(img, i, j) == 1 && CountNeighbors(img, i, j, 2, 4, 6) < 3 && CountNeighbors(img, i, j, 0, 2, 4) < 3)
                            {
                                res.Data[i, j] = false;
                                changedCount++;
                            }
                        }
                    }
                }
            }

            return res;
        }

        private static BinaryImage Subiteration2(BinaryImage img, out int changedCount)
        {
            BinaryImage res = new BinaryImage(img.Width, img.Height);
            changedCount = 0;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    res.Data[i, j] = img.Data[i, j];
                    if (img.Data[i, j])
                    {
                        int ns = CountNeighbors(img, i, j, 0, 1, 2, 3, 4, 5, 6, 7);
                        if (ns >= 2 && ns <= 6)
                        {
                            if (Count01Pattern(img, i, j) == 1 && CountNeighbors(img, i, j, 0, 4, 6) < 3 && CountNeighbors(img, i, j, 0, 2, 6) < 3)
                            {
                                res.Data[i, j] = false;
                                changedCount++;
                            }
                        }
                    }
                }
            }

            return res;
        }

        private static int Count01Pattern(BinaryImage img, int starti, int startj)
        {
            int res = 0;
            for (int i = 0; i < 8; i++)
            {
                if (img.CheckCoordinates(starti + GeometryHelper.DIJ[i, 0], startj + GeometryHelper.DIJ[i, 1]) &&
                    !img.Data[starti + GeometryHelper.DIJ[i, 0], startj + GeometryHelper.DIJ[i, 1]] &&
                    img.CheckCoordinates(starti + GeometryHelper.DIJ[(i + 1) % 8, 0], startj + GeometryHelper.DIJ[(i + 1) % 8, 1]) &&
                    img.Data[starti + GeometryHelper.DIJ[(i + 1) % 8, 0], startj + GeometryHelper.DIJ[(i + 1) % 8, 1]])
                {
                    res++;
                }
            }

            return res;
        }
    }
}
