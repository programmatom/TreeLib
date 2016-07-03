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
using System.Runtime.CompilerServices;
using System.Text;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class TestMemory : TestBase
    {
        public TestMemory()
        {
        }

        public TestMemory(string[] args)
        {
            this.args = args;
        }

        public TestMemory(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }

        public const string CallbackCommandSwitch = "-memtest-internal-command";

        private const bool ShowReports = false;

        private readonly string[] args;


        //
        // Tests
        //

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BasicMapCore(IOrderedMap<int, float> map, int count)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < count; i++)
                {
                    map.Add(i, i);
                }
                for (int i = 0; i < count; i++)
                {
                    map.Remove(i);
                }
            }
        }

        [Op("basic-map")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BasicMap()
        {
            const int Count = 1000;

            IOrderedMap<int, float> map;
            switch (args[0])
            {
                default:
                    throw new ArgumentException();
                case "avl":
                    map = new AVLTreeMap<int, float>();
                    break;
                case "redblack":
                    map = new RedBlackTreeMap<int, float>();
                    break;
                case "splay":
                    map = new SplayTreeMap<int, float>();
                    break;
            }

            BasicMapCore(map, Count);
        }

        [Op("basic-map-fixed")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BasicMapFixed()
        {
            const int Count = 1000;

            IOrderedMap<int, float> map;
            switch (args[0])
            {
                default:
                    throw new ArgumentException();
                case "avl":
                    map = new AVLTreeMap<int, float>(Count, AllocationMode.PreallocatedFixed);
                    break;
                case "redblack":
                    map = new RedBlackTreeMap<int, float>(Count, AllocationMode.PreallocatedFixed);
                    break;
                case "splay":
                    map = new SplayTreeMap<int, float>(Count, AllocationMode.PreallocatedFixed);
                    break;
            }

            BasicMapCore(map, Count);
        }

        [Op("basic-map-fixedarray")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BasicMapFixedArray()
        {
            const int Count = 1000;

            IOrderedMap<int, float> map;
            switch (args[0])
            {
                default:
                    throw new ArgumentException();
                case "avl":
                    map = new AVLTreeArrayMap<int, float>(Count, AllocationMode.PreallocatedFixed);
                    break;
                case "redblack":
                    map = new RedBlackTreeArrayMap<int, float>(Count, AllocationMode.PreallocatedFixed);
                    break;
                case "splay":
                    map = new SplayTreeArrayMap<int, float>(Count, AllocationMode.PreallocatedFixed);
                    break;
            }

            BasicMapCore(map, Count);
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void EnumerateCore(IOrderedMap<int, float> map, int count, bool fast)
        {
            for (int i = 0; i < count; i++)
            {
                map.Add(i, i);
            }
            foreach (EntryMap<int, float> entry in fast ? map.GetFastEnumerable() : map.GetRobustEnumerable())
            {
            }
        }

        [Op("enum-map")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void EnumerateMap()
        {
            const int Count = 1000;

            IOrderedMap<int, float> map;
            switch (args[0])
            {
                default:
                    throw new ArgumentException();
                case "avl":
                    map = new AVLTreeMap<int, float>();
                    break;
                case "redblack":
                    map = new RedBlackTreeMap<int, float>();
                    break;
                case "splay":
                    map = new SplayTreeMap<int, float>();
                    break;
            }

            bool fast;
            switch (args[1])
            {
                default:
                    throw new ArgumentException();
                case "fast":
                    fast = true;
                    break;
                case "robust":
                    fast = false;
                    break;
            }

            EnumerateCore(map, Count, fast);
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void ConditionalMapCore(IOrderedMap<int, float> map, int count)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < count; i++)
                {
                    map.ConditionalSetOrAdd(i, _ConditionalSetOrAdd);
                }
                for (int i = 0; i < count; i++)
                {
                    map.ConditionalSetOrRemove(i, _ConditionalSetOrRemove);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool ConditionalSetOrAdd(int key, ref float value, bool resident)
        {
            return true;
        }
        private readonly static UpdatePredicate<int, float> _ConditionalSetOrAdd = ConditionalSetOrAdd;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool ConditionalSetOrRemove(int key, ref float value, bool resident)
        {
            return true;
        }
        private readonly static UpdatePredicate<int, float> _ConditionalSetOrRemove = ConditionalSetOrRemove;

        [Op("cond-map")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void ConditionalMap()
        {
            const int Count = 1000;

            IOrderedMap<int, float> map;
            switch (args[0])
            {
                default:
                    throw new ArgumentException();
                case "avl":
                    map = new AVLTreeMap<int, float>();
                    break;
                case "redblack":
                    map = new RedBlackTreeMap<int, float>();
                    break;
                case "splay":
                    map = new SplayTreeMap<int, float>();
                    break;
            }

            ConditionalMapCore(map, Count);
        }


        //
        // Child process entry point
        //

        public enum ExitCodes : int { Success = 0, UnknownCommand = 1, Exception = 2, TestFailed = 3, UnknownError = 4 };

        public static ExitCodes Reentry(string exitCodePath, string op, string[] args)
        {
            ExitCodes exitCode = ExitCodes.UnknownError;
            Exception exception = null;

            try
            {
                MethodInfo selected;
                foreach (MethodInfo mi in typeof(TestMemory).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (CustomAttributeData attr in mi.CustomAttributes)
                    {
                        if (attr.AttributeType == typeof(OpAttribute))
                        {
                            object[] attrArgs = new object[attr.ConstructorArguments.Count];
                            for (int i = 0; i < attrArgs.Length; i++)
                            {
                                attrArgs[i] = attr.ConstructorArguments[i].Value;
                            }
                            OpAttribute opAttr = (OpAttribute)attr.Constructor.Invoke(attrArgs);
                            if (String.Equals(op, opAttr.Op))
                            {
                                selected = mi;
                                goto Found;
                            }
                        }
                    }
                }
                Debug.Assert(false, String.Format("Unknown command \"{0}\" passed to TestMemory.Reentry()", op));
                return (exitCode = ExitCodes.UnknownCommand);

            Found:
                TestMemory test = new TestMemory(args);

                selected.Invoke(test, null); // pre-JIT/warmup
                System.GC.Collect(2/*gen*/, GCCollectionMode.Forced, true/*blocking*/, true/*compacting*/);

                // Try to avoid GC to stabilize results for implementations caching workspaces via weak references.
                // compute maximum size for TryStartNoGCRegion(). Assume workstation GC (has lower limits)
                int ephemeralSegmentSize = (Environment.Is64BitProcess ? 256 : 16) * 1024 * 1024;
                System.GC.TryStartNoGCRegion(ephemeralSegmentSize);

                CLRProfilerControl.AllocationLoggingActive = true;
                selected.Invoke(test, null);
                CLRProfilerControl.AllocationLoggingActive = false;

                System.GC.EndNoGCRegion();

                return (exitCode = ExitCodes.Success);
            }
            catch (Exception exception2)
            {
                exception = exception2;
                return (exitCode = ExitCodes.Exception);
            }
            finally
            {
                // CLR Profiler doesn't pass exit code through - use a file to do so
                File.WriteAllText(exitCodePath, String.Concat((int)exitCode, Environment.NewLine, exception));
            }
        }

        private const string ProfilerDirectory = "CLRProfiler45Binaries";

        private AllocInfo[] Invoke(string op, string[] args)
        {
            IncrementIteration(true/*setLast*/);

            string ourPath = Assembly.GetExecutingAssembly().Location;

            string bits = Environment.Is64BitProcess ? "64" : "32";
            string profilerPathEnvironmentVariable = String.Format("CLR_PROFILER_PATH_{0}", bits);
            string profilerPath = Environment.GetEnvironmentVariable(profilerPathEnvironmentVariable);
            if (String.IsNullOrEmpty(profilerPath))
            {
                profilerPath = Path.Combine(
                    Environment.GetEnvironmentVariable("ProgramW6432"),
                    ProfilerDirectory,
                    bits,
                    "CLRProfiler.exe");
            }
            if (!File.Exists(profilerPath))
            {
                throw new ArgumentException(String.Format("Unable to find CLR Profiler. For {0}-bit build, expect to find profiler located at \"{1}\". To override, provide full path in environment variable \"{2}\"", bits, profilerPath, profilerPathEnvironmentVariable));
            }

            string logPath = Path.GetTempFileName();
            string exitCodePath = Path.GetTempFileName();
            string reportText = null;
            try
            {
                using (Process cmd = new Process())
                {
                    cmd.StartInfo.Arguments = String.Join(
                        " ",
                        String.Format("-o \"{0}\"", logPath),
                        "-nc",
                        "-np",
                        String.Format("-p \"{0}\"", ourPath),
                        CallbackCommandSwitch/*args[0]*/,
                        String.Format("\"{0}\"", exitCodePath)/*args[1]*/,
                        op/*args[2]*/,
                        args != null ? String.Join(" ", args) : null);
                    cmd.StartInfo.CreateNoWindow = true;
                    cmd.StartInfo.FileName = profilerPath;
                    cmd.StartInfo.UseShellExecute = false;
                    cmd.StartInfo.WorkingDirectory = Path.GetTempPath();

                    cmd.Start();
                    cmd.WaitForExit();

                    if (cmd.ExitCode != (int)ExitCodes.Success)
                    {
                        throw new UnitTestFailureException(String.Format("Memory test \"{0}\" - profiler exit code {1}", op, cmd.ExitCode));
                    }

                    using (TextReader reader = new StreamReader(exitCodePath))
                    {
                        string line = reader.ReadLine();
                        int exitCode = Int32.Parse(line);
                        while ((line = reader.ReadLine()) != null)
                        {
                            WriteLine(line);
                        }
                        if (exitCode != (int)ExitCodes.Success)
                        {
                            throw new UnitTestFailureException(String.Format("Memory test \"{0}\" - child process exit code {1} ({2})", op, exitCode, (ExitCodes)exitCode));
                        }
                    }
                }

                StringBuilder output = new StringBuilder();
                using (TextWriter outputWriter = TextWriter.Synchronized(new StringWriter(output)))
                {
                    using (Process cmd = new Process())
                    {
                        cmd.StartInfo.Arguments = String.Join(
                            " ",
                            "-a",
                            String.Format("-l \"{0}\"", logPath));
                        cmd.StartInfo.CreateNoWindow = true;
                        cmd.StartInfo.FileName = profilerPath;
                        cmd.StartInfo.UseShellExecute = false;
                        cmd.StartInfo.WorkingDirectory = Path.GetTempPath();
                        cmd.StartInfo.RedirectStandardOutput = true;
                        cmd.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) { if (e.Data != null) { outputWriter.WriteLine(e.Data); } };

                        cmd.Start();
                        cmd.BeginOutputReadLine();
                        cmd.WaitForExit();

                        if (cmd.ExitCode != (int)ExitCodes.Success)
                        {
                            // test harness defect or configuration issue
                            string message = String.Format("Profiler reporting failed with exit code {0}", cmd.ExitCode);
                            Debug.Assert(false, message);
                            throw new InvalidOperationException(message);
                        }
                    }
                }

                reportText = output.ToString();

            }
            finally
            {
                File.Delete(logPath);
                File.Delete(exitCodePath);
            }

            AllocInfo[] records = ParseAllocationReport(reportText);

#pragma warning disable CS0162 // unreachable
            if (ShowReports)
            {
                WriteLine(String.Empty);
                WriteLine("Report for {0}({1})", op, String.Join(", ", args));
                Dump(records, null);
            }
#pragma warning restore CS0162

            return records;
        }

        private struct AllocInfo
        {
            public readonly string typeName;
            public readonly int count;
            public readonly int? bytes64;
            public readonly int? bytes32;

            public int? bytes { get { return Environment.Is64BitProcess ? bytes64 : bytes32; } }

            public AllocInfo(int count, int? bytes32, int? bytes64, string typeName)
            {
                this.typeName = typeName;
                this.count = count;
                this.bytes64 = bytes64;
                this.bytes32 = bytes32;
            }
        }

        private static AllocInfo[] ParseAllocationReport(string reportText)
        {
            List<AllocInfo> records = new List<AllocInfo>();
            using (TextReader reader = new StringReader(reportText))
            {
                string line;

                line = reader.ReadLine();
                if (!line.StartsWith("Allocation summary for"))
                {
                    throw new ArgumentException();
                }
                line = reader.ReadLine();
                if (!line.StartsWith("Typename,Size(),#Instances()"))
                {
                    throw new ArgumentException();
                }
                line = reader.ReadLine();
                if (!line.StartsWith("Grand total,"))
                {
                    throw new ArgumentException();
                }

                while ((line = reader.ReadLine()) != null)
                {
                    string type, count, bytes;

                    // There may be commas in type names (separating generic type args), and CLR Profiler doesn't quote the
                    // fields like CSV should. But we know there are always 3 fields, so work backwards from last comma.

                    int i = line.LastIndexOf(',');
                    count = line.Substring(i + 1);

                    int j = line.LastIndexOf(',', i - 1);
                    bytes = line.Substring(j + 1, i - (j + 1));

                    type = line.Substring(0, j);

                    int? bytes64 = null;
                    int? bytes32 = null;
                    if (Environment.Is64BitProcess)
                    {
                        bytes64 = Int32.Parse(bytes);
                    }
                    else
                    {
                        bytes32 = Int32.Parse(bytes);
                    }
                    records.Add(new AllocInfo(Int32.Parse(count), bytes32, bytes64, type));
                }
            }

            // clean crud allocated by dynamic invocation of test method
            records.RemoveAll(
                delegate (AllocInfo candidate)
                {
                    return (String.Equals(candidate.typeName, "System.Signature") && (candidate.count == 1))
                        || (String.Equals(candidate.typeName, "System.RuntimeType []") && (candidate.count == 1));
                });

            return records.ToArray();
        }

        private static string Dequote(string s)
        {
            if ((s.Length >= 2) && s.StartsWith("\"") && s.EndsWith("\""))
            {
                s = s.Substring(1, s.Length - 2);
            }
            return s;
        }

        private static int Compare(AllocInfo left, AllocInfo right)
        {
            int c;

            c = -left.count.CompareTo(right.count);

            if ((c == 0) && left.bytes.HasValue && right.bytes.HasValue)
            {
                c = -left.bytes.Value.CompareTo(right.bytes.Value);
            }

            if (c == 0)
            {
                c = String.Compare(left.typeName, right.typeName);
            }

            return c;
        }

        private void Validate(AllocInfo[] one, AllocInfo[] two, string op, string[] args)
        {
            int? lastLine = null;
            try
            {
                TestTrue("Memory validate count", delegate () { return one.Length == two.Length; });
                one = (AllocInfo[])one.Clone();
                Array.Sort(one, Compare);
                two = (AllocInfo[])two.Clone();
                Array.Sort(two, Compare);
                for (int i = 0; i < one.Length; i++)
                {
                    lastLine = i;
                    TestTrue("Memory validate name", delegate () { return String.Equals(one[i].typeName, two[i].typeName); });
                    TestTrue("Memory validate count", delegate () { return one[i].count == two[i].count; });
                    if (one[i].bytes.HasValue)
                    {
                        TestTrue("Memory validate bytes", delegate () { return one[i].bytes.Value == two[i].bytes.Value; });
                    }
                }
            }
            catch (Exception)
            {
                WriteLine("Validate memory allocations failed:");
                WriteLine("Test: {0}({1})", op, String.Join(", ", args));
                WriteInfoLine("Count", "Bytes", "Type", false);
                WriteLine("EXPECTED:");
                Dump(one, lastLine);
                WriteLine("ACTUAL:");
                Dump(two, lastLine);
                throw;
            }
        }

        private void Dump(AllocInfo[] records, int? errorLine)
        {
            for (int i = 0; i < records.Length; i++)
            {
                AllocInfo record = records[i];
                WriteInfoLine(record.count.ToString(), record.bytes.ToString(), record.typeName, errorLine.HasValue && (errorLine.Value == i));
            }
        }

        private void WriteInfoLine(string count, string bytes, string typeName, bool errorLine)
        {
            WriteLine("  {3}  {0,6}  {1,8}  {2}", count, !String.IsNullOrEmpty(bytes) ? bytes : "*", typeName, errorLine ? "*" : " ");
        }

        private void Test(string op, string[] args, AllocInfo[] expected)
        {
            AllocInfo[] actual = Invoke(op, args);
            Validate(expected, actual, op, args);
        }


        //
        // Main entry point
        //

        public override bool Do()
        {
            try
            {
                // corrective factors for debug-only state
#if DEBUG
                const int a = 0, A = 0;
                const int b = 0, B = 0;
                const int c = 0, C = 0;
#else
                const int a = -8, A = -8;
                const int b = -8, B = -8;
                const int c = -8, C = -8;
#endif

                Test("basic-map", new string[] { "avl" }, new AllocInfo[] {
                    new AllocInfo(2000, 56000, 96000, "TreeLib.AVLTreeMap<T,U>.Node"),
                    new AllocInfo(1, 380, 760, "TreeLib.AVLTreeMap<T,U>.Node []"),
                    new AllocInfo(1, 48 + a, 72 + A, "TreeLib.AVLTreeMap<T,U>"),
                    new AllocInfo(1, 12, 24, "System.WeakReference<T>"), });
                Test("basic-map", new string[] { "redblack" }, new AllocInfo[] {
                    new AllocInfo(2000, 56000, 96000, "TreeLib.RedBlackTreeMap<T,U>.Node"),
                    new AllocInfo(1, 44 + b, 64 + B, "TreeLib.RedBlackTreeMap<T,U>"), });
                Test("basic-map", new string[] { "splay" }, new AllocInfo[] {
                    new AllocInfo(2002, 48048, 80080, "TreeLib.SplayTreeMap<T,U>.Node"),
                    new AllocInfo(1, 52 + c, 80 + C, "TreeLib.SplayTreeMap<T,U>"), });

                Test("basic-map-fixed", new string[] { "avl" }, new AllocInfo[] {
                    new AllocInfo(1000, 28000, 48000, "TreeLib.AVLTreeMap<T,U>.Node"),
                    new AllocInfo(1, 380, 760, "TreeLib.AVLTreeMap<T,U>.Node []"),
                    new AllocInfo(1, 48 + a, 72 + A, "TreeLib.AVLTreeMap<T,U>"),
                    new AllocInfo(1, 12, 24, "System.WeakReference<T>"), });
                Test("basic-map-fixed", new string[] { "redblack" }, new AllocInfo[] {
                    new AllocInfo(1000, 28000, 48000, "TreeLib.RedBlackTreeMap<T,U>.Node"),
                    new AllocInfo(1, 44 + b, 64 + B, "TreeLib.RedBlackTreeMap<T,U>"), });
                Test("basic-map-fixed", new string[] { "splay" }, new AllocInfo[] {
                    new AllocInfo(1002, 24048, 40080, "TreeLib.SplayTreeMap<T,U>.Node"),
                    new AllocInfo(1, 52 + c, 80 + C, "TreeLib.SplayTreeMap<T,U>"), });

                Test("basic-map-fixedarray", new string[] { "avl" }, new AllocInfo[] {
                    new AllocInfo(1, 20012, 32024, "TreeLib.AVLTreeArrayMap<T,U>.Node []"),
                    new AllocInfo(1, 380, 392, "TreeLib.AVLTreeArrayMap<T,U>.NodeRef []"),
                    new AllocInfo(1, 40, 72, "TreeLib.AVLTreeArrayMap<T,U>"),
                    new AllocInfo(1, 12, 24, "System.WeakReference<T>"), });
                Test("basic-map-fixedarray", new string[] { "redblack" }, new AllocInfo[] {
                    new AllocInfo(1, 20012, 32024, "TreeLib.RedBlackTreeArrayMap<T,U>.Node []"),
                    new AllocInfo(1, 36, 64, "TreeLib.RedBlackTreeArrayMap<T,U>"), });
                Test("basic-map-fixedarray", new string[] { "splay" }, new AllocInfo[] {
                    new AllocInfo(1, 16044, 24072, "TreeLib.SplayTreeArrayMap<T,U>.Node []"),
                    new AllocInfo(1, 36, 64, "TreeLib.SplayTreeArrayMap<T,U>"), });


                Test("enum-map", new string[] { "avl", "fast" }, new AllocInfo[] {
                    new AllocInfo(1000, 28000, 48000, "TreeLib.AVLTreeMap<T,U>.Node"),
                    new AllocInfo(1, 380, 760, "TreeLib.AVLTreeMap<T,U>.Node []"),
                    new AllocInfo(1, 48 + a, 72 + A, "TreeLib.AVLTreeMap<T,U>"),
                    new AllocInfo(1, 32, 48, "TreeLib.AVLTreeMap<T,U>.FastEnumeratorThreaded"),
                    new AllocInfo(1, 20, 32, "TreeLib.AVLTreeMap<T,U>.FastEnumerableSurrogate"),
                    new AllocInfo(1, 12, 24, "System.WeakReference<T>"), });
                Test("enum-map", new string[] { "redblack", "fast" }, new AllocInfo[] {
                    new AllocInfo(1000, 28000, 48000, "TreeLib.RedBlackTreeMap<T,U>.Node"),
                    new AllocInfo(1, 96, 192, "TreeLib.Internal.STuple<T> []"),
                    new AllocInfo(1, 44, 72, "TreeLib.RedBlackTreeMap<T,U>.FastEnumerator"),
                    new AllocInfo(1, 44 + b, 64 + B, "TreeLib.RedBlackTreeMap<T,U>"),
                    new AllocInfo(1, 20, 32, "TreeLib.RedBlackTreeMap<T,U>.FastEnumerableSurrogate"), });
                Test("enum-map", new string[] { "splay", "fast" }, new AllocInfo[] {
                    new AllocInfo(1002, 24048, 40080, "TreeLib.SplayTreeMap<T,U>.Node"),
                    new AllocInfo(6, 8136, 16272, "TreeLib.Internal.STuple<T> []"),
                    new AllocInfo(1, 52 + c, 80 + C, "TreeLib.SplayTreeMap<T,U>"),
                    new AllocInfo(1, 44, 72, "TreeLib.SplayTreeMap<T,U>.FastEnumerator"),
                    new AllocInfo(1, 20, 32, "TreeLib.SplayTreeMap<T,U>.FastEnumerableSurrogate"), });

                Test("enum-map", new string[] { "avl", "robust" }, new AllocInfo[] {
                    new AllocInfo(1000, 28000, 48000, "TreeLib.AVLTreeMap<T,U>.Node"),
                    new AllocInfo(1, 380, 760, "TreeLib.AVLTreeMap<T,U>.Node []"),
                    new AllocInfo(1, 48 + a, 72 + A, "TreeLib.AVLTreeMap<T,U>"),
                    new AllocInfo(1, 28, 40, "TreeLib.AVLTreeMap<T,U>.RobustEnumerator"),
                    new AllocInfo(1, 20, 32, "TreeLib.AVLTreeMap<T,U>.RobustEnumerableSurrogate"),
                    new AllocInfo(1, 12, 24, "System.WeakReference<T>"), });
                Test("enum-map", new string[] { "redblack", "robust" }, new AllocInfo[] {
                    new AllocInfo(1000, 28000, 48000, "TreeLib.RedBlackTreeMap<T,U>.Node"),
                    new AllocInfo(1, 44 + b, 64 + B, "TreeLib.RedBlackTreeMap<T,U>"),
                    new AllocInfo(1, 28, 40, "TreeLib.RedBlackTreeMap<T,U>.RobustEnumerator"),
                    new AllocInfo(1, 20, 32, "TreeLib.RedBlackTreeMap<T,U>.RobustEnumerableSurrogate"), });
                Test("enum-map", new string[] { "splay", "robust" }, new AllocInfo[] {
                    new AllocInfo(1002, 24048, 40080, "TreeLib.SplayTreeMap<T,U>.Node"),
                    new AllocInfo(1, 52 + c, 80 + C, "TreeLib.SplayTreeMap<T,U>"),
                    new AllocInfo(1, 28, 40, "TreeLib.SplayTreeMap<T,U>.RobustEnumerator"),
                    new AllocInfo(1, 20, 32, "TreeLib.SplayTreeMap<T,U>.RobustEnumerableSurrogate"), });


                Test("cond-map", new string[] { "avl" }, new AllocInfo[] {
                    new AllocInfo(2000, 56000, 96000, "TreeLib.AVLTreeMap<T,U>.Node"),
                    new AllocInfo(1, 380, 760, "TreeLib.AVLTreeMap<T,U>.Node []"),
                    new AllocInfo(1, 48 + a, 72 + A, "TreeLib.AVLTreeMap<T,U>"),
                    new AllocInfo(1, 12, 24, "System.WeakReference<T>"), });
                Test("cond-map", new string[] { "redblack" }, new AllocInfo[] {
                    new AllocInfo(2000, 56000, 96000, "TreeLib.RedBlackTreeMap<T,U>.Node"),
                    new AllocInfo(1, 44 + b, 64 + B, "TreeLib.RedBlackTreeMap<T,U>"), });
                Test("cond-map", new string[] { "splay" }, new AllocInfo[] {
                    new AllocInfo(2002, 48048, 80080, "TreeLib.SplayTreeMap<T,U>.Node"),
                    new AllocInfo(1, 52 + c, 80 + C, "TreeLib.SplayTreeMap<T,U>"), });


                return true;
            }
            catch (Exception)
            {
                WriteIteration();
                throw;
            }
        }
    }

    public class OpAttribute : Attribute
    {
        public string Op { get; set; }

        public OpAttribute(string op)
        {
            Op = op;
        }
    }
}
