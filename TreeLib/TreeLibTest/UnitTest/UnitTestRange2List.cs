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
    public class UnitTestRange2List : TestBase
    {
        public UnitTestRange2List(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private abstract class Op
        {
            public readonly int start;
            public readonly Side side;

            protected Op(int start, Side side)
            {
                this.start = start;
                this.side = side;
            }

            public abstract void Do(IRange2List tree);
            public abstract void Do(IRange2List tree, IRange2List treeAnalog);
        }

        private class AddOp : Op
        {
            public readonly int length;
            public readonly int otherLength;

            public AddOp(int start, Side side, int length, int otherLength)
                : base(start, side)
            {
                this.length = length;
                this.otherLength = otherLength;
            }

            public override void Do(IRange2List tree)
            {
                tree.Insert(start, side, side == Side.X ? length : otherLength, side == Side.Y ? length : otherLength);
            }

            public override void Do(IRange2List tree, IRange2List treeAnalog)
            {
                tree.Insert(start, side, side == Side.X ? length : otherLength, side == Side.Y ? length : otherLength);
                treeAnalog.Insert(start, side, side == Side.X ? length : otherLength, side == Side.Y ? length : otherLength);
            }
        }

        private class RemoveOp : Op
        {
            public RemoveOp(int start, Side side)
                : base(start, side)
            {
            }

            public override void Do(IRange2List tree)
            {
                tree.Delete(start, side);
            }

            public override void Do(IRange2List tree, IRange2List treeAnalog)
            {
                tree.Delete(start, side);
                treeAnalog.Delete(start, side);
            }
        }


        protected void ValidateRanges(Range2MapEntry[] ranges)
        {
            IncrementIteration();
            int offsetX = 0, offsetY = 0;
            for (int i = 0; i < ranges.Length; i++)
            {
                TestTrue("xStart", delegate () { return offsetX == ranges[i].x.start; });
                TestTrue("yStart", delegate () { return offsetY == ranges[i].y.start; });
                TestTrue("xLength > 0", delegate () { return ranges[i].x.length > 0; });
                TestTrue("yLength > 0", delegate () { return ranges[i].y.length > 0; });
                offsetX += ranges[i].x.length;
                offsetY += ranges[i].y.length;
            }
        }

        protected void ValidateRangesEqual(Range2MapEntry[] ranges1, Range2MapEntry[] ranges2)
        {
            IncrementIteration();
            TestTrue("equal count", delegate () { return ranges1.Length == ranges2.Length; });
            for (int i = 0; i < ranges1.Length; i++)
            {
                TestTrue("xStart", delegate () { return ranges1[i].x.start == ranges2[i].x.start; });
                TestTrue("yStart", delegate () { return ranges1[i].y.start == ranges2[i].y.start; });
                TestTrue("xLength", delegate () { return ranges1[i].x.length == ranges2[i].x.length; });
                TestTrue("yLength", delegate () { return ranges1[i].y.length == ranges2[i].y.length; });
                TestTrue("value", delegate () { return (ranges1[i].value == null) && (ranges2[i].value == null); });
            }
        }

        protected void ValidateRangesEqual(IRange2List tree1, IRange2List tree2)
        {
            IncrementIteration();
            Range2MapEntry[] ranges1 = ((INonInvasiveRange2MapInspection)tree1).GetRanges();
            ValidateRanges(ranges1);
            Range2MapEntry[] ranges2 = ((INonInvasiveRange2MapInspection)tree2).GetRanges();
            ValidateRanges(ranges2);
            ValidateRangesEqual(ranges1, ranges2);
            TestTrue("GetExtent.X", delegate () { return tree1.GetExtent(Side.X) == tree2.GetExtent(Side.X); });
            TestTrue("GetExtent.Y", delegate () { return tree1.GetExtent(Side.Y) == tree2.GetExtent(Side.Y); });
        }


        private void BuildTree(
            IRange2List tree,
            IEnumerable<Op> sequence)
        {
            ValidateTree(tree);
            foreach (Op op in sequence)
            {
                IncrementIteration();
                op.Do(tree);
                ValidateTree(tree);
                ValidateRanges(((INonInvasiveRange2MapInspection)tree).GetRanges());
            }
        }

        private void BuildTree(
            IRange2List tree,
            IRange2List treeAnalog,
            IEnumerable<Op> sequence)
        {
            ValidateTree(tree);
            foreach (Op op in sequence)
            {
                IncrementIteration();
                op.Do(tree, treeAnalog);
                ValidateTree(tree);
            }
            ValidateRangesEqual(tree, treeAnalog);
        }

        private void TestTree(
            string label,
            IRange2List tree,
            IEnumerable<Op> sequence,
            VoidAction action)
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

        private delegate IRange2List MakeTree();


        //

        public void Range2ListBasicCoverage()
        {
            RangeTreeBasicCoverageSpecific(
                "ReferenceRangeList",
                delegate () { return new ReferenceRange2List(); });



            RangeTreeBasicCoverageSpecific(
                "SplayTreeRange2List",
                delegate () { return new SplayTreeRange2List(); });

            RangeTreeBasicCoverageSpecific(
                "SplayTreeArrayRange2List",
                delegate () { return new SplayTreeArrayRange2List(); });

            RangeTreeBasicCoverageSpecific(
                "SplayTreeRange2ListLong",
                delegate () { return new AdaptRange2ListToRange2ListLong(new SplayTreeRange2ListLong()); });



            RangeTreeBasicCoverageSpecific(
                "RedBlackTreeRange2List",
                delegate () { return new RedBlackTreeRange2List(); });

            RangeTreeBasicCoverageSpecific(
                "RedBlackTreeArrayRange2List",
                delegate () { return new RedBlackTreeArrayRange2List(); });

            RangeTreeBasicCoverageSpecific(
                "RedBlackTreeRange2ListLong",
                delegate () { return new AdaptRange2ListToRange2ListLong(new RedBlackTreeRange2ListLong()); });



            RangeTreeBasicCoverageSpecific(
                "AVLTreeRange2List",
                delegate () { return new AVLTreeRange2List(); });

            RangeTreeBasicCoverageSpecific(
                "AVLTreeArrayRange2List",
                delegate () { return new AVLTreeArrayRange2List(); });

            RangeTreeBasicCoverageSpecific(
                "AVLTreeRange2ListLong",
                delegate () { return new AdaptRange2ListToRange2ListLong(new AVLTreeRange2ListLong()); });
        }

        private void RangeTreeBasicCoverageSpecific(
            string label,
            MakeTree makeTree)
        {
            TestBasic(
                "Basic",
                makeTree);


            TestBattery(
                "[]",
                makeTree,
                new Op[] { });


            TestBattery(
                "[0(+1):0(+1)]",
                makeTree,
                new Op[] { new AddOp(0, Side.X, 1, 1) });

            TestBattery(
                "[0(+2):0(+1)]",
                makeTree,
                new Op[] { new AddOp(0, Side.X, 2, 1) });

            TestBattery(
                "[0(+1):0(+2)]",
                makeTree,
                new Op[] { new AddOp(0, Side.X, 1, 2) });

            TestBattery(
                "[0(+2):0(+2)]",
                makeTree,
                new Op[] { new AddOp(0, Side.X, 2, 2) });


            TestBattery(
                "[0(+1):0(+1), 1(+1):1(+1)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 1, 1),
                    new AddOp(1, Side.X, 1, 1)
                });

            TestBattery(
                "[0(+2):0(+1), 2(+2):1(+1)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 2, 1),
                    new AddOp(2, Side.X, 2, 1)
                });

            TestBattery(
                "[0(+1):0(+2), 1(+1):2(+2)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 1, 2),
                    new AddOp(1, Side.X, 1, 2)
                });

            TestBattery(
                "[0(+2):0(+3), 2(+2):3(+3)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 2, 3),
                    new AddOp(2, Side.X, 2, 3)
                });


            TestBattery(
                "[0(+1):0(+1), 1(+1):1(+1), 2(+1):2(+1)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 1, 1),
                    new AddOp(1, Side.X, 1, 1),
                    new AddOp(2, Side.X, 1, 1)
                });

            TestBattery(
                "[0(+2):0(+1), 2(+2):1(+1), 4(+2):2(+1)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 2, 1),
                    new AddOp(2, Side.X, 2, 1),
                    new AddOp(4, Side.X, 2, 1)
                });

            TestBattery(
                "[0(+1):0(+2), 1(+1):2(+2), 2(+1):4(+2)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 1, 2),
                    new AddOp(1, Side.X, 1, 2),
                    new AddOp(2, Side.X, 1, 2)
                });

            TestBattery(
                "[0(+2):0(+3), 2(+2):3(+3), 4(+2):6(+3)]",
                makeTree,
                new Op[] {
                    new AddOp(0, Side.X, 2, 3),
                    new AddOp(2, Side.X, 2, 3),
                    new AddOp(4, Side.X, 2, 3)
                });
        }

        private void TestBasic(
            string label,
            MakeTree makeTree)
        {
            label = label + " basic";

            IRange2List tree;

            // argument checking

            foreach (Side side in new Side[] { Side.X, Side.Y })
            {
                tree = makeTree();
                TestTrue(label + " TryInsert.1", delegate () { return tree.TryInsert(0, side, 1, 1); });
                tree = makeTree();
                TestThrow(label + " TryInsert.2", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(0, side, 0, 1); });
                tree = makeTree();
                TestThrow(label + " TryInsert.3", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(0, side, 1, 0); });
                tree = makeTree();
                TestThrow(label + " TryInsert.4", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(0, side, -1, 1); });
                tree = makeTree();
                TestThrow(label + " TryInsert.5", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(0, side, 1, -1); });
                tree = makeTree();
                TestThrow(label + " TryInsert.6", typeof(ArgumentOutOfRangeException), delegate () { tree.TryInsert(-1, side, 1, 1); });

                tree = makeTree();
                TestTrue(label + " TrySetLength.1a", delegate () { return tree.TryInsert(0, side, 1, 1); });
                TestNoThrow(label + " TrySetLength.1b", delegate () { tree.TrySetLength(0, side, 1); });
                TestThrow(label + " TrySetLength.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.TrySetLength(0, side, 0); });
                tree = makeTree();
                TestTrue(label + " TrySetLength.2a", delegate () { return tree.TryInsert(0, side, 1, 1); });
                TestNoThrow(label + " TrySetLength.2b", delegate () { tree.TrySetLength(0, side, 1); });
                TestThrow(label + " TrySetLength.2c", typeof(ArgumentOutOfRangeException), delegate () { tree.TrySetLength(0, side, -1); });

                tree = makeTree();
                TestTrue(label + " TrySet.1a", delegate () { return tree.TryInsert(0, side, 1, 1); });
                TestNoThrow(label + " TrySet.1b", delegate () { tree.TrySet(0, side, 2, 3); });
                TestThrow(label + " TrySet.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.TrySet(0, side, -1, 1); });
                TestThrow(label + " TrySet.1d", typeof(ArgumentOutOfRangeException), delegate () { tree.TrySet(0, side, 1, -1); });
                TestNoThrow(label + " TrySet.1e", delegate () { tree.TrySet(0, side, 0, 1); });
                TestNoThrow(label + " TrySet.1f", delegate () { tree.TrySet(0, side, 1, 0); });

                tree = makeTree();
                TestNoThrow(label + " Insert.1", delegate () { tree.Insert(0, side, 1, 1); });
                tree = makeTree();
                TestThrow(label + " Insert.2", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(0, side, 0, 1); });
                tree = makeTree();
                TestThrow(label + " Insert.3", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(0, side, 1, 0); });
                tree = makeTree();
                TestThrow(label + " Insert.4", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(0, side, -1, 1); });
                tree = makeTree();
                TestThrow(label + " Insert.5", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(0, side, 1, -1); });
                tree = makeTree();
                TestThrow(label + " Insert.6", typeof(ArgumentOutOfRangeException), delegate () { tree.Insert(-1, side, 1, 1); });

                tree = makeTree();
                TestTrue(label + " SetLength.1a", delegate () { return tree.TryInsert(0, side, 1, 1); });
                TestNoThrow(label + " SetLength.1b", delegate () { tree.SetLength(0, side, 1); });
                TestThrow(label + " SetLength.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.SetLength(0, side, 0); });
                tree = makeTree();
                TestTrue(label + " SetLength.2a", delegate () { return tree.TryInsert(0, side, 1, 1); });
                TestNoThrow(label + " SetLength.2b", delegate () { tree.SetLength(0, side, 1); });
                TestThrow(label + " SetLength.2c", typeof(ArgumentOutOfRangeException), delegate () { tree.SetLength(0, side, -1); });

                tree = makeTree();
                TestTrue(label + " Set.1a", delegate () { return tree.TryInsert(0, side, 1, 1); });
                TestNoThrow(label + " Set.1b", delegate () { tree.Set(0, side, 2, 3); });
                TestThrow(label + " Set.1c", typeof(ArgumentOutOfRangeException), delegate () { tree.Set(0, side, -1, 1); });
                TestThrow(label + " Set.1d", typeof(ArgumentOutOfRangeException), delegate () { tree.Set(0, side, 1, -1); });
                TestNoThrow(label + " Set.1e", delegate () { tree.Set(0, side, 0, 1); });
                TestNoThrow(label + " Set.1f", delegate () { tree.Set(0, side, 1, 0); });

                tree = makeTree();
                TestTrue(label + " GetExtent degenerate", delegate () { return tree.GetExtent(side) == 0; });
                TestFalse(label + " NearestLessOrEqual degenerate 1", delegate () { int nearest; return tree.NearestLessOrEqual(1, side, out nearest); });
                TestTrue(label + " NearestLessOrEqual degenerate 2", delegate () { int nearest; tree.NearestLessOrEqual(1, side, out nearest); return nearest == 0; });
                TestFalse(label + " NearestLess degenerate 1", delegate () { int nearest; return tree.NearestLess(1, side, out nearest); });
                TestTrue(label + " NearestLess degenerate 2", delegate () { int nearest; tree.NearestLess(1, side, out nearest); return nearest == 0; });
                TestFalse(label + " NearestGreaterOrEqual degenerate 1", delegate () { int nearest; return tree.NearestGreaterOrEqual(1, side, out nearest); });
                TestTrue(label + " NearestGreaterOrEqual degenerate 2", delegate () { int nearest; tree.NearestGreaterOrEqual(1, side, out nearest); return nearest == 0; });
                TestFalse(label + " NearestGreater degenerate 1", delegate () { int nearest; return tree.NearestGreater(1, side, out nearest); });
                TestTrue(label + " NearestGreater degenerate 2", delegate () { int nearest; tree.NearestGreater(1, side, out nearest); return nearest == 0; });
            }

            // basic functionality

            tree = makeTree();
            BuildTree(
                tree,
                new Op[] {
                    new AddOp(0, Side.X, 1, 1),
                });
            ValidateRangesEqual(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 1), new Range(0, 1)),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op[] {
                    new AddOp(0, Side.X, 1, 1),
                    new AddOp(1, Side.X, 1, 1),
                });
            ValidateRangesEqual(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 1), new Range(0, 1)),
                    new Range2MapEntry(new Range(1, 1), new Range(1, 1)),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op[] {
                    new AddOp(0, Side.X, 1, 1),
                    new AddOp(0, Side.X, 1, 1),
                });
            ValidateRangesEqual(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 1), new Range(0, 1)),
                    new Range2MapEntry(new Range(1, 1), new Range(1, 1)),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op[] {
                    new AddOp(0, Side.X, 2, 3),
                    new AddOp(2, Side.X, 3, 4),
                    new AddOp(5, Side.X, 4, 6),
                });
            ValidateRangesEqual(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 2), new Range(0, 3)),
                    new Range2MapEntry(new Range(2, 3), new Range(3, 4)),
                    new Range2MapEntry(new Range(5, 4), new Range(7, 6)),
                });

            tree = makeTree();
            BuildTree(
                tree,
                new Op[] {
                    new AddOp(0, Side.X, 2, 3),
                    new AddOp(0, Side.X, 3, 4),
                    new AddOp(0, Side.X, 4, 6),
                });
            ValidateRangesEqual(
                ((INonInvasiveRange2MapInspection)tree).GetRanges(),
                new Range2MapEntry[] {
                    new Range2MapEntry(new Range(0, 4), new Range(0, 6)),
                    new Range2MapEntry(new Range(4, 3), new Range(6, 4)),
                    new Range2MapEntry(new Range(7, 2), new Range(10, 3)),
                });
        }

        private void TestBattery(
            string label,
            MakeTree makeTree,
            Op[] sequence)
        {
            const int Length = 12;
            const int Length2 = 15;

            ReferenceRange2List reference = new ReferenceRange2List();
            BuildTree(reference, sequence);

            Range2MapEntry[] ranges = ((INonInvasiveRange2MapInspection)reference).GetRanges();

            // test items in collection
            for (int i = 0; i < ranges.Length; i++)
            {
                IRange2List tree;
                ReferenceRange2List reference2;
                int p;
                bool f;
                Range2MapEntry r;
                Range2MapEntry endcap = new Range2MapEntry(
                    new Range(ranges[ranges.Length - 1].x.start + ranges[ranges.Length - 1].x.length, 0),
                    new Range(ranges[ranges.Length - 1].y.start + ranges[ranges.Length - 1].y.length, 0),
                    null);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Count", delegate () { return ranges.Length == unchecked((int)tree.Count); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " LongCount", delegate () { return ranges.Length == unchecked((int)tree.LongCount); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Contains.X", delegate () { return tree.Contains(ranges[i].x.start, Side.X); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Contains.Y", delegate () { return tree.Contains(ranges[i].y.start, Side.Y); });


                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryInsert(ranges[i].x.start, Side.X, Length, Length2); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryInsert.X.1", delegate () { return tree.TryInsert(ranges[i].x.start, Side.X, Length, Length2); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryInsert(ranges[i].x.start + ranges[i].x.length, Side.X, Length, Length2); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryInsert.X.2", delegate () { return tree.TryInsert(ranges[i].x.start + ranges[i].x.length, Side.X, Length, Length2); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryInsert(ranges[i].y.start, Side.Y, Length, Length2); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryInsert.Y.1", delegate () { return tree.TryInsert(ranges[i].y.start, Side.Y, Length, Length2); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryInsert(ranges[i].y.start + ranges[i].y.length, Side.Y, Length, Length2); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryInsert.Y.2", delegate () { return tree.TryInsert(ranges[i].y.start + ranges[i].y.length, Side.Y, Length, Length2); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryDelete(ranges[i].x.start, Side.X); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryDelete.X", delegate () { return tree.TryDelete(ranges[i].x.start, Side.X); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TryDelete(ranges[i].y.start, Side.Y); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryDelete.Y", delegate () { return tree.TryDelete(ranges[i].y.start, Side.Y); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetLength.X.1", delegate () { int length; return tree.TryGetLength(ranges[i].x.start, Side.X, out length); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetLength.X.2", delegate () { int length; tree.TryGetLength(ranges[i].x.start, Side.X, out length); return length == ranges[i].x.length; });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetLength.Y.1", delegate () { int length; return tree.TryGetLength(ranges[i].y.start, Side.Y, out length); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGetLength.Y.2", delegate () { int length; tree.TryGetLength(ranges[i].y.start, Side.Y, out length); return length == ranges[i].y.length; });

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySetLength(ranges[i].x.start, Side.X, Length); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySetLength.X", delegate () { return tree.TrySetLength(ranges[i].x.start, Side.X, Length); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySetLength(ranges[i].y.start, Side.Y, Length); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySetLength.Y", delegate () { return tree.TrySetLength(ranges[i].y.start, Side.Y, Length); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.X.1", delegate () { int otherStart, xLength, yLength; return tree.TryGet(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); });
                p = ranges[i].y.start; // invert x/y!
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.X.2", delegate () { int otherStart, xLength, yLength; tree.TryGet(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); return p == otherStart; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.X.3", delegate () { int otherStart, xLength, yLength; tree.TryGet(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); return xLength == ranges[i].x.length; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.X.4", delegate () { int otherStart, xLength, yLength; tree.TryGet(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); return yLength == ranges[i].y.length; });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.Y.1", delegate () { int otherStart, xLength, yLength; return tree.TryGet(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); });
                p = ranges[i].x.start; // invert x/y!
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.Y.2", delegate () { int otherStart, xLength, yLength; tree.TryGet(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); return p == otherStart; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.Y.3", delegate () { int otherStart, xLength, yLength; tree.TryGet(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); return xLength == ranges[i].x.length; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TryGet.Y.4", delegate () { int otherStart, xLength, yLength; tree.TryGet(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); return yLength == ranges[i].y.length; });

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, Side.X, 125, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.X.1", delegate () { return tree.TrySet(ranges[i].x.start, Side.X, 125, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, Side.X, 0, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.X.2", delegate () { return tree.TrySet(ranges[i].x.start, Side.X, 0, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, Side.X, 125, 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.X.3", delegate () { return tree.TrySet(ranges[i].x.start, Side.X, 125, 0); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].y.start, Side.Y, 125, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.Y.1", delegate () { return tree.TrySet(ranges[i].y.start, Side.Y, 125, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].y.start, Side.Y, 0, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.Y.2", delegate () { return tree.TrySet(ranges[i].y.start, Side.Y, 0, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].y.start, Side.Y, 125, 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " TrySet.Y.3", delegate () { return tree.TrySet(ranges[i].y.start, Side.Y, 125, 0); });
                ValidateRangesEqual(reference2, tree);


                reference2 = reference.Clone();
                reference2.Insert(ranges[i].x.start, Side.X, Length, Length2);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert.X.1", delegate () { tree.Insert(ranges[i].x.start, Side.X, Length, Length2); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                reference2.Insert(ranges[i].x.start + ranges[i].x.length, Side.X, Length, Length2);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert.X.2", delegate () { tree.Insert(ranges[i].x.start + ranges[i].x.length, Side.X, Length, Length2); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                reference2.Insert(ranges[i].y.start, Side.Y, Length, Length2);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert.Y.1", delegate () { tree.Insert(ranges[i].y.start, Side.Y, Length, Length2); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                reference2.Insert(ranges[i].y.start + ranges[i].y.length, Side.Y, Length, Length2);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert.Y.2", delegate () { tree.Insert(ranges[i].y.start + ranges[i].y.length, Side.Y, Length, Length2); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                reference2.Delete(ranges[i].x.start, Side.X);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Delete.X", delegate () { tree.Delete(ranges[i].x.start, Side.X); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                reference2.Delete(ranges[i].y.start, Side.Y);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Delete.Y", delegate () { tree.Delete(ranges[i].y.start, Side.Y); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetLength.X.1", delegate () { int length = tree.GetLength(ranges[i].x.start, Side.X); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetLength.X.2", delegate () { int length = tree.GetLength(ranges[i].x.start, Side.X); return length == ranges[i].x.length; });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " GetLength.Y.1", delegate () { int length = tree.GetLength(ranges[i].y.start, Side.Y); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetLength.Y.2", delegate () { int length = tree.GetLength(ranges[i].y.start, Side.Y); return length == ranges[i].y.length; });

                reference2 = reference.Clone();
                reference2.SetLength(ranges[i].x.start, Side.X, Length);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength.X", delegate () { tree.SetLength(ranges[i].x.start, Side.X, Length); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                reference2.SetLength(ranges[i].y.start, Side.Y, Length);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength.Y", delegate () { tree.SetLength(ranges[i].y.start, Side.Y, Length); });
                ValidateRangesEqual(reference2, tree);

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Get.X.1", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); });
                p = ranges[i].y.start; // invert x/y!
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.X.2", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); return p == otherStart; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.X.3", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); return xLength == ranges[i].x.length; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.X.4", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].x.start, Side.X, out otherStart, out xLength, out yLength); return yLength == ranges[i].y.length; });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Get.Y.1", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); });
                p = ranges[i].x.start; // invert x/y!
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.Y.2", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); return p == otherStart; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.Y.3", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); return xLength == ranges[i].x.length; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " Get.Y.4", delegate () { int otherStart, xLength, yLength; tree.Get(ranges[i].y.start, Side.Y, out otherStart, out xLength, out yLength); return yLength == ranges[i].y.length; });

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, Side.X, 125, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " TrySet.X.1", delegate () { tree.TrySet(ranges[i].x.start, Side.X, 125, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, Side.X, 0, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Set.X.2", delegate () { tree.Set(ranges[i].x.start, Side.X, 0, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].x.start, Side.X, 125, 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Set.X.3", delegate () { tree.Set(ranges[i].x.start, Side.X, 125, 0); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].y.start, Side.Y, 125, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Set.Y.1", delegate () { tree.Set(ranges[i].y.start, Side.Y, 125, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].y.start, Side.Y, 0, 223); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Set.Y.2", delegate () { tree.Set(ranges[i].y.start, Side.Y, 0, 223); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestTrue("prereq", delegate () { return reference2.TrySet(ranges[i].y.start, Side.Y, 125, 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Set.Y.3", delegate () { tree.Set(ranges[i].y.start, Side.Y, 125, 0); });
                ValidateRangesEqual(reference2, tree);


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetExtent.X", delegate () { return reference.GetExtent(Side.X) == tree.GetExtent(Side.X); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " GetExtent.Y", delegate () { return reference.GetExtent(Side.Y) == tree.GetExtent(Side.Y); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.1", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start, Side.X, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.2", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start, Side.X, out nearestStart); return nearestStart == ranges[i].x.start; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].x.start, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff & (nearestStart == ranges[i].x.start) && (otherStart == ranges[i].y.start) && (xLength == ranges[i].x.length) && (yLength == ranges[i].y.length); });

                f = i > 0;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestLessOrEqual.X.3", f, delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start - 1, Side.X, out nearestStart); });
                //
                p = f ? ranges[i].x.start - ranges[i - 1].x.length : 0;
                r = f ? ranges[i - 1] : new Range2MapEntry();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.4", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start - 1, Side.X, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].x.start - 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) & (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.5", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start + 1, Side.X, out nearestStart); });
                //
                f = (i + 1 < ranges.Length) && (ranges[i].x.length == 1);
                p = f ? ranges[i + 1].x.start : ranges[i].x.start;
                r = f ? ranges[i + 1] : ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.6", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start + 1, Side.X, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].x.start + 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.7", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].x.start + ranges[i].x.length + 1, Side.X, out nearestStart); });
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
                TestTrue(label + " NearestLessOrEqual.X.8", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].x.start + ranges[i].x.length + 1, Side.X, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.X.8a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].x.start + ranges[i].x.length + 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.1", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].y.start, Side.Y, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.2", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].y.start, Side.Y, out nearestStart); return nearestStart == ranges[i].y.start; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].y.start, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff & (nearestStart == ranges[i].y.start) && (otherStart == ranges[i].x.start) && (xLength == ranges[i].x.length) && (yLength == ranges[i].y.length); });

                f = i > 0;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestLessOrEqual.Y.3", f, delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].y.start - 1, Side.Y, out nearestStart); });
                //
                p = f ? ranges[i].y.start - ranges[i - 1].y.length : 0;
                r = f ? ranges[i - 1] : new Range2MapEntry();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.4", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].y.start - 1, Side.Y, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].y.start - 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) & (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.5", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].y.start + 1, Side.Y, out nearestStart); });
                //
                f = (i + 1 < ranges.Length) && (ranges[i].y.length == 1);
                p = f ? ranges[i + 1].y.start : ranges[i].y.start;
                r = f ? ranges[i + 1] : ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.6", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].y.start + 1, Side.Y, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].y.start + 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.7", delegate () { int nearestStart; return tree.NearestLessOrEqual(ranges[i].y.start + ranges[i].y.length + 1, Side.Y, out nearestStart); });
                //
                if (i + 1 < ranges.Length)
                {
                    if ((i + 2 < ranges.Length) && (ranges[i + 1].y.length == 1))
                    {
                        p = ranges[i + 2].y.start;
                        r = ranges[i + 2];
                    }
                    else
                    {
                        p = ranges[i + 1].y.start;
                        r = ranges[i + 1];
                    }
                }
                else
                {
                    p = ranges[i].y.start;
                    r = ranges[i];
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.8", delegate () { int nearestStart; tree.NearestLessOrEqual(ranges[i].y.start + ranges[i].y.length + 1, Side.Y, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLessOrEqual.Y.8a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLessOrEqual(ranges[i].y.start + ranges[i].y.length + 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });


                f = i > 0;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestLess.X.1", f, delegate () { int nearestStart; return tree.NearestLess(ranges[i].x.start, Side.X, out nearestStart); });
                //
                p = f ? ranges[i - 1].x.start : 0;
                r = f ? ranges[i - 1] : new Range2MapEntry();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.2", delegate () { int nearestStart; tree.NearestLess(ranges[i].x.start, Side.X, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLess(ranges[i].x.start, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.3", delegate () { int nearestStart; return tree.NearestLess(ranges[i].x.start + 1, Side.X, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.4", delegate () { int nearestStart; tree.NearestLess(ranges[i].x.start + 1, Side.X, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLess(ranges[i].x.start + 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == ranges[i].x.start) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.5", delegate () { int nearestStart; return tree.NearestLess(ranges[i].x.start + ranges[i].x.length, Side.X, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.6", delegate () { int nearestStart; tree.NearestLess(ranges[i].x.start + ranges[i].x.length, Side.X, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.X.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLess(ranges[i].x.start + ranges[i].x.length, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff & (nearestStart == ranges[i].x.start) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });


                f = i > 0;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestLess.Y.1", f, delegate () { int nearestStart; return tree.NearestLess(ranges[i].y.start, Side.Y, out nearestStart); });
                //
                p = f ? ranges[i - 1].y.start : 0;
                r = f ? ranges[i - 1] : new Range2MapEntry();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.2", delegate () { int nearestStart; tree.NearestLess(ranges[i].y.start, Side.Y, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLess(ranges[i].y.start, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.3", delegate () { int nearestStart; return tree.NearestLess(ranges[i].y.start + 1, Side.Y, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.4", delegate () { int nearestStart; tree.NearestLess(ranges[i].y.start + 1, Side.Y, out nearestStart); return nearestStart == ranges[i].y.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLess(ranges[i].y.start + 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == ranges[i].y.start) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.5", delegate () { int nearestStart; return tree.NearestLess(ranges[i].y.start + ranges[i].y.length, Side.Y, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.6", delegate () { int nearestStart; tree.NearestLess(ranges[i].y.start + ranges[i].y.length, Side.Y, out nearestStart); return nearestStart == ranges[i].y.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestLess.Y.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestLess(ranges[i].y.start + ranges[i].y.length, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff & (nearestStart == ranges[i].y.start) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.1", delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].x.start, Side.X, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.2", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].x.start, Side.X, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreaterOrEqual(ranges[i].x.start, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == ranges[i].x.start) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.3", delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].x.start - 1, Side.X, out nearestStart); });
                //
                f = (i == 0) || (ranges[i - 1].x.length != 1);
                p = f ? ranges[i].x.start : ranges[i - 1].x.start;
                r = f ? ranges[i] : ranges[i - 1];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.4", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].x.start - 1, Side.X, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreaterOrEqual(ranges[i].x.start - 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                f = i + 1 < ranges.Length;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestGreaterOrEqual.X.5", f, delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].x.start + 1, Side.X, out nearestStart); });
                //
                p = f ? ranges[i + 1].x.start : (ranges[i].x.start + ranges[i].x.length);
                r = f ? ranges[i + 1] : endcap;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.6", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].x.start + 1, Side.X, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.X.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreaterOrEqual(ranges[i].x.start + 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });


                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.1", delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].y.start, Side.Y, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.2", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].y.start, Side.Y, out nearestStart); return nearestStart == ranges[i].y.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreaterOrEqual(ranges[i].y.start, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == ranges[i].y.start) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.3", delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].y.start - 1, Side.Y, out nearestStart); });
                //
                f = (i == 0) || (ranges[i - 1].y.length != 1);
                p = f ? ranges[i].y.start : ranges[i - 1].y.start;
                r = f ? ranges[i] : ranges[i - 1];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.4", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].y.start - 1, Side.Y, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreaterOrEqual(ranges[i].y.start - 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                f = i + 1 < ranges.Length;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestGreaterOrEqual.Y.5", f, delegate () { int nearestStart; return tree.NearestGreaterOrEqual(ranges[i].y.start + 1, Side.Y, out nearestStart); });
                //
                p = f ? ranges[i + 1].y.start : (ranges[i].y.start + ranges[i].y.length);
                r = f ? ranges[i + 1] : endcap;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.6", delegate () { int nearestStart; tree.NearestGreaterOrEqual(ranges[i].y.start + 1, Side.Y, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreaterOrEqual.Y.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreaterOrEqual(ranges[i].y.start + 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });


                f = i + 1 < ranges.Length;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestGreater.X.1", f, delegate () { int nearestStart; return tree.NearestGreater(ranges[i].x.start, Side.X, out nearestStart); });
                //
                p = f ? ranges[i + 1].x.start : (ranges[i].x.start + ranges[i].x.length);
                r = f ? ranges[i + 1] : endcap;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.X.2", delegate () { int nearestStart; tree.NearestGreater(ranges[i].x.start, Side.X, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.X.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreater(ranges[i].x.start, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

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
                TestBool(label + " NearestGreater.X.3", f, delegate () { int nearestStart; return tree.NearestGreater(ranges[i].x.start + 1, Side.X, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.X.4", delegate () { int nearestStart; tree.NearestGreater(ranges[i].x.start + 1, Side.X, out nearestStart); return nearestStart == p; });
                r = ranges[i].x.length == 1 ? (f ? ranges[i + 2] : endcap) : (f ? ranges[i + 1] : endcap);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.X.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreater(ranges[i].x.start + 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.X.5", delegate () { int nearestStart; return tree.NearestGreater(ranges[i].x.start - 1, Side.X, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.X.6", delegate () { int nearestStart; tree.NearestGreater(ranges[i].x.start - 1, Side.X, out nearestStart); return nearestStart == ranges[i].x.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.X.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreater(ranges[i].x.start - 1, Side.X, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == ranges[i].x.start) && (otherStart == r.y.start) && (xLength == r.x.length) && (yLength == r.y.length); });


                f = i + 1 < ranges.Length;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestBool(label + " NearestGreater.Y.1", f, delegate () { int nearestStart; return tree.NearestGreater(ranges[i].y.start, Side.Y, out nearestStart); });
                //
                p = f ? ranges[i + 1].y.start : (ranges[i].y.start + ranges[i].y.length);
                r = f ? ranges[i + 1] : endcap;
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.Y.2", delegate () { int nearestStart; tree.NearestGreater(ranges[i].y.start, Side.Y, out nearestStart); return nearestStart == p; });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.Y.2a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreater(ranges[i].y.start, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                if ((i + 1 < ranges.Length) && (ranges[i].y.length == 1))
                {
                    p = ranges[i + 1].y.start + ranges[i + 1].y.length;
                }
                else
                {
                    p = ranges[i].y.start + ranges[i].y.length;
                }
                tree = makeTree();
                BuildTree(tree, sequence);
                f = p < ranges[ranges.Length - 1].y.start + ranges[ranges.Length - 1].y.length;
                TestBool(label + " NearestGreater.Y.3", f, delegate () { int nearestStart; return tree.NearestGreater(ranges[i].y.start + 1, Side.Y, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.Y.4", delegate () { int nearestStart; tree.NearestGreater(ranges[i].y.start + 1, Side.Y, out nearestStart); return nearestStart == p; });
                r = ranges[i].y.length == 1 ? (f ? ranges[i + 2] : endcap) : (f ? ranges[i + 1] : endcap);
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.Y.4a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreater(ranges[i].y.start + 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return (ff == f) && (nearestStart == p) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.Y.5", delegate () { int nearestStart; return tree.NearestGreater(ranges[i].y.start - 1, Side.Y, out nearestStart); });
                //
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.Y.6", delegate () { int nearestStart; tree.NearestGreater(ranges[i].y.start - 1, Side.Y, out nearestStart); return nearestStart == ranges[i].y.start; });
                r = ranges[i];
                tree = makeTree();
                BuildTree(tree, sequence);
                TestTrue(label + " NearestGreater.Y.6a", delegate () { int nearestStart, otherStart, xLength, yLength; bool ff = tree.NearestGreater(ranges[i].y.start - 1, Side.Y, out nearestStart, out otherStart, out xLength, out yLength); return ff && (nearestStart == ranges[i].y.start) && (otherStart == r.x.start) && (xLength == r.x.length) && (yLength == r.y.length); });

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Insert(ranges[i].x.start, Side.X, Int32.MaxValue - reference2.GetExtent(Side.X) - 1, 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.X.1a", delegate () { tree.Insert(ranges[i].x.start, Side.X, Int32.MaxValue - tree.GetExtent(Side.X) - 1, 1); });
                TestTrue(label + " Insert-overflow.X.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.X.1c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " Insert-overflow.X.1d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Insert(ranges[i].x.start, Side.X, Int32.MaxValue - reference2.GetExtent(Side.X) - 0, 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.X.2a", delegate () { tree.Insert(ranges[i].x.start, Side.X, Int32.MaxValue - tree.GetExtent(Side.X) - 0, 1); });
                TestTrue(label + " Insert-overflow.X.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.X.2c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " Insert-overflow.X.2d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Insert-overflow.X.3a", typeof(OverflowException), delegate () { tree.Insert(ranges[i].x.start, Side.X, Int32.MaxValue - tree.GetExtent(Side.X) + 1, 1); });
                TestTrue(label + " Insert-overflow.X.3b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.X.3c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " Insert-overflow.X.3d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Insert(ranges[i].y.start, Side.Y, 1, Int32.MaxValue - reference2.GetExtent(Side.Y) - 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.Y.1a", delegate () { tree.Insert(ranges[i].y.start, Side.Y, 1, Int32.MaxValue - tree.GetExtent(Side.Y) - 1); });
                TestTrue(label + " Insert-overflow.Y.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.Y.1c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " Insert-overflow.Y.1d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.Insert(ranges[i].y.start, Side.Y, 1, Int32.MaxValue - reference2.GetExtent(Side.Y) - 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " Insert-overflow.Y.2a", delegate () { tree.Insert(ranges[i].y.start, Side.Y, 1, Int32.MaxValue - tree.GetExtent(Side.Y) - 0); });
                TestTrue(label + " Insert-overflow.Y.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.Y.2c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " Insert-overflow.Y.2d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " Insert-overflow.Y.3a", typeof(OverflowException), delegate () { tree.Insert(ranges[i].y.start, Side.Y, 1, Int32.MaxValue - tree.GetExtent(Side.Y) + 1); });
                TestTrue(label + " Insert-overflow.Y.3b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " Insert-overflow.Y.3c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " Insert-overflow.Y.3d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetLength(ranges[i].x.start, Side.X, Int32.MaxValue - (reference2.GetExtent(Side.X) - ranges[i].x.length) - 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength-overflow.X.1a", delegate () { tree.SetLength(ranges[i].x.start, Side.X, Int32.MaxValue - (tree.GetExtent(Side.X) - ranges[i].x.length) - 1); });
                TestTrue(label + " SetLength-overflow.X.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.X.1c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " SetLength-overflow.X.1d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetLength(ranges[i].x.start, Side.X, Int32.MaxValue - (reference2.GetExtent(Side.X) - ranges[i].x.length) - 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength-overflow.X.2a", delegate () { tree.SetLength(ranges[i].x.start, Side.X, Int32.MaxValue - (tree.GetExtent(Side.X) - ranges[i].x.length) - 0); });
                TestTrue(label + " SetLength-overflow.X.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.X.2c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " SetLength-overflow.X.2d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " SetLength-overflow.X.2a", typeof(OverflowException), delegate () { tree.SetLength(ranges[i].x.start, Side.X, Int32.MaxValue - (tree.GetExtent(Side.X) - ranges[i].x.length) + 1); });
                TestTrue(label + " SetLength-overflow.X.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.X.2c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " SetLength-overflow.X.2d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);

                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetLength(ranges[i].y.start, Side.Y, Int32.MaxValue - (reference2.GetExtent(Side.Y) - ranges[i].y.length) - 1); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength-overflow.Y.1a", delegate () { tree.SetLength(ranges[i].y.start, Side.Y, Int32.MaxValue - (tree.GetExtent(Side.Y) - ranges[i].y.length) - 1); });
                TestTrue(label + " SetLength-overflow.Y.1b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.Y.1c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " SetLength-overflow.Y.1d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                TestNoThrow("prereq", delegate () { reference2.SetLength(ranges[i].y.start, Side.Y, Int32.MaxValue - (reference2.GetExtent(Side.Y) - ranges[i].y.length) - 0); });
                tree = makeTree();
                BuildTree(tree, sequence);
                TestNoThrow(label + " SetLength-overflow.Y.2a", delegate () { tree.SetLength(ranges[i].y.start, Side.Y, Int32.MaxValue - (tree.GetExtent(Side.Y) - ranges[i].y.length) - 0); });
                TestTrue(label + " SetLength-overflow.Y.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.Y.2c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " SetLength-overflow.Y.2d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
                //
                reference2 = reference.Clone();
                tree = makeTree();
                BuildTree(tree, sequence);
                TestThrow(label + " SetLength-overflow.Y.2a", typeof(OverflowException), delegate () { tree.SetLength(ranges[i].y.start, Side.Y, Int32.MaxValue - (tree.GetExtent(Side.Y) - ranges[i].y.length) + 1); });
                TestTrue(label + " SetLength-overflow.Y.2b", delegate () { return tree.Count == reference2.Count; });
                TestTrue(label + " SetLength-overflow.Y.2c", delegate () { return tree.GetExtent(Side.X) == reference2.GetExtent(Side.X); });
                TestTrue(label + " SetLength-overflow.Y.2d", delegate () { return tree.GetExtent(Side.Y) == reference2.GetExtent(Side.Y); });
                ValidateRangesEqual(reference2, tree);
            }

            // test items not in collection
            for (int i = -1; i < ranges.Length; i++)
            {
                IRange2List tree;

                int lx = i >= 0 ? ranges[i].x.start : Int32.MinValue / 2;
                int rx = ranges.Length != 0 ? (i + 1 < ranges.Length ? ranges[i + 1].x.start : Int32.MaxValue / 2) : 0;
                int px = (lx + rx) / 2;

                int ly = i >= 0 ? ranges[i].y.start : Int32.MinValue / 2;
                int ry = ranges.Length != 0 ? (i + 1 < ranges.Length ? ranges[i + 1].y.start : Int32.MaxValue / 2) : 0;
                int py = (ly + ry) / 2;


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " Contains.X", delegate () { return tree.Contains(px, Side.X); });
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " Contains.Y", delegate () { return tree.Contains(py, Side.Y); });
                }


                if ((px != lx) && (px != rx) && (px >= 0))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryInsert.X", delegate () { return tree.TryInsert(px, Side.X, Length, Length2); });
                    ValidateRangesEqual(reference, tree);
                }

                if ((py != ly) && (py != ry) && (py >= 0))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryInsert.Y", delegate () { return tree.TryInsert(py, Side.Y, Length, Length2); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryDelete.X", delegate () { return tree.TryDelete(px, Side.X); });
                    ValidateRangesEqual(reference, tree);
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryDelete.Y", delegate () { return tree.TryDelete(py, Side.Y); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryGetLength.X.1", delegate () { int length; return tree.TryGetLength(px, Side.X, out length); });
                    TestTrue(label + " TryGetLength.X.2", delegate () { int length; tree.TryGetLength(px, Side.X, out length); return length == default(int); });
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryGetLength.Y.1", delegate () { int length; return tree.TryGetLength(py, Side.Y, out length); });
                    TestTrue(label + " TryGetLength.Y.2", delegate () { int length; tree.TryGetLength(py, Side.Y, out length); return length == default(int); });
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TrySetLength.X", delegate () { return tree.TrySetLength(px, Side.X, Length); });
                    ValidateRangesEqual(reference, tree);
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TrySetLength.Y", delegate () { return tree.TrySetLength(py, Side.Y, Length); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryGet.X.1", delegate () { int otherStart, xLength, yLength; return tree.TryGet(px, Side.X, out otherStart, out xLength, out yLength); });
                    TestTrue(label + " TryGet.X.2", delegate () { int otherStart, xLength, yLength; tree.TryGet(px, Side.X, out otherStart, out xLength, out yLength); return otherStart == 0; });
                    TestTrue(label + " TryGet.X.3", delegate () { int otherStart, xLength, yLength; tree.TryGet(px, Side.X, out otherStart, out xLength, out yLength); return xLength == 0; });
                    TestTrue(label + " TryGet.X.4", delegate () { int otherStart, xLength, yLength; tree.TryGet(px, Side.X, out otherStart, out xLength, out yLength); return yLength == 0; });
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestFalse(label + " TryGet.Y.1", delegate () { int otherStart, xLength, yLength; return tree.TryGet(py, Side.Y, out otherStart, out xLength, out yLength); });
                    TestTrue(label + " TryGet.Y.2", delegate () { int otherStart, xLength, yLength; tree.TryGet(py, Side.Y, out otherStart, out xLength, out yLength); return otherStart == 0; });
                    TestTrue(label + " TryGet.Y.3", delegate () { int otherStart, xLength, yLength; tree.TryGet(py, Side.Y, out otherStart, out xLength, out yLength); return xLength == 0; });
                    TestTrue(label + " TryGet.Y.4", delegate () { int otherStart, xLength, yLength; tree.TryGet(py, Side.Y, out otherStart, out xLength, out yLength); return yLength == 0; });
                }


                if ((px != lx) && (px != rx) && (px >= 0))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Insert.X", typeof(ArgumentException), delegate () { tree.Insert(px, Side.X, Length, Length2); });
                    ValidateRangesEqual(reference, tree);
                }

                if ((py != ly) && (py != ry) && (py >= 0))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Insert.Y", typeof(ArgumentException), delegate () { tree.Insert(py, Side.Y, Length, Length2); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Delete.X", typeof(ArgumentException), delegate () { tree.Delete(px, Side.X); });
                    ValidateRangesEqual(reference, tree);
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Delete.Y", typeof(ArgumentException), delegate () { tree.Delete(py, Side.Y); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " GetLength.X", typeof(ArgumentException), delegate () { int length = tree.GetLength(px, Side.X); });
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " GetLength.Y", typeof(ArgumentException), delegate () { int length = tree.GetLength(py, Side.Y); });
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " SetLength.X", typeof(ArgumentException), delegate () { tree.SetLength(px, Side.X, Length); });
                    ValidateRangesEqual(reference, tree);
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " SetLength.Y", typeof(ArgumentException), delegate () { tree.SetLength(py, Side.Y, Length); });
                    ValidateRangesEqual(reference, tree);
                }


                if ((px != lx) && (px != rx))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Get.X.1", typeof(ArgumentException), delegate () { int otherStart, xLength, yLength; tree.Get(px, Side.X, out otherStart, out xLength, out yLength); });
                }

                if ((py != ly) && (py != ry))
                {
                    tree = makeTree();
                    BuildTree(tree, sequence);
                    TestThrow(label + " Get.Y.1", typeof(ArgumentException), delegate () { int otherStart, xLength, yLength; tree.Get(py, Side.Y, out otherStart, out xLength, out yLength); });
                }
            }
        }

        public override bool Do()
        {
            try
            {
                this.Range2ListBasicCoverage();
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
