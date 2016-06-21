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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    // This is a wrapper

    public class ReferenceRangeList :
        IRangeList,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRangeList>
    {
        private readonly ReferenceRangeMap<object> inner;

        //
        // Construction
        //

        public ReferenceRangeList()
        {
            this.inner = new ReferenceRangeMap<object>();
        }

        public ReferenceRangeList(ReferenceRangeList original)
            : this()
        {
            this.inner = new ReferenceRangeMap<object>(original.inner);
        }

        public ReferenceRangeList Clone()
        {
            return new ReferenceRangeList(this);
        }

        //
        // IRangeList
        //

        public uint Count { get { return inner.Count; } }

        public long LongCount { get { return inner.LongCount; } }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(int start)
        {
            return inner.Contains(start);
        }

        public bool TryInsert(int start, int xLength)
        {
            return inner.TryInsert(start, xLength, null);
        }

        public bool TryDelete(int start)
        {
            return inner.TryDelete(start);
        }

        public bool TryGetLength(int start, out int length)
        {
            return inner.TryGetLength(start, out length);
        }

        public bool TrySetLength(int start, int length)
        {
            return inner.TrySetLength(start, length);
        }

        public void Insert(int start, int xLength)
        {
            inner.Insert(start, xLength, null);
        }

        public void Delete(int start)
        {
            inner.Delete(start);
        }

        public int GetLength(int start)
        {
            return inner.GetLength(start);
        }

        public void SetLength(int start, int length)
        {
            inner.SetLength(start, length);
        }

        public void AdjustLength(int start, int adjust)
        {
            inner.AdjustLength(start, adjust);
        }

        public int GetExtent()
        {
            return inner.GetExtent();
        }

        public bool NearestLessOrEqual(int position, out int nearestStart)
        {
            return inner.NearestLessOrEqual(position, out nearestStart);
        }

        public bool NearestLess(int position, out int nearestStart)
        {
            return inner.NearestLess(position, out nearestStart);
        }

        public bool NearestGreaterOrEqual(int position, out int nearestStart)
        {
            return inner.NearestGreaterOrEqual(position, out nearestStart);
        }

        public bool NearestGreater(int position, out int nearestStart)
        {
            return inner.NearestGreater(position, out nearestStart);
        }

        public bool NearestLessOrEqual(int position, out int nearestStart, out int length)
        {
            length = 0;
            object value;
            bool f = inner.NearestLessOrEqual(position, out nearestStart);
            if (f)
            {
                bool g = inner.TryGet(nearestStart, out length, out value);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestLess(int position, out int nearestStart, out int length)
        {
            length = 0;
            object value;
            bool f = inner.NearestLess(position, out nearestStart);
            if (f)
            {
                bool g = inner.TryGet(nearestStart, out length, out value);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestGreaterOrEqual(int position, out int nearestStart, out int length)
        {
            length = 0;
            object value;
            bool f = inner.NearestGreaterOrEqual(position, out nearestStart);
            if (f)
            {
                bool g = inner.TryGet(nearestStart, out length, out value);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestGreater(int position, out int nearestStart, out int length)
        {
            length = 0;
            object value;
            bool f = inner.NearestGreater(position, out nearestStart);
            if (f)
            {
                bool g = inner.TryGet(nearestStart, out length, out value);
                Debug.Assert(g);
            }
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
            Range2MapEntry[] ranges = ((INonInvasiveRange2MapInspection)inner).GetRanges();
            for (int i = 0; i < ranges.Length; i++)
            {
                ranges[i].y = new Range();
            }
            return ranges;
        }

        void INonInvasiveRange2MapInspection.Validate()
        {
            ((INonInvasiveRange2MapInspection)inner).Validate();
        }


        //
        // IEnumerable
        //

        private EntryRangeList Convert(EntryRangeMap<object> entry)
        {
            return new EntryRangeList(entry.Start, entry.Length);
        }

        public IEnumerator<EntryRangeList> GetEnumerator()
        {
            return new AdaptEnumerator<EntryRangeList, EntryRangeMap<object>>(
                inner.GetEnumerator(),
                Convert);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AdaptEnumeratorOld<EntryRangeList, EntryRangeMap<object>>(
                ((IEnumerable)inner).GetEnumerator(),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetEnumerable()
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetFastEnumerable()
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetFastEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetFastEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetFastEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetRobustEnumerable()
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetRobustEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetRobustEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetRobustEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetFastEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetFastEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetFastEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetFastEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetRobustEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetRobustEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRangeList> GetRobustEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeList, EntryRangeMap<object>>(
                inner.GetRobustEnumerable(startAt, forward),
                Convert);
        }
    }
}
