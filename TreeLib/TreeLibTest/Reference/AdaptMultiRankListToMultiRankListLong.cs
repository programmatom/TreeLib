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
    public class AdaptMultiRankListToMultiRankListLong<KeyType> :
        IMultiRankList<KeyType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection,
        IEnumerable<EntryMultiRankList<KeyType>>
        where KeyType : IComparable<KeyType>
    {
        private readonly IMultiRankListLong<KeyType> inner;

        //
        // Construction
        //

        public AdaptMultiRankListToMultiRankListLong(IMultiRankListLong<KeyType> inner)
        {
            this.inner = inner;
        }

        public IMultiRankListLong<KeyType> Inner { get { return inner; } }


        //
        // IMultiRankList
        //

        public uint Count { get { return inner.Count; } }

        public int RankCount { get { return IntLong.ToInt(inner.RankCount); } }

        public long LongCount { get { return inner.LongCount; } }

        public void Clear()
        {
            inner.Clear();
        }

        public bool ContainsKey(KeyType key)
        {
            return inner.ContainsKey(key);
        }

        public bool TryAdd(KeyType key, int count)
        {
            return inner.TryAdd(key, IntLong.ToLong(count));
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

        public bool TryGet(KeyType key, out KeyType keyOut, out int rank, out int count)
        {
            long rankLong;
            long countLong;
            bool f = inner.TryGet(key, out keyOut, out rankLong, out countLong);
            rank = IntLong.ToInt(rankLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool TrySet(KeyType key, int rank)
        {
            return inner.TrySet(key, IntLong.ToLong(rank));
        }

        public bool TryGetKeyByRank(int rank, out KeyType key)
        {
            return inner.TryGetKeyByRank(IntLong.ToLong(rank), out key);
        }

        public void Add(KeyType key, int count)
        {
            inner.Add(key, IntLong.ToLong(count));
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

        public void Get(KeyType key, out KeyType keyOut, out int rank, out int count)
        {
            long rankLong;
            long countLong;
            inner.Get(key, out keyOut, out rankLong, out countLong);
            rank = IntLong.ToInt(rankLong);
            count = IntLong.ToInt(countLong);
        }

        public void Set(KeyType key, int rank)
        {
            inner.Set(key, IntLong.ToLong(rank));
        }

        public KeyType GetKeyByRank(int rank)
        {
            return inner.GetKeyByRank(IntLong.ToLong(rank));
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

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out int rank, out int count)
        {
            long rankLong, countLong;
            bool f = inner.NearestLessOrEqual(key, out nearestKey, out rankLong, out countLong);
            rank = IntLong.ToInt(rankLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out int rank, out int count)
        {
            long rankLong, countLong;
            bool f = inner.NearestLess(key, out nearestKey, out rankLong, out countLong);
            rank = IntLong.ToInt(rankLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out int rank, out int count)
        {
            long rankLong, countLong;
            bool f = inner.NearestGreaterOrEqual(key, out nearestKey, out rankLong, out countLong);
            rank = IntLong.ToInt(rankLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out int rank, out int count)
        {
            long rankLong, countLong;
            bool f = inner.NearestGreater(key, out nearestKey, out rankLong, out countLong);
            rank = IntLong.ToInt(rankLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool NearestLessOrEqualByRank(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLessOrEqualByRank(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLessByRank(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestLessByRank(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreaterOrEqualByRank(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreaterOrEqualByRank(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestGreaterByRank(int position, out int nearestStart)
        {
            long nearestStartLong;
            bool f = inner.NearestGreaterByRank(IntLong.ToLong(position), out nearestStartLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            return f;
        }

        public bool NearestLessOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count)
        {
            long nearestStartLong, countLong;
            bool f = inner.NearestLessOrEqualByRank(IntLong.ToLong(position), out nearestKey, out nearestStartLong, out countLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool NearestLessByRank(int position, out KeyType nearestKey, out int nearestStart, out int count)
        {
            long nearestStartLong, countLong;
            bool f = inner.NearestLessByRank(IntLong.ToLong(position), out nearestKey, out nearestStartLong, out countLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool NearestGreaterOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count)
        {
            long nearestStartLong, countLong;
            bool f = inner.NearestGreaterOrEqualByRank(IntLong.ToLong(position), out nearestKey, out nearestStartLong, out countLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            count = IntLong.ToInt(countLong);
            return f;
        }

        public bool NearestGreaterByRank(int position, out KeyType nearestKey, out int nearestStart, out int count)
        {
            long nearestStartLong, countLong;
            bool f = inner.NearestGreaterByRank(IntLong.ToLong(position), out nearestKey, out nearestStartLong, out countLong);
            nearestStart = IntLong.ToInt(nearestStartLong);
            count = IntLong.ToInt(countLong);
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
        // INonInvasiveMultiRankMapInspection
        //

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

        public IEnumerator<EntryMultiRankList<KeyType>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
