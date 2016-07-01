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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class UnitTestInvariants : TestBase
    {
        public UnitTestInvariants(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private const int DurationMSec = 5 * 1000;

        private void TestAVLHeight()
        {
            Random rnd = new Random(this.seed.Value);

            int minOvershoot = Int32.MaxValue;
            int maxOvershoot = 0;
            for (int i = 0; i < 64; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    ulong c = unchecked((1UL << i) + (ulong)j);

                    int estimatedMaxDepth = AVLTreeList<int>.EstimateMaxDepth(c);
                    int theoreticalMaxDepth = AVLTreeList<int>.TheoreticalMaxDepth(c);
                    try
                    {
                        TestTrue("AVL max depth", delegate () { return estimatedMaxDepth >= theoreticalMaxDepth; });
                    }
                    catch (Exception)
                    {
                        WriteLine("failing n value: {0}", c);
                        throw;
                    }

                    minOvershoot = Math.Min(minOvershoot, estimatedMaxDepth - theoreticalMaxDepth);
                    maxOvershoot = Math.Max(maxOvershoot, estimatedMaxDepth - theoreticalMaxDepth);
                }
            }

            Stopwatch timer = Stopwatch.StartNew();
            long iter = 0;
            while (timer.ElapsedMilliseconds < DurationMSec)
            {
                double r = rnd.NextDouble() + rnd.NextDouble() * (1.0 / 2147483648); // inside knowledge -- generate full 53 bits of randomness
                double d = Math.Pow(2, rnd.NextDouble() * 64);
                ulong c = Math.Max(unchecked((ulong)d), Int64.MaxValue);

                int estimatedMaxDepth = AVLTreeList<int>.EstimateMaxDepth(c);
                int theoreticalMaxDepth = AVLTreeList<int>.TheoreticalMaxDepth(c);
                try
                {
                    TestTrue("AVL max depth", delegate () { return estimatedMaxDepth >= theoreticalMaxDepth; });
                }
                catch (Exception)
                {
                    WriteLine("failing n value: {0}", c);
                    throw;
                }

                minOvershoot = Math.Min(minOvershoot, estimatedMaxDepth - theoreticalMaxDepth);
                maxOvershoot = Math.Max(maxOvershoot, estimatedMaxDepth - theoreticalMaxDepth);

                iter++;
            }

            //Console.WriteLine("AVL height estimator overshoot: min={0}, max={1}, iter={2}", minOvershoot, maxOvershoot, iter);
        }

        private void TestRedBlackHeight()
        {
            Random rnd = new Random(this.seed.Value);

            int minOvershoot = Int32.MaxValue;
            int maxOvershoot = 0;
            for (int i = 0; i < 64; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    ulong c = unchecked((1UL << i) + (ulong)j);

                    int estimatedMaxDepth = RedBlackTreeList<int>.EstimateMaxDepth(c);
                    int theoreticalMaxDepth = RedBlackTreeList<int>.TheoreticalMaxDepth(c);
                    try
                    {
                        TestTrue("AVL max depth", delegate () { return estimatedMaxDepth >= theoreticalMaxDepth; });
                    }
                    catch (Exception)
                    {
                        WriteLine("failing n value: {0}", c);
                        throw;
                    }

                    minOvershoot = Math.Min(minOvershoot, estimatedMaxDepth - theoreticalMaxDepth);
                    maxOvershoot = Math.Max(maxOvershoot, estimatedMaxDepth - theoreticalMaxDepth);
                }
            }

            Stopwatch timer = Stopwatch.StartNew();
            long iter = 0;
            while (timer.ElapsedMilliseconds < DurationMSec)
            {
                double r = rnd.NextDouble() + rnd.NextDouble() * (1.0 / 2147483648); // inside knowledge -- generate full 53 bits of randomness
                double d = Math.Pow(2, rnd.NextDouble() * 64);
                ulong c = Math.Max(unchecked((ulong)d), Int64.MaxValue);

                int estimatedMaxDepth = RedBlackTreeList<int>.EstimateMaxDepth(c);
                int theoreticalMaxDepth = RedBlackTreeList<int>.TheoreticalMaxDepth(c);
                try
                {
                    TestTrue("AVL max depth", delegate () { return estimatedMaxDepth >= theoreticalMaxDepth; });
                }
                catch (Exception)
                {
                    WriteLine("failing n value: {0}", c);
                    throw;
                }

                minOvershoot = Math.Min(minOvershoot, estimatedMaxDepth - theoreticalMaxDepth);
                maxOvershoot = Math.Max(maxOvershoot, estimatedMaxDepth - theoreticalMaxDepth);

                iter++;
            }

            //Console.WriteLine("Red-Black height estimator overshoot: min={0}, max={1}, iter={2}", minOvershoot, maxOvershoot, iter);
        }

        public override bool Do()
        {
            try
            {
                TestRedBlackHeight();
                TestAVLHeight();

                return true;
            }
            catch (Exception exception)
            {
                WriteIteration();
                ShowException("Invariants", exception);
                throw;
            }
        }
    }
}
