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
    public class UnitTestRangeMap : TestBase
    {
        public UnitTestRangeMap(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        public abstract class Op<ValueType>
        {
            public readonly int start;

            protected Op(int start)
            {
                this.start = start;
            }

            public abstract void Do(IRangeMap<ValueType> tree);
            public abstract void Do(IRangeMap<ValueType> tree, IRangeMap<ValueType> treeAnalog);
        }

        public class AddOp<ValueType> : Op<ValueType>
        {
            public readonly int length;
            public readonly ValueType value;

            public AddOp(int start, int length, ValueType value)
                : base(start)
            {
                this.length = length;
                this.value = value;
            }

            public override void Do(IRangeMap<ValueType> tree)
            {
                tree.Insert(start, length, value);
            }

            public override void Do(IRangeMap<ValueType> tree, IRangeMap<ValueType> treeAnalog)
            {
                tree.Insert(start, length, value);
                treeAnalog.Insert(start, length, value);
            }
        }

        public class RemoveOp<ValueType> : Op<ValueType>
        {
            public RemoveOp(int start)
                : base(start)
            {
            }

            public override void Do(IRangeMap<ValueType> tree)
            {
                tree.Delete(start);
            }

            public override void Do(IRangeMap<ValueType> tree, IRangeMap<ValueType> treeAnalog)
            {
                tree.Delete(start);
                treeAnalog.Delete(start);
            }
        }


        protected void ValidateRanges(Range2MapEntry[] ranges)
        {
            IncrementIteration();
            int offsetX = 0;
            for (int i = 0; i < ranges.Length; i++)
            {
                TestTrue("xStart", delegate () { return offsetX == ranges[i].x.start; });
                TestTrue("yStart", delegate () { return 0 == ranges[i].y.start; });
                TestTrue("xLength > 0", delegate () { return ranges[i].x.length > 0; });
                TestTrue("yLength > 0", delegate () { return ranges[i].y.length == 0; });
                offsetX += ranges[i].x.length;
            }
        }

        protected void ValidateRangesEqual<ValueType>(Range2MapEntry[] ranges1, Range2MapEntry[] ranges2) where ValueType : IComparable<ValueType>
        {
            IncrementIteration();
            TestTrue("equal count", delegate () { return ranges1.Length == ranges2.Length; });
            for (int i = 0; i < ranges1.Length; i++)
            {
                TestTrue("xStart", delegate () { return ranges1[i].x.start == ranges2[i].x.start; });
                TestTrue("yStart", delegate () { return ranges1[i].y.start == ranges2[i].y.start; });
                TestTrue("xLength", delegate () { return ranges1[i].x.length == ranges2[i].x.length; });
                TestTrue("yLength", delegate () { return ranges1[i].y.length == ranges2[i].y.length; });
                TestTrue("value", delegate () { return Comparer<ValueType>.Default.Compare((ValueType)ranges1[i].value, (ValueType)ranges2[i].value) == 0; });
            }
        }

        protected void ValidateRangesEqual<ValueType>(IRangeMap<ValueType> tree1, IRangeMap<ValueType> tree2) where ValueType : IComparable<ValueType>
        {
            IncrementIteration();
            Range2MapEntry[] ranges1 = ((INonInvasiveRange2MapInspection)tree1).GetRanges();
            ValidateRanges(ranges1);
            Range2MapEntry[] ranges2 = ((INonInvasiveRange2MapInspection)tree2).GetRanges();
            ValidateRanges(ranges2);
            ValidateRangesEqual<ValueType>(ranges1, ranges2);
            TestTrue("GetExtent", delegate () { return tree1.GetExtent() == tree2.GetExtent(); });
        }


        private void BuildTree<ValueType>(
            IRangeMap<ValueType> tree,
            IEnumerable<Op<ValueType>> sequence) where ValueType : IComparable<ValueType>
        {
            ValidateTree(tree);
            foreach (Op<ValueType> op in sequence)
            {
                IncrementIteration();
                op.Do(tree);
                ValidateTree(tree);
                ValidateRanges(((INonInvasiveRange2MapInspection)tree).GetRanges());
            }
        }

        private void BuildTree<ValueType>(
            IRangeMap<ValueType> tree,
            IRangeMap<ValueType> treeAnalog,
            IEnumerable<Op<ValueType>> sequence) where ValueType : IComparable<ValueType>
        {
            ValidateTree(tree);
            foreach (Op<ValueType> op in sequence)
            {
                IncrementIteration();
                op.Do(tree, treeAnalog);
                ValidateTree(tree);
            }
            ValidateRangesEqual(tree, treeAnalog);
        }

        private void TestTree<ValueType>(
            string label,
            IRangeMap<ValueType> tree,
            IEnumerable<Op<ValueType>> sequence,
            VoidAction action) where ValueType : IComparable<ValueType>
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

            ValidateRanges(((INonInvasiveRange2MapInspection)tree).GetRanges());
        }

        private delegate IRangeMap<ValueType> MakeTree<ValueType>();


        //

        public void RangeMapBasicCoverage()
        {
            RangeTreeBasicCoverageSpecific(
                "ReferenceRangeMap<string>",
                delegate () { return new ReferenceRangeMap<string>(); });



            RangeTreeBasicCoverageSpecific(
                "SplayTreeRangeMap<string>",
                delegate () { return new SplayTreeRangeMap<string>(); });

            RangeTreeBasicCoverageSpecific(
                "SplayTreeArrayRangeMap<string>",
                delegate () { return new SplayTreeArrayRangeMap<string>(); });

            RangeTreeBasicCoverageSpecific(
                "SplayTreeRangeMapLong<string>",
                delegate () { return new AdaptRangeMapToRangeMapLong<string>(new SplayTreeRangeMapLong<string>()); });



            RangeTreeBasicCoverageSpecific(
                "RedBlackTreeRangeMap<string>",
                delegate () { return new RedBlackTreeRangeMap<string>(); });

            RangeTreeBasicCoverageSpecific(
                "RedBlackTreeArrayRangeMap<string>",
                delegate () { return new RedBlackTreeArrayRangeMap<string>(); });

            RangeTreeBasicCoverageSpecific(
                "RedBlackTreeRangeMapLong<string>",
                delegate () { return new AdaptRangeMapToRangeMapLong<string>(new RedBlackTreeRangeMapLong<string>()); });



            RangeTreeBasicCoverageSpecific(
                "AVLTreeRangeMap<string>",
                delegate () { return new AVLTreeRangeMap<string>(); });

            RangeTreeBasicCoverageSpecific(
                "AVLTreeArrayRangeMap<string>",
                delegate () { return new AVLTreeArrayRangeMap<string>(); });

            RangeTreeBasicCoverageSpecific(
                "AVLTreeRangeMapLong<string>",
                delegate () { return new AdaptRangeMapToRangeMapLong<string>(new AVLTreeRangeMapLong<string>()); });
        }

        private void RangeTreeBasicCoverageSpecific(
            string label,
            MakeTree<string> makeTree)
        {
            TestBasic(
                "Basic",
                makeTree);


            TestBattery(
                "[]",
                makeTree,
                new Op<string>[] { });


            TestBattery(
                "[0(+1)]",
                makeTree,
                new Op<string>[] { new AddOp<string>(0, 1, "a") });

            TestBattery(
                "[0(+2)]",
                makeTree,
                new Op<string>[] { new AddOp<string>(0, 2, "a") });


            TestBattery(
                "[0(+1), 1(+1)]",
                makeTree,
                new Op<string>[] {
                    new AddOp<string>(0, 1, "a"),
                    new AddOp<string>(1, 1, "b")
                });

            TestBattery(
                "[0(+2), 2(+2)]",
                makeTree,
                new Op<string>[] {
                    new AddOp<string>(0, 2, "a"),
                    new AddOp<string>(2, 2, "b")
                });

            TestBattery(
                "[0(+1), 1(+2)]",
                makeTree,
                new Op<string>[] {
                    new AddOp<string>(0, 1, "a"),
                    new AddOp<string>(1, 2, "b")
                });

            TestBattery(
                "[0(+3), 3(+2)]",
                makeTree,
                new Op<string>[] {
                    new AddOp<string>(0, 3, "a"),
                    new AddOp<string>(3, 2, "b")
                });


            TestBattery(
                "[0(+1), 1(+1), 2(+1)]",
                makeTree,
                new Op<string>[] {
                    new AddOp<string>(0, 1, "a"),
                    new AddOp<string>(1, 1, "b"),
                    new AddOp<string>(2, 1, "c")
                });

            TestBattery(
                "[0(+2), 2(+2), 4(+2)]",
                makeTree,
                new Op<string>[] {
                    new AddOp<string>(0, 2, "a"),
                    new AddOp<string>(2, 2, "b"),
                    new AddOp<string>(4, 2, "c")
                });

            TestBattery(
                "[0(+3), 3(+1), 4(+2)]",
                makeTree,
                new Op<string>[] {
                    new AddOp<string>(0, 3, "a"),
                    new AddOp<string>(3, 1, "b"),
                    new AddOp<string>(4, 2, "c")
                });
        }

        private void TestBasic(
            string label,
            MakeTree<string> makeTree)
        {
            label = label + " basic";

            IRangeMap<string> tree;

            // argument checking

            tree = makeTree();
            TestTrue(label + " TryInsert.1", delegate () { return tree.TryInsert(0, 1, "foo"); });
            tree = makeTree();
            TestThrow(label + " TryInsert.2", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(0, 0, "foo"); });
            tree = makeTree();
            TestThrow(label + " TryInsert.3", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(0, -1, "foo"); });
            tree = makeTree();
            TestThrow(label + " TryInsert.4", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(-1, 1, "foo"); });

            tree = makeTree();
            TestTrue(label + " TrySetLength.1a", delegate () { return tree.TryInsert(0, 1, "foo"); });
            TestNoThrow(label + " TrySetLength.1b", delegate () { tree.TrySetLength(0, 1); });
            TestThrow(label + " TrySetLength.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.TrySetLength(0, 0); });
            tree = makeTree();
            TestTrue(label + " TrySetLength.2a", delegate () { return tree.TryInsert(0, 1, "foo"); });
            TestNoThrow(label + " TrySetLength.2b", delegate () { tree.TrySetLength(0, 1); });
            TestThrow(label + " TrySetLength.2c", typeof(ArgumentOutOfRangeException), delegate () { tree.TrySetLength(0, -1); });

            tree = makeTree();
            TestTrue(label + " TrySet.1a", delegate () { return tree.TryInsert(0, 1, "foo"); });
            TestNoThrow(label + " TrySet.1b", delegate () { tree.TrySet(0, 2, "bar"); });
            TestThrow(label + " TrySet.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.TrySet(0, -1, "bar"); });
            TestNoThrow(label + " TrySet.1d", delegate () { tree.TrySet(0, 0, "bar"); });
            tree = makeTree();
            TestFalse(label + " TrySet.1gi", delegate () { return tree.TrySet(1, 1, "baz"); });
            TestTrue(label + " TrySet.1gii", delegate () { return tree.Count == 0; });

            tree = makeTree();
            TestNoThrow(label + " Insert.1", delegate () { tree.Insert(0, 1, "foo"); });
            tree = makeTree();
            TestThrow(label + " Insert.2", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(0, 0, "foo"); });
            tree = makeTree();
            TestThrow(label + " Insert.4", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(0, -1, "foo"); });
            tree = makeTree();
            TestThrow(label + " Insert.6", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(-1, 1, "foo"); });

            tree = makeTree();
            TestTrue(label + " SetLength.1a", delegate () { return tree.TryInsert(0, 1, "foo"); });
            TestNoThrow(label + " SetLength.1b", delegate () { tree.SetLength(0, 1); });
            TestThrow(label + " SetLength.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.SetLength(0, 0); });
            tree = makeTree();
            TestTrue(label + " SetLength.2a", delegate () { return tree.TryInsert(0, 1, "foo"); });
            TestNoThrow(label + " SetLength.2b", delegate () { tree.SetLength(0, 1); });
            TestThrow(label + " SetLength.2c", typeof(ArgumentOutOfRangeException), delegate () { tree.SetLength(0, -1); });

            tree = makeTree();
            TestTrue(label + " Set.1a", delegate () { return tree.TryInsert(0, 1, "foo"); });
            TestNoThrow(label + " Set.1b", delegate () { tree.Set(0, 2, "bar"); });
            TestThrow(label + " Set.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.Set(0, -1, "bar"); });
            TestNoThrow(label + " Set.1f", delegate () { tree.Set(0, 0, "bar"); });
            tree = makeTree();
            TestThrow(label + " TrySet.1gi", typeof(ArgumentException), delegate () { tree.Set(1, 1, "baz"); });
            TestTrue(label + " TrySet.1gii", delegate () { return tree.Count == 0; });

            tree = makeTree();
            TestTrue(label + " GetExtent degenerate", delegate () { return tree.GetExtent() == 0; });
            TestFalse(label + " NearestLessOrEqual degenerate 1", delegate () { int nearest; return tree.NearestLessOrEqual(1, out nearest); });
            TestTrue(label + " NearestLessOrEqual degenerate 2", delegate () { int nearest; tree.NearestLessOrEqual(1, out nearest); return nearest == 0; });
            TestFalse(label + " NearestLess degenerate 1", delegate () { int nearest; return tree.NearestLess(1, out nearest); });
            TestTrue(label + " NearestLess degenerate 2", delegate () { int nearest; tree.NearestLess(1, out nearest); return nearest == 0; });
            TestFalse(label + " NearestGreaterOrEqual degenerate 1", delegate () { int nearest; return tree.NearestGreaterOrEqual(1, out nearest); });
            TestTrue(label + " NearestGreaterOrEqual degenerate 2", delegate () { int nearest; tree.NearestGreaterOrEqual(1, out nearest); return nearest == 0; });
            TestFalse(label + " NearestGreater degenerate 1", delegate () { int nearest; return tree.NearestGreater(1, out nearest); });
            TestTrue(label + " NearestGreater degenerate 2", delegate () { int nearest; tree.NearestGreater(1, out nearest); return nearest == 0; });

            // basic functionality

            tree = makeTree();
            BuildTree(
                tree,
                new Op<string>[] {
                    new AddOp<string>(0, 1, "a"),
                });
            ValidateRangesEqual<string>(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 1), new Range(), "a"),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<string>[] {
                    new AddOp<string>(0, 1, "a"),
                    new AddOp<string>(1, 1, "b"),
                });
            ValidateRangesEqual<string>(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 1), new Range(), "a"),
                    new Range2MapEntry(new Range(1, 1), new Range(), "b"),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<string>[] {
                    new AddOp<string>(0, 1, "a"),
                    new AddOp<string>(0, 1, "b"),
                });
            ValidateRangesEqual<string>(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 1), new Range(), "b"),
                    new Range2MapEntry(new Range(1, 1), new Range(), "a"),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<string>[] {
                    new AddOp<string>(0, 2, "a"),
                    new AddOp<string>(2, 3, "b"),
                    new AddOp<string>(5, 4, "c"),
                });
            ValidateRangesEqual<string>(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 2), new Range(), "a"),
                    new Range2MapEntry(new Range(2, 3), new Range(), "b"),
                    new Range2MapEntry(new Range(5, 4), new Range(), "c"),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op<string>[] {
                    new AddOp<string>(0, 2, "a"),
                    new AddOp<string>(0, 3, "b"),
                    new AddOp<string>(0, 4, "c"),
                });
            ValidateRangesEqual<string>(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 4), new Range(), "c"),
                    new Range2MapEntry(new Range(4, 3), new Range(), "b"),
                    new Range2MapEntry(new Range(7, 2), new Range(), "a"),
                });
        }

        private void TestBattery(
            string label,
            MakeTree<string> makeTree,
            Op<string>[] sequence)
        {
            const int Length = 12;
            const string Value = "foo";

            ReferenceRangeMap<string> reference = new ReferenceRangeMap<string>();
            BuildTree(reference, sequence);

            Range2MapEntry[] ranges = ((INonInvasiveRange2MapInspection)reference).GetRanges();

            // test items in collection
            for (int i = 0; i < ranges.Length; i++)
            {
                IRangeMap<string> tree;
                ReferenceRangeMap<string> reference2;
                int p;
                bool f;
                Range2MapEntry r;
                Range2MapEntry endcap = new Range2MapEntry(
                    new Range(ranges[ranges.Length - 1].x.start + ranges[ranges.Length - 1].x.length, 0),
                    new Range(),
                    default(string));

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Count", delegate () { return ranges.Length == unchecked((int)tree.Count); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " LongCount", delegate () { return ranges.Length == unchecked((int)tree.LongCount); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Contains", delegate () { return tree.Contains(ranges[i].x.start); });


                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryInsert(ranges[i].x.start, Length, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryInsert.1", delegate () { return tree.TryInsert(ranges[i].x.start, Length, Value); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryInsert(ranges[i].x.start + ranges[i].x.length, Length, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryInsert.2", delegate () { return tree.TryInsert(ranges[i].x.start + ranges[i].x.length, Length, Value); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryDelete(ranges[i].x.start); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryDelete", delegate () { return tree.TryDelete(ranges[i].x.start); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetLength.1", delegate () { int length; return tree.TryGetLength(ranges[i].x.start, out length); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetLength.2", delegate () { int length; tree.TryGetLength(ranges[i].x.start, out length); return length == ranges[i].x.length; });

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySetLength(ranges[i].x.start, Length); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySetLength", delegate () { return tree.TrySetLength(ranges[i].x.start, Length); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetValue.1", delegate () { string value; return tree.TryGetValue(ranges[i].x.start, out value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetValue.2", delegate () { string value; tree.TryGetValue(ranges[i].x.start, out value); return String.Equals(value, ranges[i].value); });

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySetValue(ranges[i].x.start, Value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySetValue", delegate () { return tree.TrySetValue(ranges[i].x.start, Value); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.1", delegate () { int xLength; string value; return tree.TryGet(ranges[i].x.start, out xLength, out value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.2", delegate () { int xLength; string value; tree.TryGet(ranges[i].x.start, out xLength, out value); return xLength == ranges[i].x.length; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.3", delegate () { int xLength; string value; tree.TryGet(ranges[i].x.start, out xLength, out value); return value == (string)ranges[i].value; });

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, 125, "foo"); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.X.1", delegate () { return tree.TrySet(ranges[i].x.start, 125, "foo"); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, 0, "foo"); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.X.2", delegate () { return tree.TrySet(ranges[i].x.start, 0, "foo"); });
                ValidateRangesEqual(reference2, tree);


                reference2 = reference.Clone();
                reference2.Insert(ranges[i].x.start, Length, Value);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert.1", delegate () { tree.Insert(ranges[i].x.start, Length, Value); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                reference2.Insert(ranges[i].x.start + ranges[i].x.length, Length, Value);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert.2", delegate () { tree.Insert(ranges[i].x.start + ranges[i].x.length, Length, Value); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                reference2.Delete(ranges[i].x.start);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Delete", delegate () { tree.Delete(ranges[i].x.start); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetLength.1", delegate () { int length = tree.GetLength(ranges[i].x.start); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetLength.2", delegate () { int length = tree.GetLength(ranges[i].x.start); return length == ranges[i].x.length; });

                reference2 = reference.Clone();
                reference2.SetLength(ranges[i].x.start, Length);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength", delegate () { tree.SetLength(ranges[i].x.start, Length); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetValue.1", delegate () { string value = tree.GetValue(ranges[i].x.start); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetValue.2", delegate () { string value = tree.GetValue(ranges[i].x.start); return String.Equals(value, ranges[i].value); });

                reference2 = reference.Clone();
                reference2.SetValue(ranges[i].x.start, Value);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetValue", delegate () { tree.SetValue(ranges[i].x.start, Value); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Get.1", delegate () { int xLength; string value; tree.Get(ranges[i].x.start, out xLength, out value); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.2", delegate () { int xLength; string value; tree.Get(ranges[i].x.start, out xLength, out value); return xLength == ranges[i].x.length; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.3", delegate () { int xLength; string value; tree.Get(ranges[i].x.start, out xLength, out value); return value == (string)ranges[i].value; });

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, 125, "foo"); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " TrySet.X.1", delegate () { tree.TrySet(ranges[i].x.start, 125, "foo"); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, 0, "foo"); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Set.X.2", delegate () { tree.Set(ranges[i].x.start, 0, "foo"); });
                ValidateRangesEqual(reference2, tree);


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetExtent", delegate () { return reference.GetExtent() == tree.GetExtent(); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.1", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.2", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start, out nearestStart); return nearestStart == ranges[i].x.start; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.2a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestLessOrEqual(ranges[i].x.start, out nearestStart, out xLength, out value); return ff & (nearestStart == ranges[i].x.start) && (xLength == ranges[i].x.length) && (value == (string)ranges[i].value); });

                f = i > 0;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestLessOrEqual.3", f, delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start - 1, out nearestStart); });
                //
                p = f ? ranges[i].x.start - ranges[i - 1].x.length : 0;
                r = f ? ranges[i - 1] : new Range2MapEntry();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.4", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start - 1, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.4a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestLessOrEqual(ranges[i].x.start - 1, out nearestStart, out xLength, out value); return (ff == f) & (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.5", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start + 1, out nearestStart); });
                //
                f = (i + 1 < ranges.Length) && (ranges[i].x.length == 1);
                r = f ? ranges[i + 1] : ranges[i];
                p = f ? ranges[i + 1].x.start : ranges[i].x.start;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.6", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start + 1, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.6a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestLessOrEqual(ranges[i].x.start + 1, out nearestStart, out xLength, out value); return ff && (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.7", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start + ranges[i].x.length + 1, out nearestStart); });
                //
                if (i + 1 < ranges.Length)
                {
                    if ((i + 2 < ranges.Length) && (ranges[i + 1].x.length == 1))
                    {
                        p = ranges[i + 2].x.start;
                        r = ranges[i + 2];
                    }
                    else
                    {
                        p = ranges[i + 1].x.start;
                        r = ranges[i + 1];
                    }
                }
                else
                {
                    p = ranges[i].x.start;
                    r = ranges[i];
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.8", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start + ranges[i].x.length + 1, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.8a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestLessOrEqual(ranges[i].x.start + ranges[i].x.length + 1, out nearestStart, out xLength, out value); return ff && (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });


                f = i > 0;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestLess.1", f, delegate () { int nearestStart; return tree.NearestLess(ranges[i].x.start, out nearestStart); });
                //
                p = f ? ranges[i - 1].x.start : 0;
                r = f ? ranges[i - 1] : new Range2MapEntry();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.2", delegate () { int nearestStart; tree.NearestLess(ranges[i].x.start, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.2a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestLess(ranges[i].x.start, out nearestStart, out xLength, out value); return (ff == f) && (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.3", delegate () { int nearestStart; return tree.NearestLess(ranges[i].x.start + 1, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.4", delegate () { int nearestStart; tree.NearestLess(ranges[i].x.start + 1, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.4a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestLess(ranges[i].x.start + 1, out nearestStart, out xLength, out value); return ff && (nearestStart == ranges[i].x.start) && (xLength == r.x.length) && (value == (string)r.value); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.5", delegate () { int nearestStart; return tree.NearestLess(ranges[i].x.start + ranges[i].x.length, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.6", delegate () { int nearestStart; tree.NearestLess(ranges[i].x.start + ranges[i].x.length, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.6a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestLess(ranges[i].x.start + ranges[i].x.length, out nearestStart, out xLength, out value); return ff & (nearestStart == ranges[i].x.start) && (xLength == r.x.length) && (value == (string)r.value); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.1", delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].x.start, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.2", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].x.start, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.2a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestGreaterOrEqual(ranges[i].x.start, out nearestStart, out xLength, out value); return ff && (nearestStart == ranges[i].x.start) && (xLength == r.x.length) && (value == (string)r.value); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.3", delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].x.start - 1, out nearestStart); });
                //
                f = (i == 0) || (ranges[i - 1].x.length != 1);
                p = f ? ranges[i].x.start : ranges[i - 1].x.start;
                r = f ? ranges[i] : ranges[i - 1];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.4", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].x.start - 1, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.4a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestGreaterOrEqual(ranges[i].x.start - 1, out nearestStart, out xLength, out value); return ff && (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });

                f = i + 1 < ranges.Length;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestGreaterOrEqual.5", f, delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].x.start + 1, out nearestStart); });
                //
                p = f ? ranges[i + 1].x.start : (ranges[i].x.start + ranges[i].x.length);
                r = f ? ranges[i + 1] : endcap;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.6", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].x.start + 1, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.6a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestGreaterOrEqual(ranges[i].x.start + 1, out nearestStart, out xLength, out value); return (ff == f) && (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });


                f = i + 1 < ranges.Length;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestGreater.1", f, delegate () { int nearestStart; return tree.NearestGreater(ranges[i].x.start, out nearestStart); });
                //
                p = f ? ranges[i + 1].x.start : (ranges[i].x.start + ranges[i].x.length);
                r = f ? ranges[i + 1] : endcap;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.2", delegate () { int nearestStart; tree.NearestGreater(ranges[i].x.start, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.2a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestGreater(ranges[i].x.start, out nearestStart, out xLength, out value); return (ff == f) && (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });

                if ((i + 1 < ranges.Length) && (ranges[i].x.length == 1))
                {
                    p = ranges[i + 1].x.start + ranges[i + 1].x.length;
                }
                else
                {
                    p = ranges[i].x.start + ranges[i].x.length;
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                f = p < ranges[ranges.Length - 1].x.start + ranges[ranges.Length - 1].x.length;
                TestBool(label + " NearestGreater.3", f, delegate () { int nearestStart; return tree.NearestGreater(ranges[i].x.start + 1, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.4", delegate () { int nearestStart; tree.NearestGreater(ranges[i].x.start + 1, out nearestStart); return nearestStart == p; });
                r = ranges[i].x.length == 1 ? (f ? ranges[i + 2] : endcap) : (f ? ranges[i + 1] : endcap);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.4a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestGreater(ranges[i].x.start + 1, out nearestStart, out xLength, out value); return (ff == f) && (nearestStart == p) && (xLength == r.x.length) && (value == (string)r.value); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.5", delegate () { int nearestStart; return tree.NearestGreater(ranges[i].x.start - 1, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.6", delegate () { int nearestStart; tree.NearestGreater(ranges[i].x.start - 1, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.6a", delegate () { int nearestStart, xLength; string value; bool ff = tree.NearestGreater(ranges[i].x.start - 1, out nearestStart, out xLength, out value); return ff && (nearestStart == ranges[i].x.start) && (xLength == r.x.length) && (value == (string)r.value); });

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Insert(ranges[i].x.start, Int32.MaxValue - reference2.GetExtent() - 1, null); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.1a", delegate () { tree.Insert(ranges[i].x.start, Int32.MaxValue - tree.GetExtent() - 1, null); });
                TestTrue(label + " Insert-overflow.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.1c", delegate () { return tree.GetExtent() == reference2.GetExtent(); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Insert(ranges[i].x.start, Int32.MaxValue - reference2.GetExtent() - 0, null); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.2a", delegate () { tree.Insert(ranges[i].x.start, Int32.MaxValue - tree.GetExtent() - 0, null); });
                TestTrue(label + " Insert-overflow.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.2c", delegate () { return tree.GetExtent() == reference2.GetExtent(); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Insert-overflow.3a", typeof(OverflowException), delegate () { tree.Insert(ranges[i].x.start, Int32.MaxValue - tree.GetExtent() + 1, null); });
                TestTrue(label + " Insert-overflow.3b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.3c", delegate () { return tree.GetExtent() == reference2.GetExtent(); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetLength(ranges[i].x.start, Int32.MaxValue - (reference2.GetExtent() - ranges[i].x.length) - 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength-overflow.1a", delegate () { tree.SetLength(ranges[i].x.start, Int32.MaxValue - (tree.GetExtent() - ranges[i].x.length) - 1); });
                TestTrue(label + " SetLength-overflow.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.1c", delegate () { return tree.GetExtent() == reference2.GetExtent(); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetLength(ranges[i].x.start, Int32.MaxValue - (reference2.GetExtent() - ranges[i].x.length) - 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength-overflow.2a", delegate () { tree.SetLength(ranges[i].x.start, Int32.MaxValue - (tree.GetExtent() - ranges[i].x.length) - 0); });
                TestTrue(label + " SetLength-overflow.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.2c", delegate () { return tree.GetExtent() == reference2.GetExtent(); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " SetLength-overflow.2a", typeof(OverflowException), delegate () { tree.SetLength(ranges[i].x.start, Int32.MaxValue - (tree.GetExtent() - ranges[i].x.length) + 1); });
                TestTrue(label + " SetLength-overflow.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.2c", delegate () { return tree.GetExtent() == reference2.GetExtent(); });
                ValidateRangesEqual(reference2, tree);
            }

            // test items not in collection
            for (int i = -1; i < ranges.Length; i++)
            {
                IRangeMap<string> tree;

                int lx = i >= 0 ? ranges[i].x.start : Int32.MinValue / 2;
                int rx = ranges.Length != 0 ? (i + 1 < ranges.Length ? ranges[i + 1].x.start : Int32.MaxValue / 2) : 0;
                int px = (lx + rx) / 2;


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " Contains", delegate () { return tree.Contains(px); });
                }

                if ((px != lx) && (px != rx) && (px >= 0))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryInsert", delegate () { return tree.TryInsert(px, Length, Value); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryDelete", delegate () { return tree.TryDelete(px); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryGetLength.1", delegate () { int length; return tree.TryGetLength(px, out length); });
                    TestTrue(label + " TryGetLength.2", delegate () { int length; tree.TryGetLength(px, out length); return length == default(int); });
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TrySetLength", delegate () { return tree.TrySetLength(px, Length); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryGetValue.1", delegate () { string value; return tree.TryGetValue(px, out value); });
                    TestTrue(label + " TryGetValue.2", delegate () { string value; tree.TryGetValue(px, out value); return value == null; });
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TrySetValue", delegate () { return tree.TrySetValue(px, Value); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryGet.1", delegate () { int xLength; string value; return tree.TryGet(px, out xLength, out value); });
                    TestTrue(label + " TryGet.2", delegate () { int xLength; string value; tree.TryGet(px, out xLength, out value); return xLength == 0; });
                    TestTrue(label + " TryGet.3", delegate () { int xLength; string value; tree.TryGet(px, out xLength, out value); return value == null; });
                }


                if ((px != lx) && (px != rx) && (px >= 0))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Insert", typeof(ArgumentException), delegate () { tree.Insert(px, Length, Value); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Delete", typeof(ArgumentException), delegate () { tree.Delete(px); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " GetLength", typeof(ArgumentException), delegate () { int length = tree.GetLength(px); });
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " SetLength", typeof(ArgumentException), delegate () { tree.SetLength(px, Length); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " GetValue", typeof(ArgumentException), delegate () { string value = tree.GetValue(px); });
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " SetValue", typeof(ArgumentException), delegate () { tree.SetValue(px, Value); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Get.1", typeof(ArgumentException), delegate () { int xLength; string value; tree.Get(px, out xLength, out value); });
                }
            }
        }

        public override bool Do()
        {
            try
            {
                this.RangeMapBasicCoverage();
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
