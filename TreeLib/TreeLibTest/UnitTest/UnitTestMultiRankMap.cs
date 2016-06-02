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
    public class UnitTestMultiRankMap : TestBase
    {
        public UnitTestMultiRankMap(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private abstract class Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public readonly KeyType key;

            protected Op(KeyType key)
            {
                this.key = key;
            }

            public abstract void Do(IMultiRankMap<KeyType, ValueType> tree);
            public abstract void Do(IMultiRankMap<KeyType, ValueType> tree, IMultiRankMap<KeyType, ValueType> treeAnalog);
        }

        private class AddOp<KeyType, ValueType> : Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public readonly ValueType value;
            public readonly int count;

            public AddOp(KeyType key, ValueType value, int count)
                : base(key)
            {
                this.value = value;
                this.count = count;
            }

            public override void Do(IMultiRankMap<KeyType, ValueType> tree)
            {
                tree.Add(key, value, count);
            }

            public override void Do(IMultiRankMap<KeyType, ValueType> tree, IMultiRankMap<KeyType, ValueType> treeAnalog)
            {
                tree.Add(key, value, count);
                treeAnalog.Add(key, value, count);
            }

            public override string ToString()
            {
                return String.Format("AddOp({0}==>{1}, {2})", key, value, count);
            }
        }

        private class RemoveOp<KeyType, ValueType> : Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public RemoveOp(KeyType key)
                : base(key)
            {
            }

            public override void Do(IMultiRankMap<KeyType, ValueType> tree)
            {
                tree.Remove(key);
            }

            public override void Do(IMultiRankMap<KeyType, ValueType> tree, IMultiRankMap<KeyType, ValueType> treeAnalog)
            {
                tree.Remove(key);
                treeAnalog.Remove(key);
            }

            public override string ToString()
            {
                return String.Format("RemoveOp({0})", key);
            }
        }


        protected void ValidateRanks<KeyType, ValueType>(MultiRankMapEntry[] ranks) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            int offset = 0;
            for (int i = 0; i < ranks.Length; i++)
            {
                TestTrue("start", delegate () { return offset == ranks[i].rank.start; });
                TestTrue("count > 0", delegate () { return ranks[i].rank.length > 0; });
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

        protected void ValidateRanksEqual<KeyType, ValueType>(IMultiRankMap<KeyType, ValueType> tree1, IMultiRankMap<KeyType, ValueType> tree2) where KeyType : IComparable<KeyType>
        {
            IncrementIteration();
            MultiRankMapEntry[] ranks1 = ((INonInvasiveMultiRankMapInspection)tree1).GetRanks();
            ValidateRanks<KeyType, ValueType>(ranks1);
            MultiRankMapEntry[] ranks2 = ((INonInvasiveMultiRankMapInspection)tree2).GetRanks();
            ValidateRanks<KeyType, ValueType>(ranks2);
            ValidateRanksEqual<KeyType, ValueType>(ranks1, ranks2);
            TestTrue("GetExtent", delegate () { return tree1.RankCount == tree2.RankCount; });
        }


        private void BuildTree<KeyType, ValueType>(
            IMultiRankMap<KeyType, ValueType> tree,
            IEnumerable<Op<KeyType, ValueType>> sequence) where KeyType : IComparable<KeyType>
        {
            foreach (Op<KeyType, ValueType> op in sequence)
            {
                IncrementIteration();

                op.Do(tree);

                MultiRankMapEntry[] ranks = ((INonInvasiveMultiRankMapInspection)tree).GetRanks();
                ValidateRanks<KeyType, ValueType>(ranks);
            }
        }

        private void BuildTree<KeyType, ValueType>(
            IMultiRankMap<KeyType, ValueType> tree,
            IMultiRankMap<KeyType, ValueType> treeAnalog,
            IEnumerable<Op<KeyType, ValueType>> sequence) where KeyType : IComparable<KeyType>
        {
            foreach (Op<KeyType, ValueType> op in sequence)
            {
                IncrementIteration();

                op.Do(tree, treeAnalog);
            }

            ValidateRanksEqual(tree, treeAnalog);
        }

        private void TestTree<KeyType, ValueType>(
            string label,
            IMultiRankMap<KeyType, ValueType> tree,
            IEnumerable<Op<KeyType, ValueType>> sequence,
            VoidAction action) where KeyType : IComparable<KeyType>
        {
            try
            {
                BuildTree(tree, sequence);
            }
            catch (Exception exception)
            {
                Console.WriteLine("{0} [setup]: Unexpected exception occurred: {1}", label, exception);
                throw new UnitTestFailureException(label, exception);
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                Console.WriteLine("{0} [action]: Unexpected exception occurred: {1}", label, exception);
                throw new UnitTestFailureException(label, exception);
            }

            ValidateRanks<KeyType, ValueType>(((INonInvasiveMultiRankMapInspection)tree).GetRanks());
        }

        private delegate IMultiRankMap<KeyType, ValueType> MakeTree<KeyType, ValueType>() where KeyType : IComparable<KeyType>;


        //

        public void MultiRankMapBasicCoverage()
        {
            RankTreeBasicCoverageSpecific(
                "ReferenceRankMap<string>",
                delegate () { return new ReferenceMultiRankMap<int, float>(); });



            RankTreeBasicCoverageSpecific(
                "SplayTreeMultiRankMap<int,string>",
                delegate () { return new SplayTreeMultiRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "SplayTreeArrayMultiRankMap<int,string>",
                delegate () { return new SplayTreeArrayMultiRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "SplayTreeMultiRankMapLong<int,string>",
                delegate () { return new AdaptMultiRankMapToMultiRankMapLong<int, float>(new SplayTreeMultiRankMapLong<int, float>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeList",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new SplayTreeMultiRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeArrayList",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new SplayTreeArrayMultiRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeListLong",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new AdaptMultiRankListToMultiRankListLong<KeyValue<int, float>>(new SplayTreeMultiRankListLong<KeyValue<int, float>>())); });



            RankTreeBasicCoverageSpecific(
                "RedBlackTreeMultiRankMap<int,string>",
                delegate () { return new RedBlackTreeMultiRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "RedBlackTreeArrayMultiRankMap<int,string>",
                delegate () { return new RedBlackTreeArrayMultiRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "RedBlackTreeMultiRankMapLong<int,string>",
                delegate () { return new AdaptMultiRankMapToMultiRankMapLong<int, float>(new RedBlackTreeMultiRankMapLong<int, float>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeList",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new RedBlackTreeMultiRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeArrayList",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new RedBlackTreeArrayMultiRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeListLong",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new AdaptMultiRankListToMultiRankListLong<KeyValue<int, float>>(new RedBlackTreeMultiRankListLong<KeyValue<int, float>>())); });



            RankTreeBasicCoverageSpecific(
                "AVLTreeMultiRankMap<int,string>",
                delegate () { return new AVLTreeMultiRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "AVLTreeArrayMultiRankMap<int,string>",
                delegate () { return new AVLTreeArrayMultiRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "AVLTreeMultiRankMapLong<int,string>",
                delegate () { return new AdaptMultiRankMapToMultiRankMapLong<int, float>(new AVLTreeMultiRankMapLong<int, float>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeList",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new AVLTreeMultiRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeArrayList",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new AVLTreeArrayMultiRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeListLong",
                delegate () { return new AdaptMultiRankListToMultiRankMap<int, float>(new AdaptMultiRankListToMultiRankListLong<KeyValue<int, float>>(new AVLTreeMultiRankListLong<KeyValue<int, float>>())); });
        }

        private void RankTreeBasicCoverageSpecific(
            string label,
            MakeTree<int, float> makeTree)
        {
            TestBasic(
                "Basic",
                makeTree);


            TestBattery(
                "[]",
                makeTree,
                new Op<int, float>[] { });


            TestBattery(
                "[5(1):.1]",
                makeTree,
                new Op<int, float>[] { new AddOp<int, float>(5, .1f, 1) });

            TestBattery(
                "[5(2):.1]",
                makeTree,
                new Op<int, float>[] { new AddOp<int, float>(5, .1f, 2) });


            TestBattery(
                "[5(1):.1, 10(1):.2]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f, 1),
                    new AddOp<int, float>(10, .2f, 1),
                });

            TestBattery(
                "[5(2):.1, 10(3):.2]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f, 2),
                    new AddOp<int, float>(10, .2f, 3),
                });


            TestBattery(
                "[5(2):.1, 10(3):.3, 15(4):.2]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f, 2),
                    new AddOp<int, float>(10, .3f, 3),
                    new AddOp<int, float>(15, .2f, 4),
                });

            TestBattery(
                "[5(2):.1, 15(4):.2, 10(3):.3]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f, 2),
                    new AddOp<int, float>(15, .2f, 4),
                    new AddOp<int, float>(10, .3f, 3),
                });


            TestBattery(
                "[bunch o' stuff]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(15, .15f, 5),
                    new AddOp<int, float>(5, .05f, 2),
                    new AddOp<int, float>(10, .10f, 3),
                    new AddOp<int, float>(20, .20f, 6),
                    new AddOp<int, float>(25, .25f, 1),
                    new AddOp<int, float>(30, .30f, 7),
                });
        }

        private void TestBasic(
            string label,
            MakeTree<int, float> makeTree)
        {
            // basic functionality

            IMultiRankMap<int, float> tree;

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] { });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] { });
            TestTrue(label + " RankCount", delegate () { return tree.RankCount == 0; });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] { new AddOp<int, float>(5, .1f, 1) });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] { new MultiRankMapEntry(5, new Range(0, 1), .1f) });
            TestTrue(label + " RankCount", delegate () { return tree.RankCount == 1; });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] { new AddOp<int, float>(5, .1f, 2) });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] { new MultiRankMapEntry(5, new Range(0, 2), .1f) });
            TestTrue(label + " RankCount", delegate () { return tree.RankCount == 2; });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f, 1),
                    new AddOp<int, float>(10, .2f, 1),
                });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] {
                    new MultiRankMapEntry(5, new Range(0, 1), .1f),
                    new MultiRankMapEntry(10, new Range(1, 1), .2f),
                });
            TestTrue(label + " RankCount", delegate () { return tree.RankCount == 2; });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f, 3),
                    new AddOp<int, float>(10, .2f, 2),
                });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] {
                    new MultiRankMapEntry(5, new Range(0, 3), .1f),
                    new MultiRankMapEntry(10, new Range(3, 2), .2f),
                });
            TestTrue(label + " RankCount", delegate () { return tree.RankCount == 5; });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] {
                    new AddOp<int, float>(15, .15f, 5),
                    new AddOp<int, float>(5, .05f, 2),
                    new AddOp<int, float>(10, .10f, 3),
                    new AddOp<int, float>(20, .20f, 6),
                    new AddOp<int, float>(25, .25f, 1),
                    new AddOp<int, float>(30, .30f, 7),
                });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] {
                    new MultiRankMapEntry(5, new Range(0, 2), .05f),
                    new MultiRankMapEntry(10, new Range(2, 3), .10f),
                    new MultiRankMapEntry(15, new Range(5, 5), .15f),
                    new MultiRankMapEntry(20, new Range(10, 6), .20f),
                    new MultiRankMapEntry(25, new Range(16, 1), .25f),
                    new MultiRankMapEntry(30, new Range(17, 7), .30f),
                });
            TestTrue(label + " RankCount", delegate () { return tree.RankCount == 24; });
        }

        private void TestBattery(
            string label,
            MakeTree<int, float> makeTree,
            Op<int, float>[] sequence)
        {
            const float Value = .55f;
            const int Count = 12;

            ReferenceMultiRankMap<int, float> reference = new ReferenceMultiRankMap<int, float>();
            BuildTree(reference, sequence);

            MultiRankMapEntry[] ranks = ((INonInvasiveMultiRankMapInspection)reference).GetRanks();

            // test set can't have keys that are adjacent, due to structure of tests below
            for (int i = 1; i < ranks.Length; i++)
            {
                if (!((int)ranks[i - 1].key < (int)ranks[i].key - 1))
                {
                    Fault(new object(), "Test prerequisite violated");
                }
            }

            // test items in collection
            for (int i = 0; i < ranks.Length; i++)
            {
                IMultiRankMap<int, float> tree;
                ReferenceMultiRankMap<int, float> reference2;
                int p;

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Count", delegate () { return ranks.Length == unchecked((int)tree.Count); });

                p = 0;
                for (int j = 0; j < ranks.Length; j++)
                {
                    p += ranks[j].rank.length;
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " RankCount", delegate () { return tree.RankCount == p; });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " ContainsKey", delegate () { return tree.ContainsKey((int)ranks[i].key); });

                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryAdd(p, Value, Count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryAdd.1", delegate () { return tree.TryAdd(p, Value, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i < ranks.Length - 1) ? (((int)ranks[i].key + (int)ranks[i + 1].key) / 2) : ((int)ranks[i].key + 1);
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryAdd(p, Value, Count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryAdd.2", delegate () { return tree.TryAdd(p, Value, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (int)ranks[i].key;
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryAdd.3", delegate () { return tree.TryAdd(p, Value, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " TryAdd.4", typeof(ArgumentOutOfRangeException), delegate () { bool f = tree.TryAdd(p, Value, -1); });
                ValidateRanksEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryRemove((int)ranks[i].key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryRemove.1", delegate () { return tree.TryRemove((int)ranks[i].key); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryRemove.2", delegate () { return tree.TryRemove(p); });
                ValidateRanksEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySetValue((int)ranks[i].key, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySetValue.1", delegate () { return tree.TrySetValue((int)ranks[i].key, Value); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TrySetValue.2", delegate () { return tree.TrySetValue(p, Value); });
                ValidateRanksEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetValue.1a", delegate () { float value; return tree.TryGetValue((int)ranks[i].key, out value); });
                TestTrue(label + " TryGetValue.1b", delegate () { float value; tree.TryGetValue((int)ranks[i].key, out value); return value == (float)ranks[i].value; });
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryGetValue.2a", delegate () { float value; return tree.TryGetValue(p, out value); });
                TestTrue(label + " TryGetValue.2b", delegate () { float value; tree.TryGetValue(p, out value); return value == default(float); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.1", delegate () { float value; int rank; int count; return tree.TryGet((int)ranks[i].key, out value, out rank, out count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.2", delegate () { float value; int rank; int count; tree.TryGet((int)ranks[i].key, out value, out rank, out count); return value == (float)ranks[i].value; });
                TestTrue(label + " TryGet.3", delegate () { float value; int rank; int count; tree.TryGet((int)ranks[i].key, out value, out rank, out count); return rank == ranks[i].rank.start; });
                TestTrue(label + " TryGet.4", delegate () { float value; int rank; int count; tree.TryGet((int)ranks[i].key, out value, out rank, out count); return count == ranks[i].rank.length; });
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryGet.5", delegate () { float value; int rank; int count; return tree.TryGet(p, out value, out rank, out count); });
                TestTrue(label + " TryGet.6", delegate () { float value; int rank; int count; tree.TryGet(p, out value, out rank, out count); return value == default(float); });
                TestTrue(label + " TryGet.7", delegate () { float value; int rank; int count; tree.TryGet(p, out value, out rank, out count); return rank == 0; });
                TestTrue(label + " TryGet.8", delegate () { float value; int rank; int count; tree.TryGet(p, out value, out rank, out count); return count == 0; });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetKeyByRank.A.1", delegate () { int key; return tree.TryGetKeyByRank(ranks[i].rank.start, out key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetKeyByRank.A.2", delegate () { int key; tree.TryGetKeyByRank(ranks[i].rank.start, out key); return key == (int)ranks[i].key; });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetKeyByRank.B.1", delegate () { int key; return tree.TryGetKeyByRank(ranks[i].rank.start + ranks[i].rank.length - 1, out key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetKeyByRank.B.2", delegate () { int key; tree.TryGetKeyByRank(ranks[i].rank.start + ranks[i].rank.length - 1, out key); return key == (int)ranks[i].key; });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                if (i > 0)
                {
                    TestTrue(label + " TryGetKeyByRank.C.1", delegate () { int key; return tree.TryGetKeyByRank(ranks[i].rank.start - 1, out key); });
                }
                else
                {
                    TestThrow(label + " TryGetKeyByRank.C.1", typeof(ArgumentOutOfRangeException), delegate () { int key; tree.TryGetKeyByRank(ranks[i].rank.start - 1, out key); });
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                if (i > 0)
                {
                    TestFalse(label + " TryGetKeyByRank.C.2", delegate () { int key; tree.TryGetKeyByRank(ranks[i].rank.start - 1, out key); return key == (int)ranks[i].key; });
                }
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " TryGetKeyByRank.D.1", i + 1 < ranks.Length, delegate () { int key; return tree.TryGetKeyByRank(ranks[i].rank.start + ranks[i].rank.length, out key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryGetKeyByRank.D.2", delegate () { int key; tree.TryGetKeyByRank(ranks[i].rank.start + ranks[i].rank.length, out key); return key == (int)ranks[i].key; });

                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, Value, Count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Add.1", delegate () { tree.Add(p, Value, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i < ranks.Length - 1) ? (((int)ranks[i].key + (int)ranks[i + 1].key) / 2) : ((int)ranks[i].key + 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, Value, Count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Add.2", delegate () { tree.Add(p, Value, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (int)ranks[i].key;
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Add.3", typeof(ArgumentException), delegate () { tree.Add(p, Value, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Add.4", typeof(ArgumentOutOfRangeException), delegate () { tree.Add(p, Value, -1); });
                ValidateRanksEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Remove((int)ranks[i].key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Remove.1", delegate () { tree.Remove((int)ranks[i].key); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Remove.2", typeof(ArgumentException), delegate () { tree.Remove(p); });
                ValidateRanksEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue((int)ranks[i].key, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetValue.1", delegate () { tree.SetValue((int)ranks[i].key, Value); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " SetValue.2", typeof(ArgumentException), delegate () { tree.SetValue(p, Value); });
                ValidateRanksEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetValue.1a", delegate () { float value = tree.GetValue((int)ranks[i].key); });
                TestTrue(label + " GetValue.1b", delegate () { float value = tree.GetValue((int)ranks[i].key); return value == (float)ranks[i].value; });
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " GetValue.2a", typeof(ArgumentException), delegate () { float value = tree.GetValue(p); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Get.1", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.2", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); return value == (float)ranks[i].value; });
                TestTrue(label + " Get.3", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); return rank == ranks[i].rank.start; });
                TestTrue(label + " Get.4", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); return count == ranks[i].rank.length; });
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Get.5", typeof(ArgumentException), delegate () { float value; int rank; int count; tree.Get(p, out value, out rank, out count); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetKeyByRank.A.1", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetKeyByRank.A.2", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start); return key == (int)ranks[i].key; });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetKeyByRank.B.1", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start + ranks[i].rank.length - 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetKeyByRank.B.2", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start + ranks[i].rank.length - 1); return key == (int)ranks[i].key; });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                if (i > 0)
                {
                    TestNoThrow(label + " GetKeyByRank.C.1", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start - 1); });
                }
                else
                {
                    TestThrow(label + " GetKeyByRank.C.1", typeof(ArgumentException), delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start - 1); });
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                if (i > 0)
                {
                    TestFalse(label + " GetKeyByRank.C.2", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start - 1); return key == (int)ranks[i].key; });
                }
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                if (i + 1 < ranks.Length)
                {
                    TestNoThrow(label + " GetKeyByRank.D.1", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start + ranks[i].rank.length); });
                }
                else
                {
                    TestThrow(label + " GetKeyByRank.D.1", typeof(ArgumentException), delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start + ranks[i].rank.length); });
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                if (i + 1 < ranks.Length)
                {
                    TestFalse(label + " GetKeyByRank.D.2", delegate () { int key = tree.GetKeyByRank(ranks[i].rank.start + ranks[i].rank.length); return key == (int)ranks[i].key; });
                }

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.AdjustCount((int)ranks[i].key, 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount.1a", delegate () { tree.AdjustCount((int)ranks[i].key, 1); });
                TestTrue(label + " AdjustCount.1b", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); return rank == ranks[i].rank.start; });
                TestTrue(label + " AdjustCount.1c", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); return count == ranks[i].rank.length + 1; });
                if (i + 1 < ranks.Length)
                {
                    TestTrue(label + " AdjustCount.1b", delegate () { float value; int rank; int count; tree.Get((int)ranks[i + 1].key, out value, out rank, out count); return rank == ranks[i + 1].rank.start + 1; });
                    TestTrue(label + " AdjustCount.1c", delegate () { float value; int rank; int count; tree.Get((int)ranks[i + 1].key, out value, out rank, out count); return count == ranks[i + 1].rank.length; });
                }
                ValidateRanksEqual(reference2, tree);
                //
                if (ranks[i].rank.length > 1)
                {
                    reference2 = reference.Clone();
                    TestNoThrow("prereq", delegate () { reference2.AdjustCount((int)ranks[i].key, -1); });
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestNoThrow(label + " AdjustCount.2a", delegate () { tree.AdjustCount((int)ranks[i].key, -1); });
                    ValidateRanksEqual(reference2, tree);
                    TestTrue(label + " AdjustCount.2b", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); return rank == ranks[i].rank.start; });
                    TestTrue(label + " AdjustCount.2c", delegate () { float value; int rank; int count; tree.Get((int)ranks[i].key, out value, out rank, out count); return count == ranks[i].rank.length - 1; });
                    if (i + 1 < ranks.Length)
                    {
                        TestTrue(label + " AdjustCount.2b", delegate () { float value; int rank; int count; tree.Get((int)ranks[i + 1].key, out value, out rank, out count); return rank == ranks[i + 1].rank.start - 1; });
                        TestTrue(label + " AdjustCount.2c", delegate () { float value; int rank; int count; tree.Get((int)ranks[i + 1].key, out value, out rank, out count); return count == ranks[i + 1].rank.length; });
                    }
                }
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Remove((int)ranks[i].key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount.3a", delegate () { tree.AdjustCount((int)ranks[i].key, -ranks[i].rank.length); });
                TestTrue(label + " AdjustCount.3b", delegate () { return tree.Count == ranks.Length - 1; });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, default(float), Count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount.4", delegate () { tree.AdjustCount(p, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i < ranks.Length - 1) ? (((int)ranks[i].key + (int)ranks[i + 1].key) / 2) : ((int)ranks[i].key + 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, default(float), Count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount.5", delegate () { tree.AdjustCount(p, Count); });
                ValidateRanksEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount.6", typeof(ArgumentOutOfRangeException), delegate () { tree.AdjustCount((int)ranks[i].key, -ranks[i].rank.length - 1); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount.7", typeof(ArgumentOutOfRangeException), delegate () { tree.AdjustCount(p, -1); });
                ValidateRanksEqual(reference2, tree);

                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, default(float), Int32.MaxValue - reference2.RankCount - 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.1a", delegate () { tree.Add(p, default(float), Int32.MaxValue - tree.RankCount - 1); });
                TestTrue(label + " Insert-overflow.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.1c", delegate () { return tree.RankCount == reference2.RankCount; });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, default(float), Int32.MaxValue - reference2.RankCount - 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.2a", delegate () { tree.Add(p, default(float), Int32.MaxValue - tree.RankCount - 0); });
                TestTrue(label + " Insert-overflow.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.2c", delegate () { return tree.RankCount == reference2.RankCount; });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Insert-overflow.3a", typeof(OverflowException), delegate () { tree.Add(p, default(float), Int32.MaxValue - tree.RankCount + 1); });
                TestTrue(label + " Insert-overflow.3b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.3c", delegate () { return tree.RankCount == reference2.RankCount; });
                ValidateRanksEqual(reference2, tree);

                p = (int)ranks[i].key;
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.AdjustCount(p, Int32.MaxValue - reference2.RankCount - 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount-overflow.1a", delegate () { tree.AdjustCount(p, Int32.MaxValue - tree.RankCount - 1); });
                TestTrue(label + " AdjustCount-overflow.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " AdjustCount-overflow.1c", delegate () { return tree.RankCount == reference2.RankCount; });
                ValidateRanksEqual(reference2, tree);
                //
                p = (int)ranks[i].key;
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.AdjustCount(p, Int32.MaxValue - reference2.RankCount - 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount-overflow.2a", delegate () { tree.AdjustCount(p, Int32.MaxValue - tree.RankCount - 0); });
                TestTrue(label + " AdjustCount-overflow.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " AdjustCount-overflow.2c", delegate () { return tree.RankCount == reference2.RankCount; });
                ValidateRanksEqual(reference2, tree);
                //
                p = (int)ranks[i].key;
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount-overflow.3a", typeof(OverflowException), delegate () { tree.AdjustCount(p, Int32.MaxValue - tree.RankCount + 1); });
                TestTrue(label + " AdjustCount-overflow.3b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " AdjustCount-overflow.3c", delegate () { return tree.RankCount == reference2.RankCount; });
                ValidateRanksEqual(reference2, tree);
            }
        }

        public override bool Do()
        {
            try
            {
                this.MultiRankMapBasicCoverage();
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
