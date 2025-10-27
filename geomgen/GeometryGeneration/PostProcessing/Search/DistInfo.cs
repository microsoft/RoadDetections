// <copyright file="DistInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Search
{
    using System;
    using Utils;

    public class DistInfo : IComparable<DistInfo>
    {
        public int Id { get; set; }

        public double Dist { get; set; }

        int IComparable<DistInfo>.CompareTo(DistInfo other)
        {
            var distCmp = GenericHelper.CompareDouble(this.Dist, other.Dist);
            if (distCmp != 0)
            {
                return distCmp;
            }

            return this.Id.CompareTo(other.Id);
        }
    }
}
