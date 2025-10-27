// <copyright file="Extensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Extensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }

        public static void Deconstruct<T>(this T[] arr, out T v0, out T v1)
        {
            v0 = arr[0];
            v1 = arr[1];
        }

        public static void Deconstruct<T>(this T[] arr, out T v0, out T v1, out T v2)
        {
            v0 = arr[0];
            v1 = arr[1];
            v2 = arr[2];
        }

        public static void Deconstruct<T>(this T[] arr, out T v0, out T v1, out T v2, out T v3)
        {
            v0 = arr[0];
            v1 = arr[1];
            v2 = arr[2];
            v3 = arr[3];
        }

        public static void Deconstruct<T>(this T[] arr, out T v0, out T v1, out T v2, out T v3, out T v4)
        {
            v0 = arr[0];
            v1 = arr[1];
            v2 = arr[2];
            v3 = arr[3];
            v4 = arr[4];
        }

        public static T MaxBy<T>(this IEnumerable<T> collection, Func<T, IComparable> getComparable)
        {
            T res = collection.First();
            foreach (var item in collection)
            {
                if (getComparable(item).CompareTo(getComparable(res)) == 1)
                {
                    res = item;
                }
            }

            return res;
        }

        public static T MinBy<T>(this IEnumerable<T> collection, Func<T, IComparable> getComparable)
        {
            T res = collection.First();
            foreach (var item in collection)
            {
                if (getComparable(item).CompareTo(getComparable(res)) == -1)
                {
                    res = item;
                }
            }

            return res;
        }

        public static int IdOfMinBy<T>(this IReadOnlyList<T> list, Func<T, IComparable> getComparable)
        {
            int res = 0;
            var value = getComparable(list[res]);
            for (int i = 1; i < list.Count; i++)
            {
                var curValue = getComparable(list[i]);
                if (curValue.CompareTo(value) == -1)
                {
                    res = i;
                    value = curValue;
                }
            }

            return res;
        }
    }
}