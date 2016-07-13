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
    public struct EntryRankListLong<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType>     {
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly KeyType key;

        /// <summary>
        /// Returns the key associated with a key-value pair mapping, or the key associated with a key-only collection.
        /// </summary>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public KeyType Key { get { return key; } }


        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private readonly long xStart;

        /// <summary>
        /// Returns the rank of an item in a rank collection, or the start of a range in a range collection
        /// (for range-to-range mapping, returns the X side start)
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public long Rank { get { return xStart; } }


        public EntryRankListLong(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xStart)
        {
            this.key = key;
            this.xStart = xStart;
        }

        public override bool Equals(object obj)
        {
            EntryRankListLong</*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/KeyType> other
                = (EntryRankListLong</*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/KeyType>)obj;

            int keyOrder = 0;
            keyOrder = Comparer<KeyType>.Default.Compare(this.key, other.key);
            if (keyOrder != 0)
            {
                return false;
            }

            bool xStartEqual = true;
            xStartEqual = this.xStart == other.xStart;
            if (!xStartEqual)
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
                hashCode = hashCode * HASH_FACTOR + EqualityComparer<KeyType>.Default.GetHashCode(this.key);
                hashCode = hashCode * HASH_FACTOR + this.xStart.GetHashCode();
            }
            return hashCode;
        }


        public override string ToString()
        {
            List<string> fields = new List<string>();
            fields.Add(Convert.ToString(key));
            fields.Add(Convert.ToString(xStart));
            return String.Join(", ", fields.ToArray());
        }
    }
}
