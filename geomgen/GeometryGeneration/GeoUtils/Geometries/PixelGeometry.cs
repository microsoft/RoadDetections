// <copyright file="PixelGeometry.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils.Geometries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using GeoUtils.Geometries.CoordinateSequenceTransformers;
    using NetTopologySuite.Geometries;
    using Utils;

    public class PixelGeometry
    {
        private Geometry geometry;

        public PixelGeometry(string input, GeometryRepresentation geometryRepresentation, int resolution)
        {
            this.geometry = GeoHelper.ParseGeometry(input, geometryRepresentation);
            this.Apply(new LatLongToPixelSequenceTransformer(resolution));
        }

        public PixelGeometry(Geometry geometry, int resolution)
        {
            this.geometry = geometry.Copy();
            this.Apply(new LatLongToPixelSequenceTransformer(resolution));
        }

        public PixelGeometry(LatLong latLong, int resolution)
        {
            this.geometry = GeoHelper.GeometryFactory.CreatePoint(latLong.ToCoordinate());
            this.Apply(new LatLongToPixelSequenceTransformer(resolution));
        }

        public PixelGeometry(List<LatLong> latLongs, int resolution)
        {
            this.geometry = GeoHelper.GeometryFactory.CreateLineString(latLongs.Select(latLong => latLong.ToCoordinate()).ToArray());
            this.Apply(new LatLongToPixelSequenceTransformer(resolution));
        }

        public PixelGeometry(BoundingBox boundingBox, int resolution)
        {
            this.geometry = boundingBox.ToPolygon();
            this.Apply(new LatLongToPixelSequenceTransformer(resolution));
        }

        private PixelGeometry(Geometry geometry)
        {
            this.geometry = geometry;
        }

        public string GeometryType => this.geometry.GeometryType;

        public int NumGeometries => this.geometry.NumGeometries;

        public double Area => this.geometry.Area;

        public Coordinate Coordinate => this.geometry.Coordinate;

        public Coordinate[] Coordinates => this.geometry.Coordinates;

        public bool IsEmpty => this.geometry.IsEmpty;

        public PixelGeometry GetGeometryN(int i)
        {
            return new PixelGeometry(this.geometry.GetGeometryN(i));
        }

        public PixelGeometry AddBuffer(double bufferMeters, int resolution)
        {
            var center = BingMapsTileSystemHelper.PixelXYToLatLong(GenericHelper.Round(this.geometry.Centroid.X), GenericHelper.Round(this.geometry.Centroid.Y), resolution);
            var bufferPixels = GeoHelper.MetersToPixelsDouble(bufferMeters, center, resolution);

            try
            {
                this.geometry = this.geometry.Buffer(bufferPixels);
            }
            catch
            {
                this.geometry = this.geometry.Buffer(0);
                this.geometry = this.geometry.Buffer(bufferPixels);
            }

            return this;
        }

        public PixelGeometry Union(PixelGeometry pixelGeometry)
        {
            var res = this.geometry.Union(pixelGeometry.geometry);
            if (res.IsEmpty == false)
            {
                res = res.Buffer(0);
            }

            if (res.NumPoints > 100)
            {
                res = NetTopologySuite.Simplify.DouglasPeuckerSimplifier.Simplify(res, Math.Pow(2.4, Math.Log(res.NumPoints / 100, 2)) * 1e-9);

                if (res.IsEmpty == false)
                {
                    res = res.Buffer(0);
                }
            }

            return new PixelGeometry(res);
        }

        public PixelGeometry Intersection(PixelGeometry pixelGeometry)
        {
            var res = this.geometry.Intersection(pixelGeometry.geometry);
            if (res.IsEmpty == false)
            {
                res = res.Buffer(0);
            }

            return new PixelGeometry(res);
        }

        private void Apply(ICoordinateSequenceFilter coordinateTransformer)
        {
            this.geometry.Apply(coordinateTransformer);
            if (this.geometry.IsValid == false)
            {
                this.geometry = this.geometry.Buffer(0);
            }
        }
    }
}
