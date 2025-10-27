// <copyright file="PixelToLatLongSequenceTransformer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils.Geometries.CoordinateSequenceTransformers
{
    using GeoUtils.BingMapsTileSystem;
    using NetTopologySuite.Geometries;
    using Utils;

    public class PixelToLatLongSequenceTransformer : ICoordinateSequenceFilter
    {
        private readonly int resolution;

        public PixelToLatLongSequenceTransformer(int resolution)
        {
            this.resolution = resolution;
            this.Done = false;
            this.GeometryChanged = false;
        }

        public bool Done { get; set; }

        public bool GeometryChanged { get; set; }

        public void Filter(CoordinateSequence seq, int i)
        {
            var crd = seq.GetCoordinate(i);
            var coord = BingMapsTileSystemHelper.PixelXYToLatLong(GenericHelper.Round(crd.X), GenericHelper.Round(crd.Y), this.resolution).ToCoordinate();
            seq.SetX(i, coord.X);
            seq.SetY(i, coord.Y);
        }
    }
}
