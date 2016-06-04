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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class AdaptRangeListToRangeListLong :
        IRangeList,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRangeList>
    {
        private readonly IRangeListLong inner;

        //
        // Construction
        //

        public AdaptRangeListToRangeListLong(IRangeListLong inner)
        {
            this.inner = inner;
        }

        private static long ToLong(int i)
        {
            // translate overflow tests to the equivalent for 64-bit
            if (i > Int32.MaxValue / 2)
            {
                return (long)i - Int32.MaxValue + Int64.MaxValue;
            }

            return i;
        }

        private static int ToInt(long l)
        {
            // translate overflow tests to the equivalent for 32-bit
            if (l > Int64.MaxValue / 2)
            {
                return (int)(l - Int64.MaxValue + Int32.MaxValue);
            }
            return (int)l;
        }

        //
        // IRangeMap
        //

        public uint Count { get { return inner.Count; } }

        public long LongCount { get { return inner.LongCount; } }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(int start)
        {
            return inner.Contains(ToLong(start));
        }

        public bool TryInsert(int start, int xLength)
        {
            return inner.TryInsert(ToLong(start), ToLong(xLength));
        }

        public bool TryDelete(int start)
        {
            return inner.TryDelete(ToLong(start));
        }

        public bool TryGetLength(int start, out int length)
        {
            long lengthLong;
            bool f = inner.TryGetLength(ToLong(start), out lengthLong);
            length = ToInt(lengthLong);
            return f;
        }

        public bool TrySetLength(int start, int length)
        {
            return inner.TrySetLength(ToLong(start), ToLong(length));
        }

        public void Insert(int start, int xLength)
        {
            inner.Insert(ToLong(start), ToLong(xLength));
        }

        public void Delete(int start)
        {
            inner.Delete(ToLong(start));
        }

        public int GetLength(int start)
        {
            return ToInt(inner.GetLength(ToLong(start)));
        }

        public void SetLength(int start, int length)
        {
            inner.SetLength(ToLong(start), ToLong(length));
        }

        public int GetExtent()
        {
            return ToInt(inner.GetExtent());
        }

        public bool NearestLessOrEqual(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLessOrEqual(ToLong(position), out nearestStartLong);
            nearestStart = ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLess(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLess(ToLong(position), out nearestStartLong);
            nearestStart = ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreaterOrEqual(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreaterOrEqual(ToLong(position), out nearestStartLong);
            nearestStart = ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreater(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreater(ToLong(position), out nearestStartLong);
            nearestStart = ToInt(nearestStartLong);
            return f;
        }


        //
        // INonInvasiveTreeInspection
        //

        uint INonInvasiveTreeInspection.Count { get { return ((INonInvasiveTreeInspection)inner).Count; } }

        object INonInvasiveTreeInspection.Root { get { return ((INonInvasiveTreeInspection)inner).Root; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetLeftChild(node);
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetRightChild(node);
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetKey(node);
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetValue(node);
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            return ((INonInvasiveTreeInspection)inner).GetMetadata(node);
        }

        void INonInvasiveTreeInspection.Validate()
        {
            ((INonInvasiveTreeInspection)inner).Validate();
        }

        //
        // INonInvasiveRange2MapInspection
        //

        Range2MapEntry[] INonInvasiveRange2MapInspection.GetRanges()
        {
            Range2MapEntryLong[] innerRanges = ((INonInvasiveRange2MapInspectionLong)inner).GetRanges();
            Range2MapEntry[] ranges = new Range2MapEntry[innerRanges.Length];
            for (int i = 0; i < innerRanges.Length; i++)
            {
                ranges[i].value = innerRanges[i].value;
                ranges[i].x.start = ToInt(innerRanges[i].x.start);
                ranges[i].x.length = ToInt(innerRanges[i].x.length);
                ranges[i].y.start = ToInt(innerRanges[i].y.start);
                ranges[i].y.length = ToInt(innerRanges[i].y.length);
            }
            return ranges;
        }

        void INonInvasiveRange2MapInspection.Validate()
        {
            ((INonInvasiveRange2MapInspectionLong)inner).Validate();
        }


        //
        // IEnumerable
        //

        public IEnumerator<EntryRangeList> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
