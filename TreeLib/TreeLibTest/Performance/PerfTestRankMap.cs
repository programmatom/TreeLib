﻿/*
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
using System.Runtime.CompilerServices;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class PerfTestRankMap : Measurement.PerfTest
    {
        private readonly RankMapFactory makeTree;
        private readonly int count;

        private IRankMap<int, int> tree;
        private Preparation preparation;

        private const int Seed = 1;


        private class Preparation
        {
            public readonly int[] keys;
            public readonly int[] indices;

            public Preparation(int count)
            {
                // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
                // sequence of keys (with uniform distribution). The perf test will use the same set of keys and therefore the
                // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
                ParkAndMiller random = new ParkAndMiller(Seed);

                keys = new int[count];
                indices = new int[count];
                AVLTreeList<int> used = new AVLTreeList<int>((uint)count, AllocationMode.DynamicRetainFreelist);
                for (int i = 0; i < count; i++)
                {
                    do
                    {
                        keys[i] = random.Next();
                    } while (used.ContainsKey(keys[i]));
                    indices[i] = random.Next() % (i - count);
                }
            }
        }

        public delegate IRankMap<int, int> RankMapFactory(int capacity);

        public PerfTestRankMap(RankMapFactory makeTree, int count)
        {
            this.makeTree = makeTree;
            this.count = count;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public override void UntimedPrepare()
        {
            if (preparation == null)
            {
                preparation = new Preparation(count);
            }

            tree = makeTree(count);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public override void TimedIteration()
        {
            LoadTree(tree, count, preparation.keys);
            UnloadTree(tree, null, preparation.indices);

            if (tree.Count != 0)
            {
                throw new InvalidOperationException();
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void LoadTree(IRankMap<int, int> tree, int count, int[] keys)
        {
            for (int i = 0; i < count; i++)
            {
                int key = keys[i];
                tree.TryAdd(key, key);
            }
            if (tree.Count < .999 * count)
            {
                throw new InvalidOperationException("too many collisions - choose a better seed");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void UnloadTree(IRankMap<int, int> tree, int? count, int[] indices)
        {
            int i = 0;
            while ((count.HasValue && (i < count.Value)) || (!count.HasValue && (tree.Count != 0)))
            {
                int rank = indices[i];
                int key = tree.GetKeyByRank(rank);
                tree.Remove(key);
                i++;
            }
        }
    }
}
