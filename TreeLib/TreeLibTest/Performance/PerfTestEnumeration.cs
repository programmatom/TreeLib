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
using System.Runtime.CompilerServices;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class PerfTestEnumeration<TreeType, EntryType> : Measurement.PerfTest
    {
        private readonly TreeFactory makeTree;
        private readonly int count;
        private readonly TestBase.TreeKind treeKind;
        private readonly TestBase.EnumKind enumKind;
        private readonly bool forward;

        private TreeType tree;

        private const int Seed = 3;


        public delegate TreeType TreeFactory(int capacity);

        public PerfTestEnumeration(TreeFactory makeTree, int count, TestBase.TreeKind treeKind, TestBase.EnumKind enumKind, bool forward)
        {
            this.makeTree = makeTree;
            this.count = count;
            this.treeKind = treeKind;
            this.enumKind = enumKind;
            this.forward = forward;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public override void UntimedPrepare()
        {
            // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
            // sequence of keys (with uniform distribution). The perf test will use the same set of keys and therefore the
            // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
            ParkAndMiller random = new ParkAndMiller(Seed);

            tree = makeTree(count);

            if ((tree is IRankMap<int, int>) || (tree is IMultiRankMap<int, int>) || (tree is IOrderedMap<int, int>))
            {
                // keyed collection
                for (int i = 0; i < count; i++)
                {
                    int key = random.Next();
                    if (tree is IRankMap<int, int>)
                    {
                        if (!((IRankMap<int, int>)tree).TryAdd(key, random.Next()))
                        {
                            i--;
                        }
                    }
                    else if (tree is IMultiRankMap<int, int>)
                    {
                        if (!((IMultiRankMap<int, int>)tree).TryAdd(key, random.Next(), random.Next() % 10 + 1))
                        {
                            i--;
                        }
                    }
                    else if (tree is IOrderedMap<int, int>)
                    {
                        if (!((IOrderedMap<int, int>)tree).TryAdd(key, random.Next()))
                        {
                            i--;
                        }
                    }
                    else
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                }
            }
            else if ((tree is IRange2Map<int>) || (tree is IRangeMap<int>))
            {
                // indexed collection
                for (int i = 0; i < count; i++)
                {
                    if (tree is IRange2Map<int>)
                    {
                        int extent = ((IRange2Map<int>)tree).GetExtent(Side.X);
                        int start = extent != 0 ? random.Next() % (extent + 1) : 0;
                        ((IRange2Map<int>)tree).NearestLessOrEqual(start, Side.X, out start);
                        ((IRange2Map<int>)tree).Insert(start, Side.X, random.Next() % 10 + 1, random.Next() % 10 + 1, random.Next());
                    }
                    else if (tree is IRangeMap<int>)
                    {
                        int extent = ((IRangeMap<int>)tree).GetExtent();
                        int start = extent != 0 ? random.Next() % (extent + 1) : 0;
                        ((IRangeMap<int>)tree).NearestLessOrEqual(start, out start);
                        ((IRangeMap<int>)tree).Insert(start, random.Next() % 10 + 1, random.Next());
                    }
                    else
                    {
                        Debug.Assert(false);
                        throw new ArgumentException();
                    }
                }
            }
            else
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public override void TimedIteration()
        {
            IEnumerable<EntryType> enumerable = TestBase.GetEnumerator<EntryType>(tree, enumKind, treeKind, new TestBase.EnumArgsProvider(), forward);
            foreach (EntryType entry in enumerable)
            {
                // do nothing
            }
        }
    }
}
