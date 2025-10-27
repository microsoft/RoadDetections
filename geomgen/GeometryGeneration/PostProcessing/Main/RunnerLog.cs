// <copyright file="RunnerLog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace PostProcessing.Main
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class RunnerLog
    {
        private readonly List<KeyValuePair<string, string>> log;

        public RunnerLog()
        {
            this.log = new List<KeyValuePair<string, string>>();
        }

        public RunnerLog Abort(string moduleName, Stopwatch stopwatch, double progress)
        {
            lock (this.log)
            {
                this.log.Add(new KeyValuePair<string, string>(moduleName, "Running for too long " + stopwatch.ElapsedMilliseconds + ". " + "Stopped at " + string.Format("{0:0.00}", progress) + "%."));

                if (Constants.Local)
                {
#pragma warning disable CS0162 // Unreachable code detected
                    Console.WriteLine("\tRunnerLog: " + this.log.Last().Key + "=" + this.log.Last().Value);
#pragma warning restore CS0162 // Unreachable code detected
                }
            }

            return this;
        }

        public RunnerLog Add(string moduleName, Stopwatch stopwatch)
        {
            lock (this.log)
            {
                this.log.Add(new KeyValuePair<string, string>(moduleName, "Took " + stopwatch.ElapsedMilliseconds + " ms."));

                if (Constants.Local)
                {
#pragma warning disable CS0162 // Unreachable code detected
                    Console.WriteLine("\tRunnerLog: " + this.log.Last().Key + "=" + this.log.Last().Value);
#pragma warning restore CS0162 // Unreachable code detected
                }
            }

            return this;
        }

        public RunnerLog Add(string moduleName, string message)
        {
            lock (this.log)
            {
                this.log.Add(new KeyValuePair<string, string>(moduleName, message));

                if (Constants.Local)
                {
#pragma warning disable CS0162 // Unreachable code detected
                    Console.WriteLine("\tRunnerLog: " + this.log.Last().Key + "=" + this.log.Last().Value);
#pragma warning restore CS0162 // Unreachable code detected
                }
            }

            return this;
        }

        public override string ToString()
        {
            lock (this.log)
            {
                return string.Join("|", this.log.Select(kvp => kvp.Key + "=" + kvp.Value));
            }
        }
    }
}
