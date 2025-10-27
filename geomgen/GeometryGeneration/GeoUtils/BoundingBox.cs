// <copyright file="BoundingBox.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils.BingMapsTileSystem;
    using NetTopologySuite.Geometries;

    public class BoundingBox
    {
        public BoundingBox(string quadkey)
        {
            BingMapsTileSystemHelper.QuadkeyToTileXY(quadkey, out var tileX, out var tileY);
            var size = BingMapsTileSystemHelper.GetTileSize(quadkey, BingMapsTileSystemHelper.MaxResolution);

            this.TopLeft = BingMapsTileSystemHelper.PixelXYToLatLong(tileX * size, tileY * size, BingMapsTileSystemHelper.MaxResolution);
            this.TopRight = BingMapsTileSystemHelper.PixelXYToLatLong(((tileX + 1) * size) - 1, tileY * size, BingMapsTileSystemHelper.MaxResolution);
            this.BotLeft = BingMapsTileSystemHelper.PixelXYToLatLong(tileX * size, ((tileY + 1) * size) - 1, BingMapsTileSystemHelper.MaxResolution);
            this.BotRight = BingMapsTileSystemHelper.PixelXYToLatLong(((tileX + 1) * size) - 1, ((tileY + 1) * size) - 1, BingMapsTileSystemHelper.MaxResolution);
        }

        public BoundingBox(IEnumerable<string> quadkeys, int resolution)
        {
            var latlongs = new List<LatLong>();
            foreach (var quadkey in quadkeys)
            {
                BingMapsTileSystemHelper.QuadkeyToTileXY(quadkey, out var tileX, out var tileY);
                var size = BingMapsTileSystemHelper.GetTileSize(quadkey, resolution);

                latlongs.Add(BingMapsTileSystemHelper.PixelXYToLatLong(tileX * size, tileY * size, resolution));
                latlongs.Add(BingMapsTileSystemHelper.PixelXYToLatLong(((tileX + 1) * size) - 1, tileY * size, resolution));
                latlongs.Add(BingMapsTileSystemHelper.PixelXYToLatLong(tileX * size, ((tileY + 1) * size) - 1, resolution));
                latlongs.Add(BingMapsTileSystemHelper.PixelXYToLatLong(((tileX + 1) * size) - 1, ((tileY + 1) * size) - 1, resolution));
            }

            var bb = new BoundingBox(latlongs);
            this.TopLeft = bb.TopLeft;
            this.TopRight = bb.TopRight;
            this.BotLeft = bb.BotLeft;
            this.BotRight = bb.BotRight;
        }

        public BoundingBox(LatLong point)
        {
            this.TopLeft = new LatLong(point.Latitude, point.Longitude);
            this.TopRight = new LatLong(point.Latitude, point.Longitude);
            this.BotLeft = new LatLong(point.Latitude, point.Longitude);
            this.BotRight = new LatLong(point.Latitude, point.Longitude);
        }

        public BoundingBox(LatLong ll1, LatLong ll2)
        {
            var minLat = Math.Min(ll1.Latitude, ll2.Latitude);
            var minLon = Math.Min(ll1.Longitude, ll2.Longitude);
            var maxLat = Math.Max(ll1.Latitude, ll2.Latitude);
            var maxLon = Math.Max(ll1.Longitude, ll2.Longitude);

            this.TopLeft = new LatLong(maxLat, minLon);
            this.TopRight = new LatLong(maxLat, maxLon);
            this.BotLeft = new LatLong(minLat, minLon);
            this.BotRight = new LatLong(minLat, maxLon);
        }

        public BoundingBox(List<LatLong> latlongs)
        {
            var minLat = latlongs.Min(ll => ll.Latitude);
            var minLon = latlongs.Min(ll => ll.Longitude);
            var maxLat = latlongs.Max(ll => ll.Latitude);
            var maxLon = latlongs.Max(ll => ll.Longitude);

            this.TopLeft = new LatLong(maxLat, minLon);
            this.TopRight = new LatLong(maxLat, maxLon);
            this.BotLeft = new LatLong(minLat, minLon);
            this.BotRight = new LatLong(minLat, maxLon);
        }

        public LatLong TopLeft { get; set; }

        public LatLong TopRight { get; set; }

        public LatLong BotLeft { get; set; }

        public LatLong BotRight { get; set; }

        public LatLong NorthWest => this.TopLeft;

        public LatLong NorthEast => this.TopRight;

        public LatLong SouthWest => this.BotLeft;

        public LatLong SouthEast => this.BotRight;

        public LatLong North => new LatLong(this.NorthEast, this.NorthWest);

        public LatLong South => new LatLong(this.SouthEast, this.SouthWest);

        public LatLong East => new LatLong(this.NorthEast, this.SouthEast);

        public LatLong West => new LatLong(this.NorthWest, this.SouthWest);

        public double Size => Math.Min(this.Width, this.Height);

        public LatLong Center => new LatLong((this.TopLeft.Latitude + this.BotRight.Latitude) / 2, (this.TopLeft.Longitude + this.BotRight.Longitude) / 2);

        private double Width => GeoHelper.Distance(this.TopLeft, this.TopRight);

        private double Height => GeoHelper.Distance(this.TopLeft, this.BotLeft);

        public BoundingBox AddMargin(double meters)
        {
            this.TopLeft = GeoHelper.Step(this.TopLeft, meters, meters);
            this.TopRight = GeoHelper.Step(this.TopRight, -meters, meters);
            this.BotLeft = GeoHelper.Step(this.BotLeft, meters, -meters);
            this.BotRight = GeoHelper.Step(this.BotRight, -meters, -meters);

            return this;
        }

        public Geometry ToPolygon()
        {
            return GeoHelper.GeometryFactory.CreatePolygon(new Coordinate[]
            {
                this.TopLeft.ToCoordinate(),
                this.TopRight.ToCoordinate(),
                this.BotRight.ToCoordinate(),
                this.BotLeft.ToCoordinate(),
                this.TopLeft.ToCoordinate(),
            });
        }
    }
}
