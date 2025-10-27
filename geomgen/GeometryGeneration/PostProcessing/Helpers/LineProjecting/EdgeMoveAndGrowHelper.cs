// <copyright file="EdgeMoveAndGrowHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers.LineProjecting
{
    using System;
    using System.Collections.Generic;

    public static class EdgeMoveAndGrowHelper
    {
        private const int MaxEdge = 100;
        private static readonly List<Tuple<int, int, bool>>[,,,,,] Cache = new List<Tuple<int, int, bool>>[2 * MaxEdge, 2 * MaxEdge, 3, 3, 20, 3];

        private static readonly int SIZE = (2 * MaxEdge) + 50;
        private static readonly int[,] Grid1 = new int[SIZE, SIZE];
        private static readonly int[,] Grid2 = new int[SIZE, SIZE];
        private static int gridValue = 0;
        private static readonly LocalLineProjector p = new LocalLineProjector(SIZE, SIZE);

        public static List<Tuple<int, int, bool>> GetPoints(int x0, int y0, int xfrom, int yfrom, int xto, int yto, int width, int dw, out bool invY)
        {
            int ex = xfrom - x0;
            int ey = yfrom - y0;
            int dx = xto - xfrom;
            int dy = yto - yfrom;
            int d = Math.Sign(dw) + 1;

            if (ey < 0)
            {
                ey = -ey;
                dy = -dy;
                invY = true;
            }
            else
            {
                invY = false;
            }

            if (ex > -MaxEdge && ex < MaxEdge && ey > -MaxEdge && ey < MaxEdge && width < 20)
            {
                if (Cache[ex + MaxEdge, ey + MaxEdge, dx + 1, dy + 1, width, d] == null)
                {
                    Cache[ex + MaxEdge, ey + MaxEdge, dx + 1, dy + 1, width, d] = GeneratePoints(ex, ey, dx, dy, width, dw);
                }

                return Cache[ex + MaxEdge, ey + MaxEdge, dx + 1, dy + 1, width, d];
            }

            return null;
        }

        private static List<Tuple<int, int, bool>> GeneratePoints(int ex, int ey, int dx, int dy, int width, int dw)
        {
            gridValue++;
            int startx = SIZE / 2, starty = SIZE / 2;

            var p1 = p.GetPoints(startx, starty, startx + ex, starty + ey, width);
            var p2 = p.GetPoints(startx, starty, startx + ex + dx, starty + ey + dy, width + dw);

            foreach (var (x, y) in p1)
            {
                Grid1[x, y] = gridValue;
            }

            foreach (var (x, y) in p2)
            {
                Grid2[x, y] = gridValue;
            }

            var res = new List<Tuple<int, int, bool>>();
            foreach (var (x, y) in p1)
            {
                if (Grid1[x, y] == gridValue && Grid2[x, y] != gridValue)
                {
                    res.Add(Tuple.Create(x - startx, y - starty, false));
                }
            }

            foreach (var (x, y) in p2)
            {
                if (Grid1[x, y] != gridValue && Grid2[x, y] == gridValue)
                {
                    res.Add(Tuple.Create(x - startx, y - starty, true));
                }
            }

            return res;
        }
    }
}
