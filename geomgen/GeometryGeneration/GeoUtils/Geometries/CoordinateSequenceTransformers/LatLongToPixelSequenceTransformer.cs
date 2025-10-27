// <copyright file="LatLongToPixelSequenceTransformer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils.Geometries.CoordinateSequenceTransformers
{
    using GeoUtils.BingMapsTileSystem;
    using NetTopologySuite.Geometries;

    public class LatLongToPixelSequenceTransformer : ICoordinateSequenceFilter
    {
        private readonly int resolution;

        public LatLongToPixelSequenceTransformer(int resolution)
        {
            this.resolution = resolution;
            this.Done = false;
            this.GeometryChanged = false;
        }

        public bool Done { get; set; }

        public bool GeometryChanged { get; set; }

        public void Filter(CoordinateSequence seq, int i)
        {
            var latlong = new LatLong(seq.GetCoordinate(i));
            BingMapsTileSystemHelper.LatLongToPixelXY(latlong.Latitude, latlong.Longitude, out var x, out var y, this.resolution);
            seq.SetX(i, x);
            seq.SetY(i, y);
        }
    }
}
