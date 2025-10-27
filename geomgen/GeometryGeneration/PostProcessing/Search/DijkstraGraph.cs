// <copyright file="DijkstraGraph.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Search
{
    using PostProcessing.Main;
    using Utils;

    /// <summary>
    /// Wrapper around a graph that caches and returns shortest path distances between points.
    /// </summary>
    public class DijkstraGraph
    {
        private readonly IPlanarGraph graph;
        private readonly double[][] cached;
        private readonly bool[] isCached;

        public DijkstraGraph(IPlanarGraph graph)
        {
            this.graph = graph;
            this.isCached = new bool[this.graph.NodesCount];
            this.cached = new double[this.graph.NodesCount][];
            for (int i = 0; i < this.graph.NodesCount; i++)
            {
                this.cached[i] = new double[this.graph.NodesCount];
            }
        }

        public static bool IsUnreachableDistance(double dist)
        {
            return GenericHelper.CompareDouble(dist, Constants.InfiniteDistance) >= 0;
        }

        public double GetDistance(int a, int b)
        {
            if (!this.isCached[a])
            {
                this.cached[a] = Dijkstra.Run(this.graph, a).Dist;
                this.isCached[a] = true;
            }

            return this.cached[a][b];
        }
    }
}
