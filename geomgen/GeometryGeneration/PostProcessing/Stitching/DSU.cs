// <copyright file="DSU.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Stitching
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DSU
    {
        private readonly Random rand;
        private readonly int[] parent;

        public DSU(int size)
        {
            this.parent = new int[size];
            this.rand = new Random(999);
            for (int i = 0; i < size; i++)
            {
                this.parent[i] = i;
            }
        }

        public List<List<int>> GetComponents()
        {
            var res = new Dictionary<int, List<int>>();
            for (int i = 0; i < this.parent.Length; i++)
            {
                int p = this.GetParent(i);
                if (!res.ContainsKey(p))
                {
                    res.Add(p, new List<int>());
                }

                res[p].Add(i);
            }

            return res.Values.ToList();
        }

        public int GetParent(int a)
        {
            if (this.parent[a] == a)
            {
                return a;
            }

            this.parent[a] = this.GetParent(this.parent[a]);

            return this.parent[a];
        }

        public void Union(int a, int b)
        {
            a = this.GetParent(a);
            b = this.GetParent(b);

            if (a == b)
            {
                return;
            }

            if (this.rand.Next(2) == 0)
            {
                this.parent[a] = b;
            }
            else
            {
                this.parent[b] = a;
            }
        }

        public void PrintStats()
        {
            var cnt = new int[this.parent.Length];
            for (int i = 0; i < this.parent.Length; i++)
            {
                cnt[this.GetParent(i)]++;
            }

            for (int i = 0; i < cnt.Length; i++)
            {
                if (cnt[i] != 0)
                {
                    Console.WriteLine("Component " + cnt[i] + " nodes");
                }
            }
        }
    }
}
