// <copyright file="PathsSearchResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Search
{
    public class PathsSearchResult
    {
        public PathsSearchResult(int size)
        {
            this.Dist = new double[size];
            this.From = new int[size];
        }

        public double[] Dist { get; private set; }

        public int[] From { get; private set; }
    }
}
