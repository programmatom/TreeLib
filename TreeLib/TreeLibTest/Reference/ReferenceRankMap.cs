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
using System.Reflection;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    // This is a wrapper

    public class ReferenceRankMap<KeyType, ValueType> :
        IRankMap<KeyType, ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection,
        IEnumerable<EntryRankMap<KeyType, ValueType>>
        where KeyType : IComparable<KeyType>
    {
        private ReferenceMultiRankMap<KeyType, ValueType> inner = new ReferenceMultiRankMap<KeyType, ValueType>();

        //
        // Construction
        //

        public ReferenceRankMap()
        {
        }

        public ReferenceRankMap(ReferenceRankMap<KeyType, ValueType> original)
        {
            inner = new ReferenceMultiRankMap<KeyType, ValueType>(original.inner);
        }

        public ReferenceRankMap<KeyType, ValueType> Clone()
        {
            return new ReferenceRankMap<KeyType, ValueType>(this);
        }

        //
        // IRankMap
        //

        public uint Count { get { return inner.Count; } }

        public long LongCount { get { return inner.LongCount; } }

        public void Clear()
        {
            inner.Clear();
        }

        public bool ContainsKey(KeyType key)
        {
            return inner.ContainsKey(key);
        }

        public bool TryAdd(KeyType key, ValueType value)
        {
            return inner.TryAdd(key, value, 1);
        }

        public bool TryRemove(KeyType key)
        {
            return inner.TryRemove(key);
        }

        public bool TryGetValue(KeyType key, out ValueType value)
        {
            return inner.TryGetValue(key, out value);
        }

        public bool TrySetValue(KeyType key, ValueType value)
        {
            return inner.TrySetValue(key, value);
        }

        public bool TryGet(KeyType key, out ValueType value, out int rank)
        {
            int count;
            return inner.TryGet(key, out value, out rank, out count);
        }

        public bool TryGetKeyByRank(int rank, out KeyType key)
        {
            return inner.TryGetKeyByRank(rank, out key);
        }

        public void Add(KeyType key, ValueType value)
        {
            inner.Add(key, value, 1);
        }

        public void Remove(KeyType key)
        {
            inner.Remove(key);
        }

        public ValueType GetValue(KeyType key)
        {
            return inner.GetValue(key);
        }

        public void SetValue(KeyType key, ValueType value)
        {
            inner.SetValue(key, value);
        }

        public void Get(KeyType key, out ValueType value, out int rank)
        {
            int count;
            inner.Get(key, out value, out rank, out count);
        }

        public KeyType GetKeyByRank(int rank)
        {
            return inner.GetKeyByRank(rank);
        }

        public int AdjustCount(KeyType key, int countAdjust)
        {
            int count, rank;
            ValueType value;
            inner.TryGet(key, out value, out rank, out count);
            if ((count + countAdjust < 0) || (count + countAdjust > 1))
            {
                throw new ArgumentOutOfRangeException();
            }

            return inner.AdjustCount(key, countAdjust);
        }

        public void ConditionalSetOrAdd(KeyType key, UpdatePredicate<KeyType, ValueType> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException();
            }

            ValueType value;
            bool resident = TryGetValue(key, out value);

            uint version = inner.version;
            ReferenceMultiRankMap<KeyType, ValueType> savedInner = inner;
            inner = new ReferenceMultiRankMap<KeyType, ValueType>();
            inner.version = savedInner.version;

            bool add = predicate(key, ref value, resident);

            savedInner.version = inner.version;
            inner = savedInner;
            if (version != inner.version)
            {
                throw new InvalidOperationException();
            }

            if (resident)
            {
                bool f = TrySetValue(key, value);
                Debug.Assert(f);
            }
            else if (add)
            {
                bool f = TryAdd(key, value);
                Debug.Assert(f);
            }
        }

        public void ConditionalSetOrRemove(KeyType key, UpdatePredicate<KeyType, ValueType> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException();
            }

            ValueType value;
            bool resident = TryGetValue(key, out value);

            uint version = inner.version;
            ReferenceMultiRankMap<KeyType, ValueType> savedInner = inner;
            inner = new ReferenceMultiRankMap<KeyType, ValueType>();
            inner.version = savedInner.version;

            bool remove = predicate(key, ref value, resident);

            savedInner.version = inner.version;
            inner = savedInner;
            if (version != inner.version)
            {
                throw new InvalidOperationException();
            }

            if (resident)
            {
                if (remove)
                {
                    bool f = TryRemove(key);
                    Debug.Assert(f);
                }
                else
                {
                    bool f = TrySetValue(key, value);
                    Debug.Assert(f);
                }
            }
        }

        public bool Least(out KeyType leastOut, out ValueType valueOut)
        {
            return inner.Least(out leastOut, out valueOut);
        }

        public bool Least(out KeyType leastOut)
        {
            return inner.Least(out leastOut);
        }

        public bool Greatest(out KeyType greatestOut, out ValueType valueOut)
        {
            return inner.Greatest(out greatestOut, out valueOut);
        }

        public bool Greatest(out KeyType greatestOut)
        {
            return inner.Greatest(out greatestOut);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            return inner.NearestLessOrEqual(key, out nearestKey, out valueOut);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestLessOrEqual(key, out nearestKey);
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            return inner.NearestLess(key, out nearestKey, out valueOut);
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestLess(key, out nearestKey);
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            return inner.NearestGreaterOrEqual(key, out nearestKey, out valueOut);
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestGreaterOrEqual(key, out nearestKey);
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            return inner.NearestGreater(key, out nearestKey, out valueOut);
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestGreater(key, out nearestKey);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            int count;
            return inner.NearestLessOrEqual(key, out nearestKey, out value, out rank, out count);
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            int count;
            return inner.NearestLess(key, out nearestKey, out value, out rank, out count);
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            int count;
            return inner.NearestGreaterOrEqual(key, out nearestKey, out value, out rank, out count);
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            int count;
            return inner.NearestGreater(key, out nearestKey, out value, out rank, out count);
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
            return ((INonInvasiveMultiRankMapInspection)inner).GetRanks();
        }

        void INonInvasiveMultiRankMapInspection.Validate()
        {
            ((INonInvasiveMultiRankMapInspection)inner).Validate();
        }


        //
        // IEnumerable
        //

        private EntryRankMap<KeyType, ValueType> Convert(EntryMultiRankMap<KeyType, ValueType> entry)
        {
            return new EntryRankMap<KeyType, ValueType>(
                entry.Key,
                entry.Value,
                new SetValueWrapper<ValueType>(entry),
                ((IGetEnumeratorSetValueInfo<ValueType>)entry).Version,
                entry.Rank);
        }

        public IEnumerator<EntryRankMap<KeyType, ValueType>> GetEnumerator()
        {
            return new AdaptEnumerator<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetEnumerator(),
                Convert);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AdaptEnumeratorOld<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                ((IEnumerable)inner).GetEnumerator(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetFastEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetFastEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetRobustEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetRobustEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetFastEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetFastEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetRobustEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryMultiRankMap<KeyType, ValueType>>(
                inner.GetRobustEnumerable(startAt, forward),
                Convert);
        }
    }
}
