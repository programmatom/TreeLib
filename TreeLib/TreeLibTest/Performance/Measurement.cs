/*
 *  Copyright © 2016 Thomas R. Lawrence
 * 
 *  GNU Lesser General Public License
 * 
 *  This file is part of TreeLib
 * 
 *  TreeLib is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program. If not, see <http://www.gnu.org/licenses/>.
 * 
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TreeLibTest
{
    public static class Measurement
    {
        public abstract class PerfTest
        {
            public abstract void UntimedPrepare();
            public abstract void TimedIteration();
        }

        public delegate PerfTest MakePerfTest();


        public class Result
        {
            public const int Version = 1;

            private const int Fields = 7;

            public string label;
            public double median;
            public double average;
            public double stability;
            public int iterations;
            public double sum;
            public double variance;

            public double[] data;

            private const string NumberFormat = "F3";

            public static Result FromString(string line)
            {
                string[] parts = line.Split(new char[] { '\t' });
                if (!(parts.Length >= Fields))
                {
                    throw new ArgumentException();
                }
                double[] data = new double[parts.Length - Fields];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = Double.Parse(parts[i + Fields]);
                }
                return new Result
                {
                    label = parts[0],
                    median = Double.Parse(parts[1]),
                    average = Double.Parse(parts[2]),
                    stability = Double.Parse(parts[3]),
                    iterations = Int32.Parse(parts[4]),
                    sum = Double.Parse(parts[5]),
                    variance = Double.Parse(parts[6]),

                    data = data,
                };
            }

            public override string ToString()
            {
                string[] dataText = new string[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    dataText[i] = data[i].ToString(NumberFormat);
                }
                return FormatLine(
                    new string[]
                    {
                        label,
                        median.ToString(NumberFormat),
                        average.ToString(NumberFormat),
                        stability.ToString(NumberFormat),
                        iterations.ToString(),
                        sum.ToString(NumberFormat),
                        variance.ToString(NumberFormat),
                    },
                    dataText);
            }

            private static string FormatLine(string[] fields, string[] data)
            {
                Debug.Assert(fields.Length == Fields);
                List<string> total = new List<string>(fields);
                total.AddRange(data);
                return String.Join("\t", total);
            }

            public static string Header
            {
                get
                {
                    return FormatLine(
                        new string[] { "label", "median", "average", "stability", "iterations", "sum", "variance", "data" },
                        new string[0]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static Result RunTest(string label, MakePerfTest createTest, int? requestedIterations, int multiplier)
        {
            const int DefaultIterations = 21;
            int iterations = requestedIterations.HasValue ? requestedIterations.Value : DefaultIterations;

            // warm-up
            if (iterations > 1)
            {
                PerfTest test = createTest();
                test.UntimedPrepare();
                test.TimedIteration();
                if (iterations > 5)
                {
                    test = createTest();
                    test.UntimedPrepare();
                    test.TimedIteration();
                }
            }

            double[] elapsed = new double[iterations];
            for (int i = 0; i < iterations; i++)
            {
                PerfTest test = createTest();
                test.UntimedPrepare();

                Stopwatch timer = Stopwatch.StartNew();
                for (int j = 0; j < multiplier; j++)
                {
                    test.TimedIteration();
                }
                timer.Stop();

                elapsed[i] = timer.ElapsedMilliseconds / 1000d;
            }

            Array.Sort(elapsed);

            double sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                sum += elapsed[i];
            }
            double average = sum / iterations;
            double variance = 0;
            double stability = 0;
            for (int i = 0; i < iterations; i++)
            {
                double deviation = elapsed[i] - average;
                variance += deviation * deviation;
                double sta = Math.Max(elapsed[i], average) / Math.Min(elapsed[i], average) - 1;
                stability += sta * sta;
            }
            variance /= iterations;
            stability = Math.Sqrt(stability / iterations);

            double median = ((iterations & 1) != 0)
                ? elapsed[iterations / 2]
                : (elapsed[(iterations - 1) / 2] + elapsed[(iterations - 1) / 2 + 1]) / 2;

            return new Result
            {
                label = label,
                median = median,
                average = average,
                stability = stability,
                iterations = iterations,
                sum = sum,
                variance = variance,

                data = elapsed,
            };
        }
    }
}
