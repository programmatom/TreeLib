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
using System.Runtime.CompilerServices;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class PerfTestMap : Measurement.PerfTest
    {
        private readonly MapFactory makeTree;
        private readonly int count;

        private IOrderedMap<int, int> tree;
        private Preparation preparation;

        private const int Seed = 1;


        private class Preparation
        {
            public readonly int[] insertKeys;
            public readonly int[] deleteKeys;

            public Preparation(int count)
            {
                // Although this appears to be "random", it is always seeded with the same value, so it always produces the same
                // sequence of keys (with uniform distribution). The perf test will use the same set of keys and therefore the
                // same code paths every time it is run. I.E. This is the most convenient way to generate a large set of test keys.
                ParkAndMiller random = new ParkAndMiller(Seed);

                insertKeys = new int[count];
                AVLTreeList<int> used = new AVLTreeList<int>((uint)count, AllocationMode.DynamicDiscard);
                for (int i = 0; i < count; i++)
                {
                    do
                    {
                        insertKeys[i] = random.Next();
                    } while (used.ContainsKey(insertKeys[i]));
                    used.Add(insertKeys[i]);
                }
                deleteKeys = new int[count];
                for (int i = 0; i < count; i++)
                {
                    int key2;
                    int j = 0;
                    do
                    {
                        j++;
                        deleteKeys[i] = j < 100 ? random.Next() : Int32.MinValue;
                    } while (!used.NearestGreaterOrEqual(deleteKeys[i], out key2));
                    used.Remove(key2);
                }
            }
        }


        public delegate IOrderedMap<int, int> MapFactory(int capacity);

        public PerfTestMap(MapFactory makeTree, int count)
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
            LoadTree(tree, count, preparation.insertKeys);
            UnloadTree(tree, null, preparation.deleteKeys);

            if (tree.Count != 0)
            {
                throw new InvalidOperationException();
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void LoadTree(IOrderedMap<int, int> tree, int count, int[] keys)
        {
            for (int i = 0; i < count; i++)
            {
                int key = keys[i];
                tree.Add(key, key);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void UnloadTree(IOrderedMap<int, int> tree, int? count, int[] keys)
        {
            int i = 0;
            while ((count.HasValue && (i < count.Value)) || (!count.HasValue && (tree.Count != 0)))
            {
                int key = keys[i];
                tree.NearestGreaterOrEqual(key, out key);
                tree.Remove(key);
                i++;
            }
        }
    }
}
