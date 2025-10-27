// <copyright file="Bresenham.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm.
    /// </summary>
    public static class Bresenham
    {
        public static IEnumerable<Tuple<int, int>> GetLinePoints(int x1, int y1, int x2, int y2)
        {
            int d = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));

            //// To make lines consistent change orientation of the line if it is not right
            if (x2 < x1 || (x2 == x1 && y2 < y1))
            {
                for (int iter = d; iter >= 0; iter--)
                {
                    Bresenham.GetXY(x2, y2, x1, y1, d, iter, out int x, out int y);
                    yield return Tuple.Create(x, y);
                }
            }
            else
            {
                for (int iter = 0; iter <= d; iter++)
                {
                    Bresenham.GetXY(x1, y1, x2, y2, d, iter, out int x, out int y);
                    yield return Tuple.Create(x, y);
                }
            }
        }

        private static void GetXY(int x1, int y1, int x2, int y2, int d, int iter, out int x, out int y)
        {
            int sdx = (x2 - x1) < 0 ? -1 : 1;
            int sdy = (y2 - y1) < 0 ? -1 : 1;
            if (d > 10000)
            {
                x = (int)(x1 + (d == 0 ? 0 : (((iter * (long)(x2 - x1)) + (sdx * (d / 2))) / d)));
                y = (int)(y1 + (d == 0 ? 0 : (((iter * (long)(y2 - y1)) + (sdy * (d / 2))) / d)));
            }
            else
            {
                x = x1 + (d == 0 ? 0 : (((iter * (x2 - x1)) + (sdx * (d / 2))) / d));
                y = y1 + (d == 0 ? 0 : (((iter * (y2 - y1)) + (sdy * (d / 2))) / d));
            }
        }
    }
}
