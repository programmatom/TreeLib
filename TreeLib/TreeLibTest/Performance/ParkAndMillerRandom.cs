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
using System.Diagnostics;

// We use this archaic random number generator for generating perf test fodder data
// because it is repeatable and won't change with some future .NET framework
// (System.Random is not guarranteed to stay the same.)

namespace TreeLibTest
{
    public class ParkAndMiller
    {
        public const int Minimum = 1;
        public const int Maximum = 2147483646;

        private int seed;

        // Seed must be in range - else ArgumentException is thrown
        public ParkAndMiller(int seed)
        {
#if DEBUG
            if ((seed < Minimum) || (seed > Maximum))
            {
                // seed is out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            this.seed = seed;
        }

        // Seed must be in range - else ArgumentException is thrown
        public void SetSeed(int seed)
        {
#if DEBUG
            if ((seed < Minimum) || (seed > Maximum))
            {
                // seed is out of range
                Debug.Assert(false);
                throw new ArgumentException();
            }
#endif
            this.seed = seed;
        }

        // Constains seed to be in range - always succeeds
        public void ConstrainedSetSeed(int seed)
        {
            this.seed = ConstrainSeed(seed);
        }

#if DEBUG
        private const bool CheckParkAndMiller_DBG = true;
#endif

        private const int A = 16807;
        private const int M = 2147483647;
        private const int Q = 127773;
        private const int R = 2836;

        /* this implements the Park and Miller (Communications of the ACM, 1988) Minimal */
        /* Standard random number generator. it returns a number in the range [1..2147483646] */
        /* IMPORTANT: this is a linear congruential generator, a good one, but still has */
        /* all the flaws associated with them, including: */
        /*  - lack of randomness in the low order bits */
        /*  - clustering around planes in higher dimensions */
        /*  - relatively low (2^31-2) period */
        /*  - correlation between successive numbers, especially when they are small */
#if DEBUG
        private static bool Checked_DBG;
#endif
        public int Random()
        {
            int S;
            int lo;
            int hi;

#if DEBUG
            if (CheckParkAndMiller_DBG)
            {
                if (!Checked_DBG)
                {
                    Checked_DBG = true;

                    ParkAndMiller State_DBG = new ParkAndMiller(1);
                    int Value_DBG = -1;
                    for (int Counter_DBG = 1; Counter_DBG <= 10000; Counter_DBG++)
                    {
                        Value_DBG = State_DBG.Random();
                    }
                    if (Value_DBG != 1043618065)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                }
            }
#endif

            S = this.seed;

#if DEBUG
            if ((S < Minimum) || (S > Maximum))
            {
                // seed is out of range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            hi = S / Q;
            lo = S % Q;
            S = A * lo - R * hi;
            if (S <= 0)
            {
                S += M;
            }

#if DEBUG
            if ((S < Minimum) || (S > Maximum))
            {
                // seed exceeded range
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif

            this.seed = S;
            return S;
        }

        /* convert random number to 0 through and including 1 */
        public static double Double0Through1(int rnd)
        {
#if DEBUG
            if ((rnd < Minimum) || (rnd > Maximum))
            {
                // that doesn't look like a park and miller number
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            return (rnd - Minimum) / (double)(Maximum - Minimum);
        }

        /* convert random number to 0 up to but excluding 1 */
        public static double Double0ToExcluding1(int rnd)
        {
#if DEBUG
            if ((rnd < Minimum) || (rnd > Maximum))
            {
                // that doesn't look like a park and miller number
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
#endif
            return (rnd - Minimum) / (double)(Maximum - Minimum + 1);
        }

        /* constrain initial PM seed into range */
        public static int ConstrainSeed(int seed)
        {
            return unchecked((int)(((uint)(seed - Minimum) % (Maximum - Minimum + 1)) + Minimum));
        }
    }
}
