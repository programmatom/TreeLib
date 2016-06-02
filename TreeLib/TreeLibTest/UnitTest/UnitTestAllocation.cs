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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class UnitTestAllocation : TestBase
    {
        public UnitTestAllocation()
            : base()
        {
        }

        public UnitTestAllocation(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private abstract class AllocTest
        {
            protected readonly Random random = new Random(1);

            public abstract void Add();
            public abstract void Remove();
            public abstract uint Count { get; }
            public abstract void Validate();

            protected void ValidateRange2(INonInvasiveRange2MapInspection tree1, INonInvasiveRange2MapInspection tree2)
            {
                Range2MapEntry[] items1 = tree1.GetRanges();
                Range2MapEntry[] items2 = tree2.GetRanges();
                if (items1.Length != items2.Length)
                {
                    throw new Exception("length");
                }
                for (int i = 0; i < items1.Length; i++)
                {
                    if (!(((items1[i].value == null) && (items2[i].value == null)) || items1[i].Equals(items2[i])))
                    {
                        throw new Exception("value");
                    }
                    if (items1[i].x.start != items2[i].x.start)
                    {
                        throw new Exception("x.start");
                    }
                    if (items1[i].x.length != items2[i].x.length)
                    {
                        throw new Exception("x.length");
                    }
                    if (items1[i].y.start != items2[i].y.start)
                    {
                        throw new Exception("y.start");
                    }
                    if (items1[i].y.length != items2[i].y.length)
                    {
                        throw new Exception("y.length");
                    }
                }
            }

            protected void ValidateMultiRank(INonInvasiveMultiRankMapInspection tree1, INonInvasiveMultiRankMapInspection tree2)
            {
                MultiRankMapEntry[] items1 = tree1.GetRanks();
                MultiRankMapEntry[] items2 = tree2.GetRanks();
                if (items1.Length != items2.Length)
                {
                    throw new Exception("length");
                }
                for (int i = 0; i < items1.Length; i++)
                {
                    if ((int)items1[i].key != (int)items2[i].key)
                    {
                        throw new Exception("key");
                    }
                    if ((float)items1[i].value != (float)items2[i].value)
                    {
                        throw new Exception("value");
                    }
                    if (items1[i].rank.start != items2[i].rank.start)
                    {
                        throw new Exception("rank.start");
                    }
                    if (items1[i].rank.length != items2[i].rank.length)
                    {
                        throw new Exception("rank.length");
                    }
                }
            }
        }


        private class AllocTestMap : AllocTest
        {
            private readonly ReferenceMap<int, float> reference = new ReferenceMap<int, float>();
            private readonly IOrderedMap<int, float> actual;

            public AllocTestMap(IOrderedMap<int, float> actual)
            {
                this.actual = actual;
            }

            public override uint Count
            {
                get
                {
                    if (reference.Count != actual.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    return actual.Count;
                }
            }

            public override void Add()
            {
                int key;
                do
                {
                    key = random.Next();
                } while (reference.ContainsKey(key));
                float value = (float)random.NextDouble();

                actual.Add(key, value); // throws when capacity locked & exhausted

                reference.Add(key, value);
            }

            public override void Remove()
            {
                int i = random.Next((int)reference.Count);
                int key = ((ISimpleTreeInspection<int, float>)reference).ToArray()[i].Key;

                actual.Remove(key);

                reference.Remove(key);
            }

            public override void Validate()
            {
                KeyValuePair<int, float>[] items1 = ((ISimpleTreeInspection<int, float>)reference).ToArray();
                KeyValuePair<int, float>[] items2 = TreeInspection.Flatten<int, float>((INonInvasiveTreeInspection)actual);
                if (items1.Length != items2.Length)
                {
                    throw new Exception("length");
                }
                for (int i = 0; i < items1.Length; i++)
                {
                    if (items1[i].Key != items2[i].Key)
                    {
                        throw new Exception("key");
                    }
                    if (items1[i].Value != items2[i].Value)
                    {
                        throw new Exception("value");
                    }
                }
            }
        }


        private class AllocTestMultiRankMap : AllocTest
        {
            private readonly ReferenceMultiRankMap<int, float> reference = new ReferenceMultiRankMap<int, float>();
            private readonly IMultiRankMap<int, float> actual;

            public AllocTestMultiRankMap(IMultiRankMap<int, float> actual)
            {
                this.actual = actual;
            }

            public override uint Count
            {
                get
                {
                    if (reference.Count != actual.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    return actual.Count;
                }
            }

            public override void Add()
            {
                int key;
                do
                {
                    key = random.Next();
                } while (reference.ContainsKey(key));
                int rankCount = random.Next(100) + 1;
                float value = (float)random.NextDouble();

                actual.Add(key, value, rankCount); // throws when capacity locked & exhausted

                reference.Add(key, value, rankCount);
            }

            public override void Remove()
            {
                int i = random.Next((int)reference.Count);
                int key = (int)((INonInvasiveMultiRankMapInspection)reference).GetRanks()[i].key;

                actual.Remove(key);

                reference.Remove(key);
            }

            public override void Validate()
            {
                ValidateMultiRank((INonInvasiveMultiRankMapInspection)reference, (INonInvasiveMultiRankMapInspection)actual);
            }
        }


        private class AllocTestRankMap : AllocTest
        {
            private readonly ReferenceRankMap<int, float> reference = new ReferenceRankMap<int, float>();
            private readonly IRankMap<int, float> actual;

            public AllocTestRankMap(IRankMap<int, float> actual)
            {
                this.actual = actual;
            }

            public override uint Count
            {
                get
                {
                    if (reference.Count != actual.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    return actual.Count;
                }
            }

            public override void Add()
            {
                int key;
                do
                {
                    key = random.Next();
                } while (reference.ContainsKey(key));
                float value = (float)random.NextDouble();

                actual.Add(key, value); // throws when capacity locked & exhausted

                reference.Add(key, value);
            }

            public override void Remove()
            {
                int i = random.Next((int)reference.Count);
                int key = (int)((INonInvasiveMultiRankMapInspection)reference).GetRanks()[i].key;

                actual.Remove(key);

                reference.Remove(key);
            }

            public override void Validate()
            {
                ValidateMultiRank((INonInvasiveMultiRankMapInspection)reference, (INonInvasiveMultiRankMapInspection)actual);
            }
        }


        private class AllocTestRange2Map : AllocTest
        {
            private readonly ReferenceRange2Map<float> reference = new ReferenceRange2Map<float>();
            private readonly IRange2Map<float> actual;

            public AllocTestRange2Map(IRange2Map<float> actual)
            {
                this.actual = actual;
            }

            public override uint Count
            {
                get
                {
                    if (reference.Count != actual.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    return actual.Count;
                }
            }

            public override void Add()
            {
                int i = reference.Count > 0 ? random.Next((int)reference.Count + 1) : 0;
                int xStart = i < reference.Count ? ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start : reference.GetExtent(Side.X);
                int xLength = random.Next(100) + 1;
                int yLength = random.Next(100) + 1;
                float value = (float)random.NextDouble();

                actual.Insert(xStart, Side.X, xLength, yLength, value); // throws when capacity locked & exhausted

                reference.Insert(xStart, Side.X, xLength, yLength, value);
            }

            public override void Remove()
            {
                int i = random.Next((int)reference.Count);
                int xStart = ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start;

                actual.Delete(xStart, Side.X);

                reference.Delete(xStart, Side.X);
            }

            public override void Validate()
            {
                ValidateRange2((INonInvasiveRange2MapInspection)reference, (INonInvasiveRange2MapInspection)actual);
            }
        }


        private class AllocTestRangeMap : AllocTest
        {
            private readonly ReferenceRangeMap<float> reference = new ReferenceRangeMap<float>();
            private readonly IRangeMap<float> actual;

            public AllocTestRangeMap(IRangeMap<float> actual)
            {
                this.actual = actual;
            }

            public override uint Count
            {
                get
                {
                    if (reference.Count != actual.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    return actual.Count;
                }
            }

            public override void Add()
            {
                int i = reference.Count > 0 ? random.Next((int)reference.Count + 1) : 0;
                int xStart = i < reference.Count ? ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start : reference.GetExtent();
                int xLength = random.Next(100) + 1;
                float value = (float)random.NextDouble();

                actual.Insert(xStart, xLength, value); // throws when capacity locked & exhausted

                reference.Insert(xStart, xLength, value);
            }

            public override void Remove()
            {
                int i = random.Next((int)reference.Count);
                int xStart = ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start;

                actual.Delete(xStart);

                reference.Delete(xStart);
            }

            public override void Validate()
            {
                ValidateRange2((INonInvasiveRange2MapInspection)reference, (INonInvasiveRange2MapInspection)actual);
            }
        }


        private class AllocTestRange2List : AllocTest
        {
            private readonly ReferenceRange2List reference = new ReferenceRange2List();
            private readonly IRange2List actual;

            public AllocTestRange2List(IRange2List actual)
            {
                this.actual = actual;
            }

            public override uint Count
            {
                get
                {
                    if (reference.Count != actual.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    return actual.Count;
                }
            }

            public override void Add()
            {
                int i = reference.Count > 0 ? random.Next((int)reference.Count + 1) : 0;
                int xStart = i < reference.Count ? ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start : reference.GetExtent(Side.X);
                int xLength = random.Next(100) + 1;
                int yLength = random.Next(100) + 1;

                actual.Insert(xStart, Side.X, xLength, yLength); // throws when capacity locked & exhausted

                reference.Insert(xStart, Side.X, xLength, yLength);
            }

            public override void Remove()
            {
                int i = random.Next((int)reference.Count);
                int xStart = ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start;

                actual.Delete(xStart, Side.X);

                reference.Delete(xStart, Side.X);
            }

            public override void Validate()
            {
                ValidateRange2((INonInvasiveRange2MapInspection)reference, (INonInvasiveRange2MapInspection)actual);
            }
        }


        private class AllocTestRangeList : AllocTest
        {
            private readonly ReferenceRangeList reference = new ReferenceRangeList();
            private readonly IRangeList actual;

            public AllocTestRangeList(IRangeList actual)
            {
                this.actual = actual;
            }

            public override uint Count
            {
                get
                {
                    if (reference.Count != actual.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    return actual.Count;
                }
            }

            public override void Add()
            {
                int i = reference.Count > 0 ? random.Next((int)reference.Count + 1) : 0;
                int xStart = i < reference.Count ? ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start : reference.GetExtent();
                int xLength = random.Next(100) + 1;

                actual.Insert(xStart, xLength); // throws when capacity locked & exhausted

                reference.Insert(xStart, xLength);
            }

            public override void Remove()
            {
                int i = random.Next((int)reference.Count);
                int xStart = ((INonInvasiveRange2MapInspection)reference).GetRanges()[i].x.start;

                actual.Delete(xStart);

                reference.Delete(xStart);
            }

            public override void Validate()
            {
                ValidateRange2((INonInvasiveRange2MapInspection)reference, (INonInvasiveRange2MapInspection)actual);
            }
        }


        private void TestAdd(AllocTest tree, int count, int capacity)
        {
            int c = 0;
            for (int j = 0; j < count; j++)
            {
                IncrementIteration();
                if (j < capacity)
                {
                    TestNoThrow("add", delegate () { tree.Add(); });
                    c++;
                }
                else
                {
                    TestThrow("add-fail", typeof(OutOfMemoryException), delegate () { tree.Add(); });
                }
                TestNoThrow("validate", delegate () { tree.Validate(); });
            }
            TestNoThrow("validate", delegate () { tree.Validate(); });
            TestTrue("count", delegate () { return tree.Count == c; });
        }

        private void TestRemove(AllocTest tree)
        {
            int count = (int)tree.Count;
            for (int j = 0; j < count; j++)
            {
                IncrementIteration();
                TestNoThrow("remove", delegate () { tree.Remove(); });
                TestNoThrow("validate", delegate () { tree.Validate(); });
            }
            TestNoThrow("validate", delegate () { tree.Validate(); });
            TestTrue("count", delegate () { return tree.Count == 0; });
        }

        // Tests the exactness of pre-allocated fixed capacity limiting.
        private delegate AllocTest MakeTreeUnbound(AllocationMode allocationMode, uint capacity);
        private delegate AllocTest MakeTreeBound();
        private void DoTest(MakeTreeUnbound makeTreeUnbound, bool permitDiscardMode)
        {
            Tuple<AllocationMode, uint, int>[] tests = new Tuple<AllocationMode, uint, int>[]
            {
                new Tuple<AllocationMode, uint, int>(AllocationMode.DynamicDiscard, 0, Int32.MaxValue),
                new Tuple<AllocationMode, uint, int>(AllocationMode.DynamicDiscard, 3, Int32.MaxValue),
                new Tuple<AllocationMode, uint, int>(AllocationMode.DynamicDiscard, 8, Int32.MaxValue),

                new Tuple<AllocationMode, uint, int>(AllocationMode.DynamicRetainFreelist, 0, Int32.MaxValue),
                new Tuple<AllocationMode, uint, int>(AllocationMode.DynamicRetainFreelist, 3, Int32.MaxValue),
                new Tuple<AllocationMode, uint, int>(AllocationMode.DynamicRetainFreelist, 8, Int32.MaxValue),

                new Tuple<AllocationMode, uint, int>(AllocationMode.PreallocatedFixed, 0, 0),
                new Tuple<AllocationMode, uint, int>(AllocationMode.PreallocatedFixed, 3, 3),
                new Tuple<AllocationMode, uint, int>(AllocationMode.PreallocatedFixed, 8, 8),
            };

            foreach (Tuple<AllocationMode, uint, int> test in tests)
            {
                MakeTreeBound makeTree = delegate () { return makeTreeUnbound(test.Item1, test.Item2); };

                if (permitDiscardMode)
                {
                    TestNoThrow("new", delegate () { makeTree(); });
                }
                else
                {
                    if (test.Item1 != AllocationMode.DynamicDiscard)
                    {
                        TestNoThrow("new", delegate () { makeTree(); });
                    }
                    else
                    {
                        TestThrow("new", typeof(ArgumentException), delegate () { makeTree(); });
                        continue;
                    }
                }
                TestNoThrow("validate", delegate () { makeTree().Validate(); });
                TestTrue("initial", delegate () { AllocTest tree = makeTree(); return tree.Count == 0; });

                for (int i = 0; i <= 7; i++)
                {
                    AllocTest tree = makeTree();
                    TestAdd(tree, i, test.Item3);
                    TestRemove(tree);
                    TestAdd(tree, i, test.Item3);
                }
            }
        }

        public override bool Do()
        {
            try
            {
                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new SplayTreeMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new SplayTreeArrayMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new RedBlackTreeMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new RedBlackTreeArrayMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AVLTreeMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AVLTreeArrayMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AdaptListToMap<int, float>(new SplayTreeList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AdaptListToMap<int, float>(new SplayTreeArrayList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AdaptListToMap<int, float>(new RedBlackTreeList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AdaptListToMap<int, float>(new RedBlackTreeArrayList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AdaptListToMap<int, float>(new AVLTreeList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMap(new AdaptListToMap<int, float>(new AVLTreeArrayList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new SplayTreeMultiRankMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new SplayTreeArrayMultiRankMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new RedBlackTreeMultiRankMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new RedBlackTreeArrayMultiRankMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AVLTreeMultiRankMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AVLTreeArrayMultiRankMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AdaptMultiRankListToMultiRankMap<int, float>(new SplayTreeMultiRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AdaptMultiRankListToMultiRankMap<int, float>(new SplayTreeArrayMultiRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AdaptMultiRankListToMultiRankMap<int, float>(new RedBlackTreeMultiRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AdaptMultiRankListToMultiRankMap<int, float>(new RedBlackTreeArrayMultiRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AdaptMultiRankListToMultiRankMap<int, float>(new AVLTreeMultiRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestMultiRankMap(new AdaptMultiRankListToMultiRankMap<int, float>(new AVLTreeArrayMultiRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new SplayTreeRankMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new SplayTreeArrayRankMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new RedBlackTreeRankMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new RedBlackTreeArrayRankMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AVLTreeRankMap<int, float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AVLTreeArrayRankMap<int, float>(capacity, allocationMode)); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AdaptRankListToRankMap<int, float>(new SplayTreeRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AdaptRankListToRankMap<int, float>(new SplayTreeArrayRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AdaptRankListToRankMap<int, float>(new RedBlackTreeRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AdaptRankListToRankMap<int, float>(new RedBlackTreeArrayRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AdaptRankListToRankMap<int, float>(new AVLTreeRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRankMap(new AdaptRankListToRankMap<int, float>(new AVLTreeArrayRankList<KeyValue<int, float>>(capacity, allocationMode))); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2Map(new SplayTreeRange2Map<float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2Map(new SplayTreeArrayRange2Map<float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2Map(new RedBlackTreeRange2Map<float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2Map(new RedBlackTreeArrayRange2Map<float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2Map(new AVLTreeRange2Map<float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2Map(new AVLTreeArrayRange2Map<float>(capacity, allocationMode)); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeMap(new SplayTreeRangeMap<float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeMap(new SplayTreeArrayRangeMap<float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeMap(new RedBlackTreeRangeMap<float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeMap(new RedBlackTreeArrayRangeMap<float>(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeMap(new AVLTreeRangeMap<float>(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeMap(new AVLTreeArrayRangeMap<float>(capacity, allocationMode)); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2List(new SplayTreeRange2List(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2List(new SplayTreeArrayRange2List(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2List(new RedBlackTreeRange2List(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2List(new RedBlackTreeArrayRange2List(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2List(new AVLTreeRange2List(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRange2List(new AVLTreeArrayRange2List(capacity, allocationMode)); },
                false/*permitDiscard*/);



                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeList(new SplayTreeRangeList(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeList(new SplayTreeArrayRangeList(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeList(new RedBlackTreeRangeList(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeList(new RedBlackTreeArrayRangeList(capacity, allocationMode)); },
                false/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeList(new AVLTreeRangeList(capacity, allocationMode)); },
                true/*permitDiscard*/);

                DoTest(delegate (AllocationMode allocationMode, uint capacity)
                { return new AllocTestRangeList(new AVLTreeArrayRangeList(capacity, allocationMode)); },
                false/*permitDiscard*/);



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
