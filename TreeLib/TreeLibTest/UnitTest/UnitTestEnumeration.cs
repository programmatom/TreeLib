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

        private delegate KeyType MakeNewKey<KeyType>(ParkAndMiller random, KeyType[] notFromThese);
        private delegate ValueType MakeNewValue<ValueType>(ParkAndMiller random);

        private static int MakeIntKey(ParkAndMiller random, int[] notFromThese)
        {
            int key;
            do
            {
                key = random.Next();
            }
            while ((notFromThese != null) && Array.BinarySearch(notFromThese, key, Comparer<int>.Default) >= 0);
            return key;
        }

        private static float MakeFloatValue(ParkAndMiller random)
        {
            return .01f * random.Next();
        }

        private enum Kind { Map, List };

        private enum RankKind { Rank, MultiRank };

        private enum RangeKind { Range, Range2 };

        private enum Width { Int, Long };

        private enum EnumKind { Fast, Robust };

        private class DefaultEnumeratorProxy<EntryType> : IEnumerable<EntryType>
        {
            private readonly object inner;

            public DefaultEnumeratorProxy(object tree)
            {
                this.inner = tree;
            }

            public IEnumerator<EntryType> GetEnumerator()
            {
                return ((IEnumerable<EntryType>)inner).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)inner).GetEnumerator();
            }

            public object Inner { get { return inner; } }
        }


        //
        // Map & Set
        //

        private delegate IEnumerable<EntryType> GetEnumerable<EntryType>();
        private void TestMapOrList<KeyType, ValueType, EntryType>(
            object testTree,
            Kind kind,
            EnumKind enumKind,
            MakeNewKey<KeyType> makeKey,
            MakeNewValue<ValueType> makeValue,
            GetEnumerable<EntryType> getEnumerable)
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
                foreach (EntryType entry in getEnumerable())
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

                KeyType keyToRemove = ((ISimpleTreeInspection<KeyType, ValueType>)reference).ToArray()[random.Next() % reference.Count].Key;

                reference.Remove(keyToRemove);
                removeMethod.Invoke(testTree, new object[] { keyToRemove });

                KeyValuePair<KeyType, ValueType>[] referenceEntries = ((ISimpleTreeInspection<KeyType, ValueType>)reference).ToArray();

                int n = 0;
                foreach (EntryType entry in getEnumerable())
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

                // test non-generic enumerator
                foreach (IEnumerable enumerable in new IEnumerable[] { (IEnumerable)testTree, getEnumerable() })
                {
                    n = 0;
                    foreach (object objEntry in enumerable)
                    {
                        EntryType entry = default(EntryType);
                        TestNoThrow("enumeration", delegate () { entry = (EntryType)objEntry; });

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
                }

                // test boundary cases
                {
                    IEnumerator<EntryType> enumerator = getEnumerable().GetEnumerator();

                    TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                    n = 0;
                    while (enumerator.MoveNext())
                    {
                        TestNoThrow("enumeration", delegate () { string text = enumerator.Current.ToString(); });
                        TestNoThrow("enumeration", delegate () { int hash = enumerator.Current.GetHashCode(); });

                        TestTrue("enumeration", delegate () { return enumerator.Current.Equals(enumerator.Current); });

                        n++;
                    }
                    TestTrue("enumeration", delegate () { return n == reference.Count; });

                    TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                    TestFalse("enumeration", delegate () { return enumerator.MoveNext(); }); // extra MoveNext after termination
                }

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
                    foreach (EntryType entry in getEnumerable())
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
                            if (Array.FindIndex(new string[] { "Splay", "RedBlack", "AVL" }, delegate (string part) { return testTree.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(getEnumerable(), "Object 'testTree' does not conform to the expected set of types");
                            }

                            switch (j) // increase limit of 'j' loop if cases are added
                            {
                                case 0: // no tree changes
                                    break;
                                case 1: // add node - should throw
                                    expectException = enumKind == EnumKind.Fast;
                                    addMethod.Invoke(testTree, kind == Kind.Map ? new object[] { newKey, default(ValueType) } : new object[] { newKey });
                                    break;
                                case 2: // remove node - should throw
                                    expectException = enumKind == EnumKind.Fast;
                                    removeMethod.Invoke(testTree, new object[] { firstKey });
                                    break;
                                case 3: // query node - should not throw unless splay tree
                                    expectException = (enumKind == EnumKind.Fast) && testTree.GetType().Name.Contains("Splay");
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
            EnumKind enumKind,
            MakeNewKey<KeyType> makeKey,
            MakeNewValue<ValueType> makeValue,
            GetEnumerable<EntryType> getEnumerable)
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
                foreach (EntryType entry in getEnumerable())
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
                int newLength = rankKind == RankKind.MultiRank ? random.Next() % 100 + 1 : 1;

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

                KeyType keyToRemove = (KeyType)((INonInvasiveMultiRankMapInspection)reference).GetRanks()[random.Next() % reference.Count].key;

                reference.Remove(keyToRemove);
                removeMethod.Invoke(testTree, new object[] { keyToRemove });

                MultiRankMapEntry[] referenceEntries = ((INonInvasiveMultiRankMapInspection)reference).GetRanks();

                int n = 0;
                foreach (EntryType entry in getEnumerable())
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

                // test non-generic enumerator
                foreach (IEnumerable enumerable in new IEnumerable[] { (IEnumerable)testTree, getEnumerable() })
                {
                    n = 0;
                    foreach (object objEntry in enumerable)
                    {
                        EntryType entry = default(EntryType);
                        TestNoThrow("enumeration", delegate () { entry = (EntryType)objEntry; });

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
                }

                // test boundary cases
                {
                    IEnumerator<EntryType> enumerator = getEnumerable().GetEnumerator();

                    TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                    n = 0;
                    while (enumerator.MoveNext())
                    {
                        n++;

                        TestNoThrow("enumeration", delegate () { string text = enumerator.Current.ToString(); });
                        TestNoThrow("enumeration", delegate () { int hash = enumerator.Current.GetHashCode(); });

                        TestTrue("enumeration", delegate () { return enumerator.Current.Equals(enumerator.Current); });
                    }
                    TestTrue("enumeration", delegate () { return n == reference.Count; });

                    TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                    TestFalse("enumeration", delegate () { return enumerator.MoveNext(); }); // extra MoveNext after termination
                }

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
                    foreach (EntryType entry in getEnumerable())
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
                            if (Array.FindIndex(new string[] { "Splay", "RedBlack", "AVL" }, delegate (string part) { return testTree.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(getEnumerable(), "Object 'testTree' does not conform to the expected set of types");
                            }

                            switch (j) // increase limit of 'j' loop if cases are added
                            {
                                case 0: // no tree changes
                                    break;
                                case 1: // add node - should throw
                                    expectException = enumKind == EnumKind.Fast;
                                    addMethod.Invoke(
                                        testTree,
                                        kind == Kind.Map
                                            ? (rankKind == RankKind.MultiRank ? new object[] { newKey, default(ValueType), Length } : new object[] { newKey, default(ValueType) })
                                            : (rankKind == RankKind.MultiRank ? new object[] { newKey, Length } : new object[] { newKey }));
                                    break;
                                case 2: // remove node - should throw
                                    expectException = enumKind == EnumKind.Fast;
                                    removeMethod.Invoke(testTree, new object[] { firstKey });
                                    break;
                                case 3: // query node - should not throw unless splay tree
                                    expectException = (enumKind == EnumKind.Fast) && testTree.GetType().Name.Contains("Splay");
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
            EnumKind enumKind,
            MakeNewValue<ValueType> makeValue,
            GetEnumerable<EntryType> getEnumerable)
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
                foreach (EntryType entry in getEnumerable())
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

                int xInsertBefore = reference.Count != 0 ? referenceEntries[random.Next() % reference.Count].x.start : 0;
                ValueType newValue = kind == Kind.Map ? makeValue(random) : default(ValueType);
                int newXLength = random.Next() % 100 + 1;
                int newYLength = rangeKind == RangeKind.Range2 ? random.Next() % 100 + 1 : 1;

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

                int startToRemove = ((INonInvasiveRange2MapInspection)reference).GetRanges()[random.Next() % reference.Count].x.start;

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
                foreach (EntryType entry in getEnumerable())
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

                // test non-generic enumerator
                foreach (IEnumerable enumerable in new IEnumerable[] { (IEnumerable)testTree, getEnumerable() })
                {
                    n = 0;
                    foreach (object objEntry in enumerable)
                    {
                        EntryType entry = default(EntryType);
                        TestNoThrow("enumeration", delegate () { entry = (EntryType)objEntry; });

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
                }

                // test boundary cases
                {
                    IEnumerator<EntryType> enumerator = getEnumerable().GetEnumerator();

                    TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                    n = 0;
                    while (enumerator.MoveNext())
                    {
                        TestNoThrow("enumeration", delegate () { string text = enumerator.Current.ToString(); });
                        TestNoThrow("enumeration", delegate () { int hash = enumerator.Current.GetHashCode(); });

                        TestTrue("enumeration", delegate () { return enumerator.Current.Equals(enumerator.Current); });

                        n++;
                    }
                    TestTrue("enumeration", delegate () { return n == reference.Count; });

                    TestTrue("enumeration", delegate () { return enumerator.Current.Equals(default(EntryType)); });

                    TestFalse("enumeration", delegate () { return enumerator.MoveNext(); }); // extra MoveNext after termination
                }

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
                    int newXLength = random.Next() % 100 + 1;
                    int newYLength = rangeKind == RangeKind.Range2 ? random.Next() % 100 + 1 : 1;

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
                    foreach (EntryType entry in getEnumerable())
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
                            if (Array.FindIndex(new string[] { "Splay", "RedBlack", "AVL" }, delegate (string part) { return testTree.GetType().Name.Contains(part); }) < 0)
                            {
                                Fault(getEnumerable(), "Object 'testTree' does not conform to the expected set of types");
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

        private void TestEntryComparisons(Type entryType)
        {
            foreach (FieldInfo field in entryType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
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

        private void TestSplayTree()
        {
            // Exercise all allocation modes to gain coverage of both code paths in the Clear() method.
            foreach (AllocationMode allocationMode in new AllocationMode[] { AllocationMode.DynamicDiscard, AllocationMode.DynamicRetainFreelist, AllocationMode.PreallocatedFixed })
            {
                uint capacity = allocationMode == AllocationMode.PreallocatedFixed ? TreeCapacityForFixed : 0;

                //
                // Map
                //

                {
                    SplayTreeMap<int, float> tree;
                    tree = new SplayTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayMap<int, float> tree;
                    tree = new SplayTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMap<int, float>>(tree); });
                }

                //
                // Set
                //

                {
                    SplayTreeList<int> tree;
                    tree = new SplayTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return new DefaultEnumeratorProxy<EntryList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayList<int> tree;
                    tree = new SplayTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return new DefaultEnumeratorProxy<EntryList<int>>(tree); });
                }


                //
                // RankMap
                //

                // Int32

                {
                    SplayTreeRankMap<int, float> tree;
                    tree = new SplayTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayRankMap<int, float> tree;
                    tree = new SplayTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMap<int, float>>(tree); });
                }

                // Long

                {
                    SplayTreeRankMapLong<int, float> tree;
                    tree = new SplayTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMapLong<int, float>>(tree); });
                }


                //
                // RankList
                //

                {
                    SplayTreeRankList<int> tree;
                    tree = new SplayTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayRankList<int> tree;
                    tree = new SplayTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankList<int>>(tree); });
                }

                // Long

                {
                    SplayTreeRankListLong<int> tree;
                    tree = new SplayTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankListLong<int>>(tree); });
                }


                //
                // MultiRankMap
                //

                // Int32

                {
                    SplayTreeMultiRankMap<int, float> tree;
                    tree = new SplayTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayMultiRankMap<int, float> tree;
                    tree = new SplayTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMap<int, float>>(tree); });
                }

                // Long

                {
                    SplayTreeMultiRankMapLong<int, float> tree;
                    tree = new SplayTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMapLong<int, float>>(tree); });
                }


                //
                // MultiRankList
                //

                // Int32

                {
                    SplayTreeMultiRankList<int> tree;
                    tree = new SplayTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayMultiRankList<int> tree;
                    tree = new SplayTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankList<int>>(tree); });
                }

                // Long

                {
                    SplayTreeMultiRankListLong<int> tree;
                    tree = new SplayTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankListLong<int>>(tree); });
                }


                //
                // Range2Map
                //

                // Int32

                {
                    SplayTreeRange2Map<float> tree;
                    tree = new SplayTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2Map<float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayRange2Map<float> tree;
                    tree = new SplayTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2Map<float>>(tree); });
                }

                // Long

                {
                    SplayTreeRange2MapLong<float> tree;
                    tree = new SplayTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2MapLong<float>>(tree); });
                }


                //
                // Range2List
                //

                // Int32

                {
                    SplayTreeRange2List tree;
                    tree = new SplayTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2List>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayRange2List tree;
                    tree = new SplayTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2List>(tree); });
                }

                // Long

                {
                    SplayTreeRange2ListLong tree;
                    tree = new SplayTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2ListLong>(tree); });
                }


                //
                // RangeMap
                //

                // Int32

                {
                    SplayTreeRangeMap<float> tree;
                    tree = new SplayTreeRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMap<float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayRangeMap<float> tree;
                    tree = new SplayTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMap<float>>(tree); });
                }

                // Long

                {
                    SplayTreeRangeMapLong<float> tree;
                    tree = new SplayTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMapLong<float>>(tree); });
                }


                //
                // RangeList
                //

                // Int32

                {
                    SplayTreeRangeList tree;
                    tree = new SplayTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeList>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    SplayTreeArrayRangeList tree;
                    tree = new SplayTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeList>(tree); });
                }

                // Long

                {
                    SplayTreeRangeListLong tree;
                    tree = new SplayTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new SplayTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new SplayTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeListLong>(tree); });
                }
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

                {
                    RedBlackTreeMap<int, float> tree;
                    tree = new RedBlackTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayMap<int, float> tree;
                    tree = new RedBlackTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMap<int, float>>(tree); });
                }

                //
                // Set
                //

                {
                    RedBlackTreeList<int> tree;
                    tree = new RedBlackTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return new DefaultEnumeratorProxy<EntryList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayList<int> tree;
                    tree = new RedBlackTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return new DefaultEnumeratorProxy<EntryList<int>>(tree); });
                }


                //
                // RankMap
                //

                // Int32

                {
                    RedBlackTreeRankMap<int, float> tree;
                    tree = new RedBlackTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayRankMap<int, float> tree;
                    tree = new RedBlackTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMap<int, float>>(tree); });
                }

                // Long

                {
                    RedBlackTreeRankMapLong<int, float> tree;
                    tree = new RedBlackTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMapLong<int, float>>(tree); });
                }


                //
                // RankList
                //

                {
                    RedBlackTreeRankList<int> tree;
                    tree = new RedBlackTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayRankList<int> tree;
                    tree = new RedBlackTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankList<int>>(tree); });
                }

                // Long

                {
                    RedBlackTreeRankListLong<int> tree;
                    tree = new RedBlackTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankListLong<int>>(tree); });
                }


                //
                // MultiRankMap
                //

                // Int32

                {
                    RedBlackTreeMultiRankMap<int, float> tree;
                    tree = new RedBlackTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayMultiRankMap<int, float> tree;
                    tree = new RedBlackTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMap<int, float>>(tree); });
                }

                // Long

                {
                    RedBlackTreeMultiRankMapLong<int, float> tree;
                    tree = new RedBlackTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMapLong<int, float>>(tree); });
                }


                //
                // MultiRankList
                //

                // Int32

                {
                    RedBlackTreeMultiRankList<int> tree;
                    tree = new RedBlackTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayMultiRankList<int> tree;
                    tree = new RedBlackTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankList<int>>(tree); });
                }

                // Long

                {
                    RedBlackTreeMultiRankListLong<int> tree;
                    tree = new RedBlackTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankListLong<int>>(tree); });
                }


                //
                // Range2Map
                //

                // Int32

                {
                    RedBlackTreeRange2Map<float> tree;
                    tree = new RedBlackTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2Map<float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayRange2Map<float> tree;
                    tree = new RedBlackTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2Map<float>>(tree); });
                }

                // Long

                {
                    RedBlackTreeRange2MapLong<float> tree;
                    tree = new RedBlackTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2MapLong<float>>(tree); });
                }


                //
                // Range2List
                //

                // Int32

                {
                    RedBlackTreeRange2List tree;
                    tree = new RedBlackTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2List>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayRange2List tree;
                    tree = new RedBlackTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2List>(tree); });
                }

                // Long

                {
                    RedBlackTreeRange2ListLong tree;
                    tree = new RedBlackTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2ListLong>(tree); });
                }


                //
                // RangeMap
                //

                // Int32

                {
                    RedBlackTreeRangeMap<float> tree;
                    tree = new RedBlackTreeRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRangeMap<float>(0, AllocationMode.DynamicRetainFreelist);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMap<float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayRangeMap<float> tree;
                    tree = new RedBlackTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMap<float>>(tree); });
                }

                // Long

                {
                    RedBlackTreeRangeMapLong<float> tree;
                    tree = new RedBlackTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMapLong<float>>(tree); });
                }


                //
                // RangeList
                //

                // Int32

                {
                    RedBlackTreeRangeList tree;
                    tree = new RedBlackTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeList>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    RedBlackTreeArrayRangeList tree;
                    tree = new RedBlackTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeList>(tree); });
                }

                // Long

                {
                    RedBlackTreeRangeListLong tree;
                    tree = new RedBlackTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new RedBlackTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new RedBlackTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeListLong>(tree); });
                }
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

                {
                    AVLTreeMap<int, float> tree;
                    tree = new AVLTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayMap<int, float> tree;
                    tree = new AVLTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayMap<int, float>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryMap<int, float>>(
                        tree, Kind.Map, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMap<int, float>>(tree); });
                }

                //
                // Set
                //

                {
                    AVLTreeList<int> tree;
                    tree = new AVLTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return new DefaultEnumeratorProxy<EntryList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayList<int> tree;
                    tree = new AVLTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Robust, MakeIntKey, null, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayList<int>(capacity, allocationMode);
                    TestMapOrList<int, float, EntryList<int>>(
                        tree, Kind.List, EnumKind.Fast, MakeIntKey, null, delegate () { return new DefaultEnumeratorProxy<EntryList<int>>(tree); });
                }


                //
                // RankMap
                //

                // Int32

                {
                    AVLTreeRankMap<int, float> tree;
                    tree = new AVLTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayRankMap<int, float> tree;
                    tree = new AVLTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMap<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMap<int, float>>(tree); });
                }

                // Long

                {
                    AVLTreeRankMapLong<int, float> tree;
                    tree = new AVLTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankMapLong<int, float>>(tree); });
                }


                //
                // RankList
                //

                {
                    AVLTreeRankList<int> tree;
                    tree = new AVLTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayRankList<int> tree;
                    tree = new AVLTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankList<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankList<int>>(tree); });
                }

                // Long

                {
                    AVLTreeRankListLong<int> tree;
                    tree = new AVLTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryRankListLong<int>>(
                        tree, Kind.List, RankKind.Rank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRankListLong<int>>(tree); });
                }


                //
                // MultiRankMap
                //

                // Int32

                {
                    AVLTreeMultiRankMap<int, float> tree;
                    tree = new AVLTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMap<int, float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayMultiRankMap<int, float> tree;
                    tree = new AVLTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayMultiRankMap<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMap<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMap<int, float>>(tree); });
                }

                // Long

                {
                    AVLTreeMultiRankMapLong<int, float> tree;
                    tree = new AVLTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeMultiRankMapLong<int, float>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankMapLong<int, float>>(
                        tree, Kind.Map, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankMapLong<int, float>>(tree); });
                }


                //
                // MultiRankList
                //

                // Int32

                {
                    AVLTreeMultiRankList<int> tree;
                    tree = new AVLTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankList<int>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayMultiRankList<int> tree;
                    tree = new AVLTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayMultiRankList<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankList<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankList<int>>(tree); });
                }

                // Long

                {
                    AVLTreeMultiRankListLong<int> tree;
                    tree = new AVLTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Robust, MakeIntKey, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeMultiRankListLong<int>(capacity, allocationMode);
                    TestRankMapOrSet<int, float, EntryMultiRankListLong<int>>(
                        tree, Kind.List, RankKind.MultiRank, EnumKind.Fast, MakeIntKey, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryMultiRankListLong<int>>(tree); });
                }


                //
                // Range2Map
                //

                // Int32

                {
                    AVLTreeRange2Map<float> tree;
                    tree = new AVLTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2Map<float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayRange2Map<float> tree;
                    tree = new AVLTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayRange2Map<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2Map<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2Map<float>>(tree); });
                }

                // Long

                {
                    AVLTreeRange2MapLong<float> tree;
                    tree = new AVLTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRange2MapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2MapLong<float>>(
                        tree, Kind.Map, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2MapLong<float>>(tree); });
                }


                //
                // Range2List
                //

                // Int32

                {
                    AVLTreeRange2List tree;
                    tree = new AVLTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2List>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayRange2List tree;
                    tree = new AVLTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayRange2List(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2List>(
                        tree, Kind.List, RangeKind.Range2, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2List>(tree); });
                }

                // Long

                {
                    AVLTreeRange2ListLong tree;
                    tree = new AVLTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRange2ListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRange2ListLong>(
                        tree, Kind.List, RangeKind.Range2, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRange2ListLong>(tree); });
                }


                //
                // RangeMap
                //

                // Int32

                {
                    AVLTreeRangeMap<float> tree;
                    tree = new AVLTreeRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMap<float>>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayRangeMap<float> tree;
                    tree = new AVLTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayRangeMap<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMap<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMap<float>>(tree); });
                }

                // Long

                {
                    AVLTreeRangeMapLong<float> tree;
                    tree = new AVLTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRangeMapLong<float>(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeMapLong<float>>(
                        tree, Kind.Map, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeMapLong<float>>(tree); });
                }


                //
                // RangeList
                //

                // Int32

                {
                    AVLTreeRangeList tree;
                    tree = new AVLTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeList>(tree); });
                }

                if (allocationMode != AllocationMode.DynamicDiscard)
                {
                    AVLTreeArrayRangeList tree;
                    tree = new AVLTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeArrayRangeList(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeList>(
                        tree, Kind.List, RangeKind.Range, Width.Int, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeList>(tree); });
                }

                // Long

                {
                    AVLTreeRangeListLong tree;
                    tree = new AVLTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Robust, MakeFloatValue, delegate () { return tree.GetRobustEnumerable(); });
                    tree = new AVLTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return tree.GetFastEnumerable(); });
                    tree = new AVLTreeRangeListLong(capacity, allocationMode);
                    TestRangeMapOrList<float, EntryRangeListLong>(
                        tree, Kind.List, RangeKind.Range, Width.Long, EnumKind.Fast, MakeFloatValue, delegate () { return new DefaultEnumeratorProxy<EntryRangeListLong>(tree); });
                }
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

            //TestEntryComparisons(typeof(EntryRangeList));
            //TestEntryComparisons(typeof(EntryRangeListLong));
            TestEntryComparisons(typeof(EntryRangeMap<string>));
            TestEntryComparisons(typeof(EntryRangeMapLong<string>));

            //TestEntryComparisons(typeof(EntryRange2List));
            //TestEntryComparisons(typeof(EntryRange2ListLong));
            TestEntryComparisons(typeof(EntryRange2Map<string>));
            TestEntryComparisons(typeof(EntryRange2MapLong<string>));
        }

        public override bool Do()
        {
            try
            {
                TestAVLTree();

                TestRedBlackTree();

                TestSplayTree();


                TestAllEntryComparisons();


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
