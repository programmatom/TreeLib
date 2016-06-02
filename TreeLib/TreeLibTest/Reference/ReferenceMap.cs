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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    // This is a primary implementation

    public class ReferenceMap<KeyType, ValueType> :
        IOrderedMap<KeyType, ValueType>,
        INonInvasiveTreeInspection,
        ISimpleTreeInspection<KeyType, ValueType>
        where KeyType : IComparable<KeyType>
    {
        private readonly List<KeyValuePair<KeyType, ValueType>> items = new List<KeyValuePair<KeyType, ValueType>>();


        //
        // Construction
        //

        public ReferenceMap()
        {
        }


        //
        // IOrderedMap
        //

        // Query & Mutation

        public uint Count { get { return unchecked((uint)items.Count); } }

        public long LongCount { get { return items.Count; } }

        public void Clear()
        {
            items.Clear();
        }

        public bool ContainsKey(KeyType key)
        {
            return BinarySearch(key) >= 0;
        }

        public bool SetOrAddValue(KeyType key, ValueType value)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                items[i] = new KeyValuePair<KeyType, ValueType>(key, value);
                return false;
            }
            items.Insert(~i, new KeyValuePair<KeyType, ValueType>(key, value));
            return true;
        }

        public bool TryAdd(KeyType key, ValueType value)
        {
            int i = BinarySearch(key);
            if (i < 0)
            {
                items.Insert(~i, new KeyValuePair<KeyType, ValueType>(key, value));
                return true;
            }
            return false;
        }

        public bool TryRemove(KeyType key)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                items.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool TryGetValue(KeyType key, out ValueType value)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                value = items[i].Value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        public bool TrySetValue(KeyType key, ValueType value)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                items[i] = new KeyValuePair<KeyType, ValueType>(key, value);
                return true;
            }
            return false;
        }

        public void Add(KeyType key, ValueType value)
        {
            if (!TryAdd(key, value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        public void Remove(KeyType key)
        {
            if (!TryRemove(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        public ValueType GetValue(KeyType key)
        {
            ValueType value;
            if (!TryGetValue(key, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        public void SetValue(KeyType key, ValueType value)
        {
            if (!TrySetValue(key, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        // Ordered Query

        public bool Least(out KeyType leastOut)
        {
            if (items.Count != 0)
            {
                leastOut = items[0].Key;
                return true;
            }
            leastOut = default(KeyType);
            return false;
        }

        public bool Greatest(out KeyType greatestOut)
        {
            if (items.Count != 0)
            {
                greatestOut = items[items.Count - 1].Key;
                return true;
            }
            greatestOut = default(KeyType);
            return false;
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                nearestKey = items[i].Key;
                return true;
            }
            i = ~i;
            if (i > 0)
            {
                nearestKey = items[i - 1].Key;
                return true;
            }
            nearestKey = default(KeyType);
            return false;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            int i = BinarySearch(key);
            if (i < 0)
            {
                i = ~i;
            }
            if (i > 0)
            {
                nearestKey = items[i - 1].Key;
                return true;
            }
            nearestKey = default(KeyType);
            return false;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                nearestKey = items[i].Key;
                return true;
            }
            i = ~i;
            if (i < items.Count)
            {
                nearestKey = items[i].Key;
                return true;
            }
            nearestKey = default(KeyType);
            return false;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                i++;
            }
            else
            {
                i = ~i;
            }
            if (i < items.Count)
            {
                nearestKey = items[i].Key;
                return true;
            }
            nearestKey = default(KeyType);
            return false;
        }


        //
        // Internals
        //

        private int BinarySearch(KeyType key)
        {
            return items.BinarySearch(new KeyValuePair<KeyType, ValueType>(key, default(ValueType)), Comparer);
        }

        public readonly static CompoundComparer Comparer = new CompoundComparer();
        public class CompoundComparer : IComparer<KeyValuePair<KeyType, ValueType>>
        {
            public int Compare(KeyValuePair<KeyType, ValueType> x, KeyValuePair<KeyType, ValueType> y)
            {
                return Comparer<KeyType>.Default.Compare(x.Key, y.Key);
            }
        }


        //
        // ISimpleTreeInspection
        //

        int ISimpleTreeInspection<KeyType, ValueType>.Count { get { return items.Count; } }

        KeyValuePair<KeyType, ValueType>[] ISimpleTreeInspection<KeyType, ValueType>.ToArray()
        {
            return items.ToArray();
        }

        void ISimpleTreeInspection<KeyType, ValueType>.Validate()
        {
            for (int i = 1; i < items.Count; i++)
            {
                if (!(Comparer<KeyType>.Default.Compare(items[i - 1].Key, items[i].Key) < 0))
                {
                    throw new InvalidOperationException("ordering invariant violated");
                }
            }
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
        }
    }
}
