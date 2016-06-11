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

namespace TreeLibTest
{
    public static class IntLong
    {
        //
        // translate overflow tests to the equivalent for 64-bit
        //

        public static long ToLong(int i)
        {
            if (i > Int32.MaxValue / 2)
            {
                return (long)i - Int32.MaxValue + Int64.MaxValue;
            }

            return i;
        }

        public static int ToInt(long l)
        {
            if (l > Int64.MaxValue / 2)
            {
                return (int)(l - Int64.MaxValue + Int32.MaxValue);
            }
            return (int)l;
        }

    }
}
