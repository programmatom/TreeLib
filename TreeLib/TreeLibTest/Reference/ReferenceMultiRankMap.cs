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

    public class ReferenceMultiRankMap<KeyType, ValueType> :
        IMultiRankMap<KeyType, ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection,
        IEnumerable<EntryMultiRankMap<KeyType, ValueType>>
        where KeyType : IComparable<KeyType>
    {
        private readonly List<Item> items = new List<Item>();
        private ushort version;

        private struct Item
        {
            public KeyType key;
            public ValueType value;
            public int count;

            public Item(KeyType key, ValueType value, int count)
            {
                this.key = key;
                this.value = value;
                this.count = count;
            }

            public override string ToString()
            {
                return String.Format("({0}, {1}, {2})", key, value, count);
            }
        }

        //
        // Construction
        //

        public ReferenceMultiRankMap()
        {
        }

        public ReferenceMultiRankMap(ReferenceMultiRankMap<KeyType, ValueType> original)
        {
            items.AddRange(original.items);
        }

        public ReferenceMultiRankMap<KeyType, ValueType> Clone()
        {
            return new ReferenceMultiRankMap<KeyType, ValueType>(this);
        }

        //
        // IMultiRankMap
        //

        public uint Count { get { return (uint)items.Count; } }

        public int RankCount
        {
            get
            {
                int index, offset;
                Find(Int32.MaxValue, out index, out offset, true/*includeEnd*/);
                return offset;
            }
        }

        public long LongCount { get { return items.Count; } }

        public void Clear()
        {
            items.Clear();
        }

        public bool ContainsKey(KeyType key)
        {
            return BinarySearch(key) >= 0;
        }

        public bool TryAdd(KeyType key, ValueType value, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int i = BinarySearch(key);
            if (i < 0)
            {
                int overflow = checked(RankCount + count);
                items.Insert(~i, new Item(key, value, count));
                this.version = unchecked((ushort)(this.version + 1));
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
                this.version = unchecked((ushort)(this.version + 1));
                return true;
            }
            return false;
        }

        public bool TryGetValue(KeyType key, out ValueType value)
        {
            int rank, count;
            return TryGet(key, out value, out rank, out count);
        }

        public bool TrySetValue(KeyType key, ValueType value)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                items[i] = new Item(items[i].key, value, items[i].count);
                return true;
            }
            return false;
        }

        public bool TryGet(KeyType key, out ValueType value, out int rank, out int count)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                rank = 0;
                for (int j = 0; j < i; j++)
                {
                    rank += items[j].count;
                }

                value = items[i].value;
                count = items[i].count;
                return true;
            }
            value = default(ValueType);
            rank = 0;
            count = 0;
            return false;
        }

        public bool TrySet(KeyType key, ValueType value, int rank)
        {
            if (rank <= 0)
            {
                return false;
            }

            int i = BinarySearch(key);
            if (i >= 0)
            {
                items[i] = new Item(items[i].key, value, rank);
                return true;
            }
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

        public void Add(KeyType key, ValueType value, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (!TryAdd(key, value, count))
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

        public void Get(KeyType key, out ValueType value, out int rank, out int count)
        {
            if (!TryGet(key, out value, out rank, out count))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public void Set(KeyType key, ValueType value, int rank)
        {
            if (!TrySet(key, value, rank))
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

                if (items[i].count + countAdjust > 0)
                {
                    int overflow = checked(RankCount + countAdjust);
                    items[i] = new Item(items[i].key, items[i].value, items[i].count + countAdjust);
                }
                else if (items[i].count + countAdjust == 0)
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
                    int overflow = checked(RankCount + countAdjust);
                    items.Insert(~i, new Item(key, default(ValueType), countAdjust));
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

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count)
        {
            value = default(ValueType);
            rank = 0;
            count = 0;
            bool f = NearestLessOrEqual(key, out nearestKey);
            if (f)
            {
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count)
        {
            value = default(ValueType);
            rank = 0;
            count = 0;
            bool f = NearestLess(key, out nearestKey);
            if (f)
            {
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count)
        {
            value = default(ValueType);
            rank = RankCount;
            count = 0;
            bool f = NearestGreaterOrEqual(key, out nearestKey);
            if (f)
            {
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count)
        {
            value = default(ValueType);
            rank = RankCount;
            count = 0;
            bool f = NearestGreater(key, out nearestKey);
            if (f)
            {
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
            }
            return f;
        }

        public bool NearestLessOrEqualByRank(int position, out int nearestStart)
        {
            if (position >= 0)
            {
                int index;
                return Find(Math.Min(position, RankCount - 1), out index, out nearestStart, false/*includeEnd*/);
            }
            nearestStart = 0;
            return false;
        }

        public bool NearestLessByRank(int position, out int nearestStart)
        {
            if (position - 1 >= 0)
            {
                int index;
                return Find(Math.Min(position - 1, RankCount - 1), out index, out nearestStart, false/*includeEnd*/);
            }
            nearestStart = 0;
            return false;
        }

        public bool NearestGreaterOrEqualByRank(int position, out int nearestStart)
        {
            int index;
            bool f = Find(position, out index, out nearestStart, false/*includeEnd*/);
            if (f)
            {
                if (position > nearestStart)
                {
                    nearestStart += items[index].count;
                    f = index < Count - 1;
                }
                return f;
            }
            nearestStart = RankCount;
            return false;
        }

        public bool NearestGreaterByRank(int position, out int nearestStart)
        {
            int index;
            bool f = Find(position, out index, out nearestStart, false/*includeEnd*/);
            if (f)
            {
                if (position >= nearestStart)
                {
                    f = index < items.Count - 1;
                    nearestStart += items[index].count;
                }
                return f;
            }
            nearestStart = RankCount;
            return false;
        }

        public bool NearestLessOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value)
        {
            nearestKey = default(KeyType);
            value = default(ValueType);
            count = 0;
            bool f = NearestLessOrEqualByRank(position, out nearestStart);
            if (f)
            {
                nearestKey = GetKeyByRank(nearestStart);
                int rank;
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
                Debug.Assert(rank == nearestStart);
            }
            return f;
        }

        public bool NearestLessByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value)
        {
            nearestKey = default(KeyType);
            value = default(ValueType);
            count = 0;
            bool f = NearestLessByRank(position, out nearestStart);
            if (f)
            {
                nearestKey = GetKeyByRank(nearestStart);
                int rank;
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
                Debug.Assert(rank == nearestStart);
            }
            return f;
        }

        public bool NearestGreaterOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value)
        {
            nearestKey = default(KeyType);
            value = default(ValueType);
            count = 0;
            bool f = NearestGreaterOrEqualByRank(position, out nearestStart);
            if (f)
            {
                nearestKey = GetKeyByRank(nearestStart);
                int rank;
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
                Debug.Assert(rank == nearestStart);
            }
            return f;
        }

        public bool NearestGreaterByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value)
        {
            nearestKey = default(KeyType);
            value = default(ValueType);
            count = 0;
            bool f = NearestGreaterByRank(position, out nearestStart);
            if (f)
            {
                nearestKey = GetKeyByRank(nearestStart);
                int rank;
                bool g = TryGet(nearestKey, out value, out rank, out count);
                Debug.Assert(g);
                Debug.Assert(rank == nearestStart);
            }
            return f;
        }


        //
        // Internals
        //

        private bool Find(int position, out int index, out int start, bool includeEnd)
        {
            start = 0;
            for (index = 0; index < items.Count; index++)
            {
                int end = start + items[index].count;
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
            return items.BinarySearch(new Item(key, default(ValueType), 0), Comparer);
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
                        new Range(offset, items[i].count),
                        items[i].value));
                offset += items[i].count;
            }
            return ranks.ToArray();
        }

        void INonInvasiveMultiRankMapInspection.Validate()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].count <= 0)
                {
                    throw new InvalidOperationException("count must be greater than zero");
                }
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

        private class Enumerator : IEnumerator<EntryMultiRankMap<KeyType, ValueType>>, ISetValue<ValueType>
        {
            private readonly ReferenceMultiRankMap<KeyType, ValueType> map;
            private readonly bool forward;
            private readonly bool robust;
            private readonly bool startKeyed;
            private readonly KeyType startKey;

            private int index;
            private EntryMultiRankMap<KeyType, ValueType> current;
            private ushort mapVersion;
            private ushort enumeratorVersion;

            public Enumerator(ReferenceMultiRankMap<KeyType, ValueType> map, bool forward, bool robust, bool startKeyed, KeyType startKey)
            {
                this.map = map;
                this.forward = forward;
                this.robust = robust;
                this.startKeyed = startKeyed;
                this.startKey = startKey;

                Reset();
            }

            public EntryMultiRankMap<KeyType, ValueType> Current { get { return current; } }

            object IEnumerator.Current { get { return this.Current; } }

            public void Dispose()
            {
            }

            private EntryMultiRankMap<KeyType, ValueType> GetItem(int index)
            {
                int start = 0;
                for (int i = 0; i < index; i++)
                {
                    start += map.items[i].count;
                }
                return new EntryMultiRankMap<KeyType, ValueType>(
                    map.items[index].key,
                    map.items[index].value,
                    this,
                    this.enumeratorVersion,
                    start,
                    map.items[index].count);
            }

            public bool MoveNext()
            {
                if (!robust && (mapVersion != map.version))
                {
                    throw new InvalidOperationException();
                }

                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                if (((index >= 0) && (index < map.Count)) && (0 != Comparer<KeyType>.Default.Compare(current.Key, map.items[index].key)))
                {
                    if (forward)
                    {
                        index = 0;
                        while ((index < map.items.Count) && (0 <= Comparer<KeyType>.Default.Compare(current.Key, map.items[index].key)))
                        {
                            index++;
                        }
                        index--;
                    }
                    else
                    {
                        index = map.items.Count - 1;
                        while ((index >= 0) && (0 >= Comparer<KeyType>.Default.Compare(current.Key, map.items[index].key)))
                        {
                            index--;
                        }
                        index++;
                    }
                }

                index = index + (forward ? 1 : -1);
                current = new EntryMultiRankMap<KeyType, ValueType>();
                if ((index >= 0) && (index < map.Count))
                {
                    current = GetItem(index);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                this.mapVersion = map.version;
                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                current = new EntryMultiRankMap<KeyType, ValueType>();

                if (forward)
                {
                    index = -1;
                    if (startKeyed)
                    {
                        while ((index + 1 < map.items.Count) && (0 < Comparer<KeyType>.Default.Compare(startKey, map.items[index + 1].key)))
                        {
                            index++;
                        }
                        if ((index >= 0) && (index < map.items.Count) && (0 == Comparer<KeyType>.Default.Compare(startKey, map.items[index].key)))
                        {
                            index--;
                        }
                    }
                }
                else
                {
                    index = map.items.Count;
                    if (startKeyed)
                    {
                        while ((index - 1 >= 0) && (0 > Comparer<KeyType>.Default.Compare(startKey, map.items[index - 1].key)))
                        {
                            index--;
                        }
                        if ((index >= 0) && (index < map.items.Count) && (0 == Comparer<KeyType>.Default.Compare(startKey, map.items[index].key)))
                        {
                            index++;
                        }
                    }
                }

                if ((index >= 0) && (index < map.items.Count))
                {
                    current = GetItem(index);
                }
            }

            public void SetValue(ValueType value, ushort expectedEnumeratorVersion)
            {
                if ((!robust && (this.mapVersion != map.version)) || (this.enumeratorVersion != expectedEnumeratorVersion))
                {
                    throw new InvalidOperationException();
                }

                map.items[index] = new Item(map.items[index].key, value, map.items[index].count);
            }
        }

        public class EnumerableSurrogate : IEnumerable<EntryMultiRankMap<KeyType, ValueType>>
        {
            private readonly ReferenceMultiRankMap<KeyType, ValueType> map;
            private readonly bool forward;
            private readonly bool robust;
            private readonly bool startKeyed;
            private readonly KeyType startKey;

            public EnumerableSurrogate(ReferenceMultiRankMap<KeyType, ValueType> map, bool forward, bool robust, bool startKeyed, KeyType startKey)
            {
                this.map = map;
                this.forward = forward;
                this.robust = robust;
                this.startKeyed = startKeyed;
                this.startKey = startKey;
            }

            public IEnumerator<EntryMultiRankMap<KeyType, ValueType>> GetEnumerator()
            {
                return new Enumerator(map, forward, robust, startKeyed, startKey);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public IEnumerator<EntryMultiRankMap<KeyType, ValueType>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetFastEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetFastEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, true/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetRobustEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, true/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt)
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt)
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt)
        {
            return new EnumerableSurrogate(this, true/*forward*/, true/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMultiRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new EnumerableSurrogate(this, forward, true/*robust*/, true/*startKeyed*/, startAt);
        }
    }
}
