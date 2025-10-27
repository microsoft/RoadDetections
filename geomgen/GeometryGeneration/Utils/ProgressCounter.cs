// <copyright file="ProgressCounter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Utils
{
    using System;
    using System.IO;
    using System.Linq;

    public class ProgressCounter
    {
        private readonly int totalCount;
        private int currentCount;
        private int lastOutput;
        private int lastBucket;

        public ProgressCounter(string path, int mul = 1)
        {
            this.totalCount = File.ReadLines(path).Count() * mul;
            this.currentCount = 0;
            this.lastOutput = -1;
            this.lastBucket = 0;
        }

        public ProgressCounter(int total)
        {
            this.totalCount = total;
            this.currentCount = 0;
            this.lastOutput = -1;
            this.lastBucket = 0;
        }

        public void Update(string message = null)
        {
            this.currentCount++;
            var prc = GenericHelper.Round(this.currentCount * 100.0 / this.totalCount);

            if (message != null)
            {
                this.lastOutput = prc;
                this.lastBucket = prc / 33;
                Console.WriteLine(this.lastOutput + "\t" + message);
            }
            else
            {
                if (prc > this.lastOutput)
                {
                    this.lastOutput = prc;
                    Console.Write(this.lastOutput + " ");
                }

                if (prc / 33 > this.lastBucket)
                {
                    this.lastBucket = prc / 33;
                    Console.WriteLine();
                }
            }
        }
    }
}
