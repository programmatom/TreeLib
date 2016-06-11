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
    // This is a wrapper

    public class ReferenceRange2List :
        IRange2List,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRange2List>
    {
        private readonly ReferenceRange2Map<object> inner;

        //
        // Construction
        //

        public ReferenceRange2List()
        {
            inner = new ReferenceRange2Map<object>();
        }

        public ReferenceRange2List(ReferenceRange2List original)
        {
            inner = new ReferenceRange2Map<object>(original.inner);
        }

        public ReferenceRange2List Clone()
        {
            return new ReferenceRange2List(this);
        }

        //
        // IRange2List
        //

        public uint Count { get { return inner.Count; } }

        public long LongCount { get { return inner.LongCount; } }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(int start, Side side)
        {
            return inner.Contains(start, side);
        }

        public bool TryInsert(int start, Side side, int xLength, int yLength)
        {
            return inner.TryInsert(start, side, xLength, yLength, null);
        }

        public bool TryDelete(int start, Side side)
        {
            return inner.TryDelete(start, side);
        }

        public bool TryGetLength(int start, Side side, out int length)
        {
            return inner.TryGetLength(start, side, out length);
        }

        public bool TrySetLength(int start, Side side, int length)
        {
            return inner.TrySetLength(start, side, length);
        }

        public bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength)
        {
            object value;
            return inner.TryGet(start, side, out otherStart, out xLength, out yLength, out value);
        }

        public bool TrySet(int start, Side side, int xLength, int yLength)
        {
            return inner.TrySet(start, side, xLength, yLength, null);
        }

        public void Insert(int start, Side side, int xLength, int yLength)
        {
            inner.Insert(start, side, xLength, yLength, null);
        }

        public void Delete(int start, Side side)
        {
            inner.Delete(start, side);
        }

        public int GetLength(int start, Side side)
        {
            return inner.GetLength(start, side);
        }

        public void SetLength(int start, Side side, int length)
        {
            inner.SetLength(start, side, length);
        }

        public void Get(int start, Side side, out int otherStart, out int xLength, out int yLength)
        {
            object value;
            inner.Get(start, side, out otherStart, out xLength, out yLength, out value);
        }

        public void Set(int start, Side side, int xLength, int yLength)
        {
            inner.Set(start, side, xLength, yLength, null);
        }

        public int GetExtent(Side side)
        {
            return inner.GetExtent(side);
        }

        public bool NearestLessOrEqual(int position, Side side, out int nearestStart)
        {
            return inner.NearestLessOrEqual(position, side, out nearestStart);
        }

        public bool NearestLess(int position, Side side, out int nearestStart)
        {
            return inner.NearestLess(position, side, out nearestStart);
        }

        public bool NearestGreaterOrEqual(int position, Side side, out int nearestStart)
        {
            return inner.NearestGreaterOrEqual(position, side, out nearestStart);
        }

        public bool NearestGreater(int position, Side side, out int nearestStart)
        {
            return inner.NearestGreater(position, side, out nearestStart);
        }

        public bool NearestLessOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            object value;
            return inner.NearestLessOrEqual(position, side, out nearestStart, out otherStart, out xLength, out yLength, out value);
        }

        public bool NearestLess(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            object value;
            return inner.NearestLess(position, side, out nearestStart, out otherStart, out xLength, out yLength, out value);
        }

        public bool NearestGreaterOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            object value;
            return inner.NearestGreaterOrEqual(position, side, out nearestStart, out otherStart, out xLength, out yLength, out value);
        }

        public bool NearestGreater(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
        {
            object value;
            return inner.NearestGreater(position, side, out nearestStart, out otherStart, out xLength, out yLength, out value);
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
        }

        //
        // INonInvasiveRange2MapInspection
        //

        uint INonInvasiveRange2MapInspection.Count { get { return ((INonInvasiveRange2MapInspection)inner).Count; } }

        Range2MapEntry[] INonInvasiveRange2MapInspection.GetRanges()
        {
            return ((INonInvasiveRange2MapInspection)inner).GetRanges();
        }

        void INonInvasiveRange2MapInspection.Validate()
        {
            ((INonInvasiveRange2MapInspection)inner).Validate();
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
    }
}
