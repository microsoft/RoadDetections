// <copyright file="PostProcessor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using ImageUtils.Images;
    using PostProcessing.Annealing;
    using PostProcessing.ImageProcessing;

    public static class PostProcessor
    {
        public static PostProcessorResult Run(BlackWhiteImage grayImgInit, BlackWhiteImage binImgInit, ref int padding, Configuration config, string quadkey, int resolution, string path = "")
        {
            if (config.FullTrueBasicFalse)
            {
                return RunFull(grayImgInit, binImgInit, ref padding, config, quadkey, resolution, path);
            }
            else
            {
                return RunAsIs(grayImgInit, binImgInit, ref padding, config, quadkey, resolution, path);
            }
        }

        private static PostProcessorResult RunFull(BlackWhiteImage grayImgInit, BlackWhiteImage binImgInit, ref int padding, Configuration config, string quadkey, int resolution, string path = "")
        {
            var runnerLog = new RunnerLog();
            RunMorphology(ref grayImgInit, ref binImgInit, config, out var qualGryImg, out var qualBinImg, runnerLog, path);
            RunThinning(ref qualGryImg, ref qualBinImg, ref padding, config, out var thnImg, runnerLog, path); // 50% of padding is cut from the image here and padding object's values are reduced to the corresponding value
            RunGraphConstruction(ref thnImg, out var graph, runnerLog);
            RunConnectionFinder(ref graph, ref qualGryImg, ref thnImg, config, runnerLog, path);
            RunRamerDouglasPeucker(ref graph, ref qualGryImg, config, quadkey, resolution, runnerLog, path);
            LockPadding(ref thnImg, ref qualGryImg, ref graph, padding, config, runnerLog);
            RunAnnealing(ref graph, ref qualGryImg, ref qualBinImg, padding, config, out var annealingResult, runnerLog, path);

            return new PostProcessorResult { MatchScore = annealingResult.GetMatchScore(), Lines = annealingResult.GetLines(quadkey), Image = annealingResult.GetUncutImage(), RunnerLog = runnerLog };
        }

        private static PostProcessorResult RunAsIs(BlackWhiteImage grayImg, BlackWhiteImage predBinImg, ref int padding, Configuration config, string quadkey, int resolution, string path = "")
        {
            var runnerLog = new RunnerLog();
            var watch = new Stopwatch();
            watch.Start();

            Output(grayImg, path + "1.img.png", watch);

            var qualImg = grayImg;

            Output(qualImg, path + "2.img-qual.png", watch);

            var binImg = new BinaryImage(predBinImg, ImageUtils.Constants.ImageBinaryThreshold);

            Output(binImg, path + "3.img-bin.png", watch);

            var thnImg = Thinning.Apply(binImg);
            thnImg = thnImg.CutPadding(padding, config.ThinningPaddingRatio);
            qualImg = qualImg.CutPadding(padding, config.ThinningPaddingRatio);
            binImg = binImg.CutPadding(padding, config.ThinningPaddingRatio);
            padding /= config.ThinningPaddingRatio;
            Output(thnImg, path + "4.img-thin.png", watch);

            var graph = new ImageGraph(thnImg);

            Output(graph.ToImage(Color.White, 0), path + "5.connect.png", watch);

            graph = new RamerDouglasPeucker(graph, config.RamerDouglasPeuckerMinSegmentDist, config.RamerDouglasPeuckerMinCurviness, quadkey, resolution).Run();

            var unrefPaddingInfo = padding;
            var annealingResult = AbstractAnnealing.Run(graph, qualImg, 0, (aGraph, anImg) => new RealAnnealing(aGraph, anImg, unrefPaddingInfo, config, runnerLog));
            Output(annealingResult.GetGraph().ToImage(new RgbImage(grayImg), 0), path + "6.img-annealed-graph.png", watch);
            Output(annealingResult.DrawOn(new RgbImage(grayImg)), path + "7.img-annealed-graph.png", watch);

            watch.Stop();
            runnerLog.Add("PostProcessor.RunAsIs", watch);

            return new PostProcessorResult { MatchScore = annealingResult.GetMatchScore(), Lines = annealingResult.GetLines(quadkey), RunnerLog = runnerLog };
        }

        private static void RunMorphology(ref BlackWhiteImage grayImgInit, ref BlackWhiteImage binImgInit, Configuration config, out BlackWhiteImage qualGryImg, out BinaryImage qualBinImg, RunnerLog runnerLog, string path)
        {
            var watch = new Stopwatch();

            watch.Start();

            Output(grayImgInit, path + "1.img.png", watch);

            qualGryImg = grayImgInit;
            qualGryImg = MorphologicalHelper.Closing(qualGryImg, MorphologicalHelper.Kernel(config.MorphologicalKernelSize));
            qualGryImg = MorphologicalHelper.Opening(qualGryImg, MorphologicalHelper.Kernel(config.MorphologicalKernelSize));

            Output(qualGryImg, path + "1.img-qual.png", watch);

            Output(binImgInit, path + "2.bin.png", watch);

            binImgInit = MorphologicalHelper.Closing(binImgInit, MorphologicalHelper.Kernel(config.MorphologicalKernelSize));
            binImgInit = MorphologicalHelper.Opening(binImgInit, MorphologicalHelper.Kernel(config.MorphologicalKernelSize));
            qualBinImg = new BinaryImage(binImgInit, ImageUtils.Constants.ImageBinaryThreshold);
            watch.Stop();

            Output(qualBinImg, path + "2.bin-qual.png", watch);

            runnerLog.Add("PostProcessor.RunMorphology", watch);
        }

        private static void RunThinning(ref BlackWhiteImage qualGryImg, ref BinaryImage qualBinImg, ref int padding, Configuration config, out BinaryImage thnImg, RunnerLog runnerLog, string path)
        {
            var watch = new Stopwatch();

            watch.Start();
            thnImg = Thinning.Apply(qualBinImg);
            ////thnImg = ThinningPSI.Apply(qualGryImg);
            thnImg = thnImg.CutPadding(padding, config.ThinningPaddingRatio);
            qualGryImg = qualGryImg.CutPadding(padding, config.ThinningPaddingRatio);
            qualBinImg = qualBinImg.CutPadding(padding, config.ThinningPaddingRatio);
            padding /= config.ThinningPaddingRatio;
            watch.Stop();

            Output(RgbImage.CombineWithGray(new RgbImage(thnImg), qualGryImg), path + "4.img-thin.png", watch);

            runnerLog.Add("PostProcessor.RunThinning", watch);
        }

        private static void RunGraphConstruction(ref BinaryImage thnImg, out ImageGraph graph, RunnerLog runnerLog)
        {
            var watch = new Stopwatch();

            watch.Start();
            graph = new ImageGraph(thnImg);
            watch.Stop();

            runnerLog.Add("PostProcessor.RunGraphConstruction", watch);
        }

        private static void RunConnectionFinder(ref ImageGraph graph, ref BlackWhiteImage qualGryImg, ref BinaryImage thnImg, Configuration config, RunnerLog runnerLog, string path)
        {
            var watch = new Stopwatch();

            watch.Start();
            graph = ConnectionFinder.Run(graph, qualGryImg, thnImg, out var outImg, config, runnerLog);
            watch.Stop();

            Output(RgbImage.CombineWithGray(outImg, qualGryImg), path + "5.connect.png", watch);

            runnerLog.Add("PostProcessor.RunConnectionFinder", watch);
        }

        private static void RunRamerDouglasPeucker(ref ImageGraph graph, ref BlackWhiteImage qualGryImg, Configuration config, string quadkey, int resolution, RunnerLog runnerLog, string path)
        {
            var watch = new Stopwatch();

            watch.Start();
            graph = new RamerDouglasPeucker(graph, config.RamerDouglasPeuckerMinSegmentDist, config.RamerDouglasPeuckerMinCurviness, quadkey, resolution).Run();
            watch.Stop();

            Output(RgbImage.CombineWithGray(graph.ToImage(Color.Red, 0), qualGryImg), path + "6.douglas-peucker.png", watch);

            runnerLog.Add("PostProcessor.RunRamerDouglasPeucker", watch);
        }

        private static void LockPadding(ref BinaryImage thnImg, ref BlackWhiteImage qualGryImg, ref ImageGraph graph, int padding, Configuration config, RunnerLog runnerLog)
        {
            var watch = new Stopwatch();
            watch.Start();

            if (config.AnnealingLockPadding && config.AnnealingBreakOnOriginalImageBoundaries)
            {
                var edges = graph.GetEdges();
                edges = AnnealingHelper.BreakOnOriginalImageBoundaries(edges, qualGryImg.Width, qualGryImg.Height, padding);
                graph = new ImageGraph(edges, thnImg.Width, thnImg.Height);
            }

            watch.Stop();

            runnerLog.Add("PostProcessor.LockPadding", watch);
        }

        private static void RunAnnealing(ref ImageGraph graph, ref BlackWhiteImage qualGryImg, ref BinaryImage qualBinImg, int padding, Configuration config, out AbstractAnnealing annealingResult, RunnerLog runnerLog, string path)
        {
            var watch = new Stopwatch();

            watch.Start();

            Output(RgbImage.CombineWithGray(graph.ToImage(Color.Red, 0), qualGryImg), path + "7.before-annealing.graph.png", watch);

            annealingResult = AbstractAnnealing.Run(graph, qualGryImg, config.AnnealingRuns, (aGraph, anImg) => new RealAnnealing(aGraph, anImg, padding, config, runnerLog));
            ////annealingResult = AbstractAnnealing.Run(graph, qualGryImg, config.AnnealingRuns, (aGraph, anImg) => new SimpleAnnealing(aGraph, anImg, paddingInfo, config, runnerLog));
            watch.Stop();

            Output(annealingResult.GetGraph().ToImage(new RgbImage(qualGryImg), 0).AddBorder(Color.Yellow, padding), path + "8.img-annealed-graph.png", watch);
            Output(annealingResult.GetGraph().ToImage(new RgbImage(qualBinImg), 0).AddBorder(Color.Yellow, padding), path + "8.bin-annealed-graph.png", watch);
            Output(annealingResult.DrawOn(new RgbImage(qualGryImg)).AddBorder(Color.Yellow, padding), path + "9.img-annealed-graph.png", watch);

            runnerLog.Add("PostProcessor.RunAnnealing", watch);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used when debugging and Constants.Local is set to true")]
        private static void Output(BaseImage image, string path, Stopwatch watch)
        {
            if (Constants.Local)
            {
#pragma warning disable CS0162 // Unreachable code detected
                watch.Stop();
                image.Save(path);
                watch.Start();
#pragma warning restore CS0162 // Unreachable code detected
            }
        }
    }
}
