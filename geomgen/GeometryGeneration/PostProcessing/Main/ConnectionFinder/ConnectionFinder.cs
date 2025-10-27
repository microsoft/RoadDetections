// <copyright file="ConnectionFinder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Search;
    using Utils;

    public static partial class ConnectionFinder
    {
        /// <summary>
        /// Run Dijkstra from every node until it reaches some other node
        ///  - fine for curving
        ///  - fine for dark pixels.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="img">grayscale image.</param>
        /// <param name="thinImg"></param>
        /// <param name="outImg"></param>
        /// <param name="config"></param>
        /// <param name="runnerLog"></param>
        /// <returns></returns>
        public static ImageGraph Run(ImageGraph graph, BlackWhiteImage img, BinaryImage thinImg, out RgbImage outImg, Configuration config, RunnerLog runnerLog)
        {
            outImg = new RgbImage(thinImg);

            var roadEnds = new List<int>();
            var singles = new HashSet<int>();
            for (int i = 0; i < graph.NodesCount; i++)
            {
                graph.GetCoordinates(i, out var x, out var y);

                // single white point - has no connections but color is white
                if (graph.GetConnectionsCount(i) == 0 && thinImg.Data[x, y])
                {
                    singles.Add(i);
                }
            }

            roadEnds.AddRange(singles);

            for (int i = 0; i < graph.NodesCount; i++)
            {
                // road end
                if (graph.GetConnectionsCount(i) == 1)
                {
                    roadEnds.Add(i);
                }
            }

            var weight = new double[graph.NodesCount];
            var distance = new double[graph.NodesCount];
            var colorWeight = new double[graph.NodesCount];
            var angleWeight = new double[graph.NodesCount];
            var length = new double[graph.NodesCount];
            var par = new int[graph.NodesCount];

            roadEnds = RandomHelper.Shuffle(roadEnds.ToArray(), new Random(0)).ToList();

            var watch = new Stopwatch();
            watch.Start();
            var profiler = new Profiler();

            // running from road ends
            var used = new HashSet<int>();
            var used2 = new HashSet<Tuple<int, int>>();
            for (int ri = 0; ri < roadEnds.Count; ri++)
            {
                var i = roadEnds[ri];
                if (watch.ElapsedMilliseconds > config.TimeLimitMsConnectionFinder)
                {
                    watch.Stop();
                    runnerLog.Abort("ConnectionFinder", watch, ri * 100.0 / roadEnds.Count);
                    break;
                }

                if (!singles.Contains(i) && used.Contains(i))
                {
                    continue;
                }

                profiler.Start("ConnectionFinder.Initialization");

                for (int j = 0; j < graph.NodesCount; j++)
                {
                    weight[j] = Constants.InfiniteDistance;
                    par[j] = -1;
                }

                weight[i] = 0;
                distance[i] = 0;
                colorWeight[i] = 0;
                angleWeight[i] = 0;
                length[i] = 1;
                var q = new Heap<DistInfo>();
                q.Add(new DistInfo { Id = i, Dist = weight[i] });

                graph.GetCoordinates(i, out var startx, out var starty);
                GetApproximatePrevNodeCoordinates(i, graph, 10, out var prevx, out var prevy);
                bool isSinglePoint = graph.GetConnectionsCount(i) == 0 && thinImg.Data[startx, starty];

                profiler.Stop("ConnectionFinder.Initialization");
                profiler.Start("ConnectionFinder.PathSearch");
                while (q.Count != 0)
                {
                    var top = q.Top();
                    q.Pop();
                    if (GenericHelper.CompareDouble(top.Dist, weight[top.Id]) == 1)
                    {
                        continue;
                    }

                    graph.GetCoordinates(top.Id, out var x, out var y);

                    for (int j = 0; j < 8; j++)
                    {
                        int ii = x + GeometryHelper.DIJ[j, 0];
                        int jj = y + GeometryHelper.DIJ[j, 1];

                        if (img.CheckCoordinates(ii, jj) && GeometryHelper.Distance(startx, starty, ii, jj) < config.ConnectionFinderFlyDistanceLimit)
                        {
                            var to = graph.GetId(ii, jj);
                            var weightColor = WeightColor(img.Data[ii, jj], config);
                            var weightAngle = WeightAngle(prevx, prevy, startx, starty, ii, jj, config);
                            var weightAdd = GeometryHelper.DijDist[j] + weightColor + weightAngle;

                            var newWeight = weight[top.Id] + weightAdd;

                            if (GenericHelper.CompareDouble(newWeight, config.ConnectionFinderMaxGapConnection) <= 0 && GenericHelper.CompareDouble(newWeight, weight[to]) < 0)
                            {
                                weight[to] = newWeight;
                                distance[to] = distance[top.Id] + GeometryHelper.DijDist[j];
                                colorWeight[to] = colorWeight[top.Id] + weightColor;
                                angleWeight[to] = angleWeight[top.Id] + weightAngle;
                                length[to] = length[top.Id] + 1;
                                par[to] = top.Id;
                                q.Add(new DistInfo { Id = to, Dist = weight[to] });
                            }
                        }
                    }
                }

                profiler.Stop("ConnectionFinder.PathSearch");
                profiler.Start("ConnectionFinder.FindBestConnection");
                int bestConnection = FindBestConnection(graph, img, thinImg, weight, par, i, startx, starty, prevx, prevy, config);
                profiler.Stop("ConnectionFinder.FindBestConnection");
                if (bestConnection != -1)
                {
                    profiler.Start("ConnectionFinder.Dijkstra");
                    var dijkstraDist = Dijkstra.Run(graph, i).Dist;
                    profiler.Stop("ConnectionFinder.Dijkstra");
                    profiler.Start("ConnectionFinder.AddConnection");

                    if (!(singles.Contains(i) && singles.Contains(bestConnection) && used2.Contains(Tuple.Create(i, bestConnection))))
                    {
                        AddConnection(graph, i, bestConnection, distance, colorWeight, angleWeight, length, dijkstraDist, par, used, used2, thinImg, outImg, startx, starty, img, config);
                    }

                    if (isSinglePoint)
                    {
                        int bestConnection2 = FindBestConnection(graph, img, thinImg, weight, par, i, startx, starty, prevx, prevy, config, bestConnection);

                        if (bestConnection2 != -1)
                        {
                            if (!(singles.Contains(i) && singles.Contains(bestConnection2) && used2.Contains(Tuple.Create(i, bestConnection2))))
                            {
                                AddConnection(graph, i, bestConnection2, distance, colorWeight, angleWeight, length, dijkstraDist, par, used, used2, thinImg, outImg, startx, starty, img, config);
                            }
                        }
                    }

                    profiler.Stop("ConnectionFinder.AddConnection");
                }
            }

            watch.Stop();

            runnerLog.Add("ConnectionFinder", profiler.GetTimeSpentMessage("ConnectionFinder.Initialization"));
            runnerLog.Add("ConnectionFinder", profiler.GetTimeSpentMessage("ConnectionFinder.PathSearch"));
            runnerLog.Add("ConnectionFinder", profiler.GetTimeSpentMessage("ConnectionFinder.FindBestConnection"));
            runnerLog.Add("ConnectionFinder", profiler.GetTimeSpentMessage("ConnectionFinder.Dijkstra"));
            runnerLog.Add("ConnectionFinder", profiler.GetTimeSpentMessage("ConnectionFinder.AddConnection"));

            return graph;
        }

        private static void AddConnection(ImageGraph graph, int i, int bestConnection, double[] distance, double[] colorWeight, double[] angleWeight, double[] length, double[] dijkstraDist, int[] par, HashSet<int> used, HashSet<Tuple<int, int>> used2, BinaryImage thinImg, RgbImage outImg, int startx, int starty, BlackWhiteImage img, Configuration config)
        {
            graph.GetCoordinates(i, out var xi, out var yi);
            graph.GetCoordinates(bestConnection, out var xb, out var yb);

            if (distance[bestConnection] < 5 && (IsNearTheEdge(xi, yi, thinImg.Width, thinImg.Height) || IsNearTheEdge(xb, yb, thinImg.Width, thinImg.Height)))
            {
                return;
            }

            double avgPathColor = GetAvgPathColor(graph, bestConnection, length, par, img);
            double colorSqrDiff = GetSqrColorDiffOnPath(graph, bestConnection, length, par, img, avgPathColor);
            double avgColor1 = GetAvgColor(startx, starty, 2, img);
            double avgColor2 = GetAvgColor(xb, yb, 2, img);
            double avgColorDiff = Math.Abs(avgColor1 - avgColor2) * Math.Abs(avgColor1 - avgColor2);
            double euclidDist = GeometryHelper.Distance(startx, starty, xb, yb);
            var areBothRoadEnds = graph.GetConnectionsCount(i) < 2 && graph.GetConnectionsCount(bestConnection) < 2;
            var predictTree = ConnectionFinder_TreeClassifier.PredictGoodConnection(Dijkstra.IsReachable(dijkstraDist[bestConnection]), distance[bestConnection], colorWeight[bestConnection], angleWeight[bestConnection], euclidDist, colorSqrDiff, areBothRoadEnds, avgColorDiff, avgPathColor);

            //// for data generation save features here, label and retrain
            if (predictTree > config.ConnectionFinder_TreeClassifier_Threshold)
            {
                used.Add(i);
                used.Add(bestConnection);
                used2.Add(Tuple.Create(i, bestConnection));
                used2.Add(Tuple.Create(bestConnection, i));

                int node = bestConnection;
                while (node != -1)
                {
                    graph.GetCoordinates(node, out var xp, out var yp);
                    thinImg.DrawCircle(xp, yp, 0, Color.White);
                    outImg.DrawCircle(xp, yp, 1, Color.Red);

                    if (par[node] != -1)
                    {
                        graph.AddEdge(node, par[node]);
                    }

                    if (node == bestConnection || par[node] == -1)
                    {
                        thinImg.DrawCircle(xp, yp, 0, Color.White);
                        outImg.DrawCircle(xp, yp, 1, Color.Red);
                    }

                    node = par[node];
                }
            }
        }

        private static double GetAvgPathColor(ImageGraph graph, int bestConnection, double[] length, int[] par, BlackWhiteImage img)
        {
            double res = 0.0;
            int node = bestConnection;

            while (node != -1)
            {
                graph.GetCoordinates(node, out var xp, out var yp);

                res += img.Data[xp, yp];

                node = par[node];
            }

            res /= length[bestConnection];

            return res;
        }

        private static double GetSqrColorDiffOnPath(ImageGraph graph, int bestConnection, double[] length, int[] par, BlackWhiteImage img, double avgPathColor)
        {
            double colorSqrDiff = 0.0;
            int node = bestConnection;

            while (node != -1)
            {
                graph.GetCoordinates(node, out var xp, out var yp);

                colorSqrDiff += (avgPathColor - img.Data[xp, yp]) * (avgPathColor - img.Data[xp, yp]);

                node = par[node];
            }

            colorSqrDiff /= length[bestConnection];

            return colorSqrDiff;
        }

        private static double GetAvgColor(int x, int y, int r, BlackWhiteImage img)
        {
            double sum = 0;
            int cnt = 0;
            for (int i = x - r; i <= x + r; i++)
            {
                for (int j = y - r; j <= y + r; j++)
                {
                    if (img.CheckCoordinates(i, j) && GeometryHelper.Distance2(x, y, i, j) <= r * r)
                    {
                        sum += img.Data[i, j];
                        cnt++;
                    }
                }
            }

            return sum / cnt;
        }

        private static int FindBestConnection(ImageGraph graph, BlackWhiteImage img, BinaryImage thnimg, double[] weight, int[] par, int i, int startx, int starty, int prevx, int prevy, Configuration config, int currentBestConnection = -1)
        {
            int bestConnection = -1;
            var bestConnections = new List<Tuple<double, int>>();
            for (int j = 0; j < graph.NodesCount; j++)
            {
                if (i == j || par[j] == -1)
                {
                    continue;
                }

                graph.GetCoordinates(j, out var x, out var y);

                if (currentBestConnection != -1)
                {
                    graph.GetCoordinates(currentBestConnection, out var curBestX, out var curBestY);
                    var angle = GeometryHelper.GetAngle(curBestX, curBestY, startx, starty, x, y) / Math.PI * 180.0;

                    if (GenericHelper.CompareDouble(angle, 90) >= 0)
                    {
                        continue;
                    }
                }

                if (GenericHelper.CompareDouble(weight[j], Constants.InfiniteDistance) < 0)
                {
                    if (bestConnection == -1 || weight[j] < weight[bestConnection])
                    {
                        if (graph.GetConnectionsCount(j) != 0 || thnimg.Data[x, y] || IsNearTheEdge(x, y, img.Width, img.Height))
                        {
                            var angle = GeometryHelper.GetAngle(prevx, prevy, startx, starty, x, y) / Math.PI * 180.0;
                            if ((x == startx && y == starty) || (prevx == startx && prevy == starty) ||
                                (graph.GetConnectionsCount(j) == 0 && GenericHelper.CompareDouble(angle, config.ConnectionFinderJunctionAngleConnection) <= 0) ||
                                (graph.GetConnectionsCount(j) == 1 && GenericHelper.CompareDouble(angle, config.ConnectionFinderRoadendAngleConnection) <= 0) ||
                                (graph.GetConnectionsCount(j) > 1 && GenericHelper.CompareDouble(angle, config.ConnectionFinderJunctionAngleConnection) <= 0))
                            {
                                bestConnection = j;
                                bestConnections.Add(Tuple.Create(weight[j], j));
                            }
                        }
                    }
                }
            }

            bestConnection = -1;
            foreach (var (w, j) in bestConnections.OrderBy(tp => tp))
            {
                if (FitCloseEncounters(j, par, graph, img))
                {
                    bestConnection = j;
                    break;
                }
            }

            return bestConnection;
        }

        private static bool FitCloseEncounters(int node, int[] par, ImageGraph graph, BlackWhiteImage img)
        {
            var res = new int[5];
            while (node != -1)
            {
                graph.GetCoordinates(node, out var xp, out var yp);
                int minDist = 100;
                for (int i = -(res.Length - 1); i <= res.Length - 1; i++)
                {
                    for (int j = -(res.Length - 1); j <= res.Length - 1; j++)
                    {
                        if (graph.CheckIJ(xp + i, yp + j) && (graph.CountEdges(xp + i, yp + j) > 0 || IsNearTheEdge(xp + i, yp + j, img.Width, img.Height)))
                        {
                            minDist = Math.Min(minDist, Math.Max(Math.Abs(i), Math.Abs(j)));
                        }
                    }
                }

                if (minDist < res.Length)
                {
                    res[minDist]++;
                    if (res[minDist] > 3)
                    {
                        return false;
                    }
                }

                node = par[node];
            }

            return true;
        }

        private static double WeightColor(double x, Configuration config)
        {
            if (x > config.ConnectionFinderUpperBound)
            {
                return 0.0;
            }

            if (x < config.ConnectionFinderLowerBound)
            {
                return 1e9;
            }

            x = 1 - ((x - config.ConnectionFinderLowerBound) / (config.ConnectionFinderUpperBound - config.ConnectionFinderLowerBound));

            return config.ConnectionFinderBlackPixelWeight * Math.Pow(x, config.ConnectionFinderWeightExp);
        }

        private static double WeightAngle(int prevx, int prevy, int startx, int starty, int x, int y, Configuration config)
        {
            var angle = GeometryHelper.GetAngle(prevx, prevy, startx, starty, x, y) / Math.PI * 180.0;

            if ((x == startx && y == starty) || (prevx == startx && prevy == starty))
            {
                angle = 0.0;
            }

            if (angle > config.ConnectionFinderAngleUpperBound)
            {
                return 1e9;
            }

            if (angle < config.ConnectionFinderAngleLowerBound)
            {
                return 0.0;
            }

            angle = (angle - config.ConnectionFinderAngleLowerBound) / (config.ConnectionFinderAngleUpperBound - config.ConnectionFinderAngleLowerBound);

            return config.ConnectionFinderAngleWeight * Math.Pow(angle, config.ConnectionFinderAngleWeightExp);
        }

        private static void GetApproximatePrevNodeCoordinates(int i, ImageGraph graph, int maxCnt, out int resX, out int resY)
        {
            var queue = new Queue<int>();
            var used = new HashSet<int>();
            queue.Enqueue(i);
            used.Add(i);

            resX = resY = 0;
            int cnt = 0;
            graph.GetCoordinates(i, out var startx, out var starty);

            while (queue.Count != 0 && cnt < maxCnt)
            {
                var node = queue.Dequeue();
                graph.GetCoordinates(node, out var x, out var y);
                resX += x;
                resY += y;
                cnt++;

                foreach (var to in graph.GetConnections(node))
                {
                    if (!used.Contains(to))
                    {
                        queue.Enqueue(to);
                        used.Add(to);
                    }
                }
            }

            if (cnt != 1)
            {
                resX -= startx;
                resY -= starty;
                cnt--;
            }

            resX = (int)(resX * 1.0 / cnt);
            resY = (int)(resY * 1.0 / cnt);
        }

        private static bool IsNearTheEdge(int x, int y, int width, int height)
        {
            return x == 0 || x == width - 1 || y == 0 || y == height - 1;
        }
    }
}