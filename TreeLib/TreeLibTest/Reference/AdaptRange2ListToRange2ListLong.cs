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
    public class AdaptRange2ListToRange2ListLong :
        IRange2List,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRange2List>,
        ICloneable
    {
        private readonly IRange2ListLong inner;

        //
        // Construction
        //

        public AdaptRange2ListToRange2ListLong(IRange2ListLong inner)
        {
            this.inner = inner;
        }

        public IRange2ListLong Inner { get { return inner; } }


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
            return inner.Contains(IntLong.ToLong(start), side);
        }

        public bool TryInsert(int start, Side side, int xLength, int yLength)
        {
            return inner.TryInsert(IntLong.ToLong(start), side, IntLong.ToLong(xLength), IntLong.ToLong(yLength));
        }

        public bool TryDelete(int start, Side side)
        {
            return inner.TryDelete(IntLong.ToLong(start), side);
        }

        public bool TryGetLength(int start, Side side, out int length)
        {
            long lengthLong;
            bool f = inner.TryGetLength(IntLong.ToLong(start), side, out lengthLong);
            length = IntLong.ToInt(lengthLong);
            return f;
        }

        public bool TrySetLength(int start, Side side, int length)
        {
            return inner.TrySetLength(IntLong.ToLong(start), side, IntLong.ToLong(length));
        }

        public bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength)
        {
            long otherStartLong;
            long xLengthLong;
            long yLengthLong;
            bool f = inner.TryGet(IntLong.ToLong(start), side, out otherStartLong, out xLengthLong, out yLengthLong);
            otherStart = IntLong.ToInt(otherStartLong);
            xLength = IntLong.ToInt(xLengthLong);
            yLength = IntLong.ToInt(yLengthLong);
            return f;
        }

        public bool TrySet(int start, Side side, int xLength, int yLength)
        {
            return inner.TrySet(IntLong.ToLong(start), side, IntLong.ToLong(xLength), IntLong.ToLong(yLength));
        }

        public void Insert(int start, Side side, int xLength, int yLength)
        {
            inner.Insert(IntLong.ToLong(start), side, IntLong.ToLong(xLength), IntLong.ToLong(yLength));
        }

        public void Delete(int start, Side side)
        {
            inner.Delete(IntLong.ToLong(start), side);
        }

        public int GetLength(int start, Side side)
        {
            return IntLong.ToInt(inner.GetLength(IntLong.ToLong(start), side));
        }

        public void SetLength(int start, Side side, int length)
        {
            inner.SetLength(IntLong.ToLong(start), side, IntLong.ToLong(length));
        }

        public void Get(int start, Side side, out int otherStart, out int xLength, out int yLength)
        {
            long otherStartLong;
            long xLengthLong;
            long yLengthLong;
            inner.Get(IntLong.ToLong(start), side, out otherStartLong, out xLengthLong, out yLengthLong);
            otherStart = IntLong.ToInt(otherStartLong);
            xLength = IntLong.ToInt(xLengthLong);
            yLength = IntLong.ToInt(yLengthLong);
        }

        public void Set(int start, Side side, int xLength, int yLength)
        {
            inner.Set(IntLong.ToLong(start), side, IntLong.ToLong(xLength), IntLong.ToLong(yLength));
        }

        public int GetExtent(Side side)
        {
            return IntLong.ToInt(inner.GetExtent(side));
        }

        public bool NearestLessOrEqual(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLessOrEqual(IntLong.ToLong(position), side, out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLess(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLess(IntLong.ToLong(position), side, out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreaterOrEqual(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreaterOrEqual(IntLong.ToLong(position), side, out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreater(int position, Side side, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreater(IntLong.ToLong(position), side, out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLessOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            long nearestStartLong, otherStartLong, xLengthLong, yLengthLong;
            bool f = inner.NearestLessOrEqual(IntLong.ToLong(position), side, out nearestStartLong, out otherStartLong, out xLengthLong, out yLengthLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            otherStart = IntLong.ToInt(otherStartLong);
            xLength = IntLong.ToInt(xLengthLong);
            yLength = IntLong.ToInt(yLengthLong);
            return f;
        }

        public bool NearestLess(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            long nearestStartLong, otherStartLong, xLengthLong, yLengthLong;
            bool f = inner.NearestLess(IntLong.ToLong(position), side, out nearestStartLong, out otherStartLong, out xLengthLong, out yLengthLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            otherStart = IntLong.ToInt(otherStartLong);
            xLength = IntLong.ToInt(xLengthLong);
            yLength = IntLong.ToInt(yLengthLong);
            return f;
        }

        public bool NearestGreaterOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            long nearestStartLong, otherStartLong, xLengthLong, yLengthLong;
            bool f = inner.NearestGreaterOrEqual(IntLong.ToLong(position), side, out nearestStartLong, out otherStartLong, out xLengthLong, out yLengthLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            otherStart = IntLong.ToInt(otherStartLong);
            xLength = IntLong.ToInt(xLengthLong);
            yLength = IntLong.ToInt(yLengthLong);
            return f;
        }

        public bool NearestGreater(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            long nearestStartLong, otherStartLong, xLengthLong, yLengthLong;
            bool f = inner.NearestGreater(IntLong.ToLong(position), side, out nearestStartLong, out otherStartLong, out xLengthLong, out yLengthLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            otherStart = IntLong.ToInt(otherStartLong);
            xLength = IntLong.ToInt(xLengthLong);
            yLength = IntLong.ToInt(yLengthLong);
            return f;
        }


        //
        // INonInvasiveTreeInspection
        //

        // uint Count { get; }

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
            return ((INonInvasiveTreeInspection)inner).GetKey(node);
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
                ranges[i].x.start = IntLong.ToInt(innerRanges[i].x.start);
                ranges[i].x.length = IntLong.ToInt(innerRanges[i].x.length);
                ranges[i].y.start = IntLong.ToInt(innerRanges[i].y.start);
                ranges[i].y.length = IntLong.ToInt(innerRanges[i].y.length);
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

        public IEnumerator<EntryRange2List> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }


        //
        // ICloneable
        //

        public object Clone()
        {
            return new AdaptRange2ListToRange2ListLong((IRange2ListLong)((ICloneable)inner).Clone());
        }
    }
}
