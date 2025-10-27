// <copyright file="GlobalLineProjector.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers.LineProjecting
{
    using System;
    using System.Collections.Generic;
    using GeoUtils;
    using ImageUtils;

    public class GlobalLineProjector : ILineProjector
    {
        public GlobalLineProjector()
        {
        }

        public IEnumerable<Tuple<int, int>> GetPoints(List<LatLong> road, double? meterWidth, string quadkey, int resolution)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tuple<int, int>> GetPoints(int x1, int y1, int x2, int y2, int lineWidth)
        {
            var res = new HashSet<Tuple<int, int>>();
            int r = (lineWidth - 1) / 2;
            int prevx = 0, prevy = 0;
            bool first = true;
            foreach (var (x, y) in Bresenham.GetLinePoints(x1, y1, x2, y2))
            {
                if (first)
                {
                    first = false;
                    var len = LineProjectorHelper.CirclePointsLength[r];
                    for (int i = 0; i < len; i += 2)
                    {
                        int xcur = x + LineProjectorHelper.CirclePoints[r][i];
                        int ycur = y + LineProjectorHelper.CirclePoints[r][i + 1];
                        res.Add(Tuple.Create(xcur, ycur));
                    }
                }
                else
                {
                    int di = x - prevx + 1, dj = y - prevy + 1;
                    int len = LineProjectorHelper.Length[r, di, dj];
                    for (int i = 0; i < len; i++)
                    {
                        int xcur = x + LineProjectorHelper.DX[r, di, dj][i];
                        int ycur = y + LineProjectorHelper.DY[r, di, dj][i];
                        res.Add(Tuple.Create(xcur, ycur));
                    }
                }

                prevx = x;
                prevy = y;
            }

            return res;
        }
    }
}
