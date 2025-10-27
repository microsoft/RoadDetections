// <copyright file="RandomHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class RandomHelper
    {
        private static readonly Random Rand = new Random(997);

        /// <summary>
        /// Returns a number in range [0, maxValue-1].
        /// </summary>
        public static int GetRandomInt(int maxValue, string seed = null)
        {
            if (maxValue <= 0)
            {
                throw new Exception("Max value must be > 0. maxValue='" + maxValue + "'");
            }

            return RandomHelper.GetRandomizer(seed).Next(maxValue);
        }

        public static double GetRandomDouble(string seed = null)
        {
            return RandomHelper.GetRandomizer(seed).NextDouble();
        }

        public static bool GetRandomBool(string seed = null)
        {
            return RandomHelper.GetRandomizer(seed).Next(2) == 1;
        }

        public static string GetRandomString(int len, string seed = null, bool readable = false)
        {
            var randomizer = RandomHelper.GetRandomizer(seed);

            string res = string.Empty;
            if (readable)
            {
                var vow = "AEIOU";
                var con = "BCDFGHJKLMNPQRSTVWXYZ";
                while (len-- > 0)
                {
                    if (len % 2 == 0)
                    {
                        res += con[randomizer.Next(con.Length)];
                    }
                    else
                    {
                        res += vow[randomizer.Next(vow.Length)];
                    }
                }
            }
            else
            {
                var chars = "abcdefghijklmnopqrstuvwxyz";
                while (len-- > 0)
                {
                    res += chars[randomizer.Next(chars.Length)];
                }
            }

            return res;
        }

        public static List<T> TakeRandom<T>(IEnumerable<T> list, int cnt, string seed = null)
        {
            return RandomHelper.Shuffle(list.ToArray(), RandomHelper.GetRandomizer(seed)).Take(cnt).ToList();
        }

        public static T[] Shuffle<T>(T[] arr, Random randomizer)
        {
            int n = arr.Length;
            while (n > 1)
            {
                n--;
                int k = randomizer.Next(n + 1);
                (arr[n], arr[k]) = (arr[k], arr[n]);
            }

            return arr;
        }

        private static Random GetRandomizer(string seed)
        {
            if (seed == null)
            {
                return RandomHelper.Rand;
            }
            else
            {
                return new Random(seed.GetHashCode());
            }
        }
    }
}
