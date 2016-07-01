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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class TestBase
    {
        public TestBase()
        {
        }

        public TestBase(long[] breakIterations, long startIteration)
        {
            this.iteration = startIteration;
            this.breakIterations = breakIterations;
        }



        //
        // Test iteration count and breaking
        //

        public long iteration { get; private set; }
        private readonly long[] breakIterations = new long[0];

        protected long lastActionIteration;

        public long IncrementIteration(bool setLast)
        {
            iteration = unchecked(iteration + 1);
            if (setLast)
            {
                lastActionIteration = iteration;
            }
            if (Array.IndexOf(breakIterations, iteration) >= 0)
            {
                Debug.Assert(false, String.Format("BREAK AT ITERATION {0}", iteration));
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }
            return iteration;
        }

        public long IncrementIteration()
        {
            return IncrementIteration(false/*setLast*/);
        }

        protected int? seed;
        public void SetSeed(int seed)
        {
            this.seed = seed;
        }

        public void WriteIteration()
        {
            WriteLine(ConsoleColor.Yellow, "LAST ITERATION {0}, LAST ACTION ITERATION {1}", iteration, lastActionIteration);
            WriteLine("To repro, add to command line: {0}break:{1}", seed.HasValue ? String.Format("seed:{0} ", seed.Value) : null, iteration);
        }

        private ConsoleBuffer consoleBuffer;

        public ConsoleBuffer ConsoleBuffer { set { consoleBuffer = value; } }

        public void WriteLine(string line)
        {
            if (consoleBuffer != null)
            {
                consoleBuffer.WriteText(line);
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine((ConsoleColor?)null, format, args);
        }

        public void WriteLine(ConsoleColor? color, string format, params object[] args)
        {
            string line = String.Format(format, args);
            if (consoleBuffer != null)
            {
                consoleBuffer.WriteText(line);
            }
            else
            {
                ConsoleColor savedColor = Console.ForegroundColor;
                Console.ForegroundColor = color.HasValue ? color.Value : savedColor;
                Console.WriteLine(line);
                Console.ForegroundColor = savedColor;
            }
        }

        protected void ShowException(string testName, Exception exception)
        {
            WriteIteration();
            WriteLine("{0} Failure: {1}", testName, exception.Message);
            if (exception.InnerException != null)
            {
                WriteLine("  inner: {0}", exception.InnerException.Message);
            }
            Debug.Assert(false, exception.ToString());
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }



        //
        // Test check/validation methods
        //

        public delegate void VoidAction();
        public delegate bool BoolAction();

        public void TestNoThrow(string label, VoidAction action)
        {
            IncrementIteration();
            try
            {
                action();
            }
            catch (Exception exception)
            {
                WriteIteration();
                Console.WriteLine("Unexpected exception occurred: {0}", exception);
                throw new UnitTestFailureException(label, exception);
            }
        }

        public void TestThrow(string label, Type exceptionType, VoidAction action)
        {
            IncrementIteration();
            try
            {
                action();
                Console.WriteLine("Expected exception did not occur");
                throw new UnitTestFailureException(label, new Exception("Expected exception did not occur"));
            }
            catch (Exception exception) when (exceptionType.IsAssignableFrom(exception.GetType()))
            {
            }
            catch (TargetInvocationException exception)
            {
                if (exceptionType.IsAssignableFrom(exception.InnerException.GetType()))
                {
                    // acceptable - test may have used reflection to invoke target method on object being tested
                }
                else
                {
                    WriteIteration();
                    Console.WriteLine("Unexpected exception occurred: {0}", exception);
                    throw new UnitTestFailureException(label, exception);
                }
            }
            catch (Exception exception)
            {
                WriteIteration();
                Console.WriteLine("Unexpected exception occurred: {0}", exception);
                throw new UnitTestFailureException(label, exception);
            }
        }

        public void TestBool(string label, bool value, BoolAction action)
        {
            IncrementIteration();
            try
            {
                if (value != action())
                {
                    throw new UnitTestFailureException(label);
                }
            }
            catch (Exception exception) when (!(exception is UnitTestFailureException))
            {
                WriteIteration();
                Console.WriteLine("Unexpected exception occurred: {0}", exception);
                throw new UnitTestFailureException(label, exception);
            }
        }

        public void TestTrue(string label, BoolAction action)
        {
            TestBool(label, true, action);
        }

        public void TestFalse(string label, BoolAction action)
        {
            TestBool(label, false, action);
        }

        public void Fault(object faultingObject, string description, Exception innerException)
        {
            WriteIteration();
            string message = String.Format("{0}: {1}", faultingObject != null ? faultingObject.GetType().Name : "<null>", description);
            if (innerException != null)
            {
                message = String.Concat(message, Environment.NewLine, "Initial exception: ", innerException);
            }
            Console.WriteLine(message);
            bool throwError = true;
            Debug.Assert(false, description);
            Debugger.Break();
            if (throwError)
            {
                throw new UnitTestFailureException(message);
            }
        }

        public void Fault(object faultingObject, string description)
        {
            Fault(faultingObject, description, null);
        }



        //
        // Basic tree validation
        //

        // validates tree supporting INonInvasiveTreeInspection, otherwise does nothing
        protected void ValidateTree(object tree)
        {
            IncrementIteration();

            INonInvasiveTreeInspection treeInspector;
            if ((treeInspector = tree as INonInvasiveTreeInspection) != null)
            {
                try
                {
                    IncrementIteration();
                    treeInspector.Validate();
                }
                catch (Exception exception)
                {
                    bool throwFailure = true;
                    WriteIteration();
                    WriteLine("[validate]: Unexpected exception occurred: {0}", exception);
                    Debug.Assert(false);
                    Debugger.Break();
                    if (throwFailure)
                    {
                        throw new UnitTestFailureException("validate", exception);
                    }
                }
            }
        }


        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree,
            out int maxDepth,
            bool propagateValue) where KeyType : IComparable<KeyType>
        {
            if (tree is ISimpleTreeInspection<KeyType, ValueType>)
            {
                maxDepth = 0;
                return ((ISimpleTreeInspection<KeyType, ValueType>)tree).ToArray();
            }

            List<EntryMap<KeyType, ValueType>> items = new List<EntryMap<KeyType, ValueType>>();

            maxDepth = 0;
            Stack<object> stack = new Stack<object>();
            object current = tree.Root;
            while (current != null)
            {
                stack.Push(current);
                current = tree.GetLeftChild(current);
            }
            maxDepth = stack.Count;
            while (stack.Count != 0)
            {
                current = stack.Pop();
                KeyType key = (KeyType)tree.GetKey(current);
                ValueType value = propagateValue ? (ValueType)tree.GetValue(current) : default(ValueType);
                items.Add(new EntryMap<KeyType, ValueType>(key, value, null, 0));

                object node = tree.GetRightChild(current);
                while (node != null)
                {
                    stack.Push(node);
                    node = tree.GetLeftChild(node);
                }
                maxDepth = Math.Max(maxDepth, stack.Count);
            }

            return items.ToArray();
        }

        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree,
            out int maxDepth) where KeyType : IComparable<KeyType>
        {
            return Flatten<KeyType, ValueType>(tree, out maxDepth, true/*propagateValue*/);
        }

        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree,
            bool propagateValue) where KeyType : IComparable<KeyType>
        {
            int maxDepth;
            return Flatten<KeyType, ValueType>(tree, out maxDepth, propagateValue);
        }

        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree) where KeyType : IComparable<KeyType>
        {
            int maxDepth;
            return Flatten<KeyType, ValueType>(tree, out maxDepth, true/*propagateValue*/);
        }

        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            ISimpleTreeInspection<KeyType, ValueType> list) where KeyType : IComparable<KeyType>
        {
            return list.ToArray();
        }


        private static void Dump(INonInvasiveTreeInspection tree, object root, int level, TextWriter writer)
        {
            if (root == null)
            {
                writer.WriteLine("{0}<NULL>", new string(' ', 4 * level));
            }
            else
            {
                Dump(tree, tree.GetLeftChild(root), level + 1, writer);
                object key = tree.GetKey(root);
                object value = tree.GetValue(root);
                writer.WriteLine("{0}<{1},{2}>:{3}", new string(' ', 4 * level), key, value, tree.GetMetadata(root));
                Dump(tree, tree.GetRightChild(root), level + 1, writer);
            }
        }

        public static StringBuilder Dump(INonInvasiveTreeInspection tree)
        {
            StringBuilder sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb))
            {
                Dump(tree, tree.Root, 0, writer);
            }
            return sb;
        }


        protected struct MultiRankInfo<KeyType, ValueType> : IComparable<MultiRankInfo<KeyType, ValueType>> where KeyType : IComparable<KeyType>
        {
            public readonly KeyType Key;
            public readonly ValueType Value;
            public readonly int Start;
            public readonly int Length;

            public MultiRankInfo(KeyType key, ValueType value, int start, int length)
            {
                this.Key = key;
                this.Value = value;
                this.Start = start;
                this.Length = length;
            }

            public MultiRankInfo(KeyType key)
                : this(key, default(ValueType), 0, 0)
            {
            }

            public int CompareTo(MultiRankInfo<KeyType, ValueType> other)
            {
                return Comparer<KeyType>.Default.Compare(this.Key, other.Key);
            }
        }

        protected MultiRankInfo<KeyType, ValueType>[] FlattenAnyRankTree<KeyType, ValueType>(object tree, bool multi) where KeyType : IComparable<KeyType>
        {
            if (tree is INonInvasiveMultiRankMapInspection)
            {
                MultiRankMapEntry[] ranks;
                ranks = ((INonInvasiveMultiRankMapInspection)tree).GetRanks();
                MultiRankInfo<KeyType, ValueType>[] result = new MultiRankInfo<KeyType, ValueType>[ranks.Length];
                for (int i = 0; i < ranks.Length; i++)
                {
                    result[i] = new MultiRankInfo<KeyType, ValueType>(
                        (KeyType)ranks[i].key,
                        (ValueType)ranks[i].value,
                        ranks[i].rank.start,
                        multi ? ranks[i].rank.length : 1);
                }
                return result;
            }
            else if (tree is INonInvasiveMultiRankMapInspectionLong)
            {
                MultiRankMapEntryLong[] ranks;
                ranks = ((INonInvasiveMultiRankMapInspectionLong)tree).GetRanks();
                MultiRankInfo<KeyType, ValueType>[] result = new MultiRankInfo<KeyType, ValueType>[ranks.Length];
                for (int i = 0; i < ranks.Length; i++)
                {
                    result[i] = new MultiRankInfo<KeyType, ValueType>(
                        (KeyType)ranks[i].key,
                        (ValueType)ranks[i].value,
                        (int)ranks[i].rank.start,
                        multi ? (int)ranks[i].rank.length : 1);
                }
                return result;
            }
            throw new ArgumentException();
        }


        protected void ValidateMap<KeyType, ValueType>(EntryMap<KeyType, ValueType>[] items) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            for (int i = 1; i < items.Length; i++)
            {
                TestTrue("order", delegate () { return Comparer<KeyType>.Default.Compare(items[i - 1].Key, items[i].Key) < 0; });
            }
        }

        protected void ValidateMapsEqual<KeyType, ValueType>(EntryMap<KeyType, ValueType>[] items1, EntryMap<KeyType, ValueType>[] items2) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            TestTrue("equal count", delegate () { return items1.Length == items2.Length; });
            for (int i = 0; i < items1.Length; i++)
            {
                TestTrue("key", delegate () { return Comparer<KeyType>.Default.Compare(items1[i].Key, items2[i].Key) == 0; });
                TestTrue("value", delegate () { return Comparer<ValueType>.Default.Compare(items1[i].Value, items2[i].Value) == 0; });
            }
        }

        protected void ValidateMapsEqual<KeyType, ValueType>(IOrderedMap<KeyType, ValueType> tree1, IOrderedMap<KeyType, ValueType> tree2) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            ValidateTree(tree1);
            EntryMap<KeyType, ValueType>[] items1 = Flatten<KeyType, ValueType>((INonInvasiveTreeInspection)tree1);
            ValidateMap<KeyType, ValueType>(items1);
            ValidateTree(tree2);
            EntryMap<KeyType, ValueType>[] items2 = Flatten<KeyType, ValueType>((INonInvasiveTreeInspection)tree2);
            ValidateMap<KeyType, ValueType>(items2);
            ValidateMapsEqual<KeyType, ValueType>(items1, items2);
        }


        protected void ValidateRanks<KeyType, ValueType>(MultiRankMapEntry[] ranks, bool multi) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            int offset = 0;
            for (int i = 0; i < ranks.Length; i++)
            {
                TestTrue("start", delegate () { return offset == ranks[i].rank.start; });
                TestTrue("count > 0", delegate () { return multi ? (ranks[i].rank.length >= 1) : (ranks[i].rank.length == 1); });
                offset += ranks[i].rank.length;
            }
        }

        protected void ValidateRanksEqual<KeyType, ValueType>(MultiRankMapEntry[] ranks1, MultiRankMapEntry[] ranks2) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            TestTrue("equal count", delegate () { return ranks1.Length == ranks2.Length; });
            for (int i = 0; i < ranks1.Length; i++)
            {
                TestTrue("key", delegate () { return Comparer<KeyType>.Default.Compare((KeyType)ranks1[i].key, (KeyType)ranks2[i].key) == 0; });
                TestTrue("value", delegate () { return Comparer<ValueType>.Default.Compare((ValueType)ranks1[i].value, (ValueType)ranks2[i].value) == 0; });
                TestTrue("start", delegate () { return ranks1[i].rank.start == ranks2[i].rank.start; });
                TestTrue("length", delegate () { return ranks1[i].rank.length == ranks2[i].rank.length; });
            }
        }

        protected void ValidateRanksEqual<KeyType, ValueType>(IRankMap<KeyType, ValueType> tree1, IRankMap<KeyType, ValueType> tree2) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            ValidateTree(tree1);
            MultiRankMapEntry[] ranks1 = ((INonInvasiveMultiRankMapInspection)tree1).GetRanks();
            ValidateRanks<KeyType, ValueType>(ranks1, false/*multi*/);
            ValidateTree(tree2);
            MultiRankMapEntry[] ranks2 = ((INonInvasiveMultiRankMapInspection)tree2).GetRanks();
            ValidateRanks<KeyType, ValueType>(ranks2, false/*multi*/);
            ValidateRanksEqual<KeyType, ValueType>(ranks1, ranks2);
        }

        protected void ValidateRanksEqual<KeyType, ValueType>(IMultiRankMap<KeyType, ValueType> tree1, IMultiRankMap<KeyType, ValueType> tree2) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            ValidateTree(tree1);
            MultiRankMapEntry[] ranks1 = ((INonInvasiveMultiRankMapInspection)tree1).GetRanks();
            ValidateRanks<KeyType, ValueType>(ranks1, true/*multi*/);
            ValidateTree(tree2);
            MultiRankMapEntry[] ranks2 = ((INonInvasiveMultiRankMapInspection)tree2).GetRanks();
            ValidateRanks<KeyType, ValueType>(ranks2, true/*multi*/);
            ValidateRanksEqual<KeyType, ValueType>(ranks1, ranks2);
            TestTrue("GetExtent", delegate () { return tree1.RankCount == tree2.RankCount; });
        }



        //
        // Support for testing IEnumerable
        //

        [Flags]
        public enum TreeKind
        {
            None = 0,

            Map = 1 << 0,
            List = 1 << 1,
            AllSimple = Map | List,

            RankMap = 1 << 2,
            RankList = 1 << 3,
            MultiRankMap = 1 << 4,
            MultiRankList = 1 << 5,
            AllRank = RankMap | RankList | MultiRankMap | MultiRankList,
            AllUniRank = RankMap | RankList,
            AllMultiRank = MultiRankMap | MultiRankList,

            RangeMap = 1 << 6,
            RangeList = 1 << 7,
            Range2Map = 1 << 8,
            Range2List = 1 << 9,
            AllIndexed = RangeMap | RangeList | Range2Map | Range2List,
            AllIndexed2 = Range2Map | Range2List,

            AllKeyed = Map | List | RankMap | RankList | MultiRankMap | MultiRankList,

            AllValued = Map | RankMap | MultiRankMap | RangeMap | Range2Map,


            Splay = 1 << 10, // has unique restrictions whose testing must be special-cased
        }

        [Flags]
        public enum EnumKind
        {
            None = 0,

            Default = 1 << 0,
            DefaultOld = 1 << 1,

            Enumerable = 1 << 2,
            EnumerableBidir = 1 << 3,
            EnumerableFast = 1 << 4,
            EnumerableFastBidir = 1 << 5,
            EnumerableRobust = 1 << 6,
            EnumerableRobustBidir = 1 << 7,
            AllEnumerable = Enumerable | EnumerableBidir | EnumerableFast
                | EnumerableFastBidir | EnumerableRobust | EnumerableRobustBidir,

            KeyedEnumerable = 1 << 8,
            KeyedEnumerableBidir = 1 << 9,
            KeyedEnumerableFast = 1 << 10,
            KeyedEnumerableFastBidir = 1 << 11,
            KeyedEnumerableRobust = 1 << 12,
            KeyedEnumerableRobustBidir = 1 << 13,
            AllKeyedEnumerable = KeyedEnumerable | KeyedEnumerableBidir | KeyedEnumerableFast
                | KeyedEnumerableFastBidir | KeyedEnumerableRobust | KeyedEnumerableRobustBidir,

            IndexedEnumerable = 1 << 14,
            IndexedEnumerableBidir = 1 << 15,
            IndexedEnumerableFast = 1 << 16,
            IndexedEnumerableFastBidir = 1 << 17,
            IndexedEnumerableRobust = 1 << 18,
            IndexedEnumerableRobustBidir = 1 << 19,
            AllIndexedEnumerable = IndexedEnumerable | IndexedEnumerableBidir | IndexedEnumerableFast
                | IndexedEnumerableFastBidir | IndexedEnumerableRobust | IndexedEnumerableRobustBidir,

            Indexed2Enumerable = 1 << 20,
            Indexed2EnumerableBidir = 1 << 21,
            Indexed2EnumerableFast = 1 << 22,
            Indexed2EnumerableFastBidir = 1 << 23,
            Indexed2EnumerableRobust = 1 << 24,
            Indexed2EnumerableRobustBidir = 1 << 25,
            AllIndexed2Enumerable = Indexed2Enumerable | Indexed2EnumerableBidir | Indexed2EnumerableFast | Indexed2EnumerableFastBidir | Indexed2EnumerableRobust | Indexed2EnumerableRobustBidir,

            AllDefault = Enumerable | EnumerableBidir | KeyedEnumerable | KeyedEnumerableBidir
                | IndexedEnumerable | IndexedEnumerableBidir | Indexed2Enumerable | Indexed2EnumerableBidir,

            AllFast = EnumerableFast | EnumerableFastBidir | KeyedEnumerableFast | KeyedEnumerableFastBidir
                | IndexedEnumerableFast | IndexedEnumerableFastBidir | Indexed2EnumerableFast | Indexed2EnumerableFastBidir,

            AllRobust = EnumerableRobust | EnumerableRobustBidir | KeyedEnumerableRobust | KeyedEnumerableRobustBidir
                | IndexedEnumerableRobust | IndexedEnumerableRobustBidir | Indexed2EnumerableRobust | Indexed2EnumerableRobustBidir,

            AllBidir = EnumerableBidir | EnumerableFastBidir | EnumerableRobustBidir
                | KeyedEnumerableBidir | KeyedEnumerableFastBidir | KeyedEnumerableRobustBidir
                | IndexedEnumerableBidir | IndexedEnumerableFastBidir | IndexedEnumerableRobustBidir
                | Indexed2EnumerableBidir | Indexed2EnumerableFastBidir | Indexed2EnumerableRobustBidir,
        }

        public class EnumArgsProvider
        {
            public virtual void AddArgs(List<object> args)
            {
            }
        }

        public class EnumKeyedArgsProvider<KeyType> : EnumArgsProvider where KeyType : IComparable<KeyType>
        {
            private readonly KeyType key;

            public EnumKeyedArgsProvider(KeyType key)
            {
                this.key = key;
            }

            public override void AddArgs(List<object> args)
            {
                args.Add(key);
            }
        }

        public class EnumIndexedArgsProvider : EnumArgsProvider
        {
            private readonly int value;

            public EnumIndexedArgsProvider(int value)
            {
                this.value = value;
            }

            public override void AddArgs(List<object> args)
            {
                args.Add(value);
            }
        }

        public class EnumIndexed2ArgsProvider : EnumArgsProvider
        {
            private readonly int value;
            private readonly Side side;

            public EnumIndexed2ArgsProvider(int value, Side side)
            {
                this.value = value;
                this.side = side;
            }

            public override void AddArgs(List<object> args)
            {
                args.Add(value);
                args.Add(side);
            }
        }

        public class OldEnumerableReverse<EntryTypee> : IEnumerable<EntryTypee>
        {
            private readonly IEnumerable inner;

            public OldEnumerableReverse(IEnumerable inner)
            {
                this.inner = inner;
            }

            public IEnumerator<EntryTypee> GetEnumerator()
            {
                return new AdaptEnumeratorOldReverse(inner.GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            private class AdaptEnumeratorOldReverse : IEnumerator<EntryTypee>
            {
                private readonly IEnumerator inner;

                public AdaptEnumeratorOldReverse(IEnumerator inner)
                {
                    this.inner = inner;
                }

                public EntryTypee Current { get { return (EntryTypee)inner.Current; } }

                object IEnumerator.Current { get { return this.Current; } }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    return inner.MoveNext();
                }

                public void Reset()
                {
                    inner.Reset();
                }
            }
        }

        public static IEnumerable<EntryType> GetEnumerator<EntryType>(object o, EnumKind enumKind, TreeKind treeKind, EnumArgsProvider argsProvider, bool? forward)
        {
            Debug.Assert(((int)enumKind & ((int)enumKind - 1)) == 0);

            if (enumKind == EnumKind.Default)
            {
                return (IEnumerable<EntryType>)o;
            }
            else if (enumKind == EnumKind.DefaultOld)
            {
                return new OldEnumerableReverse<EntryType>((IEnumerable)o);
            }
            else
            {
                List<object> args = new List<object>();

                argsProvider.AddArgs(args);
                Debug.Assert((args.Count == 0) || !((enumKind & EnumKind.AllEnumerable) != 0));
                Debug.Assert((args.Count == 1) || !((enumKind & EnumKind.KeyedEnumerable) != 0));
                Debug.Assert(((args.Count == 1) && ((args[0] is int) || (args[0] is long)))
                    || !((enumKind & EnumKind.IndexedEnumerable) != 0));
                Debug.Assert(((args.Count == 2) && ((args[0] is int) || (args[0] is long)) && (args[1] is Side))
                    || !((enumKind & EnumKind.Indexed2Enumerable) != 0));

                Debug.Assert(forward.HasValue == ((enumKind & EnumKind.AllBidir) != 0));
                if (forward.HasValue)
                {
                    args.Add(forward.Value);
                }

                IEnumerable<EntryType> enumerable;
                if ((enumKind & EnumKind.AllDefault) != 0)
                {
                    enumerable = (IEnumerable<EntryType>)o.GetType().InvokeMember("GetEnumerable", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, o, args.ToArray());
                }
                else if ((enumKind & EnumKind.AllFast) != 0)
                {
                    enumerable = (IEnumerable<EntryType>)o.GetType().InvokeMember("GetFastEnumerable", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, o, args.ToArray());
                }
                else if ((enumKind & EnumKind.AllRobust) != 0)
                {
                    enumerable = (IEnumerable<EntryType>)o.GetType().InvokeMember("GetRobustEnumerable", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, o, args.ToArray());
                }
                else
                {
                    Debug.Assert(false);
                    throw new ArgumentException();
                }

                return enumerable;
            }
        }

        public struct BigEntry<KeyType, ValueType>
        {
            public readonly KeyType key;
            public readonly ValueType value;
            public readonly int xStart;
            public readonly int xLength;
            public readonly int yStart;
            public readonly int yLength;

            public BigEntry(KeyType key, ValueType value)
                : this()
            {
                this.key = key;
                this.value = value;
            }

            public BigEntry(KeyType key, ValueType value, int count)
                : this()
            {
                this.key = key;
                this.value = value;
                this.xLength = count;
            }

            public BigEntry(KeyType key, ValueType value, int xStart, int xLength, int yStart, int yLength)
            {
                this.key = key;
                this.value = value;
                this.xStart = xStart;
                this.xLength = xLength;
                this.yStart = yStart;
                this.yLength = yLength;
            }

            public BigEntry(object entry, TreeKind treeKind)
                : this()
            {
                Type type = entry.GetType();


                FieldInfo keyAccessor = type.GetField("key", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(((treeKind & TreeKind.AllKeyed) != 0) == (keyAccessor != null));
                if (keyAccessor != null)
                {
                    key = (KeyType)keyAccessor.GetValue(entry);
                }
                PropertyInfo keyProperty = type.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
                if (keyProperty != null)
                {
                    object v = keyProperty.GetValue(entry);
                    if (!v.Equals(key))
                    {
                        throw new InvalidOperationException("Key");
                    }
                }

                FieldInfo valueAccessor = type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(((treeKind & TreeKind.AllValued) != 0) == (valueAccessor != null));
                if (valueAccessor != null)
                {
                    value = (ValueType)valueAccessor.GetValue(entry);
                }
                PropertyInfo valueProperty = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                if (valueProperty != null)
                {
                    object v = valueProperty.GetValue(entry);
                    if (!v.Equals(value))
                    {
                        throw new InvalidOperationException("Value");
                    }
                }


                FieldInfo xStartAccessor = type.GetField("xStart", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(((treeKind & (TreeKind.AllIndexed | TreeKind.AllRank)) != 0) == (xStartAccessor != null));
                if (xStartAccessor != null)
                {
                    object r = xStartAccessor.GetValue(entry);
                    xStart = r is int ? (int)r : (int)(long)r;
                }
                PropertyInfo xStartProperty = type.GetProperty("XStart", BindingFlags.Instance | BindingFlags.Public);
                if (xStartProperty != null)
                {
                    object v = xStartProperty.GetValue(entry);
                    if (!v.Equals(xStart))
                    {
                        throw new InvalidOperationException("XStart");
                    }
                }
                PropertyInfo rankProperty = type.GetProperty("Rank", BindingFlags.Instance | BindingFlags.Public);
                if (rankProperty != null)
                {
                    object v = rankProperty.GetValue(entry);
                    if (!v.Equals(xStart))
                    {
                        throw new InvalidOperationException("Rank");
                    }
                }
                PropertyInfo startProperty = type.GetProperty("Start", BindingFlags.Instance | BindingFlags.Public);
                if (startProperty != null)
                {
                    object v = startProperty.GetValue(entry);
                    if (!v.Equals(xStart))
                    {
                        throw new InvalidOperationException("Start");
                    }
                }

                FieldInfo xLengthAccessor = type.GetField("xLength", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(((treeKind & (TreeKind.AllIndexed | TreeKind.AllMultiRank)) != 0) == (xLengthAccessor != null));
                if (xLengthAccessor != null)
                {
                    object r = xLengthAccessor.GetValue(entry);
                    xLength = r is int ? (int)r : (int)(long)r;
                }
                else if ((treeKind & TreeKind.AllUniRank) != 0)
                {
                    xLength = 1;
                }
                PropertyInfo xLengthProperty = type.GetProperty("XLength", BindingFlags.Instance | BindingFlags.Public);
                if (xLengthProperty != null)
                {
                    object v = xLengthProperty.GetValue(entry);
                    if (!v.Equals(xLength))
                    {
                        throw new InvalidOperationException("XLength");
                    }
                }
                PropertyInfo countProperty = type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
                if (countProperty != null)
                {
                    object v = countProperty.GetValue(entry);
                    if (!v.Equals(xLength))
                    {
                        throw new InvalidOperationException("Count");
                    }
                }
                PropertyInfo lengthProperty = type.GetProperty("Length", BindingFlags.Instance | BindingFlags.Public);
                if (lengthProperty != null)
                {
                    object v = lengthProperty.GetValue(entry);
                    if (!v.Equals(xLength))
                    {
                        throw new InvalidOperationException("Length");
                    }
                }


                FieldInfo yStartAccessor = type.GetField("yStart", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(((treeKind & TreeKind.AllIndexed2) != 0) == (yStartAccessor != null));
                if (yStartAccessor != null)
                {
                    object r = yStartAccessor.GetValue(entry);
                    yStart = r is int ? (int)r : (int)(long)r;
                }
                PropertyInfo yStartProperty = type.GetProperty("YStart", BindingFlags.Instance | BindingFlags.Public);
                if (yStartProperty != null)
                {
                    object v = yStartProperty.GetValue(entry);
                    if (!v.Equals(yStart))
                    {
                        throw new InvalidOperationException("YStart");
                    }
                }

                FieldInfo yLengthAccessor = type.GetField("yLength", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(((treeKind & TreeKind.AllIndexed2) != 0) == (yLengthAccessor != null));
                if (yLengthAccessor != null)
                {
                    object r = yLengthAccessor.GetValue(entry);
                    yLength = r is int ? (int)r : (int)(long)r;
                }
                PropertyInfo yLengthProperty = type.GetProperty("YLength", BindingFlags.Instance | BindingFlags.Public);
                if (yLengthProperty != null)
                {
                    object v = yLengthProperty.GetValue(entry);
                    if (!v.Equals(yLength))
                    {
                        throw new InvalidOperationException("YLength");
                    }
                }
            }

            public override bool Equals(object obj)
            {
                BigEntry<KeyType, ValueType> other = (BigEntry<KeyType, ValueType>)obj;

                return (0 == Comparer<KeyType>.Default.Compare(this.key, other.key))
                    && (0 == Comparer<ValueType>.Default.Compare(this.value, other.value))
                    && (this.xStart == other.xStart)
                    && (this.xLength == other.xLength)
                    && (this.yStart == other.yStart)
                    && (this.yLength == other.yLength);
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }

            public override string ToString()
            {
                return String.Format("(key={0} value={1} xStart={2} xLength={3} yStart={4} yLength={5})", key, value, xStart, xLength, yStart, yLength);
            }
        }

        public class BigEntryKeyComparer<KeyType, ValueType> : IComparer<BigEntry<KeyType, ValueType>>
        {
            public int Compare(BigEntry<KeyType, ValueType> x, BigEntry<KeyType, ValueType> y)
            {
                return Comparer<KeyType>.Default.Compare(x.key, y.key);
            }
        }

        public class BigEntryStartComparer<KeyType, ValueType> : IComparer<BigEntry<KeyType, ValueType>>
        {
            private readonly Side side;

            public BigEntryStartComparer(Side side)
            {
                this.side = side;
            }

            public int Compare(BigEntry<KeyType, ValueType> x, BigEntry<KeyType, ValueType> y)
            {
                return side == Side.X ? x.xStart.CompareTo(y.xStart) : x.yStart.CompareTo(y.yStart);
            }
        }

        public static void RecalcStarts<KeyType, ValueType>(BigEntry<KeyType, ValueType>[] items)
        {
            int xStart = 0, yStart = 0;
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new BigEntry<KeyType, ValueType>(items[i].key, items[i].value, xStart, items[i].xLength, yStart, items[i].yLength);
                xStart += items[i].xLength;
                yStart += items[i].yLength;
            }
        }

        public static void RecalcStarts<KeyType, ValueType>(List<BigEntry<KeyType, ValueType>> items)
        {
            BigEntry<KeyType, ValueType>[] array = items.ToArray();
            RecalcStarts(array);
            items.Clear();
            items.AddRange(array);
        }

        public static void ZeroIndexes<KeyType, ValueType>(BigEntry<KeyType, ValueType>[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new BigEntry<KeyType, ValueType>(items[i].key, items[i].value, 0, 0, 0, 0);
            }
        }

        public static void ZeroIndexes<KeyType, ValueType>(List<BigEntry<KeyType, ValueType>> items)
        {
            BigEntry<KeyType, ValueType>[] array = items.ToArray();
            ZeroIndexes(array);
            items.Clear();
            items.AddRange(array);
        }

        public static void Validate<KeyType, ValueType>(TestBase testBase, BigEntry<KeyType, ValueType>[] left, BigEntry<KeyType, ValueType>[] right)
        {
            long startIteration = testBase.IncrementIteration();

            testBase.TestTrue("enum: Validate", delegate () { return left.Length == right.Length; });
            for (int i = 0; i < left.Length; i++)
            {
                testBase.TestTrue("enum: Validate Entry", delegate () { return left[i].Equals(right[i]); });
            }
        }

        protected void Validate<KeyType, ValueType>(BigEntry<KeyType, ValueType>[] left, BigEntry<KeyType, ValueType>[] right)
        {
            Validate(this, left, right);
        }



        //
        // Unit test base class
        //

        public virtual bool Do()
        {
            throw new NotSupportedException();
        }

        public virtual bool Do(int seed, StochasticControls control)
        {
            throw new NotSupportedException();
        }



        //
        // Stochastic helper methods
        //

        protected void KeyedEnumerateAction<EntryType>(object[] collections, Random rnd, ref string description, TreeKind treeKind, int[] keys)
        {
            bool @default = rnd.Next() % 12 == 0;

            bool keyed = rnd.Next() % 2 == 0;
            int key = 0;
            if (keyed)
            {
                if ((rnd.Next() % 2 == 0) && (keys.Length != 0))
                {
                    // existing key
                    key = keys[rnd.Next() % keys.Length];
                }
                else
                {
                    // nonexisting key
                    do
                    {
                        key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                    }
                    while (Array.BinarySearch(keys, key, Comparer<int>.Default) >= 0);
                }
            }

            bool forward = rnd.Next() % 2 == 0;

            int kind = rnd.Next() % 3;

            BigEntry<int, float>[] modelItems = null;
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    IEnumerable<EntryType> enumerable;
                    if (@default)
                    {
                        enumerable = GetEnumerator<EntryType>(collections[i], EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/);
                    }
                    else
                    {
                        EnumArgsProvider argsProvider = new EnumArgsProvider();
                        if (keyed)
                        {
                            argsProvider = new EnumKeyedArgsProvider<int>(key);
                        }
                        switch (kind)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case 0:
                                enumerable = GetEnumerator<EntryType>(collections[i], keyed ? EnumKind.KeyedEnumerableBidir : EnumKind.EnumerableBidir, treeKind, argsProvider, forward);
                                break;
                            case 1:
                                enumerable = GetEnumerator<EntryType>(collections[i], keyed ? EnumKind.KeyedEnumerableFastBidir : EnumKind.EnumerableFastBidir, treeKind, argsProvider, forward);
                                break;
                            case 2:
                                enumerable = GetEnumerator<EntryType>(collections[i], keyed ? EnumKind.KeyedEnumerableRobustBidir : EnumKind.EnumerableRobustBidir, treeKind, argsProvider, forward);
                                break;
                        }
                    }

                    List<BigEntry<int, float>> instanceItems = new List<BigEntry<int, float>>();
                    int c = 0;
                    foreach (EntryType entry in enumerable)
                    {
                        TestTrue("enum overrun", delegate () { return c < keys.Length; });
                        instanceItems.Add(new BigEntry<int, float>(entry, treeKind));
                        c++;
                    }

                    if (i == 0)
                    {
                        modelItems = instanceItems.ToArray();
                    }
                    else
                    {
                        Validate(modelItems, instanceItems.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    Fault(collections[i], "Enumerator unexpectedly threw exception", exception);
                }
            }
        }

        protected struct StartLength : IComparable<StartLength>
        {
            public readonly int start;
            public readonly int length;

            public StartLength(int start, int length)
            {
                this.start = start;
                this.length = length;
            }

            public int CompareTo(StartLength other)
            {
                return this.start.CompareTo(other.start);
            }
        }

        protected void IndexedEnumerateAction<EntryType>(object[] collections, Random rnd, ref string description, TreeKind treeKind, StartLength[] xStartLengths, StartLength[] yStartLengths)
        {
            Debug.Assert((yStartLengths == null) || (xStartLengths.Length == yStartLengths.Length));

            bool @default = rnd.Next() % 12 == 0;

            bool indexed = rnd.Next() % 2 == 0;
            Side side = yStartLengths != null ? (rnd.Next() % 2 == 0 ? Side.X : Side.Y) : Side.X;
            int start = 0;
            if (indexed)
            {
                int extent = 0;
                if (xStartLengths.Length != 0)
                {
                    if (side == Side.X)
                    {
                        extent = xStartLengths[xStartLengths.Length - 1].start + xStartLengths[xStartLengths.Length - 1].length;
                    }
                    else
                    {
                        extent = yStartLengths[xStartLengths.Length - 1].start + yStartLengths[xStartLengths.Length - 1].length;
                    }
                }

                if ((rnd.Next() % 2 == 0) && (xStartLengths.Length != 0))
                {
                    // existing index
                    int index = rnd.Next() % xStartLengths.Length;
                    start = (side == Side.X ? xStartLengths : yStartLengths)[index].start;
                }
                else
                {
                    // nonexisting index
                    do
                    {
                        start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                    }
                    while (Array.BinarySearch(side == Side.X ? xStartLengths : yStartLengths, new StartLength(start, 0), Comparer<StartLength>.Default) >= 0);
                }
            }

            bool forward = rnd.Next() % 2 == 0;

            int kind = rnd.Next() % 3;

            BigEntry<int, float>[] modelItems = null;
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    IEnumerable<EntryType> enumerable;
                    if (@default)
                    {
                        enumerable = GetEnumerator<EntryType>(collections[i], EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/);
                    }
                    else
                    {
                        EnumArgsProvider argsProvider = new EnumArgsProvider();
                        if (indexed)
                        {
                            argsProvider = yStartLengths != null
                                ? (EnumArgsProvider)new EnumIndexed2ArgsProvider(start, side)
                                : (EnumArgsProvider)new EnumIndexedArgsProvider(start);
                        }
                        switch (kind)
                        {
                            default:
                                Debug.Assert(false);
                                throw new ArgumentException();
                            case 0:
                                enumerable = GetEnumerator<EntryType>(collections[i], indexed ? EnumKind.IndexedEnumerableBidir : EnumKind.EnumerableBidir, treeKind, argsProvider, forward);
                                break;
                            case 1:
                                enumerable = GetEnumerator<EntryType>(collections[i], indexed ? EnumKind.IndexedEnumerableFastBidir : EnumKind.EnumerableFastBidir, treeKind, argsProvider, forward);
                                break;
                            case 2:
                                enumerable = GetEnumerator<EntryType>(collections[i], indexed ? EnumKind.IndexedEnumerableRobustBidir : EnumKind.EnumerableRobustBidir, treeKind, argsProvider, forward);
                                break;
                        }
                    }

                    List<BigEntry<int, float>> instanceItems = new List<BigEntry<int, float>>();
                    int c = 0;
                    foreach (EntryType entry in enumerable)
                    {
                        TestTrue("enum overrun", delegate () { return c < xStartLengths.Length; });
                        instanceItems.Add(new BigEntry<int, float>(entry, treeKind));
                        c++;
                    }

                    if (i == 0)
                    {
                        modelItems = instanceItems.ToArray();
                    }
                    else
                    {
                        Validate(modelItems, instanceItems.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    Fault(collections[i], "Enumerator unexpectedly threw exception", exception);
                }
            }
        }



        //
        // Stochastic test driver
        //

        protected long stochasticIterations;

        protected delegate void InvokeAction<TreeType>(TreeType[] collections, Random rnd, ref string lastActionDescription);
        protected delegate uint CountMethod<TreeType>(TreeType reference);
        protected delegate void ValidateMethod<TreeType>(TreeType[] collections);
        protected bool StochasticDriver<TreeType>(
            string title,
            int seed,
            StochasticControls control,
            TreeType[] collections,
            Tuple<Tuple<int, int>, InvokeAction<TreeType>>[] actions,
            CountMethod<TreeType> getCount,
            ValidateMethod<TreeType> validate)
        {
            try
            {
                this.seed = seed;
                Random rnd = new Random(seed);

                int totalProb1 = 0;
                int totalProb2 = 0;
                for (int i = 0; i < actions.Length; i++)
                {
                    totalProb1 += actions[i].Item1.Item1;
                    totalProb2 += actions[i].Item1.Item2;
                }
                Debug.Assert((totalProb1 > 0) && (totalProb2 > 0));

                const int RegimeDuration = 50000;
                const int RegimeOffset = 15000;
                int regime = 0;
                uint maxCountEver = 0;
                uint maxCount1 = 0;
                uint minCount1 = 0;
                while (!control.Stop)
                {
                    stochasticIterations++;
                    uint lastCount = getCount(collections[0]);
                    maxCountEver = Math.Max(maxCountEver, lastCount);
                    maxCount1 = Math.Max(maxCount1, lastCount);
                    minCount1 = Math.Min(minCount1, lastCount);
                    if (stochasticIterations % control.ReportingInterval == 0)
                    {
                        WriteLine("  iterations: {0:N0}  r {1}  minc {2}  maxc {3}  lastc {4}  maxc* {5}", stochasticIterations, regime, minCount1, maxCount1, lastCount, maxCountEver);
                        minCount1 = lastCount;
                        maxCount1 = lastCount;
                    }
                    if ((stochasticIterations - RegimeOffset) % RegimeDuration == 0)
                    {
                        regime = regime ^ 1;
                    }

                    string lastActionDescription = String.Empty;

                    int selector = rnd.Next(regime == 0 ? totalProb1 : totalProb2);
                    for (int i = 0; i < actions.Length; i++)
                    {
                        int selector1 = selector;
                        selector -= regime == 0 ? actions[i].Item1.Item1 : actions[i].Item1.Item2;
                        if (selector1 < (regime == 0 ? actions[i].Item1.Item1 : actions[i].Item1.Item2))
                        {
                            lastActionIteration = iteration + 1; // save iteration for setting breaks on rerun
                            IncrementIteration(); // allow breaks at predictable location
                            actions[i].Item2(collections, rnd, ref lastActionDescription);
                            break;
                        }
                    }
                    Debug.Assert(selector < 0);

                    validate(collections);
                }
            }
            catch (Exception exception)
            {
                control.Failed = true;
                ShowException(title, exception);
                return false;
            }

            return true;
        }
    }
}
