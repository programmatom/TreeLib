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
    // This is part wrapper, with non-trivial additional functionality

    public class ReferenceHugeList<T> : IHugeList<T>, IList<T>, ICollection<T>, IEnumerable<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, IHugeListValidation
    {
        private readonly List<T> inner = new List<T>();

        public ReferenceHugeList()
        {
        }

        public int MaxBlockSize { get { return 1; } }

        public int Count { get { return inner.Count; } }

        public bool IsReadOnly { get { return false; } }

        public string Metadata
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public T this[int index]
        {
            get
            {
                return inner[index];
            }

            set
            {
                inner[index] = value;
            }
        }

        public void InsertRangeDefault(int index, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            T[] defaultRange = new T[count];
            inner.InsertRange(index, defaultRange);
        }

        public void InsertRange(int index, T[] items, int offset, int count)
        {
            if (items == null)
            {
                throw new ArgumentNullException();
            }
            if ((count < 0) || (offset < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (unchecked((uint)offset + (uint)count > (uint)items.Length))
            {
                throw new ArgumentException();
            }

            T[] subset = new T[count];
            Array.Copy(items, offset, subset, 0, count);
            inner.InsertRange(index, subset);
        }

        public void InsertRange(int index, T[] items)
        {
            inner.InsertRange(index, items);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            inner.InsertRange(index, collection);
        }

        public void Insert(int index, T item)
        {
            inner.Insert(index, item);
        }

        public void Add(T item)
        {
            inner.Add(item);
        }

        public void AddRange(T[] items)
        {
            inner.AddRange(items);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            inner.AddRange(collection);
        }

        public void RemoveRange(int index, int count)
        {
            inner.RemoveRange(index, count);
        }

        public void RemoveAt(int index)
        {
            inner.RemoveAt(index);
        }

        public bool Remove(T item)
        {
            return inner.Remove(item);
        }

        public int RemoveAll(Predicate<T> match)
        {
            return inner.RemoveAll(match);
        }

        public void ReplaceRange(int index, int count, T[] items, int offset, int count2)
        {
            if ((index < 0) || (count < 0) || (offset < 0) || (count2 < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (items == null)
            {
                throw new ArgumentNullException();
            }
            if (unchecked((uint)index + (uint)count > (uint)Count)
                || unchecked((uint)offset + (uint)count2 > (uint)items.Length))
            {
                throw new ArgumentException();
            }

            inner.RemoveRange(index, count);
            InsertRange(index, items, offset, count2);
        }

        public void ReplaceRange(int index, int count, T[] items)
        {
            ReplaceRange(index, count, items, 0, items.Length);
        }

        public void Clear()
        {
            inner.Clear();
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            inner.CopyTo(index, array, arrayIndex, count);
        }

        public void CopyTo(T[] items, int arrayIndex)
        {
            inner.CopyTo(items, arrayIndex);
        }

        public void CopyTo(T[] array)
        {
            inner.CopyTo(array);
        }

        public void IterateRange(int index, T[] external, int externalOffset, int count, IterateOperator<T> op)
        {
            if ((index < 0) || (externalOffset < 0) || (count < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (unchecked((uint)index + (uint)count > (uint)inner.Count)
                || ((external != null) && unchecked((uint)externalOffset + (uint)count > (uint)external.Length)))
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < count; i++)
            {
                T item = inner[i + index];
                T externalItem = default(T);
                if (external != null)
                {
                    externalItem = external[i + externalOffset];
                }
                op(ref item, ref externalItem);
                inner[i + index] = item;
                if (external != null)
                {
                    external[i + externalOffset] = externalItem;
                }
            }
        }

        public void IterateRangeBatch(int index, T[] external, int externalOffset, int count, IterateOperatorBatch<T> op)
        {
            if ((count < 0) || (index < 0) || (externalOffset < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (unchecked((uint)index + (uint)count > (uint)this.Count))
            {
                throw new ArgumentException();
            }
            if (external != null)
            {
                if (unchecked((uint)externalOffset + (uint)count > (uint)external.Length))
                {
                    throw new ArgumentException();
                }
            }

            T[] data = inner.ToArray();
            op(data, 0, external, 0, data.Length);
            inner.Clear();
            inner.AddRange(data);
        }

        public int BinarySearch(int start, int count, T value, IComparer<T> comparer, bool multi)
        {
            int i = inner.BinarySearch(start, count, value, comparer);
            if (multi)
            {
                while ((i > 0) && (0 == comparer.Compare(inner[i - 1], value)))
                {
                    i--;
                }
            }
            return i;
        }

        public int BinarySearch(int start, int count, T value, IComparer<T> comparer)
        {
            return BinarySearch(start, count, value, comparer, false/*multi*/);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, Comparer<T>.Default);
        }

        public int IndexOfAny(T[] values, int start, int count)
        {
            if (values == null)
            {
                throw new ArgumentNullException();
            }
            inner.IndexOf(default(T), start, count); // trigger customary bounds checking to match List<> implementation

            int minIndex = Int32.MaxValue;
            foreach (T value in values)
            {
                int index = inner.IndexOf(value, start, count);
                if (index >= 0)
                {
                    minIndex = Math.Min(minIndex, index);
                }
            }
            return minIndex != Int32.MaxValue ? minIndex : -1;
        }

        public int IndexOf(T value, int start, int count)
        {
            return inner.IndexOf(value, start, count);
        }

        public int IndexOf(T value, int start)
        {
            return inner.IndexOf(value, start);
        }

        public int IndexOf(T value)
        {
            return inner.IndexOf(value);
        }

        public int FindIndex(int start, int count, Predicate<T> match)
        {
            return inner.FindIndex(start, count, match);
        }

        public int FindIndex(Predicate<T> match)
        {
            return inner.FindIndex(match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return inner.FindIndex(startIndex, match);
        }

        public int LastIndexOfAny(T[] values, int end, int count)
        {
            if (values == null)
            {
                throw new ArgumentNullException();
            }
            inner.LastIndexOf(default(T), end, count); // trigger customary bounds checking to match List<> implementation

            int maxIndex = -1;
            foreach (T value in values)
            {
                int index = inner.LastIndexOf(value, end, count);
                if (index >= 0)
                {
                    maxIndex = Math.Max(maxIndex, index);
                }
            }
            return maxIndex;
        }

        public int LastIndexOf(T value, int end, int count)
        {
            return inner.LastIndexOf(value, end, count);
        }

        public int LastIndexOf(T value, int index)
        {
            return inner.LastIndexOf(value, index);
        }

        public int LastIndexOf(T value)
        {
            return inner.LastIndexOf(value);
        }

        public int FindLastIndex(int end, int count, Predicate<T> match)
        {
            return inner.FindLastIndex(end, count, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return inner.FindLastIndex(startIndex, match);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return inner.FindLastIndex(match);
        }

        public bool Contains(T value)
        {
            return inner.Contains(value);
        }

        public T[] ToArray()
        {
            T[] array = new T[Count];
            CopyTo(array);
            return array;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)inner).GetEnumerator();
        }

        void IHugeListValidation.Validate()
        {
        }

        void IHugeListValidation.Validate(out string dump)
        {
            dump = null;
        }
    }
}
