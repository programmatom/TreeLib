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
    public class UnitTestRankMap : TestBase
    {
        public UnitTestRankMap(long[] breakIterations, long startIteration)
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

            public abstract void Do(IRankMap<KeyType, ValueType> tree);
            public abstract void Do(IRankMap<KeyType, ValueType> tree, IRankMap<KeyType, ValueType> treeAnalog);
        }

        private class AddOp<KeyType, ValueType> : Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public readonly ValueType value;

            public AddOp(KeyType key, ValueType value)
                : base(key)
            {
                this.value = value;
            }

            public override void Do(IRankMap<KeyType, ValueType> tree)
            {
                tree.Add(key, value);
            }

            public override void Do(IRankMap<KeyType, ValueType> tree, IRankMap<KeyType, ValueType> treeAnalog)
            {
                tree.Add(key, value);
                treeAnalog.Add(key, value);
            }
        }

        private class RemoveOp<KeyType, ValueType> : Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public RemoveOp(KeyType key)
                : base(key)
            {
            }

            public override void Do(IRankMap<KeyType, ValueType> tree)
            {
                tree.Remove(key);
            }

            public override void Do(IRankMap<KeyType, ValueType> tree, IRankMap<KeyType, ValueType> treeAnalog)
            {
                tree.Remove(key);
                treeAnalog.Remove(key);
            }
        }


        private void BuildTree<KeyType, ValueType>(
            IRankMap<KeyType, ValueType> tree,
            IEnumerable<Op<KeyType, ValueType>> sequence) where KeyType : IComparable<KeyType>
        {
            ValidateTree(tree);
            foreach (Op<KeyType, ValueType> op in sequence)
            {
                IncrementIteration();
                op.Do(tree);
                ValidateTree(tree);
                ValidateRanks<KeyType, ValueType>(((INonInvasiveMultiRankMapInspection)tree).GetRanks(), false/*multi*/);
            }
        }

        private void BuildTree<KeyType, ValueType>(
            IRankMap<KeyType, ValueType> tree,
            IRankMap<KeyType, ValueType> treeAnalog,
            IEnumerable<Op<KeyType, ValueType>> sequence) where KeyType : IComparable<KeyType>
        {
            ValidateTree(tree);
            foreach (Op<KeyType, ValueType> op in sequence)
            {
                IncrementIteration();
                op.Do(tree, treeAnalog);
                ValidateTree(tree);
            }
            ValidateRanksEqual(tree, treeAnalog);
        }

        private void TestTree<KeyType, ValueType>(
            string label,
            IRankMap<KeyType, ValueType> tree,
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

            ValidateRanks<KeyType, ValueType>(((INonInvasiveMultiRankMapInspection)tree).GetRanks(), false/*multi*/);
        }

        private delegate IRankMap<KeyType, ValueType> MakeTree<KeyType, ValueType>() where KeyType : IComparable<KeyType>;


        //

        public void RankMapBasicCoverage()
        {
            RankTreeBasicCoverageSpecific(
                "ReferenceRankMap<string>",
                delegate () { return new ReferenceRankMap<int, float>(); });



            RankTreeBasicCoverageSpecific(
                "SplayTreeRankMap<int,string>",
                delegate () { return new SplayTreeRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "SplayTreeArrayRankMap<int,string>",
                delegate () { return new SplayTreeArrayRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "SplayTreeRankMapLong<int,string>",
                delegate () { return new AdaptRankMapToRankMapLong<int, float>(new SplayTreeRankMapLong<int, float>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new SplayTreeRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeArrayList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new SplayTreeArrayRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeListLong",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AdaptRankListToRankListLong<KeyValue<int, float>>(new SplayTreeRankListLong<KeyValue<int, float>>())); });

            // with explicit comparer

            RankTreeBasicCoverageSpecific(
                "SplayTreeRankMap<int,string>",
                delegate () { return new SplayTreeRankMap<int, float>(Comparer<int>.Default); });

            RankTreeBasicCoverageSpecific(
                "SplayTreeArrayRankMap<int,string>",
                delegate () { return new SplayTreeArrayRankMap<int, float>(Comparer<int>.Default); });

            RankTreeBasicCoverageSpecific(
                "SplayTreeRankMapLong<int,string>",
                delegate () { return new AdaptRankMapToRankMapLong<int, float>(new SplayTreeRankMapLong<int, float>(Comparer<int>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new SplayTreeRankList<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeArrayList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new SplayTreeArrayRankList<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeListLong",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AdaptRankListToRankListLong<KeyValue<int, float>>(new SplayTreeRankListLong<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default))); });



            RankTreeBasicCoverageSpecific(
                "RedBlackTreeRankMap<int,string>",
                delegate () { return new RedBlackTreeRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "RedBlackTreeArrayRankMap<int,string>",
                delegate () { return new RedBlackTreeArrayRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "RedBlackTreeRankMapLong<int,string>",
                delegate () { return new AdaptRankMapToRankMapLong<int, float>(new RedBlackTreeRankMapLong<int, float>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new RedBlackTreeRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeArrayList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new RedBlackTreeArrayRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeListLong",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AdaptRankListToRankListLong<KeyValue<int, float>>(new RedBlackTreeRankListLong<KeyValue<int, float>>())); });

            // with explicit comparer

            RankTreeBasicCoverageSpecific(
                "RedBlackTreeRankMap<int,string>",
                delegate () { return new RedBlackTreeRankMap<int, float>(Comparer<int>.Default); });

            RankTreeBasicCoverageSpecific(
                "RedBlackTreeArrayRankMap<int,string>",
                delegate () { return new RedBlackTreeArrayRankMap<int, float>(Comparer<int>.Default); });

            RankTreeBasicCoverageSpecific(
                "RedBlackTreeRankMapLong<int,string>",
                delegate () { return new AdaptRankMapToRankMapLong<int, float>(new RedBlackTreeRankMapLong<int, float>(Comparer<int>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new RedBlackTreeRankList<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeArrayList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new RedBlackTreeArrayRankList<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeListLong",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AdaptRankListToRankListLong<KeyValue<int, float>>(new RedBlackTreeRankListLong<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default))); });



            RankTreeBasicCoverageSpecific(
                "AVLTreeRankMap<int,string>",
                delegate () { return new AVLTreeRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "AVLTreeArrayRankMap<int,string>",
                delegate () { return new AVLTreeArrayRankMap<int, float>(); });

            RankTreeBasicCoverageSpecific(
                "AVLTreeRankMapLong<int,string>",
                delegate () { return new AdaptRankMapToRankMapLong<int, float>(new AVLTreeRankMapLong<int, float>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AVLTreeRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeArrayList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AVLTreeArrayRankList<KeyValue<int, float>>()); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeListLong",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AdaptRankListToRankListLong<KeyValue<int, float>>(new AVLTreeRankListLong<KeyValue<int, float>>())); });

            // with explicit comparer

            RankTreeBasicCoverageSpecific(
                "AVLTreeRankMap<int,string>",
                delegate () { return new AVLTreeRankMap<int, float>(Comparer<int>.Default); });

            RankTreeBasicCoverageSpecific(
                "AVLTreeArrayRankMap<int,string>",
                delegate () { return new AVLTreeArrayRankMap<int, float>(Comparer<int>.Default); });

            RankTreeBasicCoverageSpecific(
                "AVLTreeRankMapLong<int,string>",
                delegate () { return new AdaptRankMapToRankMapLong<int, float>(new AVLTreeRankMapLong<int, float>(Comparer<int>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AVLTreeRankList<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeArrayList",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AVLTreeArrayRankList<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default)); });

            RankTreeBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeListLong",
                delegate () { return new AdaptRankListToRankMap<int, float>(new AdaptRankListToRankListLong<KeyValue<int, float>>(new AVLTreeRankListLong<KeyValue<int, float>>(Comparer<KeyValue<int, float>>.Default))); });
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
                "[5:.1]",
                makeTree,
                new Op<int, float>[] { new AddOp<int, float>(5, .1f) });


            TestBattery(
                "[5:.1, 10:.2]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f),
                    new AddOp<int, float>(10, .2f),
                });


            TestBattery(
                "[5:.1, 10:.3, 15:.2]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f),
                    new AddOp<int, float>(10, .3f),
                    new AddOp<int, float>(15, .2f),
                });

            TestBattery(
                "[5:.1, 15:.2, 10:.3]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f),
                    new AddOp<int, float>(15, .2f),
                    new AddOp<int, float>(10, .3f),
                });


            TestBattery(
                "[bunch o' stuff]",
                makeTree,
                new Op<int, float>[] {
                    new AddOp<int, float>(15, .15f),
                    new AddOp<int, float>(5, .05f),
                    new AddOp<int, float>(10, .10f),
                    new AddOp<int, float>(20, .20f),
                    new AddOp<int, float>(25, .25f),
                    new AddOp<int, float>(30, .30f),
                });
        }

        private void TestBasic(
            string label,
            MakeTree<int, float> makeTree)
        {
            // basic functionality

            IRankMap<int, float> tree;

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] { });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] { });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] { new AddOp<int, float>(5, .1f) });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] { new MultiRankMapEntry(5, new Range(0, 1), .1f) });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] {
                    new AddOp<int, float>(5, .1f),
                    new AddOp<int, float>(10, .2f),
                });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] {
                    new MultiRankMapEntry(5, new Range(0, 1), .1f),
                    new MultiRankMapEntry(10, new Range(1, 1), .2f),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<int, float>[] {
                    new AddOp<int, float>(15, .15f),
                    new AddOp<int, float>(5, .05f),
                    new AddOp<int, float>(10, .10f),
                    new AddOp<int, float>(20, .20f),
                    new AddOp<int, float>(25, .25f),
                    new AddOp<int, float>(30, .30f),
                });
            ValidateRanksEqual<int, float>(
                ((INonInvasiveMultiRankMapInspection)tree).GetRanks(),
                new MultiRankMapEntry[] {
                    new MultiRankMapEntry(5, new Range(0, 1), .05f),
                    new MultiRankMapEntry(10, new Range(1, 1), .10f),
                    new MultiRankMapEntry(15, new Range(2, 1), .15f),
                    new MultiRankMapEntry(20, new Range(3, 1), .20f),
                    new MultiRankMapEntry(25, new Range(4, 1), .25f),
                    new MultiRankMapEntry(30, new Range(5, 1), .30f),
                });
        }

        private void TestBattery(
            string label,
            MakeTree<int, float> makeTree,
            Op<int, float>[] sequence)
        {
            const float Value = .55f;

            ReferenceRankMap<int, float> reference = new ReferenceRankMap<int, float>();
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
                IRankMap<int, float> tree;
                ReferenceRankMap<int, float> reference2;
                int p;
                bool f;

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Count", delegate () { return ranks.Length == unchecked((int)tree.Count); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " LongCount", delegate () { return ranks.Length == unchecked((int)tree.LongCount); });

                p = 0;
                for (int j = 0; j < ranks.Length; j++)
                {
                    TestTrue(label + " RankCount[j]", delegate () { return ranks[j].rank.length == 1; });
                    p += ranks[j].rank.length;
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " RankCount", delegate () { return tree.Count == p; });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " ContainsKey", delegate () { return tree.ContainsKey((int)ranks[i].key); });

                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryAdd(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryAdd.1", delegate () { return tree.TryAdd(p, Value); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i < ranks.Length - 1) ? (((int)ranks[i].key + (int)ranks[i + 1].key) / 2) : ((int)ranks[i].key + 1);
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryAdd(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryAdd.2", delegate () { return tree.TryAdd(p, Value); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (int)ranks[i].key;
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryAdd.3", delegate () { return tree.TryAdd(p, Value); });
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
                TestTrue(label + " TryGet.1", delegate () { float value; int rank; return tree.TryGet((int)ranks[i].key, out value, out rank); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.2", delegate () { float value; int rank; tree.TryGet((int)ranks[i].key, out value, out rank); return value == (float)ranks[i].value; });
                TestTrue(label + " TryGet.3", delegate () { float value; int rank; tree.TryGet((int)ranks[i].key, out value, out rank); return rank == ranks[i].rank.start; });
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryGet.5", delegate () { float value; int rank; return tree.TryGet(p, out value, out rank); });
                TestTrue(label + " TryGet.6", delegate () { float value; int rank; tree.TryGet(p, out value, out rank); return value == default(float); });
                TestTrue(label + " TryGet.7", delegate () { float value; int rank; tree.TryGet(p, out value, out rank); return rank == 0; });

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
                TestNoThrow("prereq", delegate () { reference2.Add(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Add.1", delegate () { tree.Add(p, Value); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i < ranks.Length - 1) ? (((int)ranks[i].key + (int)ranks[i + 1].key) / 2) : ((int)ranks[i].key + 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Add.2", delegate () { tree.Add(p, Value); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (int)ranks[i].key;
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Add.3", typeof(ArgumentException), delegate () { tree.Add(p, Value); });
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
                TestNoThrow(label + " Get.1", delegate () { float value; int rank; tree.Get((int)ranks[i].key, out value, out rank); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.2", delegate () { float value; int rank; tree.Get((int)ranks[i].key, out value, out rank); return value == (float)ranks[i].value; });
                TestTrue(label + " Get.3", delegate () { float value; int rank; tree.Get((int)ranks[i].key, out value, out rank); return rank == ranks[i].rank.start; });
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Get.5", typeof(ArgumentException), delegate () { float value; int rank; tree.Get(p, out value, out rank); });

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
                TestThrow("prereq", typeof(ArgumentOutOfRangeException), delegate () { reference2.AdjustCount((int)ranks[i].key, 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount.1a", typeof(ArgumentOutOfRangeException), delegate () { tree.AdjustCount((int)ranks[i].key, 1); });
                TestTrue(label + " AdjustCount.1b", delegate () { float value; int rank; tree.Get((int)ranks[i].key, out value, out rank); return rank == ranks[i].rank.start; });
                if (i + 1 < ranks.Length)
                {
                    TestTrue(label + " AdjustCount.1b", delegate () { float value; int rank; tree.Get((int)ranks[i + 1].key, out value, out rank); return rank == ranks[i + 1].rank.start; });
                }
                ValidateRanksEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Remove((int)ranks[i].key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount.2a", delegate () { tree.AdjustCount((int)ranks[i].key, -ranks[i].rank.length); });
                TestTrue(label + " AdjustCount.2b", delegate () { return tree.Count == ranks.Length - 1; });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, default(float)); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount.3", delegate () { tree.AdjustCount(p, 1); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i < ranks.Length - 1) ? (((int)ranks[i].key + (int)ranks[i + 1].key) / 2) : ((int)ranks[i].key + 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, default(float)); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " AdjustCount.4", delegate () { tree.AdjustCount(p, 1); });
                ValidateRanksEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount.5", typeof(ArgumentOutOfRangeException), delegate () { tree.AdjustCount((int)ranks[i].key, 1); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount.6", typeof(ArgumentOutOfRangeException), delegate () { tree.AdjustCount(p, -1); });
                ValidateRanksEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount.7", typeof(ArgumentOutOfRangeException), delegate () { tree.AdjustCount((int)ranks[i].key, -ranks[i].rank.length - 1); });
                ValidateRanksEqual(reference2, tree);
                //
                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " AdjustCount.8", typeof(ArgumentOutOfRangeException), delegate () { tree.AdjustCount(p, -1); });
                ValidateRanksEqual(reference2, tree);

                // ConditionalSetOrAdd
                long lastIter1 = IncrementIteration(true/*setLast*/);

                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                // test no-op with nothing in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.1", delegate () { tree.ConditionalSetOrAdd(p, delegate (int _key, ref float _value, bool resident) { return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test no-op with valid item in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.2", delegate () { tree.ConditionalSetOrAdd((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test no-op changing value of non-existent item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue((int)ranks[i].key, reference2.GetValue((int)ranks[i].key) + (float)Math.PI); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.3", delegate () { tree.ConditionalSetOrAdd((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test changing value of existing item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue((int)ranks[i].key, reference2.GetValue((int)ranks[i].key) + (float)Math.PI); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.4", delegate () { tree.ConditionalSetOrAdd((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test adding value of non-existent item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, (float)Math.PI); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.5", delegate () { tree.ConditionalSetOrAdd(p, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return true; }); });
                ValidateRanksEqual(reference2, tree);
                // test no-op adding existing item, but updating value
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue((int)ranks[i].key, reference2.GetValue((int)ranks[i].key) + (float)Math.PI); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.6", delegate () { tree.ConditionalSetOrAdd((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return true; }); });
                ValidateRanksEqual(reference2, tree);
                // reject changes to key
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                if (tree is AdaptRankListToRankMap<int, float>)
                {
                    IRankList<KeyValue<int, float>> inner = ((AdaptRankListToRankMap<int, float>)tree).Inner;
                    TestThrow(label + " ConditionalSetOrAdd.9", typeof(ArgumentException), delegate () { inner.ConditionalSetOrAdd(new KeyValue<int, float>((int)ranks[i].key), delegate (ref KeyValue<int, float> _key, bool resident) { _key.key++; return false; }); });
                    ValidateRanksEqual(reference2, tree);
                }

                // argument validity
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " ConditionalSetOrAdd.7", typeof(ArgumentNullException), delegate () { tree.ConditionalSetOrAdd((int)ranks[i].key, null); });
                ValidateRanksEqual(reference2, tree);
                // modify tree in callback
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                f = false;
                TestThrow(label + " ConditionalSetOrAdd.8a", typeof(InvalidOperationException), delegate () { tree.ConditionalSetOrAdd((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { tree.Add(p, Value); f = true; return false; }); });
                TestTrue(label + " ConditionalSetOrAdd.8b", delegate () { return f; });
                ValidateRanksEqual(reference2, tree);

                // ConditionalSetOrRemove
                lastIter1 = IncrementIteration(true/*setLast*/);

                p = (i > 0) ? (((int)ranks[i - 1].key + (int)ranks[i].key) / 2) : ((int)ranks[i].key - 1);
                // test no-op with nothing in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.1", delegate () { tree.ConditionalSetOrRemove(p, delegate (int _key, ref float _value, bool resident) { return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test no-op with valid item in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.2", delegate () { tree.ConditionalSetOrRemove((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test no-op changing value of non-existent item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue((int)ranks[i].key, reference2.GetValue((int)ranks[i].key) + (float)Math.PI); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.3", delegate () { tree.ConditionalSetOrRemove((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test changing value of existing item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue((int)ranks[i].key, reference2.GetValue((int)ranks[i].key) + (float)Math.PI); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.4", delegate () { tree.ConditionalSetOrRemove((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return false; }); });
                ValidateRanksEqual(reference2, tree);
                // test no-op removing non-existent item
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.5", delegate () { tree.ConditionalSetOrRemove(p, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return true; }); });
                ValidateRanksEqual(reference2, tree);
                // test removing existing item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Remove((int)ranks[i].key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.6", delegate () { tree.ConditionalSetOrRemove((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { _value = _value + (float)Math.PI; return true; }); });
                ValidateRanksEqual(reference2, tree);

                // argument validity
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " ConditionalSetOrRemove.7", typeof(ArgumentNullException), delegate () { tree.ConditionalSetOrRemove((int)ranks[i].key, null); });
                ValidateRanksEqual(reference2, tree);
                // modify tree in callback
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                f = false;
                TestThrow(label + " ConditionalSetOrRemove.8a", typeof(InvalidOperationException), delegate () { tree.ConditionalSetOrRemove((int)ranks[i].key, delegate (int _key, ref float _value, bool resident) { tree.Add(p, Value); f = true; return false; }); });
                TestTrue(label + " ConditionalSetOrRemove.8b", delegate () { return f; });
                ValidateRanksEqual(reference2, tree);
                // reject changes to key
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                if (tree is AdaptRankListToRankMap<int, float>)
                {
                    IRankList<KeyValue<int, float>> inner = ((AdaptRankListToRankMap<int, float>)tree).Inner;
                    TestThrow(label + " ConditionalSetOrRemove.9", typeof(ArgumentException), delegate () { inner.ConditionalSetOrRemove(new KeyValue<int, float>((int)ranks[i].key), delegate (ref KeyValue<int, float> _key, bool resident) { _key.key++; return false; }); });
                    ValidateRanksEqual(reference2, tree);
                }
            }

            TestOrderedAccessors(
                label,
                makeTree,
                sequence,
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });
        }

        private delegate KeyType NextMethod<KeyType>(KeyType current);
        private delegate KeyType PrevMethod<KeyType>(KeyType current);
        private void TestOrderedAccessors<KeyType, ValueType>(
            string label,
            MakeTree<KeyType, ValueType> makeTree,
            IEnumerable<Op<KeyType, ValueType>> sequence,
            NextMethod<KeyType> getPrevKey,
            PrevMethod<KeyType> getNextKey) where KeyType : IComparable<KeyType>
        {
            label = label + " ordered-accessors";

            IRankMap<KeyType, ValueType> tree;
            ReferenceRankMap<KeyType, ValueType> treeAnalog1 = new ReferenceRankMap<KeyType, ValueType>();

            tree = makeTree();
            BuildTree(tree, treeAnalog1, sequence);
            MultiRankMapEntry[] ranks = ((INonInvasiveMultiRankMapInspection)treeAnalog1).GetRanks();
            MultiRankInfo<KeyType, ValueType>[] treeAnalog = FlattenAnyRankTree<KeyType, ValueType>(treeAnalog1, true/*multi*/);
            Debug.Assert(Array.TrueForAll(treeAnalog, delegate (MultiRankInfo<KeyType, ValueType> item) { return item.Length == 1; }));
            int count = treeAnalog.Length;
            bool empty = (count == 0);

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Count", delegate () { return count == tree.Count; }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " LongCount", delegate () { return count == tree.LongCount; }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Least-1", delegate () { KeyType key; return tree.Least(out key) == !empty; }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Least-2", delegate () { KeyType key; tree.Least(out key); return 0 == Comparer<KeyType>.Default.Compare(key, !empty ? treeAnalog[0].Key : default(KeyType)); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Least-3", delegate () { KeyType key; ValueType value; tree.Least(out key, out value); return (0 == Comparer<KeyType>.Default.Compare(key, !empty ? treeAnalog[0].Key : default(KeyType))) && (0 == Comparer<ValueType>.Default.Compare(value, !empty ? treeAnalog[0].Value : default(ValueType))); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Greatest-1", delegate () { KeyType key; return tree.Greatest(out key) == !empty; }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Greatest-2", delegate () { KeyType key; tree.Greatest(out key); return 0 == Comparer<KeyType>.Default.Compare(key, !empty ? treeAnalog[count - 1].Key : default(KeyType)); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Greatest-2", delegate () { KeyType key; ValueType value; tree.Greatest(out key, out value); return (0 == Comparer<KeyType>.Default.Compare(key, !empty ? treeAnalog[count - 1].Key : default(KeyType))) && (0 == Comparer<ValueType>.Default.Compare(value, !empty ? treeAnalog[count - 1].Value : default(ValueType))); }); });

            if (count == 0)
            {
                KeyType currKey = getNextKey(default(KeyType));

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.1", delegate () { KeyType key; return !tree.NearestLessOrEqual(currKey, out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.2", delegate () { KeyType key; tree.NearestLessOrEqual(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(default(KeyType), key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.3", delegate () { KeyType key; ValueType value; tree.NearestLessOrEqual(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(default(KeyType), key)) && (0 == Comparer<ValueType>.Default.Compare(default(ValueType), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.1", delegate () { KeyType key; return !tree.NearestLessOrEqual(getPrevKey(currKey), out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.2", delegate () { KeyType key; tree.NearestLessOrEqual(getPrevKey(currKey), out key); return 0 == Comparer<KeyType>.Default.Compare(default(KeyType), key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.3", delegate () { KeyType key; ValueType value; tree.NearestLessOrEqual(getPrevKey(currKey), out key, out value); return (0 == Comparer<KeyType>.Default.Compare(default(KeyType), key)) && (0 == Comparer<ValueType>.Default.Compare(default(ValueType), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.1", delegate () { KeyType key; return !tree.NearestGreaterOrEqual(currKey, out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.2", delegate () { KeyType key; tree.NearestGreaterOrEqual(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(default(KeyType), key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.3", delegate () { KeyType key; ValueType value; tree.NearestGreaterOrEqual(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(default(KeyType), key)) && (0 == Comparer<ValueType>.Default.Compare(default(ValueType), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.1", delegate () { KeyType key; return !tree.NearestGreaterOrEqual(getNextKey(currKey), out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.2", delegate () { KeyType key; tree.NearestGreaterOrEqual(getNextKey(currKey), out key); return 0 == Comparer<KeyType>.Default.Compare(default(KeyType), key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.3", delegate () { KeyType key; ValueType value; tree.NearestGreaterOrEqual(getNextKey(currKey), out key, out value); return (0 == Comparer<KeyType>.Default.Compare(default(KeyType), key)) && (0 == Comparer<ValueType>.Default.Compare(default(ValueType), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.1", delegate () { KeyType key; return !tree.NearestLess(currKey, out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.2", delegate () { KeyType key; tree.NearestLess(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(default(KeyType), key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.3", delegate () { KeyType key; ValueType value; tree.NearestLess(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(default(KeyType), key)) && (0 == Comparer<ValueType>.Default.Compare(default(ValueType), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.1", delegate () { KeyType key; return !tree.NearestGreater(currKey, out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.2", delegate () { KeyType key; tree.NearestGreater(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(default(KeyType), key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.3", delegate () { KeyType key; ValueType value; tree.NearestGreater(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(default(KeyType), key)) && (0 == Comparer<ValueType>.Default.Compare(default(ValueType), value)); }); });
            }

            for (int i = 0; i < count; i++)
            {
                KeyType currKey = treeAnalog[i].Key;
                ValueType currValue = treeAnalog[i].Value;
                bool prevKeyExists = i > 0;
                KeyType prevKey = prevKeyExists ? treeAnalog[i - 1].Key : default(KeyType);
                ValueType prevValue = prevKeyExists ? treeAnalog[i - 1].Value : default(ValueType);
                bool nextKeyExists = i < count - 1;
                KeyType nextKey = nextKeyExists ? treeAnalog[i + 1].Key : default(KeyType);
                ValueType nextValue = nextKeyExists ? treeAnalog[i + 1].Value : default(ValueType);

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.1", delegate () { KeyType key; return tree.NearestLessOrEqual(currKey, out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.2", delegate () { KeyType key; tree.NearestLessOrEqual(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(currKey, key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.3", delegate () { KeyType key; ValueType value; tree.NearestLessOrEqual(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(currKey, key)) && (0 == Comparer<ValueType>.Default.Compare(currValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.1", delegate () { KeyType key; return tree.NearestLessOrEqual(getPrevKey(currKey), out key) == prevKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.2", delegate () { KeyType key; tree.NearestLessOrEqual(getPrevKey(currKey), out key); return 0 == Comparer<KeyType>.Default.Compare(prevKey, key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.3", delegate () { KeyType key; ValueType value; tree.NearestLessOrEqual(getPrevKey(currKey), out key, out value); return (0 == Comparer<KeyType>.Default.Compare(prevKey, key)) && (0 == Comparer<ValueType>.Default.Compare(prevValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.1", delegate () { KeyType key; return tree.NearestGreaterOrEqual(currKey, out key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.2", delegate () { KeyType key; tree.NearestGreaterOrEqual(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(currKey, key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.3", delegate () { KeyType key; ValueType value; tree.NearestGreaterOrEqual(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(currKey, key)) && (0 == Comparer<ValueType>.Default.Compare(currValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.1", delegate () { KeyType key; return tree.NearestGreaterOrEqual(getNextKey(currKey), out key) == nextKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.2", delegate () { KeyType key; tree.NearestGreaterOrEqual(getNextKey(currKey), out key); return 0 == Comparer<KeyType>.Default.Compare(nextKey, key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.3", delegate () { KeyType key; ValueType value; tree.NearestGreaterOrEqual(getNextKey(currKey), out key, out value); return (0 == Comparer<KeyType>.Default.Compare(nextKey, key)) && (0 == Comparer<ValueType>.Default.Compare(nextValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.1", delegate () { KeyType key; return tree.NearestLess(currKey, out key) == prevKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.2", delegate () { KeyType key; tree.NearestLess(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(prevKey, key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.3", delegate () { KeyType key; ValueType value; tree.NearestLess(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(prevKey, key)) && (0 == Comparer<ValueType>.Default.Compare(prevValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.1", delegate () { KeyType key; return tree.NearestGreater(currKey, out key) == nextKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.2", delegate () { KeyType key; tree.NearestGreater(currKey, out key); return 0 == Comparer<KeyType>.Default.Compare(nextKey, key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.3", delegate () { KeyType key; ValueType value; tree.NearestGreater(currKey, out key, out value); return (0 == Comparer<KeyType>.Default.Compare(nextKey, key)) && (0 == Comparer<ValueType>.Default.Compare(nextValue, value)); }); });
            }
        }

        public override bool Do()
        {
            try
            {
                this.RankMapBasicCoverage();
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
