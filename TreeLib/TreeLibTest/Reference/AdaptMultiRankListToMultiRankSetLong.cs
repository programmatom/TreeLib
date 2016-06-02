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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class AdaptMultiRankListToMultiRankListLong<KeyType> :
        IMultiRankList<KeyType>,
        INonInvasiveTreeInspection,
        INonInvasiveMultiRankMapInspection
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

        private static long ToLong(int i)
        {
            // translate overflow tests to the equivalent for 64-bit
            if (i > Int32.MaxValue / 2)
            {
                return (long)i - Int32.MaxValue + Int64.MaxValue;
            }

            return i;
        }

        private static int ToInt(long l)
        {
            // translate overflow tests to the equivalent for 32-bit
            if (l > Int64.MaxValue / 2)
            {
                return (int)(l - Int64.MaxValue + Int32.MaxValue);
            }
            return (int)l;
        }

        //
        // IMultiRankList
        //

        public uint Count { get { return inner.Count; } }

        public int RankCount { get { return ToInt(inner.RankCount); } }

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
            return inner.TryAdd(key, ToLong(count));
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
            rank = ToInt(rankLong);
            count = ToInt(countLong);
            return f;
        }

        public bool TryGetKeyByRank(int rank, out KeyType key)
        {
            return inner.TryGetKeyByRank(ToLong(rank), out key);
        }

        public void Add(KeyType key, int count)
        {
            inner.Add(key, ToLong(count));
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
            rank = ToInt(rankLong);
            count = ToInt(countLong);
        }

        public KeyType GetKeyByRank(int rank)
        {
            return inner.GetKeyByRank(ToLong(rank));
        }

        public void AdjustCount(KeyType key, int countAdjust)
        {
            inner.AdjustCount(key, ToLong(countAdjust));
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
            MultiRankMapEntryLong[] innerRanks = ((INonInvasiveMultiRankMapInspectionLong)inner).GetRanks();
            MultiRankMapEntry[] ranks = new MultiRankMapEntry[innerRanks.Length];
            for (int i = 0; i < innerRanks.Length; i++)
            {
                ranks[i].key = innerRanks[i].key;
                ranks[i].value = innerRanks[i].value;
                ranks[i].rank.start = ToInt(innerRanks[i].rank.start);
                ranks[i].rank.length = ToInt(innerRanks[i].rank.length);
            }
            return ranks;
        }

        void INonInvasiveMultiRankMapInspection.Validate()
        {
            ((INonInvasiveMultiRankMapInspectionLong)inner).Validate();
        }
    }
}
