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

namespace TreeLib
{
    public enum AllocationMode
    {
        DynamicDiscard = 0,
        DynamicRetainFreelist,
        PreallocatedFixed,
    }


    //
    // IOrderedMap, IOrderedList
    //

    //[Feature(Feature.Dict)]
    public interface IOrderedMap<KeyType, ValueType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);
        bool SetOrAddValue(KeyType key, ValueType value);

        bool TryAdd(KeyType key, ValueType value);
        bool TryRemove(KeyType key);
        bool TryGetValue(KeyType key, out ValueType value);
        bool TrySetValue(KeyType key, ValueType value);

        void Add(KeyType key, ValueType value);
        void Remove(KeyType key);
        ValueType GetValue(KeyType key);
        void SetValue(KeyType key, ValueType value);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }

    //[Feature(Feature.Dict)]
    public interface IOrderedList<KeyType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key);
        bool TryRemove(KeyType key);
        bool TryGetKey(KeyType key, out KeyType keyOut);
        bool TrySetKey(KeyType key);

        void Add(KeyType key);
        void Remove(KeyType key);
        KeyType GetKey(KeyType key);
        void SetKey(KeyType key);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }


    //
    // IMultiRankMap, IMultiRankList, IMultiRankMapLong, IMultiRankListLong
    //

    //[Feature(Feature.RankMulti)]
    public interface IMultiRankMap<KeyType, ValueType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }
        int RankCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key, ValueType value, int count);
        bool TryRemove(KeyType key);
        bool TryGetValue(KeyType key, out ValueType value);
        bool TrySetValue(KeyType key, ValueType value);
        bool TryGet(KeyType key, out ValueType value, out int rank, out int count);
        bool TryGetKeyByRank(int rank, out KeyType key);

        void Add(KeyType key, ValueType value, int count);
        void Remove(KeyType key);
        ValueType GetValue(KeyType key);
        void SetValue(KeyType key, ValueType value);
        void Get(KeyType key, out ValueType value, out int rank, out int count);
        KeyType GetKeyByRank(int rank);

        void AdjustCount(KeyType key, int countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }

    //[Feature(Feature.RankMulti)]
    public interface IMultiRankList<KeyType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }
        int RankCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key, int count);
        bool TryRemove(KeyType key);
        bool TryGetKey(KeyType key, out KeyType keyOut);
        bool TrySetKey(KeyType key);
        bool TryGet(KeyType key, out KeyType keyOut, out int rank, out int count);
        bool TryGetKeyByRank(int rank, out KeyType key);

        void Add(KeyType key, int count);
        void Remove(KeyType key);
        KeyType GetKey(KeyType key);
        void SetKey(KeyType key);
        void Get(KeyType key, out KeyType keyOut, out int rank, out int count);
        KeyType GetKeyByRank(int rank);

        void AdjustCount(KeyType key, int countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }

    //[Feature(Feature.RankMulti)]
    public interface IMultiRankMapLong<KeyType, ValueType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }
        long RankCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key, ValueType value, long count);
        bool TryRemove(KeyType key);
        bool TryGetValue(KeyType key, out ValueType value);
        bool TrySetValue(KeyType key, ValueType value);
        bool TryGet(KeyType key, out ValueType value, out long rank, out long count);
        bool TryGetKeyByRank(long rank, out KeyType key);

        void Add(KeyType key, ValueType value, long count);
        void Remove(KeyType key);
        ValueType GetValue(KeyType key);
        void SetValue(KeyType key, ValueType value);
        void Get(KeyType key, out ValueType value, out long rank, out long count);
        KeyType GetKeyByRank(long rank);

        void AdjustCount(KeyType key, long countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }

    //[Feature(Feature.RankMulti)]
    public interface IMultiRankListLong<KeyType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }
        long RankCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key, long count);
        bool TryRemove(KeyType key);
        bool TryGetKey(KeyType key, out KeyType keyOut);
        bool TrySetKey(KeyType key);
        bool TryGet(KeyType key, out KeyType keyOut, out long rank, out long count);
        bool TryGetKeyByRank(long rank, out KeyType key);

        void Add(KeyType key, long count);
        void Remove(KeyType key);
        KeyType GetKey(KeyType key);
        void SetKey(KeyType key);
        void Get(KeyType key, out KeyType keyOut, out long rank, out long count);
        KeyType GetKeyByRank(long rank);

        void AdjustCount(KeyType key, long countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }


    //
    // IRankMap, IRankList, IRankMapLong, IRankListLong
    //

    //[Feature(Feature.RankMulti)]
    public interface IRankMap<KeyType, ValueType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key, ValueType value);
        bool TryRemove(KeyType key);
        bool TryGetValue(KeyType key, out ValueType value);
        bool TrySetValue(KeyType key, ValueType value);
        bool TryGet(KeyType key, out ValueType value, out int rank);
        bool TryGetKeyByRank(int rank, out KeyType key);

        void Add(KeyType key, ValueType value);
        void Remove(KeyType key);
        ValueType GetValue(KeyType key);
        void SetValue(KeyType key, ValueType value);
        void Get(KeyType key, out ValueType value, out int rank);
        KeyType GetKeyByRank(int rank);

        void AdjustCount(KeyType key, int countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }

    //[Feature(Feature.RankMulti)]
    public interface IRankList<KeyType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key);
        bool TryRemove(KeyType key);
        bool TryGetKey(KeyType key, out KeyType keyOut);
        bool TrySetKey(KeyType key);
        bool TryGet(KeyType key, out KeyType keyOut, out int rank);
        bool TryGetKeyByRank(int rank, out KeyType key);

        void Add(KeyType key);
        void Remove(KeyType key);
        KeyType GetKey(KeyType key);
        void SetKey(KeyType key);
        void Get(KeyType key, out KeyType keyOut, out int rank);
        KeyType GetKeyByRank(int rank);

        void AdjustCount(KeyType key, int countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }

    //[Feature(Feature.RankMulti)]
    public interface IRankMapLong<KeyType, ValueType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key, ValueType value);
        bool TryRemove(KeyType key);
        bool TryGetValue(KeyType key, out ValueType value);
        bool TrySetValue(KeyType key, ValueType value);
        bool TryGet(KeyType key, out ValueType value, out long rank);
        bool TryGetKeyByRank(long rank, out KeyType key);

        void Add(KeyType key, ValueType value);
        void Remove(KeyType key);
        ValueType GetValue(KeyType key);
        void SetValue(KeyType key, ValueType value);
        void Get(KeyType key, out ValueType value, out long rank);
        KeyType GetKeyByRank(long rank);

        void AdjustCount(KeyType key, long countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }

    //[Feature(Feature.RankMulti)]
    public interface IRankListLong<KeyType> where KeyType : IComparable<KeyType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool ContainsKey(KeyType key);

        bool TryAdd(KeyType key);
        bool TryRemove(KeyType key);
        bool TryGetKey(KeyType key, out KeyType keyOut);
        bool TrySetKey(KeyType key);
        bool TryGet(KeyType key, out KeyType keyOut, out long rank);
        bool TryGetKeyByRank(long rank, out KeyType key);

        void Add(KeyType key);
        void Remove(KeyType key);
        KeyType GetKey(KeyType key);
        void SetKey(KeyType key);
        void Get(KeyType key, out KeyType keyOut, out long rank);
        KeyType GetKeyByRank(long rank);

        void AdjustCount(KeyType key, long countAdjust);

        bool Least(out KeyType leastOut);
        bool Greatest(out KeyType greatestOut);

        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestLess(KeyType key, out KeyType nearestKey);
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }


    //
    // IRange2Map, IRange2List, IRange2MapLong, IRange2ListLong
    //

    public enum Side { X = 0, Y = 1 };

    //[Feature(Feature.Range2)]
    public interface IRange2Map<ValueType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(int start, Side side);

        bool TryInsert(int start, Side side, int xLength, int yLength, ValueType value);
        bool TryDelete(int start, Side side);
        bool TryGetLength(int start, Side side, out int length);
        bool TrySetLength(int start, Side side, int length);
        bool TryGetValue(int start, Side side, out ValueType value);
        bool TrySetValue(int start, Side side, ValueType value);
        bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value);

        void Insert(int start, Side side, int xLength, int yLength, ValueType value);
        void Delete(int start, Side side);
        int GetLength(int start, Side side);
        void SetLength(int start, Side side, int length);
        ValueType GetValue(int start, Side side);
        void SetValue(int start, Side side, ValueType value);
        void Get(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value);

        int GetExtent(Side side);

        bool NearestLessOrEqual(int position, Side side, out int nearestStart);
        bool NearestLess(int position, Side side, out int nearestStart);
        bool NearestGreaterOrEqual(int position, Side side, out int nearestStart);
        bool NearestGreater(int position, Side side, out int nearestStart);
    }

    //[Feature(Feature.Range2)]
    public interface IRange2List
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(int start, Side side);

        bool TryInsert(int start, Side side, int xLength, int yLength);
        bool TryDelete(int start, Side side);
        bool TryGetLength(int start, Side side, out int length);
        bool TrySetLength(int start, Side side, int length);
        bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength);

        void Insert(int start, Side side, int xLength, int yLength);
        void Delete(int start, Side side);
        int GetLength(int start, Side side);
        void SetLength(int start, Side side, int length);
        void Get(int start, Side side, out int otherStart, out int xLength, out int yLength);

        int GetExtent(Side side);

        bool NearestLessOrEqual(int position, Side side, out int nearestStart);
        bool NearestLess(int position, Side side, out int nearestStart);
        bool NearestGreaterOrEqual(int position, Side side, out int nearestStart);
        bool NearestGreater(int position, Side side, out int nearestStart);
    }

    //[Feature(Feature.Range2)]
    public interface IRange2MapLong<ValueType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(long start, Side side);

        bool TryInsert(long start, Side side, long xLength, long yLength, ValueType value);
        bool TryDelete(long start, Side side);
        bool TryGetLength(long start, Side side, out long length);
        bool TrySetLength(long start, Side side, long length);
        bool TryGetValue(long start, Side side, out ValueType value);
        bool TrySetValue(long start, Side side, ValueType value);
        bool TryGet(long start, Side side, out long otherStart, out long xLength, out long yLength, out ValueType value);

        void Insert(long start, Side side, long xLength, long yLength, ValueType value);
        void Delete(long start, Side side);
        long GetLength(long start, Side side);
        void SetLength(long start, Side side, long length);
        ValueType GetValue(long start, Side side);
        void SetValue(long start, Side side, ValueType value);
        void Get(long start, Side side, out long otherStart, out long xLength, out long yLength, out ValueType value);

        long GetExtent(Side side);

        bool NearestLessOrEqual(long position, Side side, out long nearestStart);
        bool NearestLess(long position, Side side, out long nearestStart);
        bool NearestGreaterOrEqual(long position, Side side, out long nearestStart);
        bool NearestGreater(long position, Side side, out long nearestStart);
    }

    //[Feature(Feature.Range2)]
    public interface IRange2ListLong
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(long start, Side side);

        bool TryInsert(long start, Side side, long xLength, long yLength);
        bool TryDelete(long start, Side side);
        bool TryGetLength(long start, Side side, out long length);
        bool TrySetLength(long start, Side side, long length);
        bool TryGet(long start, Side side, out long otherStart, out long xLength, out long yLength);

        void Insert(long start, Side side, long xLength, long yLength);
        void Delete(long start, Side side);
        long GetLength(long start, Side side);
        void SetLength(long start, Side side, long length);
        void Get(long start, Side side, out long otherStart, out long xLength, out long yLength);

        long GetExtent(Side side);

        bool NearestLessOrEqual(long position, Side side, out long nearestStart);
        bool NearestLess(long position, Side side, out long nearestStart);
        bool NearestGreaterOrEqual(long position, Side side, out long nearestStart);
        bool NearestGreater(long position, Side side, out long nearestStart);
    }


    //
    // IRangeMap, IRangeList, IRangeMapLong, IRangeListLong
    //

    //[Feature(Feature.Range)]
    public interface IRangeMap<ValueType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(int start);

        bool TryInsert(int start, int length, ValueType value);
        bool TryDelete(int start);
        bool TryGetLength(int start, out int length);
        bool TrySetLength(int start, int length);
        bool TryGetValue(int start, out ValueType value);
        bool TrySetValue(int start, ValueType value);
        bool TryGet(int start, out int length, out ValueType value);

        void Insert(int start, int length, ValueType value);
        void Delete(int start);
        int GetLength(int start);
        void SetLength(int start, int length);
        ValueType GetValue(int start);
        void SetValue(int start, ValueType value);
        void Get(int start, out int length, out ValueType value);

        int GetExtent();

        bool NearestLessOrEqual(int position, out int nearestStart);
        bool NearestLess(int position, out int nearestStart);
        bool NearestGreaterOrEqual(int position, out int nearestStart);
        bool NearestGreater(int position, out int nearestStart);
    }

    //[Feature(Feature.Range)]
    public interface IRangeList
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(int start);

        bool TryInsert(int start, int length);
        bool TryDelete(int start);
        bool TryGetLength(int start, out int length);
        bool TrySetLength(int start, int length);

        void Insert(int start, int length);
        void Delete(int start);
        int GetLength(int start);
        void SetLength(int start, int length);

        int GetExtent();

        bool NearestLessOrEqual(int position, out int nearestStart);
        bool NearestLess(int position, out int nearestStart);
        bool NearestGreaterOrEqual(int position, out int nearestStart);
        bool NearestGreater(int position, out int nearestStart);
    }

    //[Feature(Feature.Range)]
    public interface IRangeMapLong<ValueType>
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(long start);

        bool TryInsert(long start, long length, ValueType value);
        bool TryDelete(long start);
        bool TryGetLength(long start, out long length);
        bool TrySetLength(long start, long length);
        bool TryGetValue(long start, out ValueType value);
        bool TrySetValue(long start, ValueType value);
        bool TryGet(long start, out long length, out ValueType value);

        void Insert(long start, long length, ValueType value);
        void Delete(long start);
        long GetLength(long start);
        void SetLength(long start, long length);
        ValueType GetValue(long start);
        void SetValue(long start, ValueType value);
        void Get(long start, out long length, out ValueType value);

        long GetExtent();

        bool NearestLessOrEqual(long position, out long nearestStart);
        bool NearestLess(long position, out long nearestStart);
        bool NearestGreaterOrEqual(long position, out long nearestStart);
        bool NearestGreater(long position, out long nearestStart);
    }

    //[Feature(Feature.Range)]
    public interface IRangeListLong
    {
        uint Count { get; }
        long LongCount { get; }

        void Clear();

        bool Contains(long start);

        bool TryInsert(long start, long length);
        bool TryDelete(long start);
        bool TryGetLength(long start, out long length);
        bool TrySetLength(long start, long length);

        void Insert(long start, long length);
        void Delete(long start);
        long GetLength(long start);
        void SetLength(long start, long length);

        long GetExtent();

        bool NearestLessOrEqual(long position, out long nearestStart);
        bool NearestLess(long position, out long nearestStart);
        bool NearestGreaterOrEqual(long position, out long nearestStart);
        bool NearestGreater(long position, out long nearestStart);
    }
}
