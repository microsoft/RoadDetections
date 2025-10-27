// <copyright file="ILineProjector.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Helpers.LineProjecting
{
    using System;
    using System.Collections.Generic;
    using GeoUtils;

    public interface ILineProjector
    {
        IEnumerable<Tuple<int, int>> GetPoints(int x1, int y1, int x2, int y2, int lineWidth);

        IEnumerable<Tuple<int, int>> GetPoints(List<LatLong> road, double? meterWidth, string quadkey, int resolution);
    }
}
