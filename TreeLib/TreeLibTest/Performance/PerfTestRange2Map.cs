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
using System.Runtime.CompilerServices;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class PerfTestRange2Map : Measurement.PerfTest
    {
        private readonly Range2MapFactory makeTree;
        private readonly int count;

        private IRange2Map<int> tree;


        public delegate IRange2Map<int> Range2MapFactory(int capacity);

        public PerfTestRange2Map(Range2MapFactory makeTree, int count)
        {
            this.makeTree = makeTree;
            this.count = count;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public override void UntimedPrepare()
        {
            tree = makeTree(count);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public override void TimedIteration()
        {
            // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
            // sequence of keys (with uniform distribution). The perf test will use the same set of keys and therefore the
            // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
            ParkAndMiller random = new ParkAndMiller(Seed);

            LoadTree(tree, count, ref random);
            UnloadTree(tree, null, ref random);

            if (tree.Count != 0)
            {
                throw new InvalidOperationException();
            }
        }


        private const int Seed = 1;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void LoadTree(IRange2Map<int> tree, int count, ref ParkAndMiller random)
        {
            for (int i = 0; i < count; i++)
            {
                int start = random.Next() % Math.Max(1, tree.GetExtent(Side.X));
                tree.NearestLessOrEqual(start, Side.X, out start);
                int xLength = random.Next() % 100 + 1;
                int yLength = random.Next() % 100 + 1;
                tree.Insert(start, Side.X, xLength, yLength, random.Next());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void UnloadTree(IRange2Map<int> tree, int? count, ref ParkAndMiller random)
        {
            int i = 0;
            while ((count.HasValue && (i < count.Value)) || (!count.HasValue && (tree.Count != 0)))
            {
                int start = random.Next() % tree.GetExtent(Side.X);
                tree.NearestLessOrEqual(start, Side.X, out start);
                tree.Delete(start, Side.X);
                i++;
            }
        }
    }
}
