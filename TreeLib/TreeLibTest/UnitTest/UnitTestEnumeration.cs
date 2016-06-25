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
    public class UnitTestEnumeration : TestBase
    {
        public UnitTestEnumeration()
            : base()
        {
        }

        public UnitTestEnumeration(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private const int Seed = 1;

        private readonly static int[] TestCaseSizes = new int[] { 0, 1, 2, 3, 5, 10, 31 };



        //
        // Map & List, RankMap & RankList, MultiRankMap & MultiRankList
        //

        private delegate void KeyedAddAction(object testTree, int key, float value, int count);
        private delegate void KeyedRemoveAction(object testTree, int key);
        private delegate bool KeyedContainsAction(object testTree, int key);

        //

        private static void MapKeyedAdd<KeyType, ValueType>(object testTree, KeyType key, ValueType value, int count) where KeyType : IComparable<KeyType>
        {
            ((IOrderedMap<KeyType, ValueType>)testTree).Add(key, value);
        }

        private static void MapKeyedRemove<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            ((IOrderedMap<KeyType, ValueType>)testTree).Remove(key);
        }

        private static bool MapKeyedContains<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            return ((IOrderedMap<KeyType, ValueType>)testTree).ContainsKey(key);
        }

        private static void ListKeyedAdd<KeyType, ValueType>(object testTree, KeyType key, ValueType value, int count) where KeyType : IComparable<KeyType>
        {
            ((IOrderedList<KeyType>)testTree).Add(key);
        }

        private static void ListKeyedRemove<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            ((IOrderedList<KeyType>)testTree).Remove(key);
        }

        private static bool ListKeyedContains<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            return ((IOrderedList<KeyType>)testTree).ContainsKey(key);
        }

        //

        private static void RankMapKeyedAdd<KeyType, ValueType>(object testTree, KeyType key, ValueType value, int count) where KeyType : IComparable<KeyType>
        {
            ((IRankMap<KeyType, ValueType>)testTree).Add(key, value);
        }

        private static void RankMapKeyedRemove<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            ((IRankMap<KeyType, ValueType>)testTree).Remove(key);
        }

        private static bool RankMapKeyedContains<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            return ((IRankMap<KeyType, ValueType>)testTree).ContainsKey(key);
        }

        private static void RankListKeyedAdd<KeyType, ValueType>(object testTree, KeyType key, ValueType value, int count) where KeyType : IComparable<KeyType>
        {
            ((IRankList<KeyType>)testTree).Add(key);
        }

        private static void RankListKeyedRemove<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            ((IRankList<KeyType>)testTree).Remove(key);
        }

        private static bool RankListKeyedContains<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            return ((IRankList<KeyType>)testTree).ContainsKey(key);
        }

        //

        private static void MultiRankMapKeyedAdd<KeyType, ValueType>(object testTree, KeyType key, ValueType value, int count) where KeyType : IComparable<KeyType>
        {
            ((IMultiRankMap<KeyType, ValueType>)testTree).Add(key, value, count);
        }

        private static void MultiRankMapKeyedRemove<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            ((IMultiRankMap<KeyType, ValueType>)testTree).Remove(key);
        }

        private static bool MultiRankMapKeyedContains<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            return ((IMultiRankMap<KeyType, ValueType>)testTree).ContainsKey(key);
        }

        private static void MultiRankListKeyedAdd<KeyType, ValueType>(object testTree, KeyType key, ValueType value, int count) where KeyType : IComparable<KeyType>
        {
            ((IMultiRankList<KeyType>)testTree).Add(key, count);
        }

        private static void MultiRankListKeyedRemove<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            ((IMultiRankList<KeyType>)testTree).Remove(key);
        }

        private static bool MultiRankListKeyedContains<KeyType, ValueType>(object testTree, KeyType key) where KeyType : IComparable<KeyType>
        {
            return ((IMultiRankList<KeyType>)testTree).ContainsKey(key);
        }

        //

        private void TestMap<EntryType>(
            object testTree,
            TreeKind treeKind,
            KeyedAddAction addAction,
            KeyedRemoveAction removeAction,
            KeyedContainsAction containsKeyAction)
        {
            long startIteration = IncrementIteration();

            // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
            // sequence of keys (with uniform distribution). The unit test will use the same set of keys and therefore the
            // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
            ParkAndMiller random = new ParkAndMiller(Seed);

            const int KeyIncrement = 100;
            Debug.Assert(KeyIncrement % 2 == 0);

            bool valueValid = (treeKind & TreeKind.AllValued) != 0;
            bool countConst1 = (treeKind & TreeKind.AllUniRank) != 0;
            bool countVariable = (treeKind & TreeKind.AllMultiRank) != 0;

            foreach (int size in TestCaseSizes)
            {
                // build benchmark

                BigEntry<int, float>[] master = new BigEntry<int, float>[size];
                for (int i = 0; i < master.Length; i++)
                {
                    master[i] = new BigEntry<int, float>(
                        KeyIncrement * (i + 1),
                        valueValid ? i * .1f : 0f,
                        countConst1 ? 1 : (countVariable ? random.Next() % 100 + 1 : 0));
                }
                int nextKey = KeyIncrement * (size + 1);
                RecalcStarts(master);

                int[] keySequence = new int[size];
                {
                    bool[] entered = new bool[size];
                    for (int i = 0; i < master.Length; i++)
                    {
                        int index;
                        do
                        {
                            index = random.Next() % size;
                        } while (entered[index]);

                        keySequence[i] = index;
                        entered[index] = true;
                    }
                }

                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                foreach (int index in keySequence)
                {
                    addAction(testTree, master[index].key, master[index].value, master[index].xLength);
                }


                // test basic enumeration

                {
                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.DefaultOld, treeKind, new EnumArgsProvider(), null/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.Enumerable, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), true/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), true/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                    };

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (EntryType entry in enumerators[e])
                        {
                            TestTrue("enum overrun", delegate () { return c < master.Length; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == master.Length; });
                        Validate(master, testEntries.ToArray());
                    }
                }


                // test reverse enumeration

                {
                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    BigEntry<int, float>[] reverseMaster = (BigEntry<int, float>[])master.Clone();
                    Array.Reverse(reverseMaster);

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (EntryType entry in enumerators[e])
                        {
                            TestTrue("enum overrun", delegate () { return c < reverseMaster.Length; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == reverseMaster.Length; });
                        Validate(reverseMaster, testEntries.ToArray());
                    }
                }


                // test basic enumeration on non-generic interface
                {
                    IEnumerable[] enumerators = new IEnumerable[]
                    {
                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/)),
                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.DefaultOld, treeKind, new EnumArgsProvider(), null/*forward*/)),

                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.Enumerable, treeKind, new EnumArgsProvider(), null/*forward*/)),

                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/)),

                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/)),
                    };

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (object o in enumerators[e])
                        {
                            EntryType entry = (EntryType)o;
                            TestTrue("enum overrun", delegate () { return c < master.Length; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == master.Length; });
                        Validate(master, testEntries.ToArray());
                    }
                }


                // test key-constrained forward enumeration

                for (int testKey = KeyIncrement / 2; testKey <= nextKey; testKey += KeyIncrement / 2)
                {
                    long startIteration2 = IncrementIteration(true/*setLast*/);

                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerable, treeKind, new EnumKeyedArgsProvider<int>(testKey), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableBidir, treeKind, new EnumKeyedArgsProvider<int>(testKey), true/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableFast, treeKind, new EnumKeyedArgsProvider<int>(testKey), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableFastBidir, treeKind, new EnumKeyedArgsProvider<int>(testKey), true/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableRobust, treeKind, new EnumKeyedArgsProvider<int>(testKey), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableRobustBidir, treeKind, new EnumKeyedArgsProvider<int>(testKey), true/*forward*/),
                    };

                    List<BigEntry<int, float>> filteredMaster = new List<BigEntry<int, float>>(master);
                    filteredMaster.RemoveAll(delegate (BigEntry<int, float> candidate) { return candidate.key < testKey; });

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (EntryType entry in enumerators[e])
                        {
                            TestTrue("enum overrun", delegate () { return c < filteredMaster.Count; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == filteredMaster.Count; });
                        Validate(filteredMaster.ToArray(), testEntries.ToArray());
                    }
                }


                // test key-constrained reverse enumeration

                for (int testKey = KeyIncrement / 2; testKey <= nextKey; testKey += KeyIncrement / 2)
                {
                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableBidir, treeKind, new EnumKeyedArgsProvider<int>(testKey), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableFastBidir, treeKind, new EnumKeyedArgsProvider<int>(testKey), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.KeyedEnumerableRobustBidir, treeKind, new EnumKeyedArgsProvider<int>(testKey), false/*forward*/),
                    };

                    List<BigEntry<int, float>> filteredReversedMaster = new List<BigEntry<int, float>>(master);
                    filteredReversedMaster.RemoveAll(delegate (BigEntry<int, float> candidate) { return candidate.key > testKey; });
                    filteredReversedMaster.Reverse();

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (EntryType entry in enumerators[e])
                        {
                            TestTrue("enum overrun", delegate () { return c < filteredReversedMaster.Count; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == filteredReversedMaster.Count; });
                        Validate(filteredReversedMaster.ToArray(), testEntries.ToArray());
                    }
                }


                // test boundary cases (non-keyed cases only)

                {
                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.DefaultOld, treeKind, new EnumArgsProvider(), null/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.Enumerable, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        IEnumerator<EntryType> enumerator = enumerators[e].GetEnumerator();

                        TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                        int c = 0;
                        while (enumerator.MoveNext())
                        {
                            TestNoThrow("enumeration", delegate () { string text = enumerator.Current.ToString(); });
                            TestNoThrow("enumeration", delegate () { int hash = enumerator.Current.GetHashCode(); });

                            TestTrue("enumeration", delegate () { return enumerator.Current.Equals(enumerator.Current); });

                            c++;
                        }
                        TestTrue("enumeration", delegate () { return c == master.Length; });

                        TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                        TestFalse("enumeration", delegate () { return enumerator.MoveNext(); }); // extra MoveNext after termination
                    }
                }


                // test interrupting enumerator with tree modifications, and while we're at it, Clear()
                // (non-keyed cases only)

                if (size != 0)
                {
                    IEnumerable<EntryType>[] enumeratorsFastForward = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                    };

                    IEnumerable<EntryType>[] enumeratorsFastReverse = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    IEnumerable<EntryType>[] enumeratorsRobustForward = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                    };

                    IEnumerable<EntryType>[] enumeratorsRobustReverse = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    foreach (IEnumerable<EntryType>[] mode in new IEnumerable<EntryType>[][]
                        { enumeratorsFastForward, enumeratorsFastReverse, enumeratorsRobustForward, enumeratorsRobustReverse })
                    {
                        // FRAGILE:
                        bool forward = (mode == enumeratorsFastForward) || (mode == enumeratorsRobustForward);
                        bool robust = (mode == enumeratorsRobustForward) || (mode == enumeratorsRobustReverse);

                        bool shouldThrowOnChange = !robust;

                        // test add during enumeration

                        int actionPoint = size / 2;
                        int actionKey = forward ? master[actionPoint].key : master[master.Length - 1 - actionPoint].key;
                        for (int addKeyIndex = 0; addKeyIndex <= size; addKeyIndex++)
                        {
                            int addKey = addKeyIndex * KeyIncrement + 50;
                            int addCount = countConst1 ? 1 : (countVariable ? random.Next() % 100 + 1 : 0);
                            for (int e = 0; e < mode.Length; e++)
                            {
                                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                foreach (int index in keySequence)
                                {
                                    addAction(testTree, master[index].key, master[index].value, master[index].xLength);
                                }

                                List<BigEntry<int, float>> addedMaster = new List<BigEntry<int, float>>(master);
                                int insertPoint = ~addedMaster.BinarySearch(new BigEntry<int, float>(addKey, 0), new BigEntryKeyComparer<int, float>());
                                Debug.Assert(insertPoint >= 0);
                                if ((forward && (actionPoint < insertPoint)) || (!forward && (master.Length - 1 - actionPoint >= insertPoint)))
                                {
                                    addedMaster.Insert(insertPoint, new BigEntry<int, float>(addKey, 0f, addCount));
                                }
                                if (!forward)
                                {
                                    addedMaster.Reverse();
                                }
                                ZeroIndexes(addedMaster);

                                long startIteration1 = IncrementIteration(true/*setLast*/);
                                try
                                {
                                    int c = 0;
                                    List<BigEntry<int, float>> entries = new List<BigEntry<int, float>>();
                                    bool added = false;
                                    foreach (EntryType entry in mode[e])
                                    {
                                        if (added && shouldThrowOnChange)
                                        {
                                            Fault(testTree, "Enumerator was expected to throw and didn't");
                                        }

                                        TestTrue("enum overrun", delegate () { return c < Math.Max(master.Length, addedMaster.Count); });

                                        BigEntry<int, float> bigEntry = new BigEntry<int, float>(entry, treeKind);
                                        entries.Add(bigEntry);

                                        if (c == actionPoint)
                                        {
                                            Debug.Assert(actionKey == bigEntry.key);
                                            if (forward)
                                            {
                                                Debug.Assert((addedMaster.Count == master.Length) == (addKey < bigEntry.key));
                                            }
                                            else
                                            {
                                                Debug.Assert((addedMaster.Count == master.Length) == (addKey > bigEntry.key));
                                            }

                                            added = true;
                                            TestNoThrow("enum-add", delegate () { addAction(testTree, addKey, default(float), addCount); });
                                        }

                                        c++;
                                    }
                                    if (added && shouldThrowOnChange)
                                    {
                                        Fault(testTree, "Enumerator was expected to throw and didn't");
                                    }
                                    TestTrue("enum count", delegate () { return c == addedMaster.Count; });
                                    ZeroIndexes(entries); // TODO: check that ranks of post-insertion higher-keyed items change as expected
                                    Validate(addedMaster.ToArray(), entries.ToArray());
                                }
                                catch (InvalidOperationException) when (shouldThrowOnChange)
                                {
                                    // expected
                                }
                                catch (Exception exception)
                                {
                                    Fault(testTree, "Enumerator threw unexpected exception", exception);
                                }
                            }
                        }

                        // test remove during enumeration

                        for (int removeKeyIndex = 0; removeKeyIndex < size; removeKeyIndex++)
                        {
                            for (int e = 0; e < mode.Length; e++)
                            {
                                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                foreach (int index in keySequence)
                                {
                                    addAction(testTree, master[index].key, master[index].value, master[index].xLength);
                                }

                                List<BigEntry<int, float>> removedMaster = new List<BigEntry<int, float>>(master);
                                int removeCount = master[removeKeyIndex].xLength;
                                if ((forward && (actionPoint < removeKeyIndex)) || (!forward && (master.Length - 1 - actionPoint > removeKeyIndex)))
                                {
                                    removedMaster.RemoveAt(removeKeyIndex);
                                }
                                if (!forward)
                                {
                                    removedMaster.Reverse();
                                }
                                ZeroIndexes(removedMaster);

                                long startIteration1 = IncrementIteration(true/*setLast*/);
                                try
                                {
                                    int c = 0;
                                    List<BigEntry<int, float>> entries = new List<BigEntry<int, float>>();
                                    bool removed = false;
                                    foreach (EntryType entry in mode[e])
                                    {
                                        if (removed && shouldThrowOnChange)
                                        {
                                            Fault(testTree, "Enumerator was expected to throw and didn't");
                                        }

                                        TestTrue("enum overrun", delegate () { return c < Math.Max(master.Length, removedMaster.Count); });

                                        BigEntry<int, float> bigEntry = new BigEntry<int, float>(entry, treeKind);
                                        entries.Add(bigEntry);

                                        if (c == actionPoint)
                                        {
                                            Debug.Assert(actionKey == bigEntry.key);
                                            if (forward)
                                            {
                                                Debug.Assert((removedMaster.Count == master.Length)
                                                    == (master[removeKeyIndex].key <= bigEntry.key));
                                            }
                                            else
                                            {
                                                Debug.Assert((removedMaster.Count == master.Length)
                                                    == (master[removeKeyIndex].key >= bigEntry.key));
                                            }

                                            removed = true;
                                            TestNoThrow("enum-add", delegate () { removeAction(testTree, master[removeKeyIndex].key); });
                                        }

                                        c++;
                                    }
                                    if (removed && shouldThrowOnChange)
                                    {
                                        Fault(testTree, "Enumerator was expected to throw and didn't");
                                    }
                                    TestTrue("enum count", delegate () { return c == removedMaster.Count; });
                                    ZeroIndexes(entries); // TODO: check that ranks of post-insertion higher-keyed items change as expected
                                    Validate(removedMaster.ToArray(), entries.ToArray());
                                }
                                catch (InvalidOperationException) when (shouldThrowOnChange)
                                {
                                    // expected
                                }
                                catch (Exception exception)
                                {
                                    Fault(testTree, "Enumerator threw unexpected exception", exception);
                                }
                            }
                        }

                        // test query during enumeration

                        {
                            testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                            foreach (int index in keySequence)
                            {
                                addAction(testTree, master[index].key, master[index].value, master[index].xLength);
                            }

                            int queryKeyIndex = size / 2;

                            bool queryShouldThrowOnChange = ((treeKind & TreeKind.Splay) != 0) && !robust;

                            for (int e = 0; e < mode.Length; e++)
                            {
                                long startIteration1 = IncrementIteration(true/*setLast*/);
                                try
                                {
                                    int c = 0;
                                    List<BigEntry<int, float>> entries = new List<BigEntry<int, float>>();
                                    bool queried = false;
                                    foreach (EntryType entry in mode[e])
                                    {
                                        if (queryShouldThrowOnChange & queried)
                                        {
                                            Fault(testTree, "Enumerator was expected to throw and didn't");
                                        }

                                        TestTrue("enum overrun", delegate () { return c < master.Length; });

                                        entries.Add(new BigEntry<int, float>(entry, treeKind));

                                        if (c == actionPoint)
                                        {
                                            queried = true;
                                            TestNoThrow("enum-add", delegate () { bool f = containsKeyAction(testTree, master[queryKeyIndex].key); });
                                        }

                                        c++;
                                    }
                                    if (queried && queryShouldThrowOnChange)
                                    {
                                        Fault(testTree, "Enumerator was expected to throw and didn't");
                                    }
                                    TestTrue("enum count", delegate () { return c == master.Length; });
                                    if (!forward)
                                    {
                                        entries.Reverse();
                                    }
                                    Validate(master, entries.ToArray());
                                }
                                catch (InvalidOperationException) when (queryShouldThrowOnChange)
                                {
                                    // expected
                                }
                                catch (Exception exception)
                                {
                                    Fault(testTree, "Enumerator threw unexpected exception", exception);
                                }
                            }
                        }

                        // test enumerator's SetValue

                        PropertyInfo getValueProperty = typeof(EntryType).GetProperty("Value");
                        MethodInfo setValueMethod = typeof(EntryType).GetMethod("SetValue");
                        if (setValueMethod != null)
                        {
                            for (int e = 0; e < mode.Length; e++)
                            {
                                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                foreach (int index in keySequence)
                                {
                                    addAction(testTree, master[index].key, master[index].value, master[index].xLength);
                                }

                                long startIteration1 = IncrementIteration(true/*setLast*/);
                                try
                                {
                                    int c = 0;
                                    List<BigEntry<int, float>> entries = new List<BigEntry<int, float>>();
                                    foreach (EntryType entry in mode[e])
                                    {
                                        TestTrue("enum overrun", delegate () { return c < master.Length; });

                                        BigEntry<int, float> bigEntry = new BigEntry<int, float>(entry, treeKind);
                                        entries.Add(bigEntry);

                                        setValueMethod.Invoke(entry, new object[] { bigEntry.value + .5f });

                                        c++;
                                    }
                                    TestTrue("enum count", delegate () { return c == master.Length; });
                                    if (!forward)
                                    {
                                        entries.Reverse();
                                    }
                                    Validate(master, entries.ToArray());

                                    BigEntry<int, float>[] updatedMaster = (BigEntry<int, float>[])master.Clone();
                                    for (int i = 0; i < updatedMaster.Length; i++)
                                    {
                                        updatedMaster[i] = new BigEntry<int, float>(
                                            updatedMaster[i].key,
                                            updatedMaster[i].value + .5f,
                                            updatedMaster[i].xStart,
                                            updatedMaster[i].xLength,
                                            updatedMaster[i].yStart,
                                            updatedMaster[i].yLength);
                                    }

                                    c = 0;
                                    entries.Clear();
                                    foreach (EntryType entry in mode[e])
                                    {
                                        TestTrue("enum overrun", delegate () { return c < master.Length; });

                                        BigEntry<int, float> bigEntry = new BigEntry<int, float>(entry, treeKind);
                                        entries.Add(bigEntry);

                                        c++;
                                    }
                                    TestTrue("enum count", delegate () { return c == master.Length; });
                                    if (!forward)
                                    {
                                        entries.Reverse();
                                    }
                                    Validate(updatedMaster, entries.ToArray());
                                }
                                catch (UnitTestFailureException)
                                {
                                    throw;
                                }
                                catch (Exception exception)
                                {
                                    Fault(testTree, "Enumerator threw unexpected exception", exception);
                                }


                                if (size >= 2)
                                {
                                    // try update on enumerator after enumerator has advanced

                                    {
                                        long startIteration2 = IncrementIteration(true/*setLast*/);

                                        testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                        foreach (int index in keySequence)
                                        {
                                            addAction(testTree, master[index].key, master[index].value, master[index].xLength);
                                        }

                                        IEnumerator<EntryType> enumerator = mode[e].GetEnumerator();
                                        TestTrue("enumerator", delegate () { return enumerator.MoveNext(); });
                                        EntryType entry = default(EntryType);
                                        TestNoThrow("enumerator", delegate () { entry = enumerator.Current; });
                                        int masterIndex = forward ? 0 : master.Length - 1;
                                        TestTrue("enumerator", delegate () { return master[masterIndex].value == (float)getValueProperty.GetValue(entry); });
                                        TestNoThrow("enumerator", delegate () { setValueMethod.Invoke(entry, new object[] { 1f }); });
                                        TestNoThrow("enumerator", delegate () { setValueMethod.Invoke(entry, new object[] { 2f }); });
                                        TestTrue("enumerator", delegate () { return enumerator.MoveNext(); });
                                        TestThrow("enumerator", typeof(InvalidOperationException), delegate () { setValueMethod.Invoke(entry, new object[] { 3f }); });
                                    }

                                    // try update on enumerator after tree was modified

                                    if (robust)
                                    {
                                        long startIteration2 = IncrementIteration(true/*setLast*/);

                                        testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                        foreach (int index in keySequence)
                                        {
                                            addAction(testTree, master[index].key, master[index].value, master[index].xLength);
                                        }

                                        IEnumerator<EntryType> enumerator = mode[e].GetEnumerator();
                                        TestTrue("enumerator", delegate () { return enumerator.MoveNext(); });
                                        EntryType entry = default(EntryType);
                                        TestNoThrow("enumerator", delegate () { entry = enumerator.Current; });
                                        int masterIndex = forward ? 0 : master.Length - 1;
                                        TestTrue("enumerator", delegate () { return master[masterIndex].value == (float)getValueProperty.GetValue(entry); });
                                        addAction(testTree, -100, 0f, 1);
                                        if (robust)
                                        {
                                            TestNoThrow("enumerator", delegate () { setValueMethod.Invoke(entry, new object[] { 3f }); });
                                        }
                                        else
                                        {
                                            TestThrow("enumerator", typeof(InvalidOperationException), delegate () { setValueMethod.Invoke(entry, new object[] { 3f }); });
                                        }
                                        TestTrue("enumerator", delegate () { return enumerator.MoveNext(); }); // sanity check
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        //
        // RangeMap & RangeList
        //

        private delegate void IndexedInsertAction(object testTree, int xStart, int xLength, int yLength, float value);
        private delegate void IndexedDeleteAction(object testTree, int xStart);

        //

        private static void Range2MapInsert<ValueType>(object testTree, int start, int xLength, int yLength, ValueType value)
        {
            ((IRange2Map<ValueType>)testTree).Insert(start, Side.X, xLength, yLength, value);
        }

        private static void Range2MapDelete<ValueType>(object testTree, int start)
        {
            ((IRange2Map<ValueType>)testTree).Delete(start, Side.X);
        }

        private static void Range2ListInsert<ValueType>(object testTree, int start, int xLength, int yLength, ValueType value)
        {
            ((IRange2List)testTree).Insert(start, Side.X, xLength, yLength);
        }

        private static void Range2ListDelete<ValueType>(object testTree, int start)
        {
            ((IRange2List)testTree).Delete(start, Side.X);
        }

        //

        private static void RangeMapInsert<ValueType>(object testTree, int start, int xLength, int yLength, ValueType value)
        {
            ((IRangeMap<ValueType>)testTree).Insert(start, xLength, value);
        }

        private static void RangeMapDelete<ValueType>(object testTree, int start)
        {
            ((IRangeMap<ValueType>)testTree).Delete(start);
        }

        private static void RangeListInsert<ValueType>(object testTree, int start, int xLength, int yLength, ValueType value)
        {
            ((IRangeList)testTree).Insert(start, xLength);
        }

        private static void RangeListDelete<ValueType>(object testTree, int start)
        {
            ((IRangeList)testTree).Delete(start);
        }

        //

        private void TestRangeMap<EntryType>(
            object testTree,
            TreeKind treeKind,
            IndexedInsertAction insertAction,
            IndexedDeleteAction deleteAction)
        {
            long startIteration = IncrementIteration();

            // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
            // sequence of keys (with uniform distribution). The unit test will use the same set of keys and therefore the
            // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
            ParkAndMiller random = new ParkAndMiller(Seed);

            const int KeyIncrement = 100;
            Debug.Assert(KeyIncrement % 2 == 0);

            bool valueValid = (treeKind & TreeKind.AllValued) != 0;
            bool range2Valid = (treeKind & TreeKind.AllIndexed2) != 0;

            EnumKind indexedEnumerable = range2Valid ? EnumKind.Indexed2Enumerable : EnumKind.IndexedEnumerable;
            EnumKind indexedEnumerableFast = range2Valid ? EnumKind.Indexed2EnumerableFast : EnumKind.IndexedEnumerableFast;
            EnumKind indexedEnumerableRobust = range2Valid ? EnumKind.Indexed2EnumerableRobust : EnumKind.IndexedEnumerableRobust;
            EnumKind indexedEnumerableBidir = range2Valid ? EnumKind.Indexed2EnumerableBidir : EnumKind.IndexedEnumerableBidir;
            EnumKind indexedEnumerableFastBidir = range2Valid ? EnumKind.Indexed2EnumerableFastBidir : EnumKind.IndexedEnumerableFastBidir;
            EnumKind indexedEnumerableRobustBidir = range2Valid ? EnumKind.Indexed2EnumerableRobustBidir : EnumKind.IndexedEnumerableRobustBidir;

            foreach (int size in TestCaseSizes)
            {
                // build benchmark

                BigEntry<int, float>[] master = new BigEntry<int, float>[size];
                for (int i = 0; i < master.Length; i++)
                {
                    master[i] = new BigEntry<int, float>(
                        0,
                        valueValid ? i * .1f : 0f,
                        0,
                        random.Next() % 100 + 50,
                        0,
                        range2Valid ? random.Next() % 100 + 50 : 0);
                }
                RecalcStarts(master);
                int xExtent = master.Length != 0 ? master[master.Length - 1].xStart + master[master.Length - 1].xLength : 0;
                int yExtent = master.Length != 0 ? master[master.Length - 1].yStart + master[master.Length - 1].yLength : 0;

                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                for (int i = 0; i < master.Length; i++)
                {
                    insertAction(testTree, master[i].xStart, master[i].xLength, master[i].yLength, master[i].value);
                }


                // test basic enumeration

                {
                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.DefaultOld, treeKind, new EnumArgsProvider(), null/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.Enumerable, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), true/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), true/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                    };

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (EntryType entry in enumerators[e])
                        {
                            TestTrue("enum overrun", delegate () { return c < master.Length; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == master.Length; });
                        Validate(master, testEntries.ToArray());
                    }
                }


                // test reverse enumeration

                {
                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    BigEntry<int, float>[] reverseMaster = (BigEntry<int, float>[])master.Clone();
                    Array.Reverse(reverseMaster);

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (EntryType entry in enumerators[e])
                        {
                            TestTrue("enum overrun", delegate () { return c < reverseMaster.Length; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == reverseMaster.Length; });
                        Validate(reverseMaster, testEntries.ToArray());
                    }
                }


                // test basic enumeration on non-generic interface
                {
                    IEnumerable[] enumerators = new IEnumerable[]
                    {
                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/)),
                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.DefaultOld, treeKind, new EnumArgsProvider(), null/*forward*/)),

                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.Enumerable, treeKind, new EnumArgsProvider(), null/*forward*/)),

                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/)),

                        new OldEnumerableReverse<EntryType>(GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/)),
                    };

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                        int c = 0;
                        foreach (object o in enumerators[e])
                        {
                            EntryType entry = (EntryType)o;
                            TestTrue("enum overrun", delegate () { return c < master.Length; });
                            testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                            c++;
                        }
                        TestTrue("enum count", delegate () { return c == master.Length; });
                        Validate(master, testEntries.ToArray());
                    }
                }


                // index-constrained

                if (size != 0)
                {
                    foreach (Side side in range2Valid ? new Side[] { Side.X, Side.Y } : new Side[] { Side.X })
                    {
                        for (int testKeyCounter = 0; testKeyCounter < size * 2 + 1; testKeyCounter++)
                        {
                            int testStart;
                            if (testKeyCounter == 0)
                            {
                                testStart = side == Side.X ? master[0].xStart - 1 : master[0].yStart - 1;
                            }
                            else if (testKeyCounter < size * 2)
                            {
                                if (testKeyCounter % 2 == 1)
                                {
                                    testStart = side == Side.X ? master[testKeyCounter / 2].xStart : master[testKeyCounter / 2].yStart;
                                }
                                else
                                {
                                    testStart = side == Side.X
                                        ? (master[testKeyCounter / 2 - 1].xStart + master[testKeyCounter / 2].xStart) / 2
                                        : (master[testKeyCounter / 2 - 1].yStart + master[testKeyCounter / 2].yStart) / 2;
                                }
                            }
                            else
                            {
                                testStart = side == Side.X ? master[master.Length - 1].xStart + 1 : master[master.Length - 1].yStart + 1;
                            }
                            long startIteration2 = IncrementIteration(true/*setLast*/);

                            EnumArgsProvider argsProvider = range2Valid
                                ? (EnumArgsProvider)new EnumIndexed2ArgsProvider(testStart, side)
                                : (EnumArgsProvider)new EnumIndexedArgsProvider(testStart);


                            // test index-constrained forward enumeration

                            {
                                IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                                {
                                    GetEnumerator<EntryType>(testTree, indexedEnumerable, treeKind, argsProvider, null/*forward*/),
                                    GetEnumerator<EntryType>(testTree, indexedEnumerableBidir, treeKind, argsProvider, true/*forward*/),

                                    GetEnumerator<EntryType>(testTree, indexedEnumerableFast, treeKind, argsProvider, null/*forward*/),
                                    GetEnumerator<EntryType>(testTree, indexedEnumerableFastBidir, treeKind, argsProvider, true/*forward*/),

                                    GetEnumerator<EntryType>(testTree, indexedEnumerableRobust, treeKind, argsProvider, null/*forward*/),
                                    GetEnumerator<EntryType>(testTree, indexedEnumerableRobustBidir, treeKind, argsProvider, true/*forward*/),
                                };

                                List<BigEntry<int, float>> filteredMaster = new List<BigEntry<int, float>>(master);
                                filteredMaster.RemoveAll(delegate (BigEntry<int, float> candidate)
                                    { return (side == Side.X ? candidate.xStart : candidate.yStart) < testStart; });

                                for (int e = 0; e < enumerators.Length; e++)
                                {
                                    long startIteration1 = IncrementIteration(true/*setLast*/);

                                    List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                                    int c = 0;
                                    foreach (EntryType entry in enumerators[e])
                                    {
                                        TestTrue("enum overrun", delegate () { return c < filteredMaster.Count; });
                                        testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                                        c++;
                                    }
                                    TestTrue("enum count", delegate () { return c == filteredMaster.Count; });
                                    Validate(filteredMaster.ToArray(), testEntries.ToArray());
                                }
                            }


                            // test key-constrained reverse enumeration

                            {
                                IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                                {
                                    GetEnumerator<EntryType>(testTree, indexedEnumerableBidir, treeKind, argsProvider, false/*forward*/),

                                    GetEnumerator<EntryType>(testTree, indexedEnumerableFastBidir, treeKind, argsProvider, false/*forward*/),

                                    GetEnumerator<EntryType>(testTree, indexedEnumerableRobustBidir, treeKind, argsProvider, false/*forward*/),
                                };

                                List<BigEntry<int, float>> filteredReversedMaster = new List<BigEntry<int, float>>(master);
                                filteredReversedMaster.RemoveAll(delegate (BigEntry<int, float> candidate)
                                    { return (side == Side.X ? candidate.xStart : candidate.yStart) > testStart; });
                                filteredReversedMaster.Reverse();

                                for (int e = 0; e < enumerators.Length; e++)
                                {
                                    long startIteration1 = IncrementIteration(true/*setLast*/);

                                    List<BigEntry<int, float>> testEntries = new List<BigEntry<int, float>>();
                                    int c = 0;
                                    foreach (EntryType entry in enumerators[e])
                                    {
                                        TestTrue("enum overrun", delegate () { return c < filteredReversedMaster.Count; });
                                        testEntries.Add(new BigEntry<int, float>(entry, treeKind));
                                        c++;
                                    }
                                    TestTrue("enum count", delegate () { return c == filteredReversedMaster.Count; });
                                    Validate(filteredReversedMaster.ToArray(), testEntries.ToArray());
                                }
                            }
                        }
                    }
                }


                // test boundary cases (non-keyed cases only)

                {
                    IEnumerable<EntryType>[] enumerators = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.Default, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.DefaultOld, treeKind, new EnumArgsProvider(), null/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.Enumerable, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), false/*forward*/),

                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    for (int e = 0; e < enumerators.Length; e++)
                    {
                        long startIteration1 = IncrementIteration(true/*setLast*/);

                        IEnumerator<EntryType> enumerator = enumerators[e].GetEnumerator();

                        TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                        int c = 0;
                        while (enumerator.MoveNext())
                        {
                            TestNoThrow("enumeration", delegate () { string text = enumerator.Current.ToString(); });
                            TestNoThrow("enumeration", delegate () { int hash = enumerator.Current.GetHashCode(); });

                            TestTrue("enumeration", delegate () { return enumerator.Current.Equals(enumerator.Current); });

                            c++;
                        }
                        TestTrue("enumeration", delegate () { return c == master.Length; });

                        TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                        TestFalse("enumeration", delegate () { return enumerator.MoveNext(); }); // extra MoveNext after termination
                    }
                }


                // test interrupting enumerator with tree modifications, and while we're at it, Clear()
                // (non-keyed cases only)

                if (size != 0)
                {
                    IEnumerable<EntryType>[] enumeratorsFastForward = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFast, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                    };

                    IEnumerable<EntryType>[] enumeratorsFastReverse = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableFastBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    IEnumerable<EntryType>[] enumeratorsRobustForward = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobust, treeKind, new EnumArgsProvider(), null/*forward*/),
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), true/*forward*/),
                    };

                    IEnumerable<EntryType>[] enumeratorsRobustReverse = new IEnumerable<EntryType>[]
                    {
                        GetEnumerator<EntryType>(testTree, EnumKind.EnumerableRobustBidir, treeKind, new EnumArgsProvider(), false/*forward*/),
                    };

                    foreach (IEnumerable<EntryType>[] mode in new IEnumerable<EntryType>[][]
                        { enumeratorsFastForward, enumeratorsFastReverse, enumeratorsRobustForward, enumeratorsRobustReverse })
                    {
                        // FRAGILE:
                        bool forward = (mode == enumeratorsFastForward) || (mode == enumeratorsRobustForward);
                        bool robust = (mode == enumeratorsRobustForward) || (mode == enumeratorsRobustReverse);

                        // test add during enumeration

                        int actionPoint = size / 2;
                        for (int insertIndex = 0; insertIndex <= size; insertIndex++)
                        {
                            for (int e = 0; e < mode.Length; e++)
                            {
                                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                for (int i = 0; i < master.Length; i++)
                                {
                                    insertAction(testTree, master[i].xStart, master[i].xLength, master[i].yLength, master[i].value);
                                }

                                long startIteration1 = IncrementIteration(true/*setLast*/);
                                try
                                {
                                    int c = 0;
                                    bool added = false;
                                    foreach (EntryType entry in mode[e])
                                    {
                                        if (added)
                                        {
                                            Fault(testTree, "Enumerator was expected to throw and didn't");
                                        }

                                        TestTrue("enum overrun", delegate () { return c < master.Length + 1; });

                                        if (c == actionPoint)
                                        {
                                            added = true;
                                            TestNoThrow("enum-add", delegate () { insertAction(testTree, insertIndex < master.Length ? master[insertIndex].xStart : master[insertIndex - 1].xStart + master[insertIndex - 1].xLength, 1, 1, default(float)); });
                                        }

                                        c++;
                                    }
                                    if (added)
                                    {
                                        Fault(testTree, "Enumerator was expected to throw and didn't");
                                    }
                                }
                                catch (InvalidOperationException)
                                {
                                    // expected
                                }
                                catch (Exception exception)
                                {
                                    Fault(testTree, "Enumerator threw unexpected exception", exception);
                                }
                            }
                        }

                        // test remove during enumeration

                        for (int deleteIndex = 0; deleteIndex < size; deleteIndex++)
                        {
                            for (int e = 0; e < mode.Length; e++)
                            {
                                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                for (int i = 0; i < master.Length; i++)
                                {
                                    insertAction(testTree, master[i].xStart, master[i].xLength, master[i].yLength, master[i].value);
                                }

                                long startIteration1 = IncrementIteration(true/*setLast*/);
                                try
                                {
                                    int c = 0;
                                    bool removed = false;
                                    foreach (EntryType entry in mode[e])
                                    {
                                        if (removed)
                                        {
                                            Fault(testTree, "Enumerator was expected to throw and didn't");
                                        }

                                        TestTrue("enum overrun", delegate () { return c < master.Length; });

                                        if (c == actionPoint)
                                        {
                                            removed = true;
                                            TestNoThrow("enum-delete", delegate () { deleteAction(testTree, master[deleteIndex].xStart); });
                                        }

                                        c++;
                                    }
                                    if (removed)
                                    {
                                        Fault(testTree, "Enumerator was expected to throw and didn't");
                                    }
                                }
                                catch (InvalidOperationException)
                                {
                                    // expected
                                }
                                catch (Exception exception)
                                {
                                    Fault(testTree, "Enumerator threw unexpected exception", exception);
                                }
                            }
                        }


                        // test enumerator's SetValue

                        PropertyInfo getValueProperty = typeof(EntryType).GetProperty("Value");
                        MethodInfo setValueMethod = typeof(EntryType).GetMethod("SetValue");
                        if (setValueMethod != null)
                        {
                            for (int e = 0; e < mode.Length; e++)
                            {
                                testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                for (int i = 0; i < master.Length; i++)
                                {
                                    insertAction(testTree, master[i].xStart, master[i].xLength, master[i].yLength, master[i].value);
                                }

                                long startIteration1 = IncrementIteration(true/*setLast*/);
                                try
                                {
                                    int c = 0;
                                    List<BigEntry<int, float>> entries = new List<BigEntry<int, float>>();
                                    foreach (EntryType entry in mode[e])
                                    {
                                        TestTrue("enum overrun", delegate () { return c < master.Length; });

                                        BigEntry<int, float> bigEntry = new BigEntry<int, float>(entry, treeKind);
                                        entries.Add(bigEntry);

                                        setValueMethod.Invoke(entry, new object[] { bigEntry.value + .5f });

                                        c++;
                                    }
                                    TestTrue("enum count", delegate () { return c == master.Length; });
                                    if (!forward)
                                    {
                                        entries.Reverse();
                                    }
                                    Validate(master, entries.ToArray());

                                    BigEntry<int, float>[] updatedMaster = (BigEntry<int, float>[])master.Clone();
                                    for (int i = 0; i < updatedMaster.Length; i++)
                                    {
                                        updatedMaster[i] = new BigEntry<int, float>(
                                            updatedMaster[i].key,
                                            updatedMaster[i].value + .5f,
                                            updatedMaster[i].xStart,
                                            updatedMaster[i].xLength,
                                            updatedMaster[i].yStart,
                                            updatedMaster[i].yLength);
                                    }

                                    c = 0;
                                    entries.Clear();
                                    foreach (EntryType entry in mode[e])
                                    {
                                        TestTrue("enum overrun", delegate () { return c < master.Length; });

                                        BigEntry<int, float> bigEntry = new BigEntry<int, float>(entry, treeKind);
                                        entries.Add(bigEntry);

                                        c++;
                                    }
                                    TestTrue("enum count", delegate () { return c == master.Length; });
                                    if (!forward)
                                    {
                                        entries.Reverse();
                                    }
                                    Validate(updatedMaster, entries.ToArray());
                                }
                                catch (UnitTestFailureException)
                                {
                                    throw;
                                }
                                catch (Exception exception)
                                {
                                    Fault(testTree, "Enumerator threw unexpected exception", exception);
                                }


                                if (size >= 2)
                                {
                                    // try update on enumerator after enumerator has advanced

                                    {
                                        long startIteration2 = IncrementIteration(true/*setLast*/);

                                        testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                        for (int i = 0; i < master.Length; i++)
                                        {
                                            insertAction(testTree, master[i].xStart, master[i].xLength, master[i].yLength, master[i].value);
                                        }

                                        IEnumerator<EntryType> enumerator = mode[e].GetEnumerator();
                                        TestTrue("enumerator", delegate () { return enumerator.MoveNext(); });
                                        EntryType entry = default(EntryType);
                                        TestNoThrow("enumerator", delegate () { entry = enumerator.Current; });
                                        int masterIndex = forward ? 0 : master.Length - 1;
                                        TestTrue("enumerator", delegate () { return master[masterIndex].value == (float)getValueProperty.GetValue(entry); });
                                        TestNoThrow("enumerator", delegate () { setValueMethod.Invoke(entry, new object[] { 1f }); });
                                        TestNoThrow("enumerator", delegate () { setValueMethod.Invoke(entry, new object[] { 2f }); });
                                        TestTrue("enumerator", delegate () { return enumerator.MoveNext(); });
                                        TestThrow("enumerator", typeof(InvalidOperationException), delegate () { setValueMethod.Invoke(entry, new object[] { 3f }); });
                                    }

                                    // try update on enumerator after tree was modified

                                    {
                                        long startIteration2 = IncrementIteration(true/*setLast*/);

                                        testTree.GetType().InvokeMember("Clear", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, testTree, null);
                                        for (int i = 0; i < master.Length; i++)
                                        {
                                            insertAction(testTree, master[i].xStart, master[i].xLength, master[i].yLength, master[i].value);
                                        }

                                        IEnumerator<EntryType> enumerator = mode[e].GetEnumerator();
                                        TestTrue("enumerator", delegate () { return enumerator.MoveNext(); });
                                        EntryType entry = default(EntryType);
                                        TestNoThrow("enumerator", delegate () { entry = enumerator.Current; });
                                        int masterIndex = forward ? 0 : master.Length - 1;
                                        TestTrue("enumerator", delegate () { return master[masterIndex].value == (float)getValueProperty.GetValue(entry); });
                                        insertAction(testTree, 0, 1, 1, 0f);
                                        TestThrow("enumerator", typeof(InvalidOperationException), delegate () { setValueMethod.Invoke(entry, new object[] { 3f }); });
                                        // because range maps don't permit insert/delete with enumeration
                                        TestThrow("enumerator", typeof(InvalidOperationException), delegate () { enumerator.MoveNext(); }); // sanity check
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        //
        // Entry equality and hashcode
        //

        private void TestEntryComparisons(Type entryType)
        {
            foreach (FieldInfo field in entryType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (String.Equals(field.Name, "enumerator") || String.Equals(field.Name, "version"))
                {
                    continue;
                }

                object first = null;
                TestNoThrow("entry", delegate () { first = Activator.CreateInstance(entryType); });
                object second = null;
                TestNoThrow("entry", delegate () { second = Activator.CreateInstance(entryType); });

                TestTrue("entry", delegate () { return first.Equals(second); });

                int hashFirst = Int32.MinValue;
                int hashSecond = Int32.MinValue;
                TestNoThrow("entry", delegate () { hashFirst = first.GetHashCode(); });
                TestNoThrow("entry", delegate () { hashSecond = second.GetHashCode(); });
                TestTrue("entry", delegate () { return hashFirst == hashSecond; });

                if (field.FieldType == typeof(int))
                {
                    field.SetValue(second, (int)field.GetValue(second) + 1);
                    TestFalse("entry", delegate () { return first.Equals(second); });
                    TestNoThrow("entry", delegate () { hashSecond = second.GetHashCode(); });
                    TestTrue("entry", delegate () { return hashFirst != hashSecond; });
                }
                else if (field.FieldType == typeof(long))
                {
                    field.SetValue(second, (long)field.GetValue(second) + 1);
                    TestFalse("entry", delegate () { return first.Equals(second); });
                    TestNoThrow("entry", delegate () { hashSecond = second.GetHashCode(); });
                    TestTrue("entry", delegate () { return hashFirst != hashSecond; });
                }
                else if (field.FieldType == typeof(float))
                {
                    field.SetValue(second, (float)field.GetValue(second) + 1);
                    TestFalse("entry", delegate () { return first.Equals(second); });
                    TestNoThrow("entry", delegate () { hashSecond = second.GetHashCode(); });
                    TestTrue("entry", delegate () { return hashFirst != hashSecond; });
                }
                else if (field.FieldType == typeof(string))
                {
                    field.SetValue(second, String.Concat((string)field.GetValue(second), "x"));
                    TestFalse("entry", delegate () { return first.Equals(second); });
                    TestNoThrow("entry", delegate () { hashSecond = second.GetHashCode(); });
                    TestTrue("entry", delegate () { return hashFirst != hashSecond; });
                }
                else
                {
                    Fault(entryType, "entry contains field of type that was not expected");
                }

                string toStringFirst = null;
                TestNoThrow("entry", delegate () { toStringFirst = first.ToString(); });
                TestTrue("entry", delegate () { return toStringFirst != null; });
                string toStringSecond = null;
                TestNoThrow("entry", delegate () { toStringSecond = second.ToString(); });
                TestTrue("entry", delegate () { return toStringSecond != null; });
                TestTrue("entry", delegate () { return !String.Equals(toStringFirst, toStringSecond); });
            }
        }


        //
        // main test
        //

        private const uint TreeCapacityForFixed = 200;

        private void TestReference()
        {
            // Map/List

            TestMap<EntryMap<int, float>>(new ReferenceMap<int, float>(), TreeKind.Map, MapKeyedAdd<int, float>, MapKeyedRemove<int, float>, MapKeyedContains<int, float>);


            // RankMap/RankList, MultiRankMap/MultiRankList

            TestMap<EntryRankMap<int, float>>(new ReferenceRankMap<int, float>(), TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);

            TestMap<EntryMultiRankMap<int, float>>(new ReferenceMultiRankMap<int, float>(), TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);


            // Range2Map/Range2List, RangeMap/RangeList

            TestRangeMap<EntryRange2Map<float>>(new ReferenceRange2Map<float>(), TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);

            TestRangeMap<EntryRange2List>(new ReferenceRange2List(), TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);

            TestRangeMap<EntryRangeMap<float>>(new ReferenceRangeMap<float>(), TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);

            TestRangeMap<EntryRangeList>(new ReferenceRangeList(), TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
        }

        private void TestEntryConstruction()
        {
            const int Key = 1;
            const float Value = 2.5f;
            const int XStart = 10;
            const int XLength = 15;
            const int YStart = 20;
            const int YLength = 25;


            {
                EntryMap<int, float> one = new EntryMap<int, float>(Key, Value);
                EntryMap<int, float> two = new EntryMap<int, float>(Key, Value, null/*enumerator*/, 0/*version*/);
                TestTrue("EntryMap", delegate () { return one.Equals(two); });
            }


            {
                EntryRankMap<int, float> one = new EntryRankMap<int, float>(Key, Value, XStart);
                EntryRankMap<int, float> two = new EntryRankMap<int, float>(Key, Value, null/*enumerator*/, 0/*version*/, XStart);
                TestTrue("EntryRankMap", delegate () { return one.Equals(two); });
            }

            {
                EntryRankMapLong<int, float> one = new EntryRankMapLong<int, float>(Key, Value, XStart);
                EntryRankMapLong<int, float> two = new EntryRankMapLong<int, float>(Key, Value, null/*enumerator*/, 0/*version*/, XStart);
                TestTrue("EntryRankMapLong", delegate () { return one.Equals(two); });
            }


            {
                EntryMultiRankMap<int, float> one = new EntryMultiRankMap<int, float>(Key, Value, XStart, XLength);
                EntryMultiRankMap<int, float> two = new EntryMultiRankMap<int, float>(Key, Value, null/*enumerator*/, 0/*version*/, XStart, XLength);
                TestTrue("EntryMultiRankMap", delegate () { return one.Equals(two); });
            }

            {
                EntryMultiRankMapLong<int, float> one = new EntryMultiRankMapLong<int, float>(Key, Value, XStart, XLength);
                EntryMultiRankMapLong<int, float> two = new EntryMultiRankMapLong<int, float>(Key, Value, null/*enumerator*/, 0/*version*/, XStart, XLength);
                TestTrue("EntryMultiRankMapLong", delegate () { return one.Equals(two); });
            }


            {
                EntryRangeMap<float> one = new EntryRangeMap<float>(Value, XStart, XLength);
                EntryRangeMap<float> two = new EntryRangeMap<float>(Value, null/*enumerator*/, 0/*version*/, XStart, XLength);
                TestTrue("EntryRangeMap", delegate () { return one.Equals(two); });
            }

            {
                EntryRangeMapLong<float> one = new EntryRangeMapLong<float>(Value, XStart, XLength);
                EntryRangeMapLong<float> two = new EntryRangeMapLong<float>(Value, null/*enumerator*/, 0/*version*/, XStart, XLength);
                TestTrue("EntryRangeMapLong", delegate () { return one.Equals(two); });
            }


            {
                EntryRange2Map<float> one = new EntryRange2Map<float>(Value, XStart, XLength, YStart, YLength);
                EntryRange2Map<float> two = new EntryRange2Map<float>(Value, null/*enumerator*/, 0/*version*/, XStart, XLength, YStart, YLength);
                TestTrue("EntryRange2Map", delegate () { return one.Equals(two); });
            }

            {
                EntryRange2MapLong<float> one = new EntryRange2MapLong<float>(Value, XStart, XLength, YStart, YLength);
                EntryRange2MapLong<float> two = new EntryRange2MapLong<float>(Value, null/*enumerator*/, 0/*version*/, XStart, XLength, YStart, YLength);
                TestTrue("EntryRange2MapLong", delegate () { return one.Equals(two); });
            }
        }

        private void TestSetValueValidation()
        {
            KeyValuePair<Type, bool>[] types = new KeyValuePair<Type, bool>[]
            {
                new KeyValuePair<Type, bool>(typeof(EntryList<int>), false),
                new KeyValuePair<Type, bool>(typeof(EntryMap<int, float>), true),

                new KeyValuePair<Type, bool>(typeof(EntryRankList<int>), false),
                new KeyValuePair<Type, bool>(typeof(EntryRankListLong<int>), false),
                new KeyValuePair<Type, bool>(typeof(EntryRankMap<int, float>), true),
                new KeyValuePair<Type, bool>(typeof(EntryRankMapLong<int, float>), true),

                new KeyValuePair<Type, bool>(typeof(EntryMultiRankList<int>), false),
                new KeyValuePair<Type, bool>(typeof(EntryMultiRankListLong<int>), false),
                new KeyValuePair<Type, bool>(typeof(EntryMultiRankMap<int, float>), true),
                new KeyValuePair<Type, bool>(typeof(EntryMultiRankMapLong<int, float>), true),

                new KeyValuePair<Type, bool>(typeof(EntryRangeList), false),
                new KeyValuePair<Type, bool>(typeof(EntryRangeListLong), false),
                new KeyValuePair<Type, bool>(typeof(EntryRangeMap<float>), true),
                new KeyValuePair<Type, bool>(typeof(EntryRangeMapLong<float>), true),

                new KeyValuePair<Type, bool>(typeof(EntryRange2List), false),
                new KeyValuePair<Type, bool>(typeof(EntryRange2ListLong), false),
                new KeyValuePair<Type, bool>(typeof(EntryRange2Map<float>), true),
                new KeyValuePair<Type, bool>(typeof(EntryRange2MapLong<float>), true),
            };

            foreach (KeyValuePair<Type, bool> info in types)
            {
                object entry = Activator.CreateInstance(info.Key);
                MethodInfo setValueInfo = entry.GetType().GetMethod("SetValue");
                TestTrue("SetValue exists", delegate () { return info.Value == (setValueInfo != null); });
                if (setValueInfo != null)
                {
                    TestThrow("SetValue", typeof(InvalidOperationException), delegate () { setValueInfo.Invoke(entry, new object[] { 1f }); });

                    IGetEnumeratorSetValueInfo<float> accessor = (IGetEnumeratorSetValueInfo<float>)entry;
                    TestTrue("Version", delegate () { return accessor.Version == 0; });
                    TestTrue("SetValueCallack ", delegate () { return accessor.SetValueCallack == null; });
                }
            }
        }

        private void TestSplayTree()
        {
            // Exercise all allocation modes to gain coverage of both code paths in the Clear() method.
            foreach (AllocationMode allocationMode in new AllocationMode[] { AllocationMode.DynamicDiscard, AllocationMode.DynamicRetainFreelist, AllocationMode.PreallocatedFixed })
            {
                uint capacity = allocationMode == AllocationMode.PreallocatedFixed ? TreeCapacityForFixed : 0;

                //
                // Map
                //

                TestMap<EntryMap<int, float>>(new SplayTreeMap<int, float>(capacity, allocationMode), TreeKind.Splay | TreeKind.Map, MapKeyedAdd<int, float>, MapKeyedRemove<int, float>, MapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMap<int, float>>(new SplayTreeArrayMap<int, float>(capacity, allocationMode), TreeKind.Splay | TreeKind.Map, MapKeyedAdd<int, float>, MapKeyedRemove<int, float>, MapKeyedContains<int, float>);
                }

                //
                // List
                //

                TestMap<EntryList<int>>(new SplayTreeList<int>(capacity, allocationMode), TreeKind.Splay | TreeKind.List, ListKeyedAdd<int, float>, ListKeyedRemove<int, float>, ListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryList<int>>(new SplayTreeArrayList<int>(capacity, allocationMode), TreeKind.Splay | TreeKind.List, ListKeyedAdd<int, float>, ListKeyedRemove<int, float>, ListKeyedContains<int, float>);
                }


                //
                // RankMap
                //

                // Int32

                TestMap<EntryRankMap<int, float>>(new SplayTreeRankMap<int, float>(capacity, allocationMode), TreeKind.Splay | TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryRankMap<int, float>>(new SplayTreeArrayRankMap<int, float>(capacity, allocationMode), TreeKind.Splay | TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryRankMap<int, float>>(new AdaptRankMapToRankMapLong<int, float>(new SplayTreeRankMapLong<int, float>(capacity, allocationMode)), TreeKind.Splay | TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);


                //
                // RankList
                //

                TestMap<EntryRankList<int>>(new SplayTreeRankList<int>(capacity, allocationMode), TreeKind.Splay | TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryRankList<int>>(new SplayTreeArrayRankList<int>(capacity, allocationMode), TreeKind.Splay | TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryRankList<int>>(new AdaptRankListToRankListLong<int>(new SplayTreeRankListLong<int>(capacity, allocationMode)), TreeKind.Splay | TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);


                //
                // MultiRankMap
                //

                // Int32

                TestMap<EntryMultiRankMap<int, float>>(new SplayTreeMultiRankMap<int, float>(capacity, allocationMode), TreeKind.Splay | TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMultiRankMap<int, float>>(new SplayTreeArrayMultiRankMap<int, float>(capacity, allocationMode), TreeKind.Splay | TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryMultiRankMap<int, float>>(new AdaptMultiRankMapToMultiRankMapLong<int, float>(new SplayTreeMultiRankMapLong<int, float>(capacity, allocationMode)), TreeKind.Splay | TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);


                //
                // MultiRankList
                //

                // Int32

                TestMap<EntryMultiRankList<int>>(new SplayTreeMultiRankList<int>(capacity, allocationMode), TreeKind.Splay | TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMultiRankList<int>>(new SplayTreeArrayMultiRankList<int>(capacity, allocationMode), TreeKind.Splay | TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryMultiRankList<int>>(new AdaptMultiRankListToMultiRankListLong<int>(new SplayTreeMultiRankListLong<int>(capacity, allocationMode)), TreeKind.Splay | TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);


                //
                // Range2Map
                //

                // Int32

                TestRangeMap<EntryRange2Map<float>>(new SplayTreeRange2Map<float>(capacity, allocationMode), TreeKind.Splay | TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRange2Map<float>>(new SplayTreeArrayRange2Map<float>(capacity, allocationMode), TreeKind.Splay | TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);
                }

                // Long

                TestRangeMap<EntryRange2Map<float>>(new AdaptRange2MapToRange2MapLong<float>(new SplayTreeRange2MapLong<float>(capacity, allocationMode)), TreeKind.Splay | TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);


                //
                // Range2List
                //

                // Int32

                TestRangeMap<EntryRange2List>(new SplayTreeRange2List(capacity, allocationMode), TreeKind.Splay | TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRange2List>(new SplayTreeArrayRange2List(capacity, allocationMode), TreeKind.Splay | TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);
                }

                // Long

                TestRangeMap<EntryRange2List>(new AdaptRange2ListToRange2ListLong(new SplayTreeRange2ListLong(capacity, allocationMode)), TreeKind.Splay | TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);


                //
                // RangeMap
                //

                // Int32

                TestRangeMap<EntryRangeMap<float>>(new SplayTreeRangeMap<float>(capacity, allocationMode), TreeKind.Splay | TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRangeMap<float>>(new SplayTreeArrayRangeMap<float>(capacity, allocationMode), TreeKind.Splay | TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);
                }

                // Long

                TestRangeMap<EntryRangeMap<float>>(new AdaptRangeMapToRangeMapLong<float>(new SplayTreeRangeMapLong<float>(capacity, allocationMode)), TreeKind.Splay | TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);


                //
                // RangeList
                //

                // Int32

                TestRangeMap<EntryRangeList>(new SplayTreeRangeList(capacity, allocationMode), TreeKind.Splay | TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRangeList>(new SplayTreeArrayRangeList(capacity, allocationMode), TreeKind.Splay | TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
                }

                // Long

                TestRangeMap<EntryRangeList>(new AdaptRangeListToRangeListLong(new SplayTreeRangeListLong(capacity, allocationMode)), TreeKind.Splay | TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
            }
        }

        private void TestRedBlackTree()
        {
            // Exercise all allocation modes to gain coverage of both code paths in the Clear() method.
            foreach (AllocationMode allocationMode in new AllocationMode[] { AllocationMode.DynamicDiscard, AllocationMode.DynamicRetainFreelist, AllocationMode.PreallocatedFixed })
            {
                uint capacity = allocationMode == AllocationMode.PreallocatedFixed ? TreeCapacityForFixed : 0;

                //
                // Map
                //

                TestMap<EntryMap<int, float>>(new RedBlackTreeMap<int, float>(capacity, allocationMode), TreeKind.Map, MapKeyedAdd<int, float>, MapKeyedRemove<int, float>, MapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMap<int, float>>(new RedBlackTreeArrayMap<int, float>(capacity, allocationMode), TreeKind.Map, MapKeyedAdd<int, float>, MapKeyedRemove<int, float>, MapKeyedContains<int, float>);
                }

                //
                // List
                //

                TestMap<EntryList<int>>(new RedBlackTreeList<int>(capacity, allocationMode), TreeKind.List, ListKeyedAdd<int, float>, ListKeyedRemove<int, float>, ListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryList<int>>(new RedBlackTreeArrayList<int>(capacity, allocationMode), TreeKind.List, ListKeyedAdd<int, float>, ListKeyedRemove<int, float>, ListKeyedContains<int, float>);
                }


                //
                // RankMap
                //

                // Int32

                TestMap<EntryRankMap<int, float>>(new RedBlackTreeRankMap<int, float>(capacity, allocationMode), TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryRankMap<int, float>>(new RedBlackTreeArrayRankMap<int, float>(capacity, allocationMode), TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryRankMap<int, float>>(new AdaptRankMapToRankMapLong<int, float>(new RedBlackTreeRankMapLong<int, float>(capacity, allocationMode)), TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);


                //
                // RankList
                //

                TestMap<EntryRankList<int>>(new RedBlackTreeRankList<int>(capacity, allocationMode), TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryRankList<int>>(new RedBlackTreeArrayRankList<int>(capacity, allocationMode), TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryRankList<int>>(new AdaptRankListToRankListLong<int>(new RedBlackTreeRankListLong<int>(capacity, allocationMode)), TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);


                //
                // MultiRankMap
                //

                // Int32

                TestMap<EntryMultiRankMap<int, float>>(new RedBlackTreeMultiRankMap<int, float>(capacity, allocationMode), TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMultiRankMap<int, float>>(new RedBlackTreeArrayMultiRankMap<int, float>(capacity, allocationMode), TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryMultiRankMap<int, float>>(new AdaptMultiRankMapToMultiRankMapLong<int, float>(new RedBlackTreeMultiRankMapLong<int, float>(capacity, allocationMode)), TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);


                //
                // MultiRankList
                //

                // Int32

                TestMap<EntryMultiRankList<int>>(new RedBlackTreeMultiRankList<int>(capacity, allocationMode), TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMultiRankList<int>>(new RedBlackTreeArrayMultiRankList<int>(capacity, allocationMode), TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryMultiRankList<int>>(new AdaptMultiRankListToMultiRankListLong<int>(new RedBlackTreeMultiRankListLong<int>(capacity, allocationMode)), TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);


                //
                // Range2Map
                //

                // Int32

                TestRangeMap<EntryRange2Map<float>>(new RedBlackTreeRange2Map<float>(capacity, allocationMode), TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRange2Map<float>>(new RedBlackTreeArrayRange2Map<float>(capacity, allocationMode), TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);
                }

                // Long

                TestRangeMap<EntryRange2Map<float>>(new AdaptRange2MapToRange2MapLong<float>(new RedBlackTreeRange2MapLong<float>(capacity, allocationMode)), TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);


                //
                // Range2List
                //

                // Int32

                TestRangeMap<EntryRange2List>(new RedBlackTreeRange2List(capacity, allocationMode), TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRange2List>(new RedBlackTreeArrayRange2List(capacity, allocationMode), TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);
                }

                // Long

                TestRangeMap<EntryRange2List>(new AdaptRange2ListToRange2ListLong(new RedBlackTreeRange2ListLong(capacity, allocationMode)), TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);


                //
                // RangeMap
                //

                // Int32

                TestRangeMap<EntryRangeMap<float>>(new RedBlackTreeRangeMap<float>(capacity, allocationMode), TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRangeMap<float>>(new RedBlackTreeArrayRangeMap<float>(capacity, allocationMode), TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);
                }

                // Long

                TestRangeMap<EntryRangeMap<float>>(new AdaptRangeMapToRangeMapLong<float>(new RedBlackTreeRangeMapLong<float>(capacity, allocationMode)), TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);


                //
                // RangeList
                //

                // Int32

                TestRangeMap<EntryRangeList>(new RedBlackTreeRangeList(capacity, allocationMode), TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRangeList>(new RedBlackTreeArrayRangeList(capacity, allocationMode), TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
                }

                // Long

                TestRangeMap<EntryRangeList>(new AdaptRangeListToRangeListLong(new RedBlackTreeRangeListLong(capacity, allocationMode)), TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
            }
        }

        private void TestAVLTree()
        {
            // Exercise all allocation modes to gain coverage of both code paths in the Clear() method.
            foreach (AllocationMode allocationMode in new AllocationMode[] { AllocationMode.DynamicDiscard, AllocationMode.DynamicRetainFreelist, AllocationMode.PreallocatedFixed })
            {
                uint capacity = allocationMode == AllocationMode.PreallocatedFixed ? TreeCapacityForFixed : 0;

                //
                // Map
                //

                TestMap<EntryMap<int, float>>(new AVLTreeMap<int, float>(capacity, allocationMode), TreeKind.Map, MapKeyedAdd<int, float>, MapKeyedRemove<int, float>, MapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMap<int, float>>(new AVLTreeArrayMap<int, float>(capacity, allocationMode), TreeKind.Map, MapKeyedAdd<int, float>, MapKeyedRemove<int, float>, MapKeyedContains<int, float>);
                }

                //
                // List
                //

                TestMap<EntryList<int>>(new AVLTreeList<int>(capacity, allocationMode), TreeKind.List, ListKeyedAdd<int, float>, ListKeyedRemove<int, float>, ListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryList<int>>(new AVLTreeArrayList<int>(capacity, allocationMode), TreeKind.List, ListKeyedAdd<int, float>, ListKeyedRemove<int, float>, ListKeyedContains<int, float>);
                }


                //
                // RankMap
                //

                // Int32

                TestMap<EntryRankMap<int, float>>(new AVLTreeRankMap<int, float>(capacity, allocationMode), TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryRankMap<int, float>>(new AVLTreeArrayRankMap<int, float>(capacity, allocationMode), TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryRankMap<int, float>>(new AdaptRankMapToRankMapLong<int, float>(new AVLTreeRankMapLong<int, float>(capacity, allocationMode)), TreeKind.RankMap, RankMapKeyedAdd<int, float>, RankMapKeyedRemove<int, float>, RankMapKeyedContains<int, float>);


                //
                // RankList
                //

                TestMap<EntryRankList<int>>(new AVLTreeRankList<int>(capacity, allocationMode), TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryRankList<int>>(new AVLTreeArrayRankList<int>(capacity, allocationMode), TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryRankList<int>>(new AdaptRankListToRankListLong<int>(new AVLTreeRankListLong<int>(capacity, allocationMode)), TreeKind.RankList, RankListKeyedAdd<int, float>, RankListKeyedRemove<int, float>, RankListKeyedContains<int, float>);


                //
                // MultiRankMap
                //

                // Int32

                TestMap<EntryMultiRankMap<int, float>>(new AVLTreeMultiRankMap<int, float>(capacity, allocationMode), TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMultiRankMap<int, float>>(new AVLTreeArrayMultiRankMap<int, float>(capacity, allocationMode), TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryMultiRankMap<int, float>>(new AdaptMultiRankMapToMultiRankMapLong<int, float>(new AVLTreeMultiRankMapLong<int, float>(capacity, allocationMode)), TreeKind.MultiRankMap, MultiRankMapKeyedAdd<int, float>, MultiRankMapKeyedRemove<int, float>, MultiRankMapKeyedContains<int, float>);


                //
                // MultiRankList
                //

                // Int32

                TestMap<EntryMultiRankList<int>>(new AVLTreeMultiRankList<int>(capacity, allocationMode), TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestMap<EntryMultiRankList<int>>(new AVLTreeArrayMultiRankList<int>(capacity, allocationMode), TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);
                }

                // Long

                TestMap<EntryMultiRankList<int>>(new AdaptMultiRankListToMultiRankListLong<int>(new AVLTreeMultiRankListLong<int>(capacity, allocationMode)), TreeKind.MultiRankList, MultiRankListKeyedAdd<int, float>, MultiRankListKeyedRemove<int, float>, MultiRankListKeyedContains<int, float>);


                //
                // Range2Map
                //

                // Int32

                TestRangeMap<EntryRange2Map<float>>(new AVLTreeRange2Map<float>(capacity, allocationMode), TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRange2Map<float>>(new AVLTreeArrayRange2Map<float>(capacity, allocationMode), TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);
                }

                // Long

                TestRangeMap<EntryRange2Map<float>>(new AdaptRange2MapToRange2MapLong<float>(new AVLTreeRange2MapLong<float>(capacity, allocationMode)), TreeKind.Range2Map, Range2MapInsert<float>, Range2MapDelete<float>);


                //
                // Range2List
                //

                // Int32

                TestRangeMap<EntryRange2List>(new AVLTreeRange2List(capacity, allocationMode), TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRange2List>(new AVLTreeArrayRange2List(capacity, allocationMode), TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);
                }

                // Long

                TestRangeMap<EntryRange2List>(new AdaptRange2ListToRange2ListLong(new AVLTreeRange2ListLong(capacity, allocationMode)), TreeKind.Range2List, Range2ListInsert<float>, Range2ListDelete<float>);


                //
                // RangeMap
                //

                // Int32

                TestRangeMap<EntryRangeMap<float>>(new AVLTreeRangeMap<float>(capacity, allocationMode), TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRangeMap<float>>(new AVLTreeArrayRangeMap<float>(capacity, allocationMode), TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);
                }

                // Long

                TestRangeMap<EntryRangeMap<float>>(new AdaptRangeMapToRangeMapLong<float>(new AVLTreeRangeMapLong<float>(capacity, allocationMode)), TreeKind.RangeMap, RangeMapInsert<float>, RangeMapDelete<float>);


                //
                // RangeList
                //

                // Int32

                TestRangeMap<EntryRangeList>(new AVLTreeRangeList(capacity, allocationMode), TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    TestRangeMap<EntryRangeList>(new AVLTreeArrayRangeList(capacity, allocationMode), TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
                }

                // Long

                TestRangeMap<EntryRangeList>(new AdaptRangeListToRangeListLong(new AVLTreeRangeListLong(capacity, allocationMode)), TreeKind.RangeList, RangeListInsert<float>, RangeListDelete<float>);
            }
        }

        private void TestAllEntryComparisons()
        {
            // test value types

            TestEntryComparisons(typeof(EntryList<int>));
            TestEntryComparisons(typeof(EntryMap<int, float>));

            TestEntryComparisons(typeof(EntryRankList<int>));
            TestEntryComparisons(typeof(EntryRankListLong<int>));
            TestEntryComparisons(typeof(EntryRankMap<int, float>));
            TestEntryComparisons(typeof(EntryRankMapLong<int, float>));

            TestEntryComparisons(typeof(EntryMultiRankList<int>));
            TestEntryComparisons(typeof(EntryMultiRankListLong<int>));
            TestEntryComparisons(typeof(EntryMultiRankMap<int, float>));
            TestEntryComparisons(typeof(EntryMultiRankMapLong<int, float>));

            TestEntryComparisons(typeof(EntryRangeList));
            TestEntryComparisons(typeof(EntryRangeListLong));
            TestEntryComparisons(typeof(EntryRangeMap<int>));
            TestEntryComparisons(typeof(EntryRangeMapLong<int>));

            TestEntryComparisons(typeof(EntryRange2List));
            TestEntryComparisons(typeof(EntryRange2ListLong));
            TestEntryComparisons(typeof(EntryRange2Map<int>));
            TestEntryComparisons(typeof(EntryRange2MapLong<int>));

            // test nullable types

            TestEntryComparisons(typeof(EntryList<string>));
            TestEntryComparisons(typeof(EntryMap<string, string>));

            TestEntryComparisons(typeof(EntryRankList<string>));
            TestEntryComparisons(typeof(EntryRankListLong<string>));
            TestEntryComparisons(typeof(EntryRankMap<string, string>));
            TestEntryComparisons(typeof(EntryRankMapLong<string, string>));

            TestEntryComparisons(typeof(EntryMultiRankList<string>));
            TestEntryComparisons(typeof(EntryMultiRankListLong<string>));
            TestEntryComparisons(typeof(EntryMultiRankMap<string, string>));
            TestEntryComparisons(typeof(EntryMultiRankMapLong<string, string>));

            //TestEntryComparisons(typeof(EntryRangeList)); - not applicable
            //TestEntryComparisons(typeof(EntryRangeListLong)); - not applicable
            TestEntryComparisons(typeof(EntryRangeMap<string>));
            TestEntryComparisons(typeof(EntryRangeMapLong<string>));

            //TestEntryComparisons(typeof(EntryRange2List)); - not applicable
            //TestEntryComparisons(typeof(EntryRange2ListLong)); - not applicable
            TestEntryComparisons(typeof(EntryRange2Map<string>));
            TestEntryComparisons(typeof(EntryRange2MapLong<string>));
        }

        public override bool Do()
        {
            try
            {
                TestReference();

                TestAVLTree();

                TestRedBlackTree();

                TestSplayTree();


                TestAllEntryComparisons();

                TestEntryConstruction();

                TestSetValueValidation();


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
