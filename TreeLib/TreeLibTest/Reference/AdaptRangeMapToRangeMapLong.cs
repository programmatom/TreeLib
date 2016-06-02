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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class AdaptRangeMapToRangeMapLong<ValueType> :
        IRangeMap<ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection
    {
        private readonly IRangeMapLong<ValueType> inner;

        //
        // Construction
        //

        public AdaptRangeMapToRangeMapLong(IRangeMapLong<ValueType> inner)
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
        // IRange2Map
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

        public bool TryInsert(int start, int xLength, ValueType value)
        {
            return inner.TryInsert(ToLong(start), ToLong(xLength), value);
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

        public bool TryGetValue(int start, out ValueType value)
        {
            return inner.TryGetValue(ToLong(start), out value);
        }

        public bool TrySetValue(int start, ValueType value)
        {
            return inner.TrySetValue(ToLong(start), value);
        }

        public bool TryGet(int start, out int xLength, out ValueType value)
        {
            long xLengthLong;
            bool f = inner.TryGet(ToLong(start), out xLengthLong, out value);
            xLength = ToInt(xLengthLong);
            return f;
        }

        public void Insert(int start, int xLength, ValueType value)
        {
            inner.Insert(ToLong(start), ToLong(xLength), value);
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

        public ValueType GetValue(int start)
        {
            return inner.GetValue(ToLong(start));
        }

        public void SetValue(int start, ValueType value)
        {
            inner.SetValue(ToLong(start), value);
        }

        public void Get(int start, out int xLength, out ValueType value)
        {
            long xLengthLong;
            inner.Get(ToLong(start), out xLengthLong, out value);
            xLength = ToInt(xLengthLong);
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

        // uint Count { get; }

        object INonInvasiveTreeInspection.Root { get { throw new NotSupportedException(); } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            throw new NotSupportedException();
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            throw new NotSupportedException();
        }

        void INonInvasiveTreeInspection.Validate()
        {
            throw new NotSupportedException();
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
    }
}
