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
    public struct EntryRange2Map<[Payload(Payload.Value)] ValueType> :
        /*[Payload(Payload.Value)]*/IGetEnumeratorSetValueInfo<ValueType>
    {


        [Payload(Payload.Value)]
        private readonly ValueType value;

        /// <summary>
        /// Returns the value associated with a key-value pair mapping.
        /// </summary>
        [Payload(Payload.Value)]
        public ValueType Value { get { return value; } }


        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private readonly int xStart;

        /// <summary>
        /// Returns the rank of an item in a rank collection, or the start of a range in a range collection
        /// (for range-to-range mapping, returns the X side start)
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int XStart { get { return xStart; } }


        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private readonly int xLength;

        /// <summary>
        /// Returns the count of an item in a multi-rank collection, or the length of a range in a range collection
        /// (for range-to-range mapping, returns the X side length)
        /// </summary>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int XLength { get { return xLength; } }


        [Feature(Feature.Range2)]
        [Widen]
        private readonly int yStart;

        /// <summary>
        /// Returns the Y side start of a range in a range-to-range mapping collection
        /// </summary>
        [Feature(Feature.Range2)]
        [Widen]
        public int YStart { get { return yStart; } }


        [Feature(Feature.Range2)]
        [Widen]
        private readonly int yLength;

        /// <summary>
        /// Returns the Y side length of a range in a range-to-range mapping collection
        /// </summary>
        [Feature(Feature.Range2)]
        [Widen]
        public int YLength { get { return yLength; } }


        [Payload(Payload.Value)]
        private readonly ISetValue<ValueType> enumerator;
        [Payload(Payload.Value)]
        private readonly uint version;

        [Payload(Payload.Value)]
        public void SetValue(ValueType value)
        {
            if (enumerator == null)
            {
                throw new InvalidOperationException();
            }

            enumerator.SetValue(value, version);
        }

        [Payload(Payload.Value)]
        uint IGetEnumeratorSetValueInfo<ValueType>.Version { get { return version; } }

        [Payload(Payload.Value)]
        ISetValue<ValueType> IGetEnumeratorSetValueInfo<ValueType>.SetValueCallack { get { return enumerator; } }


        public EntryRange2Map(            [Payload(Payload.Value)] ValueType value,            [Payload(Payload.Value)] ISetValue<ValueType> enumerator,            [Payload(Payload.Value)] uint version,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xStart,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,            [Feature(Feature.Range2)][Widen] int yStart,            [Feature(Feature.Range2)][Widen] int yLength)
        {
            this.value = value;
            this.xStart = xStart;
            this.xLength = xLength;
            this.yStart = yStart;
            this.yLength = yLength;

            this.enumerator = enumerator;
            this.version = version;
        }

        [Payload(Payload.Value)]
        public EntryRange2Map(            [Payload(Payload.Value)] ValueType value,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xStart,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,            [Feature(Feature.Range2)][Widen] int yStart,            [Feature(Feature.Range2)][Widen] int yLength)
        {
            this.value = value;
            this.xStart = xStart;
            this.xLength = xLength;
            this.yStart = yStart;
            this.yLength = yLength;

            this.enumerator = null;
            this.version = 0;
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
            // need a reasonable initial value
            int hashCode = 0L.GetHashCode();
            // implementation derived from Roslyn compiler implementation for anonymous types:
            // Microsoft.CodeAnalysis.CSharp.Symbols.AnonymousTypeManager.AnonymousTypeGetHashCodeMethodSymbol 
            const int HASH_FACTOR = -1521134295;
            unchecked
            {
                hashCode = hashCode * HASH_FACTOR + EqualityComparer<ValueType>.Default.GetHashCode(this.value);
                hashCode = hashCode * HASH_FACTOR + this.xStart.GetHashCode();
                hashCode = hashCode * HASH_FACTOR + this.xLength.GetHashCode();
                hashCode = hashCode * HASH_FACTOR + this.yStart.GetHashCode();
                hashCode = hashCode * HASH_FACTOR + this.yLength.GetHashCode();
            }
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
