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
using System.Reflection;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class UnitTestHugeList : TestBase
    {
        public UnitTestHugeList()
            : base()
        {
        }

        public UnitTestHugeList(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }



        private const int Seed = 1;
        private const int HugeListInternalChunkSize = 5;

        private abstract class Op<T> where T : IComparable<T>
        {
            public delegate T MakeT();
            public virtual void FillData(MakeT makeT)
            {
            }

            public abstract void Do(IHugeList<T> list);
        }

        private class OpInsert<T> : Op<T> where T : IComparable<T>
        {
            private readonly int index;
            private T item;

            public OpInsert(int index, T item)
            {
                this.index = index;
                this.item = item;
            }

            public override void FillData(MakeT makeT)
            {
                item = makeT();
            }

            public override void Do(IHugeList<T> list)
            {
                list.Insert(index, item);
            }

            public override string ToString()
            {
                return String.Format("OpInsert<{0}>({1}, default({0}))", typeof(T).Name, index);
            }
        }

        private class OpRemoveAt<T> : Op<T> where T : IComparable<T>
        {
            private readonly int index;

            public OpRemoveAt(int index)
            {
                this.index = index;
            }

            public override void Do(IHugeList<T> list)
            {
                list.RemoveAt(index);
            }

            public override string ToString()
            {
                return String.Format("OpRemoveAt<{0}>({1})", typeof(T).Name, index);
            }
        }

        private class OpInsertRange<T> : Op<T> where T : IComparable<T>
        {
            private readonly int index;
            private readonly T[] items;
            private readonly int start;
            private readonly int count;

            public OpInsertRange(int index, T[] items, int start, int count)
            {
                this.index = index;
                this.items = items;
                this.start = start;
                this.count = count;

                if (items == null)
                {
                    this.items = new T[Math.Max(start + count, 0)];
                }
            }

            public override void FillData(MakeT makeT)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = makeT();
                }
            }

            public override void Do(IHugeList<T> list)
            {
                list.InsertRange(index, items, start, count);
            }

            public override string ToString()
            {
                return String.Format("OpInsertRange<{0}>({1}, null, {2}, {3})", typeof(T).Name, index, start, count);
            }
        }

        private class OpRemoveRange<T> : Op<T> where T : IComparable<T>
        {
            private readonly int index;
            private readonly int count;

            public OpRemoveRange(int index, int count)
            {
                this.index = index;
                this.count = count;
            }

            public override void Do(IHugeList<T> list)
            {
                list.RemoveRange(index, count);
            }

            public override string ToString()
            {
                return String.Format("OpRemoveRange<{0}>({1}, {2})", typeof(T).Name, index, count);
            }
        }

        private void TestOps<T>(string label, IHugeList<T> test, Op<T>.MakeT makeT, bool pruning, Op<T>[] ops) where T : IComparable<T>
        {
            List<Tuple<int, Op<T>>> pruneList = new List<Tuple<int, Op<T>>>();

            ReferenceHugeList<T> reference = new ReferenceHugeList<T>();
            for (int i = 0; i < ops.Length; i++)
            {
                string text = ops[i].ToString(); // to help for finding invalid ones

                ops[i].FillData(makeT);

                try
                {
                    TestNoThrow(String.Format("{0}.{1}", label, i),
                        delegate ()
                        {
                            ops[i].Do(reference);
                            ops[i].Do(test);
                        });
                }
                catch (Exception)
                {
                    if (!pruning)
                    {
                        throw;
                    }
                    pruneList.Add(new Tuple<int, Op<T>>(i, ops[i]));
                }

                ((IHugeListValidation)test).Validate();

                T[] refArray = reference.ToArray();
                T[] testArray = test.ToArray();
                TestTrue(String.Format("{0} Counts", label), delegate () { return refArray.Length == testArray.Length; });
                for (int j = 0; j < refArray.Length; j++)
                {
                    TestTrue(String.Format("{0} items", label), delegate () { return 0 == Comparer<T>.Default.Compare(refArray[j], testArray[j]); });
                }
            }
            if (pruneList.Count != 0)
            {
                foreach (Tuple<int, Op<T>> item in pruneList)
                {
                    Console.WriteLine("{0}: {1}", item.Item1, item.Item2);
                }
                Debugger.Break();
            }
        }

        private void TestInsert(ParkAndMiller random/*null==default*/, int start, int count, List<int> reference, IHugeList<int> test)
        {
            IncrementIteration(true/*setLast*/);

            int[] data = null;
            if (random != null)
            {
                data = new int[count];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = random.Next();
                }
            }

            if (random != null)
            {
                reference.InsertRange(start, data);
                TestNoThrow("InsertRange", delegate () { test.InsertRange(start, data); });
            }
            else
            {
                reference.InsertRange(start, new int[count]);
                TestNoThrow("InsertRange", delegate () { test.InsertRangeDefault(start, count); });
            }

            Validate(reference, test);
        }

        private void TestRemove(int start, int count, List<int> reference, IHugeList<int> test)
        {
            IncrementIteration(true/*setLast*/);

            reference.RemoveRange(start, count);
            TestNoThrow("RemoveRange", delegate () { test.RemoveRange(start, count); });

            Validate(reference, test);
        }

        private void TestClear(List<int> reference, IHugeList<int> test)
        {
            IncrementIteration(true/*setLast*/);

            reference.Clear();
            TestNoThrow("Clear", delegate () { test.Clear(); });

            Validate(reference, test);
        }

        private delegate IHugeList<T> MakeHugeList<T>();
        private void HugeListTestSpecific(MakeHugeList<int> makeList, bool checkBlockSize)
        {
            long startIter = IncrementIteration(true/*setLast*/);

            ParkAndMiller rand = new ParkAndMiller(Seed);

            List<int> reference, referDflt;
            IHugeList<int> test, testDefault;
            long previousIteration, currentIteration = 0;


            // Insert and Remove internals unit test


            reference = new List<int>();
            referDflt = new List<int>();
            test = makeList();
            testDefault = makeList();

            if (checkBlockSize)
            {
                TestTrue("MaxBlockSize", delegate () { return test.MaxBlockSize == HugeListInternalChunkSize; });
            }
            TestFalse("IsReadOnly ", delegate () { return test.IsReadOnly; });

            // CASE A0

            // Fill one item at a time to test append code path
            for (int i = 0; i < HugeListInternalChunkSize * 3; i++)
            {
                long startIter1 = IncrementIteration(true/*setLast*/);

                previousIteration = currentIteration;
                currentIteration = iteration;

                TestInsert(rand, i, 1, reference, test);
                TestInsert(null, i, 1, referDflt, testDefault);
            }
            TestTrue("Count", delegate () { return test.Count == reference.Count; });
            TestTrue("Count", delegate () { return testDefault.Count == referDflt.Count; });

            // Delete one item at a time, in reverse, to test end removal code path
            for (int i = HugeListInternalChunkSize * 3 - 1; i >= 0; i--)
            {
                long startIter1 = IncrementIteration(true/*setLast*/);

                previousIteration = currentIteration;
                currentIteration = iteration;

                TestRemove(i, 1, reference, test);
                TestRemove(i, 1, referDflt, testDefault);
            }
            TestTrue("Count", delegate () { return test.Count == reference.Count; });
            TestTrue("Count", delegate () { return testDefault.Count == referDflt.Count; });

            // Fill one item at a time to test insert at start code path
            for (int i = 0; i < HugeListInternalChunkSize * 3; i++)
            {
                long startIter1 = IncrementIteration(true/*setLast*/);

                previousIteration = currentIteration;
                currentIteration = iteration;

                TestInsert(rand, 0, 1, reference, test);
                TestInsert(null, 0, 1, referDflt, testDefault);
            }
            TestTrue("Count", delegate () { return test.Count == reference.Count; });
            TestTrue("Count", delegate () { return testDefault.Count == referDflt.Count; });

            // Test set method
            for (int i = 0; i < HugeListInternalChunkSize * 3; i++)
            {
                long startIter1 = IncrementIteration(true/*setLast*/);

                TestNoThrow("indexer", delegate () { reference[i] = -reference[i]; });
                TestNoThrow("indexer", delegate () { test[i] = -test[i]; });
                Validate(reference, test);
            }
            // bounds check
            TestThrow("indexer", typeof(ArgumentOutOfRangeException), delegate () { int f = test[-1]; });
            TestThrow("indexer", typeof(ArgumentOutOfRangeException), delegate () { int f = test[reference.Count]; });

            // Delete one item at a time, in reverse, to test start removal code path
            for (int i = HugeListInternalChunkSize * 3 - 1; i >= 0; i--)
            {
                long startIter1 = IncrementIteration(true/*setLast*/);

                previousIteration = currentIteration;
                currentIteration = iteration;

                TestRemove(0, 1, reference, test);
                TestRemove(0, 1, referDflt, testDefault);
            }
            TestTrue("Count", delegate () { return test.Count == reference.Count; });
            TestTrue("Count", delegate () { return testDefault.Count == referDflt.Count; });

            // Test insert and add code paths
            for (int i = 0; i < HugeListInternalChunkSize * 3; i++)
            {
                long startIter1 = IncrementIteration(true/*setLast*/);

                int item;
                int[] items, items2;
                int p, q, r;

                item = rand.Next();
                Debug.Assert(item >= 0);
                p = reference.Count;
                TestNoThrow("Insert(int index, T item)", delegate () { test.Insert(Math.Max(0, p - 1), item); });
                TestNoThrow("Insert(int index, T item)", delegate () { reference.Insert(Math.Max(0, p - 1), item); });
                Validate(reference, test);

                item = rand.Next();
                TestNoThrow("Add(T item)", delegate () { test.Add(item); });
                TestNoThrow("Add(T item)", delegate () { reference.Add(item); });
                Validate(reference, test);

                items = new int[3];
                for (int j = 0; j < items.Length; j++)
                {
                    items[j] = rand.Next();
                    Debug.Assert(items[j] >= 0);
                }
                TestNoThrow("AddRange(T[] items)", delegate () { test.AddRange(items); });
                TestNoThrow("AddRange(T[] items)", delegate () { reference.AddRange(items); });
                Validate(reference, test);

                for (int c = 4; c <= 6; c++)
                {
                    long startIter2 = IncrementIteration(true/*setLast*/);

                    items = new int[c];
                    for (int j = 0; j < items.Length; j++)
                    {
                        items[j] = rand.Next();
                        Debug.Assert(items[j] >= 0);
                    }
                    IEnumerable<int> enumerable = new List<int>(items);
                    int k = 0;
                    foreach (int x in enumerable)
                    {
                        k++;
                    }
                    Debug.Assert(k == items.Length);
                    TestNoThrow("AddRange(IEnumerable<T> collection)", delegate () { test.AddRange(enumerable); });
                    TestNoThrow("AddRange(IEnumerable<T> collection)", delegate () { reference.AddRange(enumerable); });
                    Validate(reference, test);
                }

                p = reference.Count / 2;
                TestNoThrow("RemoveAt(int index)", delegate () { test.RemoveAt(p); });
                TestNoThrow("RemoveAt(int index)", delegate () { reference.RemoveAt(p); });
                Validate(reference, test);

                // item was added earlier
                p = reference.Count;
                TestTrue("Remove(T item)", delegate () { return test.Remove(item); });
                TestTrue("Remove(T item)", delegate () { return reference.Remove(item); });
                TestTrue("Remove(T item)", delegate () { return (test.Count == p - 1) && (reference.Count == p - 1); });
                Validate(reference, test);
                // rnd never make negatives
                p = reference.Count;
                TestFalse("Remove(T item)", delegate () { return test.Remove(-1); });
                TestFalse("Remove(T item)", delegate () { return reference.Remove(-1); });
                TestTrue("Remove(T item)", delegate () { return (test.Count == p) && (reference.Count == p); });
                Validate(reference, test);

                p = reference.Count;
                q = 0; r = 0;
                TestNoThrow("RemoveAll(Predicate<T> match)", delegate () { q = test.RemoveAll(delegate (int c) { return c % 2 * HugeListInternalChunkSize != 0; }); });
                TestNoThrow("RemoveAll(Predicate<T> match)", delegate () { r = reference.RemoveAll(delegate (int c) { return c % 2 * HugeListInternalChunkSize != 0; }); });
                TestTrue("RemoveAll(Predicate<T> match)", delegate () { return q == r; });
                TestTrue("RemoveAll(Predicate<T> match)", delegate () { return (test.Count + q == p) && (reference.Count + r == p); });
                Validate(reference, test);

                // arg checking
                TestThrow("RemoveAll(Predicate<T> match)", typeof(ArgumentException), delegate () { q = test.RemoveAll(null); });
                Validate(reference, test);

                // replace range with increasing size
                items = new int[2 * HugeListInternalChunkSize];
                for (int j = 0; j < items.Length; j++)
                {
                    items[j] = rand.Next();
                    Debug.Assert(items[j] >= 0);
                }
                p = Math.Max(reference.Count - 3 - items.Length, 0);
                q = Math.Min(HugeListInternalChunkSize * 3 + 1, reference.Count - p);
                TestNoThrow("ReplaceRange(int index, int count, T[] items)", delegate () { test.ReplaceRange(p, q, items); });
                TestNoThrow("ReplaceRange(int index, int count, T[] items)", delegate () { reference.RemoveRange(p, q); reference.InsertRange(p, items); });
                Validate(reference, test);

                for (int j = 0; j < items.Length; j++)
                {
                    items[j] = rand.Next();
                    Debug.Assert(items[j] >= 0);
                }
                p = Math.Min(3, reference.Count);
                q = Math.Min(HugeListInternalChunkSize * 3 + 1, reference.Count - p);
                TestNoThrow("ReplaceRange(int index, int count, T[] items, int offset, int count2)", delegate () { test.ReplaceRange(p, q, items, 1, items.Length - 2); });
                {
                    List<int> subset = new List<int>(items);
                    subset.RemoveAt(0);
                    subset.RemoveAt(subset.Count - 1);
                    TestNoThrow("ReplaceRange(int index, int count, T[] items, int offset, int count2)", delegate () { reference.RemoveRange(p, q); reference.InsertRange(p, subset); });
                }
                Validate(reference, test);

                // replace range with decreasing size
                items = new int[HugeListInternalChunkSize];
                for (int j = 0; j < items.Length; j++)
                {
                    items[j] = rand.Next();
                    Debug.Assert(items[j] >= 0);
                }
                p = Math.Min(3, reference.Count);
                q = Math.Min(items.Length + 2, reference.Count - p);
                TestNoThrow("ReplaceRange(int index, int count, T[] items)", delegate () { test.ReplaceRange(p, q, items); });
                TestNoThrow("ReplaceRange(int index, int count, T[] items)", delegate () { reference.RemoveRange(p, q); reference.InsertRange(p, items); });
                Validate(reference, test);

                p = Math.Min(1, reference.Count);
                q = Math.Max(0, reference.Count - 1 - p);
                items = new int[q + 2];
                items2 = new int[q + 2];
                TestNoThrow("CopyTo(int, T[], int, int)", delegate () { test.CopyTo(p, items, 1, q); });
                TestNoThrow("CopyTo(int, T[], int, int)", delegate () { reference.CopyTo(p, items2, 1, q); });
                for (int j = 0; j < items.Length; j++)
                {
                    long startIter2 = IncrementIteration(true/*setLast*/);

                    TestTrue("CopyTo", delegate () { return items[j] == items2[j]; });
                }
                //
                items = new int[reference.Count + 1];
                items2 = new int[reference.Count + 1];
                TestNoThrow("CopyTo(T[] items, int arrayIndex)", delegate () { test.CopyTo(items, 1); });
                TestNoThrow("CopyTo(T[] items, int arrayIndex)", delegate () { reference.CopyTo(items2, 1); });
                for (int j = 0; j < items.Length; j++)
                {
                    TestTrue("CopyTo", delegate () { return items[j] == items2[j]; });
                }
                //
                items = new int[reference.Count];
                items2 = new int[reference.Count];
                TestNoThrow("CopyTo(T[] items)", delegate () { test.CopyTo(items); });
                TestNoThrow("CopyTo(T[] items)", delegate () { reference.CopyTo(items2); });
                for (int j = 0; j < items.Length; j++)
                {
                    TestTrue("CopyTo", delegate () { return items[j] == items2[j]; });
                }

                // CopyTo bounds checking
                p = Math.Min(1, reference.Count);
                items = new int[reference.Count];
                items2 = new int[reference.Count];
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentNullException), delegate () { test.CopyTo(0, null, 0, reference.Count); });
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.CopyTo(-1, items, 0, reference.Count); });
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentException), delegate () { test.CopyTo(0, items, 0, reference.Count + 1); });
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentException), delegate () { test.CopyTo(1, items, 0, reference.Count); });
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.CopyTo(0, items, -1, reference.Count); });
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.CopyTo(0, items, 0, -1); });
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentException), delegate () { test.CopyTo(0, items, 1, reference.Count); });
                TestThrow("CopyTo(int, T[], int, int)", typeof(ArgumentException), delegate () { test.CopyTo(Int32.MaxValue, items, Int32.MaxValue, reference.Count); });
                TestThrow("ReplaceRange(int index, int count, T[] items)", typeof(ArgumentNullException), delegate () { test.ReplaceRange(0, 1, null, 0, 1); });
                Validate(reference, test);

                // iterate range
                for (int j = 3; j < reference.Count - 3; j++)
                {
                    reference[j] = unchecked(reference[j] + 10);
                }
                TestNoThrow(
                    "IterateRange",
                    delegate ()
                    {
                        test.IterateRange(3, null, 0, Math.Max(reference.Count - 6, 0), delegate (ref int v, ref int x) { v = unchecked(v + 10); x = rand.Next(); });
                    });
                Validate(reference, test);
                // iterate range 2
                {
                    int[] addend = new int[1 + (reference.Count - 6) + 1];
                    for (int j = 0; j < reference.Count - 6; j++)
                    {
                        addend[j + 1] = rand.Next();
                    }
                    for (int j = 3; j < reference.Count - 3; j++)
                    {
                        reference[j] = unchecked(reference[j] + addend[j - 3 + 1]);
                    }
                    int[] negatedAddend = (int[])addend.Clone();
                    TestNoThrow(
                        "IterateRange",
                        delegate ()
                        {
                            test.IterateRange(3, addend, 1, Math.Max(reference.Count - 6, 0), delegate (ref int v, ref int x) { v = unchecked(v + x); x = -x; });
                        });
                    Validate(reference, test);
                    for (int j = 0; j < addend.Length; j++)
                    {
                        TestTrue("IterateRange", delegate () { return addend[j] == -negatedAddend[j]; });
                    }
                }
                // iterate range batch external array bounds check
                TestThrow(
                    "IterateRangeBatch",
                    typeof(ArgumentException),
                    delegate ()
                    {
                        test.IterateRangeBatch(0, new int[5], 5, 1, delegate (int[] v, int vOffset, int[] x, int xOffset, int count) { });
                    });
                TestThrow(
                    "IterateRangeBatch",
                    typeof(ArgumentException),
                    delegate ()
                    {
                        test.IterateRangeBatch(0, new int[5], 0, 6, delegate (int[] v, int vOffset, int[] x, int xOffset, int count) { });
                    });

                // ToArray
                {
                    int[] toArray = null, toArrayReference = null;
                    TestNoThrow("ToArray", delegate () { toArray = test.ToArray(); });
                    TestNoThrow("ToArray", delegate () { toArrayReference = reference.ToArray(); });
                    TestTrue("ToArray", delegate () { return (toArray != null) && (toArrayReference != null); });
                    TestTrue("ToArray", delegate () { return toArray.Length == toArrayReference.Length; });
                    TestTrue("ToArray", delegate () { return (toArray.Length == reference.Count) && (toArrayReference.Length == reference.Count); });
                    for (int j = 0; j < toArrayReference.Length; j++)
                    {
                        TestTrue("ToArray", delegate () { return toArray[j] == toArrayReference[j]; });
                    }
                    Validate(reference, test);
                }

                // enumeration
                {
                    List<int> collected = new List<int>();
                    foreach (int item2 in test)
                    {
                        TestTrue("enumeration overrun", delegate () { return collected.Count < reference.Count; });
                        collected.Add(item2);
                    }
                    TestTrue("enumeration count", delegate () { return collected.Count == reference.Count; });
                    for (int j = 0; j < reference.Count; j++)
                    {
                        TestTrue("enumeration", delegate () { return collected[j] == reference[j]; });
                    }
                    int kk = 0;
                    foreach (int item2 in (IEnumerable)test) // test non-generic version
                    {
                        TestTrue("enumeration overrun", delegate () { return kk < reference.Count; });
                        TestTrue("enumeration", delegate () { return collected[kk] == item2; });
                        kk++;
                    }
                    TestTrue("enumeration count", delegate () { return kk == reference.Count; });
                    // break enumeration with insertion
                    if (reference.Count != 0)
                    {
                        int j = 0;
                        TestThrow(
                            "enumeration",
                            typeof(InvalidOperationException),
                            delegate ()
                            {
                                foreach (int item2 in test)
                                {
                                    j++;
                                    test.Insert(reference.Count, item2);
                                    reference.Insert(reference.Count, item2);
                                }
                            });
                        TestTrue("enumeration", delegate () { return j == 1; });
                    }
#if false // TODO:
                    // unlike List<>, our enumeration permits setting elements
                    if (reference.Count != 0)
                    {
                        int j = 0;
                        TestNoThrow(
                            "enumeration",
                            delegate ()
                            {
                                foreach (int item2 in test)
                                {
                                    int k = rand.Next() % test.Count;
                                    test[k] = unchecked(test[k] + 1);
                                    reference[k] = unchecked(reference[k] + 1);
                                    j++;
                                }
                            });
                        TestTrue("enumeration", delegate () { return j == reference.Count; });
                    }
#endif
                    Validate(reference, test);
                }
            }
            // clean up
            TestClear(reference, test);
            Debug.Assert((reference.Count == 0) && (referDflt.Count == 0));

            // empty enumeration
            {
                int i = 0;
                foreach (int item2 in test)
                {
                    Fault(test, "enumeration overrun");
                    i++;
                }
                TestTrue("enumeration", delegate () { return i == 0; });
            }

            // Test insertion into previous unfilled segment
            for (int trailerCount = 0; trailerCount <= 1; trailerCount++)
            {
                for (int initialFill = 0; initialFill <= 1; initialFill++)
                {
                    long startIter1 = IncrementIteration(true/*setLast*/);

                    previousIteration = currentIteration;
                    currentIteration = iteration;

                    for (int i = 0; i < initialFill; i++)
                    {
                        TestInsert(rand, i * HugeListInternalChunkSize, HugeListInternalChunkSize, reference, test);
                        TestInsert(null, i * HugeListInternalChunkSize, HugeListInternalChunkSize, referDflt, testDefault);
                    }

                    int basis = reference.Count;

                    // segment to remove from
                    TestInsert(rand, reference.Count, HugeListInternalChunkSize, reference, test);
                    TestInsert(null, referDflt.Count, HugeListInternalChunkSize, referDflt, testDefault);

                    // following full segment segment to suppress coalescing
                    TestInsert(rand, reference.Count, HugeListInternalChunkSize, reference, test);
                    TestInsert(null, referDflt.Count, HugeListInternalChunkSize, referDflt, testDefault);

                    // add fill (0 or 1) after following to test with and without following being last
                    for (int j = 0; j < trailerCount; j++)
                    {
                        TestInsert(rand, reference.Count, HugeListInternalChunkSize, reference, test);
                        TestInsert(null, referDflt.Count, HugeListInternalChunkSize, referDflt, testDefault);
                    }

                    // CASE A1
                    // test removal
                    TestRemove(basis + 4, 1, reference, test);
                    TestRemove(basis + 4, 1, referDflt, testDefault);

                    // case A2
                    // remove from final segment to force excess to be moved
                    TestRemove(basis + 4 - 1 + HugeListInternalChunkSize, 1, reference, test);
                    TestRemove(basis + 4 - 1 + HugeListInternalChunkSize, 1, referDflt, testDefault);

                    // CASE A3
                    // remove another from previous to force another move of excess
                    TestRemove(basis + 3, 1, reference, test);
                    TestRemove(basis + 3, 1, referDflt, testDefault);

                    // CASE A5
                    // remove two from previous - will cause it to coalesce with next
                    TestRemove(basis + 1, 2, reference, test);
                    TestRemove(basis + 1, 2, referDflt, testDefault);

                    Debug.Assert(reference.Count % HugeListInternalChunkSize == 0);

                    // CASE A6
                    // remove one 
                    TestRemove(basis + 4, 1, reference, test);
                    TestRemove(basis + 4, 1, referDflt, testDefault);
                    //
                    // insert beginning segment 
                    TestInsert(rand, 0, HugeListInternalChunkSize, reference, test);
                    TestInsert(null, 0, HugeListInternalChunkSize, referDflt, testDefault);
                    //
                    // remove from beginning segment to move excess
                    TestRemove(0, 1, reference, test);
                    TestRemove(0, 1, referDflt, testDefault);
                    //
                    // remove again, shifting excess (A6)
                    TestRemove(basis + (HugeListInternalChunkSize - 1) + 3, 1, reference, test);
                    TestRemove(basis + (HugeListInternalChunkSize - 1) + 3, 1, referDflt, testDefault);

                    // CASE A4
                    // clear for next iteration
                    TestClear(reference, test);
                    TestClear(referDflt, testDefault);
                }
            }

            // CASE A7
            // coalescing delete
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            TestRemove(2, HugeListInternalChunkSize, reference, test);
            TestRemove(2, HugeListInternalChunkSize, referDflt, testDefault);
            // clear for next test
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A8
            // non-coalescing delete with extra
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            TestRemove(2, HugeListInternalChunkSize - 1, reference, test);
            TestRemove(2, HugeListInternalChunkSize - 1, referDflt, testDefault);
            // clear for next test
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A21
            // coalescing remove with excess on the next segment 
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            TestRemove(0, 1, reference, test);
            TestRemove(0, 1, referDflt, testDefault);
            // remove from second segment - excess moves here
            TestRemove(HugeListInternalChunkSize, 1, reference, test);
            TestRemove(HugeListInternalChunkSize, 1, referDflt, testDefault);
            // now 4 in each segment
            // remove enough to coalesce
            TestRemove(2, 4, reference, test);
            TestRemove(2, 4, referDflt, testDefault);
            // clear for next test
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // Bounds violation checks
            TestInsert(rand, 0, HugeListInternalChunkSize, reference, test);
            TestThrow("RemoveRange", typeof(ArgumentOutOfRangeException), delegate () { test.RemoveRange(-1, 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            Validate(reference, test);
            TestThrow("RemoveRange", typeof(ArgumentException), delegate () { test.RemoveRange(HugeListInternalChunkSize, 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            Validate(reference, test);
            TestThrow("RemoveRange", typeof(ArgumentException), delegate () { test.RemoveRange(HugeListInternalChunkSize - 1, 2); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            Validate(reference, test);
            TestThrow("RemoveRange", typeof(ArgumentException), delegate () { test.RemoveRange(0, HugeListInternalChunkSize + 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            Validate(reference, test);
            TestThrow("RemoveRange", typeof(ArgumentOutOfRangeException), delegate () { test.RemoveRange(0, -1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            Validate(reference, test);
            TestThrow("RemoveRange", typeof(ArgumentException), delegate () { test.RemoveRange(Int32.MaxValue, Int32.MaxValue); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            Validate(reference, test);
            // clear for next iteration
            TestClear(reference, test);

            // CASE A9
            // test insert preceding segment excess space infill
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from first segment to create excess space
            TestRemove(0, 1, reference, test);
            TestRemove(0, 1, referDflt, testDefault);
            // insert again (A9)
            TestInsert(rand, HugeListInternalChunkSize - 1, 1, reference, test);
            TestInsert(null, HugeListInternalChunkSize - 1, 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A10
            // test insert preceding segment excess space infill
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from first segment to create excess space
            TestRemove(0, 2, reference, test);
            TestRemove(0, 1, referDflt, testDefault);
            // insert again, without complete filling (A10)
            TestInsert(rand, HugeListInternalChunkSize - 2, 1, reference, test);
            TestInsert(null, HugeListInternalChunkSize - 2, 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A11
            // test insert preceding segment excess space infill
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from first segment to create excess space
            TestRemove(0, 2, reference, test);
            TestRemove(0, 2, referDflt, testDefault);
            // insert again, without complete filling (A11)
            TestInsert(rand, HugeListInternalChunkSize - 1, 1, reference, test);
            TestInsert(null, HugeListInternalChunkSize - 1, 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A12
            // test insert preceding segment excess space infill, with excess having been after point of insertion
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from first segment to create excess space
            TestRemove(0, 2, reference, test);
            TestRemove(0, 2, referDflt, testDefault);
            // insert at end not using all capacity, to move excess
            TestInsert(rand, reference.Count, HugeListInternalChunkSize - 1, reference, test);
            TestInsert(null, referDflt.Count, HugeListInternalChunkSize - 1, referDflt, testDefault);
            // insert again, without complete filling (A12)
            TestInsert(rand, HugeListInternalChunkSize - 1, 1, reference, test);
            TestInsert(null, HugeListInternalChunkSize - 1, 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A13
            // test following segment absorption
            // setup
            TestInsert(rand, 0, 3 * HugeListInternalChunkSize - 3, reference, test);
            TestInsert(null, 0, 3 * HugeListInternalChunkSize - 3, referDflt, testDefault);
            // remove from first segment to create excess space
            TestInsert(rand, 2 * HugeListInternalChunkSize - 1, HugeListInternalChunkSize - 1, reference, test);
            TestInsert(null, 2 * HugeListInternalChunkSize - 1, HugeListInternalChunkSize - 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A14
            // test following segment absorption, with segment prefix, full segment in middle, and suffix
            // setup
            TestInsert(rand, 0, 3 * HugeListInternalChunkSize - 3, reference, test);
            TestInsert(null, 0, 3 * HugeListInternalChunkSize - 3, referDflt, testDefault);
            // remove from first segment to create excess space
            TestInsert(rand, 2 * HugeListInternalChunkSize - 1, HugeListInternalChunkSize + HugeListInternalChunkSize - 1, reference, test);
            TestInsert(null, 2 * HugeListInternalChunkSize - 1, HugeListInternalChunkSize + HugeListInternalChunkSize - 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A15
            // fill in preceding segment with excess change
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from first segment to create excess space
            TestRemove(HugeListInternalChunkSize - 2, 2, reference, test);
            TestRemove(HugeListInternalChunkSize - 2, 2, referDflt, testDefault);
            // move excess to end
            TestInsert(rand, reference.Count, HugeListInternalChunkSize - 1, reference, test);
            TestInsert(null, referDflt.Count, HugeListInternalChunkSize - 1, referDflt, testDefault);
            // insert again
            TestInsert(rand, HugeListInternalChunkSize - 2, 1, reference, test);
            TestInsert(null, HugeListInternalChunkSize - 2, 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A16
            // fill in preceding segment with excess change
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from first segment to create excess space
            TestRemove(HugeListInternalChunkSize - 2, 2, reference, test);
            TestRemove(HugeListInternalChunkSize - 2, 2, referDflt, testDefault);
            // move excess to end
            TestInsert(rand, reference.Count, HugeListInternalChunkSize - 1, reference, test);
            TestInsert(null, referDflt.Count, HugeListInternalChunkSize - 1, referDflt, testDefault);
            // insert again
            TestInsert(rand, HugeListInternalChunkSize - 2, HugeListInternalChunkSize + 1, reference, test);
            TestInsert(null, HugeListInternalChunkSize - 2, HugeListInternalChunkSize + 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A17
            // insert into middle of block with resize to make excess
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from first segment to create excess space
            TestRemove(HugeListInternalChunkSize - 2, 2, reference, test);
            TestRemove(HugeListInternalChunkSize - 2, 2, referDflt, testDefault);
            // move excess to end
            TestInsert(rand, reference.Count, HugeListInternalChunkSize - 1, reference, test);
            TestInsert(null, referDflt.Count, HugeListInternalChunkSize - 1, referDflt, testDefault);
            // insert again
            TestInsert(rand, 1, 1, reference, test);
            TestInsert(null, 1, 1, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A18
            // insert that splits a segment and then remaining count and extra are too big for one segment (and next is not full but unmergable)
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from next to make it unfull
            TestRemove(HugeListInternalChunkSize, 1, reference, test);
            TestRemove(HugeListInternalChunkSize, 1, referDflt, testDefault);
            // insert again
            TestInsert(rand, 1, HugeListInternalChunkSize + 2, reference, test);
            TestInsert(null, 1, HugeListInternalChunkSize + 2, referDflt, testDefault);
            // follow-on insert to try to fill end of 'count' segment
            TestInsert(rand, 1 + HugeListInternalChunkSize + 2, 2, reference, test);
            TestInsert(null, 1 + HugeListInternalChunkSize + 2, 2, referDflt, testDefault);
            // follow-on insert to try to fill at beginning of extra segment
            TestInsert(rand, 1 + HugeListInternalChunkSize + 2 + 2, 3, reference, test);
            TestInsert(null, 1 + HugeListInternalChunkSize + 2 + 2, 3, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A19
            // insert that splits a segment and then remaining count and extra are too big for one segment (and next is not full but unmergable)
            // setup
            TestInsert(rand, 0, 2 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 2 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from next to make it unfull
            TestRemove(HugeListInternalChunkSize, 1, reference, test);
            TestRemove(HugeListInternalChunkSize, 1, referDflt, testDefault);
            // move excess to end
            TestInsert(rand, reference.Count, HugeListInternalChunkSize - 1, reference, test);
            TestInsert(null, referDflt.Count, HugeListInternalChunkSize - 1, referDflt, testDefault);
            // invalidate excess
            TestInsert(rand, reference.Count, 1, reference, test);
            TestInsert(null, referDflt.Count, 1, referDflt, testDefault);
            // insert again
            TestInsert(rand, 1, HugeListInternalChunkSize + 2, reference, test);
            TestInsert(null, 1, HugeListInternalChunkSize + 2, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // CASE A20
            // insert that splits a segment and then remaining count and extra are too big for one segment (and next is not full but unmergable)
            // like A19 except contains extra leading segment to have an excess preceding the insertion point
            // setup
            TestInsert(rand, 0, 4 * HugeListInternalChunkSize, reference, test);
            TestInsert(null, 0, 4 * HugeListInternalChunkSize, referDflt, testDefault);
            // remove from next to make it unfull
            TestRemove(3 * HugeListInternalChunkSize, 1, reference, test);
            TestRemove(3 * HugeListInternalChunkSize, 1, referDflt, testDefault);
            // move excess to start
            TestRemove(0, 1, reference, test);
            TestRemove(0, 1, referDflt, testDefault);
            // insert again
            TestInsert(rand, 2 * HugeListInternalChunkSize - 1 + 1, HugeListInternalChunkSize + 2, reference, test);
            TestInsert(null, 2 * HugeListInternalChunkSize - 1 + 1, HugeListInternalChunkSize + 2, referDflt, testDefault);
            // clear for next iteration
            TestClear(reference, test);
            TestClear(referDflt, testDefault);

            // Insert bound check
            TestInsert(rand, 0, HugeListInternalChunkSize, reference, test);
            TestThrow("InsertRangeDefault", typeof(ArgumentOutOfRangeException), delegate () { test.InsertRangeDefault(-1, 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRangeDefault", typeof(ArgumentException), delegate () { test.InsertRangeDefault(HugeListInternalChunkSize + 1, 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRangeDefault", typeof(ArgumentOutOfRangeException), delegate () { test.InsertRangeDefault(0, -1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            if (!test.GetType().Name.Contains("Reference"))
            {
                TestThrow("InsertRangeDefault", typeof(OverflowException), delegate () { test.InsertRangeDefault(HugeListInternalChunkSize, Int32.MaxValue); });
                TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            }
            //
            TestThrow("InsertRange", typeof(ArgumentException), delegate () { test.InsertRange(0, new int[0], 0, 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentException), delegate () { test.InsertRange(0, new int[1], 0, 2); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentException), delegate () { test.InsertRange(0, new int[1], 1, 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentOutOfRangeException), delegate () { test.InsertRange(0, new int[1], 0, -1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentOutOfRangeException), delegate () { test.InsertRange(0, new int[2], -1, 1); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentException), delegate () { test.InsertRange(0, new int[2], 1, 2); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentException), delegate () { test.InsertRange(0, new int[2], 1, Int32.MaxValue); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentNullException), delegate () { test.InsertRange(0, (int[])null); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestThrow("InsertRange", typeof(ArgumentNullException), delegate () { test.InsertRange(0, (IEnumerable<int>)null); });
            TestTrue("Count", delegate () { return (reference.Count == HugeListInternalChunkSize) && (test.Count == HugeListInternalChunkSize); });
            TestClear(reference, test);

            // find index
            {
                int p, q = 0, r = 0, item;
                TestInsert(null, 0, 4 * HugeListInternalChunkSize, reference, test);
                p = reference.Count / 2;
                do
                {
                    item = rand.Next();
                } while ((item == 0) || unchecked(item + 1 == 0) || unchecked(item + 2 == 0));
                // set up item in middle
                TestNoThrow("Insert", delegate () { test.Insert(p, item); });
                TestNoThrow("Insert", delegate () { reference.Insert(p, item); });
                // find item 
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T)", delegate () { q = test.IndexOf(item); });
                TestNoThrow("IndexOf(T)", delegate () { r = reference.IndexOf(item); });
                TestTrue("IndexOf", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(Predicate<T>)", delegate () { q = test.FindIndex(delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(Predicate<T>)", delegate () { r = reference.FindIndex(delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindIndex", delegate () { return (q == p) && (r == p); });
                TestTrue("Contains(T)", delegate () { return test.Contains(item); });
                TestTrue("Contains(T)", delegate () { return reference.Contains(item); });
                // test not found
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T)", delegate () { q = test.IndexOf(unchecked(item + 1)); });
                TestNoThrow("IndexOf(T)", delegate () { r = reference.IndexOf(unchecked(item + 1)); });
                TestTrue("IndexOf", delegate () { return (q == -1) && (r == -1); });
                TestFalse("Contains(T)", delegate () { return test.Contains(unchecked(item + 1)); });
                TestFalse("Contains(T)", delegate () { return reference.Contains(unchecked(item + 1)); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(Predicate<T>)", delegate () { q = test.FindIndex(delegate (int candidate) { return false; }); });
                TestNoThrow("FindIndex(Predicate<T>)", delegate () { r = reference.FindIndex(delegate (int candidate) { return false; }); });
                TestTrue("FindIndex", delegate () { return (q == -1) && (r == -1); });
                // set up with multiple instances
                q = r = Int32.MinValue;
                TestNoThrow("Insert", delegate () { test.Insert(1, reference[p]); });
                TestNoThrow("Insert", delegate () { reference.Insert(1, reference[p]); });
                p++;
                // test indexing
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T, int)", delegate () { q = test.IndexOf(item, 2); });
                TestNoThrow("IndexOf(T, int)", delegate () { r = reference.IndexOf(item, 2); });
                TestTrue("IndexOf", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(int, T)", delegate () { q = test.FindIndex(2, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(int, T)", delegate () { r = reference.FindIndex(2, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindIndex", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T, int)", delegate () { q = test.IndexOf(item, 1); });
                TestNoThrow("IndexOf(T, int)", delegate () { r = reference.IndexOf(item, 1); });
                TestTrue("IndexOf", delegate () { return (q == 1) && (r == 1); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(int, T)", delegate () { q = test.FindIndex(1, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(int, T)", delegate () { r = reference.FindIndex(1, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindIndex", delegate () { return (q == 1) && (r == 1); });
                // test subrange indexing
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T, int, int)", delegate () { q = test.IndexOf(item, 2, p - 2); });
                TestNoThrow("IndexOf(T, int, int)", delegate () { r = reference.IndexOf(item, 2, p - 2); });
                TestTrue("IndexOf", delegate () { return (q == -1) && (r == -1); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { q = test.FindIndex(2, p - 2, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { r = reference.FindIndex(2, p - 2, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindIndex", delegate () { return (q == -1) && (r == -1); });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T, int, int)", delegate () { q = test.IndexOf(item, 1, p - 1); });
                TestNoThrow("IndexOf(T, int, int)", delegate () { r = reference.IndexOf(item, 1, p - 1); });
                TestTrue("IndexOf", delegate () { return (q == 1) && (r == 1); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { q = test.FindIndex(1, p - 1, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { r = reference.FindIndex(1, p - 1, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindIndex", delegate () { return (q == 1) && (r == 1); });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T, int, int)", delegate () { q = test.IndexOf(item, 2, p - 1); });
                TestNoThrow("IndexOf(T, int, int)", delegate () { r = reference.IndexOf(item, 2, p - 1); });
                TestTrue("IndexOf", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { q = test.FindIndex(2, p - 1, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { r = reference.FindIndex(2, p - 1, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindIndex", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOf(T, int, int)", delegate () { q = test.IndexOf(item, 1, p); });
                TestNoThrow("IndexOf(T, int, int)", delegate () { r = reference.IndexOf(item, 1, p); });
                TestTrue("IndexOf", delegate () { return (q == 1) && (r == 1); });
                q = r = Int32.MinValue;
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { q = test.FindIndex(1, p, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { r = reference.FindIndex(1, p, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindIndex", delegate () { return (q == 1) && (r == 1); });
                // bound check
                TestNoThrow("IndexOf(T, int, int)", delegate () { test.IndexOf(item, 0, 0); });
                TestNoThrow("IndexOf(T, int, int)", delegate () { test.IndexOf(item, reference.Count, 0); });
                TestThrow("IndexOf(T, int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.IndexOf(item, -1, 1); });
                TestThrow("IndexOf(T, int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.IndexOf(item, 0, -1); });
                TestThrow("IndexOf(T, int, int)", typeof(ArgumentException), delegate () { test.IndexOf(item, 0, reference.Count + 1); });
                TestThrow("IndexOf(T, int, int)", typeof(ArgumentException), delegate () { test.IndexOf(item, 1, reference.Count); });
                TestThrow("FindIndex(Predicate<T>)", typeof(ArgumentNullException), delegate () { test.FindIndex(null); });
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { q = test.FindIndex(0, 0, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindIndex(int, int, Predicate<T>)", delegate () { q = test.FindIndex(reference.Count, 0, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindIndex(int, int, Predicate<T>)", typeof(ArgumentOutOfRangeException), delegate () { q = test.FindIndex(-1, 0, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindIndex(int, int, Predicate<T>)", typeof(ArgumentOutOfRangeException), delegate () { q = test.FindIndex(0, -1, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindIndex(int, int, Predicate<T>)", typeof(ArgumentException), delegate () { q = test.FindIndex(0, reference.Count + 1, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindIndex(int, int, Predicate<T>)", typeof(ArgumentException), delegate () { q = test.FindIndex(1, reference.Count, delegate (int candidate) { return candidate == item; }); });
                Validate(reference, test);
                // indexofany
                TestNoThrow("Insert", delegate () { test.Insert(p + 1, unchecked(item + 1)); });
                TestNoThrow("Insert", delegate () { reference.Insert(p + 1, unchecked(item + 1)); });
                TestNoThrow("Insert", delegate () { test.Insert(p + 2, unchecked(item + 2)); });
                TestNoThrow("Insert", delegate () { reference.Insert(p + 2, unchecked(item + 2)); });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { item, unchecked(item + 1) }, 0, reference.Count); });
                TestTrue("IndexOfAny", delegate () { return q == 1; });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { item, unchecked(item + 1) }, 2, reference.Count - 2); });
                TestTrue("IndexOfAny", delegate () { return q == p; });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, 0, reference.Count); });
                TestTrue("IndexOfAny", delegate () { return q == p + 1; });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, p + 1, reference.Count - (p + 1)); });
                TestTrue("IndexOfAny", delegate () { return q == p + 1; });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, p + 2, reference.Count - (p + 2)); });
                TestTrue("IndexOfAny", delegate () { return q == p + 2; });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, p + 3, reference.Count - (p + 3)); });
                TestTrue("IndexOfAny", delegate () { return q == -1; });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, 0, 0); });
                TestTrue("IndexOfAny", delegate () { return q == -1; });
                q = r = Int32.MinValue;
                TestNoThrow("IndexOfAny", delegate () { q = test.IndexOfAny(new int[] { }, 0, reference.Count); });
                TestTrue("IndexOfAny", delegate () { return q == -1; });
                // bounds check
                TestThrow("IndexOfAny", typeof(ArgumentNullException), delegate () { test.IndexOfAny(null, 0, reference.Count); });
                TestThrow("IndexOfAny", typeof(ArgumentOutOfRangeException), delegate () { test.IndexOfAny(new int[] { 1 }, -1, 0); });
                TestThrow("IndexOfAny", typeof(ArgumentOutOfRangeException), delegate () { test.IndexOfAny(new int[] { 1 }, 0, -1); });
                TestThrow("IndexOfAny", typeof(ArgumentException), delegate () { test.IndexOfAny(new int[] { 1 }, 0, reference.Count + 1); });
                TestThrow("IndexOfAny", typeof(ArgumentException), delegate () { test.IndexOfAny(new int[] { 1 }, 1, reference.Count); });
                Validate(reference, test);
                // clear for next test
                TestClear(reference, test);
            }

            // find index reverse
            {
                int p, q = 0, r = 0, item;
                TestInsert(null, 0, 4 * HugeListInternalChunkSize, reference, test);
                p = reference.Count / 2;
                do
                {
                    item = rand.Next();
                } while ((item == 0) || unchecked(item + 1 == 0) || unchecked(item + 2 == 0));
                // set up item in middle
                TestNoThrow("Insert", delegate () { test.Insert(p, item); });
                TestNoThrow("Insert", delegate () { reference.Insert(p, item); });
                // find item 
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T)", delegate () { q = test.LastIndexOf(item); });
                TestNoThrow("LastIndexOf(T)", delegate () { r = reference.LastIndexOf(item); });
                TestTrue("LastIndexOf", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(Predicate<T>)", delegate () { q = test.FindLastIndex(delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindLastIndex(Predicate<T>)", delegate () { r = reference.FindLastIndex(delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex", delegate () { return (q == p) && (r == p); });
                // test not found
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T)", delegate () { q = test.LastIndexOf(unchecked(item + 1)); });
                TestNoThrow("LastIndexOf(T)", delegate () { r = reference.LastIndexOf(unchecked(item + 1)); });
                TestTrue("LastIndexOf", delegate () { return (q == -1) && (r == -1); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(Predicate<T>)", delegate () { q = test.FindLastIndex(delegate (int candidate) { return false; }); });
                TestNoThrow("FindLastIndex(Predicate<T>)", delegate () { r = reference.FindLastIndex(delegate (int candidate) { return false; }); });
                TestTrue("FindLastIndex", delegate () { return (q == -1) && (r == -1); });
                // set up with multiple instances
                q = r = Int32.MinValue;
                TestNoThrow("Insert", delegate () { test.Insert(reference.Count - 1, reference[p]); });
                TestNoThrow("Insert", delegate () { reference.Insert(reference.Count - 1, reference[p]); });
                // test indexing
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T, int)", delegate () { q = test.LastIndexOf(item, reference.Count - 3); });
                TestNoThrow("LastIndexOf(T, int)", delegate () { r = reference.LastIndexOf(item, reference.Count - 3); });
                TestTrue("LastIndexOf", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(int, T)", delegate () { q = test.FindLastIndex(reference.Count - 3, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindLastIndex(int, T)", delegate () { r = reference.FindLastIndex(reference.Count - 3, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T, int)", delegate () { q = test.LastIndexOf(item, reference.Count - 2); });
                TestNoThrow("LastIndexOf(T, int)", delegate () { r = reference.LastIndexOf(item, reference.Count - 2); });
                TestTrue("LastIndexOf", delegate () { return (q == reference.Count - 2) && (r == reference.Count - 2); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(int, T)", delegate () { q = test.FindLastIndex(reference.Count - 2, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindLastIndex(int, T)", delegate () { r = reference.FindLastIndex(reference.Count - 2, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex", delegate () { return (q == reference.Count - 2) && (r == reference.Count - 2); });
                // test subrange indexing
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { q = test.LastIndexOf(item, reference.Count - 3, reference.Count - 2 - (p + 1)); });
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { r = reference.LastIndexOf(item, reference.Count - 3, reference.Count - 2 - (p + 1)); });
                TestTrue("LastIndexOf", delegate () { return (q == -1) && (r == -1); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { q = test.FindLastIndex(reference.Count - 3, reference.Count - 2 - (p + 1), delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { r = reference.FindLastIndex(reference.Count - 3, reference.Count - 2 - (p + 1), delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex", delegate () { return (q == -1) && (r == -1); });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { q = test.LastIndexOf(item, reference.Count - 2, reference.Count - 1 - (p + 1)); });
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { r = reference.LastIndexOf(item, reference.Count - 2, reference.Count - 1 - (p + 1)); });
                TestTrue("LastIndexOf", delegate () { return (q == reference.Count - 2) && (r == reference.Count - 2); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { q = test.FindLastIndex(reference.Count - 2, reference.Count - 1 - (p + 1), delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { r = reference.FindLastIndex(reference.Count - 2, reference.Count - 1 - (p + 1), delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex", delegate () { return (q == reference.Count - 2) && (r == reference.Count - 2); });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { q = test.LastIndexOf(item, reference.Count - 3, reference.Count - 2 - p); });
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { r = reference.LastIndexOf(item, reference.Count - 3, reference.Count - 2 - p); });
                TestTrue("LastIndexOf", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { q = test.FindLastIndex(reference.Count - 3, reference.Count - 2 - p, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { r = reference.FindLastIndex(reference.Count - 3, reference.Count - 2 - p, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex", delegate () { return (q == p) && (r == p); });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { q = test.LastIndexOf(item, reference.Count - 2, reference.Count - 1 - p); });
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { r = reference.LastIndexOf(item, reference.Count - 2, reference.Count - 1 - p); });
                TestTrue("LastIndexOf", delegate () { return (q == reference.Count - 2) && (r == reference.Count - 2); });
                q = r = Int32.MinValue;
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { q = test.FindLastIndex(reference.Count - 2, reference.Count - 1 - p, delegate (int candidate) { return candidate == item; }); });
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { r = reference.FindLastIndex(reference.Count - 2, reference.Count - 1 - p, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex", delegate () { return (q == reference.Count - 2) && (r == reference.Count - 2); });
                // bound check
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { test.LastIndexOf(item, reference.Count - 1, 0); });
                TestThrow("LastIndexOf(T, int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.LastIndexOf(item, -1, 1); });
                TestThrow("LastIndexOf(T, int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.LastIndexOf(item, reference.Count, 1); });
                TestThrow("LastIndexOf(T, int, int)", typeof(ArgumentOutOfRangeException), delegate () { test.LastIndexOf(item, reference.Count - 1, -1); });
                TestThrow("LastIndexOf(T, int, int)", typeof(ArgumentException), delegate () { test.LastIndexOf(item, reference.Count - 1, reference.Count + 1); });
                TestThrow("LastIndexOf(T, int, int)", typeof(ArgumentException), delegate () { test.LastIndexOf(item, reference.Count - 2, reference.Count); });
                TestThrow("FindLastIndex(Predicate<T>)", typeof(ArgumentNullException), delegate () { test.FindLastIndex(null); });
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { q = test.FindLastIndex(reference.Count - 1, 0, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindLastIndex(int, int, Predicate<T>)", typeof(ArgumentOutOfRangeException), delegate () { q = test.FindLastIndex(-1, 0, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindLastIndex(int, int, Predicate<T>)", typeof(ArgumentOutOfRangeException), delegate () { q = test.FindLastIndex(reference.Count, 0, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindLastIndex(int, int, Predicate<T>)", typeof(ArgumentOutOfRangeException), delegate () { q = test.FindLastIndex(reference.Count - 1, -1, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindLastIndex(int, int, Predicate<T>)", typeof(ArgumentException), delegate () { q = test.FindLastIndex(reference.Count - 1, reference.Count + 1, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindLastIndex(int, int, Predicate<T>)", typeof(ArgumentException), delegate () { q = test.FindLastIndex(reference.Count - 2, reference.Count, delegate (int candidate) { return candidate == item; }); });
                Validate(reference, test);
                // indexofany
                TestNoThrow("Insert", delegate () { test.Insert(p, unchecked(item + 1)); });
                TestNoThrow("Insert", delegate () { reference.Insert(p, unchecked(item + 1)); });
                TestNoThrow("Insert", delegate () { test.Insert(p, unchecked(item + 2)); });
                TestNoThrow("Insert", delegate () { reference.Insert(p, unchecked(item + 2)); });
                p += 2;
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { item, unchecked(item + 1) }, reference.Count - 1, reference.Count); });
                TestTrue("LastIndexOfAny", delegate () { return q == reference.Count - 2; });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { item, unchecked(item + 1) }, reference.Count - 3, reference.Count - 2); });
                TestTrue("LastIndexOfAny", delegate () { return q == p; });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, reference.Count - 1, reference.Count); });
                TestTrue("LastIndexOfAny", delegate () { return q == p - 1; });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, p - 1, p); });
                TestTrue("LastIndexOfAny", delegate () { return q == p - 1; });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, p - 2, p - 1); });
                TestTrue("LastIndexOfAny", delegate () { return q == p - 2; });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, p - 3, p - 2); });
                TestTrue("LastIndexOfAny", delegate () { return q == -1; });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { unchecked(item + 1), unchecked(item + 2) }, reference.Count - 1, 0); });
                TestTrue("LastIndexOfAny", delegate () { return q == -1; });
                q = r = Int32.MinValue;
                TestNoThrow("LastIndexOfAny", delegate () { q = test.LastIndexOfAny(new int[] { }, reference.Count - 1, reference.Count); });
                TestTrue("LastIndexOfAny", delegate () { return q == -1; });
                // bounds check
                TestThrow("LastIndexOfAny", typeof(ArgumentNullException), delegate () { test.LastIndexOfAny(null, reference.Count - 1, reference.Count); });
                TestThrow("LastIndexOfAny", typeof(ArgumentOutOfRangeException), delegate () { test.LastIndexOfAny(new int[] { 1 }, -1, reference.Count); });
                TestThrow("LastIndexOfAny", typeof(ArgumentOutOfRangeException), delegate () { test.LastIndexOfAny(new int[] { 1 }, reference.Count, 0); });
                TestThrow("LastIndexOfAny", typeof(ArgumentOutOfRangeException), delegate () { test.LastIndexOfAny(new int[] { 1 }, reference.Count - 1, -1); });
                TestThrow("LastIndexOfAny", typeof(ArgumentException), delegate () { test.LastIndexOfAny(new int[] { 1 }, reference.Count - 1, reference.Count + 1); });
                TestThrow("LastIndexOfAny", typeof(ArgumentException), delegate () { test.LastIndexOfAny(new int[] { 1 }, reference.Count - 2, reference.Count); });
                Validate(reference, test);
                // clear for next test
                TestClear(reference, test);
                // final bounds check special case
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { test.LastIndexOf(item, -1, 0); });
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { test.LastIndexOf(item, 0, 0); });
                TestNoThrow("LastIndexOf(T, int, int)", delegate () { test.LastIndexOf(item, -1, 1); });
                TestNoThrow("FindLastIndex(int, int, Predicate<T>)", delegate () { q = test.FindLastIndex(-1, 0, delegate (int candidate) { return candidate == item; }); });
                TestThrow("FindLastIndex(int, int, Predicate<T>)", typeof(ArgumentOutOfRangeException), delegate () { q = test.FindLastIndex(0, 0, delegate (int candidate) { return candidate == item; }); });
                TestTrue("FindLastIndex(int, int, Predicate<T>)", delegate () { return -1 == test.FindLastIndex(-1, 0, delegate (int candidate) { return candidate == item; }); });
                TestTrue("LastIndexOfAny", delegate () { return -1 == test.LastIndexOfAny(new int[] { 1 }, -1, 0); });
            }

            // binary search
            {
                int[] data = new int[25 * HugeListInternalChunkSize];
                for (int i = 0; i < data.Length; i++)
                {
                    int item;
                    do
                    {
                        item = rand.Next();
                    } while (Array.IndexOf(data, item, 0, i) >= 0); //unique
                    data[i] = item;
                }
                Array.Sort(data);
                TestNoThrow("AddRange", delegate () { test.AddRange(data); });
                TestNoThrow("AddRange", delegate () { reference.AddRange(data); });
                // searches
                for (int i = 0; i < data.Length; i++)
                {
                    long startIter1 = IncrementIteration(true/*setLast*/);

                    int p = Int32.MinValue, q = Int32.MinValue;
                    int item = data[i];
                    TestNoThrow("BinarySearch(T)", delegate () { p = test.BinarySearch(item); });
                    TestNoThrow("BinarySearch(T)", delegate () { q = reference.BinarySearch(item); });
                    TestTrue("BinarySearch", delegate () { return p == q; });
                    TestTrue("BinarySearch", delegate () { return (reference[q] == item) && (test[p] == item); });
                    p = q = Int32.MinValue;
                    TestNoThrow("BinarySearch(int, int, T, Comparer<T>)", delegate () { p = test.BinarySearch(0, data.Length, item, null/*use default comparer*/); });
                    TestNoThrow("BinarySearch(int, int, T, Comparer<T>)", delegate () { q = reference.BinarySearch(0, data.Length, item, null/*use default comparer*/); });
                    TestTrue("BinarySearch", delegate () { return p == q; });
                    TestTrue("BinarySearch", delegate () { return (reference[q] == item) && (test[p] == item); });
                    p = q = Int32.MinValue;
                    TestNoThrow("BinarySearch(int, int, T, Comparer<T>)", delegate () { p = test.BinarySearch(4, data.Length - 8, item, Comparer<int>.Default); });
                    TestNoThrow("BinarySearch(int, int, T, Comparer<T>)", delegate () { q = reference.BinarySearch(4, data.Length - 8, item, Comparer<int>.Default); });
                    TestTrue("BinarySearch", delegate () { return p == q; });
                    if ((i < 4) || (i >= data.Length - 4))
                    {
                        TestTrue("BinarySearch", delegate () { return (p < 0) && (q < 0); });
                    }
                    else
                    {
                        TestTrue("BinarySearch", delegate () { return (reference[q] == item) && (test[p] == item); });
                    }
                }
                TestClear(reference, test);
                // test custom comparer
                data = new int[25 * HugeListInternalChunkSize];
                for (int i = 0; i < data.Length; i++)
                {
                    int item;
                    do
                    {
                        item = rand.Next();
                    } while (Array.IndexOf(data, item, 0, i) >= 0); //unique
                    data[i] = item;
                }
                Array.Sort(data);
                Array.Reverse(data);
                TestNoThrow("AddRange", delegate () { test.AddRange(data); });
                TestNoThrow("AddRange", delegate () { reference.AddRange(data); });
                // searches
                for (int i = 0; i < data.Length; i++)
                {
                    long startIter1 = IncrementIteration(true/*setLast*/);

                    int p = Int32.MinValue, q = Int32.MinValue;
                    int item = data[i];
                    TestNoThrow("BinarySearch(T, Comparer<T>)", delegate () { p = test.BinarySearch(item, new InvertedComparer()); });
                    TestNoThrow("BinarySearch(T, Comparer<T>)", delegate () { q = reference.BinarySearch(item, new InvertedComparer()); });
                    TestTrue("BinarySearch", delegate () { return p == q; });
                    TestTrue("BinarySearch", delegate () { return (reference[q] == item) && (test[p] == item); });
                }
                for (int i = 0; i < data.Length; i++)
                {
                    long startIter1 = IncrementIteration(true/*setLast*/);

                    int p = Int32.MinValue, q = Int32.MinValue;
                    int item = data[i];
                    TestNoThrow("BinarySearch(T, Comparer<T>)", delegate () { p = test.BinarySearch(0, data.Length, item, new InvertedComparer()); });
                    TestNoThrow("BinarySearch(T, Comparer<T>)", delegate () { q = reference.BinarySearch(0, data.Length, item, new InvertedComparer()); });
                    TestTrue("BinarySearch", delegate () { return p == q; });
                    TestTrue("BinarySearch", delegate () { return (reference[q] == item) && (test[p] == item); });
                }
                // boundary conditions
                TestNoThrow("BinarySearch", delegate () { test.BinarySearch(0, 0, Int32.MaxValue, Comparer<int>.Default); });
                TestNoThrow("BinarySearch", delegate () { test.BinarySearch(data.Length, 0, Int32.MaxValue, Comparer<int>.Default); });
                TestThrow("BinarySearch", typeof(ArgumentOutOfRangeException), delegate () { test.BinarySearch(-1, data.Length, Int32.MaxValue, Comparer<int>.Default); });
                TestThrow("BinarySearch", typeof(ArgumentOutOfRangeException), delegate () { test.BinarySearch(0, -1, Int32.MaxValue, Comparer<int>.Default); });
                TestThrow("BinarySearch", typeof(ArgumentException), delegate () { test.BinarySearch(0, data.Length + 1, Int32.MaxValue, Comparer<int>.Default); });
                TestThrow("BinarySearch", typeof(ArgumentException), delegate () { test.BinarySearch(1, data.Length, Int32.MaxValue, Comparer<int>.Default); });
                Validate(reference, test);
                // multi
                TestClear(reference, test);
                data = new int[25 * HugeListInternalChunkSize];
                const int Duplicity = 5;
                for (int i = 0; i < data.Length; i++)
                {
                    int item;
                    do
                    {
                        item = rand.Next();
                    } while (Array.IndexOf(data, item, 0, i) >= 0); //unique
                    for (int j = 0; (j < Duplicity) && (i < data.Length); j++, i++)
                    {
                        data[i] = item;
                    }
                }
                Array.Sort(data);
                TestNoThrow("AddRange", delegate () { test.AddRange(data); });
                TestNoThrow("AddRange", delegate () { reference.AddRange(data); });
                for (int i = 0; i < data.Length; i += Duplicity)
                {
                    long startIter1 = IncrementIteration(true/*setLast*/);

                    int item = data[i];
                    int p, q;
                    p = q = Int32.MinValue;
                    TestNoThrow("BinarySearch(T, Comparer<T>)", delegate () { p = test.BinarySearch(0, data.Length, item, Comparer<int>.Default, true/*multi*/); });
                    TestNoThrow("BinarySearch(T, Comparer<T>)", delegate () { q = reference.BinarySearch(0, data.Length, item, Comparer<int>.Default); });
                    while ((q > 0) && (data[q] == data[q - 1]))
                    {
                        q--;
                    }
                    TestTrue("BinarySearch", delegate () { return p == q; });
                    TestTrue("BinarySearch", delegate () { return (reference[q] == item) && (test[p] == item); });
                }
                // clear for next test
                TestClear(reference, test);
            }

            // RemoveAll reentrance checking
            // (Note, List<> does not handle this case, so avoid testing it on the reference implementation)
            if (!String.Equals(test.GetType().Name, "ReferenceHugeList`1"))
            {
                for (int i = 0; i < 100; i++)
                {
                    test.Add(rand.Next());
                }

                int j = 0;
                TestThrow("RemoveAll(Predicate<T> match)", typeof(InvalidOperationException),
                    delegate ()
                    {
                        test.RemoveAll(
                            delegate (int candidate)
                            {
                                j++;
                                if (j == 50)
                                {
                                    test.Add(rand.Next()); // illegal
                                }
                                return candidate % 2 == 0;
                            });
                    });
                SelfValidate(test); // state of data not defined, but internal structure must be consistent
                test.Clear();
            }

            // CASE A22 [code coverage, randomly derived] - valid only in 512 block size case
            TestOps("A22", test, delegate () { return rand.Next(); }, false/*pruning*/, new Op<Int32>[] {
                new OpInsertRange<Int32>(0, null, 0, 1293),
                new OpInsert<Int32>(1086, default(Int32)),
                new OpRemoveAt<Int32>(1061),
                new OpRemoveRange<Int32>(570, 37),
                new OpRemoveRange<Int32>(312, 944),
                new OpInsert<Int32>(80, default(Int32)),
                new OpRemoveAt<Int32>(32),
                new OpInsertRange<Int32>(148, null, 0, 4207),
                new OpInsert<Int32>(1082, default(Int32)),
                new OpRemoveAt<Int32>(4315),
                new OpRemoveAt<Int32>(4334),
                new OpRemoveRange<Int32>(3544, 14),
                new OpRemoveAt<Int32>(2812),
                new OpInsertRange<Int32>(3730, null, 0, 408) });
            test.Clear();

            // CASE A23 [code coverage, randomly derived] - valid only in 512 block size case
            TestOps("A23", test, delegate () { return rand.Next(); }, false/*pruning*/, new Op<Int32>[] {
                new OpInsertRange<Int32>(0, null, 0, 4146),
                new OpRemoveRange<Int32>(1686, 8),
                new OpInsert<Int32>(1946, default(Int32)),
                new OpRemoveRange<Int32>(2602, 112),
                new OpInsert<Int32>(3991, default(Int32)),
                new OpInsert<Int32>(962, default(Int32)),
                new OpInsert<Int32>(2529, default(Int32)),
                new OpRemoveAt<Int32>(1413),
                new OpInsertRange<Int32>(2422, null, 0, 0),
                new OpRemoveAt<Int32>(2061),
                new OpRemoveAt<Int32>(4022),
                new OpRemoveRange<Int32>(3464, 365) });
            test.Clear();

            // CASE A24 [code coverage, randomly derived] - valid only in 512 block size case
            TestOps("A24", test, delegate () { return rand.Next(); }, false/*pruning*/, new Op<Int32>[] {
                new OpInsert<Int32>(0, default(Int32)),
                new OpRemoveRange<Int32>(0, 0),
                new OpRemoveRange<Int32>(0, 0),
                new OpRemoveRange<Int32>(0, 0),
                new OpRemoveAt<Int32>(0),
                new OpRemoveRange<Int32>(0, 0),
                new OpInsertRange<Int32>(0, null, 0, 1172),
                new OpRemoveRange<Int32>(953, 219),
                new OpInsert<Int32>(41, default(Int32)),
                new OpInsertRange<Int32>(452, null, 1, 21),
                new OpRemoveRange<Int32>(36, 939),
                new OpInsertRange<Int32>(36, null, 0, 408),
                new OpRemoveAt<Int32>(391),
                new OpRemoveRange<Int32>(0, 229),
                new OpInsert<Int32>(49, default(Int32)),
                new OpRemoveAt<Int32>(94),
                new OpRemoveAt<Int32>(210),
                new OpInsert<Int32>(205, default(Int32)),
                new OpInsert<Int32>(95, default(Int32)),
                new OpRemoveAt<Int32>(121),
                new OpInsertRange<Int32>(200, null, 0, 3686),
                new OpRemoveRange<Int32>(2215, 26),
                new OpInsertRange<Int32>(3128, null, 0, 30),
                new OpRemoveAt<Int32>(2521),
                new OpInsertRange<Int32>(2725, null, 1, 250),
                new OpRemoveAt<Int32>(3991),
                new OpRemoveRange<Int32>(593, 1786),
                new OpInsert<Int32>(1847, default(Int32)),
                new OpInsert<Int32>(178, default(Int32)),
                new OpRemoveAt<Int32>(2068),
                new OpRemoveRange<Int32>(647, 24),
                new OpInsert<Int32>(1892, default(Int32)),
                new OpInsert<Int32>(462, default(Int32)),
                new OpInsertRange<Int32>(0, null, 1, 1503),
                new OpRemoveRange<Int32>(2889, 941) });
            test.Clear();

            // CASE A25 [code coverage, randomly derived] - valid only in 512 block size case
            TestOps("A25", test, delegate () { return rand.Next(); }, true/*pruning*/, new Op<Int32>[] {
                new OpInsert<Int32>(0, default(Int32)),
                new OpInsertRange<Int32>(0, null, 0, 1614),
                new OpRemoveRange<Int32>(835, 580),
                new OpInsert<Int32>(570, default(Int32)),
                new OpRemoveRange<Int32>(1000, 36),
                new OpRemoveRange<Int32>(166, 834),
                new OpInsert<Int32>(102, default(Int32)),
                new OpRemoveRange<Int32>(17, 84) });
            test.Clear();

            // LAST USED: A25
        }

        private class InvertedComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return (-x).CompareTo(-y);
            }
        }

        private void SelfValidate(IHugeList<int> test)
        {
            IHugeListValidation validation = test as IHugeListValidation;
            if (validation != null)
            {
                validation.Validate();
            }
        }

        private void Validate(List<int> reference, IHugeList<int> test)
        {
            SelfValidate(test);

            TestTrue("Count", delegate () { return reference.Count == test.Count; });
            for (int i = 0; i < reference.Count; i++)
            {
                TestTrue("item", delegate () { return reference[i] == test[i]; });
            }
        }

        private class NoDefaultConstructor<T>
        {
            private NoDefaultConstructor()
            {
            }

            public NoDefaultConstructor(T t)
            {
            }
        }


        private void HugeListBasicCoverage()
        {
            HugeListTestSpecific(delegate () { return new ReferenceHugeList<int>(); }, false/*checkBlockSize*/);

            // test all three tree types (really shouldn't matter) with specific testing-tuned block size
            HugeListTestSpecific(delegate () { return new HugeList<int>(new AVLTreeRangeMap<int[]>(), HugeListInternalChunkSize); }, true/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new HugeList<int>(new RedBlackTreeRangeMap<int[]>(), HugeListInternalChunkSize); }, true/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new HugeList<int>(new SplayTreeRangeMap<int[]>(), HugeListInternalChunkSize); }, true/*checkBlockSize*/);

            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(new RedBlackTreeRangeMapLong<int[]>(), HugeListInternalChunkSize)); }, true/*checkBlockSize*/);

            // test variations based on construction
            // typeof constructor variation
            HugeListTestSpecific(delegate () { return new HugeList<int>(typeof(AVLTreeRangeMap<>), HugeListInternalChunkSize); }, true/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(AVLTreeRangeMapLong<>), HugeListInternalChunkSize)); }, true/*checkBlockSize*/);
            // pass in instance, without chunk size argument (uses internal default)
            HugeListTestSpecific(delegate () { return new HugeList<int>(new AVLTreeRangeMap<int[]>()); }, false/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(new AVLTreeRangeMapLong<int[]>())); }, false/*checkBlockSize*/);
            // typeof, without chunk size argument (uses internal default)
            HugeListTestSpecific(delegate () { return new HugeList<int>(typeof(AVLTreeRangeMap<>)); }, false/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(AVLTreeRangeMapLong<>))); }, false/*checkBlockSize*/);
            // special case - max block size 1 (degenerate form)
            HugeListTestSpecific(delegate () { return new HugeList<int>(typeof(AVLTreeRangeMap<>), 1); }, false/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(AVLTreeRangeMapLong<>), 1)); }, false/*checkBlockSize*/);
            // special case - max block size 2
            HugeListTestSpecific(delegate () { return new HugeList<int>(typeof(AVLTreeRangeMap<>), 2); }, false/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(AVLTreeRangeMapLong<>), 2)); }, false/*checkBlockSize*/);
            // default (splay) constructor variation
            HugeListTestSpecific(delegate () { return new HugeList<int>(); }, false/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>()); }, false/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new HugeList<int>(HugeListInternalChunkSize); }, true/*checkBlockSize*/);
            HugeListTestSpecific(delegate () { return new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(HugeListInternalChunkSize)); }, true/*checkBlockSize*/);

            // validate constructor error checking
            TestThrow("constructor", typeof(ArgumentNullException), delegate () { new HugeList<int>((AVLTreeRangeMap<int[]>)null); });
            TestThrow("constructor", typeof(ArgumentNullException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>((AVLTreeRangeMapLong<int[]>)null)); });
            //
            TestThrow("constructor", typeof(ArgumentNullException), delegate () { new HugeList<int>((Type)null); });
            TestThrow("constructor", typeof(ArgumentNullException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>((Type)null)); });
            //
            TestThrow("constructor", typeof(ArgumentOutOfRangeException), delegate () { new HugeList<int>(new AVLTreeRangeMap<int[]>(), 0); });
            TestThrow("constructor", typeof(ArgumentOutOfRangeException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(new AVLTreeRangeMapLong<int[]>(), 0)); });
            //
            TestThrow("constructor", typeof(ArgumentOutOfRangeException), delegate () { new HugeList<int>(typeof(AVLTreeRangeMap<>), 0); });
            TestThrow("constructor", typeof(ArgumentOutOfRangeException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(AVLTreeRangeMapLong<>), 0)); });
            //
            TestThrow("constructor", typeof(ArgumentOutOfRangeException), delegate () { new HugeList<int>(0); });
            TestThrow("constructor", typeof(ArgumentOutOfRangeException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(0)); });
            //
            TestThrow("constructor", typeof(ArgumentException), delegate () { new HugeList<int>(typeof(List<>)); });
            TestThrow("constructor", typeof(ArgumentException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(List<>))); });
            //
            TestThrow("constructor", typeof(ArgumentException), delegate () { new HugeList<int>(typeof(NoDefaultConstructor<>)); });
            TestThrow("constructor", typeof(ArgumentException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(NoDefaultConstructor<>))); });
            //
            TestThrow("constructor", typeof(ArgumentException), delegate () { new HugeList<int>(typeof(int)); });
            TestThrow("constructor", typeof(ArgumentException), delegate () { new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(int))); });
            //
            TestThrow(
                "constructor",
                typeof(ArgumentException),
                delegate ()
                {
                    IRangeMap<int[]> tree = new AVLTreeRangeMap<int[]>();
                    tree.Insert(0, 1, null);
                    IHugeList<int> x = new HugeList<int>(tree);
                });
            TestThrow(
                "constructor",
                typeof(ArgumentException),
                delegate ()
                {
                    IRangeMapLong<int[]> tree = new AVLTreeRangeMapLong<int[]>();
                    tree.Insert(0, 1, null);
                    IHugeListLong<int> x = new HugeListLong<int>(tree);
                });
        }


        public override bool Do()
        {
            try
            {
                this.HugeListBasicCoverage();

                return true;
            }
            catch (Exception)
            {
                WriteIteration();
                throw;
            }
        }
    }
}
