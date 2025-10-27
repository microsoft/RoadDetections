// <copyright file="QuadkeyOrientationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GeoUtils.BingMapsTileSystem
{
    using System;
    using System.Collections.Generic;

    public static class QuadkeyOrientationHelper
    {
        public enum Orientation
        {
            Top,
            Bot,
            Right,
            Left,
        }

        public static KeyValuePair<string, Tuple<int, int>>[] GetNeighbouringQuadkeys(string quadkey)
        {
            return new KeyValuePair<string, Tuple<int, int>>[]
            {
                new KeyValuePair<string, Tuple<int, int>>(quadkey, Tuple.Create(0, 0)),
                new KeyValuePair<string, Tuple<int, int>>(Top(quadkey), Tuple.Create(0, -1)),
                new KeyValuePair<string, Tuple<int, int>>(Right(Top(quadkey)), Tuple.Create(1, -1)),
                new KeyValuePair<string, Tuple<int, int>>(Right(quadkey), Tuple.Create(1, 0)),
                new KeyValuePair<string, Tuple<int, int>>(Right(Bot(quadkey)), Tuple.Create(1, 1)),
                new KeyValuePair<string, Tuple<int, int>>(Bot(quadkey), Tuple.Create(0, 1)),
                new KeyValuePair<string, Tuple<int, int>>(Left(Bot(quadkey)), Tuple.Create(-1, 1)),
                new KeyValuePair<string, Tuple<int, int>>(Left(quadkey), Tuple.Create(-1, 0)),
                new KeyValuePair<string, Tuple<int, int>>(Left(Top(quadkey)), Tuple.Create(-1, -1)),
            };
        }

        public static bool AreNeighbors(string quadkey1, string quadkey2)
        {
            if (quadkey1.Length != quadkey2.Length)
            {
                throw new Exception("To check if quadkeys are neighbors, please provide same-level quadkeys");
            }

            return quadkey1 == Top(quadkey2) ||
                quadkey1 == TopRight(quadkey2) ||
                quadkey1 == Right(quadkey2) ||
                quadkey1 == BotRight(quadkey2) ||
                quadkey1 == Bot(quadkey2) ||
                quadkey1 == BotLeft(quadkey2) ||
                quadkey1 == Left(quadkey2) ||
                quadkey1 == TopLeft(quadkey2);
        }

        public static string GetRelativeQuadkey(string quadkey, int i, int j, int n)
        {
            string res = quadkey;
            while (n > 1)
            {
                if (i < n / 2 && j < n / 2)
                {
                    res += "0";
                }
                else if (i < n / 2 && j >= n / 2)
                {
                    res += "1";
                }
                else if (i >= n / 2 && j < n / 2)
                {
                    res += "2";
                }
                else if (i >= n / 2 && j >= n / 2)
                {
                    res += "3";
                }

                n /= 2;
                i %= n;
                j %= n;
            }

            return res;
        }

        public static string TopLeft(string quadkey)
        {
            return Top(Left(quadkey));
        }

        public static string TopRight(string quadkey)
        {
            return Top(Right(quadkey));
        }

        public static string BotLeft(string quadkey)
        {
            return Bot(Left(quadkey));
        }

        public static string BotRight(string quadkey)
        {
            return Bot(Right(quadkey));
        }

        public static string Move(string quadkey, Orientation direction)
        {
            switch (direction)
            {
                case Orientation.Bot:
                    return Bot(quadkey);
                case Orientation.Left:
                    return Left(quadkey);
                case Orientation.Right:
                    return Right(quadkey);
                case Orientation.Top:
                    return Top(quadkey);
            }

            throw new Exception("Bad movement direction of quadkey");
        }

        public static string Top(string quadkey)
        {
            var v = new int[quadkey.Length];
            for (int i = 0; i < quadkey.Length; i++)
            {
                v[i] = quadkey[i] - '0';
            }

            for (int i = quadkey.Length - 1; i >= 0; i--)
            {
                v[i] = 2 - v[i] + (2 * (v[i] % 2));
                if (v[i] == 0 || v[i] == 1)
                {
                    break;
                }
            }

            string res = string.Empty;
            for (int i = 0; i < v.Length; i++)
            {
                res += v[i];
            }

            return res;
        }

        public static string Bot(string quadkey, int cnt = 1)
        {
            string res = quadkey;
            while (cnt-- > 0)
            {
                var v = new int[res.Length];
                for (int i = 0; i < res.Length; i++)
                {
                    v[i] = res[i] - '0';
                }

                for (int i = res.Length - 1; i >= 0; i--)
                {
                    v[i] = 2 - v[i] + (2 * (v[i] % 2));
                    if (v[i] == 2 || v[i] == 3)
                    {
                        break;
                    }
                }

                res = string.Empty;
                for (int i = 0; i < v.Length; i++)
                {
                    res += v[i];
                }
            }

            return res;
        }

        public static string Left(string quadkey)
        {
            var v = new int[quadkey.Length];
            for (int i = 0; i < quadkey.Length; i++)
            {
                v[i] = quadkey[i] - '0';
            }

            for (int i = quadkey.Length - 1; i >= 0; i--)
            {
                v[i] = 1 - (v[i] % 2) + (2 * (v[i] / 2));
                if (v[i] == 0 || v[i] == 2)
                {
                    break;
                }
            }

            string res = string.Empty;
            for (int i = 0; i < v.Length; i++)
            {
                res += v[i];
            }

            return res;
        }

        public static string Right(string quadkey, int cnt = 1)
        {
            string res = quadkey;
            while (cnt-- > 0)
            {
                var v = new int[res.Length];
                for (int i = 0; i < res.Length; i++)
                {
                    v[i] = res[i] - '0';
                }

                for (int i = res.Length - 1; i >= 0; i--)
                {
                    v[i] = 1 - (v[i] % 2) + (2 * (v[i] / 2));
                    if (v[i] == 1 || v[i] == 3)
                    {
                        break;
                    }
                }

                res = string.Empty;
                for (int i = 0; i < v.Length; i++)
                {
                    res += v[i];
                }
            }

            return res;
        }

        public static List<string> GetQuadkeysAround(string centerQuadkey, int radius)
        {
            var curQuadkey = centerQuadkey;
            for (int i = 0; i < radius; i++)
            {
                curQuadkey = QuadkeyOrientationHelper.TopLeft(curQuadkey);
            }

            int n = (2 * radius) + 1;
            var iQuadkey = curQuadkey;
            var res = new List<string>();
            for (int i = 0; i < n; i++)
            {
                var ijQuadkey = iQuadkey;
                for (int j = 0; j < n; j++)
                {
                    res.Add(ijQuadkey);
                    ijQuadkey = QuadkeyOrientationHelper.Right(ijQuadkey);
                }

                iQuadkey = QuadkeyOrientationHelper.Bot(iQuadkey);
            }

            return res;
        }

        public static List<string> GetQuadkeys(string quadkey, int quadkeyLength)
        {
            var res = new List<string>();

            if (quadkey.Length > quadkeyLength)
            {
                res.Add(quadkey.Substring(0, quadkeyLength));
            }
            else if (quadkey.Length == quadkeyLength)
            {
                res.Add(quadkey);
            }
            else
            {
                res.AddRange(QuadkeyOrientationHelper.GetQuadkeys(quadkey + "0", quadkeyLength));
                res.AddRange(QuadkeyOrientationHelper.GetQuadkeys(quadkey + "1", quadkeyLength));
                res.AddRange(QuadkeyOrientationHelper.GetQuadkeys(quadkey + "2", quadkeyLength));
                res.AddRange(QuadkeyOrientationHelper.GetQuadkeys(quadkey + "3", quadkeyLength));
            }

            return res;
        }

        public static string GetLca(string q1, string q2)
        {
            for (int i = 0; i < q1.Length && i < q2.Length; i++)
            {
                if (q1[i] != q2[i])
                {
                    return q1.Substring(0, i);
                }
            }

            return q1.Length < q2.Length ? q1 : q2;
        }

        public static void GetSubquadkeyCoordinates(string parentQuadkey, string subquadkey, out int starti, out int startj, int parentSize)
        {
            starti = 0;
            startj = 0;
            for (int i = parentQuadkey.Length; i < subquadkey.Length; i++)
            {
                if ((subquadkey[i] - '0') % 2 == 1)
                {
                    starti += parentSize / 2;
                }

                if ((subquadkey[i] - '0') / 2 == 1)
                {
                    startj += parentSize / 2;
                }

                parentSize /= 2;
            }
        }
    }
}