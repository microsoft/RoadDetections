// <copyright file="SimpleAnnealing.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using ImageUtils.Images;
    using PostProcessing.Main;
    using Utils;

    public class SimpleAnnealing : AbstractAnnealing
    {
        private readonly int padding;

        public SimpleAnnealing(ImageGraph graph, BlackWhiteImage img, int padding, Configuration config, RunnerLog runnerLog, int hardTimeLimitMs = -1)
            : base(graph, img, padding, config, runnerLog, hardTimeLimitMs)
        {
            this.padding = padding;
        }

        protected override AbstractAnnealing Run(int runId)
        {
            var watch = new Stopwatch();
            watch.Start();

            var rng = new Random(999 + runId);

            int stepsSinceLastScoreChange = 0;
            while (stepsSinceLastScoreChange < 5)
            {
                var scoreWas = this.Score;
                var operations = AnnealingHelper.GetOperationsOrder(this.Edges, this.Connections, this.Source, this.padding, this.Config);
                var operationsOrder = Enumerable.Range(0, operations.Length).ToArray();
                RandomHelper.Shuffle(operationsOrder, rng);
                for (int i = 0; i < operationsOrder.Length; i++)
                {
                    var operationType = operations[operationsOrder[i]].Item1;
                    var id = operations[operationsOrder[i]].Item2;

                    if (operationType == AnnealingHelper.AnnealingOperation.Move || operationType == AnnealingHelper.AnnealingOperation.GrowMove)
                    {
                        var direction = operations[operationsOrder[i]].Item3;
                        if (direction == 1)
                        {
                            this.TryMove(this.Edges[id].X1, this.Edges[id].Y1, Main.Constants.AnnealingMaxAmplitude, operationType == AnnealingHelper.AnnealingOperation.GrowMove, rng);
                        }
                        else if (direction == 2)
                        {
                            this.TryMove(this.Edges[id].X2, this.Edges[id].Y2, Main.Constants.AnnealingMaxAmplitude, operationType == AnnealingHelper.AnnealingOperation.GrowMove, rng);
                        }
                    }
                    else if (operationType == AnnealingHelper.AnnealingOperation.GrowShrink)
                    {
                        this.TryGrowShrink(this.Edges[id]);
                    }
                }

                if (GenericHelper.CompareDouble(this.Score, scoreWas) == 0)
                {
                    stepsSinceLastScoreChange++;
                }
                else
                {
                    stepsSinceLastScoreChange = 0;

                    if (this.Score < scoreWas)
                    {
                        throw new Exception("Score should not decrease in Simple Annealing");
                    }
                }
            }

            this.Edges = this.Connections.FitEdges(this.Edges, this.Res);

            watch.Stop();

            this.RunnerLog.Add("SimpleAnnealing", "PixelScore=" + this.Res.Score + ",AngleScore=" + this.Connections.AngleScore + ",MatchScore=" + this.Res.GetMatchScore());
            this.RunnerLog.Add("SimpleAnnealing." + runId, watch);

            return this;
        }

        private bool TryGrowShrink(Edge edge)
        {
            double growScore = -1, shrinkScore = -1;

            if (edge.Width < Main.Constants.MinEdgeWidth || this.Connections.CheckIfEdgeWidthFits(edge, edge.Width + Main.Constants.EdgeAddWidth, this.Edges))
            {
                // apply changes
                this.Res.EraseEdge(edge);
                edge.Width += Main.Constants.EdgeAddWidth;
                this.Res.DrawEdge(edge);

                growScore = this.Score;

                // revert changes
                this.Res.EraseEdge(edge);
                edge.Width -= Main.Constants.EdgeAddWidth;
                this.Res.DrawEdge(edge);
            }

            if (edge.Width > Main.Constants.MaxEdgeWidth || this.Connections.CheckIfEdgeWidthFits(edge, edge.Width - Main.Constants.EdgeAddWidth, this.Edges))
            {
                // apply changes
                this.Res.EraseEdge(edge);
                edge.Width -= Main.Constants.EdgeAddWidth;
                this.Res.DrawEdge(edge);

                shrinkScore = this.Score;

                // revert changes
                this.Res.EraseEdge(edge);
                edge.Width += Main.Constants.EdgeAddWidth;
                this.Res.DrawEdge(edge);
            }

            if (GenericHelper.CompareDouble(growScore, 0) >= 0 && GenericHelper.CompareDouble(growScore, this.Score) >= 0 && GenericHelper.CompareDouble(growScore, shrinkScore) >= 0)
            {
                this.Res.EraseEdge(edge);
                edge.Width += Main.Constants.EdgeAddWidth;
                this.Res.DrawEdge(edge);

                return true;
            }
            else if (GenericHelper.CompareDouble(growScore, 0) >= 0 && GenericHelper.CompareDouble(shrinkScore, this.Score) > 0)
            {
                this.Res.EraseEdge(edge);
                edge.Width -= Main.Constants.EdgeAddWidth;
                this.Res.DrawEdge(edge);

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryMove(int x1, int y1, int amplitude, bool tryGrow, Random rng)
        {
            double bestScore = -1;
            int bestdx = -1, bestdy = -1;
            bool bestExists = false;
            AnnealingHelper.Dirorder[amplitude] = RandomHelper.Shuffle(AnnealingHelper.Dirorder[amplitude].ToArray(), rng).ToList();
            for (int i = 0; i < AnnealingHelper.Dirorder[amplitude].Count; i++)
            {
                int dx = AnnealingHelper.Dxs[AnnealingHelper.Dirorder[amplitude][i]];
                int dy = AnnealingHelper.Dys[AnnealingHelper.Dirorder[amplitude][i]];
                int x2 = x1 + dx;
                int y2 = y1 + dy;

                if (this.CanMove(x1, y1, x2, y2))
                {
                    // apply changes
                    this.Connections.MoveNode(x1, y1, x2, y2, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, true, false);

                    var curScore = this.Score;

                    // revert changes
                    this.Connections.MoveNode(x2, y2, x1, y1, this.Edges, this.Res, tryGrow ? -Main.Constants.EdgeAddWidth : 0, true, true);

                    if (!bestExists || GenericHelper.CompareDouble(curScore, bestScore) >= 0)
                    {
                        bestScore = curScore;
                        bestdx = dx;
                        bestdy = dy;
                        bestExists = true;
                    }
                }
            }

            if (bestExists)
            {
                int x2 = x1 + bestdx;
                int y2 = y1 + bestdy;

                if (GenericHelper.CompareDouble(bestScore, this.Score) >= 0)
                {
                    this.Connections.MoveNode(x1, y1, x2, y2, this.Edges, this.Res, tryGrow ? Main.Constants.EdgeAddWidth : 0, false, false);

                    return true;
                }
            }

            return false;
        }
    }
}
