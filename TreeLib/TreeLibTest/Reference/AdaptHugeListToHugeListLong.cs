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
    public class AdaptHugeListToHugeListLong<T> : IHugeList<T>, IHugeListValidation
    {
        private readonly IHugeListLong<T> inner;

        public AdaptHugeListToHugeListLong(IHugeListLong<T> inner)
        {
            this.inner = inner;
        }

        public T this[int index]
        {
            get
            {
                return inner[IntLong.ToLong(index)];
            }

            set
            {
                inner[IntLong.ToLong(index)] = value;
            }
        }

        public int Count
        {
            get
            {
                return IntLong.ToInt(inner.Count);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return inner.IsReadOnly;
            }
        }

        public int MaxBlockSize
        {
            get
            {
                return inner.MaxBlockSize;
            }
        }

        public void Add(T item)
        {
            inner.Add(item);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            inner.AddRange(collection);
        }

        public void AddRange(T[] items)
        {
            inner.AddRange(items);
        }

        public int BinarySearch(T item)
        {
            return IntLong.ToInt(inner.BinarySearch(item));
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return IntLong.ToInt(inner.BinarySearch(item, comparer));
        }

        public int BinarySearch(int start, int count, T item, IComparer<T> comparer)
        {
            return IntLong.ToInt(inner.BinarySearch(IntLong.ToLong(start), IntLong.ToLong(count), item, comparer));
        }

        public int BinarySearch(int start, int count, T item, IComparer<T> comparer, bool multi)
        {
            return IntLong.ToInt(inner.BinarySearch(IntLong.ToLong(start), IntLong.ToLong(count), item, comparer, multi));
        }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(T item)
        {
            return inner.Contains(item);
        }

        public void CopyTo(T[] array)
        {
            inner.CopyTo(array);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            inner.CopyTo(array, IntLong.ToLong(arrayIndex));
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            inner.CopyTo(IntLong.ToLong(index), array, IntLong.ToLong(arrayIndex), IntLong.ToLong(count));
        }

        public int FindIndex(Predicate<T> match)
        {
            return IntLong.ToInt(inner.FindIndex(match));
        }

        public int FindIndex(int start, Predicate<T> match)
        {
            return IntLong.ToInt(inner.FindIndex(IntLong.ToLong(start), match));
        }

        public int FindIndex(int start, int count, Predicate<T> match)
        {
            return IntLong.ToInt(inner.FindIndex(IntLong.ToLong(start), IntLong.ToLong(count), match));
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return IntLong.ToInt(inner.FindLastIndex(match));
        }

        public int FindLastIndex(int start, Predicate<T> match)
        {
            return IntLong.ToInt(inner.FindLastIndex(IntLong.ToLong(start), match));
        }

        public int FindLastIndex(int start, int count, Predicate<T> match)
        {
            return IntLong.ToInt(inner.FindLastIndex(IntLong.ToLong(start), IntLong.ToLong(count), match));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return IntLong.ToInt(inner.IndexOf(item));
        }

        public int IndexOf(T value, int start)
        {
            return IntLong.ToInt(inner.IndexOf(value, IntLong.ToLong(start)));
        }

        public int IndexOf(T value, int start, int count)
        {
            return IntLong.ToInt(inner.IndexOf(value, IntLong.ToLong(start), IntLong.ToLong(count)));
        }

        public int IndexOfAny(T[] values, int start, int count)
        {
            return IntLong.ToInt(inner.IndexOfAny(values, IntLong.ToLong(start), IntLong.ToLong(count)));
        }

        public void Insert(int index, T item)
        {
            inner.Insert(IntLong.ToLong(index), item);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            inner.InsertRange(IntLong.ToLong(index), collection);
        }

        public void InsertRange(int index, T[] items)
        {
            inner.InsertRange(IntLong.ToLong(index), items);
        }

        public void InsertRange(int index, T[] items, int offset, int count)
        {
            inner.InsertRange(IntLong.ToLong(index), items, IntLong.ToLong(offset), IntLong.ToLong(count));
        }

        public void InsertRangeDefault(int index, int count)
        {
            inner.InsertRangeDefault(IntLong.ToLong(index), IntLong.ToLong(count));
        }

        public void IterateRange(int index, T[] external, int externalOffset, int count, IterateOperator<T> op)
        {
            inner.IterateRange(IntLong.ToLong(index), external, IntLong.ToLong(externalOffset), IntLong.ToLong(count), op);
        }

        public void IterateRangeBatch(int index, T[] external, int externalOffset, int count, IterateOperatorBatch<T> op)
        {
            inner.IterateRangeBatch(
                IntLong.ToLong(index),
                external,
                IntLong.ToLong(externalOffset),
                IntLong.ToLong(count),
                delegate (T[] v, long vOffset, T[] x, long xOffset, long count2)
                {
                    op(v, IntLong.ToInt(vOffset), x, IntLong.ToInt(xOffset), IntLong.ToInt(count2));
                });
        }

        public int LastIndexOf(T value)
        {
            return IntLong.ToInt(inner.LastIndexOf(value));
        }

        public int LastIndexOf(T value, int index)
        {
            return IntLong.ToInt(inner.LastIndexOf(value, IntLong.ToLong(index)));
        }

        public int LastIndexOf(T value, int index, int count)
        {
            return IntLong.ToInt(inner.LastIndexOf(value, IntLong.ToLong(index), IntLong.ToLong(count)));
        }

        public int LastIndexOfAny(T[] values, int start, int count)
        {
            return IntLong.ToInt(inner.LastIndexOfAny(values, IntLong.ToLong(start), IntLong.ToLong(count)));
        }

        public bool Remove(T item)
        {
            return inner.Remove(item);
        }

        public int RemoveAll(Predicate<T> match)
        {
            return IntLong.ToInt(inner.RemoveAll(match));
        }

        public void RemoveAt(int index)
        {
            inner.RemoveAt(IntLong.ToLong(index));
        }

        public void RemoveRange(int index, int count)
        {
            inner.RemoveRange(IntLong.ToLong(index), IntLong.ToLong(count));
        }

        public void ReplaceRange(int index, int count, T[] items)
        {
            inner.ReplaceRange(IntLong.ToLong(index), IntLong.ToLong(count), items);
        }

        public void ReplaceRange(int index, int count, T[] items, int offset, int count2)
        {
            inner.ReplaceRange(IntLong.ToLong(index), IntLong.ToLong(count), items, IntLong.ToLong(offset), IntLong.ToLong(count2));
        }

        public T[] ToArray()
        {
            return inner.ToArray();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)inner).GetEnumerator();
        }

        string IHugeListValidation.Metadata
        {
            get
            {
                return ((IHugeListValidation)inner).Metadata;
            }
        }

        void IHugeListValidation.Validate()
        {
            ((IHugeListValidation)inner).Validate();
        }

        void IHugeListValidation.Validate(out string dump)
        {
            ((IHugeListValidation)inner).Validate(out dump);
        }
    }
}
