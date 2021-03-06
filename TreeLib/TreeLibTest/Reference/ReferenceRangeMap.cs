﻿/*
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
    // This is a wrapper

    public class ReferenceRangeMap<ValueType> :
        IRangeMap<ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveRange2MapInspection,
        IEnumerable<EntryRangeMap<ValueType>>
    {
        private readonly ReferenceRange2Map<ValueType> inner;

        //
        // Construction
        //

        public ReferenceRangeMap()
        {
            this.inner = new ReferenceRange2Map<ValueType>();
        }

        public ReferenceRangeMap(ReferenceRangeMap<ValueType> original)
            : this()
        {
            this.inner = new ReferenceRange2Map<ValueType>(original.inner);
        }

        public ReferenceRangeMap<ValueType> Clone()
        {
            return new ReferenceRangeMap<ValueType>(this);
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
            return inner.Contains(start, Side.X);
        }

        public bool TryInsert(int start, int xLength, ValueType value)
        {
            return inner.TryInsert(start, Side.X, xLength, 1, value);
        }

        public bool TryDelete(int start)
        {
            return inner.TryDelete(start, Side.X);
        }

        public bool TryGetLength(int start, out int length)
        {
            return inner.TryGetLength(start, Side.X, out length);
        }

        public bool TrySetLength(int start, int length)
        {
            return inner.TrySetLength(start, Side.X, length);
        }

        public bool TryGetValue(int start, out ValueType value)
        {
            return inner.TryGetValue(start, Side.X, out value);
        }

        public bool TrySetValue(int start, ValueType value)
        {
            return inner.TrySetValue(start, Side.X, value);
        }

        public bool TryGet(int start, out int xLength, out ValueType value)
        {
            int otherStart, yLength;
            return inner.TryGet(start, Side.X, out otherStart, out xLength, out yLength, out value);
        }

        public bool TrySet(int start, int length, ValueType value)
        {
            return inner.TrySet(start, Side.X, length, 1, value);
        }

        public void Insert(int start, int xLength, ValueType value)
        {
            inner.Insert(start, Side.X, xLength, 1, value);
        }

        public void Delete(int start)
        {
            inner.Delete(start, Side.X);
        }

        public int GetLength(int start)
        {
            return inner.GetLength(start, Side.X);
        }

        public void SetLength(int start, int length)
        {
            inner.SetLength(start, Side.X, length);
        }

        public ValueType GetValue(int start)
        {
            return inner.GetValue(start, Side.X);
        }

        public void SetValue(int start, ValueType value)
        {
            inner.SetValue(start, Side.X, value);
        }

        public void Get(int start, out int xLength, out ValueType value)
        {
            int otherStart, yLength;
            inner.Get(start, Side.X, out otherStart, out xLength, out yLength, out value);
        }

        public void Set(int start, int length, ValueType value)
        {
            inner.Set(start, Side.X, length, 1, value);
        }

        public int AdjustLength(int start, int adjust)
        {
            return inner.AdjustLength(start, Side.X, adjust, adjust == -inner.GetLength(start, Side.X) ? -1 : 0);
        }

        public int GetExtent()
        {
            return inner.GetExtent(Side.X);
        }

        public bool NearestLessOrEqual(int position, out int nearestStart)
        {
            return inner.NearestLessOrEqual(position, Side.X, out nearestStart);
        }

        public bool NearestLess(int position, out int nearestStart)
        {
            return inner.NearestLess(position, Side.X, out nearestStart);
        }

        public bool NearestGreaterOrEqual(int position, out int nearestStart)
        {
            return inner.NearestGreaterOrEqual(position, Side.X, out nearestStart);
        }

        public bool NearestGreater(int position, out int nearestStart)
        {
            return inner.NearestGreater(position, Side.X, out nearestStart);
        }

        public bool NearestLessOrEqual(int position, out int nearestStart, out int length, out ValueType value)
        {
            int otherStart, yLength;
            return inner.NearestLessOrEqual(position, Side.X, out nearestStart, out otherStart, out length, out yLength, out value);
        }

        public bool NearestLess(int position, out int nearestStart, out int length, out ValueType value)
        {
            int otherStart, yLength;
            return inner.NearestLess(position, Side.X, out nearestStart, out otherStart, out length, out yLength, out value);
        }

        public bool NearestGreaterOrEqual(int position, out int nearestStart, out int length, out ValueType value)
        {
            int otherStart, yLength;
            return inner.NearestGreaterOrEqual(position, Side.X, out nearestStart, out otherStart, out length, out yLength, out value);
        }

        public bool NearestGreater(int position, out int nearestStart, out int length, out ValueType value)
        {
            int otherStart, yLength;
            return inner.NearestGreater(position, Side.X, out nearestStart, out otherStart, out length, out yLength, out value);
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

        private EntryRangeMap<ValueType> Convert(EntryRange2Map<ValueType> entry)
        {
            return new EntryRangeMap<ValueType>(
                entry.Value,
                new SetValueWrapper<ValueType>(entry),
                ((IGetEnumeratorSetValueInfo<ValueType>)entry).Version,
                entry.XStart,
                entry.XLength);
        }

        public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
        {
            return new AdaptEnumerator<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetEnumerator(),
                Convert);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AdaptEnumeratorOld<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                ((IEnumerable)inner).GetEnumerator(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable()
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable()
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetFastEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetFastEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable()
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetRobustEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetRobustEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetEnumerable(startAt, Side.X),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetEnumerable(startAt, Side.X, forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetFastEnumerable(startAt, Side.X),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetFastEnumerable(startAt, Side.X, forward),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable(int startAt)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetRobustEnumerable(startAt, Side.X),
                Convert);
        }

        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable(int startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRangeMap<ValueType>, EntryRange2Map<ValueType>>(
                inner.GetRobustEnumerable(startAt, Side.X, forward),
                Convert);
        }
    }
}
