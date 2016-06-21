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
    public class AdaptRankMapToRankMapLong<KeyType, ValueType> :
        IRankMap<KeyType, ValueType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection,
        IEnumerable<EntryRankMap<KeyType, ValueType>>,
        ICloneable
        where KeyType : IComparable<KeyType>
    {
        private readonly IRankMapLong<KeyType, ValueType> inner;


        //
        // Construction
        //

        // Caller must create inner collection and provide it, since we can't know how to select the implementation.
        public AdaptRankMapToRankMapLong(IRankMapLong<KeyType, ValueType> inner)
        {
            this.inner = inner;
        }

        public IRankMapLong<KeyType, ValueType> Inner { get { return inner; } }


        //
        // IRankMapLong
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
            return inner.TryAdd(key, value);
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
            long rankLong;
            bool f = inner.TryGet(key, out value, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool TryGetKeyByRank(int rank, out KeyType key)
        {
            return inner.TryGetKeyByRank(rank, out key);
        }

        public void Add(KeyType key, ValueType value)
        {
            inner.Add(key, value);
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
            long rankLong;
            inner.Get(key, out value, out rankLong);
            rank = IntLong.ToInt(rankLong);
        }

        public KeyType GetKeyByRank(int rank)
        {
            return inner.GetKeyByRank(rank);
        }

        public void AdjustCount(KeyType key, int countAdjust)
        {
            inner.AdjustCount(key, IntLong.ToLong(countAdjust));
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
            long rankLong;
            bool f = inner.NearestLessOrEqual(key, out nearestKey, out value, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            long rankLong;
            bool f = inner.NearestLess(key, out nearestKey, out value, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            long rankLong;
            bool f = inner.NearestGreaterOrEqual(key, out nearestKey, out value, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out int rank)
        {
            long rankLong;
            bool f = inner.NearestGreater(key, out nearestKey, out value, out rankLong);
            rank = IntLong.ToInt(rankLong);
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
            KeyValue<KeyType, ValueType> kv = (KeyValue<KeyType, ValueType>)((INonInvasiveTreeInspection)inner).GetKey(node);
            return kv.key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            KeyValue<KeyType, ValueType> kv = (KeyValue<KeyType, ValueType>)((INonInvasiveTreeInspection)inner).GetKey(node);
            return kv.value;
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
        // INonInvasiveMultiRankMapInspection
        //

        uint INonInvasiveMultiRankMapInspection.Count { get { return (uint)((INonInvasiveMultiRankMapInspectionLong)inner).Count; } }

        MultiRankMapEntry[] INonInvasiveMultiRankMapInspection.GetRanks()
        {
            MultiRankMapEntryLong[] innerRanks = ((INonInvasiveMultiRankMapInspectionLong)inner).GetRanks();
            MultiRankMapEntry[] ranks = new MultiRankMapEntry[innerRanks.Length];
            for (int i = 0; i < innerRanks.Length; i++)
            {
                ranks[i].key = innerRanks[i].key;
                ranks[i].value = innerRanks[i].value;
                ranks[i].rank.start = IntLong.ToInt(innerRanks[i].rank.start);
                ranks[i].rank.length = IntLong.ToInt(innerRanks[i].rank.length);
            }
            return ranks;
        }

        void INonInvasiveMultiRankMapInspection.Validate()
        {
            ((INonInvasiveMultiRankMapInspectionLong)inner).Validate();
        }


        //
        // ICloneable
        //

        public object Clone()
        {
            return new AdaptRankMapToRankMapLong<KeyType, ValueType>((IRankMapLong<KeyType, ValueType>)((ICloneable)inner).Clone());
        }


        //
        // IEnumerable
        //

        private EntryRankMap<KeyType, ValueType> Convert(EntryRankMapLong<KeyType, ValueType> entry)
        {
            return new EntryRankMap<KeyType, ValueType>(
                entry.Key,
                entry.Value,
                (ISetValue<ValueType>)entry.GetType().GetField("enumerator", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry),
                (ushort)entry.GetType().GetField("version", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry),
                IntLong.ToInt(entry.Rank));
        }

        public IEnumerator<EntryRankMap<KeyType, ValueType>> GetEnumerator()
        {
            return new AdaptEnumerator<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetEnumerator(),
                Convert);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AdaptEnumeratorOld<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                ((IEnumerable)inner).GetEnumerator(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetFastEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetFastEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetRobustEnumerable(),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetRobustEnumerable(forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetFastEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetFastEnumerable(startAt, forward),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetRobustEnumerable(startAt),
                Convert);
        }

        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new AdaptEnumerable<EntryRankMap<KeyType, ValueType>, EntryRankMapLong<KeyType, ValueType>>(
                inner.GetRobustEnumerable(startAt, forward),
                Convert);
        }
    }
}
