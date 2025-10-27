// <copyright file="RealAnnealing.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Main;
    using Utils;

    public class RealAnnealing : AbstractAnnealing
    {
        private readonly int padding;

        public RealAnnealing(ImageGraph graph, BlackWhiteImage img, int padding, Configuration config, RunnerLog runnerLog, int hardTimeLimitMs = -1)
            : base(graph, img, padding, config, runnerLog, hardTimeLimitMs)
        {
            this.padding = padding;
        }

        protected override AbstractAnnealing Run(int runId)
        {
            var watch = new Stopwatch();
            watch.Start();

            var operations = AnnealingHelper.GetOperationsOrder(this.Edges, this.Connections, this.Source, this.padding, this.Config);
            var iterations = Math.Min(this.Config.AnnealingMaxIterations, operations.Length * this.Config.RealAnnealingIterations);
            this.RunnerLog.Add("RealAnnealing." + runId, "Iterations = " + iterations + ". (unrestricted value was " + (operations.Length * this.Config.RealAnnealingIterations) + ")");

            var rng = new Random(999 + runId);
            double scoreBase = this.Res.MaxScore + this.Connections.GetMaxAngleScore();

            for (int i = 0; i < 25; i++)
            {
                foreach (var edge in this.Edges)
                {
                    if (i < 3 || (this.GetGrowShrinkScore(edge, true, out var newScore) && (GenericHelper.CompareDouble(newScore, this.Score) >= 0)))
                    {
                        this.GrowShrink(edge, true);
                    }
                }
            }

            int iterationsSinceLastChange = 0;
            bool skipComplexOperations = false;
            for (int iter = 1; operations.Length > 0 && iter <= iterations; iter++)
            {
                if (watch.ElapsedMilliseconds > this.Config.TimeLimitMsAnnealing)
                {
                    watch.Stop();
                    this.RunnerLog.Abort("RealAnnealing." + runId, watch, iter * 100.0 / iterations);
                    break;
                }

                if (!skipComplexOperations && watch.ElapsedMilliseconds > 0.75 * this.Config.TimeLimitMsAnnealing)
                {
                    skipComplexOperations = true;
                    this.RunnerLog.Add("RealAnnealing." + runId, " at " + GenericHelper.Round(iter * 100.0 / iterations) + "% progrees and 75% of time limit is up - skipping complex operations now...");
                }

                if (this.HardTimeLimitMs != -1 && watch.ElapsedMilliseconds > this.HardTimeLimitMs)
                {
                    watch.Stop();
                    this.RunnerLog.Add("RealAnnealing." + runId, "Over hard time limit");
                    return null;
                }

                var operation = operations[rng.Next(operations.Length)];

                var operationType = operation.Item1;
                var id = operation.Item2;

                if (this.Edges[id] == null)
                {
                    continue;
                }

                iterationsSinceLastChange++;

                if (operationType == AnnealingHelper.AnnealingOperation.Move || operationType == AnnealingHelper.AnnealingOperation.GrowMove)
                {
                    var direction = operation.Item3;
                    int amplitude = Main.Constants.AnnealingMaxAmplitude - (Main.Constants.AnnealingMaxAmplitude * (iter - 1) / iterations);
                    var moveId = AnnealingHelper.Dirorder[amplitude][rng.Next(AnnealingHelper.Dirorder[amplitude].Count)];
                    int x = direction == 1 ? this.Edges[id].X1 : this.Edges[id].X2;
                    int y = direction == 1 ? this.Edges[id].Y1 : this.Edges[id].Y2;

                    if (this.GetMoveScore(x, y, AnnealingHelper.Dxs[moveId], AnnealingHelper.Dys[moveId], operationType == AnnealingHelper.AnnealingOperation.GrowMove, out var newScore) && (GenericHelper.CompareDouble(newScore, this.Score) >= 0 || (this.Score - newScore) / scoreBase < rng.NextDouble() * this.GetTemperature(iter, iterations)))
                    {
                        iterationsSinceLastChange = 0;
                        this.Move(x, y, AnnealingHelper.Dxs[moveId], AnnealingHelper.Dys[moveId], operationType == AnnealingHelper.AnnealingOperation.GrowMove);
                    }
                }
                else if (operationType == AnnealingHelper.AnnealingOperation.GrowShrink)
                {
                    var growTrueShrinkFalse = rng.Next(2) == 0;
                    if (this.GetGrowShrinkScore(this.Edges[id], growTrueShrinkFalse, out var newScore) && (GenericHelper.CompareDouble(newScore, this.Score) >= 0 || (this.Score - newScore) / scoreBase < rng.NextDouble() * this.GetTemperature(iter, iterations)))
                    {
                        iterationsSinceLastChange = 0;
                        this.GrowShrink(this.Edges[id], growTrueShrinkFalse);

                        // at least 50% of iterations passed
                        if (iter > 0.5 * iterations)
                        {
                            if (this.Config.AnnealingDeleteNoWidthEdges && this.Edges[id].Width < 0)
                            {
                                this.Connections.DeleteEdge(id, this.Edges, this.Res);
                                operations = AnnealingHelper.GetOperationsOrder(this.Edges, this.Connections, this.Source, this.padding, this.Config);
                                scoreBase = this.Res.MaxScore + this.Connections.GetMaxAngleScore();
                            }
                        }
                    }
                }
                else if (!skipComplexOperations)
                {
                    if (operationType == AnnealingHelper.AnnealingOperation.MirrorMove || operationType == AnnealingHelper.AnnealingOperation.MirrorGrowMove)
                    {
                        var moveId = AnnealingHelper.Dirorder[Main.Constants.AnnealingMaxAmplitude][rng.Next(AnnealingHelper.Dirorder[Main.Constants.AnnealingMaxAmplitude].Count)];
                        int x = this.Edges[id].X1;
                        int y = this.Edges[id].Y1;
                        int x2 = this.Edges[id].X2;
                        int y2 = this.Edges[id].Y2;
                        if (this.GetMirrorMoveScore(x, y, x2, y2, AnnealingHelper.Dxs[moveId], AnnealingHelper.Dys[moveId], operationType == AnnealingHelper.AnnealingOperation.MirrorGrowMove, out var newScore) && (GenericHelper.CompareDouble(newScore, this.Score) >= 0 || (this.Score - newScore) / scoreBase < rng.NextDouble() * this.GetTemperature(iter, iterations)))
                        {
                            iterationsSinceLastChange = 0;
                            this.MirrorMove(x, y, x2, y2, AnnealingHelper.Dxs[moveId], AnnealingHelper.Dys[moveId], operationType == AnnealingHelper.AnnealingOperation.MirrorGrowMove);
                        }
                    }
                    else if (operationType == AnnealingHelper.AnnealingOperation.DoubleMove || operationType == AnnealingHelper.AnnealingOperation.DoubleGrowMove)
                    {
                        var moveId = AnnealingHelper.Dirorder[Main.Constants.AnnealingMaxAmplitude][rng.Next(AnnealingHelper.Dirorder[Main.Constants.AnnealingMaxAmplitude].Count)];
                        var moveId2 = AnnealingHelper.Dirorder[Main.Constants.AnnealingMaxAmplitude][rng.Next(AnnealingHelper.Dirorder[Main.Constants.AnnealingMaxAmplitude].Count)];
                        int x = this.Edges[id].X1;
                        int y = this.Edges[id].Y1;
                        int x2 = this.Edges[id].X2;
                        int y2 = this.Edges[id].Y2;
                        if (this.GetDoubleMoveScore(x, y, x2, y2, AnnealingHelper.Dxs[moveId], AnnealingHelper.Dys[moveId], AnnealingHelper.Dxs[moveId2], AnnealingHelper.Dys[moveId2], operationType == AnnealingHelper.AnnealingOperation.DoubleGrowMove, out var newScore) && (GenericHelper.CompareDouble(newScore, this.Score) >= 0 || (this.Score - newScore) / scoreBase < rng.NextDouble() * this.GetTemperature(iter, iterations)))
                        {
                            iterationsSinceLastChange = 0;
                            this.DoubleMove(x, y, x2, y2, AnnealingHelper.Dxs[moveId], AnnealingHelper.Dys[moveId], AnnealingHelper.Dxs[moveId2], AnnealingHelper.Dys[moveId2], operationType == AnnealingHelper.AnnealingOperation.DoubleGrowMove);
                        }
                    }
                }

                // at least 33% of iterations passed
                if (iter > 0.33 * iterations)
                {
                    // this happens overall 50 times uniformly distributed through iterations
                    if (iter % (iterations / 50) == 0)
                    {
                        this.RemoveArtifacts();
                        this.RemoveUselessNodes(this.Config, this.padding);
                        this.MergeNodes(this.Config, this.padding);
                        operations = AnnealingHelper.GetOperationsOrder(this.Edges, this.Connections, this.Source, this.padding, this.Config);
                        scoreBase = this.Res.MaxScore + this.Connections.GetMaxAngleScore();
                    }
                }

                if (Constants.Local && Constants.DebugAnnealling && iter % (iterations / 50) == 0)
                {
                    ////this.Connections.Draw(this.Edges).Save(path);
                }
            }

            //// for debugging:
            ////if (Constants.Local && Constants.DebugAnnealling)
            ////{
            ////    this.FindBestMove();
            ////}

            if (this.Config.AnnealingTryDeleteEdgesWithMinWidthAfterAnnealingIsFinished)
            {
                for (int i = 0; i < this.Edges.Count; i++)
                {
                    if (this.Edges[i] != null && this.Edges[i].Width <= Main.Constants.MinEdgeWidth)
                    {
                        this.Res.EraseEdge(this.Edges[i]);
                        var score = this.Score;
                        this.Res.DrawEdge(this.Edges[i]);

                        if (GenericHelper.CompareDouble(score, this.Score) > 0)
                        {
                            this.Connections.DeleteEdge(i, this.Edges, this.Res);
                        }
                    }
                }
            }

            this.UncutImage = this.Res.ToBinaryImage();

            this.Edges = this.Connections.FitEdges(this.Edges, this.Res);

            watch.Stop();

            this.RunnerLog.Add("RealAnnealing." + runId, "PixelScore=" + this.Res.Score + ",AngleScore=" + this.Connections.AngleScore + ",MatchScore=" + this.Res.GetMatchScore());
            this.RunnerLog.Add("RealAnnealing." + runId, watch);

            return this;
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void FindBestMove()
#pragma warning restore IDE0051 // Remove unused private members
        {
            var points = new HashSet<Tuple<int, int>>();
            foreach (var edge in this.Edges)
            {
                if (edge != null)
                {
                    foreach (var point in edge.Points)
                    {
                        points.Add(point);
                    }
                }
            }

            double bestScore = this.Score;
            string move = string.Empty;
            foreach (var point in points)
            {
                for (int i = 0; i < GeometryHelper.DIJ.Length / 2; i++)
                {
                    int di = GeometryHelper.DIJ[i, 0];
                    int dj = GeometryHelper.DIJ[i, 1];
                    if (this.GetMoveScore(point.Item1, point.Item2, di, dj, false, out var asc))
                    {
                        bestScore = asc;
                        move = point.Item1 + " " + point.Item2 + " " + di + " " + dj;
                    }
                }
            }

            Console.WriteLine("Best move here is:\t" + this.Score + "->" + bestScore + " " + (bestScore - this.Score) + "\t" + move);
        }

        private double GetTemperature(int iter, int iterations)
        {
            return this.Config.RealAnnealingTemperatureCoefficient * (iterations - iter) / iterations;
        }

        private bool GetGrowShrinkScore(Edge edge, bool growTrueShrinkFalse, out double res)
        {
            int add = growTrueShrinkFalse ? Main.Constants.EdgeAddWidth : -Main.Constants.EdgeAddWidth;

            if (edge == null || !this.Connections.CheckIfEdgeWidthFits(edge, edge.Width + add, this.Edges))
            {
                res = 0;
                return false;
            }

            // apply changes
            this.Res.MoveEdge(edge.X1, edge.Y1, edge.X2, edge.Y2, edge.X2, edge.Y2, edge.Width, add);

            res = this.Score;

            // revert changes
            this.Res.MoveEdge(edge.X1, edge.Y1, edge.X2, edge.Y2, edge.X2, edge.Y2, edge.Width + add, -add);

            return true;
        }

        private void GrowShrink(Edge edge, bool growTrueShrinkFalse)
        {
            int add = growTrueShrinkFalse ? Main.Constants.EdgeAddWidth : -Main.Constants.EdgeAddWidth;
            this.Res.MoveEdge(edge.X1, edge.Y1, edge.X2, edge.Y2, edge.X2, edge.Y2, edge.Width, add);
            edge.Width += add;
        }

        private bool GetMoveScore(int xfrom, int yfrom, int dx, int dy, bool tryGrow, out double res)
        {
            int xto = xfrom + dx;
            int yto = yfrom + dy;

            if (!this.CanMove(xfrom, yfrom, xto, yto))
            {
                res = 0;
                return false;
            }

            // apply changes
            this.Connections.MoveNode(xfrom, yfrom, xto, yto, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, true, false);

            res = this.Score;

            // revert changes
            this.Connections.MoveNode(xto, yto, xfrom, yfrom, this.Edges, this.Res, tryGrow ? -Main.Constants.EdgeAddWidth : 0, true, true);
            return true;
        }

        private bool GetMirrorMoveScore(int xfrom, int yfrom, int x2from, int y2from, int dx, int dy, bool tryGrow, out double res)
        {
            int xto = xfrom + dx;
            int yto = yfrom + dy;

            int x2to = x2from - dx;
            int y2to = y2from - dy;

            if ((xto == x2to && yto == y2to) || !this.CanMove(xfrom, yfrom, xto, yto) || !this.CanMove(x2from, y2from, x2to, y2to))
            {
                res = 0;
                return false;
            }

            // apply changes
            this.Connections.MoveNode(xfrom, yfrom, xto, yto, this.Edges, this.Res, 0, true, false);
            this.Connections.MoveNode(x2from, y2from, x2to, y2to, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, true, false);

            res = this.Score;

            // revert changes
            this.Connections.MoveNode(x2to, y2to, x2from, y2from, this.Edges, this.Res, tryGrow ? -Main.Constants.EdgeAddWidth : 0, true, true);
            this.Connections.MoveNode(xto, yto, xfrom, yfrom, this.Edges, this.Res, 0, true, true);
            return true;
        }

        private bool GetDoubleMoveScore(int xfrom, int yfrom, int x2from, int y2from, int dx, int dy, int dx2, int dy2, bool tryGrow, out double res)
        {
            int xto = xfrom + dx;
            int yto = yfrom + dy;

            int x2to = x2from + dx2;
            int y2to = y2from + dy2;

            if ((xto == x2to && yto == y2to) || !this.CanMove(xfrom, yfrom, xto, yto) || !this.CanMove(x2from, y2from, x2to, y2to))
            {
                res = 0;
                return false;
            }

            // apply changes
            this.Connections.MoveNode(xfrom, yfrom, xto, yto, this.Edges, this.Res, 0, true, false);
            this.Connections.MoveNode(x2from, y2from, x2to, y2to, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, true, false);

            res = this.Score;

            // revert changes
            this.Connections.MoveNode(x2to, y2to, x2from, y2from, this.Edges, this.Res, tryGrow ? -Main.Constants.EdgeAddWidth : 0, true, true);
            this.Connections.MoveNode(xto, yto, xfrom, yfrom, this.Edges, this.Res, 0, true, true);

            return true;
        }

        private void Move(int x1, int y1, int dx, int dy, bool tryGrow = false)
        {
            int x2 = x1 + dx;
            int y2 = y1 + dy;
            this.Connections.MoveNode(x1, y1, x2, y2, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, false, false);
        }

        private void MirrorMove(int xfrom, int yfrom, int x2from, int y2from, int dx, int dy, bool tryGrow = false)
        {
            int xto = xfrom + dx;
            int yto = yfrom + dy;

            int x2to = x2from - dx;
            int y2to = y2from - dy;

            this.Connections.MoveNode(xfrom, yfrom, xto, yto, this.Edges, this.Res, 0, false, false);
            this.Connections.MoveNode(x2from, y2from, x2to, y2to, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, false, false);
        }

        private void DoubleMove(int xfrom, int yfrom, int x2from, int y2from, int dx, int dy, int dx2, int dy2, bool tryGrow = false)
        {
            int xto = xfrom + dx;
            int yto = yfrom + dy;

            int x2to = x2from + dx2;
            int y2to = y2from + dy2;

            this.Connections.MoveNode(xfrom, yfrom, xto, yto, this.Edges, this.Res, 0, false, false);
            this.Connections.MoveNode(x2from, y2from, x2to, y2to, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, false, false);
        }
    }
}
