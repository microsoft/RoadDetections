// <copyright file="ThinningPSI.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.ImageProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GeoUtils;
    using ImageUtils.Images;
    using PostProcessing.Main;
    using PostProcessing.Search;
    using Utils;

    public static class ThinningPSI
    {
        /// <summary>
        /// Skeletonization of Grayscale Character Images Using Pixel Superiority Index
        /// https://koasas.kaist.ac.kr/bitstream/10203/17398/1/Skeletonization%20Of%20Grayscale%20Character%20Images%20Using%20Pixel%20Superiority%20Index%c2%a0.pdf.
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static BinaryImage Apply(BlackWhiteImage pred)
        {
            var img = new BlackWhiteImage(pred);
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    if (img.Data[i, j] < 64)
                    {
                        img.Data[i, j] = 0;
                    }
                }
            }

            img = MorphologicalHelper.Averaging(img, MorphologicalHelper.Kernel(3));
            img = MorphologicalHelper.Averaging(img, MorphologicalHelper.Kernel(3));
            img = MorphologicalHelper.Averaging(img, MorphologicalHelper.Kernel(3));

            var psi = new BlackWhiteImage(img.Width, img.Height);
            for (int i = 0; i < psi.Width; i++)
            {
                for (int j = 0; j < psi.Height; j++)
                {
                    if (img.Data[i, j] > 0)
                    {
                        psi.Data[i, j] = GetPSI(img, i, j);
                    }
                }
            }

            var psi5 = new BinaryImage(img.Width, img.Height);
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    psi5.Data[i, j] = psi.Data[i, j] >= 5;
                }
            }

            var res = Iteration1(img, psi);
            res = Iteration2(res, psi);

            return res;
        }

        private static BinaryImage Iteration1(BlackWhiteImage img, BlackWhiteImage psi)
        {
            var res = new BinaryImage(img.Width, img.Height);
            var pxs = new List<Tuple<int, int>>[9];
            for (int i = 0; i < pxs.Length; i++)
            {
                pxs[i] = new List<Tuple<int, int>>();
            }

            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    res.Data[i, j] = psi.Data[i, j] >= 6;
                    pxs[psi.Data[i, j]].Add(Tuple.Create(i, j));
                }
            }

            var rand = new Random();
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int psi_to_delete = 6; psi_to_delete <= 8; psi_to_delete++)
                {
                    var queue = new List<Tuple<int, int>>();
                    foreach (var (i, j) in pxs[psi_to_delete])
                    {
                        if (res.Data[i, j] && IsContourPixel(res, i, j))
                        {
                            queue.Add(Tuple.Create(i, j));
                        }
                    }

                    foreach (var (i, j) in RandomHelper.Shuffle(queue.ToArray(), rand))
                    {
                        if (CanBeDeleted(res, i, j))
                        {
                            res.Data[i, j] = false;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        break;
                    }
                }
            }

            res = new BinaryImage(MorphologicalHelper.Closing(new BlackWhiteImage(res), MorphologicalHelper.Kernel(5)), ImageUtils.Constants.ImageBinaryThreshold);

            return res;
        }

        private static BinaryImage Iteration2(BinaryImage res, BlackWhiteImage psi)
        {
            var dsu = new Stitching.DSU(res.Width * res.Height);
            var graph = new ImageGraph(res.Width, res.Height);
            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    if (res.Data[i, j])
                    {
                        for (int p = 0; p < GeometryHelper.DIJ.Length / 2; p++)
                        {
                            int ii = i + GeometryHelper.DIJ[p, 0];
                            int jj = j + GeometryHelper.DIJ[p, 1];
                            if (res.CheckCoordinates(ii, jj) && res.Data[ii, jj])
                            {
                                dsu.Union((i * res.Height) + j, (ii * res.Height) + jj);
                            }
                        }
                    }

                    if (psi.Data[i, j] >= 5)
                    {
                        for (int p = 0; p < GeometryHelper.DIJ.Length / 2; p++)
                        {
                            int ii = i + GeometryHelper.DIJ[p, 0];
                            int jj = j + GeometryHelper.DIJ[p, 1];
                            if (res.CheckCoordinates(ii, jj) && psi.Data[ii, jj] >= 5)
                            {
                                graph.AddEdge(graph.GetId(i, j), graph.GetId(ii, jj), true);
                            }
                        }
                    }
                }
            }

            var comps = dsu.GetComponents();
            comps = comps.Where(comp => res.Data[comp.First() / res.Height, comp.First() % res.Height]).ToList();

            //// compGraph[i][j] is a closest path between components i and j. Distance and the path
            var resPaths = new List<int[]>();
            int cnt = 0;
            foreach (var comp in comps)
            {
                Console.WriteLine(cnt++ + "\t" + comps.Count + ":" + comp.Count());
                int parent = dsu.GetParent(comp.First());

                var dists = Dijkstra.Run(graph, comp, 20.0);
                var closestDistToComp = new Dictionary<int, Tuple<double, int>>();
                for (int i = 0; i < dists.Dist.Length; i++)
                {
                    int ii = i / res.Height, jj = i % res.Height;
                    if (res.Data[ii, jj] && Dijkstra.IsReachable(dists.Dist[i]) && CountNeighbors(res, ii, jj) <= 1)
                    {
                        var p = dsu.GetParent(i);
                        if (parent != p)
                        {
                            if (closestDistToComp.ContainsKey(p))
                            {
                                if (closestDistToComp[p].Item1 > dists.Dist[i])
                                {
                                    closestDistToComp[p] = Tuple.Create(dists.Dist[i], i);
                                }
                            }
                            else
                            {
                                closestDistToComp.Add(p, Tuple.Create(dists.Dist[i], i));
                            }
                        }
                    }
                }

                var cons = new Dictionary<int, Tuple<double, int[]>>();
                foreach (var kvp in closestDistToComp)
                {
                    resPaths.Add(Dijkstra.GetPath(dists, kvp.Value.Item2));
                }
            }

            foreach (var path in resPaths)
            {
                foreach (var i in path)
                {
                    int ii = i / res.Height, jj = i % res.Height;
                    res.Data[ii, jj] = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Contour pixel is defined as a pixel having at least one white 4-neighbour.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private static bool IsContourPixel(BinaryImage img, int i, int j)
        {
            for (int p = 0; p < GeometryHelper.DIJ.Length / 2; p += 2)
            {
                int ii = i + GeometryHelper.DIJ[p, 0];
                int jj = j + GeometryHelper.DIJ[p, 1];
                if (img.CheckCoordinates(ii, jj) && !img.Data[ii, jj])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If deleting this pixel doesnt change 8-connectivity of it's neighbors, then it can be deleted.
        /// </summary>
        private static bool CanBeDeleted(BinaryImage img, int i, int j)
        {
            int nulls = 8 - CountNeighbors(img, i, j);

            if (nulls >= 7)
            {
                return false;
            }

            int size = GeometryHelper.DIJ.Length / 2;
            var dsu = new Stitching.DSU(size);
            for (int p = 0; p < size; p++)
            {
                int id1 = p;
                int ii1 = i + GeometryHelper.DIJ[id1, 0];
                int jj1 = j + GeometryHelper.DIJ[id1, 1];

                int id2 = (p + 1) % size;
                int ii2 = i + GeometryHelper.DIJ[id2, 0];
                int jj2 = j + GeometryHelper.DIJ[id2, 1];

                if (img.CheckCoordinates(ii1, jj1) && img.CheckCoordinates(ii2, jj2) && img.Data[ii1, jj1] && img.Data[ii2, jj2])
                {
                    dsu.Union(id1, id2);
                }
            }

            for (int p = 0; p < size; p += 2)
            {
                int id1 = p;
                int ii1 = i + GeometryHelper.DIJ[id1, 0];
                int jj1 = j + GeometryHelper.DIJ[id1, 1];

                int id2 = (p + 2) % size;
                int ii2 = i + GeometryHelper.DIJ[id2, 0];
                int jj2 = j + GeometryHelper.DIJ[id2, 1];

                if (img.CheckCoordinates(ii1, jj1) && img.CheckCoordinates(ii2, jj2) && img.Data[ii1, jj1] && img.Data[ii2, jj2])
                {
                    dsu.Union(id1, id2);
                }
            }

            return dsu.GetComponents().Count - nulls == 1;
        }

        private static int GetPSI(BlackWhiteImage img, int i, int j)
        {
            int res = 0;
            for (int p = 0; p < GeometryHelper.DIJ.Length / 2; p++)
            {
                int ii = i + GeometryHelper.DIJ[p, 0];
                int jj = j + GeometryHelper.DIJ[p, 1];
                if (img.CheckCoordinates(ii, jj) && img.Data[ii, jj] <= img.Data[i, j])
                {
                    res++;
                }
            }

            return res;
        }

        private static int CountNeighbors(BinaryImage img, int i, int j)
        {
            int res = 0;
            for (int p = 0; p < GeometryHelper.DIJ.Length / 2; p++)
            {
                int ii = i + GeometryHelper.DIJ[p, 0];
                int jj = j + GeometryHelper.DIJ[p, 1];
                if (img.CheckCoordinates(ii, jj) && img.Data[ii, jj])
                {
                    res++;
                }
            }

            return res;
        }
    }
}
