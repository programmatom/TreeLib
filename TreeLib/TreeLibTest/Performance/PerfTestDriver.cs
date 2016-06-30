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
using System.Reflection;
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

        public readonly static string[] CategoryTokens = new string[] { "basic", "enum" };

        //
        // Templates
        //

        public enum Group { Priority = 0, Full = 1 };

        public class CreateTreeInfo<TreeType>
        {
            public readonly string kind;
            public readonly Group group;
            public readonly TreeFactory<TreeType> treeFactory;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public CreateTreeInfo(string kind, Group group, TreeFactory<TreeType> treeFactory)
            {
                this.kind = kind;
                this.group = group;
                this.treeFactory = treeFactory;
            }
        }

        public class TreeFactory<TreeType>
        {
            private readonly MakeTree makeTree;

            public delegate TreeType MakeTree(int capacity);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TreeFactory(MakeTree makeTree)
            {
                this.makeTree = makeTree;
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TreeType Create(int capacity)
            {
                return makeTree(capacity);
            }
        }

        private class TestInfo<TreeType>
        {
            public readonly string label;
            public readonly Group group;
            public readonly int multiplier;
            public readonly int count;
            public readonly int trials;
            public readonly PerfTestFactoryMaker makePerfTestFactory;

            public delegate Measurement.MakePerfTest PerfTestFactoryMaker(TreeFactory<TreeType> treeFactory, int count);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public TestInfo(string label, Group group, int multiplier, int count, int trials, PerfTestFactoryMaker makePerfTestFactory)
            {
                this.label = label;
                this.group = group;
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
                "AVLTreeMap", Group.Priority,
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new AVLTreeMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IOrderedMap<int, int>>(
                "AVLTreeArrayMap", Group.Full,
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new AVLTreeArrayMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IOrderedMap<int, int>>(
                "RedBlackTreeMap", Group.Priority,
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new RedBlackTreeMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IOrderedMap<int, int>>(
                "RedBlackTreeArrayMap", Group.Full,
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new RedBlackTreeArrayMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IOrderedMap<int, int>>(
                "SplayTreeMap", Group.Priority,
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new SplayTreeMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IOrderedMap<int, int>>(
                "SplayTreeArrayMap", Group.Full,
                new TreeFactory<IOrderedMap<int, int>>(delegate (int count) { return new SplayTreeArrayMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IOrderedMap<int, int>>[] MapTests = new TestInfo<IOrderedMap<int, int>>[]
        {
            new TestInfo<IOrderedMap<int, int>>("Small", Group.Priority, 15, SmallCount, SmallNumTrials, MakePerfTestMapMaker),
            new TestInfo<IOrderedMap<int, int>>("Med", Group.Priority, 1, MediumCount, MediumNumTrials, MakePerfTestMapMaker),
            new TestInfo<IOrderedMap<int, int>>("Large", Group.Priority, 1, LargeCount, LargeNumTrials, MakePerfTestMapMaker),
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
                "AVLTreeRankMap", Group.Priority,
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new AVLTreeRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRankMap<int,int>>(
                "AVLTreeArrayRankMap", Group.Full,
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new AVLTreeArrayRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRankMap<int,int>>(
                "RedBlackTreeRankMap", Group.Priority,
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new RedBlackTreeRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRankMap<int,int>>(
                "RedBlackTreeArrayRankMap", Group.Full,
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new RedBlackTreeArrayRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRankMap<int,int>>(
                "SplayTreeRankMap", Group.Priority,
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new SplayTreeRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRankMap<int,int>>(
                "SplayTreeArrayRankMap", Group.Full,
                new TreeFactory<IRankMap<int, int>>(delegate (int count) { return new SplayTreeArrayRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IRankMap<int, int>>[] RankMapTests = new TestInfo<IRankMap<int, int>>[]
        {
            new TestInfo<IRankMap<int, int>>("Small", Group.Priority, 10, SmallCount, SmallNumTrials, MakePerfTestRankMapMaker),
            new TestInfo<IRankMap<int, int>>("Med", Group.Priority, 1, MediumCount, MediumNumTrials, MakePerfTestRankMapMaker),
            new TestInfo<IRankMap<int, int>>("Large", Group.Priority, 1, LargeCount, LargeNumTrials, MakePerfTestRankMapMaker),
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
                "AVLTreeMultiRankMap", Group.Full,
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new AVLTreeMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "AVLTreeArrayMultiRankMap", Group.Full,
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new AVLTreeArrayMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "RedBlackTreeMultiRankMap", Group.Full,
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new RedBlackTreeMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "RedBlackTreeArrayMultiRankMap", Group.Full,
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new RedBlackTreeArrayMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "SplayTreeMultiRankMap", Group.Full,
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new SplayTreeMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IMultiRankMap<int, int>>(
                "SplayTreeArrayMultiRankMap", Group.Full,
                new TreeFactory<IMultiRankMap<int, int>>(delegate (int count) { return new SplayTreeArrayMultiRankMap<int, int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IMultiRankMap<int, int>>[] MultiRankMapTests = new TestInfo<IMultiRankMap<int, int>>[]
        {
            new TestInfo<IMultiRankMap<int, int>>("Small", Group.Priority, 10, SmallCount, SmallNumTrials, MakePerfTestMultiRankMapMaker),
            new TestInfo<IMultiRankMap<int, int>>("Med", Group.Priority, 1, MediumCount, MediumNumTrials, MakePerfTestMultiRankMapMaker),
            new TestInfo<IMultiRankMap<int, int>>("Large", Group.Priority, 1, LargeCount, LargeNumTrials, MakePerfTestMultiRankMapMaker),
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
                "AVLTreeRangeMap", Group.Full,
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new AVLTreeRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRangeMap<int>>(
                "AVLTreeArrayRangeMap", Group.Full,
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new AVLTreeArrayRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRangeMap<int>>(
                "RedBlackTreeRangeMap", Group.Full,
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new RedBlackTreeRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRangeMap<int>>(
                "RedBlackTreeArrayRangeMap", Group.Full,
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new RedBlackTreeArrayRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRangeMap<int>>(
                "SplayTreeRangeMap", Group.Full,
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new SplayTreeRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRangeMap<int>>(
                "SplayTreeArrayRangeMap", Group.Full,
                new TreeFactory<IRangeMap<int>>(delegate (int count) { return new SplayTreeArrayRangeMap<int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IRangeMap<int>>[] RangeMapTests = new TestInfo<IRangeMap<int>>[]
        {
            new TestInfo<IRangeMap<int>>("Small", Group.Priority, 10, SmallCount, SmallNumTrials, MakePerfTestRangeMapMaker),
            new TestInfo<IRangeMap<int>>("Med", Group.Priority, 1, MediumCount, MediumNumTrials, MakePerfTestRangeMapMaker),
            new TestInfo<IRangeMap<int>>("Large", Group.Priority, 1, LargeCount, LargeNumTrials, MakePerfTestRangeMapMaker),
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
                "AVLTreeRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new AVLTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRange2Map<int>>(
                "AVLTreeArrayRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new AVLTreeArrayRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRange2Map<int>>(
                "RedBlackTreeRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new RedBlackTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRange2Map<int>>(
                "RedBlackTreeArrayRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new RedBlackTreeArrayRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRange2Map<int>>(
                "SplayTreeRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new SplayTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
            new CreateTreeInfo<IRange2Map<int>>(
                "SplayTreeArrayRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new SplayTreeArrayRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private readonly static TestInfo<IRange2Map<int>>[] Range2MapTests = new TestInfo<IRange2Map<int>>[]
        {
            new TestInfo<IRange2Map<int>>("Small", Group.Priority, 10, SmallCount, SmallNumTrials, MakePerfTestRange2MapMaker),
            new TestInfo<IRange2Map<int>>("Med", Group.Priority, 1, MediumCount, MediumNumTrials, MakePerfTestRange2MapMaker),
            new TestInfo<IRange2Map<int>>("Large", Group.Priority, 1, LargeCount, LargeNumTrials, MakePerfTestRange2MapMaker),
        };

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Measurement.MakePerfTest MakePerfTestRange2MapMaker(TreeFactory<IRange2Map<int>> treeFactory, int count)
        {
            return delegate () { return new PerfTestRange2Map(delegate (int _count) { return treeFactory.Create(_count); }, count); };
        }


        // 
        // IEnumerable
        //

        private readonly static CreateTreeInfo<IOrderedMap<int, int>>[] MapEnumerationCreators = MapCreators;

        private readonly static CreateTreeInfo<IRange2Map<int>>[] Range2MapEnumerationCreators = new CreateTreeInfo<IRange2Map<int>>[]
        {
            new CreateTreeInfo<IRange2Map<int>>(
                "AVLTreeRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new AVLTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRange2Map<int>>(
                "RedBlackTreeRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new RedBlackTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),

            new CreateTreeInfo<IRange2Map<int>>(
                "SplayTreeRange2Map", Group.Priority,
                new TreeFactory<IRange2Map<int>>(delegate (int count) { return new SplayTreeRange2Map<int>((uint)count, AllocationMode.PreallocatedFixed); })),
        };

        private enum EnumKind { Fast, Robust };

        private readonly static TrialInfo[] EnumerationTestTrialInfos = new TrialInfo[]
        {
            new TrialInfo("SmFaFw", Group.Priority, 150, SmallCount, SmallNumTrials, EnumKind.Fast, true/*forward*/),
            new TrialInfo("SmFaRv", Group.Full, 150, SmallCount, SmallNumTrials, EnumKind.Fast, false/*forward*/),

            new TrialInfo("SmRoFw", Group.Priority, 50, SmallCount, SmallNumTrials, EnumKind.Robust, true/*forward*/),
            new TrialInfo("SmRoRv", Group.Full, 50, SmallCount, SmallNumTrials, EnumKind.Robust, false/*forward*/),

            //

            new TrialInfo("MeFaFw", Group.Priority, 10, MediumCount, MediumNumTrials, EnumKind.Fast, true/*forward*/),
            new TrialInfo("MeFaRv", Group.Full, 10, MediumCount, MediumNumTrials, EnumKind.Fast, false/*forward*/),

            new TrialInfo("MeRoFw", Group.Priority, 5, MediumCount, MediumNumTrials, EnumKind.Robust, true/*forward*/),
            new TrialInfo("MeRoRv", Group.Full, 5, MediumCount, MediumNumTrials, EnumKind.Robust, false/*forward*/),

            //

            new TrialInfo("LgFaFw", Group.Full, 1, LargeCount, LargeNumTrials, EnumKind.Fast, true/*forward*/),
            new TrialInfo("LgFaRv", Group.Full, 1, LargeCount, LargeNumTrials, EnumKind.Fast, false/*forward*/),

            new TrialInfo("LgRoFw", Group.Full, 1, LargeCount, LargeNumTrials, EnumKind.Robust, true/*forward*/),
            new TrialInfo("LgRoRv", Group.Full, 1, LargeCount, LargeNumTrials, EnumKind.Robust, false/*forward*/),
        };

        private readonly static TestInfo<IRange2Map<int>>[] Range2MapEnumerationTests
            = GetEnumerationTests<IRange2Map<int>, EntryRange2Map<int>>(TestBase.TreeKind.Range2Map, EnumerationTestTrialInfos);

        private readonly static TestInfo<IOrderedMap<int, int>>[] MapEnumerationTests
            = GetEnumerationTests<IOrderedMap<int, int>, EntryMap<int, int>>(TestBase.TreeKind.Map, EnumerationTestTrialInfos);

        private class TrialInfo : Tuple<string, int, int, int, EnumKind, bool, Group>
        {
            public TrialInfo(string label, Group group, int multiplier, int count, int numTrials, EnumKind enumKind, bool forward)
                : base(label, multiplier, count, numTrials, enumKind, forward, group)
            {
            }

            public string Label { get { return Item1; } }
            public int Multiplier { get { return Item2; } }
            public int Count { get { return Item3; } }
            public int NumTrials { get { return Item4; } }
            public EnumKind EnumKind { get { return Item5; } }
            public bool Forward { get { return Item6; } }
            public Group Group { get { return Item7; } }
        }

        private static TestInfo<TreeType>[] GetEnumerationTests<TreeType, EntryType>(TestBase.TreeKind treeKind, TrialInfo[] trialInfos)
        {
            List<TestInfo<TreeType>> list = new List<TestInfo<TreeType>>();
            foreach (TrialInfo trialInfo in trialInfos)
            {
                TestBase.EnumKind enumKind;
                switch (trialInfo.EnumKind)
                {
                    default:
                        throw new ArgumentException();
                    case EnumKind.Fast:
                        if ((treeKind & TestBase.TreeKind.AllIndexed2) != 0)
                        {
                            enumKind = TestBase.EnumKind.Indexed2EnumerableFastBidir;
                        }
                        else if ((treeKind & TestBase.TreeKind.AllKeyed) != 0)
                        {
                            enumKind = TestBase.EnumKind.KeyedEnumerableFastBidir;
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                        break;
                    case EnumKind.Robust:
                        if ((treeKind & TestBase.TreeKind.AllIndexed2) != 0)
                        {
                            enumKind = TestBase.EnumKind.Indexed2EnumerableRobustBidir;
                        }
                        else if ((treeKind & TestBase.TreeKind.AllKeyed) != 0)
                        {
                            enumKind = TestBase.EnumKind.KeyedEnumerableRobustBidir;
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                        break;
                }

                list.Add(
                    new TestInfo<TreeType>(
                        trialInfo.Label,
                        trialInfo.Group,
                        trialInfo.Multiplier,
                        trialInfo.Count,
                        trialInfo.NumTrials,
                        MakePerfTestEnumerationMakerBinder<TreeType, EntryType>(
                            treeKind,
                            enumKind,
                            trialInfo.Forward)));
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static TestInfo<TreeType>.PerfTestFactoryMaker MakePerfTestEnumerationMakerBinder<TreeType, EntryType>(TestBase.TreeKind treeKind, TestBase.EnumKind enumKind, bool forward)
        {
            return delegate (TreeFactory<TreeType> _treeFactory, int _count)
            {
                return MakePerfTestEnumerationMaker<TreeType, EntryType>(_treeFactory, _count, treeKind, enumKind, forward);
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Measurement.MakePerfTest MakePerfTestEnumerationMaker<TreeType, EntryType>(TreeFactory<TreeType> treeFactory, int count, TestBase.TreeKind treeKind, TestBase.EnumKind enumKind, bool forward)
        {
            return delegate ()
            {
                return new PerfTestEnumeration<TreeType, EntryType>(delegate (int _count) { return treeFactory.Create(_count); }, count, treeKind, enumKind, forward);
            };
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
            Group group,
            IEnumerable<CreateTreeInfo<ITree>> creators,
            IEnumerable<TestInfo<ITree>> tests,
            ref bool success,
            bool resetBaseline,
            Dictionary<string, Measurement.Result> baselineResults,
            List<Measurement.Result> results)
        {
            foreach (CreateTreeInfo<ITree> createTreeInfo in creators)
            {
                if (createTreeInfo.group <= group)
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
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool RunAllPerfTests(bool enabled, bool resetBaseline, Group group, KeyValuePair<string, bool>[] enables)
        {
            Debug.Assert(enables.Length == CategoryTokens.Length);

            bool success = false;
            string status = "SKIPPED";

            Console.WriteLine("Performance Regression Tests - Started");
#if DEBUG
#pragma warning disable CS0162 // complaint about unreachable code
            Console.WriteLine("  DEBUG build - skipping");
            if (false)
#else
            if (enabled)
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

                        if (enables[Array.FindIndex(enables, delegate (KeyValuePair<string, bool> candidate) { return String.Equals("basic", candidate.Key); })].Value)
                        {
                            RunTestCategory(phase, group, MapCreators, MapTests, ref success, resetBaseline, baselineResults, results);
                            RunTestCategory(phase, group, RankMapCreators, RankMapTests, ref success, resetBaseline, baselineResults, results);
                            RunTestCategory(phase, group, MultiRankMapCreators, MultiRankMapTests, ref success, resetBaseline, baselineResults, results);
                            RunTestCategory(phase, group, RangeMapCreators, RangeMapTests, ref success, resetBaseline, baselineResults, results);
                            RunTestCategory(phase, group, Range2MapCreators, Range2MapTests, ref success, resetBaseline, baselineResults, results);
                        }

                        if (enables[Array.FindIndex(enables, delegate (KeyValuePair<string, bool> candidate) { return String.Equals("enum", candidate.Key); })].Value)
                        {
                            RunTestCategory(phase, group, MapEnumerationCreators, MapEnumerationTests, ref success, resetBaseline, baselineResults, results);
                            RunTestCategory(phase, group, Range2MapEnumerationCreators, Range2MapEnumerationTests, ref success, resetBaseline, baselineResults, results);
                        }

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

            Program.WritePassFail("Performance Regression Tests - Finished", success ? Program.TestResultCode.Passed : Program.TestResultCode.Failed, status);
            return success;
        }
    }
}
