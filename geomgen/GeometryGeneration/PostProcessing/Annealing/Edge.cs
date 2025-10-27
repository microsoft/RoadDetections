// <copyright file="Edge.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using GeoUtils;

    public class Edge
    {
        public Edge()
        {
        }

        public Edge(string data)
        {
            var tags = data.Split(' ');
            this.X1 = int.Parse(tags[0]);
            this.Y1 = int.Parse(tags[1]);
            this.X2 = int.Parse(tags[2]);
            this.Y2 = int.Parse(tags[3]);
            this.Width = int.Parse(tags[4]);
            this.IsRevertable = bool.Parse(tags[5]);
        }

        public Edge(Edge other)
        {
            this.X1 = other.X1;
            this.Y1 = other.Y1;
            this.X2 = other.X2;
            this.Y2 = other.Y2;
            this.Width = other.Width;
            this.IsRevertable = other.IsRevertable;
        }

        public int X1 { get; set; }

        public int Y1 { get; set; }

        public int X2 { get; set; }

        public int Y2 { get; set; }

        public int Width { get; set; }

        public bool IsRevertable { get; set; }

        public Tuple<int, int>[] Points => new Tuple<int, int>[] { Tuple.Create(this.X1, this.Y1), Tuple.Create(this.X2, this.Y2) };

        public bool IsSame(Edge other)
        {
            return (this.X1 == other.X1 && this.Y1 == other.Y1 && this.X2 == other.X2 && this.Y2 == other.Y2) || (this.X2 == other.X1 && this.Y2 == other.Y1 && this.X1 == other.X2 && this.Y1 == other.Y2);
        }

        public override string ToString()
        {
            return this.X1 + " " + this.Y1 + " " + this.X2 + " " + this.Y2 + " " + this.Width + " " + this.IsRevertable + " " + this.GetLength();
        }

        public double GetLength()
        {
            return GeometryHelper.Distance(this.X1, this.Y1, this.X2, this.Y2);
        }

        public bool IsSelfLoop()
        {
            return this.X1 == this.X2 && this.Y1 == this.Y2;
        }

        public Edge MoveBy(int x, int y)
        {
            this.X1 += x;
            this.X2 += x;
            this.Y1 += y;
            this.Y2 += y;

            return this;
        }

        internal Edge MoveNode(int xfrom, int yfrom, int xto, int yto)
        {
            if (this.X1 == xfrom && this.Y1 == yfrom)
            {
                this.X1 = xto;
                this.Y1 = yto;
            }

            if (this.X2 == xfrom && this.Y2 == yfrom)
            {
                this.X2 = xto;
                this.Y2 = yto;
            }

            return this;
        }
    }
}
