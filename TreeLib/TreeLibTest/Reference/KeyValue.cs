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

namespace TreeLibTest
{
    public struct KeyValue<KeyType, ValueType> : IComparable<KeyValue<KeyType, ValueType>> where KeyType : IComparable<KeyType>
    {
        public KeyType key;
        public ValueType value;

        public KeyValue(KeyType key)
        {
            this.key = key;
            this.value = default(ValueType);
        }

        public KeyValue(KeyType key, ValueType value)
        {
            this.key = key;
            this.value = value;
        }

        public int CompareTo(KeyValue<KeyType, ValueType> other)
        {
            return Comparer<KeyType>.Default.Compare(this.key, other.key);
        }

        public override string ToString()
        {
            return String.Format("<{0}, {1}>", key, value);
        }
    }
}
