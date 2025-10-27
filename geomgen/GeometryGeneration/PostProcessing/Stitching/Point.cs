// <copyright file="Point.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Stitching
{
    using System;

    public class Point : IComparable<Point>, IComparable
    {
        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int CompareTo(Point other)
        {
            if (this.X != other.X)
            {
                return this.X.CompareTo(other.X);
            }

            return this.Y.CompareTo(other.Y);
        }

        public int CompareTo(object obj)
        {
            return this.CompareTo((Point)obj);
        }

        /*public override bool Equals(object obj)
        {
            return this.Equals((Point)obj);
        }*/

        public bool Equals(Point other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        public override string ToString()
        {
            return "(" + this.X + " " + this.Y + ")";
        }
    }
}
