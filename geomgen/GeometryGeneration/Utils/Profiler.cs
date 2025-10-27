// <copyright file="Profiler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class Profiler
    {
        private readonly Stopwatch stopwatch;
        private readonly Dictionary<string, long> timeSpent;
        private readonly Dictionary<string, long> timestamp;

        public Profiler()
        {
            this.stopwatch = new Stopwatch();
            this.timeSpent = new Dictionary<string, long>();
            this.timestamp = new Dictionary<string, long>();

            this.stopwatch.Start();
        }

        public void Start(string name)
        {
            if (this.timestamp.ContainsKey(name))
            {
                this.timestamp[name] = this.stopwatch.ElapsedMilliseconds;
            }
            else
            {
                this.timestamp.Add(name, this.stopwatch.ElapsedMilliseconds);
            }
        }

        public void Stop(string name)
        {
            if (!this.timestamp.ContainsKey(name))
            {
                throw new Exception("No '" + name + "' found in Profiler records");
            }

            if (this.timeSpent.ContainsKey(name))
            {
                this.timeSpent[name] += this.stopwatch.ElapsedMilliseconds - this.timestamp[name];
            }
            else
            {
                this.timeSpent[name] = this.stopwatch.ElapsedMilliseconds - this.timestamp[name];
            }
        }

        public string GetTimeSpentMessage(string name)
        {
            if (this.timeSpent.ContainsKey(name))
            {
                return name + " took " + this.timeSpent[name] + " ms";
            }
            else
            {
                return name + " took " + 0 + " ms";
            }
        }
    }
}
