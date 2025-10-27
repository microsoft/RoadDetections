// <copyright file="TopologyMetric.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PostProcessing.Helpers;
    using PostProcessing.Main;
    using PostProcessing.Search;
    using Utils;

    public class TopologyMetric : PrecisionRecall
    {
        private double groundTruthPathsDist;
        private double trueGroundTruthPathsDist;
        private double candidatePathsDist;
        private double trueCandidatePathsDist;

        public TopologyMetric(int groundTruthPathsDist, int trueGroundTruthPathsDist, int candidatePathsDist, int trueCandidatePathsDist)
        {
            this.groundTruthPathsDist = groundTruthPathsDist;
            this.trueGroundTruthPathsDist = trueGroundTruthPathsDist;
            this.candidatePathsDist = candidatePathsDist;
            this.trueCandidatePathsDist = trueCandidatePathsDist;
        }

        public TopologyMetric(ImageGraph candidate, ImageGraph groundTruth, Configuration config, string quadkey, int resolution, bool addMidPoints = false, bool normalize = true, bool minMaxRatio = false, string path = "")
        {
            this.groundTruthPathsDist = 0.0;
            this.trueGroundTruthPathsDist = 0.0;
            this.candidatePathsDist = 0.0;
            this.trueCandidatePathsDist = 0.0;

            var cGraph = new SparsePlanarGraph(candidate);
            var gGraph = new SparsePlanarGraph(groundTruth);

            this.Initialize(cGraph, gGraph, config, quadkey, resolution, addMidPoints, normalize, minMaxRatio, path);
        }

        public override double Precision => GenericHelper.CompareDouble(this.candidatePathsDist, 0) == 0 ? double.NaN : (1.0 - (this.trueCandidatePathsDist / this.candidatePathsDist));

        public override double Recall => GenericHelper.CompareDouble(this.groundTruthPathsDist, 0) == 0 ? double.NaN : (1.0 - (this.trueGroundTruthPathsDist / this.groundTruthPathsDist));

        public static PrecisionRecall Average(IEnumerable<TopologyMetric> list)
        {
            var res = new TopologyMetric(0, 0, 0, 0);
            foreach (var pr in list)
            {
                res.candidatePathsDist += pr.candidatePathsDist;
                res.trueCandidatePathsDist += pr.trueCandidatePathsDist;
                res.groundTruthPathsDist += pr.groundTruthPathsDist;
                res.trueGroundTruthPathsDist += pr.trueGroundTruthPathsDist;
            }

            return res;
        }

        private void Initialize(SparsePlanarGraph cGraph, SparsePlanarGraph gGraph, Configuration config, string quadkey, int resolution, bool addMidPoints = false, bool normalize = true, bool minMaxRatio = false, string path = "")
        {
            var pairsTuple = TopologyHelper.GetPairs(cGraph, gGraph, config.TopologyMaxDistToNode, config.TopologyMaxDist2ToEdge);

            var pairs1 = pairsTuple.Item1;
            var pairs2 = pairsTuple.Item2;

            var allpairs = new HashSet<Tuple<int, int>>();
            foreach (var tuple in pairs1)
            {
                allpairs.Add(tuple);
            }

            foreach (var tuple in pairs2)
            {
                allpairs.Add(tuple);
            }

            var pairs = allpairs.ToList();

            foreach (var pair in pairs)
            {
                cGraph.IsControlPoint[pair.Item1] = true;
                gGraph.IsControlPoint[pair.Item2] = true;
            }

            if (addMidPoints)
            {
                TopologyHelper.AddMidPoints(cGraph, gGraph, pairs, quadkey, resolution);

                foreach (var pair in pairs)
                {
                    cGraph.IsControlPoint[pair.Item1] = true;
                    gGraph.IsControlPoint[pair.Item2] = true;
                }
            }

            if (path != string.Empty)
            {
                DrawHelper.Draw(cGraph, gGraph, pairs, 1024, 33, 77).Save(path + "apls1-" + (addMidPoints ? "MidPoints" : string.Empty) + (normalize ? "Normalized" : string.Empty) + ".png");
                DrawHelper.Draw(cGraph, gGraph, pairs, 1024, 77, 33).Save(path + "apls2-" + (addMidPoints ? "MidPoints" : string.Empty) + (normalize ? "Normalized" : string.Empty) + ".png");
            }

            var cPaths = new DijkstraGraph(cGraph);
            var gPaths = new DijkstraGraph(gGraph);

            for (int i = 0; i < pairs.Count; i++)
            {
                for (int j = i + 1; j < pairs.Count; j++)
                {
                    var candidateDist = cPaths.GetDistance(pairs[i].Item1, pairs[j].Item1);
                    var groundTruthDist = gPaths.GetDistance(pairs[i].Item2, pairs[j].Item2);
                    if (!DijkstraGraph.IsUnreachableDistance(candidateDist) && !DijkstraGraph.IsUnreachableDistance(groundTruthDist))
                    {
                        if (minMaxRatio)
                        {
                            var mn = Math.Min(groundTruthDist, candidateDist);
                            var mx = Math.Max(groundTruthDist, candidateDist);
                            this.trueGroundTruthPathsDist += (mx - mn) / mx * (normalize ? groundTruthDist : 1.0);
                            this.trueCandidatePathsDist += (mx - mn) / mx * (normalize ? candidateDist : 1.0);
                        }
                        else
                        {
                            this.trueGroundTruthPathsDist += Math.Min(1.0, Math.Abs(candidateDist - groundTruthDist) / groundTruthDist) * (normalize ? groundTruthDist : 1.0);
                            this.trueCandidatePathsDist += Math.Min(1.0, Math.Abs(candidateDist - groundTruthDist) / candidateDist) * (normalize ? candidateDist : 1.0);
                        }

                        this.groundTruthPathsDist += normalize ? groundTruthDist : 1.0;
                        this.candidatePathsDist += normalize ? candidateDist : 1.0;
                    }
                    else if (DijkstraGraph.IsUnreachableDistance(candidateDist) && !DijkstraGraph.IsUnreachableDistance(groundTruthDist))
                    {
                        this.trueGroundTruthPathsDist += normalize ? groundTruthDist : 1.0;
                        this.groundTruthPathsDist += normalize ? groundTruthDist : 1.0;
                    }
                    else if (!DijkstraGraph.IsUnreachableDistance(candidateDist) && DijkstraGraph.IsUnreachableDistance(groundTruthDist))
                    {
                        this.trueCandidatePathsDist += normalize ? candidateDist : 1.0;
                        this.candidatePathsDist += normalize ? candidateDist : 1.0;
                    }
                }
            }
        }
    }
}