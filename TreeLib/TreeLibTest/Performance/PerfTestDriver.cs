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
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public static class PerfTestDriver
    {
        //public const int SmallNumTrials = 121;
        //public const int MediumNumTrials = 41;
        //public const int LargeNumTrials = 21;
        public const int SmallNumTrials = 51;
        public const int MediumNumTrials = 21;
        public const int LargeNumTrials = 11;

        private const int SmallCount = 10000;
        private const int MediumCount = 100000;
        private const int LargeCount = 1000000;


        //
        // Templates
        //

        public struct CreateTreeInfo<ITree>
        {
            public readonly string kind;
            public readonly TreeFactory<ITree> treeFactory;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public CreateTreeInfo(string kind, TreeFactory<ITree> treeFactory)
            {
                this.kind = kind;
                this.treeFactory = treeFactory;
            }
        }

        public class TreeFactory<ITree>
        {
            private readonly MakeTree makeTree;

            public delegate ITree MakeTree(int capacity);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TreeFactory(MakeTree makeTree)
            {
                this.makeTree = makeTree;
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public ITree Create(int capacity)
            {
                return makeTree(capacity);
            }
        }

        private class TestInfo<ITree>
        {
            public readonly string label;
            public readonly int multiplier;
            public readonly int count;
            public readonly int trials;
            public readonly PerfTestFactoryMaker makePerfTestFactory;

            public delegate Measurement.MakePerfTest PerfTestFactoryMaker(TreeFactory<ITree> treeFactory, int count);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TestInfo(string label, int multiplier, int count, int trials, PerfTestFactoryMaker makePerfTestFactory)
            {
                this.label = label;
                this.multiplier = multiplier;
                this.count = count;
                this.trials = trials;
                this.makePerfTestFactory = makePerfTestFactory;
            }
        }

        //
        // IOrderedMap
        //

        private readonly static CreateTreeInfo<IOrderedMap<int, int>>[] MapCreators = new CreateTreeInfo<IOrderedMap<int, int>>[]
        {
            new CreateTreeInfo<IOrderedMap<int, int>>(
                "AVLTreeMap",
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new AVLTreeMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            //new CreateTreeInfo<IOrderedMap<int, int>>(
            //    "AVLTreeArrayMap",
            //    new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new AVLTreeArrayMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IOrderedMap<int, int>>(
                "RedBlackTreeMap",
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new RedBlackTreeMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            //new CreateTreeInfo<IOrderedMap<int, int>>(
            //    "RedBlackTreeArrayMap",
            //    new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new RedBlackTreeArrayMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IOrderedMap<int, int>>(
                "SplayTreeMap",
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new SplayTreeMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            //new CreateTreeInfo<IOrderedMap<int, int>>(
            //    "SplayTreeArrayMap",
            //    new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new SplayTreeArrayMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IOrderedMap<int, int>>[] MapTests = new TestInfo<IOrderedMap<int, int>>[]
        {
            new TestInfo<IOrderedMap<int, int>>("Small", 15, SmallCount, SmallNumTrials, MakePerfTestMapMaker),
            new TestInfo<IOrderedMap<int, int>>("Med", 1, MediumCount, MediumNumTrials, MakePerfTestMapMaker),
            new TestInfo<IOrderedMap<int, int>>("Large", 1, LargeCount, LargeNumTrials, MakePerfTestMapMaker),
        };

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Measurement.MakePerfTest MakePerfTestMapMaker(TreeFactory<IOrderedMap<int, int>> treeFactory, int count)
        {
            return delegate () { return new PerfTestMap(delegate (int _count) { return treeFactory.Create(_count); }, count); };
        }

        //
        // IRankMap
        //

        private readonly static CreateTreeInfo<IRankMap<int, int>>[] RankMapCreators = new CreateTreeInfo<IRankMap<int, int>>[]
        {
            new CreateTreeInfo<IRankMap<int,int>>(
                "AVLTreeRankMap",
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new AVLTreeRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            //new CreateTreeInfo<IRankMap<int,int>>(
            //    "AVLTreeArrayRankMap",
            //    new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new AVLTreeArrayRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRankMap<int,int>>(
                "RedBlackTreeRankMap",
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new RedBlackTreeRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            //new CreateTreeInfo<IRankMap<int,int>>(
            //    "RedBlackTreeArrayRankMap",
            //    new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new RedBlackTreeArrayRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRankMap<int,int>>(
                "SplayTreeRankMap",
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new SplayTreeRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            //new CreateTreeInfo<IRankMap<int,int>>(
            //    "SplayTreeArrayRankMap",
            //    new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new SplayTreeArrayRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IRankMap<int, int>>[] RankMapTests = new TestInfo<IRankMap<int, int>>[]
        {
            new TestInfo<IRankMap<int, int>>("Small", 10, SmallCount, SmallNumTrials, MakePerfTestRankMapMaker),
            new TestInfo<IRankMap<int, int>>("Med", 1, MediumCount, MediumNumTrials, MakePerfTestRankMapMaker),
            new TestInfo<IRankMap<int, int>>("Large", 1, LargeCount, LargeNumTrials, MakePerfTestRankMapMaker),
        };

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Measurement.MakePerfTest MakePerfTestRankMapMaker(TreeFactory<IRankMap<int, int>> treeFactory, int count)
        {
            return delegate () { return new PerfTestRankMap(delegate (int _count) { return treeFactory.Create(_count); }, count); };
        }

        //
        // IMultiRankMap
        //

        private readonly static CreateTreeInfo<IMultiRankMap<int, int>>[] MultiRankMapCreators = new CreateTreeInfo<IMultiRankMap<int, int>>[]
        {
            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "AVLTreeMultiRankMap",
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new AVLTreeMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "AVLTreeArrayMultiRankMap",
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new AVLTreeArrayMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "RedBlackTreeMultiRankMap",
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new RedBlackTreeMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "RedBlackTreeArrayMultiRankMap",
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new RedBlackTreeArrayMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "SplayTreeMultiRankMap",
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new SplayTreeMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "SplayTreeArrayMultiRankMap",
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new SplayTreeArrayMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IMultiRankMap<int, int>>[] MultiRankMapTests = new TestInfo<IMultiRankMap<int, int>>[]
        {
            new TestInfo<IMultiRankMap<int, int>>("Small", 10, SmallCount, SmallNumTrials, MakePerfTestMultiRankMapMaker),
            new TestInfo<IMultiRankMap<int, int>>("Med", 1, MediumCount, MediumNumTrials, MakePerfTestMultiRankMapMaker),
            new TestInfo<IMultiRankMap<int, int>>("Large", 1, LargeCount, LargeNumTrials, MakePerfTestMultiRankMapMaker),
        };

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Measurement.MakePerfTest MakePerfTestMultiRankMapMaker(TreeFactory<IMultiRankMap<int, int>> treeFactory, int count)
        {
            return delegate () { return new PerfTestMultiRankMap(delegate (int _count) { return treeFactory.Create(_count); }, count); };
        }

        //
        // IRangeMap
        //

        private readonly static CreateTreeInfo<IRangeMap<int>>[] RangeMapCreators = new CreateTreeInfo<IRangeMap<int>>[]
        {
            new CreateTreeInfo<IRangeMap<int>>(
                "AVLTreeRangeMap",
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new AVLTreeRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRangeMap<int>>(
                "AVLTreeArrayRangeMap",
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new AVLTreeArrayRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRangeMap<int>>(
                "RedBlackTreeRangeMap",
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new RedBlackTreeRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRangeMap<int>>(
                "RedBlackTreeArrayRangeMap",
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new RedBlackTreeArrayRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRangeMap<int>>(
                "SplayTreeRangeMap",
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new SplayTreeRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRangeMap<int>>(
                "SplayTreeArrayRangeMap",
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new SplayTreeArrayRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IRangeMap<int>>[] RangeMapTests = new TestInfo<IRangeMap<int>>[]
        {
            new TestInfo<IRangeMap<int>>("Small", 10, SmallCount, SmallNumTrials, MakePerfTestRangeMapMaker),
            new TestInfo<IRangeMap<int>>("Med", 1, MediumCount, MediumNumTrials, MakePerfTestRangeMapMaker),
            new TestInfo<IRangeMap<int>>("Large", 1, LargeCount, LargeNumTrials, MakePerfTestRangeMapMaker),
        };

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Measurement.MakePerfTest MakePerfTestRangeMapMaker(TreeFactory<IRangeMap<int>> treeFactory, int count)
        {
            return delegate () { return new PerfTestRangeMap(delegate (int _count) { return treeFactory.Create(_count); }, count); };
        }

        //
        // IRange2Map
        //

        private readonly static CreateTreeInfo<IRange2Map<int>>[] Range2MapCreators = new CreateTreeInfo<IRange2Map<int>>[]
        {
            new CreateTreeInfo<IRange2Map<int>>(
                "AVLTreeRange2Map",
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new AVLTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRange2Map<int>>(
                "AVLTreeArrayRange2Map",
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new AVLTreeArrayRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRange2Map<int>>(
                "RedBlackTreeRange2Map",
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new RedBlackTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRange2Map<int>>(
                "RedBlackTreeArrayRange2Map",
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new RedBlackTreeArrayRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRange2Map<int>>(
                "SplayTreeRange2Map",
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new SplayTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRange2Map<int>>(
                "SplayTreeArrayRange2Map",
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new SplayTreeArrayRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IRange2Map<int>>[] Range2MapTests = new TestInfo<IRange2Map<int>>[]
        {
            new TestInfo<IRange2Map<int>>("Small", 10, SmallCount, SmallNumTrials, MakePerfTestRange2MapMaker),
            new TestInfo<IRange2Map<int>>("Med", 1, MediumCount, MediumNumTrials, MakePerfTestRange2MapMaker),
            new TestInfo<IRange2Map<int>>("Large", 1, LargeCount, LargeNumTrials, MakePerfTestRange2MapMaker),
        };

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Measurement.MakePerfTest MakePerfTestRange2MapMaker(TreeFactory<IRange2Map<int>> treeFactory, int count)
        {
            return delegate () { return new PerfTestRange2Map(delegate (int _count) { return treeFactory.Create(_count); }, count); };
        }


        //
        // Perf Test
        //

        private const double MedianMeanEpsilon = .03;
        private const double StabilityEpsilon = .03;

        private static void Test(double current, double baseline, double epsilon, out bool accepted, out bool improved)
        {
            accepted = current / baseline - 1 <= epsilon;
            improved = current / baseline - 1 <= -epsilon;
        }

        private static void TestMedian(Measurement.Result result, Measurement.Result baseline, out bool accepted, out bool improved)
        {
            if (baseline == null)
            {
                accepted = true;
                improved = false;
                return;
            }
            Test(result.median, baseline.median, MedianMeanEpsilon, out accepted, out improved);
        }

        private static void TestAverage(Measurement.Result result, Measurement.Result baseline, out bool accepted, out bool improved)
        {
            if (baseline == null)
            {
                accepted = true;
                improved = false;
                return;
            }
            Test(result.average, baseline.average, MedianMeanEpsilon, out accepted, out improved);
        }

        private static void TestStability(Measurement.Result result, Measurement.Result baseline, out bool accepted)
        {
            if (result.stability > StabilityEpsilon)
            {
                accepted = false;
                return;
            }
            if ((baseline != null) && (baseline.stability > StabilityEpsilon))
            {
                accepted = false;
                return;
            }
            accepted = true;
        }

        private enum Theme { Normal, Green, Red };

        private static char SetTheme(Theme theme)
        {
            switch (theme)
            {
                default:
                    throw new ArgumentException();
                case Theme.Normal:
                    //Console.ForegroundColor = ConsoleColor.Gray;
                    //Console.BackgroundColor = ConsoleColor.Black;
                    return ' ';
                case Theme.Red:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    return '*';
                case Theme.Green:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    return '+';
            }
        }

        private static void DisplayLine(string label, string medianLabel, Theme medianTheme, double median, string averageLabel, Theme averageTheme, double average, string stabilityLabel, Theme stabilityTheme, double? stability, Theme tagTheme, string tag)
        {
            ConsoleColor oldForeground = Console.ForegroundColor;
            ConsoleColor oldBackground = Console.BackgroundColor;

            Console.Write(" {0,-37} {1} ", label, !String.IsNullOrEmpty(label) ? "-" : " ");

            char c = SetTheme(medianTheme);
            Console.Write("{2,3} {0,6:F3}{1}", median, c, medianLabel);
            Console.ForegroundColor = oldForeground;
            Console.BackgroundColor = oldBackground;

            Console.Write(" ");

            c = SetTheme(averageTheme);
            Console.Write("{2,3} {0,6:F3}{1}", average, c, averageLabel);
            Console.ForegroundColor = oldForeground;
            Console.BackgroundColor = oldBackground;

            Console.Write(" ");

            c = SetTheme(stabilityTheme);
            Console.Write("{2,3} {0,6:F3}{1}", stability, c, stabilityLabel);
            Console.ForegroundColor = oldForeground;
            Console.BackgroundColor = oldBackground;

            Console.Write(" ");

            SetTheme(tagTheme);
            Console.Write("{0,-2}", tag);
            Console.ForegroundColor = oldForeground;
            Console.BackgroundColor = oldBackground;

            Console.WriteLine();
        }

        private static bool DisplayResult(Measurement.Result result, Measurement.Result baseline)
        {
            bool acceptMedian, acceptAverage, acceptStability;
            bool improvedMedian, improvedAverage;

            TestMedian(result, baseline, out acceptMedian, out improvedMedian);
            TestAverage(result, baseline, out acceptAverage, out improvedAverage);
            TestStability(result, baseline, out acceptStability);

            bool accepted = acceptMedian && acceptAverage && acceptStability;
            bool improved = improvedMedian || improvedAverage;

            DisplayLine(
                result.label,
                "med",
                acceptMedian ? (improvedMedian ? Theme.Green : Theme.Normal) : Theme.Red,
                result.median,
                "avg",
                acceptAverage ? (improvedAverage ? Theme.Green : Theme.Normal) : Theme.Red,
                result.average,
                "sta",
                acceptStability ? Theme.Normal : Theme.Red,
                result.stability,
                baseline != null ? Theme.Normal : Theme.Red,
                baseline != null ? String.Empty : "NB");

            if (baseline != null)
            {
                DisplayLine(
                    String.Empty,
                    "x",
                    acceptMedian ? (improvedMedian ? Theme.Green : Theme.Normal) : Theme.Red,
                    result.median / baseline.median,
                    "x",
                    acceptAverage ? (improvedAverage ? Theme.Green : Theme.Normal) : Theme.Red,
                    result.average / baseline.average,
                    String.Empty/*"x"*/,
                    Theme.Normal/*acceptStability ? Theme.Normal : Theme.Red*/,
                    null/*result.stability / baseline.stability*/,
                    Theme.Normal,
                    String.Empty);
            }

            return accepted;
        }

        private enum Kind { Dry, Real };

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void RunTestCategory<ITree>(
            Kind run,
            IEnumerable<CreateTreeInfo<ITree>> creators,
            IEnumerable<TestInfo<ITree>> tests,
            ref bool success,
            bool resetBaseline,
            Dictionary<string, Measurement.Result> baselineResults,
            List<Measurement.Result> results)
        {
            foreach (CreateTreeInfo<ITree> createTreeInfo in creators)
            {
                foreach (TestInfo<ITree> test in tests)
                {
                    Measurement.Result result = Measurement.RunTest(
                        String.Format("{0}:{1}", createTreeInfo.kind, test.label),
                        delegate () { return test.makePerfTestFactory(createTreeInfo.treeFactory, test.count)(); },
                        run == Kind.Real ? test.trials : 1,
                        test.multiplier);
                    if (run == Kind.Real)
                    {
                        Measurement.Result baseline;
                        baselineResults.TryGetValue(result.label, out baseline);
                        bool success1 = DisplayResult(result, baseline) && ((baseline != null) || resetBaseline);
                        success = success1 && (success || resetBaseline);
                    }
                    results.Add(result);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool RunAllPerfTests(bool resetBaseline)
        {
            bool success = false;
            string status = "SKIPPED";

            Console.WriteLine("Performance Tests - Started");
#if DEBUG
#pragma warning disable CS0162 // complaint about unreachable code
            Console.WriteLine("  DEBUG build - skipping");
            if (false)
#endif
            {
                GCLatencyMode oldGCLatencyMode = GCSettings.LatencyMode;
                ProcessPriorityClass oldProcessPriority = Process.GetCurrentProcess().PriorityClass;
                ThreadPriority oldThreadPriority = Thread.CurrentThread.Priority;
                try
                {
                    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
                    Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                    string baselinePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TreeLibTest", "perfbaseline.txt");

                    List<Measurement.Result> results = new List<Measurement.Result>();

                    success = true;

                    Dictionary<string, Measurement.Result> baselineResults = new Dictionary<string, Measurement.Result>();
                    if (!resetBaseline)
                    {
                        if (File.Exists(baselinePath))
                        {
                            using (TextReader reader = new StreamReader(baselinePath))
                            {
                                string line = reader.ReadLine();
                                int version = Int32.Parse(line);
                                if (version == Measurement.Result.Version)
                                {
                                    reader.ReadLine(); // headers
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        Measurement.Result result = Measurement.Result.FromString(line);
                                        baselineResults.Add(result.label, result);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(" ** baseline file has wrong version - not loaded **");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(" ** no baseline file found **");
                        }
                    }

                    foreach (Kind phase in new Kind[] { Kind.Dry, Kind.Real })
                    {
                        results.Clear();

                        TextWriter savedOutput = null;
                        if (phase == Kind.Dry)
                        {
                            savedOutput = Console.Out;
                            Console.SetOut(TextWriter.Null);
                        }

                        RunTestCategory(phase, MapCreators, MapTests, ref success, resetBaseline, baselineResults, results);
                        RunTestCategory(phase, RankMapCreators, RankMapTests, ref success, resetBaseline, baselineResults, results);
                        // redundant: RunTestCategory(phase, MultiRankMapCreators, MultiRankMapTests, ref success, resetBaseline, baselineResults, results);
                        // redundant: RunTestCategory(phase, RangeMapCreators, RangeMapTests, ref success, resetBaseline, baselineResults, results);
                        RunTestCategory(phase, Range2MapCreators, Range2MapTests, ref success, resetBaseline, baselineResults, results);

                        if (savedOutput != null)
                        {
                            Console.SetOut(savedOutput);
                        }
                    }

                    if (resetBaseline)
                    {
                        status = "NEW BASELINE";
                        Directory.CreateDirectory(Path.GetDirectoryName(baselinePath));
                        using (TextWriter writer = new StreamWriter(baselinePath))
                        {
                            writer.WriteLine(Measurement.Result.Version);
                            writer.WriteLine(Measurement.Result.Header);
                            for (int i = 0; i < results.Count; i++)
                            {
                                writer.WriteLine(results[i].ToString());
                            }
                        }
                    }
                    else
                    {
                        status = success ? "PASSED" : "FAILED";
                    }
                }
                finally
                {
                    GCSettings.LatencyMode = oldGCLatencyMode;
                    Process.GetCurrentProcess().PriorityClass = oldProcessPriority;
                    Thread.CurrentThread.Priority = oldThreadPriority;
                }
            }

            Program.WritePassFail("Performance Tests - Finished", success, status, status);
            return success;
        }
    }
}
