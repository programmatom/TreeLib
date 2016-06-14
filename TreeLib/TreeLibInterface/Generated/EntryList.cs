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
    public struct EntryList<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType>
    {
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly KeyType key;

        /// <summary>
        /// Returns the key associated with a key-value pair mapping, or the key associated with a key-only collection.
        /// </summary>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public KeyType Key { get { return key; } }


        public EntryList(            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
        {
            this.key = key;
        }

        public override bool Equals(object obj)
        {
            EntryList</*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/KeyType> other
                = (EntryList</*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/KeyType>)obj;

            int keyOrder = 0;
            keyOrder = Comparer<KeyType>.Default.Compare(this.key, other.key);
            if (keyOrder != 0)
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
                // key may be a reference type
                hashCode = unchecked(hashCode + this.key.GetHashCode());
            }
            catch (NullReferenceException)
            {
            }
            return hashCode;
        }

        public override string ToString()
        {
            List<string> fields = new List<string>();
            fields.Add(Convert.ToString(key));
            return String.Join(", ", fields.ToArray());
        }
    }
}
