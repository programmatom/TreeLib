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
    public struct EntryMap<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType>
    {
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly KeyType key;

        /// <summary>
        /// Returns the key associated with a key-value pair mapping, or the key associated with a key-only collection.
        /// </summary>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public KeyType Key { get { return key; } }


        [Payload(Payload.Value)]
        private ValueType value;

        /// <summary>
        /// Returns the value associated with a key-value pair mapping.
        /// </summary>
        [Payload(Payload.Value)]
        public ValueType Value { get { return value; } }


        public EntryMap(            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Payload(Payload.Value)] ValueType value)
        {
            this.key = key;
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            EntryMap</*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ KeyType, /*[Payload(Payload.Value)]*/ ValueType> other
                = (EntryMap</*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ KeyType, /*[Payload(Payload.Value)]*/ ValueType>)obj;

            int keyOrder = 0;
            keyOrder = Comparer<KeyType>.Default.Compare(this.key, other.key);
            if (keyOrder != 0)
            {
                return false;
            }

            int valueOrder = 0;
            valueOrder = Comparer<ValueType>.Default.Compare(this.value, other.value);
            if (valueOrder != 0)
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
            try
            {
                // value may be a reference type
                hashCode = unchecked(hashCode + this.value.GetHashCode());
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
            fields.Add(Convert.ToString(value));
            return String.Join(", ", fields.ToArray());
        }
    }
}
