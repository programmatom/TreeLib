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

namespace TreeLib.Internal
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveTreeInspection
    {
        uint Count { get; }

        object Root { get; }
        object GetLeftChild(object node);
        object GetRightChild(object node);

        object GetKey(object node);
        object GetValue(object node);
        object GetMetadata(object node);

        void Validate();
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct Range
    {
        public int start;
        public int length;

        public Range(int start, int length)
        {
            this.start = start;
            this.length = length;
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})", start, length);
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct RangeLong
    {
        public long start;
        public long length;

        public RangeLong(long start, long length)
        {
            this.start = start;
            this.length = length;
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})", start, length);
        }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct Range2MapEntry
    {
        public Range x;
        public Range y;
        public object value;

        public Range2MapEntry(Range x, Range y)
        {
            this.x = x;
            this.y = y;
            this.value = null;
        }

        public Range2MapEntry(Range x, Range y, object value)
        {
            this.x = x;
            this.y = y;
            this.value = value;
        }

        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", x, y, value);
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct Range2MapEntryLong
    {
        public RangeLong x;
        public RangeLong y;
        public object value;

        public Range2MapEntryLong(RangeLong x, RangeLong y)
        {
            this.x = x;
            this.y = y;
            this.value = null;
        }

        public Range2MapEntryLong(RangeLong x, RangeLong y, object value)
        {
            this.x = x;
            this.y = y;
            this.value = value;
        }

        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", x, y, value);
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveRange2MapInspection
    {
        uint Count { get; }

        Range2MapEntry[] GetRanges();

        void Validate();
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveRange2MapInspectionLong
    {
        uint Count { get; }

        Range2MapEntryLong[] GetRanges();

        void Validate();
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct MultiRankMapEntry
    {
        public object key;
        public object value;
        public Range rank;

        public MultiRankMapEntry(object key, Range rank)
        {
            this.key = key;
            this.value = null;
            this.rank = rank;
        }

        public MultiRankMapEntry(object key, Range rank, object value)
        {
            this.key = key;
            this.value = value;
            this.rank = rank;
        }

        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", key, rank, value);
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct MultiRankMapEntryLong
    {
        public object key;
        public object value;
        public RangeLong rank;

        public MultiRankMapEntryLong(object key, RangeLong rank)
        {
            this.key = key;
            this.value = null;
            this.rank = rank;
        }

        public MultiRankMapEntryLong(object key, RangeLong rank, object value)
        {
            this.key = key;
            this.value = value;
            this.rank = rank;
        }

        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", key, rank, value);
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveMultiRankMapInspection
    {
        uint Count { get; }

        MultiRankMapEntry[] GetRanks();

        void Validate();
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveMultiRankMapInspectionLong
    {
        uint Count { get; }

        MultiRankMapEntryLong[] GetRanks();

        void Validate();
    }
}
