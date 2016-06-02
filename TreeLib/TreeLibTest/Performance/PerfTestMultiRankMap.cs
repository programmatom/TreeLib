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
    public class PerfTestMultiRankMap : Measurement.PerfTest
    {
        private readonly MultiRankMapFactory makeTree;
        private readonly int count;

        private IMultiRankMap<int, int> tree;


        public delegate IMultiRankMap<int, int> MultiRankMapFactory(int capacity);

        public PerfTestMultiRankMap(MultiRankMapFactory makeTree, int count)
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
        private static void LoadTree(IMultiRankMap<int, int> tree, int count, ref ParkAndMiller random)
        {
            for (int i = 0; i < count; i++)
            {
                int key = random.Random();
                int rankCount = random.Random() % 100 + 1;
                tree.TryAdd(key, key, rankCount);
            }
            if (tree.Count < .999 * count)
            {
                throw new InvalidOperationException("too many collisions - choose a better seed");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void UnloadTree(IMultiRankMap<int, int> tree, int? count, ref ParkAndMiller random)
        {
            int i = 0;
            while ((count.HasValue && (i < count.Value)) || (!count.HasValue && (tree.Count != 0)))
            {
                int rank = random.Random() % unchecked((int)tree.RankCount);
                int key = tree.GetKeyByRank(rank);
                tree.Remove(key);
                i++;
            }
        }
    }
}
