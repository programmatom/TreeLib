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
    public class AdaptRankListToRankListLong<KeyType> :
        IRankList<KeyType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection,
        IEnumerable<EntryRankList<KeyType>>
        where KeyType : IComparable<KeyType>
    {
        private readonly IRankListLong<KeyType> inner;


        //
        // Construction
        //

        // Caller must create inner collection and provide it, since we can't know how to select the implementation.
        public AdaptRankListToRankListLong(IRankListLong<KeyType> inner)
        {
            this.inner = inner;
        }

        public IRankListLong<KeyType> Inner { get { return inner; } }


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

        public bool TryAdd(KeyType key)
        {
            return inner.TryAdd(key);
        }

        public bool TryRemove(KeyType key)
        {
            return inner.TryRemove(key);
        }

        public bool TryGetKey(KeyType key, out KeyType keyOut)
        {
            return inner.TryGetKey(key, out keyOut);
        }

        public bool TrySetKey(KeyType key)
        {
            return inner.TrySetKey(key);
        }

        public bool TryGet(KeyType key, out KeyType keyOut, out int rank)
        {
            long rankLong;
            bool f = inner.TryGet(key, out keyOut, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool TryGetKeyByRank(int rank, out KeyType key)
        {
            return inner.TryGetKeyByRank(rank, out key);
        }

        public void Add(KeyType key)
        {
            inner.Add(key);
        }

        public void Remove(KeyType key)
        {
            inner.Remove(key);
        }

        public KeyType GetKey(KeyType key)
        {
            return inner.GetKey(key);
        }

        public void SetKey(KeyType key)
        {
            inner.SetKey(key);
        }

        public void Get(KeyType key, out KeyType keyOut, out int rank)
        {
            long rankLong;
            inner.Get(key, out keyOut, out rankLong);
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

        public bool Least(out KeyType leastOut)
        {
            return inner.Least(out leastOut);
        }

        public bool Greatest(out KeyType greatestOut)
        {
            return inner.Greatest(out greatestOut);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestLessOrEqual(key, out nearestKey);
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestLess(key, out nearestKey);
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestGreaterOrEqual(key, out nearestKey);
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            return inner.NearestGreater(key, out nearestKey);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out int rank)
        {
            long rankLong;
            bool f = inner.NearestLessOrEqual(key, out nearestKey, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out int rank)
        {
            long rankLong;
            bool f = inner.NearestLess(key, out nearestKey, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out int rank)
        {
            long rankLong;
            bool f = inner.NearestGreaterOrEqual(key, out nearestKey, out rankLong);
            rank = IntLong.ToInt(rankLong);
            return f;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out int rank)
        {
            long rankLong;
            bool f = inner.NearestGreater(key, out nearestKey, out rankLong);
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
        // IEnumerable
        //

        public IEnumerator<EntryRankList<KeyType>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
