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

#pragma warning disable CS1591 // silence warning about missing Xml documentation

namespace TreeLib.Internal
{
    /// <summary>
    /// INonInvasiveTreeInspection is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveTreeInspection
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        uint Count { get; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object Root { get; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object GetLeftChild(object node);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object GetRightChild(object node);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object GetKey(object node);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object GetValue(object node);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object GetMetadata(object node);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void Validate();
    }


    /// <summary>
    /// Range is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public struct Range
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int start;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int length;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range(int start, int length)
        {
            this.start = start;
            this.length = length;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString()
        {
            return String.Format("({0}, {1})", start, length);
        }
    }

    /// <summary>
    /// RangeLong is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public struct RangeLong
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public long start;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public long length;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public RangeLong(long start, long length)
        {
            this.start = start;
            this.length = length;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString()
        {
            return String.Format("({0}, {1})", start, length);
        }
    }


    /// <summary>
    /// Range2MapEntry is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public struct Range2MapEntry
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range x;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range y;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public object value;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range2MapEntry(Range x, Range y)
        {
            this.x = x;
            this.y = y;
            this.value = null;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range2MapEntry(Range x, Range y, object value)
        {
            this.x = x;
            this.y = y;
            this.value = value;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", x, y, value);
        }
    }

    /// <summary>
    /// Range2MapEntryLong is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public struct Range2MapEntryLong
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public RangeLong x;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public RangeLong y;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public object value;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range2MapEntryLong(RangeLong x, RangeLong y)
        {
            this.x = x;
            this.y = y;
            this.value = null;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range2MapEntryLong(RangeLong x, RangeLong y, object value)
        {
            this.x = x;
            this.y = y;
            this.value = value;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", x, y, value);
        }
    }

    /// <summary>
    /// INonInvasiveRange2MapInspection is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveRange2MapInspection
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        uint Count { get; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Range2MapEntry[] GetRanges();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void Validate();
    }

    /// <summary>
    /// INonInvasiveRange2MapInspectionLong is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveRange2MapInspectionLong
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        uint Count { get; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Range2MapEntryLong[] GetRanges();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void Validate();
    }


    /// <summary>
    /// MultiRankMapEntry is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public struct MultiRankMapEntry
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public object key;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public object value;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Range rank;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MultiRankMapEntry(object key, Range rank)
        {
            this.key = key;
            this.value = null;
            this.rank = rank;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MultiRankMapEntry(object key, Range rank, object value)
        {
            this.key = key;
            this.value = value;
            this.rank = rank;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", key, rank, value);
        }
    }

    /// <summary>
    /// MultiRankMapEntryLong is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public struct MultiRankMapEntryLong
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public object key;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public object value;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public RangeLong rank;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MultiRankMapEntryLong(object key, RangeLong rank)
        {
            this.key = key;
            this.value = null;
            this.rank = rank;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MultiRankMapEntryLong(object key, RangeLong rank, object value)
        {
            this.key = key;
            this.value = value;
            this.rank = rank;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString()
        {
            return String.Format("[{0},{1},{2}]", key, rank, value);
        }
    }

    /// <summary>
    /// INonInvasiveMultiRankMapInspection is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveMultiRankMapInspection
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        uint Count { get; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        MultiRankMapEntry[] GetRanks();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void Validate();
    }

    /// <summary>
    /// INonInvasiveMultiRankMapInspectionLong is a diagnostic interface intended to be used ONLY for validation of trees
    /// during unit testing. It is not intended for consumption by users of the library and there is no
    /// guarrantee that it will be supported in future versions.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface INonInvasiveMultiRankMapInspectionLong
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        uint Count { get; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        MultiRankMapEntryLong[] GetRanks();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void Validate();
    }
}
