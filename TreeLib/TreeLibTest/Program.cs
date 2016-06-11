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
using System.Threading;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class StochasticControls
    {
        private int reportingInterval = Debugger.IsAttached ? 250 : 2500;
        private int stop;
        private int failed;

        public int ReportingInterval
        {
            get { return Thread.VolatileRead(ref reportingInterval); }
            set { Thread.VolatileWrite(ref reportingInterval, value); }
        }

        public bool Stop
        {
            get { return Thread.VolatileRead(ref stop) != 0; }
            set { Thread.VolatileWrite(ref stop, value ? 1 : 0); }
        }

        public bool Failed
        {
            get { return Thread.VolatileRead(ref failed) != 0; }
            set { Thread.VolatileWrite(ref failed, value ? 1 : 0); }
        }
    }

    public class ConsoleBuffer
    {
        private readonly string title;
        private string status;
        private readonly int width;
        private readonly int height;
        private readonly string[] lines;
        private int index;
        private int changed = 1;

        public ConsoleBuffer(string title, int width, int height)
        {
            this.title = title;
            this.width = width;
            this.height = height;
            this.lines = new string[height];
        }

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                Thread.VolatileWrite(ref changed, 1);
            }
        }

        public void WriteLine(string text)
        {
            WriteText(String.Concat(text, Environment.NewLine));
        }

        public void WriteLine(string line, params object[] args)
        {
            WriteText(String.Format(line, args));
        }

        // TODO:
        public void WriteLine(ConsoleColor color, string line, params object[] args)
        {
            WriteText(String.Format(line, args));
        }

        public void WriteText(string text)
        {
            lock (this)
            {
                if (text == null)
                {
                    text = String.Empty;
                }
                foreach (string line1 in text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    string line2 = line1;
                    while (line2.Length > width)
                    {
                        lines[index] = line2.Substring(0, width);
                        line2 = line2.Substring(width);
                        index = (index + 1) % height;
                    }
                    lines[index] = line1;
                    index = (index + 1) % height;
                }
                Thread.VolatileWrite(ref changed, 1);
            }
        }

        public void PrintBuffer()
        {
            string[] items = new string[height];
            lock (this)
            {
                Thread.VolatileWrite(ref changed, 0);
                for (int i = 0; i < height; i++)
                {
                    items[i] = lines[(i + index) % height];
                }
            }
            string padding = new string(' ', Console.BufferWidth);
            Console.Write((title + (!String.IsNullOrEmpty(status) ? String.Concat(" (", status, ")") : String.Empty) + padding).Substring(0, width));
            foreach (string item in items)
            {
                Console.Write(String.Concat(item, padding).Substring(0, width));
            }
        }

        public bool Changed { get { return Thread.VolatileRead(ref changed) != 0; } }
    }

    public class Program
    {
        // "You know it seemed a bit daft me having to guard him when he's a guard."
        private static void TestFramework()
        {
            TextWriter savedOutput = Console.Out;
            Console.SetOut(TextWriter.Null);

            TestBase testBase = new TestBase();
            UnitTestMap unitTestMap = new UnitTestMap();
            testBase.TestNoThrow(String.Empty, delegate () { testBase.TestTrue(String.Empty, delegate () { return true; }); });
            testBase.TestThrow(String.Empty, typeof(UnitTestFailureException), delegate () { testBase.TestTrue(String.Empty, delegate () { return false; }); });
            testBase.TestThrow(String.Empty, typeof(UnitTestFailureException), delegate () { testBase.TestTrue(String.Empty, delegate () { throw new NotImplementedException(); }); });
            testBase.TestThrow(String.Empty, typeof(UnitTestFailureException), delegate () { testBase.TestNoThrow(String.Empty, delegate () { throw new NotImplementedException(); }); });
            testBase.TestThrow(String.Empty, typeof(UnitTestFailureException), delegate () { testBase.TestThrow(String.Empty, typeof(NotImplementedException), delegate () { throw new NotImplementedException(); }); });
            testBase.TestThrow(String.Empty, typeof(UnitTestFailureException), delegate () { testBase.TestThrow(String.Empty, typeof(NotImplementedException), delegate () { throw new Exception(); }); });
            testBase.TestNoThrow(String.Empty, delegate () { unitTestMap.TestTree(String.Empty, new SplayTreeMap<int, bool>(), new UnitTestMap.Op<int, bool>[] { }, delegate () { }); });
            testBase.TestThrow(String.Empty, typeof(UnitTestFailureException), delegate () { unitTestMap.TestTree(String.Empty, new SplayTreeMap<int, bool>(), new UnitTestMap.Op<int, bool>[] { new FailOp(), }, delegate () { throw new NotImplementedException(); }); });
            testBase.TestThrow(String.Empty, typeof(UnitTestFailureException), delegate () { unitTestMap.TestTree(String.Empty, new SplayTreeMap<int, bool>(), new UnitTestMap.Op<int, bool>[] { }, delegate () { throw new NotImplementedException(); }); });

            Console.WriteLine(new Range2MapEntry(new Range(0, 0), new Range(0, 0), String.Empty).ToString());
            Console.WriteLine(new MultiRankMapEntry(0, new Range(0, 0), 0));

            SplayTreeMap<int, int> tree = new SplayTreeMap<int, int>();
            tree.Add(1, 1);
            tree.Add(2, 2);
            TreeInspection.Dump(tree);

            Console.SetOut(savedOutput);
        }

        private class FailOp : UnitTestMap.Op<int, bool>
        {
            public FailOp()
                : base(0)
            {
            }

            public override void Do(IOrderedMap<int, bool> tree)
            {
                throw new NotImplementedException();
            }

            public override void Do(IOrderedMap<int, bool> tree, List<KeyValuePair<int, bool>> treeAnalog)
            {
                throw new NotImplementedException();
            }
        }

        private readonly static string[] UnitTestTokens = new string[]
        {
            "map", "range2map", "rangemap", "range2list", "rangelist", "multirankmap", "multiranklist", "alloc", "enum", "clone", "hugelist"
        };
        private delegate TestBase MakeTestBase(long startIteration);
        private static void UnitTestsKernel(Options options)
        {
            Tuple<string, MakeTestBase>[] unitTests = new Tuple<string, MakeTestBase>[]
            {
                new Tuple<string, MakeTestBase>("map",              delegate (long startIter) { return new UnitTestMap(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("range2map",        delegate (long startIter) { return new UnitTestRange2Map(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("rangemap",         delegate (long startIter) { return new UnitTestRangeMap(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("range2list",       delegate (long startIter) { return new UnitTestRange2List(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("rangelist",        delegate (long startIter) { return new UnitTestRangeList(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("multirankmap",     delegate (long startIter) { return new UnitTestMultiRankMap(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("multiranklist",    delegate (long startIter) { return new UnitTestRankMap(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("alloc",            delegate (long startIter) { return new UnitTestAllocation(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("enum",             delegate (long startIter) { return new UnitTestEnumeration(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("clone",            delegate (long startIter) { return new UnitTestClone(options.breakIterations, startIter); }),
                new Tuple<string, MakeTestBase>("hugelist",         delegate (long startIter) { return new UnitTestHugeList(options.breakIterations, startIter); }),
            };

            long iteration = 0;
            for (int i = 0; i < unitTests.Length; i++)
            {
                Debug.Assert(String.Equals(unitTests[i].Item1, UnitTestTokens[i]));
                if (Array.Find(options.unitEnables, x => String.Equals(x.Key, unitTests[i].Item1)).Value)
                {
                    Console.WriteLine("Unit test: {0}", unitTests[i].Item1);
                    TestBase unitTest = unitTests[i].Item2(iteration);
                    unitTest.Do();
                    iteration = unitTest.iteration;
                }
            }
        }

        public static void WritePassFail(string message, bool success, string pass, string fail)
        {
            Console.Write("{0}: ", message);
            ConsoleColor savedForeColor = Console.ForegroundColor;
            ConsoleColor savedBackColor = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = success ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
            Console.WriteLine(success ? pass : fail);
            Console.ForegroundColor = savedForeColor;
            Console.BackgroundColor = savedBackColor;
        }

        public static void WritePassFail(string message, bool success)
        {
            WritePassFail(message, success, "PASSED", "FAILED");
        }

        private static bool UnitTests(Options options)
        {
            Console.WriteLine("Unit Tests - Started");

            bool success = false;
            if (Debugger.IsAttached || options.failHard)
            {
                UnitTestsKernel(options);
                success = true;
            }
            else
            {
                try
                {
                    UnitTestsKernel(options);
                    success = true;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                }
            }

            WritePassFail("Unit Tests - Finished", success);
            return success;
        }

        private delegate void DoStochasticTest();
        private class StochasticTestEntry
        {
            public readonly string token;
            public readonly TestBase testObject;
            public readonly ConsoleBuffer consoleBuffer;
            public readonly Thread thread;
            public readonly StochasticControls control;
            public readonly int seed;

            public bool failed { get; private set; }
            public bool actuallyDo { get; set; }

            public StochasticTestEntry(string token, TestBase testObject, ConsoleBuffer consoleBuffer, int seed, StochasticControls control)
            {
                this.token = token;
                this.testObject = testObject;
                this.consoleBuffer = consoleBuffer;
                this.control = control;
                this.seed = seed;

                testObject.ConsoleBuffer = consoleBuffer;
                consoleBuffer.Status = "not started";

                this.thread = new Thread(Do);
                this.thread.Priority = ThreadPriority.BelowNormal;
            }

            public void Start()
            {
                thread.Start();
            }

            private void Do()
            {
                if (actuallyDo)
                {
                    consoleBuffer.Status = "running";
                    consoleBuffer.WriteLine("  [starting]");
                    if (testObject.Do(seed, control))
                    {
                        failed = false;
                        consoleBuffer.Status = "ended successfully";
                    }
                    else
                    {
                        failed = true;
                        control.Failed = true;
                        consoleBuffer.WriteLine("  **TERMINATED WITH FAILURE!**");
                        consoleBuffer.Status = "FAILED";
                    }
                }
                else
                {
                    consoleBuffer.WriteLine("  [disabled]");
                }
            }
        }

        private static void RefreshConsoles(StochasticTestEntry[] tests)
        {
            Console.WriteLine();
            foreach (StochasticTestEntry test in tests)
            {
                test.consoleBuffer.PrintBuffer();
                Console.WriteLine();
            }
        }

        private const int StochasticTestCount = 8;

        private readonly static string[] StochasticTokens = new string[StochasticTestCount]
        {
            "map", "range", "range2", "rangelist", "range2list", "rank", "multirank", "hugelist"
        };

        private readonly static int[] ReportingIntervals = new int[] { 100, 250, 500, 1000, 1250, 2500, 5000, 10000, 12500, 25000 };

        private static bool StochasticTests(Options options)
        {
            Console.Write("Stochastic Tests - Started [Seed ");
            ConsoleColor savedColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("{0}", options.seed);
            Console.ForegroundColor = savedColor;
            Console.WriteLine("]");

            StochasticControls control = new StochasticControls();

            int bufferHeight1 = Math.Max(1, (Console.WindowHeight - 6) / StochasticTestCount - 2);
            StochasticTestEntry[] tests = new StochasticTestEntry[StochasticTestCount]
            {
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "map")],
                    new StochasticTestMap(options.breakIterations, 0),
                    new ConsoleBuffer("Map/List Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "range")],
                    new StochasticTestRangeMap(options.breakIterations, 0),
                    new ConsoleBuffer("Range Map Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "rangelist")],
                    new StochasticTestRangeList(options.breakIterations, 0),
                    new ConsoleBuffer("Range List Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "range2")],
                    new StochasticTestRange2Map(options.breakIterations, 0),
                    new ConsoleBuffer("Range2 Map Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "range2list")],
                    new StochasticTestRange2List(options.breakIterations, 0),
                    new ConsoleBuffer("Range2 List Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "rank")],
                    new StochasticTestRankMap(options.breakIterations, 0),
                    new ConsoleBuffer("Rank Map Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "multirank")],
                    new StochasticTestMultiRankMap(options.breakIterations, 0),
                    new ConsoleBuffer("MultiRank Map Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
                new StochasticTestEntry(
                    StochasticTokens[Array.IndexOf(StochasticTokens, "hugelist")],
                    new StochasticTestHugeList(options.breakIterations, 0),
                    new ConsoleBuffer("HugeList Stochastic Test:", Console.BufferWidth, bufferHeight1),
                    options.seed, control),
            };
            for (int i = 0; i < tests.Length; i++)
            {
                tests[i].actuallyDo = Array.Find(options.stochasticEnables, x => String.Equals(x.Key, tests[i].token)).Value;
                tests[i].Start();
            }

            int totalHeight = tests.Length * (bufferHeight1 + 1) + 2;
            for (int i = 0; i < totalHeight; i++)
            {
                Console.WriteLine();
            }
            Console.CursorTop = Math.Max(0, Console.CursorTop - totalHeight);

            Stopwatch started = Stopwatch.StartNew();
            const int PollIntervalMSec = 250;
            while (true)
            {
                if (!Array.TrueForAll(tests, delegate (StochasticTestEntry candidate) { return !candidate.consoleBuffer.Changed; }))
                {
                    int y = Console.CursorTop;
                    RefreshConsoles(tests);
                    Console.CursorTop = y;
                }

                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true/*intercept*/);
                    if (keyInfo.Key == ConsoleKey.Q)
                    {
                        goto Done;
                    }
                    else if (keyInfo.KeyChar == '+')
                    {
                        int i = Array.BinarySearch(ReportingIntervals, control.ReportingInterval);
                        i = i >= 0 ? i + 1 : ~i;
                        control.ReportingInterval = i < ReportingIntervals.Length ? ReportingIntervals[i] : control.ReportingInterval * 2;
                    }
                    else if (keyInfo.KeyChar == '-')
                    {
                        int i = Array.BinarySearch(ReportingIntervals, control.ReportingInterval);
                        i = i >= 0 ? i - 1 : ~i - 1;
                        control.ReportingInterval = i >= 0 ? ReportingIntervals[i] : control.ReportingInterval / 2;
                    }
                }

                Thread.Sleep(PollIntervalMSec);

                if (options.timeLimit.HasValue && (started.ElapsedMilliseconds >= 1000L * options.timeLimit.Value))
                {
                    break;
                }
            }
            Done:
            control.Stop = true;
            bool allStopped = false;
            while (!allStopped)
            {
                allStopped = true;
                foreach (StochasticTestEntry test in tests)
                {
                    if (test.thread.ThreadState != System.Threading.ThreadState.Stopped)
                    {
                        allStopped = false;
                        break;
                    }
                }
            }
            RefreshConsoles(tests);

            Console.WriteLine();

            bool success = !control.Failed;
            WritePassFail("Stochastic Tests - Finished", success);
            return success;
        }

        private delegate bool TestMethod(Options options);
        private readonly static KeyValuePair<string, TestMethod>[] Tests = new KeyValuePair<string, TestMethod>[]
        {
            new KeyValuePair<string, TestMethod>("unit", UnitTests),
            new KeyValuePair<string, TestMethod>("perf", delegate(Options options) { return PerfTestDriver.RunAllPerfTests(options.baseline); }),
            new KeyValuePair<string, TestMethod>("random", StochasticTests),
        };

        private struct Options
        {
            public int seed;
            public int? timeLimit;
            public bool baseline;
            public bool failHard;
            public long[] breakIterations;
            public KeyValuePair<string, bool>[] stochasticEnables;
            public KeyValuePair<string, bool>[] unitEnables;
        }

        public static int Main(string[] args)
        {
            TestFramework();

            Options options = new Options();
            List<long> breakIterations = new List<long>();
            Dictionary<string, bool> stochasticEnables = new Dictionary<string, bool>();
            foreach (string token in StochasticTokens)
            {
                stochasticEnables.Add(token, true);
            }
            Dictionary<string, bool> unitEnables = new Dictionary<string, bool>();
            foreach (string token in UnitTestTokens)
            {
                unitEnables.Add(token, true);
            }
            options.seed = Environment.TickCount;
            bool[] disables = new bool[Tests.Length];
            for (int n = 0; n < args.Length; n++)
            {
                string arg = args[n];
                if (arg.StartsWith("seed:"))
                {
                    options.seed = Int32.Parse(arg.Substring(5));
                }
                else if (arg.StartsWith("timelimit:"))
                {
                    options.timeLimit = Int32.Parse(arg.Substring(10));
                }
                else if (String.Equals(arg, "-all"))
                {
                    for (int i = 0; i < disables.Length; i++)
                    {
                        disables[i] = true;
                    }
                }
                else if (String.Equals(arg, "+all"))
                {
                    for (int i = 0; i < disables.Length; i++)
                    {
                        disables[i] = false;
                    }
                }
                else if (String.Equals(arg, "baseline"))
                {
                    options.baseline = true;
                }
                else if (String.Equals(arg, "failhard:"))
                {
                    arg = arg.Substring(9);
                    if (Array.IndexOf(new string[] { "on", "yes", "true" }, arg) >= 0)
                    {
                        options.failHard = true;
                    }
                    else if (Array.IndexOf(new string[] { "off", "no", "false" }, arg) >= 0)
                    {
                        options.failHard = false;
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                }
                else if (arg.StartsWith("break:"))
                {
                    breakIterations.Add(Int64.Parse(arg.Substring(6)));
                }
                else if (arg.StartsWith("+unit:") || arg.StartsWith("-unit:"))
                {
                    bool enable = arg[0] == '+';
                    arg = arg.Substring(6);
                    if (String.Equals(arg, "all"))
                    {
                        foreach (string key in UnitTestTokens)
                        {
                            unitEnables[key] = enable;
                        }
                    }
                    else
                    {
                        if (!unitEnables.ContainsKey(arg))
                        {
                            throw new ArgumentException(String.Format("Unit test \"{0}\" does not exist", arg));
                        }
                        unitEnables[arg] = enable;
                    }
                }
                else if (arg.StartsWith("+random:") || arg.StartsWith("-random:"))
                {
                    bool enable = arg[0] == '+';
                    arg = arg.Substring(8);
                    if (String.Equals(arg, "all"))
                    {
                        foreach (string key in StochasticTokens)
                        {
                            stochasticEnables[key] = enable;
                        }
                    }
                    else
                    {
                        if (!stochasticEnables.ContainsKey(arg))
                        {
                            throw new ArgumentException(String.Format("Stochastic test \"{0}\" does not exist", arg));
                        }
                        stochasticEnables[arg] = enable;
                    }
                }
                else
                {
                    bool disable;
                    if (arg.StartsWith("+"))
                    {
                        disable = false;
                        arg = arg.Substring(1);
                    }
                    else if (arg.StartsWith("-"))
                    {
                        disable = true;
                        arg = arg.Substring(1);
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                    int i = Array.FindIndex(Tests, delegate (KeyValuePair<string, TestMethod> candidate) { return String.Equals(candidate.Key, arg); });
                    if (i < 0)
                    {
                        throw new ArgumentException(String.Format("Specified test \"{0}\" not found", arg));
                    }
                    disables[i] = disable;
                }
            }
            options.breakIterations = breakIterations.ToArray();
            options.stochasticEnables = new List<KeyValuePair<string, bool>>(stochasticEnables).ToArray();
            options.unitEnables = new List<KeyValuePair<string, bool>>(unitEnables).ToArray();

            int exitCode = 0;

            for (int i = 0; i < Tests.Length; i++)
            {
                if (!disables[i])
                {
                    if (!Tests[i].Value(options))
                    {
                        exitCode = 1;
                    }
                    Console.WriteLine();
                }
            }

            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }

            return exitCode;
        }
    }
}
