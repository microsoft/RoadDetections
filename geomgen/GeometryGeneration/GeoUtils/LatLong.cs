// <copyright file="LatLong.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils
{
    using System.Linq;
    using NetTopologySuite.Geometries;
    using Utils;

    public class LatLong
    {
        public LatLong(double lat, double lon)
        {
            this.Latitude = lat;
            this.Longitude = lon;
        }

        public LatLong(params LatLong[] ps)
        {
            this.Latitude = ps.Average(latlon => latlon.Latitude);
            this.Longitude = ps.Average(latlon => latlon.Longitude);
        }

        public LatLong(Coordinate coord)
        {
            this.Latitude = coord.Y;
            this.Longitude = coord.X;
        }

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as LatLong);
        }

        public override int GetHashCode()
        {
            return CombineHashCodes(this.Latitude.GetHashCode(), this.Longitude.GetHashCode());
        }

        public bool Equals(LatLong other)
        {
            return other != null
                   && GenericHelper.CompareDouble(this.Latitude, other.Latitude) == 0
                   && GenericHelper.CompareDouble(this.Longitude, other.Longitude) == 0;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", this.Latitude, this.Longitude);
        }

        public Coordinate ToCoordinate()
        {
            return new Coordinate(this.Longitude, this.Latitude);
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }
    }
}
