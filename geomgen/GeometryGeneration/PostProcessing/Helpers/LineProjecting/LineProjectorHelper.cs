// <copyright file="LineProjectorHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers.LineProjecting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using PostProcessing.Main;

    /// <summary>
    /// class that caches redrawings of edges.
    /// </summary>
    public class LineProjectorHelper
    {
        public static readonly int MaxRadius = Constants.MaxEdgeWidth;
        ////(Constants.MaxEdgeWidth - 1) / 2;

        public static readonly int[,,][] DX = new int[MaxRadius + 1, 3, 3][];
        public static readonly int[,,][] DY = new int[MaxRadius + 1, 3, 3][];
        public static readonly int[,,] Length = new int[MaxRadius + 1, 3, 3];

        public static readonly int[][] CirclePoints = new int[MaxRadius + 1][];
        public static readonly int[] CirclePointsLength = new int[MaxRadius + 1];

        static LineProjectorHelper()
        {
            for (int r = 0; r < MaxRadius + 1; r++)
            {
                CirclePoints[r] = new int[2 * (r + r + 1) * (r + r + 1)];
                for (int di = -r; di <= r; di++)
                {
                    for (int dj = -r; dj <= r; dj++)
                    {
                        if ((di * di) + (dj * dj) <= r * r)
                        {
                            CirclePoints[r][CirclePointsLength[r]++] = di;
                            CirclePoints[r][CirclePointsLength[r]++] = dj;
                        }
                    }
                }
            }

            for (int r = 0; r < MaxRadius + 1; r++)
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var points = new HashSet<Tuple<int, int>>();
                        var len = LineProjectorHelper.CirclePointsLength[r];
                        for (int ir = 0; ir < len; ir += 2)
                        {
                            int di = LineProjectorHelper.CirclePoints[r][ir];
                            int dj = LineProjectorHelper.CirclePoints[r][ir + 1];
                            points.Add(Tuple.Create(-(i - 1) + di, -(j - 1) + dj));
                        }

                        var list = new List<Tuple<int, int>>();
                        for (int ir = 0; ir < len; ir += 2)
                        {
                            int di = LineProjectorHelper.CirclePoints[r][ir];
                            int dj = LineProjectorHelper.CirclePoints[r][ir + 1];
                            var point = Tuple.Create(di, dj);
                            if (!points.Contains(point))
                            {
                                list.Add(point);
                            }
                        }

                        list = list.OrderByDescending(tuple => GeometryHelper.SignedDistanceToLine(tuple.Item1, tuple.Item2, -(i - 1), -(j - 1), 0, 0)).ToList();

                        DX[r, i, j] = new int[list.Count];
                        DY[r, i, j] = new int[list.Count];
                        Length[r, i, j] = 0;
                        for (int p = 0; p < list.Count; p++)
                        {
                            DX[r, i, j][Length[r, i, j]] = list[p].Item1;
                            DY[r, i, j][Length[r, i, j]] = list[p].Item2;
                            Length[r, i, j]++;
                        }
                    }
                }
            }
        }
    }
}
