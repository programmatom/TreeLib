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
    // This implementation is INCOMPLETE - minimum necessary to wrap 'items' in method:
    //   AdaptHugeListToHugeListLong<T>.InsertRange(int index, IHugeList<T> items, int offset, int count)

    public class AdaptHugeListLongToHugeList<T> : IHugeListLong<T>, IHugeListValidation
    {
        private readonly IHugeList<T> inner;

        public AdaptHugeListLongToHugeList(IHugeList<T> inner)
        {
            this.inner = inner;
        }

        public T this[long index]
        {
            get
            {
                return inner[IntLong.ToInt(index)];
            }

            set
            {
                inner[IntLong.ToInt(index)] = value;
            }
        }

        public long Count { get { return inner.Count; } }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int MaxBlockSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void AddRange(IHugeListLong<T> collection)
        {
            throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            throw new NotImplementedException();
        }

        public void AddRange(T[] items)
        {
            throw new NotImplementedException();
        }

        public long BinarySearch(T item)
        {
            throw new NotImplementedException();
        }

        public long BinarySearch(T item, IComparer<T> comparer)
        {
            throw new NotImplementedException();
        }

        public long BinarySearch(long start, long count, T item, IComparer<T> comparer)
        {
            throw new NotImplementedException();
        }

        public long BinarySearch(long start, long count, T item, IComparer<T> comparer, bool multi)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, long arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(long index, T[] array, long arrayIndex, long count)
        {
            throw new NotImplementedException();
        }

        public long FindIndex(Predicate<T> match)
        {
            throw new NotImplementedException();
        }

        public long FindIndex(long start, Predicate<T> match)
        {
            throw new NotImplementedException();
        }

        public long FindIndex(long start, long count, Predicate<T> match)
        {
            throw new NotImplementedException();
        }

        public long FindLastIndex(Predicate<T> match)
        {
            throw new NotImplementedException();
        }

        public long FindLastIndex(long start, Predicate<T> match)
        {
            throw new NotImplementedException();
        }

        public long FindLastIndex(long start, long count, Predicate<T> match)
        {
            throw new NotImplementedException();
        }

        public long IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public long IndexOf(T value, long start)
        {
            throw new NotImplementedException();
        }

        public long IndexOf(T value, long start, long count)
        {
            throw new NotImplementedException();
        }

        public long IndexOfAny(T[] values, long start, long count)
        {
            throw new NotImplementedException();
        }

        public void Insert(long index, T item)
        {
            throw new NotImplementedException();
        }

        public void InsertRange(long index, IEnumerable<T> collection)
        {
            throw new NotImplementedException();
        }

        public void InsertRange(long index, T[] items)
        {
            throw new NotImplementedException();
        }

        public void InsertRange(long index, IHugeListLong<T> items, long offset, long count)
        {
            throw new NotImplementedException();
        }

        public void InsertRange(long index, T[] items, long offset, long count)
        {
            throw new NotImplementedException();
        }

        public void InsertRangeDefault(long index, long count)
        {
            throw new NotImplementedException();
        }

        public void IterateRange(long index, T[] external, long externalOffset, long count, IterateOperator<T> op)
        {
            throw new NotImplementedException();
        }

        public void IterateRangeBatch(long index, T[] external, long externalOffset, long count, IterateOperatorBatchLong<T> op)
        {
            throw new NotImplementedException();
        }

        public long LastIndexOf(T value)
        {
            throw new NotImplementedException();
        }

        public long LastIndexOf(T value, long index)
        {
            throw new NotImplementedException();
        }

        public long LastIndexOf(T value, long index, long count)
        {
            throw new NotImplementedException();
        }

        public long LastIndexOfAny(T[] values, long start, long count)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public long RemoveAll(Predicate<T> match)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(long index)
        {
            throw new NotImplementedException();
        }

        public void RemoveRange(long index, long count)
        {
            throw new NotImplementedException();
        }

        public void ReplaceRange(long index, long count, T[] items)
        {
            throw new NotImplementedException();
        }

        public void ReplaceRange(long index, long count, T[] items, long offset, long count2)
        {
            throw new NotImplementedException();
        }

        public T[] ToArray()
        {
            throw new NotImplementedException();
        }


        //
        // Enumeration
        //

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)inner).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        private static EntryRangeMapLong<T[]> Convert(EntryRangeMap<T[]> entry)
        {
            return new EntryRangeMapLong<T[]>(
                entry.Value,
                ((IGetEnumeratorSetValueInfo<T[]>)entry).SetValueCallack,
                ((IGetEnumeratorSetValueInfo<T[]>)entry).Version,
                IntLong.ToLong(entry.Start),
                IntLong.ToLong(entry.Length));
        }

        public IEnumerable<EntryRangeMapLong<T[]>> GetEnumerableChunked()
        {
            return new AdaptEnumerable<EntryRangeMapLong<T[]>, EntryRangeMap<T[]>>(inner.GetEnumerableChunked(), Convert);
        }

        public IEnumerable<EntryRangeMapLong<T[]>> GetEnumerableChunked(bool forward)
        {
            return new AdaptEnumerable<EntryRangeMapLong<T[]>, EntryRangeMap<T[]>>(inner.GetEnumerableChunked(forward), Convert);
        }

        public IEnumerable<EntryRangeMapLong<T[]>> GetEnumerableChunked(long start)
        {
            return new AdaptEnumerable<EntryRangeMapLong<T[]>, EntryRangeMap<T[]>>(inner.GetEnumerableChunked(IntLong.ToInt(start)), Convert);
        }

        public IEnumerable<EntryRangeMapLong<T[]>> GetEnumerableChunked(long start, bool forward)
        {
            return new AdaptEnumerable<EntryRangeMapLong<T[]>, EntryRangeMap<T[]>>(inner.GetEnumerableChunked(IntLong.ToInt(start), forward), Convert);
        }


        //
        // Validation
        //

        public string Metadata { get { return ((IHugeListValidation)inner).Metadata; } }

        public void Validate()
        {
            ((IHugeListValidation)inner).Validate();
        }

        public void Validate(out string dump)
        {
            ((IHugeListValidation)inner).Validate(out dump);
        }
    }
}
