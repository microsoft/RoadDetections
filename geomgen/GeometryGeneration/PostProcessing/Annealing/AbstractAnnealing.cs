// <copyright file="AbstractAnnealing.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Annealing
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Main;
    using PostProcessing.Metrics;
    using PostProcessing.Stitching;
    using Utils;

    public abstract class AbstractAnnealing
    {
        protected AbstractAnnealing(ImageGraph graph, BlackWhiteImage img, int padding, Configuration config, RunnerLog runnerLog, int hardTimeLimitMs)
        {
            this.Config = config;
            this.Edges = graph.GetEdges();
            this.Res = new AnnealingImage(img);
            this.Source = img;
            this.HardTimeLimitMs = hardTimeLimitMs;
            this.RunnerLog = runnerLog;
            foreach (var edge in this.Edges)
            {
                this.Res.DrawEdge(edge);
            }

            this.Connections = new ConnectionsGraph(this.Edges, this.Source, config.AnnealingImageMargin, padding, config);
        }

        public double Score => this.Res.Score + this.Connections.AngleScore;

        protected Configuration Config { get; }

        protected ConnectionsGraph Connections { get; set; }

        protected AnnealingImage Res { get; set; }

        protected BinaryImage UncutImage { get; set; }

        protected List<Edge> Edges { get; set; }

        protected BlackWhiteImage Source { get; set; }

        protected int HardTimeLimitMs { get; set; }

        protected RunnerLog RunnerLog { get; set; }

        public static AbstractAnnealing Run(ImageGraph graph, BlackWhiteImage img, int runs, Func<ImageGraph, BlackWhiteImage, AbstractAnnealing> factory)
        {
#pragma warning disable CS0162 // Unreachable code detected
#if DEBUG
            if (Main.Constants.Local)
            {
                return RunConsec(graph, img, runs, factory);
            }
            else
            {
                return RunParallel(graph, img, runs, factory);
            }
#else
            if (Main.Constants.Local)
            {
                return RunConsec(graph, img, runs, factory);
            }
            else
            {
                return RunParallel(graph, img, runs, factory);
            }
#endif
#pragma warning restore CS0162 // Unreachable code detected
        }

        public ImageGraph GetGraph()
        {
            return new ImageGraph(this.Edges, this.Res.Width, this.Res.Height);
        }

        public BinaryImage GetResultImage()
        {
            return this.Res.ToBinaryImage();
        }

        public BinaryImage GetUncutImage()
        {
            return new BinaryImage(this.UncutImage);
        }

        public PixelMetric GetMatchScore()
        {
            return this.Res.GetMatchScore();
        }

        public RgbImage DrawOn(RgbImage img, int width = -1, bool drawLines = true)
        {
            foreach (var edge in this.Edges)
            {
                if (edge != null)
                {
                    img.DrawLine(edge.X1, edge.Y1, edge.X2, edge.Y2, Color.White, width == -1 ? edge.Width : width);
                }
            }

            if (drawLines)
            {
                foreach (var edge in this.Edges)
                {
                    if (edge != null)
                    {
                        img.DrawLine(edge.X1, edge.Y1, edge.X2, edge.Y2, Color.Blue, 0);
                    }
                }

                foreach (var edge in this.Edges)
                {
                    if (edge != null)
                    {
                        img.DrawCircle(edge.X1, edge.Y1, 0, Color.Green);
                        img.DrawCircle(edge.X2, edge.Y2, 0, Color.Green);
                    }
                }
            }

            return img;
        }

        public List<Line> GetLines(string quadkey)
        {
            var dsu = new DSU(this.Edges.Count);
            for (int i = 0; i < this.Edges.Count; i++)
            {
                if (this.Connections.CountEdges(this.Edges[i].X1, this.Edges[i].Y1) == 2)
                {
                    var eid = this.Connections.GetFirstEdge(this.Edges[i].X1, this.Edges[i].Y1, i);
                    if (!this.Config.MergeOnlySameWidthEdges || this.Edges[i].Width == this.Edges[eid].Width)
                    {
                        dsu.Union(i, eid);
                    }
                }

                if (this.Connections.CountEdges(this.Edges[i].X2, this.Edges[i].Y2) == 2)
                {
                    var eid = this.Connections.GetFirstEdge(this.Edges[i].X2, this.Edges[i].Y2, i);
                    if (!this.Config.MergeOnlySameWidthEdges || this.Edges[i].Width == this.Edges[eid].Width)
                    {
                        dsu.Union(i, eid);
                    }
                }
            }

            var lines = new Dictionary<int, List<int>>();
            for (int i = 0; i < this.Edges.Count; i++)
            {
                int p = dsu.GetParent(i);
                if (!lines.ContainsKey(p))
                {
                    lines.Add(p, new List<int>());
                }

                lines[p].Add(i);
            }

            var res = new Dictionary<string, Line>();
            foreach (var list in lines.Values)
            {
                var line = new Line(quadkey, list, this.Edges, this.Connections);
                var key = line.GetNormalizedCoordinates();
                if (!res.ContainsKey(key))
                {
                    res.Add(key, line);
                }
            }

            return res.Values.ToList();
        }

        protected abstract AbstractAnnealing Run(int iteration);

        /// <summary>
        /// Removing artifacts - short roadends.
        /// </summary>
        protected void RemoveArtifacts()
        {
            bool IsCloseToEdge(int x, int y, int width, int height) => x < 10 || y < 10 || x >= width - 10 || y >= height - 10;
            for (int eid = 0; eid < this.Edges.Count; eid++)
            {
                if (this.Edges[eid] != null && this.Edges[eid].GetLength() <= this.Config.ArtifactLength)
                {
                    int c1 = this.Connections.CountEdges(this.Edges[eid].X1, this.Edges[eid].Y1);
                    int c2 = this.Connections.CountEdges(this.Edges[eid].X2, this.Edges[eid].Y2);

                    if (c1 == 1 && c2 >= 3 && !IsCloseToEdge(this.Edges[eid].X1, this.Edges[eid].Y1, this.Res.Width, this.Res.Height))
                    {
                        this.Connections.DeleteEdge(eid, this.Edges, this.Res);
                    }
                    else if (c1 >= 3 && c2 == 1 && !IsCloseToEdge(this.Edges[eid].X2, this.Edges[eid].Y2, this.Res.Width, this.Res.Height))
                    {
                        this.Connections.DeleteEdge(eid, this.Edges, this.Res);
                    }
                }
            }
        }

        protected void MergeNodes(Configuration config, int padding)
        {
            for (int eid = 0; eid < this.Edges.Count; eid++)
            {
                if (this.Edges[eid] != null && this.Edges[eid].GetLength() < this.Config.JunctionsMergeDistance)
                {
                    int c1 = this.Connections.CountEdges(this.Edges[eid].X1, this.Edges[eid].Y1);
                    int c2 = this.Connections.CountEdges(this.Edges[eid].X2, this.Edges[eid].Y2);

                    if (c1 > 1 && c2 > 1 && (!config.AnnealingLockPadding || AnnealingHelper.IsInInnerArea(this.Edges[eid], this.Source.Width, this.Source.Height, padding)))
                    {
                        this.Connections.MergeNodes(this.Edges[eid], this.Edges, this.Res);
                    }
                }
            }
        }

        protected void RemoveUselessNodes(Configuration config, int padding)
        {
            var points = new HashSet<Tuple<int, int>>();
            foreach (var edge in this.Edges)
            {
                if (edge != null)
                {
                    foreach (var point in edge.Points)
                    {
                        if (this.Connections.CountEdges(point.Item1, point.Item2) == 2)
                        {
                            if (!config.AnnealingLockPadding || AnnealingHelper.IsInInnerArea(point.Item1, point.Item2, this.Source.Width, this.Source.Height, padding))
                            {
                                points.Add(point);
                            }
                        }
                    }
                }
            }

            foreach (var point in points)
            {
                int px = point.Item1;
                int py = point.Item2;

                var ns = this.Connections.GetNeighbors(px, py);
                int p1x = ns[0].Item1;
                int p1y = ns[0].Item2;
                int p2x = ns[1].Item1;
                int p2y = ns[1].Item2;

                int eid1 = ns[0].Item3;
                int eid2 = ns[1].Item3;
                if (!this.Config.MergeOnlySameWidthEdges || this.Edges[eid1].Width == this.Edges[eid2].Width)
                {
                    double angle = 180 - (GeometryHelper.GetAngle(p1x, p1y, px, py, p2x, p2y) / Math.PI * 180.0);
                    if (angle > this.Config.AnnealingStraightAngleThreshold)
                    {
                        this.Connections.RemoveNeedlessNode(ns, this.Edges, this.Res);
                    }
                }
            }
        }

        protected bool CanMove(int xfrom, int yfrom, int xto, int yto)
        {
            return
                GenericHelper.IsBetween(xto, -this.Config.AnnealingImageMargin, this.Res.Width - 1 + this.Config.AnnealingImageMargin) &&
                GenericHelper.IsBetween(yto, -this.Config.AnnealingImageMargin, this.Res.Height - 1 + this.Config.AnnealingImageMargin) &&
                this.Connections.CountEdges(xto, yto) == 0 &&
                (!this.Config.AnnealingCheckAngleWhenMoveNode || this.Connections.CheckAngle(xfrom, yfrom, xto, yto));
        }

        private static AbstractAnnealing RunConsec(ImageGraph graph, BlackWhiteImage img, int runs, Func<ImageGraph, BlackWhiteImage, AbstractAnnealing> factory)
        {
            if (runs == 0)
            {
                return factory(graph, img);
            }

            AbstractAnnealing res = null;

            for (int i = 0; i < runs; i++)
            {
                var runner = factory(graph, img);
                if (runner.Run(i) == null)
                {
                    continue;
                }

                if (double.IsNaN(runner.Score))
                {
                    throw new Exception("Annealing score must not be NaN");
                }

                if (res == null || GenericHelper.CompareDouble(runner.Res.Score, res.Res.Score) > 0)
                {
                    res = runner;
                }
            }

            return res;
        }

        private static AbstractAnnealing RunParallel(ImageGraph graph, BlackWhiteImage img, int runs, Func<ImageGraph, BlackWhiteImage, AbstractAnnealing> factory)
        {
            if (runs == 0)
            {
                return factory(graph, img);
            }

            AbstractAnnealing res = null;

            var mutex = new Mutex();
            Parallel.ForEach(Enumerable.Range(0, runs), run =>
            {
                var runner = factory(graph, img);
                if (runner.Run(run) == null)
                {
                    return;
                }

                if (double.IsNaN(runner.Score))
                {
                    throw new Exception("Annealing score must not be NaN");
                }

                mutex.WaitOne();
                if (res == null || GenericHelper.CompareDouble(runner.Res.Score, res.Res.Score) > 0)
                {
                    res = runner;
                }

                mutex.ReleaseMutex();
            });

            return res;
        }
    }
}
