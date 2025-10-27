// <copyright file="GenericHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Utils
{
    using System;
    using System.Collections.Generic;

    public static class GenericHelper
    {
        private const double Epsilon = 1e-8;

        public static int Log2(int a)
        {
            int res = 0;
            while (a > 1)
            {
                res++;
                a >>= 1;
            }

            return res;
        }

        public static int CompareDouble(double v1, double v2)
        {
            if (Math.Abs(v1 - v2) < GenericHelper.Epsilon)
            {
                return 0;
            }

            return v1.CompareTo(v2);
        }

        public static int Sign(double value, int precisionDegree = 1)
        {
            double eps = Math.Pow(GenericHelper.Epsilon, precisionDegree);
            if (value < -eps)
            {
                return -1;
            }

            if (value > eps)
            {
                return 1;
            }

            return 0;
        }

        public static int Round(double value)
        {
            return (int)(value + (Math.Sign(value) * 0.5));
        }

        public static int Floor(double value)
        {
            return (int)(value + GenericHelper.Epsilon);
        }

        public static bool IsBetween(int value, int minValue, int maxValue)
        {
            return minValue <= value && value <= maxValue;
        }

        public static bool IsBetween(double value, double minValue, double maxValue)
        {
            return GenericHelper.CompareDouble(minValue, value) <= 0 && GenericHelper.CompareDouble(value, maxValue) <= 0;
        }

        public static double WeightedAverage(List<double> values, List<double> weights)
        {
            double numer = 0;
            double denom = 0;
            for (int i = 0; i < values.Count; i++)
            {
                numer += values[i] * weights[i];
                denom += weights[i];
            }

            return numer / denom;
        }

        public static string GetMostFrequent(IEnumerable<string> values)
        {
            var cnt = new Dictionary<string, int>();
            foreach (var value in values)
            {
                GenericHelper.AddToDictionary(cnt, value, 1);
            }

            return cnt.MaxBy(kvp => kvp.Value).Key;
        }

        public static void AddToDictionary<TK, TV>(IDictionary<TK, List<TV>> dic, TK key, TV value)
        {
            if (dic.ContainsKey(key))
            {
                dic[key].Add(value);
            }
            else
            {
                dic.Add(key, new List<TV> { value });
            }
        }

        public static void AddToDictionary<TK, TV>(IDictionary<TK, HashSet<TV>> dic, TK key, TV value)
        {
            if (dic.ContainsKey(key))
            {
                dic[key].Add(value);
            }
            else
            {
                dic.Add(key, new HashSet<TV> { value });
            }
        }

        public static void AddToDictionary<TK, TV>(IDictionary<TK, SortedSet<TV>> dic, TK key, TV value)
        {
            if (dic.ContainsKey(key))
            {
                dic[key].Add(value);
            }
            else
            {
                dic.Add(key, new SortedSet<TV> { value });
            }
        }

        public static void AddToDictionary<TK>(IDictionary<TK, double> dic, TK key, double value)
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, 0.0);
            }

            dic[key] += value;
        }

        public static void AddToDictionary<TK>(IDictionary<TK, int> dic, TK key, int value)
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, 0);
            }

            dic[key] += value;
        }

        public static void AddToDictionary<TK>(IDictionary<TK, long> dic, TK key, long value)
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, 0);
            }

            dic[key] += value;
        }

        public static void SetToDictionary<TK, TV>(IDictionary<TK, TV> dic, TK key, TV value)
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, value);
            }

            dic[key] = value;
        }

        public static void MaxToDictionary<TK, TV>(IDictionary<TK, TV> dic, TK key, TV value)
            where TV : IComparable
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, value);
            }

            if (value.CompareTo(dic[key]) == 1)
            {
                dic[key] = value;
            }
        }

        public static void MinToDictionary<TK, TV>(IDictionary<TK, TV> dic, TK key, TV value)
            where TV : IComparable
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, value);
            }

            if (value.CompareTo(dic[key]) == -1)
            {
                dic[key] = value;
            }
        }
    }
}
