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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    // This is a primary implementation

    public class ReferenceMap<KeyType, ValueType> :
        IOrderedMap<KeyType, ValueType>,
        INonInvasiveTreeInspection,
        ISimpleTreeInspection<KeyType, ValueType>,
        IEnumerable<EntryMap<KeyType, ValueType>>
        where KeyType : IComparable<KeyType>
    {
        private List<EntryMap<KeyType, ValueType>> items = new List<EntryMap<KeyType, ValueType>>();
        private uint version;


        //
        // Construction
        //

        public ReferenceMap()
        {
        }

        public ReferenceMap(ReferenceMap<KeyType, ValueType> original)
        {
            items.AddRange(original.items);
        }

        public ReferenceMap<KeyType, ValueType> Clone()
        {
            return new ReferenceMap<KeyType, ValueType>(this);
        }


        //
        // IOrderedMap
        //

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
                items[i] = new EntryMap<KeyType, ValueType>(key, value, null, 0);
                return false;
            }
            items.Insert(~i, new EntryMap<KeyType, ValueType>(key, value, null, 0));
            this.version = unchecked(this.version + 1);
            return true;
        }

        public bool TryAdd(KeyType key, ValueType value)
        {
            int i = BinarySearch(key);
            if (i < 0)
            {
                items.Insert(~i, new EntryMap<KeyType, ValueType>(key, value, null, 0));
                this.version = unchecked(this.version + 1);
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
                this.version = unchecked(this.version + 1);
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
                items[i] = new EntryMap<KeyType, ValueType>(key, value, null, 0);
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

        public void ConditionalSetOrAdd(KeyType key, UpdatePredicate<KeyType, ValueType> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException();
            }

            ValueType value;
            bool resident = TryGetValue(key, out value);

            uint version = this.version;
            List<EntryMap<KeyType, ValueType>> savedItems = items;
            items = new List<EntryMap<KeyType, ValueType>>();

            bool add = predicate(key, ref value, resident);

            items = savedItems;
            if (version != this.version)
            {
                throw new InvalidOperationException();
            }

            if (resident)
            {
                bool f = TrySetValue(key, value);
                Debug.Assert(f);
            }
            else if (add)
            {
                bool f = TryAdd(key, value);
                Debug.Assert(f);
            }
        }

        public void ConditionalSetOrRemove(KeyType key, UpdatePredicate<KeyType, ValueType> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException();
            }

            ValueType value;
            bool resident = TryGetValue(key, out value);

            uint version = this.version;
            List<EntryMap<KeyType, ValueType>> savedItems = items;
            items = new List<EntryMap<KeyType, ValueType>>();

            bool remove = predicate(key, ref value, resident);

            items = savedItems;
            if (version != this.version)
            {
                throw new InvalidOperationException();
            }

            if (resident)
            {
                if (remove)
                {
                    bool f = TryRemove(key);
                    Debug.Assert(f);
                }
                else
                {
                    bool f = TrySetValue(key, value);
                    Debug.Assert(f);
                }
            }
        }

        public bool Least(out KeyType leastOut, out ValueType valueOut)
        {
            if (items.Count != 0)
            {
                leastOut = items[0].Key;
                valueOut = items[0].Value;
                return true;
            }
            leastOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool Least(out KeyType leastOut)
        {
            ValueType value;
            return Least(out leastOut, out value);
        }

        public bool Greatest(out KeyType greatestOut, out ValueType valueOut)
        {
            if (items.Count != 0)
            {
                greatestOut = items[items.Count - 1].Key;
                valueOut = items[items.Count - 1].Value;
                return true;
            }
            greatestOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool Greatest(out KeyType greatestOut)
        {
            ValueType value;
            return Greatest(out greatestOut, out value);
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                nearestKey = items[i].Key;
                valueOut = items[i].Value;
                return true;
            }
            i = ~i;
            if (i > 0)
            {
                nearestKey = items[i - 1].Key;
                valueOut = items[i - 1].Value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLessOrEqual(key, out nearestKey, out value);
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            int i = BinarySearch(key);
            if (i < 0)
            {
                i = ~i;
            }
            if (i > 0)
            {
                nearestKey = items[i - 1].Key;
                valueOut = items[i - 1].Value;
                return true;
            }
            valueOut = default(ValueType);
            nearestKey = default(KeyType);
            return false;
        }

        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLess(key, out nearestKey, out value);
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType valueOut)
        {
            int i = BinarySearch(key);
            if (i >= 0)
            {
                nearestKey = items[i].Key;
                valueOut = items[i].Value;
                return true;
            }
            i = ~i;
            if (i < items.Count)
            {
                nearestKey = items[i].Key;
                valueOut = items[i].Value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreaterOrEqual(key, out nearestKey, out value);
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType valueOut)
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
                valueOut = items[i].Value;
                return true;
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreater(key, out nearestKey, out value);
        }


        //
        // Internals
        //

        private int BinarySearch(KeyType key)
        {
            return items.BinarySearch(new EntryMap<KeyType, ValueType>(key, default(ValueType), null, 0), Comparer);
        }

        public readonly static CompoundComparer Comparer = new CompoundComparer();
        public class CompoundComparer : IComparer<EntryMap<KeyType, ValueType>>
        {
            public int Compare(EntryMap<KeyType, ValueType> x, EntryMap<KeyType, ValueType> y)
            {
                return Comparer<KeyType>.Default.Compare(x.Key, y.Key);
            }
        }


        //
        // ISimpleTreeInspection
        //

        int ISimpleTreeInspection<KeyType, ValueType>.Count { get { return items.Count; } }

        public EntryMap<KeyType, ValueType>[] ToArray()
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


        //
        // IEnumerable
        //

        private class Enumerator : IEnumerator<EntryMap<KeyType, ValueType>>, ISetValue<ValueType>
        {
            private readonly ReferenceMap<KeyType, ValueType> map;
            private readonly bool forward;
            private readonly bool robust;
            private readonly bool startKeyed;
            private readonly KeyType startKey;

            private int index;
            private EntryMap<KeyType, ValueType> current;
            private uint mapVersion;
            private uint enumeratorVersion;

            public Enumerator(ReferenceMap<KeyType, ValueType> map, bool forward, bool robust, bool startKeyed, KeyType startKey)
            {
                this.map = map;
                this.forward = forward;
                this.robust = robust;
                this.startKeyed = startKeyed;
                this.startKey = startKey;

                Reset();
            }

            public EntryMap<KeyType, ValueType> Current { get { return current; } }

            object IEnumerator.Current { get { return this.Current; } }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (!robust && (mapVersion != map.version))
                {
                    throw new InvalidOperationException();
                }

                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);

                if (((index >= 0) && (index < map.Count)) && (0 != Comparer<KeyType>.Default.Compare(current.Key, map.items[index].Key)))
                {
                    if (forward)
                    {
                        index = 0;
                        while ((index < map.items.Count) && (0 <= Comparer<KeyType>.Default.Compare(current.Key, map.items[index].Key)))
                        {
                            index++;
                        }
                        index--;
                    }
                    else
                    {
                        index = map.items.Count - 1;
                        while ((index >= 0) && (0 >= Comparer<KeyType>.Default.Compare(current.Key, map.items[index].Key)))
                        {
                            index--;
                        }
                        index++;
                    }
                }

                index = index + (forward ? 1 : -1);
                current = new EntryMap<KeyType, ValueType>();
                if ((index >= 0) && (index < map.Count))
                {
                    current = new EntryMap<KeyType, ValueType>(
                        map.items[index].Key,
                        map.items[index].Value,
                        this,
                        this.enumeratorVersion);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                this.mapVersion = map.version;
                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);

                current = new EntryMap<KeyType, ValueType>();

                if (forward)
                {
                    index = -1;
                    if (startKeyed)
                    {
                        while ((index + 1 < map.items.Count) && (0 < Comparer<KeyType>.Default.Compare(startKey, map.items[index + 1].Key)))
                        {
                            index++;
                        }
                        if ((index >= 0) && (index < map.items.Count) && (0 == Comparer<KeyType>.Default.Compare(startKey, map.items[index].Key)))
                        {
                            index--;
                        }
                    }
                }
                else
                {
                    index = map.items.Count;
                    if (startKeyed)
                    {
                        while ((index - 1 >= 0) && (0 > Comparer<KeyType>.Default.Compare(startKey, map.items[index - 1].Key)))
                        {
                            index--;
                        }
                        if ((index >= 0) && (index < map.items.Count) && (0 == Comparer<KeyType>.Default.Compare(startKey, map.items[index].Key)))
                        {
                            index++;
                        }
                    }
                }

                if ((index >= 0) && (index < map.items.Count))
                {
                    current = new EntryMap<KeyType, ValueType>(map.items[index].Key, map.items[index].Value, this, this.mapVersion);
                }
            }

            public void SetValue(ValueType value, uint expectedEnumeratorVersion)
            {
                if ((!robust && (this.mapVersion != map.version)) || (this.enumeratorVersion != expectedEnumeratorVersion))
                {
                    throw new InvalidOperationException();
                }

                map.SetValue(current.Key, value);
            }
        }

        public class EnumerableSurrogate : IEnumerable<EntryMap<KeyType, ValueType>>
        {
            private readonly ReferenceMap<KeyType, ValueType> map;
            private readonly bool forward;
            private readonly bool robust;
            private readonly bool startKeyed;
            private readonly KeyType startKey;

            public EnumerableSurrogate(ReferenceMap<KeyType, ValueType> map, bool forward, bool robust, bool startKeyed, KeyType startKey)
            {
                this.map = map;
                this.forward = forward;
                this.robust = robust;
                this.startKeyed = startKeyed;
                this.startKey = startKey;
            }

            public IEnumerator<EntryMap<KeyType, ValueType>> GetEnumerator()
            {
                return new Enumerator(map, forward, robust, startKeyed, startKey);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public IEnumerator<EntryMap<KeyType, ValueType>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetFastEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetFastEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new EnumerableSurrogate(this, true/*forward*/, true/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetRobustEnumerable(bool forward)
        {
            return new EnumerableSurrogate(this, forward, true/*robust*/, false/*startKeyed*/, default(KeyType));
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetEnumerable(KeyType startAt)
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt)
        {
            return new EnumerableSurrogate(this, true/*forward*/, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new EnumerableSurrogate(this, forward, false/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt)
        {
            return new EnumerableSurrogate(this, true/*forward*/, true/*robust*/, true/*startKeyed*/, startAt);
        }

        public IEnumerable<EntryMap<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new EnumerableSurrogate(this, forward, true/*robust*/, true/*startKeyed*/, startAt);
        }
    }
}
