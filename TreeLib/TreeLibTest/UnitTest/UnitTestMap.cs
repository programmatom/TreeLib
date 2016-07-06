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
    public class UnitTestMap : TestBase
    {
        public UnitTestMap()
            : base()
        {
        }

        public UnitTestMap(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        public abstract class Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public readonly KeyType Key;

            protected Op(KeyType Key)
            {
                this.Key = Key;
            }

            public abstract void Do(IOrderedMap<KeyType, ValueType> tree);
            public abstract void Do(IOrderedMap<KeyType, ValueType> tree, List<KeyValuePair<KeyType, ValueType>> treeAnalog);
        }

        public class AddOp<KeyType, ValueType> : Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public readonly ValueType Value;

            public AddOp(KeyType Key, ValueType value)
                : base(Key)
            {
                this.Value = value;
            }

            public override void Do(IOrderedMap<KeyType, ValueType> tree)
            {
                tree.Add(this.Key, this.Value);
            }

            public override void Do(IOrderedMap<KeyType, ValueType> tree, List<KeyValuePair<KeyType, ValueType>> treeAnalog)
            {
                tree.Add(this.Key, this.Value);
                treeAnalog.Add(new KeyValuePair<KeyType, ValueType>(this.Key, this.Value));
            }
        }

        public class RemoveOp<KeyType, ValueType> : Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public RemoveOp(KeyType Key)
                : base(Key)
            {
            }

            public override void Do(IOrderedMap<KeyType, ValueType> tree)
            {
                tree.Remove(this.Key);
            }

            public override void Do(IOrderedMap<KeyType, ValueType> tree, List<KeyValuePair<KeyType, ValueType>> treeAnalog)
            {
                tree.Remove(this.Key);
                treeAnalog.RemoveAll(delegate (KeyValuePair<KeyType, ValueType> candidate) { return 0 == Comparer<KeyType>.Default.Compare(this.Key, candidate.Key); });
            }
        }


        private void BuildTree<KeyType, ValueType>(
            IOrderedMap<KeyType, ValueType> tree,
            IEnumerable<Op<KeyType, ValueType>> sequence) where KeyType : IComparable<KeyType>
        {
            long lastIter = IncrementIteration(true/*setLast*/);

            ValidateTree(tree);
            foreach (Op<KeyType, ValueType> op in sequence)
            {
                IncrementIteration();
                op.Do(tree);
                ValidateTree(tree);
            }
            ValidateTree(tree);
        }

        private void BuildTree<KeyType, ValueType>(
            IOrderedMap<KeyType, ValueType> tree,
            List<KeyValuePair<KeyType, ValueType>> treeAnalog,
            IEnumerable<Op<KeyType, ValueType>> sequence) where KeyType : IComparable<KeyType>
        {
            long lastIter = IncrementIteration(true/*setLast*/);

            foreach (Op<KeyType, ValueType> op in sequence)
            {
                IncrementIteration();
                op.Do(tree, treeAnalog);
            }
            treeAnalog.Sort(delegate (KeyValuePair<KeyType, ValueType> l, KeyValuePair<KeyType, ValueType> r) { return Comparer<KeyType>.Default.Compare(l.Key, r.Key); });
            ValidateTree(tree);
        }

        public void TestTree<KeyType, ValueType>(
            string label,
            IOrderedMap<KeyType, ValueType> tree,
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

            ValidateTree(tree);
        }

        public delegate IOrderedMap<KeyType, ValueType> MakeTree<KeyType, ValueType>() where KeyType : IComparable<KeyType>;


        //

        public void MapBasicCoverage()
        {
            MapBasicCoverageSpecific(
                "ReferenceSet<int,string>",
                delegate () { return new ReferenceMap<int, string>(); });



            MapBasicCoverageSpecific(
                "SplayTreeMap<int,string>",
                delegate () { return new SplayTreeMap<int, string>(); });

            MapBasicCoverageSpecific(
                "SplayTreeArrayMap<int,string>",
                delegate () { return new SplayTreeArrayMap<int, string>(); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeList",
                delegate () { return new AdaptListToMap<int, string>(new SplayTreeList<KeyValue<int, string>>()); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeArrayList",
                delegate () { return new AdaptListToMap<int, string>(new SplayTreeArrayList<KeyValue<int, string>>()); });

            // with explicit comparer

            MapBasicCoverageSpecific(
                "SplayTreeMap<int,string>",
                delegate () { return new SplayTreeMap<int, string>(Comparer<int>.Default); });

            MapBasicCoverageSpecific(
                "SplayTreeArrayMap<int,string>",
                delegate () { return new SplayTreeArrayMap<int, string>(Comparer<int>.Default); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeList",
                delegate () { return new AdaptListToMap<int, string>(new SplayTreeList<KeyValue<int, string>>(Comparer<KeyValue<int, string>>.Default)); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:SplayTreeArrayList",
                delegate () { return new AdaptListToMap<int, string>(new SplayTreeArrayList<KeyValue<int, string>>(Comparer<KeyValue<int, string>>.Default)); });



            MapBasicCoverageSpecific(
                "RedBlackTreeMap<int,string>",
                delegate () { return new RedBlackTreeMap<int, string>(); });

            MapBasicCoverageSpecific(
                "RedBlackTreeArrayMap<int,string>",
                delegate () { return new RedBlackTreeArrayMap<int, string>(); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeList",
                delegate () { return new AdaptListToMap<int, string>(new RedBlackTreeList<KeyValue<int, string>>()); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeArrayList",
                delegate () { return new AdaptListToMap<int, string>(new RedBlackTreeArrayList<KeyValue<int, string>>()); });

            // with explicit comparer

            MapBasicCoverageSpecific(
                "RedBlackTreeMap<int,string>",
                delegate () { return new RedBlackTreeMap<int, string>(Comparer<int>.Default); });

            MapBasicCoverageSpecific(
                "RedBlackTreeArrayMap<int,string>",
                delegate () { return new RedBlackTreeArrayMap<int, string>(Comparer<int>.Default); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeList",
                delegate () { return new AdaptListToMap<int, string>(new RedBlackTreeList<KeyValue<int, string>>(Comparer<KeyValue<int, string>>.Default)); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:RedBlackTreeArrayList",
                delegate () { return new AdaptListToMap<int, string>(new RedBlackTreeArrayList<KeyValue<int, string>>(Comparer<KeyValue<int, string>>.Default)); });



            MapBasicCoverageSpecific(
                "AVLTreeMap<int,string>",
                delegate () { return new AVLTreeMap<int, string>(); });

            MapBasicCoverageSpecific(
                "AVLTreeArrayMap<int,string>",
                delegate () { return new AVLTreeArrayMap<int, string>(); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeList",
                delegate () { return new AdaptListToMap<int, string>(new AVLTreeList<KeyValue<int, string>>()); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeArrayList",
                delegate () { return new AdaptListToMap<int, string>(new AVLTreeArrayList<KeyValue<int, string>>()); });

            // with explicit comparer

            MapBasicCoverageSpecific(
                "AVLTreeMap<int,string>",
                delegate () { return new AVLTreeMap<int, string>(Comparer<int>.Default); });

            MapBasicCoverageSpecific(
                "AVLTreeArrayMap<int,string>",
                delegate () { return new AVLTreeArrayMap<int, string>(Comparer<int>.Default); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeList",
                delegate () { return new AdaptListToMap<int, string>(new AVLTreeList<KeyValue<int, string>>(Comparer<KeyValue<int, string>>.Default)); });

            MapBasicCoverageSpecific(
                "AdaptSetToMap<int,string>:AVLTreeArrayList",
                delegate () { return new AdaptListToMap<int, string>(new AVLTreeArrayList<KeyValue<int, string>>(Comparer<KeyValue<int, string>>.Default)); });
        }

        private void MapBasicCoverageSpecific(
            string label,
            MakeTree<int, string> makeTree)
        {
            // tests for item not in tree

            TestAccessorsNotInTree(
                label + " empty",
                makeTree,
                new Op<int, string>[] { },
                5,
                "foo");

            TestAccessorsNotInTree(
                label + " [1]",
                makeTree,
                new Op<int, string>[] { new AddOp<int, string>(1, "a") },
                5,
                "foo");

            TestAccessorsNotInTree(
                label + " [9]",
                makeTree,
                new Op<int, string>[] { new AddOp<int, string>(9, "m") },
                5,
                "foo");

            TestAccessorsNotInTree(
                label + " [1,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(9, "m")},
                5,
                "foo");

            TestAccessorsNotInTree(
                label + " [1,3]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(3, "b")},
                5,
                "foo");

            TestAccessorsNotInTree(
                label + " [7,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(7, "l"),
                    new AddOp<int, string>(9, "m")},
                5,
                "foo");


            // tests for item in tree

            TestAccessorsInTree(
                label + " [5]",
                makeTree,
                new Op<int, string>[] { new AddOp<int, string>(5, "e") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [1,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(5, "e") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [5,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(9, "m")},
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [1,5,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(9, "m")},
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [1,3,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(3, "b"),
                    new AddOp<int, string>(5, "e")},
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [5,3,1]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(3, "b"),
                    new AddOp<int, string>(1, "a") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [5,7,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(7, "l"),
                    new AddOp<int, string>(9, "m") },
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [9,7,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(9, "m"),
                    new AddOp<int, string>(7, "l"),
                    new AddOp<int, string>(5, "e") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [-1,1,3,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(-1, "a"),
                    new AddOp<int, string>(1, "b"),
                    new AddOp<int, string>(3, "c"),
                    new AddOp<int, string>(5, "e")},
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [5,3,1,-1]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(3, "c"),
                    new AddOp<int, string>(1, "b"),
                    new AddOp<int, string>(-1, "a") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [5,7,9,11]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(7, "k"),
                    new AddOp<int, string>(9, "l"),
                    new AddOp<int, string>(11, "m") },
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [11,9,7,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(11, "m"),
                    new AddOp<int, string>(9, "l"),
                    new AddOp<int, string>(7, "k"),
                    new AddOp<int, string>(5, "e") },
                5,
                "foo",
                "e");


            // test ordered accessors

            TestOrderedAccessors(
                label + " empty",
                makeTree,
                new Op<int, string>[] { },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });

            TestOrderedAccessors(
                label + " [5]",
                makeTree,
                new Op<int, string>[] { new AddOp<int, string>(5, "e") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });

            TestOrderedAccessors(
                label + " [5,6]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(6, "f") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });
            TestOrderedAccessors(
                label + " [6,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(6, "f"),
                    new AddOp<int, string>(5, "e") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });

            TestOrderedAccessors(
                label + " [4,5,6]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(4, "d"),
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(6, "f") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });
            TestOrderedAccessors(
                label + " [6,5,4]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(6, "f"),
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(4, "d") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });

            TestOrderedAccessors(
                label + " [3,5,7]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(3, "c"),
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(7, "g") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });
            TestOrderedAccessors(
                label + " [7,5,3]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(7, "g"),
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(3, "c") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });

            TestOrderedAccessors(
                label + " [3,5,7,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(3, "c"),
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(7, "g"),
                    new AddOp<int, string>(9, "i") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });
            TestOrderedAccessors(
                label + " [9,7,5,3]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(9, "i"),
                    new AddOp<int, string>(7, "g"),
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(3, "c") },
                delegate (int k) { return k - 1; },
                delegate (int k) { return k + 1; });
        }

        private void TestBattery(
             string label,
             MakeTree<int, string> makeTree,
             Op<int, string>[] sequence)
        {
            const string Value = "foobar";

            ReferenceMap<int, string> reference = new ReferenceMap<int, string>();
            BuildTree(reference, sequence);

            EntryMap<int, string>[] items = reference.ToArray();

            // test set can't have keys that are adjacent, due to structure of tests below
            for (int i = 1; i < items.Length; i++)
            {
                if (!(items[i - 1].Key < items[i].Key - 1))
                {
                    Fault(new object(), "Test prerequisite violated");
                }
            }

            // test items in collection
            for (int i = 0; i < items.Length; i++)
            {
                IOrderedMap<int, string> tree;
                ReferenceMap<int, string> reference2;
                int p;
                bool f;

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Count", delegate () { return items.Length == unchecked((int)tree.Count); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " LongCount", delegate () { return items.Length == unchecked((int)tree.LongCount); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " ContainsKey", delegate () { return tree.ContainsKey(items[i].Key); });

                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryAdd(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryAdd.1", delegate () { return tree.TryAdd(p, Value); });
                ValidateMapsEqual(reference2, tree);
                //
                p = (i < items.Length - 1) ? ((items[i].Key + items[i + 1].Key) / 2) : (items[i].Key + 1);
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryAdd(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryAdd.2", delegate () { return tree.TryAdd(p, Value); });
                ValidateMapsEqual(reference2, tree);
                //
                p = items[i].Key;
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryAdd.3", delegate () { return tree.TryAdd(p, Value); });
                ValidateMapsEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryRemove(items[i].Key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryRemove.1", delegate () { return tree.TryRemove(items[i].Key); });
                ValidateMapsEqual(reference2, tree);
                //
                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryRemove.2", delegate () { return tree.TryRemove(p); });
                ValidateMapsEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySetValue(items[i].Key, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySetValue.1", delegate () { return tree.TrySetValue(items[i].Key, Value); });
                ValidateMapsEqual(reference2, tree);
                //
                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TrySetValue.2", delegate () { return tree.TrySetValue(p, Value); });
                ValidateMapsEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetValue.1a", delegate () { string value; return tree.TryGetValue(items[i].Key, out value); });
                TestTrue(label + " TryGetValue.1b", delegate () { string value; tree.TryGetValue(items[i].Key, out value); return value == items[i].Value; });
                //
                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestFalse(label + " TryGetValue.2a", delegate () { string value; return tree.TryGetValue(p, out value); });
                TestTrue(label + " TryGetValue.2b", delegate () { string value; tree.TryGetValue(p, out value); return value == default(string); });

                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Add.1", delegate () { tree.Add(p, Value); });
                ValidateMapsEqual(reference2, tree);
                //
                p = (i < items.Length - 1) ? ((items[i].Key + items[i + 1].Key) / 2) : (items[i].Key + 1);
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Add.2", delegate () { tree.Add(p, Value); });
                ValidateMapsEqual(reference2, tree);
                //
                p = items[i].Key;
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Add.3", typeof(ArgumentException), delegate () { tree.Add(p, Value); });
                ValidateMapsEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Remove(items[i].Key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Remove.1", delegate () { tree.Remove(items[i].Key); });
                ValidateMapsEqual(reference2, tree);
                //
                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Remove.2", typeof(ArgumentException), delegate () { tree.Remove(p); });
                ValidateMapsEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue(items[i].Key, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetValue.1", delegate () { tree.SetValue(items[i].Key, Value); });
                ValidateMapsEqual(reference2, tree);
                //
                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " SetValue.2", typeof(ArgumentException), delegate () { tree.SetValue(p, Value); });
                ValidateMapsEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetValue.1a", delegate () { string value = tree.GetValue(items[i].Key); });
                TestTrue(label + " GetValue.1b", delegate () { string value = tree.GetValue(items[i].Key); return value == items[i].Value; });
                //
                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " GetValue.2a", typeof(ArgumentException), delegate () { string value = tree.GetValue(p); });

                // ConditionalSetOrAdd
                long lastIter1 = IncrementIteration(true/*setLast*/);

                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                // test no-op with nothing in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.1", delegate () { tree.ConditionalSetOrAdd(p, delegate (int _key, ref string _value, bool resident) { return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test no-op with valid item in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.2", delegate () { tree.ConditionalSetOrAdd(items[i].Key, delegate (int _key, ref string _value, bool resident) { return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test no-op changing value of non-existent item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue(items[i].Key, String.Concat(reference2.GetValue(items[i].Key), "-foo2")); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.3", delegate () { tree.ConditionalSetOrAdd(items[i].Key, delegate (int _key, ref string _value, bool resident) { _value = String.Concat(_value, "-foo2"); return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test changing value of existing item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue(items[i].Key, String.Concat(reference2.GetValue(items[i].Key), "-foo2")); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.4", delegate () { tree.ConditionalSetOrAdd(items[i].Key, delegate (int _key, ref string _value, bool resident) { _value = String.Concat(_value, "-foo2"); return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test adding value of non-existent item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Add(p, "-foo2"); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.5", delegate () { tree.ConditionalSetOrAdd(p, delegate (int _key, ref string _value, bool resident) { _value = String.Concat(_value, "-foo2"); return true; }); });
                ValidateMapsEqual(reference2, tree);
                // test no-op adding existing item, but updating value
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue(items[i].Key, String.Concat(reference2.GetValue(items[i].Key), "-foo2")); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrAdd.6", delegate () { tree.ConditionalSetOrAdd(items[i].Key, delegate (int _key, ref string _value, bool resident) { _value = String.Concat(_value, "-foo2"); return true; }); });
                ValidateMapsEqual(reference2, tree);

                // argument validity
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " ConditionalSetOrAdd.7", typeof(ArgumentNullException), delegate () { tree.ConditionalSetOrAdd(items[i].Key, null); });
                ValidateMapsEqual(reference2, tree);
                // modify tree in callback
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                f = false;
                TestThrow(label + " ConditionalSetOrAdd.8a", typeof(InvalidOperationException), delegate () { tree.ConditionalSetOrAdd(items[i].Key, delegate (int _key, ref string _value, bool resident) { tree.Add(p, Value); f = true; return false; }); });
                TestTrue(label + " ConditionalSetOrAdd.8b", delegate () { return f; });
                ValidateMapsEqual(reference2, tree);
                // reject changes to key
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                if (tree is AdaptListToMap<int, string>)
                {
                    IOrderedList<KeyValue<int, string>> inner = ((AdaptListToMap<int, string>)tree).Inner;
                    TestThrow(label + " ConditionalSetOrAdd.9", typeof(ArgumentException), delegate () { inner.ConditionalSetOrAdd(new KeyValue<int, string>(items[i].Key), delegate (ref KeyValue<int, string> _key, bool resident) { _key.key++; return false; }); });
                    ValidateMapsEqual(reference2, tree);
                }

                // ConditionalSetOrRemove
                lastIter1 = IncrementIteration(true/*setLast*/);

                p = (i > 0) ? ((items[i - 1].Key + items[i].Key) / 2) : (items[i].Key - 1);
                // test no-op with nothing in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.1", delegate () { tree.ConditionalSetOrRemove(p, delegate (int _key, ref string _value, bool resident) { return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test no-op with valid item in tree
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.2", delegate () { tree.ConditionalSetOrRemove(items[i].Key, delegate (int _key, ref string _value, bool resident) { return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test no-op changing value of non-existent item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue(items[i].Key, reference2.GetValue(items[i].Key) + "-foo2"); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.3", delegate () { tree.ConditionalSetOrRemove(items[i].Key, delegate (int _key, ref string _value, bool resident) { _value = _value + "-foo2"; return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test changing value of existing item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetValue(items[i].Key, reference2.GetValue(items[i].Key) + "-foo2"); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.4", delegate () { tree.ConditionalSetOrRemove(items[i].Key, delegate (int _key, ref string _value, bool resident) { _value = _value + "-foo2"; return false; }); });
                ValidateMapsEqual(reference2, tree);
                // test no-op removing non-existent item
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.5", delegate () { tree.ConditionalSetOrRemove(p, delegate (int _key, ref string _value, bool resident) { _value = _value + "-foo2"; return true; }); });
                ValidateMapsEqual(reference2, tree);
                // test removing existing item
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Remove(items[i].Key); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " ConditionalSetOrRemove.6", delegate () { tree.ConditionalSetOrRemove(items[i].Key, delegate (int _key, ref string _value, bool resident) { _value = _value + "-foo2"; return true; }); });
                ValidateMapsEqual(reference2, tree);

                // argument validity
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " ConditionalSetOrRemove.7", typeof(ArgumentNullException), delegate () { tree.ConditionalSetOrRemove(items[i].Key, null); });
                ValidateMapsEqual(reference2, tree);
                // modify tree in callback
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                f = false;
                TestThrow(label + " ConditionalSetOrRemove.8a", typeof(InvalidOperationException), delegate () { tree.ConditionalSetOrRemove(items[i].Key, delegate (int _key, ref string _value, bool resident) { tree.Add(p, Value); f = true; return false; }); });
                TestTrue(label + " ConditionalSetOrRemove.8b", delegate () { return f; });
                ValidateMapsEqual(reference2, tree);
                // reject changes to key
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                if (tree is AdaptListToMap<int, string>)
                {
                    IOrderedList<KeyValue<int, string>> inner = ((AdaptListToMap<int, string>)tree).Inner;
                    TestThrow(label + " ConditionalSetOrRemove.9", typeof(ArgumentException), delegate () { inner.ConditionalSetOrRemove(new KeyValue<int, string>(items[i].Key), delegate (ref KeyValue<int, string> _key, bool resident) { _key.key++; return false; }); });
                    ValidateMapsEqual(reference2, tree);
                }
            }
        }

        private void TestAccessorsNotInTree(
            string label,
            MakeTree<int, string> makeTree,
            Op<int, string>[] sequence,
            int testKey,
            string testValue)
        {
            label = label + " not-in-tree";

            IOrderedMap<int, string> tree;

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " ContainsKey", delegate () { return !tree.ContainsKey(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " SetOrAddValue-1", delegate () { return tree.SetOrAddValue(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " SetOrAddValue-2", delegate () { tree.SetOrAddValue(testKey, testValue); return String.Equals(tree.GetValue(testKey), testValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryAdd-1", delegate () { return tree.TryAdd(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryAdd-2", delegate () { tree.TryAdd(testKey, testValue); return String.Equals(tree.GetValue(testKey), testValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryRemove", delegate () { return !tree.TryRemove(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryGetValue", delegate () { string value; return !tree.TryGetValue(testKey, out value); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TrySetValue-1", delegate () { return !tree.TrySetValue(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TrySetValue-2", delegate () { tree.TrySetValue(testKey, testValue); return !tree.ContainsKey(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestNoThrow(label + " Add-1", delegate () { tree.Add(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Add-2", delegate () { tree.Add(testKey, testValue); return String.Equals(tree.GetValue(testKey), testValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestThrow(label + " Remove", typeof(ArgumentException), delegate () { tree.Remove(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestThrow(label + " GetValue", typeof(ArgumentException), delegate () { tree.GetValue(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestThrow(label + " SetValue-1", typeof(ArgumentException), delegate () { tree.SetValue(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " SetValue-2", delegate () { try { tree.SetValue(testKey, testValue); } catch { } return !tree.ContainsKey(testKey); }); });


            TestBattery(
                label,
                makeTree,
                sequence);
        }

        private void TestAccessorsInTree(
            string label,
            MakeTree<int, string> makeTree,
            Op<int, string>[] sequence,
            int testKey,
            string testValue,
            string oldValue)
        {
            label = label + " in-tree";

            IOrderedMap<int, string> tree;

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " ContainsKey", delegate () { return tree.ContainsKey(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " SetOrAddValue-1", delegate () { return !tree.SetOrAddValue(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " SetOrAddValue-2", delegate () { tree.SetOrAddValue(testKey, testValue); return String.Equals(tree.GetValue(testKey), testValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryAdd-1", delegate () { return !tree.TryAdd(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryAdd-2", delegate () { tree.TryAdd(testKey, testValue); return String.Equals(tree.GetValue(testKey), oldValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryRemove-1", delegate () { return tree.TryRemove(testKey); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryRemove-2", delegate () { tree.TryRemove(testKey); return !tree.ContainsKey(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryGetValue-1", delegate () { string value; return tree.TryGetValue(testKey, out value); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TryGetValue-2", delegate () { string value; tree.TryGetValue(testKey, out value); return String.Equals(value, oldValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TrySetValue-1", delegate () { return tree.TrySetValue(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " TrySetValue-2", delegate () { tree.TrySetValue(testKey, testValue); return String.Equals(tree.GetValue(testKey), testValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestThrow(label + " Add-1", typeof(ArgumentException), delegate () { tree.Add(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Add-2", delegate () { try { tree.Add(testKey, testValue); } catch { } return String.Equals(tree.GetValue(testKey), oldValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestNoThrow(label + " Remove-1", delegate () { tree.Remove(testKey); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Remove-2", delegate () { tree.Remove(testKey); return !tree.ContainsKey(testKey); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " GetValue", delegate () { return String.Equals(tree.GetValue(testKey), oldValue); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestNoThrow(label + " SetValue-1", delegate () { tree.SetValue(testKey, testValue); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " SetValue-2", delegate () { tree.SetValue(testKey, testValue); return String.Equals(tree.GetValue(testKey), testValue); }); });


            TestBattery(
                label,
                makeTree,
                sequence);
        }

        private delegate int NextMethod(int current);
        private delegate int PrevMethod(int current);
        private void TestOrderedAccessors(
            string label,
            MakeTree<int, string> makeTree,
            Op<int, string>[] sequence,
            NextMethod getPrevKey,
            PrevMethod getNextKey)
        {
            label = label + " ordered-accessors";

            IOrderedMap<int, string> tree;
            List<KeyValuePair<int, string>> treeAnalog;

            tree = makeTree();
            treeAnalog = new List<KeyValuePair<int, string>>();
            BuildTree(tree, treeAnalog, sequence);
            int count = treeAnalog.Count;
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
                delegate () { TestTrue(label + " Least-1", delegate () { int Key; return tree.Least(out Key) == !empty; }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Least-2", delegate () { int Key; tree.Least(out Key); return 0 == Comparer<int>.Default.Compare(Key, !empty ? treeAnalog[0].Key : default(int)); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Least-3", delegate () { int Key; string value; tree.Least(out Key, out value); return (0 == Comparer<int>.Default.Compare(Key, !empty ? treeAnalog[0].Key : default(int))) && (0 == Comparer<string>.Default.Compare(value, !empty ? treeAnalog[0].Value : default(string))); }); });

            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Greatest-1", delegate () { int Key; return tree.Greatest(out Key) == !empty; }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Greatest-2", delegate () { int Key; tree.Greatest(out Key); return 0 == Comparer<int>.Default.Compare(Key, !empty ? treeAnalog[count - 1].Key : default(int)); }); });
            tree = makeTree();
            TestTree(
                label,
                tree,
                sequence,
                delegate () { TestTrue(label + " Greatest-2", delegate () { int Key; string value; tree.Greatest(out Key, out value); return (0 == Comparer<int>.Default.Compare(Key, !empty ? treeAnalog[count - 1].Key : default(int))) && (0 == Comparer<string>.Default.Compare(value, !empty ? treeAnalog[count - 1].Value : default(string))); }); });

            if (count == 0)
            {
                int currKey = getNextKey(default(int));

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.1", delegate () { int Key; return !tree.NearestLessOrEqual(currKey, out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.2", delegate () { int Key; tree.NearestLessOrEqual(currKey, out Key); return 0 == Comparer<int>.Default.Compare(default(int), Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.3", delegate () { int Key; string value; tree.NearestLessOrEqual(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(default(int), Key)) && (0 == Comparer<string>.Default.Compare(default(string), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.1", delegate () { int Key; return !tree.NearestLessOrEqual(getPrevKey(currKey), out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.2", delegate () { int Key; tree.NearestLessOrEqual(getPrevKey(currKey), out Key); return 0 == Comparer<int>.Default.Compare(default(int), Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.3", delegate () { int Key; string value; tree.NearestLessOrEqual(getPrevKey(currKey), out Key, out value); return (0 == Comparer<int>.Default.Compare(default(int), Key)) && (0 == Comparer<string>.Default.Compare(default(string), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.1", delegate () { int Key; return !tree.NearestGreaterOrEqual(currKey, out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.2", delegate () { int Key; tree.NearestGreaterOrEqual(currKey, out Key); return 0 == Comparer<int>.Default.Compare(default(int), Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.3", delegate () { int Key; string value; tree.NearestGreaterOrEqual(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(default(int), Key)) && (0 == Comparer<string>.Default.Compare(default(string), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.1", delegate () { int Key; return !tree.NearestGreaterOrEqual(getNextKey(currKey), out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.2", delegate () { int Key; tree.NearestGreaterOrEqual(getNextKey(currKey), out Key); return 0 == Comparer<int>.Default.Compare(default(int), Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.3", delegate () { int Key; string value; tree.NearestGreaterOrEqual(getNextKey(currKey), out Key, out value); return (0 == Comparer<int>.Default.Compare(default(int), Key)) && (0 == Comparer<string>.Default.Compare(default(string), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.1", delegate () { int Key; return !tree.NearestLess(currKey, out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.2", delegate () { int Key; tree.NearestLess(currKey, out Key); return 0 == Comparer<int>.Default.Compare(default(int), Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.3", delegate () { int Key; string value; tree.NearestLess(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(default(int), Key)) && (0 == Comparer<string>.Default.Compare(default(string), value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.1", delegate () { int Key; return !tree.NearestGreater(currKey, out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.2", delegate () { int Key; tree.NearestGreater(currKey, out Key); return 0 == Comparer<int>.Default.Compare(default(int), Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.3", delegate () { int Key; string value; tree.NearestGreater(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(default(int), Key)) && (0 == Comparer<string>.Default.Compare(default(string), value)); }); });
            }

            for (int i = 0; i < count; i++)
            {
                int currKey = treeAnalog[i].Key;
                string currValue = treeAnalog[i].Value;
                bool prevKeyExists = i > 0;
                int prevKey = prevKeyExists ? treeAnalog[i - 1].Key : default(int);
                string prevValue = prevKeyExists ? treeAnalog[i - 1].Value : default(string);
                bool nextKeyExists = i < count - 1;
                int nextKey = nextKeyExists ? treeAnalog[i + 1].Key : default(int);
                string nextValue = nextKeyExists ? treeAnalog[i + 1].Value : default(string);

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.1", delegate () { int Key; return tree.NearestLessOrEqual(currKey, out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.2", delegate () { int Key; tree.NearestLessOrEqual(currKey, out Key); return 0 == Comparer<int>.Default.Compare(currKey, Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual+0.3", delegate () { int Key; string value; tree.NearestLessOrEqual(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(currKey, Key)) && (0 == Comparer<string>.Default.Compare(currValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.1", delegate () { int Key; return tree.NearestLessOrEqual(getPrevKey(currKey), out Key) == prevKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.2", delegate () { int Key; tree.NearestLessOrEqual(getPrevKey(currKey), out Key); return 0 == Comparer<int>.Default.Compare(prevKey, Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLessOrEqual-1.3", delegate () { int Key; string value; tree.NearestLessOrEqual(getPrevKey(currKey), out Key, out value); return (0 == Comparer<int>.Default.Compare(prevKey, Key)) && (0 == Comparer<string>.Default.Compare(prevValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.1", delegate () { int Key; return tree.NearestGreaterOrEqual(currKey, out Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.2", delegate () { int Key; tree.NearestGreaterOrEqual(currKey, out Key); return 0 == Comparer<int>.Default.Compare(currKey, Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+0.3", delegate () { int Key; string value; tree.NearestGreaterOrEqual(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(currKey, Key)) && (0 == Comparer<string>.Default.Compare(currValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.1", delegate () { int Key; return tree.NearestGreaterOrEqual(getNextKey(currKey), out Key) == nextKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.2", delegate () { int Key; tree.NearestGreaterOrEqual(getNextKey(currKey), out Key); return 0 == Comparer<int>.Default.Compare(nextKey, Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreaterOrEqual+1.3", delegate () { int Key; string value; tree.NearestGreaterOrEqual(getNextKey(currKey), out Key, out value); return (0 == Comparer<int>.Default.Compare(nextKey, Key)) && (0 == Comparer<string>.Default.Compare(nextValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.1", delegate () { int Key; return tree.NearestLess(currKey, out Key) == prevKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.2", delegate () { int Key; tree.NearestLess(currKey, out Key); return 0 == Comparer<int>.Default.Compare(prevKey, Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestLess.3", delegate () { int Key; string value; tree.NearestLess(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(prevKey, Key)) && (0 == Comparer<string>.Default.Compare(prevValue, value)); }); });

                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.1", delegate () { int Key; return tree.NearestGreater(currKey, out Key) == nextKeyExists; }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.2", delegate () { int Key; tree.NearestGreater(currKey, out Key); return 0 == Comparer<int>.Default.Compare(nextKey, Key); }); });
                tree = makeTree();
                TestTree(
                    label,
                    tree,
                    sequence,
                    delegate () { TestTrue(label + " NearestGreater.3", delegate () { int Key; string value; tree.NearestGreater(currKey, out Key, out value); return (0 == Comparer<int>.Default.Compare(nextKey, Key)) && (0 == Comparer<string>.Default.Compare(nextValue, value)); }); });
            }
        }

        public override bool Do()
        {
            try
            {
                this.MapBasicCoverage();
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
