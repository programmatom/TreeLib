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

        private delegate KeyType MakeNewKey<KeyType>(ParkAndMiller random, KeyType[] notFromThese);
        private delegate ValueType MakeNewValue<ValueType>(ParkAndMiller random);

        private static int MakeIntKey(ParkAndMiller random, int[] notFromThese)
        {
            int key;
            do
            {
                key = random.Random();
            }
            while (Array.BinarySearch(notFromThese, key, Comparer<int>.Default) >= 0);
            return key;
        }

        private static float MakeFloatValue(ParkAndMiller random)
        {
            return .01f * random.Random();
        }

        private enum Kind { Map, Set };

        private enum RankKind { Rank, MultiRank };

        private enum RangeKind { Range, Range2 };

        private enum Width { Int, Long };


        //
        // Map & Set
        //

        private void TestMapOrSet<KeyType, ValueType, EntryType>(
            object testTree,
            Kind kind,
            MakeNewKey<KeyType> makeKey,
            MakeNewValue<ValueType> makeValue,
            IEnumerable<EntryType> enumerable)
            where KeyType : IComparable<KeyType> where ValueType : IComparable<ValueType>
        {
            IncrementIteration();
            long startIteration = iteration;
            IncrementIteration();

            // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
            // sequence of keys (with uniform distribution). The unit test will use the same set of keys and therefore the
            // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
            ParkAndMiller random = new ParkAndMiller(Seed);

            object[] emptyParameters = new object[0];
            PropertyInfo keyAccessor = typeof(EntryType).GetProperty("Key");
            PropertyInfo valueAccessor = kind == Kind.Map ? typeof(EntryType).GetProperty("Value") : null;

            MethodInfo addMethod = testTree.GetType().GetMethod("Add");
            MethodInfo removeMethod = testTree.GetType().GetMethod("Remove");

            IOrderedMap<KeyType, ValueType> reference = new ReferenceMap<KeyType, ValueType>();
            for (int i = 0; i < 100; i++)
            {
                IncrementIteration();
                long startIteration1 = iteration;
                IncrementIteration();

                KeyValuePair<KeyType, ValueType>[] referenceEntries = ((ISimpleTreeInspection<KeyType, ValueType>)reference).ToArray();

                int n = 0;
                foreach (EntryType entry in enumerable)
                {
                    TestTrue("enumeration", delegate () { return n < i; });

                    KeyType entryKey = (KeyType)keyAccessor.GetMethod.Invoke(entry, emptyParameters);
                    ValueType entryValue = kind == Kind.Map ? (ValueType)valueAccessor.GetMethod.Invoke(entry, emptyParameters) : default(ValueType);

                    TestTrue("enumeration", delegate () { return 0 == Comparer<KeyType>.Default.Compare(referenceEntries[n].Key, entryKey); });
                    if (kind == Kind.Map)
                    {
                        TestTrue("enumeration", delegate () { return 0 == Comparer<ValueType>.Default.Compare(referenceEntries[n].Value, entryValue); });
                    }

                    n++;
                }
                TestTrue("enumeration", delegate () { return n == reference.Count; });

                KeyType[] notFromThese = Array.ConvertAll(referenceEntries, delegate (KeyValuePair<KeyType, ValueType> item) { return item.Key; });
                KeyType newKey = makeKey(random, notFromThese);
                ValueType newValue = kind == Kind.Map ? makeValue(random) : default(ValueType);

                reference.Add(newKey, newValue);
                addMethod.Invoke(testTree, kind == Kind.Map ? new object[] { newKey, newValue } : new object[] { newKey });
            }

            do
            {
                IncrementIteration();
                long startIteration1 = iteration;
                IncrementIteration();

                KeyType keyToRemove = ((ISimpleTreeInspection<KeyType, ValueType>)reference).ToArray()[random.Random() % reference.Count].Key;

                reference.Remove(keyToRemove);
                removeMethod.Invoke(testTree, new object[] { keyToRemove });

                KeyValuePair<KeyType, ValueType>[] referenceEntries = ((ISimpleTreeInspection<KeyType, ValueType>)reference).ToArray();

                int n = 0;
                foreach (EntryType entry in enumerable)
                {
                    TestTrue("enumeration", delegate () { return n < reference.Count; });

                    KeyType entryKey = (KeyType)keyAccessor.GetMethod.Invoke(entry, emptyParameters);
                    ValueType entryValue = kind == Kind.Map ? (ValueType)valueAccessor.GetMethod.Invoke(entry, emptyParameters) : default(ValueType);

                    TestTrue("enumeration", delegate () { return 0 == Comparer<KeyType>.Default.Compare(referenceEntries[n].Key, entryKey); });
                    if (kind == Kind.Map)
                    {
                        TestTrue("enumeration", delegate () { return 0 == Comparer<ValueType>.Default.Compare(referenceEntries[n].Value, entryValue); });
                    }

                    n++;
                }
                TestTrue("enumeration", delegate () { return n == reference.Count; });

            } while (reference.Count != 0);

            // test interrupting enumerator with tree modifications, and while we're at it, Clear()
            MethodInfo clearMethod = testTree.GetType().GetMethod("Clear");
            for (int j = 0; j < 4; j++) // j needs to cover all switch cases below
            {
                KeyValuePair<KeyType, ValueType>[] referenceEntries;

                reference.Clear();
                clearMethod.Invoke(testTree, emptyParameters);

                const int ItemCount = 100;
                KeyType firstKey = default(KeyType);
                KeyType newKey = default(KeyType);
                for (int i = 0; i < ItemCount; i++)
                {
                    referenceEntries = ((ISimpleTreeInspection<KeyType, ValueType>)reference).ToArray();
                    KeyType[] notFromThese = Array.ConvertAll(referenceEntries, delegate (KeyValuePair<KeyType, ValueType> item) { return item.Key; });
                    newKey = makeKey(random, notFromThese);

                    if (i == 0)
                    {
                        firstKey = newKey;
                    }
                    if (i < ItemCount - 1) // don't add last key - we'll use it later to test breaking the enumerator
                    {
                        reference.Add(newKey, default(ValueType));
                        addMethod.Invoke(testTree, kind == Kind.Map ? new object[] { newKey, default(ValueType) } : new object[] { newKey });
                    }
                }
                Debug.Assert(!reference.ContainsKey(newKey));

                referenceEntries = ((ISimpleTreeInspection<KeyType, ValueType>)reference).ToArray();

                bool expectException = false;
                try
                {
                    int n = 0;
                    foreach (EntryType entry in enumerable)
                    {
                        if (expectException)
                        {
                            Fault(testTree, "Expected enumerator to throw but it didn't");
                        }

                        TestTrue("enumeration", delegate () { return n < ItemCount; });

                        KeyType entryKey = (KeyType)keyAccessor.GetMethod.Invoke(entry, emptyParameters);
                        TestTrue("enumeration", delegate () { return 0 == Comparer<KeyType>.Default.Compare(referenceEntries[n].Key, entryKey); });

                        if (n == 3 * ItemCount / 4)
                        {
                            if (Array.FindIndex(new string[] { "Fast", "Robust" }, delegate (string part) { return enumerable.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(enumerable, "Object 'enumerable' does not conform to the expected set of types");
                            }
                            if (Array.FindIndex(new string[] { "Splay", "RedBlack", "AVL" }, delegate (string part) { return testTree.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(enumerable, "Object 'testTree' does not conform to the expected set of types");
                            }

                            switch (j) // increase limit of 'j' loop if cases are added
                            {
                                case 0: // no tree changes
                                    break;
                                case 1: // add node - should throw
                                    expectException = enumerable.GetType().Name.Contains("Fast");
                                    addMethod.Invoke(testTree, kind == Kind.Map ? new object[] { newKey, default(ValueType) } : new object[] { newKey });
                                    break;
                                case 2: // remove node - should throw
                                    expectException = enumerable.GetType().Name.Contains("Fast");
                                    removeMethod.Invoke(testTree, new object[] { firstKey });
                                    break;
                                case 3: // query node - should not throw unless splay tree
                                    expectException = enumerable.GetType().Name.Contains("Fast")
                                        && testTree.GetType().Name.Contains("Splay");
                                    testTree.GetType().GetMethod("ContainsKey").Invoke(testTree, new object[] { newKey });
                                    break;
                            }
                        }
                        n++;
                    }
                    if (expectException)
                    {
                        Fault(testTree, "Expected enumerator to throw but it didn't");
                    }
                }
                catch (InvalidOperationException) when (expectException)
                {
                    // expected
                }
            }
        }

        //
        // RankMap & RankList
        //

        private void TestRankMapOrSet<KeyType, ValueType, EntryType>(
            object testTree,
            Kind kind,
            RankKind rankKind,
            MakeNewKey<KeyType> makeKey,
            MakeNewValue<ValueType> makeValue,
            IEnumerable<EntryType> enumerable)
            where KeyType : IComparable<KeyType> where ValueType : IComparable<ValueType>
        {
            IncrementIteration();
            long startIteration = iteration;
            IncrementIteration();

            // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
            // sequence of keys (with uniform distribution). The unit test will use the same set of keys and therefore the
            // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
            ParkAndMiller random = new ParkAndMiller(Seed);

            object[] emptyParameters = new object[0];
            PropertyInfo keyAccessor = typeof(EntryType).GetProperty("Key");
            PropertyInfo valueAccessor = kind == Kind.Map ? typeof(EntryType).GetProperty("Value") : null;
            PropertyInfo startAccessor = typeof(EntryType).GetProperty("Rank");
            PropertyInfo lengthAccessor = rankKind == RankKind.MultiRank ? typeof(EntryType).GetProperty("Count") : null;

            MethodInfo addMethod = testTree.GetType().GetMethod("Add");
            MethodInfo removeMethod = testTree.GetType().GetMethod("Remove");

            IMultiRankMap<KeyType, ValueType> reference = new ReferenceMultiRankMap<KeyType, ValueType>();
            for (int i = 0; i < 100; i++)
            {
                IncrementIteration();
                long startIteration1 = iteration;
                IncrementIteration();

                MultiRankMapEntry[] referenceEntries = ((INonInvasiveMultiRankMapInspection)reference).GetRanks();

                int n = 0;
                foreach (EntryType entry in enumerable)
                {
                    TestTrue("enumeration", delegate () { return n < i; });

                    KeyType entryKey = (KeyType)keyAccessor.GetMethod.Invoke(entry, emptyParameters);
                    ValueType entryValue = kind == Kind.Map ? (ValueType)valueAccessor.GetMethod.Invoke(entry, emptyParameters) : default(ValueType);
                    object startValueRaw = startAccessor.GetMethod.Invoke(entry, emptyParameters);
                    int startValue = startValueRaw.GetType().IsAssignableFrom(typeof(long)) ? (int)(long)startValueRaw : (int)startValueRaw; // must unbox first, then coerce
                    int lengthValue = 1;
                    if (rankKind == RankKind.MultiRank)
                    {
                        object lengthValueRaw = lengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                        lengthValue = lengthValueRaw.GetType().IsAssignableFrom(typeof(long)) ? (int)(long)lengthValueRaw : (int)lengthValueRaw; // must unbox first, then coerce
                    }

                    TestTrue("enumeration", delegate () { return 0 == Comparer<KeyType>.Default.Compare((KeyType)referenceEntries[n].key, entryKey); });
                    TestTrue("enumeration", delegate () { return 0 == Comparer<ValueType>.Default.Compare((ValueType)referenceEntries[n].value, entryValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].rank.start.CompareTo(startValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].rank.length.CompareTo(lengthValue); });

                    n++;
                }
                TestTrue("enumeration", delegate () { return n == reference.Count; });

                KeyType[] notFromThese = Array.ConvertAll(referenceEntries, delegate (MultiRankMapEntry item) { return (KeyType)item.key; });
                KeyType newKey = makeKey(random, notFromThese);
                ValueType newValue = kind == Kind.Map ? makeValue(random) : default(ValueType);
                int newLength = rankKind == RankKind.MultiRank ? random.Random() % 100 + 1 : 1;

                reference.Add(newKey, newValue, newLength);
                addMethod.Invoke(
                    testTree,
                    kind == Kind.Map
                        ? (rankKind == RankKind.MultiRank ? new object[] { newKey, newValue, newLength } : new object[] { newKey, newValue })
                        : (rankKind == RankKind.MultiRank ? new object[] { newKey, newLength } : new object[] { newKey }));
            }

            do
            {
                IncrementIteration();
                long startIteration1 = iteration;
                IncrementIteration();

                KeyType keyToRemove = (KeyType)((INonInvasiveMultiRankMapInspection)reference).GetRanks()[random.Random() % reference.Count].key;

                reference.Remove(keyToRemove);
                removeMethod.Invoke(testTree, new object[] { keyToRemove });

                MultiRankMapEntry[] referenceEntries = ((INonInvasiveMultiRankMapInspection)reference).GetRanks();

                int n = 0;
                foreach (EntryType entry in enumerable)
                {
                    TestTrue("enumeration", delegate () { return n < reference.Count; });

                    KeyType entryKey = (KeyType)keyAccessor.GetMethod.Invoke(entry, emptyParameters);
                    ValueType entryValue = kind == Kind.Map ? (ValueType)valueAccessor.GetMethod.Invoke(entry, emptyParameters) : default(ValueType);
                    object startValueRaw = startAccessor.GetMethod.Invoke(entry, emptyParameters);
                    int startValue = startValueRaw.GetType().IsAssignableFrom(typeof(long)) ? (int)(long)startValueRaw : (int)startValueRaw; // must unbox first, then coerce
                    int lengthValue = 1;
                    if (rankKind == RankKind.MultiRank)
                    {
                        object lengthValueRaw = lengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                        lengthValue = lengthValueRaw.GetType().IsAssignableFrom(typeof(long)) ? (int)(long)lengthValueRaw : (int)lengthValueRaw; // must unbox first, then coerce
                    }

                    TestTrue("enumeration", delegate () { return 0 == Comparer<KeyType>.Default.Compare((KeyType)referenceEntries[n].key, entryKey); });
                    TestTrue("enumeration", delegate () { return 0 == Comparer<ValueType>.Default.Compare((ValueType)referenceEntries[n].value, entryValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].rank.start.CompareTo(startValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].rank.length.CompareTo(lengthValue); });

                    n++;
                }
                TestTrue("enumeration", delegate () { return n == reference.Count; });

            } while (reference.Count != 0);

            // test interrupting enumerator with tree modifications, and while we're at it, Clear()
            MethodInfo clearMethod = testTree.GetType().GetMethod("Clear");
            for (int j = 0; j < 4; j++) // j needs to cover all switch cases below
            {
                const int Length = 1;
                MultiRankMapEntry[] referenceEntries;

                reference.Clear();
                clearMethod.Invoke(testTree, emptyParameters);

                const int ItemCount = 100;
                KeyType firstKey = default(KeyType);
                KeyType newKey = default(KeyType);
                for (int i = 0; i < ItemCount; i++)
                {
                    referenceEntries = ((INonInvasiveMultiRankMapInspection)reference).GetRanks();
                    KeyType[] notFromThese = Array.ConvertAll(referenceEntries, delegate (MultiRankMapEntry item) { return (KeyType)item.key; });
                    newKey = makeKey(random, notFromThese);

                    if (i == 0)
                    {
                        firstKey = newKey;
                    }
                    if (i < ItemCount - 1) // don't add last key - we'll use it later to test breaking the enumerator
                    {
                        reference.Add(newKey, default(ValueType), Length);
                        addMethod.Invoke(
                            testTree,
                            kind == Kind.Map
                                ? (rankKind == RankKind.MultiRank ? new object[] { newKey, default(ValueType), Length } : new object[] { newKey, default(ValueType) })
                                : (rankKind == RankKind.MultiRank ? new object[] { newKey, Length } : new object[] { newKey }));
                    }
                }
                Debug.Assert(!reference.ContainsKey(newKey));

                referenceEntries = ((INonInvasiveMultiRankMapInspection)reference).GetRanks();

                bool expectException = false;
                try
                {
                    int n = 0;
                    foreach (EntryType entry in enumerable)
                    {
                        if (expectException)
                        {
                            Fault(testTree, "Expected enumerator to throw but it didn't");
                        }

                        TestTrue("enumeration", delegate () { return n < ItemCount; });

                        KeyType entryKey = (KeyType)keyAccessor.GetMethod.Invoke(entry, emptyParameters);
                        TestTrue("enumeration", delegate () { return 0 == Comparer<KeyType>.Default.Compare((KeyType)referenceEntries[n].key, entryKey); });

                        if (n == 3 * ItemCount / 4)
                        {
                            if (Array.FindIndex(new string[] { "Fast", "Robust" }, delegate (string part) { return enumerable.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(enumerable, "Object 'enumerable' does not conform to the expected set of types");
                            }
                            if (Array.FindIndex(new string[] { "Splay", "RedBlack", "AVL" }, delegate (string part) { return testTree.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(enumerable, "Object 'testTree' does not conform to the expected set of types");
                            }

                            switch (j) // increase limit of 'j' loop if cases are added
                            {
                                case 0: // no tree changes
                                    break;
                                case 1: // add node - should throw
                                    expectException = enumerable.GetType().Name.Contains("Fast");
                                    addMethod.Invoke(
                                        testTree,
                                        kind == Kind.Map
                                            ? (rankKind == RankKind.MultiRank ? new object[] { newKey, default(ValueType), Length } : new object[] { newKey, default(ValueType) })
                                            : (rankKind == RankKind.MultiRank ? new object[] { newKey, Length } : new object[] { newKey }));
                                    break;
                                case 2: // remove node - should throw
                                    expectException = enumerable.GetType().Name.Contains("Fast");
                                    removeMethod.Invoke(testTree, new object[] { firstKey });
                                    break;
                                case 3: // query node - should not throw unless splay tree
                                    expectException = enumerable.GetType().Name.Contains("Fast")
                                        && testTree.GetType().Name.Contains("Splay");
                                    testTree.GetType().GetMethod("ContainsKey").Invoke(testTree, new object[] { newKey });
                                    break;
                            }
                        }
                        n++;
                    }
                    if (expectException)
                    {
                        Fault(testTree, "Expected enumerator to throw but it didn't");
                    }
                }
                catch (InvalidOperationException) when (expectException)
                {
                    // expected
                }
            }
        }

        //
        // RankMap & RankList
        //

        private void TestRangeMapOrList<ValueType, EntryType>(
            object testTree,
            Kind kind,
            RangeKind rangeKind,
            Width width,
            MakeNewValue<ValueType> makeValue,
            IEnumerable<EntryType> enumerable)
            where ValueType : IComparable<ValueType>
        {
            IncrementIteration();
            long startIteration = iteration;
            IncrementIteration();

            // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
            // sequence of keys (with uniform distribution). The unit test will use the same set of keys and therefore the
            // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
            ParkAndMiller random = new ParkAndMiller(Seed);

            object[] emptyParameters = new object[0];
            PropertyInfo valueAccessor = kind == Kind.Map ? typeof(EntryType).GetProperty("Value") : null;
            PropertyInfo xStartAccessor = typeof(EntryType).GetProperty(rangeKind == RangeKind.Range2 ? "XStart" : "Start");
            PropertyInfo xLengthAccessor = typeof(EntryType).GetProperty(rangeKind == RangeKind.Range2 ? "XLength" : "Length");
            PropertyInfo yStartAccessor = rangeKind == RangeKind.Range2 ? typeof(EntryType).GetProperty("YStart") : null;
            PropertyInfo yLengthAccessor = rangeKind == RangeKind.Range2 ? typeof(EntryType).GetProperty("YLength") : null;

            MethodInfo insertMethod = testTree.GetType().GetMethod("Insert");
            MethodInfo deleteMethod = testTree.GetType().GetMethod("Delete");

            IRange2Map<ValueType> reference = new ReferenceRange2Map<ValueType>();
            for (int i = 0; i < 100; i++)
            {
                IncrementIteration();
                long startIteration1 = iteration;
                IncrementIteration();

                Range2MapEntry[] referenceEntries = ((INonInvasiveRange2MapInspection)reference).GetRanges();
                if (rangeKind == RangeKind.Range)
                {
                    for (int j = 0; j < referenceEntries.Length; j++)
                    {
                        referenceEntries[j].y.start = 0;
                        referenceEntries[j].y.length = 0;
                    }
                }

                int n = 0;
                foreach (EntryType entry in enumerable)
                {
                    TestTrue("enumeration", delegate () { return n < i; });

                    ValueType entryValue = kind == Kind.Map ? (ValueType)valueAccessor.GetMethod.Invoke(entry, emptyParameters) : default(ValueType);
                    object xStartValueRaw = xStartAccessor.GetMethod.Invoke(entry, emptyParameters);
                    int xStartValue = width == Width.Long ? (int)(long)xStartValueRaw : (int)xStartValueRaw; // must unbox first, then coerce
                    object xLengthValueRaw = xLengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                    int xLengthValue = width == Width.Long ? (int)(long)xLengthValueRaw : (int)xLengthValueRaw; // must unbox first, then coerce
                    int yStartValue = 0, yLengthValue = 0;
                    if (rangeKind == RangeKind.Range2)
                    {
                        object yStartValueRaw = yStartAccessor.GetMethod.Invoke(entry, emptyParameters);
                        yStartValue = width == Width.Long ? (int)(long)yStartValueRaw : (int)yStartValueRaw; // must unbox first, then coerce
                        object yLengthValueRaw = yLengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                        yLengthValue = width == Width.Long ? (int)(long)yLengthValueRaw : (int)yLengthValueRaw; // must unbox first, then coerce
                    }

                    TestTrue("enumeration", delegate () { return 0 == Comparer<ValueType>.Default.Compare((ValueType)referenceEntries[n].value, entryValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].x.start.CompareTo(xStartValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].x.length.CompareTo(xLengthValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].y.start.CompareTo(yStartValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].y.length.CompareTo(yLengthValue); });

                    n++;
                }
                TestTrue("enumeration", delegate () { return n == reference.Count; });

                int xInsertBefore = reference.Count != 0 ? referenceEntries[random.Random() % reference.Count].x.start : 0;
                ValueType newValue = kind == Kind.Map ? makeValue(random) : default(ValueType);
                int newXLength = random.Random() % 100 + 1;
                int newYLength = rangeKind == RangeKind.Range2 ? random.Random() % 100 + 1 : 1;

                reference.Insert(xInsertBefore, Side.X, newXLength, rangeKind == RangeKind.Range2 ? newYLength : 1, newValue);
                if (width == Width.Int)
                {
                    insertMethod.Invoke(
                        testTree,
                        kind == Kind.Map
                            ? (rangeKind == RangeKind.Range2
                                ? new object[] { xInsertBefore, Side.X, newXLength, newYLength, newValue }
                                : new object[] { xInsertBefore, newXLength, newValue })
                            : (rangeKind == RangeKind.Range2
                                ? new object[] { xInsertBefore, Side.X, newXLength, newYLength }
                                : new object[] { xInsertBefore, newXLength }));
                }
                else
                {
                    insertMethod.Invoke(
                        testTree,
                        kind == Kind.Map
                            ? (rangeKind == RangeKind.Range2
                                ? new object[] { (long)xInsertBefore, Side.X, (long)newXLength, (long)newYLength, newValue }
                                : new object[] { (long)xInsertBefore, (long)newXLength, newValue })
                            : (rangeKind == RangeKind.Range2
                                ? new object[] { (long)xInsertBefore, Side.X, (long)newXLength, (long)newYLength }
                                : new object[] { (long)xInsertBefore, (long)newXLength }));
                }
            }

            do
            {
                IncrementIteration();
                long startIteration1 = iteration;
                IncrementIteration();

                int startToRemove = ((INonInvasiveRange2MapInspection)reference).GetRanges()[random.Random() % reference.Count].x.start;

                reference.Delete(startToRemove, Side.X);
                if (width == Width.Int)
                {
                    if (rangeKind == RangeKind.Range2)
                    {
                        deleteMethod.Invoke(testTree, new object[] { startToRemove, Side.X });
                    }
                    else
                    {
                        deleteMethod.Invoke(testTree, new object[] { startToRemove });
                    }
                }
                else
                {
                    if (rangeKind == RangeKind.Range2)
                    {
                        deleteMethod.Invoke(testTree, new object[] { (long)startToRemove, Side.X });
                    }
                    else
                    {
                        deleteMethod.Invoke(testTree, new object[] { (long)startToRemove });
                    }
                }

                Range2MapEntry[] referenceEntries = ((INonInvasiveRange2MapInspection)reference).GetRanges();
                if (rangeKind == RangeKind.Range)
                {
                    for (int j = 0; j < referenceEntries.Length; j++)
                    {
                        referenceEntries[j].y.start = 0;
                        referenceEntries[j].y.length = 0;
                    }
                }

                int n = 0;
                foreach (EntryType entry in enumerable)
                {
                    TestTrue("enumeration", delegate () { return n < reference.Count; });

                    ValueType entryValue = kind == Kind.Map ? (ValueType)valueAccessor.GetMethod.Invoke(entry, emptyParameters) : default(ValueType);
                    object xStartValueRaw = xStartAccessor.GetMethod.Invoke(entry, emptyParameters);
                    int xStartValue = width == Width.Long ? (int)(long)xStartValueRaw : (int)xStartValueRaw; // must unbox first, then coerce
                    object xLengthValueRaw = xLengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                    int xLengthValue = width == Width.Long ? (int)(long)xLengthValueRaw : (int)xLengthValueRaw; // must unbox first, then coerce
                    int yStartValue = 0, yLengthValue = 0;
                    if (rangeKind == RangeKind.Range2)
                    {
                        object yStartValueRaw = yStartAccessor.GetMethod.Invoke(entry, emptyParameters);
                        yStartValue = width == Width.Long ? (int)(long)yStartValueRaw : (int)yStartValueRaw; // must unbox first, then coerce
                        object yLengthValueRaw = yLengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                        yLengthValue = width == Width.Long ? (int)(long)yLengthValueRaw : (int)yLengthValueRaw; // must unbox first, then coerce
                    }

                    TestTrue("enumeration", delegate () { return 0 == Comparer<ValueType>.Default.Compare((ValueType)referenceEntries[n].value, entryValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].x.start.CompareTo(xStartValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].x.length.CompareTo(xLengthValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].y.start.CompareTo(yStartValue); });
                    TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].y.length.CompareTo(yLengthValue); });

                    n++;
                }
                TestTrue("enumeration", delegate () { return n == reference.Count; });

            } while (reference.Count != 0);

            // test interrupting enumerator with tree modifications, and while we're at it, Clear()
            MethodInfo clearMethod = testTree.GetType().GetMethod("Clear");
            for (int j = 0; j < 4; j++) // j needs to cover all switch cases below
            {
                reference.Clear();
                clearMethod.Invoke(testTree, emptyParameters);

                const int ItemCount = 100;
                for (int i = 0; i < ItemCount; i++)
                {
                    const int xInsertBefore = 0;
                    ValueType newValue = kind == Kind.Map ? makeValue(random) : default(ValueType);
                    int newXLength = random.Random() % 100 + 1;
                    int newYLength = rangeKind == RangeKind.Range2 ? random.Random() % 100 + 1 : 1;

                    reference.Insert(xInsertBefore, Side.X, newXLength, rangeKind == RangeKind.Range2 ? newYLength : 1, newValue);
                    if (width == Width.Int)
                    {
                        insertMethod.Invoke(
                            testTree,
                            kind == Kind.Map
                                ? (rangeKind == RangeKind.Range2
                                    ? new object[] { xInsertBefore, Side.X, newXLength, newYLength, newValue }
                                    : new object[] { xInsertBefore, newXLength, newValue })
                                : (rangeKind == RangeKind.Range2
                                    ? new object[] { xInsertBefore, Side.X, newXLength, newYLength }
                                    : new object[] { xInsertBefore, newXLength }));
                    }
                    else
                    {
                        insertMethod.Invoke(
                            testTree,
                            kind == Kind.Map
                                ? (rangeKind == RangeKind.Range2
                                    ? new object[] { (long)xInsertBefore, Side.X, (long)newXLength, (long)newYLength, newValue }
                                    : new object[] { (long)xInsertBefore, (long)newXLength, newValue })
                                : (rangeKind == RangeKind.Range2
                                    ? new object[] { (long)xInsertBefore, Side.X, (long)newXLength, (long)newYLength }
                                    : new object[] { (long)xInsertBefore, (long)newXLength }));
                    }
                }

                Range2MapEntry[] referenceEntries = ((INonInvasiveRange2MapInspection)reference).GetRanges();
                if (rangeKind == RangeKind.Range)
                {
                    for (int k = 0; k < referenceEntries.Length; k++)
                    {
                        referenceEntries[k].y.start = 0;
                        referenceEntries[k].y.length = 0;
                    }
                }

                bool expectException = false;
                try
                {
                    int n = 0;
                    foreach (EntryType entry in enumerable)
                    {
                        if (expectException)
                        {
                            Fault(testTree, "Expected enumerator to throw but it didn't");
                        }

                        TestTrue("enumeration", delegate () { return n < ItemCount; });

                        ValueType entryValue = kind == Kind.Map ? (ValueType)valueAccessor.GetMethod.Invoke(entry, emptyParameters) : default(ValueType);
                        object xStartValueRaw = xStartAccessor.GetMethod.Invoke(entry, emptyParameters);
                        int xStartValue = width == Width.Long ? (int)(long)xStartValueRaw : (int)xStartValueRaw; // must unbox first, then coerce
                        object xLengthValueRaw = xLengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                        int xLengthValue = width == Width.Long ? (int)(long)xLengthValueRaw : (int)xLengthValueRaw; // must unbox first, then coerce
                        int yStartValue = 0, yLengthValue = 0;
                        if (rangeKind == RangeKind.Range2)
                        {
                            object yStartValueRaw = yStartAccessor.GetMethod.Invoke(entry, emptyParameters);
                            yStartValue = width == Width.Long ? (int)(long)yStartValueRaw : (int)yStartValueRaw; // must unbox first, then coerce
                            object yLengthValueRaw = yLengthAccessor.GetMethod.Invoke(entry, emptyParameters);
                            yLengthValue = width == Width.Long ? (int)(long)yLengthValueRaw : (int)yLengthValueRaw; // must unbox first, then coerce
                        }

                        TestTrue("enumeration", delegate () { return 0 == Comparer<ValueType>.Default.Compare((ValueType)referenceEntries[n].value, entryValue); });
                        TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].x.start.CompareTo(xStartValue); });
                        TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].x.length.CompareTo(xLengthValue); });
                        TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].y.start.CompareTo(yStartValue); });
                        TestTrue("enumeration", delegate () { return 0 == referenceEntries[n].y.length.CompareTo(yLengthValue); });

                        if (n == 3 * ItemCount / 4)
                        {
                            if (Array.FindIndex(new string[] { "Fast", "Robust" }, delegate (string part) { return enumerable.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(enumerable, "Object 'enumerable' does not conform to the expected set of types");
                            }
                            if (Array.FindIndex(new string[] { "Splay", "RedBlack", "AVL" }, delegate (string part) { return testTree.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(enumerable, "Object 'testTree' does not conform to the expected set of types");
                            }

                            const int xInsertBefore = 0;
                            const int newXLength = 1;
                            const int newYLength = 1;
                            switch (j) // increase limit of 'j' loop if cases are added
                            {
                                case 0: // no tree changes
                                    break;
                                case 1: // add node - should throw
                                    expectException = true; // always, even for Robust in the case of Range collections
                                    ValueType newValue = default(ValueType);
                                    if (width == Width.Int)
                                    {
                                        insertMethod.Invoke(
                                            testTree,
                                            kind == Kind.Map
                                                ? (rangeKind == RangeKind.Range2
                                                    ? new object[] { xInsertBefore, Side.X, newXLength, newYLength, newValue }
                                                    : new object[] { xInsertBefore, newXLength, newValue })
                                                : (rangeKind == RangeKind.Range2
                                                    ? new object[] { xInsertBefore, Side.X, newXLength, newYLength }
                                                    : new object[] { xInsertBefore, newXLength }));
                                    }
                                    else
                                    {
                                        insertMethod.Invoke(
                                            testTree,
                                            kind == Kind.Map
                                                ? (rangeKind == RangeKind.Range2
                                                    ? new object[] { (long)xInsertBefore, Side.X, (long)newXLength, (long)newYLength, newValue }
                                                    : new object[] { (long)xInsertBefore, (long)newXLength, newValue })
                                                : (rangeKind == RangeKind.Range2
                                                    ? new object[] { (long)xInsertBefore, Side.X, (long)newXLength, (long)newYLength }
                                                    : new object[] { (long)xInsertBefore, (long)newXLength }));
                                    }
                                    break;
                                case 2: // remove node - should throw
                                    expectException = true; // always, even for Robust in the case of Range collections
                                    if (width == Width.Int)
                                    {
                                        deleteMethod.Invoke(testTree, rangeKind == RangeKind.Range2 ? new object[] { 0, Side.X } : new object[] { 0 });
                                    }
                                    else
                                    {
                                        deleteMethod.Invoke(testTree, rangeKind == RangeKind.Range2 ? new object[] { 0L, Side.X } : new object[] { 0L });
                                    }
                                    break;
                                case 3: // query node - should not throw unless splay tree
                                    expectException = testTree.GetType().Name.Contains("Splay");
                                    if (width == Width.Int)
                                    {
                                        testTree.GetType().GetMethod("Contains").Invoke(testTree, rangeKind == RangeKind.Range2 ? new object[] { 0, Side.X } : new object[] { 0 });
                                    }
                                    else
                                    {
                                        testTree.GetType().GetMethod("Contains").Invoke(testTree, rangeKind == RangeKind.Range2 ? new object[] { 0L, Side.X } : new object[] { 0L });
                                    }
                                    break;
                            }
                        }
                        n++;
                    }
                    if (expectException)
                    {
                        Fault(testTree, "Expected enumerator to throw but it didn't");
                    }
                }
                catch (InvalidOperationException) when (expectException)
                {
                    // expected
                }
            }
        }


        //
        // main test
        //

        // We're using AllocationMode.DynamicRetainFreelist in all of these in order to gain coverage of the Clear() method's
        // complex code path (that must free each node).

        private void TestSplayTree()
        {
            //
            // Map
            //

            {
                SplayTreeMap<int, float> tree;
                tree = new SplayTreeMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new SplayTreeMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new SplayTreeMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayMap<int, float> tree;
                tree = new SplayTreeArrayMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new SplayTreeArrayMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new SplayTreeArrayMap<int, float>.FastEnumerableSurrogate(tree));
            }

            //
            // Set
            //

            {
                SplayTreeList<int> tree;
                tree = new SplayTreeList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new SplayTreeList<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new SplayTreeList<int>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayList<int> tree;
                tree = new SplayTreeArrayList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new SplayTreeArrayList<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new SplayTreeArrayList<int>.FastEnumerableSurrogate(tree));
            }


            //
            // RankMap
            //

            // Int32

            {
                SplayTreeRankMap<int, float> tree;
                tree = new SplayTreeRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayRankMap<int, float> tree;
                tree = new SplayTreeArrayRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeArrayRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeArrayRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeRankMapLong<int, float> tree;
                tree = new SplayTreeRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankMapLong<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankMapLong<int, float>.FastEnumerableSurrogate(tree));
            }


            //
            // RankList
            //

            {
                SplayTreeRankList<int> tree;
                tree = new SplayTreeRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankList<int>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayRankList<int> tree;
                tree = new SplayTreeArrayRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeArrayRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeArrayRankList<int>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeRankListLong<int> tree;
                tree = new SplayTreeRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankListLong<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new SplayTreeRankListLong<int>.FastEnumerableSurrogate(tree));
            }


            //
            // MultiRankMap
            //

            // Int32

            {
                SplayTreeMultiRankMap<int, float> tree;
                tree = new SplayTreeMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayMultiRankMap<int, float> tree;
                tree = new SplayTreeArrayMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeArrayMultiRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeArrayMultiRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeMultiRankMapLong<int, float> tree;
                tree = new SplayTreeMultiRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankMapLong<int, float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeMultiRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankMapLong<int, float>.FastEnumerableSurrogate(tree));
            }


            //
            // MultiRankList
            //

            // Int32

            {
                SplayTreeMultiRankList<int> tree;
                tree = new SplayTreeMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankList<int>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayMultiRankList<int> tree;
                tree = new SplayTreeArrayMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeArrayMultiRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeArrayMultiRankList<int>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeMultiRankListLong<int> tree;
                tree = new SplayTreeMultiRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankListLong<int>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeMultiRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new SplayTreeMultiRankListLong<int>.FastEnumerableSurrogate(tree));
            }


            //
            // Range2Map
            //

            // Int32

            {
                SplayTreeRange2Map<float> tree;
                tree = new SplayTreeRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeRange2Map<float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeRange2Map<float>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayRange2Map<float> tree;
                tree = new SplayTreeArrayRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeArrayRange2Map<float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeArrayRange2Map<float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeRange2MapLong<float> tree;
                tree = new SplayTreeRange2MapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Long, MakeFloatValue, new SplayTreeRange2MapLong<float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRange2MapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Long, MakeFloatValue, new SplayTreeRange2MapLong<float>.FastEnumerableSurrogate(tree));
            }


            //
            // Range2List
            //

            // Int32

            {
                SplayTreeRange2List tree;
                tree = new SplayTreeRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeRange2List.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeRange2List.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayRange2List tree;
                tree = new SplayTreeArrayRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeArrayRange2List.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new SplayTreeArrayRange2List.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeRange2ListLong tree;
                tree = new SplayTreeRange2ListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2ListLong>(
                    tree, Kind.Set, RangeKind.Range2, Width.Long, MakeFloatValue, new SplayTreeRange2ListLong.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRange2ListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2ListLong>(
                    tree, Kind.Set, RangeKind.Range2, Width.Long, MakeFloatValue, new SplayTreeRange2ListLong.FastEnumerableSurrogate(tree));
            }


            //
            // RangeMap
            //

            // Int32

            {
                SplayTreeRangeMap<float> tree;
                tree = new SplayTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeRangeMap<float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeRangeMap<float>.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayRangeMap<float> tree;
                tree = new SplayTreeArrayRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeArrayRangeMap<float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeArrayRangeMap<float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeRangeMapLong<float> tree;
                tree = new SplayTreeRangeMapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Long, MakeFloatValue, new SplayTreeRangeMapLong<float>.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRangeMapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Long, MakeFloatValue, new SplayTreeRangeMapLong<float>.FastEnumerableSurrogate(tree));
            }


            //
            // RangeList
            //

            // Int32

            {
                SplayTreeRangeList tree;
                tree = new SplayTreeRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeRangeList.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeRangeList.FastEnumerableSurrogate(tree));
            }

            {
                SplayTreeArrayRangeList tree;
                tree = new SplayTreeArrayRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeArrayRangeList.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeArrayRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new SplayTreeArrayRangeList.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                SplayTreeRangeListLong tree;
                tree = new SplayTreeRangeListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeListLong>(
                    tree, Kind.Set, RangeKind.Range, Width.Long, MakeFloatValue, new SplayTreeRangeListLong.RobustEnumerableSurrogate(tree));
                tree = new SplayTreeRangeListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeListLong>(
                    tree, Kind.Set, RangeKind.Range, Width.Long, MakeFloatValue, new SplayTreeRangeListLong.FastEnumerableSurrogate(tree));
            }
        }

        private void TestRedBlackTree()
        {
            //
            // Map
            //

            {
                RedBlackTreeMap<int, float> tree;
                tree = new RedBlackTreeMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new RedBlackTreeMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new RedBlackTreeMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayMap<int, float> tree;
                tree = new RedBlackTreeArrayMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayMap<int, float>.FastEnumerableSurrogate(tree));
            }

            //
            // Set
            //

            {
                RedBlackTreeList<int> tree;
                tree = new RedBlackTreeList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new RedBlackTreeList<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new RedBlackTreeList<int>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayList<int> tree;
                tree = new RedBlackTreeArrayList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new RedBlackTreeArrayList<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new RedBlackTreeArrayList<int>.FastEnumerableSurrogate(tree));
            }


            //
            // RankMap
            //

            // Int32

            {
                RedBlackTreeRankMap<int, float> tree;
                tree = new RedBlackTreeRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayRankMap<int, float> tree;
                tree = new RedBlackTreeArrayRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeRankMapLong<int, float> tree;
                tree = new RedBlackTreeRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankMapLong<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankMapLong<int, float>.FastEnumerableSurrogate(tree));
            }


            //
            // RankList
            //

            {
                RedBlackTreeRankList<int> tree;
                tree = new RedBlackTreeRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankList<int>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayRankList<int> tree;
                tree = new RedBlackTreeArrayRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayRankList<int>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeRankListLong<int> tree;
                tree = new RedBlackTreeRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankListLong<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new RedBlackTreeRankListLong<int>.FastEnumerableSurrogate(tree));
            }


            //
            // MultiRankMap
            //

            // Int32

            {
                RedBlackTreeMultiRankMap<int, float> tree;
                tree = new RedBlackTreeMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayMultiRankMap<int, float> tree;
                tree = new RedBlackTreeArrayMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayMultiRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayMultiRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeMultiRankMapLong<int, float> tree;
                tree = new RedBlackTreeMultiRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankMapLong<int, float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeMultiRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankMapLong<int, float>.FastEnumerableSurrogate(tree));
            }


            //
            // MultiRankList
            //

            // Int32

            {
                RedBlackTreeMultiRankList<int> tree;
                tree = new RedBlackTreeMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankList<int>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayMultiRankList<int> tree;
                tree = new RedBlackTreeArrayMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayMultiRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeArrayMultiRankList<int>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeMultiRankListLong<int> tree;
                tree = new RedBlackTreeMultiRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankListLong<int>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeMultiRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new RedBlackTreeMultiRankListLong<int>.FastEnumerableSurrogate(tree));
            }


            //
            // Range2Map
            //

            // Int32

            {
                RedBlackTreeRange2Map<float> tree;
                tree = new RedBlackTreeRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeRange2Map<float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeRange2Map<float>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayRange2Map<float> tree;
                tree = new RedBlackTreeArrayRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeArrayRange2Map<float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeArrayRange2Map<float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeRange2MapLong<float> tree;
                tree = new RedBlackTreeRange2MapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Long, MakeFloatValue, new RedBlackTreeRange2MapLong<float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRange2MapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Long, MakeFloatValue, new RedBlackTreeRange2MapLong<float>.FastEnumerableSurrogate(tree));
            }


            //
            // Range2List
            //

            // Int32

            {
                RedBlackTreeRange2List tree;
                tree = new RedBlackTreeRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeRange2List.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeRange2List.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayRange2List tree;
                tree = new RedBlackTreeArrayRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeArrayRange2List.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new RedBlackTreeArrayRange2List.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeRange2ListLong tree;
                tree = new RedBlackTreeRange2ListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2ListLong>(
                    tree, Kind.Set, RangeKind.Range2, Width.Long, MakeFloatValue, new RedBlackTreeRange2ListLong.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRange2ListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2ListLong>(
                    tree, Kind.Set, RangeKind.Range2, Width.Long, MakeFloatValue, new RedBlackTreeRange2ListLong.FastEnumerableSurrogate(tree));
            }


            //
            // RangeMap
            //

            // Int32

            {
                RedBlackTreeRangeMap<float> tree;
                tree = new RedBlackTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeRangeMap<float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeRangeMap<float>.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayRangeMap<float> tree;
                tree = new RedBlackTreeArrayRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeArrayRangeMap<float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeArrayRangeMap<float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeRangeMapLong<float> tree;
                tree = new RedBlackTreeRangeMapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Long, MakeFloatValue, new RedBlackTreeRangeMapLong<float>.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRangeMapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Long, MakeFloatValue, new RedBlackTreeRangeMapLong<float>.FastEnumerableSurrogate(tree));
            }


            //
            // RangeList
            //

            // Int32

            {
                RedBlackTreeRangeList tree;
                tree = new RedBlackTreeRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeRangeList.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeRangeList.FastEnumerableSurrogate(tree));
            }

            {
                RedBlackTreeArrayRangeList tree;
                tree = new RedBlackTreeArrayRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeArrayRangeList.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeArrayRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new RedBlackTreeArrayRangeList.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                RedBlackTreeRangeListLong tree;
                tree = new RedBlackTreeRangeListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeListLong>(
                    tree, Kind.Set, RangeKind.Range, Width.Long, MakeFloatValue, new RedBlackTreeRangeListLong.RobustEnumerableSurrogate(tree));
                tree = new RedBlackTreeRangeListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeListLong>(
                    tree, Kind.Set, RangeKind.Range, Width.Long, MakeFloatValue, new RedBlackTreeRangeListLong.FastEnumerableSurrogate(tree));
            }
        }

        private void TestAVLTree()
        {
            //
            // Map
            //

            {
                AVLTreeMap<int, float> tree;
                tree = new AVLTreeMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new AVLTreeMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new AVLTreeMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayMap<int, float> tree;
                tree = new AVLTreeArrayMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new AVLTreeArrayMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryMap<int, float>>(
                    tree, Kind.Map, MakeIntKey, MakeFloatValue, new AVLTreeArrayMap<int, float>.FastEnumerableSurrogate(tree));
            }

            //
            // Set
            //

            {
                AVLTreeList<int> tree;
                tree = new AVLTreeList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new AVLTreeList<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new AVLTreeList<int>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayList<int> tree;
                tree = new AVLTreeArrayList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new AVLTreeArrayList<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestMapOrSet<int, float, EntryList<int>>(
                    tree, Kind.Set, MakeIntKey, null, new AVLTreeArrayList<int>.FastEnumerableSurrogate(tree));
            }


            //
            // RankMap
            //

            // Int32

            {
                AVLTreeRankMap<int, float> tree;
                tree = new AVLTreeRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayRankMap<int, float> tree;
                tree = new AVLTreeArrayRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeArrayRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeArrayRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeRankMapLong<int, float> tree;
                tree = new AVLTreeRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankMapLong<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankMapLong<int, float>.FastEnumerableSurrogate(tree));
            }


            //
            // RankList
            //

            {
                AVLTreeRankList<int> tree;
                tree = new AVLTreeRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankList<int>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayRankList<int> tree;
                tree = new AVLTreeArrayRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeArrayRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankList<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeArrayRankList<int>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeRankListLong<int> tree;
                tree = new AVLTreeRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankListLong<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                    tree, Kind.Set, RankKind.Rank, MakeIntKey, MakeFloatValue, new AVLTreeRankListLong<int>.FastEnumerableSurrogate(tree));
            }


            //
            // MultiRankMap
            //

            // Int32

            {
                AVLTreeMultiRankMap<int, float> tree;
                tree = new AVLTreeMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayMultiRankMap<int, float> tree;
                tree = new AVLTreeArrayMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeArrayMultiRankMap<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayMultiRankMap<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeArrayMultiRankMap<int, float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeMultiRankMapLong<int, float> tree;
                tree = new AVLTreeMultiRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankMapLong<int, float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeMultiRankMapLong<int, float>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                    tree, Kind.Map, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankMapLong<int, float>.FastEnumerableSurrogate(tree));
            }


            //
            // MultiRankList
            //

            // Int32

            {
                AVLTreeMultiRankList<int> tree;
                tree = new AVLTreeMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankList<int>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayMultiRankList<int> tree;
                tree = new AVLTreeArrayMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeArrayMultiRankList<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayMultiRankList<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeArrayMultiRankList<int>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeMultiRankListLong<int> tree;
                tree = new AVLTreeMultiRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankListLong<int>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeMultiRankListLong<int>(0, AllocationMode.DynamicRetainFreelist);
                TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                    tree, Kind.Set, RankKind.MultiRank, MakeIntKey, MakeFloatValue, new AVLTreeMultiRankListLong<int>.FastEnumerableSurrogate(tree));
            }


            //
            // Range2Map
            //

            // Int32

            {
                AVLTreeRange2Map<float> tree;
                tree = new AVLTreeRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeRange2Map<float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeRange2Map<float>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayRange2Map<float> tree;
                tree = new AVLTreeArrayRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeArrayRange2Map<float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayRange2Map<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2Map<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeArrayRange2Map<float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeRange2MapLong<float> tree;
                tree = new AVLTreeRange2MapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Long, MakeFloatValue, new AVLTreeRange2MapLong<float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRange2MapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                    tree, Kind.Map, RangeKind.Range2, Width.Long, MakeFloatValue, new AVLTreeRange2MapLong<float>.FastEnumerableSurrogate(tree));
            }


            //
            // Range2List
            //

            // Int32

            {
                AVLTreeRange2List tree;
                tree = new AVLTreeRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeRange2List.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeRange2List.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayRange2List tree;
                tree = new AVLTreeArrayRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeArrayRange2List.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayRange2List(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2List>(
                    tree, Kind.Set, RangeKind.Range2, Width.Int, MakeFloatValue, new AVLTreeArrayRange2List.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeRange2ListLong tree;
                tree = new AVLTreeRange2ListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2ListLong>(
                    tree, Kind.Set, RangeKind.Range2, Width.Long, MakeFloatValue, new AVLTreeRange2ListLong.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRange2ListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRange2ListLong>(
                    tree, Kind.Set, RangeKind.Range2, Width.Long, MakeFloatValue, new AVLTreeRange2ListLong.FastEnumerableSurrogate(tree));
            }


            //
            // RangeMap
            //

            // Int32

            {
                AVLTreeRangeMap<float> tree;
                tree = new AVLTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeRangeMap<float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeRangeMap<float>.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayRangeMap<float> tree;
                tree = new AVLTreeArrayRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeArrayRangeMap<float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMap<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeArrayRangeMap<float>.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeRangeMapLong<float> tree;
                tree = new AVLTreeRangeMapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Long, MakeFloatValue, new AVLTreeRangeMapLong<float>.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRangeMapLong<float>(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                    tree, Kind.Map, RangeKind.Range, Width.Long, MakeFloatValue, new AVLTreeRangeMapLong<float>.FastEnumerableSurrogate(tree));
            }


            //
            // RangeList
            //

            // Int32

            {
                AVLTreeRangeList tree;
                tree = new AVLTreeRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeRangeList.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeRangeList.FastEnumerableSurrogate(tree));
            }

            {
                AVLTreeArrayRangeList tree;
                tree = new AVLTreeArrayRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeArrayRangeList.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeArrayRangeList(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeList>(
                    tree, Kind.Set, RangeKind.Range, Width.Int, MakeFloatValue, new AVLTreeArrayRangeList.FastEnumerableSurrogate(tree));
            }

            // Long

            {
                AVLTreeRangeListLong tree;
                tree = new AVLTreeRangeListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeListLong>(
                    tree, Kind.Set, RangeKind.Range, Width.Long, MakeFloatValue, new AVLTreeRangeListLong.RobustEnumerableSurrogate(tree));
                tree = new AVLTreeRangeListLong(0, AllocationMode.DynamicRetainFreelist);
                TestRangeMapOrList<float, EntryRangeListLong>(
                    tree, Kind.Set, RangeKind.Range, Width.Long, MakeFloatValue, new AVLTreeRangeListLong.FastEnumerableSurrogate(tree));
            }
        }

        public override bool Do()
        {
            try
            {
                TestSplayTree();

                TestRedBlackTree();

                TestAVLTree();


                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("LAST ITERATION {0}, LAST ACTION ITERATION {1}", iteration, lastActionIteration);
                throw;
            }
        }
    }
}
