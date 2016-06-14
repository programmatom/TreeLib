// NOTE: This file is auto-generated. DO NOT MAKE CHANGES HERE! They will be overwritten on rebuild.

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
using System.Collections.Generic;

using TreeLib.Internal;

#pragma warning disable CS1591

namespace TreeLib
{
    /// <summary>
    /// A type defining the struct returned for each item in a tree by an enumerator. The struct contains properties
    /// for all relevant per-item data, including one or more of key, value, rank/count, and/or range start/length, as
    /// appropriate for the type of collection.
    /// </summary>
    public struct EntryRange2Map<[Payload(Payload.Value)] ValueType>
    {


        [Payload(Payload.Value)]
        private ValueType value;

        /// <summary>
        /// Returns the value associated with a key-value pair mapping.
        /// </summary>
        [Payload(Payload.Value)]
        public ValueType Value { get { return value; } }


        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xStart;

        /// <summary>
        /// Returns the rank of an item in a rank collection, or the start of a range in a range collection
        /// (for range-to-range mapping, returns the X side start)
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int XStart { get { return xStart; } }


        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xLength;

        /// <summary>
        /// Returns the count of an item in a multi-rank collection, or the length of a range in a range collection
        /// (for range-to-range mapping, returns the X side length)
        /// </summary>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int XLength { get { return xLength; } }


        [Feature(Feature.Range2)]
        [Widen]
        private int yStart;

        /// <summary>
        /// Returns the Y side start of a range in a range-to-range mapping collection
        /// </summary>
        [Feature(Feature.Range2)]
        [Widen]
        public int YStart { get { return yStart; } }


        [Feature(Feature.Range2)]
        [Widen]
        private int yLength;

        /// <summary>
        /// Returns the Y side length of a range in a range-to-range mapping collection
        /// </summary>
        [Feature(Feature.Range2)]
        [Widen]
        public int YLength { get { return yLength; } }


        public EntryRange2Map(            [Payload(Payload.Value)] ValueType value,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xStart,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,            [Feature(Feature.Range2)][Widen] int yStart,            [Feature(Feature.Range2)][Widen] int yLength)
        {
            this.value = value;
            this.xStart = xStart;
            this.xLength = xLength;
            this.yStart = yStart;
            this.yLength = yLength;
        }

        public override bool Equals(object obj)
        {
            EntryRange2Map</*[Payload(Payload.Value)]*/ValueType> other
                = (EntryRange2Map</*[Payload(Payload.Value)]*/ValueType>)obj;

            int valueOrder = 0;
            valueOrder = Comparer<ValueType>.Default.Compare(this.value, other.value);
            if (valueOrder != 0)
            {
                return false;
            }

            bool xStartEqual = true;
            xStartEqual = this.xStart == other.xStart;
            if (!xStartEqual)
            {
                return false;
            }

            bool xLengthEqual = true;
            xLengthEqual = this.xLength == other.xLength;
            if (!xLengthEqual)
            {
                return false;
            }

            bool yStartEqual = true;
            yStartEqual = this.yStart == other.yStart;
            if (!yStartEqual)
            {
                return false;
            }

            bool yLengthEqual = true;
            yLengthEqual = this.yLength == other.yLength;
            if (!yLengthEqual)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            try
            {
                // value may be a reference type
                hashCode = unchecked(hashCode + this.value.GetHashCode());
            }
            catch (NullReferenceException)
            {
            }
            hashCode = unchecked(hashCode + this.xStart.GetHashCode());
            hashCode = unchecked(hashCode + this.xLength.GetHashCode());
            hashCode = unchecked(hashCode + this.yStart.GetHashCode());
            hashCode = unchecked(hashCode + this.yLength.GetHashCode());
            return hashCode;
        }

        public override string ToString()
        {
            List<string> fields = new List<string>();
            fields.Add(Convert.ToString(value));
            fields.Add(Convert.ToString(xStart));
            fields.Add(Convert.ToString(xLength));
            fields.Add(Convert.ToString(yStart));
            fields.Add(Convert.ToString(yLength));
            return String.Join(", ", fields.ToArray());
        }
    }
}
