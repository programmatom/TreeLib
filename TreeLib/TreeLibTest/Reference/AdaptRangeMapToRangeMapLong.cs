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
using System.Reflection;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class AdaptRangeMapToRangeMapLong<ValueType> :
        IRangeMap<ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRangeMap<ValueType>>,
        ICloneable
    {
        private readonly IRangeMapLong<ValueType> inner;

        //
        // Construction
        //

        public AdaptRangeMapToRangeMapLong(IRangeMapLong<ValueType> inner)
        {
            this.inner = inner;
        }

        public IRangeMapLong<ValueType> Inner { get { return inner; } }


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
            return inner.Contains(IntLong.ToLong(start));
        }

        public bool TryInsert(int start, int xLength, ValueType value)
        {
            return inner.TryInsert(IntLong.ToLong(start), IntLong.ToLong(xLength), value);
        }

        public bool TryDelete(int start)
        {
            return inner.TryDelete(IntLong.ToLong(start));
        }

        public bool TryGetLength(int start, out int length)
        {
            long lengthLong;
            bool f = inner.TryGetLength(IntLong.ToLong(start), out lengthLong);
            length = IntLong.ToInt(lengthLong);
            return f;
        }

        public bool TrySetLength(int start, int length)
        {
            return inner.TrySetLength(IntLong.ToLong(start), IntLong.ToLong(length));
        }

        public bool TryGetValue(int start, out ValueType value)
        {
            return inner.TryGetValue(IntLong.ToLong(start), out value);
        }

        public bool TrySetValue(int start, ValueType value)
        {
            return inner.TrySetValue(IntLong.ToLong(start), value);
        }

        public bool TryGet(int start, out int xLength, out ValueType value)
        {
            long xLengthLong;
            bool f = inner.TryGet(IntLong.ToLong(start), out xLengthLong, out value);
            xLength = IntLong.ToInt(xLengthLong);
            return f;
        }

        public bool TrySet(int start, int length, ValueType value)
        {
            return inner.TrySet(IntLong.ToLong(start), IntLong.ToLong(length), value);
        }

        public void Insert(int start, int xLength, ValueType value)
        {
            inner.Insert(IntLong.ToLong(start), IntLong.ToLong(xLength), value);
        }

        public void Delete(int start)
        {
            inner.Delete(IntLong.ToLong(start));
        }

        public int GetLength(int start)
        {
            return IntLong.ToInt(inner.GetLength(IntLong.ToLong(start)));
        }

        public void SetLength(int start, int length)
        {
            inner.SetLength(IntLong.ToLong(start), IntLong.ToLong(length));
        }

        public ValueType GetValue(int start)
        {
            return inner.GetValue(IntLong.ToLong(start));
        }

        public void SetValue(int start, ValueType value)
        {
            inner.SetValue(IntLong.ToLong(start), value);
        }

        public void Get(int start, out int xLength, out ValueType value)
        {
            long xLengthLong;
            inner.Get(IntLong.ToLong(start), out xLengthLong, out value);
            xLength = IntLong.ToInt(xLengthLong);
        }

        public void Set(int start, int length, ValueType value)
        {
            inner.Set(IntLong.ToLong(start), IntLong.ToLong(length), value);
        }

        public void AdjustLength(int start, int adjust)
        {
            inner.AdjustLength(IntLong.ToLong(start), IntLong.ToLong(adjust));
        }

        public int GetExtent()
        {
            return IntLong.ToInt(inner.GetExtent());
        }

        public bool NearestLessOrEqual(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLessOrEqual(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLess(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLess(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreaterOrEqual(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreaterOrEqual(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreater(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreater(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLessOrEqual(int position, out int nearestStart, out int length, out ValueType value)
        {
            long nearestStartLong, lengthLong;
            bool f = inner.NearestLessOrEqual(IntLong.ToLong(position), out nearestStartLong, out lengthLong, out value);
            nearestStart = IntLong.ToInt(nearestStartLong);
            length = IntLong.ToInt(lengthLong);
            return f;
        }

        public bool NearestLess(int position, out int nearestStart, out int length, out ValueType value)
        {
            long nearestStartLong, lengthLong;
            bool f = inner.NearestLess(IntLong.ToLong(position), out nearestStartLong, out lengthLong, out value);
            nearestStart = IntLong.ToInt(nearestStartLong);
            length = IntLong.ToInt(lengthLong);
            return f;
        }

        public bool NearestGreaterOrEqual(int position, out int nearestStart, out int length, out ValueType value)
        {
            long nearestStartLong, lengthLong;
            bool f = inner.NearestGreaterOrEqual(IntLong.ToLong(position), out nearestStartLong, out lengthLong, out value);
            nearestStart = IntLong.ToInt(nearestStartLong);
            length = IntLong.ToInt(lengthLong);
            return f;
        }

        public bool NearestGreater(int position, out int nearestStart, out int length, out ValueType value)
        {
            long nearestStartLong, lengthLong;
            bool f = inner.NearestGreater(IntLong.ToLong(position), out nearestStartLong, out lengthLong, out value);
            nearestStart = IntLong.ToInt(nearestStartLong);
            length = IntLong.ToInt(lengthLong);
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
        // ICloneable
        //

        public object Clone()
        {
            return new AdaptRangeMapToRangeMapLong<ValueType>((IRangeMapLong<ValueType>)((ICloneable)inner).Clone());
        }


        //
        // IEnumerable
        //

        private EntryRangeMap<ValueType> Convert(EntryRangeMapLong<ValueType> entry)
        {
            return new EntryRangeMap<ValueType>(
                entry.Value,
                (ISetValue<ValueType>)entry.GetType().GetField("enumerator", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry),
                (ushort)entry.GetType().GetField("version", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry),
                IntLong.ToInt(entry.Start),
                IntLong.ToInt(entry.Length));
        }

        public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
        {
            return new AdaptEnumerator<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetEnumerator(),
                Convert);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AdaptEnumeratorOld<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                ((IEnumerable)inner).GetEnumerator(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable()
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable()
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetFastEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetFastEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable()
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetRobustEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetRobustEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetFastEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetFastEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetRobustEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRangeMapLong<ValueType>>(
                inner.GetRobustEnumerable(startAt, forward),
                Convert);
        }
    }
}
