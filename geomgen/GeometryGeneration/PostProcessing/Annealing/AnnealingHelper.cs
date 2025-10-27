// <copyright file="AnnealingHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using ImageUtils;
    using ImageUtils.Images;
    using PostProcessing.Main;

    public class AnnealingHelper
    {
        public static readonly List<int>[] Dirorder;
        public static readonly int[] Dxs;
        public static readonly int[] Dys;

        static AnnealingHelper()
        {
            Dxs = new int[(((2 * Main.Constants.AnnealingMaxAmplitude) + 1) * ((2 * Main.Constants.AnnealingMaxAmplitude) + 1)) - 1];
            Dys = new int[(((2 * Main.Constants.AnnealingMaxAmplitude) + 1) * ((2 * Main.Constants.AnnealingMaxAmplitude) + 1)) - 1];
            Dirorder = new List<int>[Main.Constants.AnnealingMaxAmplitude + 1];

            for (int i = 0; i < Dirorder.Length; i++)
            {
                Dirorder[i] = new List<int>();
            }

            int dxi = 0, dyi = 0;
            for (int dx = -Main.Constants.AnnealingMaxAmplitude; dx <= Main.Constants.AnnealingMaxAmplitude; dx++)
            {
                for (int dy = -Main.Constants.AnnealingMaxAmplitude; dy <= Main.Constants.AnnealingMaxAmplitude; dy++)
                {
                    int amplitude = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    if (amplitude == 0)
                    {
                        continue;
                    }

                    Dxs[dxi++] = dx;
                    Dys[dyi++] = dy;

                    for (int a = 1; a <= amplitude; a++)
                    {
                        Dirorder[a].Add(dxi - 1);
                    }
                }
            }
        }

        public enum AnnealingOperation
        {
            Move,
            GrowMove,
            MirrorMove,
            MirrorGrowMove,
            DoubleMove,
            DoubleGrowMove,
            GrowShrink,
        }

        public static List<Edge> BreakOnOriginalImageBoundaries(List<Edge> edges, int imageWidth, int imageHeight, int padding)
        {
            var res = new List<Edge>();

            foreach (var edge in edges)
            {
                var pixels = Bresenham.GetLinePoints(edge.X1, edge.Y1, edge.X2, edge.Y2).ToList();
                var insides = pixels.Select(pixel => AnnealingHelper.IsInOriginalImage(pixel.Item1, pixel.Item2, imageWidth, imageHeight, padding)).ToList();
                var points = new List<Tuple<int, int>>
                {
                    pixels.First()
                };

                for (int i = 1; i + 1 < pixels.Count; i++)
                {
                    if (insides[i] != insides[i - 1] || insides[i] != insides[i + 1])
                    {
                        points.Add(pixels[i]);
                    }
                }

                points.Add(pixels.Last());

                for (int i = 0; i + 1 < points.Count; i++)
                {
                    res.Add(new Edge { X1 = points[i].Item1, Y1 = points[i].Item2, X2 = points[i + 1].Item1, Y2 = points[i + 1].Item2, Width = edge.Width });
                }
            }

            return res;
        }

        public static bool IsInOriginalImage(int x, int y, int imageWidth, int imageHeight, int padding)
        {
            return x >= padding
                && x <= imageWidth - padding - 1
                && y >= padding
                && y <= imageHeight - padding - 1;
        }

        public static bool IsInInnerArea(int x, int y, int imageWidth, int imageHeight, int padding)
        {
            return (padding == 0 || x >= padding + 1)
                && (padding == 0 || x <= imageWidth - padding - 2)
                && (padding == 0 || y >= padding + 1)
                && (padding == 0 || y <= imageHeight - padding - 2);
        }

        public static bool IsInInnerArea(Edge edge, int imageWidth, int imageHeight, int padding)
        {
            return IsInInnerArea(edge.Points[0].Item1, edge.Points[0].Item2, imageWidth, imageHeight, padding) && IsInInnerArea(edge.Points[1].Item1, edge.Points[1].Item2, imageWidth, imageHeight, padding);
        }

        public static Tuple<AnnealingOperation, int, int>[] GetOperationsOrder(List<Edge> edges, ConnectionsGraph graph, BlackWhiteImage image, int padding, Configuration config)
        {
            var res = new Dictionary<AnnealingOperation, List<Tuple<AnnealingOperation, int, int>>>
            {
                { AnnealingOperation.Move, new List<Tuple<AnnealingOperation, int, int>>() },
                { AnnealingOperation.GrowMove, new List<Tuple<AnnealingOperation, int, int>>() },
                { AnnealingOperation.MirrorMove, new List<Tuple<AnnealingOperation, int, int>>() },
                { AnnealingOperation.MirrorGrowMove, new List<Tuple<AnnealingOperation, int, int>>() },
                { AnnealingOperation.DoubleMove, new List<Tuple<AnnealingOperation, int, int>>() },
                { AnnealingOperation.DoubleGrowMove, new List<Tuple<AnnealingOperation, int, int>>() },
                { AnnealingOperation.GrowShrink, new List<Tuple<AnnealingOperation, int, int>>() },
            };

            var used = new HashSet<Tuple<int, int>>();
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i] == null)
                {
                    continue;
                }

                var isInside1 = config.AnnealingBreakOnOriginalImageBoundaries ? AnnealingHelper.IsInInnerArea(edges[i].X1, edges[i].Y1, image.Width, image.Height, padding) : AnnealingHelper.IsInOriginalImage(edges[i].X1, edges[i].Y1, image.Width, image.Height, padding);
                var isInside2 = config.AnnealingBreakOnOriginalImageBoundaries ? AnnealingHelper.IsInInnerArea(edges[i].X2, edges[i].Y2, image.Width, image.Height, padding) : AnnealingHelper.IsInOriginalImage(edges[i].X2, edges[i].Y2, image.Width, image.Height, padding);

                // node 1
                {
                    var node = Tuple.Create(edges[i].X1, edges[i].Y1);
                    if (!config.AnnealingLockPadding || isInside1 || (!config.AnnealingBreakOnOriginalImageBoundaries && isInside2))
                    {
                        if (config.MoveMidlines || graph.CountEdges(edges[i].X1, edges[i].Y1) != 2)
                        {
                            if (!used.Contains(node))
                            {
                                used.Add(node);
                                res[AnnealingOperation.Move].Add(Tuple.Create(AnnealingOperation.Move, i, 1));
                            }

                            res[AnnealingOperation.GrowMove].Add(Tuple.Create(AnnealingOperation.GrowMove, i, 1));
                        }
                    }
                }

                // node 2
                {
                    var node = Tuple.Create(edges[i].X2, edges[i].Y2);
                    if (!config.AnnealingLockPadding || isInside2 || (!config.AnnealingBreakOnOriginalImageBoundaries && isInside1))
                    {
                        if (config.MoveMidlines || graph.CountEdges(edges[i].X2, edges[i].Y2) != 2)
                        {
                            if (!used.Contains(node))
                            {
                                used.Add(node);
                                res[AnnealingOperation.Move].Add(Tuple.Create(AnnealingOperation.Move, i, 2));
                            }

                            res[AnnealingOperation.GrowMove].Add(Tuple.Create(AnnealingOperation.GrowMove, i, 2));
                        }
                    }
                }
            }

            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i] == null)
                {
                    continue;
                }

                res[AnnealingOperation.GrowShrink].Add(Tuple.Create(AnnealingOperation.GrowShrink, i, -1));
                if (config.UseMirrorMove)
                {
                    if (!config.AnnealingLockPadding || AnnealingHelper.IsInInnerArea(edges[i], image.Width, image.Height, padding))
                    {
                        res[AnnealingOperation.MirrorMove].Add(Tuple.Create(AnnealingOperation.MirrorMove, i, -1));
                        res[AnnealingOperation.MirrorGrowMove].Add(Tuple.Create(AnnealingOperation.MirrorGrowMove, i, -1));
                    }
                }

                if (config.UseDoubleMove)
                {
                    if (!config.AnnealingLockPadding || AnnealingHelper.IsInInnerArea(edges[i], image.Width, image.Height, padding))
                    {
                        res[AnnealingOperation.DoubleMove].Add(Tuple.Create(AnnealingOperation.DoubleMove, i, -1));
                        res[AnnealingOperation.DoubleGrowMove].Add(Tuple.Create(AnnealingOperation.DoubleGrowMove, i, -1));
                    }
                }
            }

            var weights = new[]
            {
                Tuple.Create(AnnealingOperation.GrowShrink, 2), //// E*s
                Tuple.Create(AnnealingOperation.Move, 2), //// V*2s
                Tuple.Create(AnnealingOperation.GrowMove, 1), //// 2E*2s
                Tuple.Create(AnnealingOperation.MirrorMove, 1), //// E*4s
                Tuple.Create(AnnealingOperation.MirrorGrowMove, 1), //// E*4s
                Tuple.Create(AnnealingOperation.DoubleMove, 1), //// E
                Tuple.Create(AnnealingOperation.DoubleGrowMove, 1), //// E
            };

            var resOps = new List<Tuple<AnnealingOperation, int, int>>();
            foreach (var weight in weights)
            {
                foreach (var op in res[weight.Item1])
                {
                    for (int i = 0; i < weight.Item2; i++)
                    {
                        resOps.Add(Tuple.Create(op.Item1, op.Item2, op.Item3));
                    }
                }
            }

            return resOps.ToArray();
        }

        public static double GetAngleRestriction(int xfrom, int yfrom, int n1x, int n1y, int neighbors, Configuration config)
        {
            if (GeometryHelper.Distance2(xfrom, yfrom, n1x, n1y) < config.AnnealingMinDist2ToCheckAngle)
            {
                if (neighbors == 1)
                {
                    return 45.0;
                }
                else if (neighbors == 2)
                {
                    return 45.0;
                }
                else
                {
                    return 120.0;
                }
            }
            else
            {
                if (neighbors == 1)
                {
                    return 120.0;
                }
                else if (neighbors == 2)
                {
                    return 45.0;
                }
                else
                {
                    return 120.0;
                }
            }
        }
    }
}