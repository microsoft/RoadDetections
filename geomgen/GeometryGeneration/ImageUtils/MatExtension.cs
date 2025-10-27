// <copyright file="MatExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ImageUtils
{
    using System.Runtime.InteropServices;
    using Emgu.CV;

    /// <summary>
    /// https://stackoverflow.com/questions/32255440/how-can-i-get-and-set-pixel-values-of-an-emgucv-mat-image/32559496#32559496
    /// Simplified to use only DepthType.Cv8U.
    /// </summary>
    public static class MatExtension
    {
        public static byte GetValue(this Mat mat, int row, int col)
        {
            var value = new byte[1];
            Marshal.Copy(mat.DataPointer + (((row * mat.Cols) + col) * mat.ElementSize), value, 0, 1);
            return value[0];
        }

        public static void SetValue(this Mat mat, int row, int col, byte value)
        {
            Marshal.Copy(new byte[] { value }, 0, mat.DataPointer + (((row * mat.Cols) + col) * mat.ElementSize), 1);
        }
    }
}
