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
    public class UnitTestClone : TestBase
    {
        public UnitTestClone()
            : base()
        {
        }

        public UnitTestClone(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private const int Seed = 1;

        private readonly ParkAndMiller random = new ParkAndMiller(Seed);

        private const int MaximumTestCaseSize = 100;
        private readonly static int[] TestCaseSizes = new int[] { 0, 1, 2, 3, 5, 10, 20, 50, 100 };

        private delegate KeyType MakeNewKey<KeyType>(ParkAndMiller random, KeyType[] notFromThese);
        private delegate ValueType MakeNewValue<ValueType>(ParkAndMiller random);

        private static int MakeIntKey(ParkAndMiller random, int[] notFromThese)
        {
            int key;
            do
            {
                key = random.Next();
            }
            while (Array.BinarySearch(notFromThese, key, Comparer<int>.Default) >= 0);
            return key;
        }

        private static float MakeFloatValue(ParkAndMiller random)
        {
            return .01f * random.Next();
        }



        //
        // Keyed test support (Map/List, with Rank & Multi-Rank variations)
        //

        [Flags]
        private enum KeyAxis { None = 0, Value = 1, Rank = 2, Count = 4 };

        private enum CloneMethod { Constructor, IClonable };

        private abstract class KeyedTestHarness<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public abstract object Tree { get; }

            public abstract void Add(KeyType key, ValueType value, int count);
            public abstract bool ContainsKey(KeyType key);
            public abstract void Remove(KeyType key);

            public abstract KeyedTestHarness<KeyType, ValueType> Clone(CloneMethod method);

            public abstract MultiRankMapEntry[] ToArray();
        }

        private void ValidateKeyedEquivalence<KeyType, ValueType>(MultiRankMapEntry[] items1, MultiRankMapEntry[] items2, KeyAxis axes) where KeyType : IComparable<KeyType>
        {
            TestTrue("KeyedTest compare", delegate () { return items1.Length == items2.Length; });
            for (int j = 0; j < items1.Length; j++)
            {
                TestTrue("KeyedTest compare key", delegate () { return 0 == Comparer<KeyType>.Default.Compare((KeyType)items1[j].key, (KeyType)items2[j].key); });
                if ((axes & KeyAxis.Value) != 0)
                {
                    TestTrue("KeyedTest compare value", delegate () { return 0 == Comparer<ValueType>.Default.Compare((ValueType)items1[j].value, (ValueType)items2[j].value); });
                }
                if ((axes & KeyAxis.Rank) != 0)
                {
                    TestTrue("KeyedTest compare rank", delegate () { return items1[j].rank.start == items2[j].rank.start; });
                }
                if ((axes & KeyAxis.Count) != 0)
                {
                    TestTrue("KeyedTest compare rank count", delegate () { return items1[j].rank.length == items2[j].rank.length; });
                }
            }
        }

        private delegate KeyedTestHarness<KeyType, ValueType> MakeTree<KeyType, ValueType>() where KeyType : IComparable<KeyType>;
        private void KeyedTest<KeyType, ValueType>(
            MakeTree<KeyType, ValueType> makeTree,
            MakeTree<KeyType, ValueType> makeReference,
            MakeNewKey<KeyType> makeKey,
            MakeNewValue<ValueType> makeValue,
            KeyAxis axes) where KeyType : IComparable<KeyType>
        {
            foreach (CloneMethod cloneMethod in new CloneMethod[] { CloneMethod.Constructor, CloneMethod.IClonable })
            {
                foreach (int i in TestCaseSizes)
                {
                    KeyedTestHarness<KeyType, ValueType> reference1 = makeReference();
                    KeyedTestHarness<KeyType, ValueType> reference2 = makeReference();
                    KeyedTestHarness<KeyType, ValueType> tree1 = makeTree();

                    Tally(tree1);

                    // create initial data
                    for (int j = 0; j < i; j++)
                    {
                        IncrementIteration();

                        KeyType[] notFromThese = Array.ConvertAll(reference1.ToArray(), delegate (MultiRankMapEntry entry) { return (KeyType)entry.key; });
                        KeyType key = makeKey(random, notFromThese);

                        ValueType value = default(ValueType);
                        if ((axes & KeyAxis.Value) != 0)
                        {
                            value = makeValue(random);
                        }

                        int count = 0;
                        if ((axes & KeyAxis.Count) != 0)
                        {
                            count = random.Next() % 100 + 1;
                        }

                        reference1.Add(key, value, count);
                        reference2.Add(key, value, count);
                        tree1.Add(key, value, count);
                    }
                    IncrementIteration();

                    // verify trees are equivalent
                    {
                        MultiRankMapEntry[] reference1Items = reference1.ToArray();
                        MultiRankMapEntry[] tree1Items = tree1.ToArray();
                        ValidateKeyedEquivalence<KeyType, ValueType>(reference1Items, tree1Items, axes);
                    }

                    // clone
                    KeyedTestHarness<KeyType, ValueType> tree2 = tree1.Clone(cloneMethod);

                    // make some updates
                    for (int j = 0; j < i; j++)
                    {
                        KeyType[] notFromThese;
                        KeyType key;
                        ValueType value;
                        int count;

                        IncrementIteration();


                        // modify originals

                        notFromThese = Array.ConvertAll(reference1.ToArray(), delegate (MultiRankMapEntry entry) { return (KeyType)entry.key; });
                        key = notFromThese[random.Next() % notFromThese.Length];

                        reference1.Remove(key);
                        tree1.Remove(key);

                        key = makeKey(random, notFromThese);

                        value = default(ValueType);
                        if ((axes & KeyAxis.Value) != 0)
                        {
                            value = makeValue(random);
                        }

                        count = 0;
                        if ((axes & KeyAxis.Count) != 0)
                        {
                            count = random.Next() % 100 + 1;
                        }

                        reference1.Add(key, value, count);
                        tree1.Add(key, value, count);


                        // modify copies

                        notFromThese = Array.ConvertAll(reference2.ToArray(), delegate (MultiRankMapEntry entry) { return (KeyType)entry.key; });
                        key = notFromThese[random.Next() % notFromThese.Length];

                        reference2.Remove(key);
                        tree2.Remove(key);

                        key = makeKey(random, notFromThese);

                        value = default(ValueType);
                        if ((axes & KeyAxis.Value) != 0)
                        {
                            value = makeValue(random);
                        }

                        count = 0;
                        if ((axes & KeyAxis.Count) != 0)
                        {
                            count = random.Next() % 100 + 1;
                        }

                        reference2.Add(key, value, count);
                        tree2.Add(key, value, count);
                    }
                    IncrementIteration();

                    // verify tree1 and reference1 are the same
                    {
                        MultiRankMapEntry[] referenceItems = reference1.ToArray();
                        MultiRankMapEntry[] treeItems = tree1.ToArray();
                        ValidateKeyedEquivalence<KeyType, ValueType>(referenceItems, treeItems, axes);
                    }

                    // verify tree2 and reference2 are the same
                    {
                        MultiRankMapEntry[] referenceItems = reference2.ToArray();
                        MultiRankMapEntry[] treeItems = tree2.ToArray();
                        ValidateKeyedEquivalence<KeyType, ValueType>(referenceItems, treeItems, axes);
                    }

                    // check for allocation overflow equivalency
                    {
                        List<KeyType> notFromThese = new List<KeyType>(Array.ConvertAll(reference1.ToArray(), delegate (MultiRankMapEntry entry) { return (KeyType)entry.key; }));
                        notFromThese.AddRange(Array.ConvertAll(reference2.ToArray(), delegate (MultiRankMapEntry entry) { return (KeyType)entry.key; }));
                        KeyType key = makeKey(random, notFromThese.ToArray());

                        bool tree1Succeeded = true;
                        try
                        {
                            tree1.Add(key, default(ValueType), 1);
                        }
                        catch (OutOfMemoryException)
                        {
                            tree1Succeeded = false;
                        }

                        bool tree2Succeeded = true;
                        try
                        {
                            tree2.Add(key, default(ValueType), 1);
                        }
                        catch (OutOfMemoryException)
                        {
                            tree2Succeeded = false;
                        }

                        TestTrue("clone capacity limit", delegate () { return tree1Succeeded == tree2Succeeded; });
                    }
                }
            }
        }

        //

        private abstract class MapListTestHarness<KeyType, ValueType> : KeyedTestHarness<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            protected static MultiRankMapEntry[] ToArray(object tree, bool propagateValue)
            {
                KeyValuePair<KeyType, ValueType>[] items;

                ISimpleTreeInspection<KeyType, ValueType> simple;
                INonInvasiveTreeInspection nonInvasive;

                if ((simple = tree as ISimpleTreeInspection<KeyType, ValueType>) != null)
                {
                    items = simple.ToArray();
                }
                else if ((nonInvasive = tree as INonInvasiveTreeInspection) != null)
                {
                    items = TreeInspection.Flatten<KeyType, ValueType>(nonInvasive, propagateValue);
                }
                else
                {
                    throw new ArgumentException();
                }

                MultiRankMapEntry[] ranks = new MultiRankMapEntry[items.Length];
                for (int i = 0; i < ranks.Length; i++)
                {
                    ranks[i].key = items[i].Key;
                    ranks[i].value = items[i].Value;
                }
                return ranks;
            }
        }

        private delegate IOrderedMap<KeyType, ValueType> MapCloneMethod<KeyType, ValueType>(IOrderedMap<KeyType, ValueType> tree) where KeyType : IComparable<KeyType>;
        private class MapTestHarness<KeyType, ValueType> : MapListTestHarness<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            private readonly IOrderedMap<KeyType, ValueType> tree;
            private readonly MapCloneMethod<KeyType, ValueType> clone;

            public MapTestHarness(IOrderedMap<KeyType, ValueType> tree, MapCloneMethod<KeyType, ValueType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override void Add(KeyType key, ValueType value, int count)
            {
                tree.Add(key, value);
            }

            public override bool ContainsKey(KeyType key)
            {
                return tree.ContainsKey(key);
            }

            public override void Remove(KeyType key)
            {
                tree.Remove(key);
            }

            public override KeyedTestHarness<KeyType, ValueType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new MapTestHarness<KeyType, ValueType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new MapTestHarness<KeyType, ValueType>((IOrderedMap<KeyType, ValueType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override MultiRankMapEntry[] ToArray()
            {
                return ToArray(tree, true/*propagateValue*/);
            }
        }

        private delegate IOrderedList<KeyType> ListCloneMethod<KeyType>(IOrderedList<KeyType> tree) where KeyType : IComparable<KeyType>;
        private class ListTestHarness<KeyType> : MapListTestHarness<KeyType, KeyType> where KeyType : IComparable<KeyType>
        {
            private readonly IOrderedList<KeyType> tree;
            private readonly ListCloneMethod<KeyType> clone;

            public ListTestHarness(IOrderedList<KeyType> tree, ListCloneMethod<KeyType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override void Add(KeyType key, KeyType value, int count)
            {
                tree.Add(key);
            }

            public override bool ContainsKey(KeyType key)
            {
                return tree.ContainsKey(key);
            }

            public override void Remove(KeyType key)
            {
                tree.Remove(key);
            }

            public override KeyedTestHarness<KeyType, KeyType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new ListTestHarness<KeyType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new ListTestHarness<KeyType>((IOrderedList<KeyType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override MultiRankMapEntry[] ToArray()
            {
                return ToArray(tree, false/*propagateValue*/);
            }
        }

        //

        private abstract class RankMapListTestHarness<KeyType, ValueType> : KeyedTestHarness<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            protected static MultiRankMapEntry[] ToArray(object tree, bool propagateValue)
            {
                INonInvasiveMultiRankMapInspection inspection = (INonInvasiveMultiRankMapInspection)tree;

                MultiRankMapEntry[] ranks = inspection.GetRanks();

                return ranks;
            }
        }

        private delegate IRankMap<KeyType, ValueType> RankMapCloneMethod<KeyType, ValueType>(IRankMap<KeyType, ValueType> tree) where KeyType : IComparable<KeyType>;
        private class RankMapTestHarness<KeyType, ValueType> : RankMapListTestHarness<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            private readonly IRankMap<KeyType, ValueType> tree;
            private readonly RankMapCloneMethod<KeyType, ValueType> clone;

            public RankMapTestHarness(IRankMap<KeyType, ValueType> tree, RankMapCloneMethod<KeyType, ValueType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override void Add(KeyType key, ValueType value, int count)
            {
                tree.Add(key, value);
            }

            public override bool ContainsKey(KeyType key)
            {
                return tree.ContainsKey(key);
            }

            public override void Remove(KeyType key)
            {
                tree.Remove(key);
            }

            public override KeyedTestHarness<KeyType, ValueType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new RankMapTestHarness<KeyType, ValueType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new RankMapTestHarness<KeyType, ValueType>((IRankMap<KeyType, ValueType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override MultiRankMapEntry[] ToArray()
            {
                return ToArray(tree, true/*propagateValue*/);
            }
        }

        private delegate IRankList<KeyType> RankListCloneMethod<KeyType>(IRankList<KeyType> tree) where KeyType : IComparable<KeyType>;
        private class RankListTestHarness<KeyType> : RankMapListTestHarness<KeyType, KeyType> where KeyType : IComparable<KeyType>
        {
            private readonly IRankList<KeyType> tree;
            private readonly RankListCloneMethod<KeyType> clone;

            public RankListTestHarness(IRankList<KeyType> tree, RankListCloneMethod<KeyType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override void Add(KeyType key, KeyType value, int count)
            {
                tree.Add(key);
            }

            public override bool ContainsKey(KeyType key)
            {
                return tree.ContainsKey(key);
            }

            public override void Remove(KeyType key)
            {
                tree.Remove(key);
            }

            public override KeyedTestHarness<KeyType, KeyType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new RankListTestHarness<KeyType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new RankListTestHarness<KeyType>((IRankList<KeyType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override MultiRankMapEntry[] ToArray()
            {
                return ToArray(tree, false/*propagateValue*/);
            }
        }

        //

        private abstract class MultiRankMapListTestHarness<KeyType, ValueType> : KeyedTestHarness<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            protected static MultiRankMapEntry[] ToArray(object tree, bool propagateValue)
            {
                INonInvasiveMultiRankMapInspection inspection = (INonInvasiveMultiRankMapInspection)tree;

                MultiRankMapEntry[] ranks = inspection.GetRanks();

                return ranks;
            }
        }

        private delegate IMultiRankMap<KeyType, ValueType> MultiRankMapCloneMethod<KeyType, ValueType>(IMultiRankMap<KeyType, ValueType> tree) where KeyType : IComparable<KeyType>;
        private class MultiRankMapTestHarness<KeyType, ValueType> : MultiRankMapListTestHarness<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            private readonly IMultiRankMap<KeyType, ValueType> tree;
            private readonly MultiRankMapCloneMethod<KeyType, ValueType> clone;

            public MultiRankMapTestHarness(IMultiRankMap<KeyType, ValueType> tree, MultiRankMapCloneMethod<KeyType, ValueType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override void Add(KeyType key, ValueType value, int count)
            {
                tree.Add(key, value, count);
            }

            public override bool ContainsKey(KeyType key)
            {
                return tree.ContainsKey(key);
            }

            public override void Remove(KeyType key)
            {
                tree.Remove(key);
            }

            public override KeyedTestHarness<KeyType, ValueType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new MultiRankMapTestHarness<KeyType, ValueType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new MultiRankMapTestHarness<KeyType, ValueType>((IMultiRankMap<KeyType, ValueType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override MultiRankMapEntry[] ToArray()
            {
                return ToArray(tree, true/*propagateValue*/);
            }
        }

        private delegate IMultiRankList<KeyType> MultiRankListCloneMethod<KeyType>(IMultiRankList<KeyType> tree) where KeyType : IComparable<KeyType>;
        private class MultiRankListTestHarness<KeyType> : MultiRankMapListTestHarness<KeyType, KeyType> where KeyType : IComparable<KeyType>
        {
            private readonly IMultiRankList<KeyType> tree;
            private readonly MultiRankListCloneMethod<KeyType> clone;

            public MultiRankListTestHarness(IMultiRankList<KeyType> tree, MultiRankListCloneMethod<KeyType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override void Add(KeyType key, KeyType value, int count)
            {
                tree.Add(key, count);
            }

            public override bool ContainsKey(KeyType key)
            {
                return tree.ContainsKey(key);
            }

            public override void Remove(KeyType key)
            {
                tree.Remove(key);
            }

            public override KeyedTestHarness<KeyType, KeyType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new MultiRankListTestHarness<KeyType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new MultiRankListTestHarness<KeyType>((IMultiRankList<KeyType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override MultiRankMapEntry[] ToArray()
            {
                return ToArray(tree, false/*propagateValue*/);
            }
        }



        //
        // Range test support (Range2 & Range map/list)
        //

        [Flags]
        private enum RangeAxis { None = 0, Value = 1, Range2 = 2 };

        private abstract class RangeTestHarness<ValueType>
        {
            public abstract object Tree { get; }

            public abstract int Extent { get; }
            public abstract void Insert(int xStart, int xLength, int yLength, ValueType value);
            public abstract bool NearestLessOrEqual(int xStart, out int nearestXStart);
            public abstract void Delete(int xStart);

            public abstract RangeTestHarness<ValueType> Clone(CloneMethod method);

            public abstract Range2MapEntry[] ToArray();
        }

        private void ValidateRangeEquivalence<ValueType>(Range2MapEntry[] items1, Range2MapEntry[] items2, RangeAxis axes)
        {
            TestTrue("RangeTest compare", delegate () { return items1.Length == items2.Length; });
            for (int j = 0; j < items1.Length; j++)
            {
                TestTrue("RangeTest compare x.start", delegate () { return items1[j].x.start == items2[j].x.start; });
                TestTrue("RangeTest compare x.length", delegate () { return items1[j].x.length == items2[j].x.length; });
                if ((axes & RangeAxis.Value) != 0)
                {
                    TestTrue("RangeTest compare value", delegate () { return 0 == Comparer<ValueType>.Default.Compare((ValueType)items1[j].value, (ValueType)items2[j].value); });
                }
                if ((axes & RangeAxis.Range2) != 0)
                {
                    TestTrue("RangeTest compare y.start", delegate () { return items1[j].y.start == items2[j].y.start; });
                    TestTrue("RangeTest compare y.length", delegate () { return items1[j].y.length == items2[j].y.length; });
                }
            }
        }

        private delegate RangeTestHarness<ValueType> MakeTree<ValueType>();
        private void RangeTest<ValueType>(
            MakeTree<ValueType> makeTree,
            MakeTree<ValueType> makeReference,
            MakeNewValue<ValueType> makeValue,
            RangeAxis axes)
        {
            foreach (CloneMethod cloneMethod in new CloneMethod[] { CloneMethod.Constructor, CloneMethod.IClonable })
            {
                foreach (int i in TestCaseSizes)
                {
                    RangeTestHarness<ValueType> reference1 = makeReference();
                    RangeTestHarness<ValueType> reference2 = makeReference();
                    RangeTestHarness<ValueType> tree1 = makeTree();

                    Tally(tree1);

                    // create initial data
                    for (int j = 0; j < i; j++)
                    {
                        IncrementIteration();

                        int xStart = reference1.Extent != 0 ? random.Next() % reference1.Extent : 0;
                        reference1.NearestLessOrEqual(xStart, out xStart);

                        ValueType value = default(ValueType);
                        if ((axes & RangeAxis.Value) != 0)
                        {
                            value = makeValue(random);
                        }

                        int xLength = random.Next() % 100 + 1;

                        int yLength = 0;
                        if ((axes & RangeAxis.Range2) != 0)
                        {
                            yLength = random.Next() % 100 + 1;
                        }

                        reference1.Insert(xStart, xLength, yLength, value);
                        reference2.Insert(xStart, xLength, yLength, value);
                        tree1.Insert(xStart, xLength, yLength, value);
                    }
                    IncrementIteration();

                    // verify trees are equivalent
                    {
                        Range2MapEntry[] referenceItems = reference1.ToArray();
                        Range2MapEntry[] treeItems = tree1.ToArray();
                        ValidateRangeEquivalence<ValueType>(referenceItems, treeItems, axes);
                    }

                    // clone
                    RangeTestHarness<ValueType> tree2 = tree1.Clone(cloneMethod);

                    // make some updates
                    for (int j = 0; j < i; j++)
                    {
                        int xStart, xLength, yLength;
                        ValueType value;

                        IncrementIteration();


                        // modify originals

                        xStart = random.Next() % reference1.Extent;
                        reference1.NearestLessOrEqual(xStart, out xStart);

                        reference1.Delete(xStart);
                        tree1.Delete(xStart);

                        xStart = reference1.Extent != 0 ? random.Next() % reference1.Extent : 0;
                        reference1.NearestLessOrEqual(xStart, out xStart);

                        value = default(ValueType);
                        if ((axes & RangeAxis.Value) != 0)
                        {
                            value = makeValue(random);
                        }

                        xLength = random.Next() % 100 + 1;

                        yLength = 0;
                        if ((axes & RangeAxis.Range2) != 0)
                        {
                            yLength = random.Next() % 100 + 1;
                        }

                        reference1.Insert(xStart, xLength, yLength, value);
                        tree1.Insert(xStart, xLength, yLength, value);


                        // modify copies

                        xStart = random.Next() % reference2.Extent;
                        reference2.NearestLessOrEqual(xStart, out xStart);

                        reference2.Delete(xStart);
                        tree2.Delete(xStart);

                        xStart = reference2.Extent != 0 ? random.Next() % reference2.Extent : 0;
                        reference2.NearestLessOrEqual(xStart, out xStart);

                        value = default(ValueType);
                        if ((axes & RangeAxis.Value) != 0)
                        {
                            value = makeValue(random);
                        }

                        xLength = random.Next() % 100 + 1;

                        yLength = 0;
                        if ((axes & RangeAxis.Range2) != 0)
                        {
                            yLength = random.Next() % 100 + 1;
                        }

                        reference2.Insert(xStart, xLength, yLength, value);
                        tree2.Insert(xStart, xLength, yLength, value);
                    }
                    IncrementIteration();

                    // verify tree1 and reference1 are the same
                    {
                        Range2MapEntry[] referenceItems = reference1.ToArray();
                        Range2MapEntry[] treeItems = tree1.ToArray();
                        ValidateRangeEquivalence<ValueType>(referenceItems, treeItems, axes);
                    }

                    // verify tree2 and reference2 are the same
                    {
                        Range2MapEntry[] referenceItems = reference2.ToArray();
                        Range2MapEntry[] treeItems = tree2.ToArray();
                        ValidateRangeEquivalence<ValueType>(referenceItems, treeItems, axes);
                    }

                    // check for allocation overflow equivalency
                    {
                        bool tree1Succeeded = true;
                        try
                        {
                            tree1.Insert(0, 1, 1, default(ValueType));
                        }
                        catch (OutOfMemoryException)
                        {
                            tree1Succeeded = false;
                        }

                        bool tree2Succeeded = true;
                        try
                        {
                            tree2.Insert(0, 1, 1, default(ValueType));
                        }
                        catch (OutOfMemoryException)
                        {
                            tree2Succeeded = false;
                        }

                        TestTrue("clone capacity limit", delegate () { return tree1Succeeded == tree2Succeeded; });
                    }
                }
            }
        }

        //

        private delegate IRangeMap<ValueType> RangeMapCloneMethod<ValueType>(IRangeMap<ValueType> tree);
        private class RangeMapTestHarness<ValueType> : RangeTestHarness<ValueType>
        {
            private readonly IRangeMap<ValueType> tree;
            private readonly RangeMapCloneMethod<ValueType> clone;

            public RangeMapTestHarness(IRangeMap<ValueType> tree, RangeMapCloneMethod<ValueType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override int Extent { get { return tree.GetExtent(); } }

            public override void Insert(int xStart, int xLength, int yLength, ValueType value)
            {
                tree.Insert(xStart, xLength, value);
            }

            public override bool NearestLessOrEqual(int xStart, out int nearestXStart)
            {
                return tree.NearestLessOrEqual(xStart, out nearestXStart);
            }

            public override void Delete(int xStart)
            {
                tree.Delete(xStart);
            }

            public override RangeTestHarness<ValueType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new RangeMapTestHarness<ValueType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new RangeMapTestHarness<ValueType>((IRangeMap<ValueType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override Range2MapEntry[] ToArray()
            {
                return ((INonInvasiveRange2MapInspection)tree).GetRanges();
            }
        }

        //

        private delegate IRangeList RangeListCloneMethod(IRangeList tree);
        private class RangeListTestHarness : RangeTestHarness<object>
        {
            private readonly IRangeList tree;
            private readonly RangeListCloneMethod clone;

            public RangeListTestHarness(IRangeList tree, RangeListCloneMethod clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override int Extent { get { return tree.GetExtent(); } }

            public override void Insert(int xStart, int xLength, int yLength, object value)
            {
                tree.Insert(xStart, xLength);
            }

            public override bool NearestLessOrEqual(int xStart, out int nearestXStart)
            {
                return tree.NearestLessOrEqual(xStart, out nearestXStart);
            }

            public override void Delete(int xStart)
            {
                tree.Delete(xStart);
            }

            public override RangeTestHarness<object> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new RangeListTestHarness(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new RangeListTestHarness((IRangeList)((ICloneable)tree).Clone(), clone);
                }
            }

            public override Range2MapEntry[] ToArray()
            {
                return ((INonInvasiveRange2MapInspection)tree).GetRanges();
            }
        }

        //

        private delegate IRange2Map<ValueType> Range2MapCloneMethod<ValueType>(IRange2Map<ValueType> tree);
        private class Range2MapTestHarness<ValueType> : RangeTestHarness<ValueType>
        {
            private readonly IRange2Map<ValueType> tree;
            private readonly Range2MapCloneMethod<ValueType> clone;

            public Range2MapTestHarness(IRange2Map<ValueType> tree, Range2MapCloneMethod<ValueType> clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override int Extent { get { return tree.GetExtent(Side.X); } }

            public override void Insert(int xStart, int xLength, int yLength, ValueType value)
            {
                tree.Insert(xStart, Side.X, xLength, yLength, value);
            }

            public override bool NearestLessOrEqual(int xStart, out int nearestXStart)
            {
                return tree.NearestLessOrEqual(xStart, Side.X, out nearestXStart);
            }

            public override void Delete(int xStart)
            {
                tree.Delete(xStart, Side.X);
            }

            public override RangeTestHarness<ValueType> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new Range2MapTestHarness<ValueType>(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new Range2MapTestHarness<ValueType>((IRange2Map<ValueType>)((ICloneable)tree).Clone(), clone);
                }
            }

            public override Range2MapEntry[] ToArray()
            {
                return ((INonInvasiveRange2MapInspection)tree).GetRanges();
            }
        }

        //

        private delegate IRange2List Range2ListCloneMethod(IRange2List tree);
        private class Range2ListTestHarness : RangeTestHarness<object>
        {
            private readonly IRange2List tree;
            private readonly Range2ListCloneMethod clone;

            public Range2ListTestHarness(IRange2List tree, Range2ListCloneMethod clone)
            {
                this.tree = tree;
                this.clone = clone;
            }

            public override object Tree { get { return tree; } }

            public override int Extent { get { return tree.GetExtent(Side.X); } }

            public override void Insert(int xStart, int xLength, int yLength, object value)
            {
                tree.Insert(xStart, Side.X, xLength, yLength);
            }

            public override bool NearestLessOrEqual(int xStart, out int nearestXStart)
            {
                return tree.NearestLessOrEqual(xStart, Side.X, out nearestXStart);
            }

            public override void Delete(int xStart)
            {
                tree.Delete(xStart, Side.X);
            }

            public override RangeTestHarness<object> Clone(CloneMethod method)
            {
                switch (method)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case CloneMethod.Constructor:
                        return new Range2ListTestHarness(clone(tree), clone);
                    case CloneMethod.IClonable:
                        return new Range2ListTestHarness((IRange2List)((ICloneable)tree).Clone(), clone);
                }
            }

            public override Range2MapEntry[] ToArray()
            {
                return ((INonInvasiveRange2MapInspection)tree).GetRanges();
            }
        }



        //
        // Main test driver
        //

        private int SplayCount = 0;
        private int RedBlackCount = 0;
        private int AVLCount = 0;

        private void Tally(object tree)
        {
            string typeName = tree.GetType().Name;
            if (typeName.Contains("Harness"))
            {
                object inner = tree.GetType().GetProperty("Tree").GetMethod.Invoke(tree, null);
                Tally(inner);
            }
            else if (typeName.Contains("Adapt"))
            {
                object inner = tree.GetType().GetProperty("Inner").GetMethod.Invoke(tree, null);
                Tally(inner);
            }
            else if (typeName.Contains("Splay"))
            {
                SplayCount++;
            }
            else if (typeName.Contains("RedBlack"))
            {
                RedBlackCount++;
            }
            else if (typeName.Contains("AVL"))
            {
                AVLCount++;
            }
            else
            {
                Debug.Assert(false, "tree type unknown");
            }
        }

        public override bool Do()
        {
            try
            {
                // test cloning for each allocation mode
                foreach (AllocationMode allocationMode in new AllocationMode[] { AllocationMode.DynamicDiscard, AllocationMode.DynamicRetainFreelist, AllocationMode.PreallocatedFixed })
                {
                    //
                    // Map
                    //

                    // Heap

                    KeyedTest(
                        delegate ()
                        {
                            return new MapTestHarness<int, float>(new AVLTreeMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IOrderedMap<int, float> tree)
                            {
                                return new AVLTreeMap<int, float>((AVLTreeMap<int, float>)tree);
                            });
                        },
                        delegate () { return new MapTestHarness<int, float>(new ReferenceMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value);

                    KeyedTest(
                        delegate ()
                        {
                            return new MapTestHarness<int, float>(new RedBlackTreeMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IOrderedMap<int, float> tree)
                            {
                                return new RedBlackTreeMap<int, float>((RedBlackTreeMap<int, float>)tree);
                            });
                        },
                        delegate () { return new MapTestHarness<int, float>(new ReferenceMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value);

                    KeyedTest(
                        delegate ()
                        {
                            return new MapTestHarness<int, float>(new SplayTreeMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IOrderedMap<int, float> tree)
                            {
                                return new SplayTreeMap<int, float>((SplayTreeMap<int, float>)tree);
                            });
                        },
                        delegate () { return new MapTestHarness<int, float>(new ReferenceMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        KeyedTest(
                            delegate ()
                            {
                                return new MapTestHarness<int, float>(new AVLTreeArrayMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IOrderedMap<int, float> tree)
                                {
                                    return new AVLTreeArrayMap<int, float>((AVLTreeArrayMap<int, float>)tree);
                                });
                            },
                            delegate () { return new MapTestHarness<int, float>(new ReferenceMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value);

                        KeyedTest(
                            delegate ()
                            {
                                return new MapTestHarness<int, float>(new RedBlackTreeArrayMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IOrderedMap<int, float> tree)
                                {
                                    return new RedBlackTreeArrayMap<int, float>((RedBlackTreeArrayMap<int, float>)tree);
                                });
                            },
                            delegate () { return new MapTestHarness<int, float>(new ReferenceMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value);

                        KeyedTest(
                            delegate ()
                            {
                                return new MapTestHarness<int, float>(new SplayTreeArrayMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IOrderedMap<int, float> tree)
                                {
                                    return new SplayTreeArrayMap<int, float>((SplayTreeArrayMap<int, float>)tree);
                                });
                            },
                            delegate () { return new MapTestHarness<int, float>(new ReferenceMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value);
                    }



                    //
                    // List
                    //

                    // Heap

                    KeyedTest(
                        delegate ()
                        {
                            return new ListTestHarness<int>(new AVLTreeList<int>(MaximumTestCaseSize, allocationMode), delegate (IOrderedList<int> tree)
                            {
                                return new AVLTreeList<int>((AVLTreeList<int>)tree);
                            });
                        },
                        delegate () { return new MapTestHarness<int, int>(new ReferenceMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.None);

                    KeyedTest(
                        delegate ()
                        {
                            return new ListTestHarness<int>(new RedBlackTreeList<int>(MaximumTestCaseSize, allocationMode), delegate (IOrderedList<int> tree)
                            {
                                return new RedBlackTreeList<int>((RedBlackTreeList<int>)tree);
                            });
                        },
                        delegate () { return new MapTestHarness<int, int>(new ReferenceMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.None);

                    KeyedTest(
                        delegate ()
                        {
                            return new ListTestHarness<int>(new SplayTreeList<int>(MaximumTestCaseSize, allocationMode), delegate (IOrderedList<int> tree)
                            {
                                return new SplayTreeList<int>((SplayTreeList<int>)tree);
                            });
                        },
                        delegate () { return new MapTestHarness<int, int>(new ReferenceMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.None);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        KeyedTest(
                            delegate ()
                            {
                                return new ListTestHarness<int>(new AVLTreeArrayList<int>(MaximumTestCaseSize, allocationMode), delegate (IOrderedList<int> tree)
                                {
                                    return new AVLTreeArrayList<int>((AVLTreeArrayList<int>)tree);
                                });
                            },
                            delegate () { return new MapTestHarness<int, int>(new ReferenceMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.None);

                        KeyedTest(
                            delegate ()
                            {
                                return new ListTestHarness<int>(new RedBlackTreeArrayList<int>(MaximumTestCaseSize, allocationMode), delegate (IOrderedList<int> tree)
                                {
                                    return new RedBlackTreeArrayList<int>((RedBlackTreeArrayList<int>)tree);
                                });
                            },
                            delegate () { return new MapTestHarness<int, int>(new ReferenceMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.None);

                        KeyedTest(
                            delegate ()
                            {
                                return new ListTestHarness<int>(new SplayTreeArrayList<int>(MaximumTestCaseSize, allocationMode), delegate (IOrderedList<int> tree)
                                {
                                    return new SplayTreeArrayList<int>((SplayTreeArrayList<int>)tree);
                                });
                            },
                            delegate () { return new MapTestHarness<int, int>(new ReferenceMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.None);
                    }



                    //
                    // Rank Map
                    //

                    // Int32

                    KeyedTest(
                        delegate ()
                        {
                            return new RankMapTestHarness<int, float>(new AVLTreeRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IRankMap<int, float> tree)
                            {
                                return new AVLTreeRankMap<int, float>((AVLTreeRankMap<int, float>)tree);
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankMapTestHarness<int, float>(new RedBlackTreeRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IRankMap<int, float> tree)
                            {
                                return new RedBlackTreeRankMap<int, float>((RedBlackTreeRankMap<int, float>)tree);
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankMapTestHarness<int, float>(new SplayTreeRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IRankMap<int, float> tree)
                            {
                                return new SplayTreeRankMap<int, float>((SplayTreeRankMap<int, float>)tree);
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank);

                    // Long

                    KeyedTest(
                        delegate ()
                        {
                            return new RankMapTestHarness<int, float>(new AdaptRankMapToRankMapLong<int, float>(new AVLTreeRankMapLong<int, float>(MaximumTestCaseSize, allocationMode)), delegate (IRankMap<int, float> tree)
                            {
                                return new AdaptRankMapToRankMapLong<int, float>(new AVLTreeRankMapLong<int, float>((AVLTreeRankMapLong<int, float>)(((AdaptRankMapToRankMapLong<int, float>)tree).Inner)));
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankMapTestHarness<int, float>(new AdaptRankMapToRankMapLong<int, float>(new RedBlackTreeRankMapLong<int, float>(MaximumTestCaseSize, allocationMode)), delegate (IRankMap<int, float> tree)
                            {
                                return new AdaptRankMapToRankMapLong<int, float>(new RedBlackTreeRankMapLong<int, float>((RedBlackTreeRankMapLong<int, float>)(((AdaptRankMapToRankMapLong<int, float>)tree).Inner)));
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankMapTestHarness<int, float>(new AdaptRankMapToRankMapLong<int, float>(new SplayTreeRankMapLong<int, float>(MaximumTestCaseSize, allocationMode)), delegate (IRankMap<int, float> tree)
                            {
                                return new AdaptRankMapToRankMapLong<int, float>(new SplayTreeRankMapLong<int, float>((SplayTreeRankMapLong<int, float>)(((AdaptRankMapToRankMapLong<int, float>)tree).Inner)));
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        KeyedTest(
                            delegate ()
                            {
                                return new RankMapTestHarness<int, float>(new AVLTreeArrayRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IRankMap<int, float> tree)
                                {
                                    return new AVLTreeArrayRankMap<int, float>((AVLTreeArrayRankMap<int, float>)tree);
                                });
                            },
                            delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value | KeyAxis.Rank);

                        KeyedTest(
                            delegate ()
                            {
                                return new RankMapTestHarness<int, float>(new RedBlackTreeArrayRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IRankMap<int, float> tree)
                                {
                                    return new RedBlackTreeArrayRankMap<int, float>((RedBlackTreeArrayRankMap<int, float>)tree);
                                });
                            },
                            delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value | KeyAxis.Rank);

                        KeyedTest(
                            delegate ()
                            {
                                return new RankMapTestHarness<int, float>(new SplayTreeArrayRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IRankMap<int, float> tree)
                                {
                                    return new SplayTreeArrayRankMap<int, float>((SplayTreeArrayRankMap<int, float>)tree);
                                });
                            },
                            delegate () { return new RankMapTestHarness<int, float>(new ReferenceRankMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value | KeyAxis.Rank);
                    }



                    //
                    // Rank List
                    //

                    // Int32

                    KeyedTest(
                        delegate ()
                        {
                            return new RankListTestHarness<int>(new AVLTreeRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IRankList<int> tree)
                            {
                                return new AVLTreeRankList<int>((AVLTreeRankList<int>)tree);
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankListTestHarness<int>(new RedBlackTreeRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IRankList<int> tree)
                            {
                                return new RedBlackTreeRankList<int>((RedBlackTreeRankList<int>)tree);
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankListTestHarness<int>(new SplayTreeRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IRankList<int> tree)
                            {
                                return new SplayTreeRankList<int>((SplayTreeRankList<int>)tree);
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank);

                    // Long

                    KeyedTest(
                        delegate ()
                        {
                            return new RankListTestHarness<int>(new AdaptRankListToRankListLong<int>(new AVLTreeRankListLong<int>(MaximumTestCaseSize, allocationMode)), delegate (IRankList<int> tree)
                            {
                                return new AdaptRankListToRankListLong<int>(new AVLTreeRankListLong<int>((AVLTreeRankListLong<int>)(((AdaptRankListToRankListLong<int>)tree).Inner)));
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankListTestHarness<int>(new AdaptRankListToRankListLong<int>(new RedBlackTreeRankListLong<int>(MaximumTestCaseSize, allocationMode)), delegate (IRankList<int> tree)
                            {
                                return new AdaptRankListToRankListLong<int>(new RedBlackTreeRankListLong<int>((RedBlackTreeRankListLong<int>)(((AdaptRankListToRankListLong<int>)tree).Inner)));
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank);

                    KeyedTest(
                        delegate ()
                        {
                            return new RankListTestHarness<int>(new AdaptRankListToRankListLong<int>(new SplayTreeRankListLong<int>(MaximumTestCaseSize, allocationMode)), delegate (IRankList<int> tree)
                            {
                                return new AdaptRankListToRankListLong<int>(new SplayTreeRankListLong<int>((SplayTreeRankListLong<int>)(((AdaptRankListToRankListLong<int>)tree).Inner)));
                            });
                        },
                        delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        KeyedTest(
                            delegate ()
                            {
                                return new RankListTestHarness<int>(new AVLTreeArrayRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IRankList<int> tree)
                                {
                                    return new AVLTreeArrayRankList<int>((AVLTreeArrayRankList<int>)tree);
                                });
                            },
                            delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.Rank);

                        KeyedTest(
                            delegate ()
                            {
                                return new RankListTestHarness<int>(new RedBlackTreeArrayRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IRankList<int> tree)
                                {
                                    return new RedBlackTreeArrayRankList<int>((RedBlackTreeArrayRankList<int>)tree);
                                });
                            },
                            delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.Rank);

                        KeyedTest(
                            delegate ()
                            {
                                return new RankListTestHarness<int>(new SplayTreeArrayRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IRankList<int> tree)
                                {
                                    return new SplayTreeArrayRankList<int>((SplayTreeArrayRankList<int>)tree);
                                });
                            },
                            delegate () { return new RankMapTestHarness<int, int>(new ReferenceRankMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.Rank);
                    }



                    //
                    // Multi-Rank Map
                    //

                    // Int32

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankMapTestHarness<int, float>(new AVLTreeMultiRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankMap<int, float> tree)
                            {
                                return new AVLTreeMultiRankMap<int, float>((AVLTreeMultiRankMap<int, float>)tree);
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankMapTestHarness<int, float>(new RedBlackTreeMultiRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankMap<int, float> tree)
                            {
                                return new RedBlackTreeMultiRankMap<int, float>((RedBlackTreeMultiRankMap<int, float>)tree);
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankMapTestHarness<int, float>(new SplayTreeMultiRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankMap<int, float> tree)
                            {
                                return new SplayTreeMultiRankMap<int, float>((SplayTreeMultiRankMap<int, float>)tree);
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                    // Long

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankMapTestHarness<int, float>(new AdaptMultiRankMapToMultiRankMapLong<int, float>(new AVLTreeMultiRankMapLong<int, float>(MaximumTestCaseSize, allocationMode)), delegate (IMultiRankMap<int, float> tree)
                            {
                                return new AdaptMultiRankMapToMultiRankMapLong<int, float>(new AVLTreeMultiRankMapLong<int, float>((AVLTreeMultiRankMapLong<int, float>)(((AdaptMultiRankMapToMultiRankMapLong<int, float>)tree).Inner)));
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankMapTestHarness<int, float>(new AdaptMultiRankMapToMultiRankMapLong<int, float>(new RedBlackTreeMultiRankMapLong<int, float>(MaximumTestCaseSize, allocationMode)), delegate (IMultiRankMap<int, float> tree)
                            {
                                return new AdaptMultiRankMapToMultiRankMapLong<int, float>(new RedBlackTreeMultiRankMapLong<int, float>((RedBlackTreeMultiRankMapLong<int, float>)(((AdaptMultiRankMapToMultiRankMapLong<int, float>)tree).Inner)));
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankMapTestHarness<int, float>(new AdaptMultiRankMapToMultiRankMapLong<int, float>(new SplayTreeMultiRankMapLong<int, float>(MaximumTestCaseSize, allocationMode)), delegate (IMultiRankMap<int, float> tree)
                            {
                                return new AdaptMultiRankMapToMultiRankMapLong<int, float>(new SplayTreeMultiRankMapLong<int, float>((SplayTreeMultiRankMapLong<int, float>)(((AdaptMultiRankMapToMultiRankMapLong<int, float>)tree).Inner)));
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                        MakeIntKey,
                        MakeFloatValue,
                        KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        KeyedTest(
                            delegate ()
                            {
                                return new MultiRankMapTestHarness<int, float>(new AVLTreeArrayMultiRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankMap<int, float> tree)
                                {
                                    return new AVLTreeArrayMultiRankMap<int, float>((AVLTreeArrayMultiRankMap<int, float>)tree);
                                });
                            },
                            delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                        KeyedTest(
                            delegate ()
                            {
                                return new MultiRankMapTestHarness<int, float>(new RedBlackTreeArrayMultiRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankMap<int, float> tree)
                                {
                                    return new RedBlackTreeArrayMultiRankMap<int, float>((RedBlackTreeArrayMultiRankMap<int, float>)tree);
                                });
                            },
                            delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);

                        KeyedTest(
                            delegate ()
                            {
                                return new MultiRankMapTestHarness<int, float>(new SplayTreeArrayMultiRankMap<int, float>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankMap<int, float> tree)
                                {
                                    return new SplayTreeArrayMultiRankMap<int, float>((SplayTreeArrayMultiRankMap<int, float>)tree);
                                });
                            },
                            delegate () { return new MultiRankMapTestHarness<int, float>(new ReferenceMultiRankMap<int, float>(), null); },
                            MakeIntKey,
                            MakeFloatValue,
                            KeyAxis.Value | KeyAxis.Rank | KeyAxis.Count);
                    }



                    //
                    // Multi-Rank List
                    //

                    // Int32

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankListTestHarness<int>(new AVLTreeMultiRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankList<int> tree)
                            {
                                return new AVLTreeMultiRankList<int>((AVLTreeMultiRankList<int>)tree);
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankListTestHarness<int>(new RedBlackTreeMultiRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankList<int> tree)
                            {
                                return new RedBlackTreeMultiRankList<int>((RedBlackTreeMultiRankList<int>)tree);
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankListTestHarness<int>(new SplayTreeMultiRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankList<int> tree)
                            {
                                return new SplayTreeMultiRankList<int>((SplayTreeMultiRankList<int>)tree);
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank | KeyAxis.Count);

                    // Long

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankListTestHarness<int>(new AdaptMultiRankListToMultiRankListLong<int>(new AVLTreeMultiRankListLong<int>(MaximumTestCaseSize, allocationMode)), delegate (IMultiRankList<int> tree)
                            {
                                return new AdaptMultiRankListToMultiRankListLong<int>(new AVLTreeMultiRankListLong<int>((AVLTreeMultiRankListLong<int>)(((AdaptMultiRankListToMultiRankListLong<int>)tree).Inner)));
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankListTestHarness<int>(new AdaptMultiRankListToMultiRankListLong<int>(new RedBlackTreeMultiRankListLong<int>(MaximumTestCaseSize, allocationMode)), delegate (IMultiRankList<int> tree)
                            {
                                return new AdaptMultiRankListToMultiRankListLong<int>(new RedBlackTreeMultiRankListLong<int>((RedBlackTreeMultiRankListLong<int>)(((AdaptMultiRankListToMultiRankListLong<int>)tree).Inner)));
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank | KeyAxis.Count);

                    KeyedTest(
                        delegate ()
                        {
                            return new MultiRankListTestHarness<int>(new AdaptMultiRankListToMultiRankListLong<int>(new SplayTreeMultiRankListLong<int>(MaximumTestCaseSize, allocationMode)), delegate (IMultiRankList<int> tree)
                            {
                                return new AdaptMultiRankListToMultiRankListLong<int>(new SplayTreeMultiRankListLong<int>((SplayTreeMultiRankListLong<int>)(((AdaptMultiRankListToMultiRankListLong<int>)tree).Inner)));
                            });
                        },
                        delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                        MakeIntKey,
                        null,
                        KeyAxis.Rank | KeyAxis.Count);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        KeyedTest(
                            delegate ()
                            {
                                return new MultiRankListTestHarness<int>(new AVLTreeArrayMultiRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankList<int> tree)
                                {
                                    return new AVLTreeArrayMultiRankList<int>((AVLTreeArrayMultiRankList<int>)tree);
                                });
                            },
                            delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.Rank | KeyAxis.Count);

                        KeyedTest(
                            delegate ()
                            {
                                return new MultiRankListTestHarness<int>(new RedBlackTreeArrayMultiRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankList<int> tree)
                                {
                                    return new RedBlackTreeArrayMultiRankList<int>((RedBlackTreeArrayMultiRankList<int>)tree);
                                });
                            },
                            delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.Rank | KeyAxis.Count);

                        KeyedTest(
                            delegate ()
                            {
                                return new MultiRankListTestHarness<int>(new SplayTreeArrayMultiRankList<int>(MaximumTestCaseSize, allocationMode), delegate (IMultiRankList<int> tree)
                                {
                                    return new SplayTreeArrayMultiRankList<int>((SplayTreeArrayMultiRankList<int>)tree);
                                });
                            },
                            delegate () { return new MultiRankMapTestHarness<int, int>(new ReferenceMultiRankMap<int, int>(), null); },
                            MakeIntKey,
                            null,
                            KeyAxis.Rank | KeyAxis.Count);
                    }



                    //
                    // Range Map
                    //

                    // Int32

                    RangeTest(
                        delegate ()
                        {
                            return new RangeMapTestHarness<float>(new AVLTreeRangeMap<float>(MaximumTestCaseSize, allocationMode), delegate (IRangeMap<float> tree)
                            {
                                return new AVLTreeRangeMap<float>((AVLTreeRangeMap<float>)tree);
                            });
                        },
                        delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeMapTestHarness<float>(new RedBlackTreeRangeMap<float>(MaximumTestCaseSize, allocationMode), delegate (IRangeMap<float> tree)
                            {
                                return new RedBlackTreeRangeMap<float>((RedBlackTreeRangeMap<float>)tree);
                            });
                        },
                        delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeMapTestHarness<float>(new SplayTreeRangeMap<float>(MaximumTestCaseSize, allocationMode), delegate (IRangeMap<float> tree)
                            {
                                return new SplayTreeRangeMap<float>((SplayTreeRangeMap<float>)tree);
                            });
                        },
                        delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value);

                    // Long

                    RangeTest(
                        delegate ()
                        {
                            return new RangeMapTestHarness<float>(new AdaptRangeMapToRangeMapLong<float>(new AVLTreeRangeMapLong<float>(MaximumTestCaseSize, allocationMode)), delegate (IRangeMap<float> tree)
                            {
                                return new AdaptRangeMapToRangeMapLong<float>(new AVLTreeRangeMapLong<float>((AVLTreeRangeMapLong<float>)(((AdaptRangeMapToRangeMapLong<float>)tree).Inner)));
                            });
                        },
                        delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeMapTestHarness<float>(new AdaptRangeMapToRangeMapLong<float>(new RedBlackTreeRangeMapLong<float>(MaximumTestCaseSize, allocationMode)), delegate (IRangeMap<float> tree)
                            {
                                return new AdaptRangeMapToRangeMapLong<float>(new RedBlackTreeRangeMapLong<float>((RedBlackTreeRangeMapLong<float>)(((AdaptRangeMapToRangeMapLong<float>)tree).Inner)));
                            });
                        },
                        delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeMapTestHarness<float>(new AdaptRangeMapToRangeMapLong<float>(new SplayTreeRangeMapLong<float>(MaximumTestCaseSize, allocationMode)), delegate (IRangeMap<float> tree)
                            {
                                return new AdaptRangeMapToRangeMapLong<float>(new SplayTreeRangeMapLong<float>((SplayTreeRangeMapLong<float>)(((AdaptRangeMapToRangeMapLong<float>)tree).Inner)));
                            });
                        },
                        delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        RangeTest(
                            delegate ()
                            {
                                return new RangeMapTestHarness<float>(new AVLTreeArrayRangeMap<float>(MaximumTestCaseSize, allocationMode), delegate (IRangeMap<float> tree)
                                {
                                    return new AVLTreeArrayRangeMap<float>((AVLTreeArrayRangeMap<float>)tree);
                                });
                            },
                            delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                            MakeFloatValue,
                            RangeAxis.Value);

                        RangeTest(
                            delegate ()
                            {
                                return new RangeMapTestHarness<float>(new RedBlackTreeArrayRangeMap<float>(MaximumTestCaseSize, allocationMode), delegate (IRangeMap<float> tree)
                                {
                                    return new RedBlackTreeArrayRangeMap<float>((RedBlackTreeArrayRangeMap<float>)tree);
                                });
                            },
                            delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                            MakeFloatValue,
                            RangeAxis.Value);

                        RangeTest(
                            delegate ()
                            {
                                return new RangeMapTestHarness<float>(new SplayTreeArrayRangeMap<float>(MaximumTestCaseSize, allocationMode), delegate (IRangeMap<float> tree)
                                {
                                    return new SplayTreeArrayRangeMap<float>((SplayTreeArrayRangeMap<float>)tree);
                                });
                            },
                            delegate () { return new RangeMapTestHarness<float>(new ReferenceRangeMap<float>(), null); },
                            MakeFloatValue,
                            RangeAxis.Value);
                    }



                    //
                    // Range List
                    //

                    // Int32

                    RangeTest(
                        delegate ()
                        {
                            return new RangeListTestHarness(new AVLTreeRangeList(MaximumTestCaseSize, allocationMode), delegate (IRangeList tree)
                            {
                                return new AVLTreeRangeList((AVLTreeRangeList)tree);
                            });
                        },
                        delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                        null,
                        RangeAxis.None);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeListTestHarness(new RedBlackTreeRangeList(MaximumTestCaseSize, allocationMode), delegate (IRangeList tree)
                            {
                                return new RedBlackTreeRangeList((RedBlackTreeRangeList)tree);
                            });
                        },
                        delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                        null,
                        RangeAxis.None);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeListTestHarness(new SplayTreeRangeList(MaximumTestCaseSize, allocationMode), delegate (IRangeList tree)
                            {
                                return new SplayTreeRangeList((SplayTreeRangeList)tree);
                            });
                        },
                        delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                        null,
                        RangeAxis.None);

                    // Long

                    RangeTest(
                        delegate ()
                        {
                            return new RangeListTestHarness(new AdaptRangeListToRangeListLong(new AVLTreeRangeListLong(MaximumTestCaseSize, allocationMode)), delegate (IRangeList tree)
                            {
                                return new AdaptRangeListToRangeListLong(new AVLTreeRangeListLong((AVLTreeRangeListLong)(((AdaptRangeListToRangeListLong)tree).Inner)));
                            });
                        },
                        delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                        null,
                        RangeAxis.None);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeListTestHarness(new AdaptRangeListToRangeListLong(new RedBlackTreeRangeListLong(MaximumTestCaseSize, allocationMode)), delegate (IRangeList tree)
                            {
                                return new AdaptRangeListToRangeListLong(new RedBlackTreeRangeListLong((RedBlackTreeRangeListLong)(((AdaptRangeListToRangeListLong)tree).Inner)));
                            });
                        },
                        delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                        null,
                        RangeAxis.None);

                    RangeTest(
                        delegate ()
                        {
                            return new RangeListTestHarness(new AdaptRangeListToRangeListLong(new SplayTreeRangeListLong(MaximumTestCaseSize, allocationMode)), delegate (IRangeList tree)
                            {
                                return new AdaptRangeListToRangeListLong(new SplayTreeRangeListLong((SplayTreeRangeListLong)(((AdaptRangeListToRangeListLong)tree).Inner)));
                            });
                        },
                        delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                        null,
                        RangeAxis.None);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        RangeTest(
                            delegate ()
                            {
                                return new RangeListTestHarness(new AVLTreeArrayRangeList(MaximumTestCaseSize, allocationMode), delegate (IRangeList tree)
                                {
                                    return new AVLTreeArrayRangeList((AVLTreeArrayRangeList)tree);
                                });
                            },
                            delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                            null,
                            RangeAxis.None);

                        RangeTest(
                            delegate ()
                            {
                                return new RangeListTestHarness(new RedBlackTreeArrayRangeList(MaximumTestCaseSize, allocationMode), delegate (IRangeList tree)
                                {
                                    return new RedBlackTreeArrayRangeList((RedBlackTreeArrayRangeList)tree);
                                });
                            },
                            delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                            null,
                            RangeAxis.None);

                        RangeTest(
                            delegate ()
                            {
                                return new RangeListTestHarness(new SplayTreeArrayRangeList(MaximumTestCaseSize, allocationMode), delegate (IRangeList tree)
                                {
                                    return new SplayTreeArrayRangeList((SplayTreeArrayRangeList)tree);
                                });
                            },
                            delegate () { return new RangeMapTestHarness<object>(new ReferenceRangeMap<object>(), null); },
                            null,
                            RangeAxis.None);
                    }



                    //
                    // Range2 Map
                    //

                    // Int32

                    RangeTest(
                        delegate ()
                        {
                            return new Range2MapTestHarness<float>(new AVLTreeRange2Map<float>(MaximumTestCaseSize, allocationMode), delegate (IRange2Map<float> tree)
                            {
                                return new AVLTreeRange2Map<float>((AVLTreeRange2Map<float>)tree);
                            });
                        },
                        delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value | RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2MapTestHarness<float>(new RedBlackTreeRange2Map<float>(MaximumTestCaseSize, allocationMode), delegate (IRange2Map<float> tree)
                            {
                                return new RedBlackTreeRange2Map<float>((RedBlackTreeRange2Map<float>)tree);
                            });
                        },
                        delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value | RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2MapTestHarness<float>(new SplayTreeRange2Map<float>(MaximumTestCaseSize, allocationMode), delegate (IRange2Map<float> tree)
                            {
                                return new SplayTreeRange2Map<float>((SplayTreeRange2Map<float>)tree);
                            });
                        },
                        delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value | RangeAxis.Range2);

                    // Long

                    RangeTest(
                        delegate ()
                        {
                            return new Range2MapTestHarness<float>(new AdaptRange2MapToRange2MapLong<float>(new AVLTreeRange2MapLong<float>(MaximumTestCaseSize, allocationMode)), delegate (IRange2Map<float> tree)
                            {
                                return new AdaptRange2MapToRange2MapLong<float>(new AVLTreeRange2MapLong<float>((AVLTreeRange2MapLong<float>)(((AdaptRange2MapToRange2MapLong<float>)tree).Inner)));
                            });
                        },
                        delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value | RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2MapTestHarness<float>(new AdaptRange2MapToRange2MapLong<float>(new RedBlackTreeRange2MapLong<float>(MaximumTestCaseSize, allocationMode)), delegate (IRange2Map<float> tree)
                            {
                                return new AdaptRange2MapToRange2MapLong<float>(new RedBlackTreeRange2MapLong<float>((RedBlackTreeRange2MapLong<float>)(((AdaptRange2MapToRange2MapLong<float>)tree).Inner)));
                            });
                        },
                        delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value | RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2MapTestHarness<float>(new AdaptRange2MapToRange2MapLong<float>(new SplayTreeRange2MapLong<float>(MaximumTestCaseSize, allocationMode)), delegate (IRange2Map<float> tree)
                            {
                                return new AdaptRange2MapToRange2MapLong<float>(new SplayTreeRange2MapLong<float>((SplayTreeRange2MapLong<float>)(((AdaptRange2MapToRange2MapLong<float>)tree).Inner)));
                            });
                        },
                        delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                        MakeFloatValue,
                        RangeAxis.Value | RangeAxis.Range2);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        RangeTest(
                            delegate ()
                            {
                                return new Range2MapTestHarness<float>(new AVLTreeArrayRange2Map<float>(MaximumTestCaseSize, allocationMode), delegate (IRange2Map<float> tree)
                                {
                                    return new AVLTreeArrayRange2Map<float>((AVLTreeArrayRange2Map<float>)tree);
                                });
                            },
                            delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                            MakeFloatValue,
                            RangeAxis.Value | RangeAxis.Range2);

                        RangeTest(
                            delegate ()
                            {
                                return new Range2MapTestHarness<float>(new RedBlackTreeArrayRange2Map<float>(MaximumTestCaseSize, allocationMode), delegate (IRange2Map<float> tree)
                                {
                                    return new RedBlackTreeArrayRange2Map<float>((RedBlackTreeArrayRange2Map<float>)tree);
                                });
                            },
                            delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                            MakeFloatValue,
                            RangeAxis.Value | RangeAxis.Range2);

                        RangeTest(
                            delegate ()
                            {
                                return new Range2MapTestHarness<float>(new SplayTreeArrayRange2Map<float>(MaximumTestCaseSize, allocationMode), delegate (IRange2Map<float> tree)
                                {
                                    return new SplayTreeArrayRange2Map<float>((SplayTreeArrayRange2Map<float>)tree);
                                });
                            },
                            delegate () { return new Range2MapTestHarness<float>(new ReferenceRange2Map<float>(), null); },
                            MakeFloatValue,
                            RangeAxis.Value | RangeAxis.Range2);
                    }



                    //
                    // Range2 List
                    //

                    // Int32

                    RangeTest(
                        delegate ()
                        {
                            return new Range2ListTestHarness(new AVLTreeRange2List(MaximumTestCaseSize, allocationMode), delegate (IRange2List tree)
                            {
                                return new AVLTreeRange2List((AVLTreeRange2List)tree);
                            });
                        },
                        delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                        null,
                        RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2ListTestHarness(new RedBlackTreeRange2List(MaximumTestCaseSize, allocationMode), delegate (IRange2List tree)
                            {
                                return new RedBlackTreeRange2List((RedBlackTreeRange2List)tree);
                            });
                        },
                        delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                        null,
                        RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2ListTestHarness(new SplayTreeRange2List(MaximumTestCaseSize, allocationMode), delegate (IRange2List tree)
                            {
                                return new SplayTreeRange2List((SplayTreeRange2List)tree);
                            });
                        },
                        delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                        null,
                        RangeAxis.Range2);

                    // Long

                    RangeTest(
                        delegate ()
                        {
                            return new Range2ListTestHarness(new AdaptRange2ListToRange2ListLong(new AVLTreeRange2ListLong(MaximumTestCaseSize, allocationMode)), delegate (IRange2List tree)
                            {
                                return new AdaptRange2ListToRange2ListLong(new AVLTreeRange2ListLong((AVLTreeRange2ListLong)(((AdaptRange2ListToRange2ListLong)tree).Inner)));
                            });
                        },
                        delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                        null,
                        RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2ListTestHarness(new AdaptRange2ListToRange2ListLong(new RedBlackTreeRange2ListLong(MaximumTestCaseSize, allocationMode)), delegate (IRange2List tree)
                            {
                                return new AdaptRange2ListToRange2ListLong(new RedBlackTreeRange2ListLong((RedBlackTreeRange2ListLong)(((AdaptRange2ListToRange2ListLong)tree).Inner)));
                            });
                        },
                        delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                        null,
                        RangeAxis.Range2);

                    RangeTest(
                        delegate ()
                        {
                            return new Range2ListTestHarness(new AdaptRange2ListToRange2ListLong(new SplayTreeRange2ListLong(MaximumTestCaseSize, allocationMode)), delegate (IRange2List tree)
                            {
                                return new AdaptRange2ListToRange2ListLong(new SplayTreeRange2ListLong((SplayTreeRange2ListLong)(((AdaptRange2ListToRange2ListLong)tree).Inner)));
                            });
                        },
                        delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                        null,
                        RangeAxis.Range2);

                    // Array

                    if (allocationMode != AllocationMode.DynamicDiscard)
                    {
                        RangeTest(
                            delegate ()
                            {
                                return new Range2ListTestHarness(new AVLTreeArrayRange2List(MaximumTestCaseSize, allocationMode), delegate (IRange2List tree)
                                {
                                    return new AVLTreeArrayRange2List((AVLTreeArrayRange2List)tree);
                                });
                            },
                            delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                            null,
                            RangeAxis.Range2);

                        RangeTest(
                            delegate ()
                            {
                                return new Range2ListTestHarness(new RedBlackTreeArrayRange2List(MaximumTestCaseSize, allocationMode), delegate (IRange2List tree)
                                {
                                    return new RedBlackTreeArrayRange2List((RedBlackTreeArrayRange2List)tree);
                                });
                            },
                            delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                            null,
                            RangeAxis.Range2);

                        RangeTest(
                            delegate ()
                            {
                                return new Range2ListTestHarness(new SplayTreeArrayRange2List(MaximumTestCaseSize, allocationMode), delegate (IRange2List tree)
                                {
                                    return new SplayTreeArrayRange2List((SplayTreeArrayRange2List)tree);
                                });
                            },
                            delegate () { return new Range2MapTestHarness<object>(new ReferenceRange2Map<object>(), null); },
                            null,
                            RangeAxis.Range2);
                    }
                }



                // sanity check
                Debug.Assert(SplayCount == RedBlackCount);
                Debug.Assert(SplayCount == AVLCount);

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
