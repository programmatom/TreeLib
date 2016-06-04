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
    public class AdaptRange2MapToRange2MapLong<ValueType> :
        IRange2Map<ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRange2Map<ValueType>>
    {
        private readonly IRange2MapLong<ValueType> inner;

        //
        // Construction
        //

        public AdaptRange2MapToRange2MapLong(IRange2MapLong<ValueType> inner)
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

        public bool Contains(int start, Side side)
        {
            return inner.Contains(ToLong(start), side);
        }

        public bool TryInsert(int start, Side side, int xLength, int yLength, ValueType value)
        {
            return inner.TryInsert(ToLong(start), side, ToLong(xLength), ToLong(yLength), value);
        }

        public bool TryDelete(int start, Side side)
        {
            return inner.TryDelete(ToLong(start), side);
        }

        public bool TryGetLength(int start, Side side, out int length)
        {
            long lengthLong;
            bool f = inner.TryGetLength(ToLong(start), side, out lengthLong);
            length = ToInt(lengthLong);
            return f;
        }

        public bool TrySetLength(int start, Side side, int length)
        {
            return inner.TrySetLength(ToLong(start), side, ToLong(length));
        }

        public bool TryGetValue(int start, Side side, out ValueType value)
        {
            return inner.TryGetValue(ToLong(start), side, out value);
        }

        public bool TrySetValue(int start, Side side, ValueType value)
        {
            return inner.TrySetValue(ToLong(start), side, value);
        }

        public bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            long otherStartLong;
            long xLengthLong;
            long yLengthLong;
            bool f = inner.TryGet(ToLong(start), side, out otherStartLong, out xLengthLong, out yLengthLong, out value);
            otherStart = ToInt(otherStartLong);
            xLength = ToInt(xLengthLong);
            yLength = ToInt(yLengthLong);
            return f;
        }

        public void Insert(int start, Side side, int xLength, int yLength, ValueType value)
        {
            inner.Insert(ToLong(start), side, ToLong(xLength), ToLong(yLength), value);
        }

        public void Delete(int start, Side side)
        {
            inner.Delete(ToLong(start), side);
        }

        public int GetLength(int start, Side side)
        {
            return ToInt(inner.GetLength(ToLong(start), side));
        }

        public void SetLength(int start, Side side, int length)
        {
            inner.SetLength(ToLong(start), side, ToLong(length));
        }

        public ValueType GetValue(int start, Side side)
        {
            return inner.GetValue(ToLong(start), side);
        }

        public void SetValue(int start, Side side, ValueType value)
        {
            inner.SetValue(ToLong(start), side, value);
        }

        public void Get(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value)
        {
            long otherStartLong;
            long xLengthLong;
            long yLengthLong;
            inner.Get(ToLong(start), side, out otherStartLong, out xLengthLong, out yLengthLong, out value);
            otherStart = ToInt(otherStartLong);
            xLength = ToInt(xLengthLong);
            yLength = ToInt(yLengthLong);
        }

        public int GetExtent(Side side)
        {
            return ToInt(inner.GetExtent(side));
        }

        public bool NearestLessOrEqual(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLessOrEqual(ToLong(position), side, out nearestStartLong);
            nearestStart = ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLess(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLess(ToLong(position), side, out nearestStartLong);
            nearestStart = ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreaterOrEqual(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreaterOrEqual(ToLong(position), side, out nearestStartLong);
            nearestStart = ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreater(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreater(ToLong(position), side, out nearestStartLong);
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


        //
        // IEnumerable
        //

        public IEnumerator<EntryRange2Map<ValueType>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
