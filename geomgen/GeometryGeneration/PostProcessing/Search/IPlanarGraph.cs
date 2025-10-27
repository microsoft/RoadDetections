// <copyright file="IPlanarGraph.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Search
{
    using System.Collections.Generic;

    public interface IPlanarGraph
    {
        int NodesCount { get; }

        int ConnectionsCount { get; }

        IEnumerable<int> GetConnections(int node);

        int GetConnectionsCount(int node);

        void GetCoordinates(int id, out int x, out int y);
    }
}
