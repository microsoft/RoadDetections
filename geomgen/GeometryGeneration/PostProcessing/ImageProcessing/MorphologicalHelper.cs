// <copyright file="MorphologicalHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.ImageProcessing
{
    using System;
    using ImageUtils.Images;
    using Utils;

    public static class MorphologicalHelper
    {
        /// <summary>
        /// Performs "Opening" operation with the image
        /// https://opencv-python-tutroals.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_morphological_ops/py_morphological_ops.html.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static BlackWhiteImage Opening(BlackWhiteImage img, BinaryImage kernel)
        {
            var res = Erosion(img, kernel);
            res = Dilation(res, kernel);

            return res;
        }

        /// <summary>
        /// Performs "Closing" operation with the image
        /// https://opencv-python-tutroals.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_morphological_ops/py_morphological_ops.html.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static BlackWhiteImage Closing(BlackWhiteImage img, BinaryImage kernel)
        {
            var res = Dilation(img, kernel);
            res = Erosion(res, kernel);

            return res;
        }

        public static BlackWhiteImage Averaging(BlackWhiteImage img, BinaryImage kernel)
        {
            BlackWhiteImage res = new BlackWhiteImage(img.Width, img.Height);

            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    res.Data[i, j] = Average(img, i, j, kernel);
                }
            }

            return res;
        }

        /// <summary>
        /// Performs "Erosion" operation with the image
        /// https://opencv-python-tutroals.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_morphological_ops/py_morphological_ops.html.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static BlackWhiteImage Erosion(BlackWhiteImage img, BinaryImage kernel)
        {
            BlackWhiteImage res = new BlackWhiteImage(img.Width, img.Height);

            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    res.Data[i, j] = Minimum(img, i, j, kernel);
                }
            }

            return res;
        }

        /// <summary>
        /// Performs "Dilation" operation with the image
        /// https://opencv-python-tutroals.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_morphological_ops/py_morphological_ops.html.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static BlackWhiteImage Dilation(BlackWhiteImage img, BinaryImage kernel)
        {
            var res = new BlackWhiteImage(img.Width, img.Height);

            for (int i = 0; i < res.Width; i++)
            {
                for (int j = 0; j < res.Height; j++)
                {
                    res.Data[i, j] = Maximum(img, i, j, kernel);
                }
            }

            return res;
        }

        public static BinaryImage Kernel(int size)
        {
            var res = new BinaryImage(size, size);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    res.Data[i, j] = true;
                }
            }

            return res;
        }

        private static int Minimum(BlackWhiteImage img, int starti, int startj, BinaryImage kernel)
        {
            return ApplyKernel(img, starti, startj, kernel, -1);
        }

        private static int Maximum(BlackWhiteImage img, int starti, int startj, BinaryImage kernel)
        {
            return ApplyKernel(img, starti, startj, kernel, 1);
        }

        private static int Average(BlackWhiteImage img, int starti, int startj, BinaryImage kernel)
        {
            if (kernel.Width % 2 == 0 || kernel.Height % 2 == 0)
            {
                throw new Exception("Kernel size must be an odd number");
            }

            int di = (kernel.Width - 1) / 2;
            int dj = (kernel.Height - 1) / 2;

            double sum = 0;
            int cnt = 0;
            bool isSet = false;
            for (int i = 0; i < kernel.Width; i++)
            {
                for (int j = 0; j < kernel.Height; j++)
                {
                    if (kernel.Data[i, j] && img.CheckCoordinates(starti + i - di, startj + j - dj))
                    {
                        sum += img.Data[starti + i - di, startj + j - dj];
                        cnt++;
                        isSet = true;
                    }
                }
            }

            if (!isSet)
            {
                throw new Exception("Intersection of image [starti, startj] and kernel is empty");
            }

            return GenericHelper.Round(sum / cnt);
        }

        private static int ApplyKernel(BlackWhiteImage img, int starti, int startj, BinaryImage kernel, int dominateCompareValue)
        {
            if (kernel.Width % 2 == 0 || kernel.Height % 2 == 0)
            {
                throw new Exception("Kernel size must be an odd number");
            }

            int di = (kernel.Width - 1) / 2;
            int dj = (kernel.Height - 1) / 2;

            int res = 0;
            bool isSet = false;
            for (int i = 0; i < kernel.Width; i++)
            {
                for (int j = 0; j < kernel.Height; j++)
                {
                    if (kernel.Data[i, j] && img.CheckCoordinates(starti + i - di, startj + j - dj))
                    {
                        if (!isSet || img.Data[starti + i - di, startj + j - dj].CompareTo(res) == dominateCompareValue)
                        {
                            res = img.Data[starti + i - di, startj + j - dj];
                            isSet = true;
                        }
                    }
                }
            }

            if (!isSet)
            {
                throw new Exception("Intersection of image [starti, startj] and kernel is empty");
            }

            return res;
        }
    }
}
