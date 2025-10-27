// <copyright file="Configuration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    public class Configuration
    {
        public static readonly Configuration Default = new Configuration
        {
            FullTrueBasicFalse = true,

            TimeLimitMsConnectionFinder = 5 * 60 * 1000,
            TimeLimitMsAnnealing = 5 * 60 * 1000,

            // Morphological
            ThinningPaddingRatio = 2,
            MorphologicalKernelSize = 3,

            // RamerDouglasPeucker
            RamerDouglasPeuckerMinSegmentDist = 5,
            RamerDouglasPeuckerMinCurviness = 1.0,

            // ConnectionFinder - dont change these numbers - threshold of an ML model depends on them
            ConnectionFinder_TreeClassifier_Threshold = 2.39535165, // 2.39535165 : P/R=93.3/62.8         2.833093 : P/R=95.3/55.6

            ConnectionFinderMaxGapConnection = 60000,
            ConnectionFinderEuclidFilter = 12,
            ConnectionFinderCurDistFilter = 2,
            ConnectionFinderRoadendAngleConnection = 90,
            ConnectionFinderJunctionAngleConnection = 60,

            ConnectionFinderBlackPixelWeight = 100,
            ConnectionFinderLowerBound = 32,
            ConnectionFinderUpperBound = 127,
            ConnectionFinderWeightExp = 2,
            ConnectionFinderFlyDistanceLimit = 350,

            ConnectionFinderAngleWeight = 25,
            ConnectionFinderAngleLowerBound = 15,
            ConnectionFinderAngleUpperBound = 90,
            ConnectionFinderAngleWeightExp = 2,

            // JunctionsFixer
            JunctionsMergeDistance = 4,

            // Annealing
            AnnealingMaxIterations = 800 * 1000,
            AnnealingImageMargin = 10,
            AnnealingMinDist2ToCheckAngle = 50,
            AnnealingStraightAngleThreshold = 177.5,
            RealAnnealingIterations = 225,
            RealAnnealingTemperatureCoefficient = 1.0 / (1 << 16),
            AnnealingRuns = 1,
            ArtifactLength = 2,
            Junction2AngleWeight = 300,
            Junction2AngleCurviness = 1,
            Junction3AngleWeight = 7,
            Junction3AngleCurviness = 100,
            AnnealingCheckAngleWhenMoveNode = false,
            AnnealingDeleteNoWidthEdges = false,
            AnnealingTryDeleteEdgesWithMinWidthAfterAnnealingIsFinished = false,
            MoveMidlines = true,
            UseMirrorMove = true,
            UseDoubleMove = false,
            AnnealingLockPadding = true,
            AnnealingBreakOnOriginalImageBoundaries = true,
            AnnealingCheckEdgeWidthBalance = true,
            AnnealingWholeLineSameWidth = true,
            MergeOnlySameWidthEdges = false,

            // TopologyMetric
            TopologyMaxDistToNode = 10,
            TopologyMaxDist2ToSimilarNode = 25 * 25,
            TopologyMaxDist2ToEdge = 10 * 10,

            // Stitching
            ProcessingPadding = 128,
            MaxPointsDistToStitchLines = 2,
        };

        public int TimeLimitMsConnectionFinder { get; private set; }

        public int TimeLimitMsAnnealing { get; private set; }

        // Morphological
        public int ThinningPaddingRatio { get; private set; }

        public int MorphologicalKernelSize { get; private set; }

        // RamerDouglasPeucker
        public int RamerDouglasPeuckerMinSegmentDist { get; private set; }

        public double RamerDouglasPeuckerMinCurviness { get; private set; }

        // ConnectionFinder
        public double ConnectionFinder_TreeClassifier_Threshold { get; private set; }

        public int ConnectionFinderMaxGapConnection { get; private set; }

        public double ConnectionFinderBlackPixelWeight { get; private set; }

        public double ConnectionFinderAngleWeight { get; private set; }

        public double ConnectionFinderAngleWeightExp { get; private set; }

        public double ConnectionFinderFlyDistanceLimit { get; private set; }

        public double ConnectionFinderLowerBound { get; private set; }

        public double ConnectionFinderUpperBound { get; private set; }

        public double ConnectionFinderAngleUpperBound { get; private set; }

        public double ConnectionFinderAngleLowerBound { get; private set; }

        public double ConnectionFinderWeightExp { get; private set; }

        public int ConnectionFinderEuclidFilter { get; private set; }

        public int ConnectionFinderCurDistFilter { get; private set; }

        public double ConnectionFinderRoadendAngleConnection { get; private set; }

        public double ConnectionFinderJunctionAngleConnection { get; private set; }

        // JunctionsFixer
        public int JunctionsMergeDistance { get; private set; }

        // Annealing
        public int AnnealingImageMargin { get; private set; }

        public int AnnealingMaxIterations { get; private set; }

        public int AnnealingMinDist2ToCheckAngle { get; private set; }

        public double AnnealingStraightAngleThreshold { get; private set; }

        public int RealAnnealingIterations { get; private set; }

        public double RealAnnealingTemperatureCoefficient { get; private set; }

        public bool AnnealingLockPadding { get; private set; }

        public bool AnnealingBreakOnOriginalImageBoundaries { get; private set; }

        public bool AnnealingCheckEdgeWidthBalance { get; private set; }

        public bool AnnealingWholeLineSameWidth { get; private set; }

        public bool AnnealingDeleteNoWidthEdges { get; private set; }

        public bool AnnealingTryDeleteEdgesWithMinWidthAfterAnnealingIsFinished { get; private set; }

        public int AnnealingRuns { get; private set; }

        public double ArtifactLength { get; private set; }

        public double Junction2AngleWeight { get; private set; }

        public double Junction2AngleCurviness { get; private set; }

        public double Junction3AngleWeight { get; private set; }

        public double Junction3AngleCurviness { get; private set; }

        public bool AnnealingCheckAngleWhenMoveNode { get; private set; }

        public bool MoveMidlines { get; private set; }

        public bool UseMirrorMove { get; private set; }

        public bool UseDoubleMove { get; private set; }

        public bool MergeOnlySameWidthEdges { get; private set; }

        // TopologyMetric
        public double TopologyMaxDistToNode { get; private set; }

        public int TopologyMaxDist2ToSimilarNode { get; private set; }

        public int TopologyMaxDist2ToEdge { get; private set; }

        // Stitching
        public int ProcessingPadding { get; private set; }

        public int MaxPointsDistToStitchLines { get; private set; }

        public bool FullTrueBasicFalse { get; private set; }
    }
}
