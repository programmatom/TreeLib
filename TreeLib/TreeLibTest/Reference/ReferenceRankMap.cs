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
    // This is a primary implementation

    public class ReferenceRankMap<KeyType, ValueType> :
        IRankMap<KeyType, ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection,
        IEnumerable<EntryRankMap<KeyType, ValueType>>
        where KeyType : IComparable<KeyType>
    {
        private readonly List<Item> items = new List<Item>();

        private struct Item
        {
            public KeyType key;
            public ValueType value;

            public Item(KeyType key, ValueType value)
            {
                this.key = key;
                this.value = value;
            }

            public override string ToString()
            {
                return String.Format("({0}, {1})", key, value);
            }
        }

        //
        // Construction
        //

        public ReferenceRankMap()
        {
        }

        public ReferenceRankMap(ReferenceRankMap<KeyType, ValueType> original)
        {
            items.AddRange(original.items);
        }

        public ReferenceRankMap<KeyType, ValueType> Clone()
        {
            return new ReferenceRankMap<KeyType, ValueType>(this);
        }

        //
        // IRankMap
        //

        public uint Count { get { return (uint)items.Count; } }

        public long LongCount { get { return items.Count; } }

        public void Clear()
        {
            items.Clear();
        }

        public bool ContainsKey(KeyType key)
        {
            return BinarySearch(key) >= 0;
        }

        public bool TryAdd(KeyType key, ValueType value)
        {
            int i = BinarySearch(key);
            if (i < 0)
            {
                items.Insert(~i, new Item(key, value));
                return true;
            }
            return false;
        }

        public bool TryRemove(KeyType key)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                items.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool TryGetValue(KeyType key, out ValueType value)
        {
            int rank;
            return TryGet(key, out value, out rank);
        }

        public bool TrySetValue(KeyType key, ValueType value)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                items[i] = new Item(items[i].key, value);
                return true;
            }
            return false;
        }

        public bool TryGet(KeyType key, out ValueType value, out int rank)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                rank = 0;
                for (int j = 0; j < i; j++)
                {
                    rank += 1;
                }

                value = items[i].value;
                return true;
            }
            value = default(ValueType);
            rank = 0;
            return false;
        }

        public bool TryGetKeyByRank(int rank, out KeyType key)
        {
            if (rank < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int index, start;
            if (Find(rank, out index, out start, false/*includeEnd*/))
            {
                key = items[index].key;
                return true;
            }
            key = default(KeyType);
            return false;
        }

        public void Add(KeyType key, ValueType value)
        {
            if (!TryAdd(key, value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        public void Remove(KeyType key)
        {
            if (!TryRemove(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public ValueType GetValue(KeyType key)
        {
            ValueType value;
            if (!TryGetValue(key, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        public void SetValue(KeyType key, ValueType value)
        {
            if (!TrySetValue(key, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public void Get(KeyType key, out ValueType value, out int rank)
        {
            if (!TryGet(key, out value, out rank))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public KeyType GetKeyByRank(int rank)
        {
            KeyType key;
            if (!TryGetKeyByRank(rank, out key))
            {
                throw new ArgumentException("index not in tree");
            }
            return key;
        }

        public void AdjustCount(KeyType key, int countAdjust)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                // update and possibly remove

                if (countAdjust == 0)
                {
                }
                else if (countAdjust == -1)
                {
                    items.RemoveAt(i);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                // add

                if (countAdjust < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                else if (countAdjust > 0)
                {
                    if (countAdjust > 1)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    items.Insert(~i, new Item(key, default(ValueType)));
                }
                else
                {
                    // allow non-adding case
                    Debug.Assert(countAdjust == 0);
                }
            }
        }

        public bool Least(out KeyType leastOut, out ValueType valueOut)
        {
            if (items.Count != 0)
            {
                leastOut = items[0].key;
                valueOut = items[0].value;
                return true;
            }
            leastOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool Least(out KeyType leastOut)
        {
            ValueType value;
            return Least(out leastOut, out value);
        }

        public bool Greatest(out KeyType greatestOut, out ValueType valueOut)
        {
            if (items.Count != 0)
            {
                greatestOut = items[items.Count - 1].key;
                valueOut = items[items.Count - 1].value;
                return true;
            }
            greatestOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool Greatest(out KeyType greatestOut)
        {
            ValueType value;
            return Greatest(out greatestOut, out value);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                nearestKey = items[i].key;
                valueOut = items[i].value;
                return true;
            }
            i = ~i;
            if (i > 0)
            {
                nearestKey = items[i - 1].key;
                valueOut = items[i - 1].value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLessOrEqual(key, out nearestKey, out value);
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            int i = BinarySearch(key);
            if (i < 0)
            {
                i = ~i;
            }
            if (i > 0)
            {
                nearestKey = items[i - 1].key;
                valueOut = items[i - 1].value;
                return true;
            }
            valueOut = default(ValueType);
            nearestKey = default(KeyType);
            return false;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLess(key, out nearestKey, out value);
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                nearestKey = items[i].key;
                valueOut = items[i].value;
                return true;
            }
            i = ~i;
            if (i < items.Count)
            {
                nearestKey = items[i].key;
                valueOut = items[i].value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreaterOrEqual(key, out nearestKey, out value);
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                i++;
            }
            else
            {
                i = ~i;
            }
            if (i < items.Count)
            {
                nearestKey = items[i].key;
                valueOut = items[i].value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreater(key, out nearestKey, out value);
        }


        //
        // Internals
        //

        private bool Find(int position, out int index, out int start, bool includeEnd)
        {
            start = 0;
            for (index = 0; index < items.Count; index++)
            {
                int end = start + 1;
                if (position < end)
                {
                    return true;
                }
                start = end;
            }
            return includeEnd && (position == start);
        }

        private int BinarySearch(KeyType key)
        {
            return items.BinarySearch(new Item(key, default(ValueType)), Comparer);
        }

        private readonly static CompoundComparer Comparer = new CompoundComparer();
        private class CompoundComparer : IComparer<Item>
        {
            public int Compare(Item x, Item y)
            {
                return Comparer<KeyType>.Default.Compare(x.key, y.key);
            }
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
        // INonInvasiveMultiRankMapInspection
        //

        MultiRankMapEntry[] INonInvasiveMultiRankMapInspection.GetRanks()
        {
            List<MultiRankMapEntry> ranks = new List<MultiRankMapEntry>();
            int offset = 0;
            for (int i = 0; i < items.Count; i++)
            {
                ranks.Add(
                    new MultiRankMapEntry(
                        items[i].key,
                        new Range(offset, 1),
                        items[i].value));
                offset += 1;
            }
            return ranks.ToArray();
        }

        void INonInvasiveMultiRankMapInspection.Validate()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    if (!(Comparer<KeyType>.Default.Compare(items[i - 1].key, items[i].key) < 0))
                    {
                        throw new InvalidOperationException("ordering invariant violated");
                    }
                }
            }
        }


        //
        // IEnumerable
        //

        public IEnumerator<EntryRankMap<KeyType, ValueType>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
