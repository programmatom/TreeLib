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

            protected Op(KeyType key)
            {
                this.Key = key;
            }

            public abstract void Do(IOrderedMap<KeyType, ValueType> tree);
            public abstract void Do(IOrderedMap<KeyType, ValueType> tree, List<KeyValuePair<KeyType, ValueType>> treeAnalog);
        }

        public class AddOp<KeyType, ValueType> : Op<KeyType, ValueType> where KeyType : IComparable<KeyType>
        {
            public readonly ValueType Value;

            public AddOp(KeyType key, ValueType value)
                : base(key)
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
            public RemoveOp(KeyType key)
                : base(key)
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
                label + " [1,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(2, "b")},
                5,
                "foo");

            TestAccessorsNotInTree(
                label + " [1,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(8, "l"),
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
                label + " [1,2,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(2, "b"),
                    new AddOp<int, string>(5, "e")},
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [5,2,1]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(2, "b"),
                    new AddOp<int, string>(1, "a") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [5,8,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(8, "l"),
                    new AddOp<int, string>(9, "m") },
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [9,8,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(9, "m"),
                    new AddOp<int, string>(8, "l"),
                    new AddOp<int, string>(5, "e") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [1,2,3,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(1, "a"),
                    new AddOp<int, string>(2, "b"),
                    new AddOp<int, string>(3, "c"),
                    new AddOp<int, string>(5, "e")},
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [5,3,2,1]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(3, "c"),
                    new AddOp<int, string>(2, "b"),
                    new AddOp<int, string>(1, "a") },
                5,
                "foo",
                "e");

            TestAccessorsInTree(
                label + " [5,7,8,9]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(5, "e"),
                    new AddOp<int, string>(7, "k"),
                    new AddOp<int, string>(8, "l"),
                    new AddOp<int, string>(9, "m") },
                5,
                "foo",
                "e");
            TestAccessorsInTree(
                label + " [9,8,7,5]",
                makeTree,
                new Op<int, string>[] {
                    new AddOp<int, string>(9, "m"),
                    new AddOp<int, string>(8, "l"),
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

        private void TestAccessorsNotInTree(
            string label,
            MakeTree<int, string> makeTree,
            IEnumerable<Op<int, string>> sequence,
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
        }

        private void TestAccessorsInTree(
            string label,
            MakeTree<int, string> makeTree,
            IEnumerable<Op<int, string>> sequence,
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

            IOrderedMap<KeyType, ValueType> tree;
            List<KeyValuePair<KeyType, ValueType>> treeAnalog;

            tree = makeTree();
            treeAnalog = new List<KeyValuePair<KeyType, ValueType>>();
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
