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

namespace TreeLib
{
    /// <summary>
    /// Specifies the manner in which nodes within the tree are allocated and freed.
    /// </summary>
    public enum AllocationMode
    {
        /// <summary>
        /// Nodes are allocated from the heap as need and discarded when removed from the tree. The garbage
        /// collector can reclaim the memory as soon as the node is removed.
        /// </summary>
        DynamicDiscard = 0,
        /// <summary>
        /// Nodes are allocated either from a free list of existing nodes, or from the heap as need. When removed from
        /// the tree, the storage for the node is returned to the free list.
        /// </summary>
        DynamicRetainFreelist,
        /// <summary>
        /// Nodes are allocated from a free list as needed and returned to the free list when removed from the tree.
        /// The free list is initialized with a fixed number of entries (specified to the constructor of the tree).
        /// An OutOfMemory exception will be thrown if there is an attempt to add more nodes to the tree than were
        /// initially allocated to the free list.
        /// </summary>
        PreallocatedFixed,
    }


    //
    // IOrderedMap, IOrderedList
    //

    /// <summary>
    /// Represents a ordered key-value mapping.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    /// <typeparam name="ValueType">Type of value associated with each entry.</typeparam>
    public interface IOrderedMap<KeyType, ValueType> : IEnumerable<EntryMap<KeyType, ValueType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of key-value pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue key-value pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of key-value pairs in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all key-value pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);
        /// <summary>
        /// Either set the value associated with a key (if the key is already present in the collection)
        /// or insert a new key-value pair into the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <returns>true if key was not present and key-value pair was added; false if key-value pair was already present and value was updated</returns>
        bool SetOrAddValue(KeyType key, ValueType value);

        /// <summary>
        /// Attempts to add a key-value pair to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <returns>true if the key-value pair was added; false if the key was already present</returns>
        bool TryAdd(KeyType key, ValueType value);
        /// <summary>
        /// Attempts to remove a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key-value pair was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <returns>true if they key was found</returns>
        bool TryGetValue(KeyType key, out ValueType value);
        /// <summary>
        /// Attempts to set the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <returns>true if the key-value pair was found and the value was updated</returns>
        bool TrySetValue(KeyType key, ValueType value);

        /// <summary>
        /// Adds a key-value pair to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        void Add(KeyType key, ValueType value);
        /// <summary>
        /// Removes a key-value pair from the collection.
        /// </summary>
        /// <param name="key">key of the key-value pair to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        ValueType GetValue(KeyType key);
        /// <summary>
        /// Updates the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void SetValue(KeyType key, ValueType value);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the lowest key-value pair in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value);
    }

    /// <summary>
    /// Represents an ordered key collection.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public interface IOrderedList<KeyType> : IEnumerable<EntryList<KeyType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of keys in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue keys.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of keys in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all keys from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <returns>true if the key was added; false if the key was already present</returns>
        bool TryAdd(KeyType key);
        /// <summary>
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <returns>true if they key was found</returns>
        bool TryGetKey(KeyType key, out KeyType keyOut);
        /// <summary>
        /// Attempts to update the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        bool TrySetKey(KeyType key);

        /// <summary>
        /// Adds a key to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        void Add(KeyType key);
        /// <summary>
        /// Removes a key from the collection.
        /// </summary>
        /// <param name="key">key to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKey(KeyType key);
        /// <summary>
        /// Updates the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        void SetKey(KeyType key);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);
    }


    //
    // IRankMap, IRankList, IRankMapLong, IRankListLong
    //

    /// <summary>
    /// Represents a ordered key-value mapping, augmented with rank information. The rank of a key-value pair is the index it would
    /// be located in if all the key-value pairs in the tree were placed into a sorted array.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    /// <typeparam name="ValueType">Type of value associated with each entry.</typeparam>
    public interface IRankMap<KeyType, ValueType> : IEnumerable<EntryRankMap<KeyType, ValueType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of key-value pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue key-value pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of key-value pairs in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all key-value pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key-value pair to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <returns>true if the key-value pair was added; false if the key was already present</returns>
        bool TryAdd(KeyType key, ValueType value);
        /// <summary>
        /// Attempts to remove a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key-value pair was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <returns>true if they key was found</returns>
        bool TryGetValue(KeyType key, out ValueType value);
        /// <summary>
        /// Attempts to set the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <returns>true if the key-value pair was found and the value was updated</returns>
        bool TrySetValue(KeyType key, ValueType value);
        /// <summary>
        /// Attempts to get the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out ValueType value, out int rank);
        /// <summary>
        /// Attempts to return the key of a key-value pair at the specified rank index.
        /// If all key-value pairs in the collection were converted to a sorted array, this would be the equivalent of array[rank].Key.
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <param name="key">the key located at that index</param>
        /// <returns>true if there is an element at the the specified index</returns>
        bool TryGetKeyByRank(int rank, out KeyType key);

        /// <summary>
        /// Adds a key-value pair to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        void Add(KeyType key, ValueType value);
        /// <summary>
        /// Removes a key-value pair from the collection.
        /// </summary>
        /// <param name="key">key of the key-value pair to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        ValueType GetValue(KeyType key);
        /// <summary>
        /// Updates the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void SetValue(KeyType key, ValueType value);
        /// <summary>
        /// Retrieves the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Get(KeyType key, out ValueType value, out int rank);
        /// <summary>
        /// Retrieves the key of a key-value pair at the specified rank index.
        /// If all key-value pairs in the collection were converted to a sorted array, this would be the equivalent of array[rank].Key.
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKeyByRank(int rank);

        /// <summary>
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// For a RankMap, the only valid values are 0 (which does nothing) and -1 (which removes the key-value pair).
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        void AdjustCount(KeyType key, int countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the lowest in the collection (in sort order) and the value associated with it.
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order) and the value associated with it.
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value and rank associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value and rank  associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out int rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value and rank  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value and rank  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out int rank);
    }

    /// <summary>
    /// Represents a ordered key collection, augmented with rank information. The rank of a key is the index it would
    /// be located in if all the keys in the tree were placed into a sorted array.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public interface IRankList<KeyType> : IEnumerable<EntryRankList<KeyType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of keys in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue keys.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of keys in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all keys from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <returns>true if the key was added; false if the key was already present</returns>
        bool TryAdd(KeyType key);
        /// <summary>
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <returns>true if they key was found</returns>
        bool TryGetKey(KeyType key, out KeyType keyOut);
        /// <summary>
        /// Attempts to update the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        bool TrySetKey(KeyType key);
        /// <summary>
        /// Attempts to get the actual key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out KeyType keyOut, out int rank);
        /// <summary>
        /// Retrieves the key at the specified rank index.
        /// If all keys in the collection were converted to a sorted array, this would be the equivalent of array[rank].
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <param name="key">an out paramter receiving the key at the specified rank index</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        bool TryGetKeyByRank(int rank, out KeyType key);

        /// <summary>
        /// Adds a key to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        void Add(KeyType key);
        /// <summary>
        /// Removes a key from the collection.
        /// </summary>
        /// <param name="key">key to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKey(KeyType key);
        /// <summary>
        /// Updates the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        void SetKey(KeyType key);
        /// <summary>
        /// Retrieves the actual key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        void Get(KeyType key, out KeyType keyOut, out int rank);
        /// <summary>
        /// Retrieves the key at the specified rank index.
        /// If all keys in the collection were converted to a sorted array, this would be the equivalent of array[rank].
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKeyByRank(int rank);

        /// <summary>
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// For a RankMap, the only valid values are 0 (which does nothing) and -1 (which removes the key-value pair).
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        void AdjustCount(KeyType key, int countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out int rank);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out int rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out int rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out int rank);
    }

    /// <summary>
    /// Represents a ordered key-value mapping, augmented with rank information. The rank of a key-value pair is the index it would
    /// be located in if all the key-value pairs in the tree were placed into a sorted array.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    /// <typeparam name="ValueType">Type of value associated with each entry.</typeparam>
    public interface IRankMapLong<KeyType, ValueType> : IEnumerable<EntryRankMapLong<KeyType, ValueType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of key-value pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue key-value pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of key-value pairs in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all key-value pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key-value pair to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <returns>true if the key-value pair was added; false if the key was already present</returns>
        bool TryAdd(KeyType key, ValueType value);
        /// <summary>
        /// Attempts to remove a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key-value pair was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <returns>true if they key was found</returns>
        bool TryGetValue(KeyType key, out ValueType value);
        /// <summary>
        /// Attempts to set the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <returns>true if the key-value pair was found and the value was updated</returns>
        bool TrySetValue(KeyType key, ValueType value);
        /// <summary>
        /// Attempts to get the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out ValueType value, out long rank);
        /// <summary>
        /// Attempts to return the key of a key-value pair at the specified rank index.
        /// If all key-value pairs in the collection were converted to a sorted array, this would be the equivalent of array[rank].Key.
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <param name="key">the key located at that index</param>
        /// <returns>true if there is an element at the the specified index</returns>
        bool TryGetKeyByRank(long rank, out KeyType key);

        /// <summary>
        /// Adds a key-value pair to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        void Add(KeyType key, ValueType value);
        /// <summary>
        /// Removes a key-value pair from the collection.
        /// </summary>
        /// <param name="key">key of the key-value pair to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        ValueType GetValue(KeyType key);
        /// <summary>
        /// Updates the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void SetValue(KeyType key, ValueType value);
        /// <summary>
        /// Retrieves the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Get(KeyType key, out ValueType value, out long rank);
        /// <summary>
        /// Retrieves the key of a key-value pair at the specified rank index.
        /// If all key-value pairs in the collection were converted to a sorted array, this would be the equivalent of array[rank].Key.
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKeyByRank(long rank);

        /// <summary>
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// For a RankMap, the only valid values are 0 (which does nothing) and -1 (which removes the key-value pair).
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        void AdjustCount(KeyType key, long countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the lowest in the collection (in sort order) and the value associated with it.
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order) and the value associated with it.
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value and rank associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out long rank);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value and rank  associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out long rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value and rank  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out long rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value and rank  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out long rank);
    }

    /// <summary>
    /// Represents a ordered key collection, augmented with rank information. The rank of a key is the index it would
    /// be located in if all the keys in the tree were placed into a sorted array.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public interface IRankListLong<KeyType> : IEnumerable<EntryRankListLong<KeyType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of keys in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue keys.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of keys in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all keys from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <returns>true if the key was added; false if the key was already present</returns>
        bool TryAdd(KeyType key);
        /// <summary>
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <returns>true if they key was found</returns>
        bool TryGetKey(KeyType key, out KeyType keyOut);
        /// <summary>
        /// Attempts to update the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        bool TrySetKey(KeyType key);
        /// <summary>
        /// Attempts to get the actual key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out KeyType keyOut, out long rank);
        /// <summary>
        /// Retrieves the key at the specified rank index.
        /// If all keys in the collection were converted to a sorted array, this would be the equivalent of array[rank].
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <param name="key">the key located at the specified rank index</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        bool TryGetKeyByRank(long rank, out KeyType key);

        /// <summary>
        /// Adds a key to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        void Add(KeyType key);
        /// <summary>
        /// Removes a key from the collection.
        /// </summary>
        /// <param name="key">key to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKey(KeyType key);
        /// <summary>
        /// Updates the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        void SetKey(KeyType key);
        /// <summary>
        /// Retrieves the actual key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        void Get(KeyType key, out KeyType keyOut, out long rank);
        /// <summary>
        /// Retrieves the key at the specified rank index.
        /// If all keys in the collection were converted to a sorted array, this would be the equivalent of array[rank].
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKeyByRank(long rank);

        /// <summary>
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// For a RankMap, the only valid values are 0 (which does nothing) and -1 (which removes the key-value pair).
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        void AdjustCount(KeyType key, long countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out long rank);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out long rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out long rank);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out long rank);
    }


    //
    // IMultiRankMap, IMultiRankList, IMultiRankMapLong, IMultiRankListLong
    //

    /// <summary>
    /// Represents a ordered key-value mapping, augmented with multi-rank information. The rank of a key-value pair is the index it would
    /// be located in if all the key-value pairs in the tree were placed into a sorted array. Each key-value pair also has a count
    /// associated with it, which models sorted arrays containing multiple instances of a key. It is equivalent to the number of times
    /// the key-value pair appears in the array. Rank index values account for such multiple occurrences. In this case, the rank
    /// index is the index at which the first instance of a particular key would occur in a sorted array containing all keys.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    /// <typeparam name="ValueType">Type of value associated with each entry.</typeparam>
    public interface IMultiRankMap<KeyType, ValueType> : IEnumerable<EntryMultiRankMap<KeyType, ValueType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of key-value pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue key-value pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of key-value pairs in the collection.
        /// </summary>
        long LongCount { get; }
        /// <summary>
        /// Returns the total size of an array containing all key-value pairs, where each key occurs one or more times, determined by
        /// the 'count' associated with each key-value pair.
        /// </summary>
        int RankCount { get; }

        /// <summary>
        /// Removes all key-value pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key to the collection with an associated value and count.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <param name="count">number of instances to repeat this key-value pair if the collection were converted to a sorted array</param>
        /// <returns>true if key was not present and key-value pair was added; false if key-value pair was already present and value was updated</returns>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        bool TryAdd(KeyType key, ValueType value, int count);
        /// <summary>
        /// Attempts to remove a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// The entire key-value pair is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key-value pair was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <returns>true if they key was found</returns>
        bool TryGetValue(KeyType key, out ValueType value);
        /// <summary>
        /// Attempts to set the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <returns>true if the key-value pair was found and the value was updated</returns>
        bool TrySetValue(KeyType key, ValueType value);
        /// <summary>
        /// Attempts to get the value, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key-value pair if the collection were converted to a sorted array</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out ValueType value, out int rank, out int count);
        /// <summary>
        /// Attempts to update the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">the new value to set</param>
        /// <param name="rank">the new rank count</param>
        /// <returns>true if they key was found and the rank was a valid value or false if the rank count was not at least 1
        /// or the sum of counts would have exceeded Int32.MaxValue</returns>
        bool TrySet(KeyType key, ValueType value, int rank);
        /// <summary>
        /// Attempts to return the key of a key-value pair at the specified rank index. If all key-value pairs in the collection were
        /// converted to a sorted array of key-value pairs, this would be the equivalent of array[rank].Key, subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <param name="key">the key located at that index</param>
        /// <returns>true if there is an element at the the specified index and it corresponds to the first in the virtual
        /// ordered sequence of multiple instances in an equivalent sorted array</returns>
        bool TryGetKeyByRank(int rank, out KeyType key);

        /// <summary>
        /// Adds a key to the collection with an associated value and count.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <param name="count">number of instances to repeat this key-value pair if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        void Add(KeyType key, ValueType value, int count);
        /// <summary>
        /// Removes a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// The entire key-value pair is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        ValueType GetValue(KeyType key);
        /// <summary>
        /// Updates the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void SetValue(KeyType key, ValueType value);
        /// <summary>
        /// Retrieves the value, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key-value pair if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Get(KeyType key, out ValueType value, out int rank, out int count);
        /// <summary>
        /// Updates the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">the new value to set</param>
        /// <param name="rank">the new rank count</param>
        /// <exception cref="ArgumentException">the rank count was not at least 1</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        void Set(KeyType key, ValueType value, int rank);
        /// <summary>
        /// Retrieves the key of a key-value pair at the specified rank index. If all key-value pairs in the collection were
        /// converted to a sorted array of key-value pairs, this would be the equivalent of array[rank].Key, subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKeyByRank(int rank);

        /// <summary>
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// If the countAdjust is equal to the negative value of the current count, the key-value pair will be removed.
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        void AdjustCount(KeyType key, int countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the lowest key-value pair in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value, rank and count associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value, rank and count  associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value, rank and count  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value, rank and count  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out int rank, out int count);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(int position, out int nearestStart);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(int position, out KeyType nearestKey, out int nearestStart, out int count, out ValueType value);
    }

    /// <summary>
    /// Represents a ordered key collection, augmented with multi-rank information. The rank of a key is the index it would
    /// be located in if all the keys in the tree were placed into a sorted array. Each key also has a count
    /// associated with it, which models sorted arrays containing multiple instances of a key. It is equivalent to the number of times
    /// the key appears in the array. Rank index values account for such multiple occurrences. In this case, the rank
    /// index is the index at which the first instance of a particular key would occur in a sorted array containing all keys.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public interface IMultiRankList<KeyType> : IEnumerable<EntryMultiRankList<KeyType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of keys in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue keys.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of keys in the collection.
        /// </summary>
        long LongCount { get; }
        /// <summary>
        /// Returns the total size of an array containing all keys, where each key occurs one or more times, determined by
        /// the 'count' associated with each key.
        /// </summary>
        int RankCount { get; }

        /// <summary>
        /// Removes all keys from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key to the collection.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="count">number of instances to repeat this key if the collection were converted to a sorted array</param>
        /// <returns>true if key was not present and key was added; false if key was already present</returns>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        bool TryAdd(KeyType key, int count);
        /// <summary>
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// The entire key is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <returns>true if they key was found</returns>
        bool TryGetKey(KeyType key, out KeyType keyOut);
        /// <summary>
        /// Attempts to update the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        bool TrySetKey(KeyType key);
        /// <summary>
        /// Attempts to get the actual key, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">out parameter that returns the actual key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key if the collection were converted to a sorted array</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out KeyType keyOut, out int rank, out int count);
        /// <summary>
        /// Attempts to update the key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for and also update</param>
        /// <param name="rank">the new rank count</param>
        /// <returns>true if they key was found and the rank was a valid value or false if the rank count was not at least 1
        /// or the sum of counts would have exceeded Int32.MaxValue</returns>
        bool TrySet(KeyType key, int rank);
        /// <summary>
        /// Attempts to return the key at the specified rank index. If all keys in the collection were
        /// converted to a sorted array of keys, this would be the equivalent of array[rank]s, subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <param name="key">the key located at that index</param>
        /// <returns>true if there is an element at the the specified index and it corresponds to the first in the virtual
        /// ordered sequence of multiple instances in an equivalent sorted array</returns>
        bool TryGetKeyByRank(int rank, out KeyType key);

        /// <summary>
        /// Adds a key to the collection with an associated count.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="count">number of instances to repeat this key if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        void Add(KeyType key, int count);
        /// <summary>
        /// Removes a key from the collection. If the key is not present, no change is made to the collection.
        /// The entire key is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKey(KeyType key);
        /// <summary>
        /// Updates the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        void SetKey(KeyType key);
        /// <summary>
        /// Retrieves the actual key, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">out parameter that returns the actual key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Get(KeyType key, out KeyType keyOut, out int rank, out int count);
        /// <summary>
        /// Updates the key and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for and also update</param>
        /// <param name="rank">the new rank count</param>
        /// <exception cref="ArgumentException">the rank count was not at least 1</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        void Set(KeyType key, int rank);
        /// <summary>
        /// Retrieves the key at the specified rank index. If all keys in the collection were
        /// converted to a sorted array of keys, this would be the equivalent of array[rank], subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKeyByRank(int rank);

        /// <summary>
        /// Adjusts the rank count associated with the key. The countAdjust added to the existing count.
        /// If the countAdjust is equal to the negative value of the current count, the key will be removed.
        /// </summary>
        /// <param name="key">key identifying the key to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        void AdjustCount(KeyType key, int countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out int rank, out int count);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out int rank, out int count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out int rank, out int count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out int rank, out int count);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(int position, out int nearestStart);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(int position, out KeyType nearestKey, out int nearestStart, out int count);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(int position, out KeyType nearestKey, out int nearestStart, out int count);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(int position, out KeyType nearestKey, out int nearestStart, out int count);
    }

    /// <summary>
    /// Represents a ordered key-value mapping, augmented with multi-rank information. The rank of a key-value pair is the index it would
    /// be located in if all the key-value pairs in the tree were placed into a sorted array. Each key-value pair also has a count
    /// associated with it, which models sorted arrays containing multiple instances of a key. It is equivalent to the number of times
    /// the key-value pair appears in the array. Rank index values account for such multiple occurrences. In this case, the rank
    /// index is the index at which the first instance of a particular key would occur in a sorted array containing all keys.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    /// <typeparam name="ValueType">Type of value associated with each entry.</typeparam>
    public interface IMultiRankMapLong<KeyType, ValueType> : IEnumerable<EntryMultiRankMapLong<KeyType, ValueType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of key-value pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue key-value pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of key-value pairs in the collection.
        /// </summary>
        long LongCount { get; }
        /// <summary>
        /// Returns the total size of an array containing all key-value pairs, where each key occurs one or more times, determined by
        /// the 'count' associated with each key-value pair.
        /// </summary>
        long RankCount { get; }

        /// <summary>
        /// Removes all key-value pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key to the collection with an associated value and count.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <param name="count">number of instances to repeat this key-value pair if the collection were converted to a sorted array</param>
        /// <returns>true if key was not present and key-value pair was added; false if key-value pair was already present and value was updated</returns>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        bool TryAdd(KeyType key, ValueType value, long count);
        /// <summary>
        /// Attempts to remove a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// The entire key-value pair is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key-value pair was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <returns>true if they key was found</returns>
        bool TryGetValue(KeyType key, out ValueType value);
        /// <summary>
        /// Attempts to set the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <returns>true if the key-value pair was found and the value was updated</returns>
        bool TrySetValue(KeyType key, ValueType value);
        /// <summary>
        /// Attempts to get the value, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key-value pair if the collection were converted to a sorted array</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out ValueType value, out long rank, out long count);
        /// <summary>
        /// Attempts to update the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">the new value to set</param>
        /// <param name="rank">the new rank count</param>
        /// <returns>true if they key was found and the rank was a valid value or false if the rank count was not at least 1
        /// or the sum of counts would have exceeded Int64.MaxValue</returns>
        bool TrySet(KeyType key, ValueType value, long rank);
        /// <summary>
        /// Attempts to return the key of a key-value pair at the specified rank index. If all key-value pairs in the collection were
        /// converted to a sorted array of key-value pairs, this would be the equivalent of array[rank].Key, subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <param name="key">the key located at that index</param>
        /// <returns>true if there is an element at the the specified index and it corresponds to the first in the virtual
        /// ordered sequence of multiple instances in an equivalent sorted array</returns>
        bool TryGetKeyByRank(long rank, out KeyType key);

        /// <summary>
        /// Adds a key to the collection with an associated value and count.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <param name="count">number of instances to repeat this key-value pair if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        void Add(KeyType key, ValueType value, long count);
        /// <summary>
        /// Removes a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// The entire key-value pair is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        ValueType GetValue(KeyType key);
        /// <summary>
        /// Updates the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void SetValue(KeyType key, ValueType value);
        /// <summary>
        /// Retrieves the value, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key-value pair if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Get(KeyType key, out ValueType value, out long rank, out long count);
        /// <summary>
        /// Updates the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">the new value to set</param>
        /// <param name="rank">the new rank count</param>
        /// <exception cref="ArgumentException">the rank count was not at least 1</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        void Set(KeyType key, ValueType value, long rank);
        /// <summary>
        /// Retrieves the key of a key-value pair at the specified rank index. If all key-value pairs in the collection were
        /// converted to a sorted array of key-value pairs, this would be the equivalent of array[rank].Key, subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        KeyType GetKeyByRank(long rank);

        /// <summary>
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// If the countAdjust is equal to the negative value of the current count, the key-value pair will be removed.
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        void AdjustCount(KeyType key, long countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the lowest key-value pair in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value, rank and count associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out long rank, out long count);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value, rank and count  associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out ValueType value, out long rank, out long count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value, rank and count  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out ValueType value, out long rank, out long count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value, rank and count  associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out ValueType value, out long rank, out long count);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(long position, out long nearestStart);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(long position, out KeyType nearestKey, out long nearestStart, out long count, out ValueType value);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(long position, out KeyType nearestKey, out long nearestStart, out long count, out ValueType value);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(long position, out KeyType nearestKey, out long nearestStart, out long count, out ValueType value);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <param name="value">the value associated with the key</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(long position, out KeyType nearestKey, out long nearestStart, out long count, out ValueType value);
    }

    /// <summary>
    /// Represents a ordered key collection, augmented with multi-rank information. The rank of a key is the index it would
    /// be located in if all the keys in the tree were placed into a sorted array. Each key also has a count
    /// associated with it, which models sorted arrays containing multiple instances of a key. It is equivalent to the number of times
    /// the key appears in the array. Rank index values account for such multiple occurrences. In this case, the rank
    /// index is the index at which the first instance of a particular key would occur in a sorted array containing all keys.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public interface IMultiRankListLong<KeyType> : IEnumerable<EntryMultiRankListLong<KeyType>> where KeyType : IComparable<KeyType>
    {
        /// <summary>
        /// Returns the number of keys in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue keys.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of keys in the collection.
        /// </summary>
        long LongCount { get; }
        /// <summary>
        /// Returns the total size of an array containing all keys, where each key occurs one or more times, determined by
        /// the 'count' associated with each key.
        /// </summary>
        long RankCount { get; }

        /// <summary>
        /// Removes all keys from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        bool ContainsKey(KeyType key);

        /// <summary>
        /// Attempts to add a key to the collection.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="count">number of instances to repeat this key if the collection were converted to a sorted array</param>
        /// <returns>true if key was not present and key was added; false if key was already present</returns>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        bool TryAdd(KeyType key, long count);
        /// <summary>
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// The entire key is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        bool TryRemove(KeyType key);
        /// <summary>
        /// Attempts to get the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <returns>true if they key was found</returns>
        bool TryGetKey(KeyType key, out KeyType keyOut);
        /// <summary>
        /// Attempts to update the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        bool TrySetKey(KeyType key);
        /// <summary>
        /// Attempts to get the actual key, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">out parameter that returns the actual key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key if the collection were converted to a sorted array</param>
        /// <returns>true if they key was found</returns>
        bool TryGet(KeyType key, out KeyType keyOut, out long rank, out long count);
        /// <summary>
        /// Attempts to update the key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for and also update</param>
        /// <param name="rank">the new rank count</param>
        /// <returns>true if they key was found and the rank was a valid value or false if the rank count was not at least 1
        /// or the sum of counts would have exceeded Int64.MaxValue</returns>
        bool TrySet(KeyType key, long rank);
        /// <summary>
        /// Attempts to return the key at the specified rank index. If all keys in the collection were
        /// converted to a sorted array of keys, this would be the equivalent of array[rank]s, subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <param name="key">the key located at that index</param>
        /// <returns>true if there is an element at the the specified index and it corresponds to the first in the virtual
        /// ordered sequence of multiple instances in an equivalent sorted array</returns>
        bool TryGetKeyByRank(long rank, out KeyType key);

        /// <summary>
        /// Adds a key to the collection with an associated count.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="count">number of instances to repeat this key if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        void Add(KeyType key, long count);
        /// <summary>
        /// Removes a key from the collection. If the key is not present, no change is made to the collection.
        /// The entire key is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        void Remove(KeyType key);
        /// <summary>
        /// Retrieves the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKey(KeyType key);
        /// <summary>
        /// Updates the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        void SetKey(KeyType key);
        /// <summary>
        /// Retrieves the actual key, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">out parameter that returns the actual key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        void Get(KeyType key, out KeyType keyOut, out long rank, out long count);
        /// <summary>
        /// Updates the key and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for and also update</param>
        /// <param name="rank">the new rank count</param>
        /// <exception cref="ArgumentException">the rank count was not at least 1</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        void Set(KeyType key, long rank);
        /// <summary>
        /// Retrieves the key at the specified rank index. If all keys in the collection were
        /// converted to a sorted array of keys, this would be the equivalent of array[rank], subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        KeyType GetKeyByRank(long rank);

        /// <summary>
        /// Adjusts the rank count associated with the key. The countAdjust added to the existing count.
        /// If the countAdjust is equal to the negative value of the current count, the key will be removed.
        /// </summary>
        /// <param name="key">key identifying the key to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        void AdjustCount(KeyType key, long countAdjust);

        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Least(out KeyType leastOut);
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        bool Greatest(out KeyType greatestOut);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey);

        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, out long rank, out long count);
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        bool NearestLess(KeyType key, out KeyType nearestKey, out long rank, out long count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, out long rank, out long count);
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        bool NearestGreater(KeyType key, out KeyType nearestKey, out long rank, out long count);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(long position, out long nearestStart);

        /// <summary>
        /// Search for the nearest key's index that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a key's range into the start index of a key's range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the specified index is greater than or equal to the extent it will return the last key's start index.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqualByRank(long position, out KeyType nearestKey, out long nearestStart, out long count);
        /// <summary>
        /// Search for the nearest key's index that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a key's range, the start of the previous key's range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no keys in the collection or position is less than or equal to 0, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLessByRank(long position, out KeyType nearestKey, out long nearestStart, out long count);
        /// <summary>
        /// Search for the nearest key's index that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range, that index will be returned.
        /// If the index refers to the interior index for a key's range, the start of the next key's range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqualByRank(long position, out KeyType nearestKey, out long nearestStart, out long count);
        /// <summary>
        /// Search for the nearest key's range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index that was found.
        /// If the index refers to the start of a key's range or is an interior index for a range, the next key's range will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first key's range.
        /// If the index is greater than or equal to the start of the last key's range, no index will be found.
        /// </param>
        /// <param name="nearestKey">the key that was found</param>
        /// <param name="count">the count for the key (i.e. the length of the key's range)</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreaterByRank(long position, out KeyType nearestKey, out long nearestStart, out long count);
    }


    //
    // IRangeMap, IRangeList, IRangeMapLong, IRangeListLong
    //

    /// <summary>
    /// Represents an sequenced collection of ranges with associated values. Each range is defined by it's length, and occupies
    /// a particular position in the sequence, determined by the location where it was inserted (and any insertions/deletions that
    /// have occurred before or after it in the sequence). The start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// All ranges must have a length of at least 1.
    /// </summary>
    /// <typeparam name="ValueType">type of the value associated with each range</typeparam>
    public interface IRangeMap<ValueType> : IEnumerable<EntryRangeMap<ValueType>>
    {
        /// <summary>
        /// Returns the number of ranges in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue ranges.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all ranges from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range in the collection starting at the specified index.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(int start);

        /// <summary>
        /// Attempt to insert a range of a given length at the specified start index and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <param name="value">value to associate with the range</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        bool TryInsert(int start, int length, ValueType value);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        bool TryDelete(int start);
        /// <summary>
        /// Attempt to query the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(int start, out int length);
        /// <summary>
        /// Attempt to change the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        bool TrySetLength(int start, int length);
        /// <summary>
        /// Attempt to query the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetValue(int start, out ValueType value);
        /// <summary>
        /// Attempt to update the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TrySetValue(int start, ValueType value);
        /// <summary>
        /// Attempt to get the value and length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGet(int start, out int length, out ValueType value);
        /// <summary>
        /// Attempt to change the length and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int32.MaxValue</returns>
        bool TrySet(int start, int length, ValueType value);

        /// <summary>
        /// Inserts a range of a given length at the specified start index and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <param name="value">value to associate with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        void Insert(int start, int length, ValueType value);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void Delete(int start);
        /// <summary>
        /// Retrieves the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the length of the range found at the specified start index</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        int GetLength(int start);
        /// <summary>
        /// Changes the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        void SetLength(int start, int length);
        /// <summary>
        /// Retrieves the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the value associated with the range</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        ValueType GetValue(int start);
        /// <summary>
        /// Updates the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void SetValue(int start, ValueType value);
        /// <summary>
        /// Retrieves the value and length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void Get(int start, out int length, out ValueType value);
        /// <summary>
        /// Changes the length and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <exception cref="ArgumentException">the start was not the beginning of a range</exception>
        /// <exception cref="OverflowException">sum of lengths would have exceeded Int32.MaxValue</exception>
        void Set(int start, int length, ValueType value);

        /// <summary>
        /// Adjust the length of the range starting at 'start' by adding 'adjust' to the current length of the
        /// range. If the length would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="adjust">the amount to adjust the length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start'.</exception>
        /// <exception cref="ArgumentOutOfRangeException">the length would become negative</exception>
        /// <exception cref="OverflowException">the extent would become larger than Int32.MaxValue</exception>
        void AdjustLength(int start, int adjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <returns>the extent of the ranges</returns>
        int GetExtent();

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, out int nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, out int nearestStart, out int length, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, out int nearestStart, out int length, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, out int nearestStart, out int length, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, out int nearestStart, out int length, out ValueType value);
    }

    /// <summary>
    /// Represents an sequenced collection of ranges (without associated values). Each range is defined by it's length, and occupies
    /// a particular position in the sequence, determined by the location where it was inserted (and any insertions/deletions that
    /// have occurred before or after it in the sequence). The start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// All ranges must have a length of at least 1.
    /// </summary>
    public interface IRangeList : IEnumerable<EntryRangeList>
    {
        /// <summary>
        /// Returns the number of ranges in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue ranges.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all ranges from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range in the collection starting at the specified index.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(int start);

        /// <summary>
        /// Attempt to insert a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        bool TryInsert(int start, int length);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        bool TryDelete(int start);
        /// <summary>
        /// Attempt to query the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(int start, out int length);
        /// <summary>
        /// Attempt to change the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        bool TrySetLength(int start, int length);

        /// <summary>
        /// Inserts a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        void Insert(int start, int length);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void Delete(int start);
        /// <summary>
        /// Retrieves the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the length of the range found at the specified start index</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        int GetLength(int start);
        /// <summary>
        /// Changes the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        void SetLength(int start, int length);

        /// <summary>
        /// Adjust the length of the range starting at 'start' by adding 'adjust' to the current length of the
        /// range. If the length would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="adjust">the amount to adjust the length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start'.</exception>
        /// <exception cref="ArgumentOutOfRangeException">the length would become negative</exception>
        /// <exception cref="OverflowException">the extent would become larger than Int32.MaxValue</exception>
        void AdjustLength(int start, int adjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <returns>the extent of the ranges</returns>
        int GetExtent();

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, out int nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, out int nearestStart, out int length);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, out int nearestStart, out int length);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, out int nearestStart, out int length);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, out int nearestStart, out int length);
    }

    /// <summary>
    /// Represents an sequenced collection of ranges with associated values. Each range is defined by it's length, and occupies
    /// a particular position in the sequence, determined by the location where it was inserted (and any insertions/deletions that
    /// have occurred before or after it in the sequence). The start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// All ranges must have a length of at least 1.
    /// </summary>
    /// <typeparam name="ValueType">type of the value associated with each range</typeparam>
    public interface IRangeMapLong<ValueType> : IEnumerable<EntryRangeMapLong<ValueType>>
    {
        /// <summary>
        /// Returns the number of ranges in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue ranges.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all ranges from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range in the collection starting at the specified index.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(long start);

        /// <summary>
        /// Attempt to insert a range of a given length at the specified start index and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <param name="value">value to associate with the range</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        bool TryInsert(long start, long length, ValueType value);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        bool TryDelete(long start);
        /// <summary>
        /// Attempt to query the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(long start, out long length);
        /// <summary>
        /// Attempt to change the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        bool TrySetLength(long start, long length);
        /// <summary>
        /// Attempt to query the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetValue(long start, out ValueType value);
        /// <summary>
        /// Attempt to update the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TrySetValue(long start, ValueType value);
        /// <summary>
        /// Attempt to get the value and length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGet(long start, out long length, out ValueType value);
        /// <summary>
        /// Attempt to change the length and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int64.MaxValue</returns>
        bool TrySet(long start, long length, ValueType value);

        /// <summary>
        /// Inserts a range of a given length at the specified start index and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <param name="value">value to associate with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        void Insert(long start, long length, ValueType value);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void Delete(long start);
        /// <summary>
        /// Retrieves the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the length of the range found at the specified start index</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        long GetLength(long start);
        /// <summary>
        /// Changes the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        void SetLength(long start, long length);
        /// <summary>
        /// Retrieves the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the value associated with the range</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        ValueType GetValue(long start);
        /// <summary>
        /// Updates the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void SetValue(long start, ValueType value);
        /// <summary>
        /// Retrieves the value and length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void Get(long start, out long length, out ValueType value);
        /// <summary>
        /// Changes the length and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <exception cref="ArgumentException">the start was not the beginning of a range</exception>
        /// <exception cref="OverflowException">sum of lengths would have exceeded Int64.MaxValue</exception>
        void Set(long start, long length, ValueType value);

        /// <summary>
        /// Adjust the length of the range starting at 'start' by adding 'adjust' to the current length of the
        /// range. If the length would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="adjust">the amount to adjust the length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start'.</exception>
        /// <exception cref="ArgumentOutOfRangeException">the length would become negative</exception>
        /// <exception cref="OverflowException">the extent would become larger than Int64.MaxValue</exception>
        void AdjustLength(long start, long adjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <returns>the extent of the ranges</returns>
        long GetExtent();

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, out long nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, out long nearestStart, out long length, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, out long nearestStart, out long length, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, out long nearestStart, out long length, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, out long nearestStart, out long length, out ValueType value);
    }

    /// <summary>
    /// Represents an sequenced collection of ranges (without associated values). Each range is defined by it's length, and occupies
    /// a particular position in the sequence, determined by the location where it was inserted (and any insertions/deletions that
    /// have occurred before or after it in the sequence). The start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// All ranges must have a length of at least 1.
    /// </summary>
    public interface IRangeListLong : IEnumerable<EntryRangeListLong>
    {
        /// <summary>
        /// Returns the number of ranges in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue ranges.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all ranges from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range in the collection starting at the specified index.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(long start);

        /// <summary>
        /// Attempt to insert a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        bool TryInsert(long start, long length);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        bool TryDelete(long start);
        /// <summary>
        /// Attempt to query the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(long start, out long length);
        /// <summary>
        /// Attempt to change the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        bool TrySetLength(long start, long length);

        /// <summary>
        /// Inserts a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        void Insert(long start, long length);
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        void Delete(long start);
        /// <summary>
        /// Retrieves the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the length of the range found at the specified start index</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        long GetLength(long start);
        /// <summary>
        /// Changes the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        void SetLength(long start, long length);

        /// <summary>
        /// Adjust the length of the range starting at 'start' by adding 'adjust' to the current length of the
        /// range. If the length would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="adjust">the amount to adjust the length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start'.</exception>
        /// <exception cref="ArgumentOutOfRangeException">the length would become negative</exception>
        /// <exception cref="OverflowException">the extent would become larger than Int64.MaxValue</exception>
        void AdjustLength(long start, long adjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <returns>the extent of the ranges</returns>
        long GetExtent();

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, out long nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, out long nearestStart, out long length);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, out long nearestStart, out long length);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, out long nearestStart, out long length);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, out long nearestStart, out long length);
    }


    //
    // IRange2Map, IRange2List, IRange2MapLong, IRange2ListLong
    //

    /// <summary>
    /// Identifies which side of a Range2 mapping an index refers to.
    /// </summary>
    public enum Side
    {
        /// <summary>
        /// The index applies to the first (left side) range sequence
        /// </summary>
        X = 0,
        /// <summary>
        /// The index applies to the second (right side) range sequence
        /// </summary>
        Y = 1,
    };

    /// <summary>
    /// Represents an sequenced collection of range-to-range pairs with associated values. Each range pair is defined by two lengths,
    /// one for the X sequence and one for the Y sequence.
    /// With regard to a particular sequence, each range occupies a particular position in the sequence, determined by the location
    /// where it was inserted (and any insertions/deletions that have occurred before or after it in the sequence).
    /// Within the sequence, the start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// The above applies separately to both the X side sequence and the Y side sequence.
    /// All ranges must have a lengths of at least 1, on both sides.
    /// </summary>
    /// <typeparam name="ValueType">type of the value associated with each range pair</typeparam>
    public interface IRange2Map<ValueType> : IEnumerable<EntryRange2Map<ValueType>>
    {
        /// <summary>
        /// Returns the number of range pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue range pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all range pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range pair in the collection starting at the index specified, with respect to the side specified.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(int start, Side side);

        /// <summary>
        /// Attempt to insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <param name="value">the value to associate with the range pair</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue on either side</exception>
        bool TryInsert(int start, Side side, int xLength, int yLength, ValueType value);
        /// <summary>
        /// Attempt to delete the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <returns>true if a range pair was successfully deleted</returns>
        bool TryDelete(int start, Side side);
        /// <summary>
        /// Attempt to query the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <param name="length">the length of the range from the specified side (X or Y)</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(int start, Side side, out int length);
        /// <summary>
        /// Attempt to change the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int32.MaxValue</exception>
        bool TrySetLength(int start, Side side, int length);
        /// <summary>
        /// Attempt to query the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index on the specified side</returns>
        bool TryGetValue(int start, Side side, out ValueType value);
        /// <summary>
        /// Attempt to update the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        bool TrySetValue(int start, Side side, ValueType value);
        /// <summary>
        /// Attempt to get the value and lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value);
        /// <summary>
        /// Attempt to change the lengths and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int32.MaxValue</returns>
        bool TrySet(int start, Side side, int xLength, int yLength, ValueType value);

        /// <summary>
        /// Insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <param name="value">the value to associate with the range pair</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue on either side</exception>
        void Insert(int start, Side side, int xLength, int yLength, ValueType value);
        /// <summary>
        /// Deletes the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Delete(int start, Side side);
        /// <summary>
        /// Retrieves the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <returns>the length of the range from the specified side (X or Y)</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        int GetLength(int start, Side side);
        /// <summary>
        /// Changes the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int32.MaxValue</exception>
        void SetLength(int start, Side side, int length);
        /// <summary>
        /// Retrieves the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <returns>the value associated with the range</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        ValueType GetValue(int start, Side side);
        /// <summary>
        /// Updates the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void SetValue(int start, Side side, ValueType value);
        /// <summary>
        /// Attempt to get the value and lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Get(int start, Side side, out int otherStart, out int xLength, out int yLength, out ValueType value);
        /// <summary>
        /// Changes the lengths and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">sum of lengths would have exceeded Int32.MaxValue</exception>
        void Set(int start, Side side, int xLength, int yLength, ValueType value);

        /// <summary>
        /// Adjust the lengths of the range starting at 'start' by adding xAdjust and yAdjust to the current lengths of the
        /// range. If the lengths would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="side">which side (X or Y) the start parameter applies</param>
        /// <param name="xAdjust">the amount to adjust the X length by. Value may be negative to shrink the length</param>
        /// <param name="yAdjust">the amount to adjust the Y length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start', or the length on
        /// one side would become 0 while the length on the other side would not be 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException">one or both of the lengths would become negative</exception>
        /// <exception cref="OverflowException">the X or Y extent would become larger than Int32.MaxValue</exception>
        void AdjustLength(int start, Side side, int xAdjust, int yAdjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges on the specified side. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <param name="side">the side (X or Y) to which the query applies.</param>
        /// <returns>the extent of the ranges on the specified side</returns>
        int GetExtent(Side side);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, Side side, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, Side side, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, Side side, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, Side side, out int nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength, out ValueType value);
    }

    /// <summary>
    /// Represents an sequenced collection of range-to-range pairs (without associated values). Each range pair is defined by two lengths,
    /// one for the X sequence and one for the Y sequence.
    /// With regard to a particular sequence, each range occupies a particular position in the sequence, determined by the location
    /// where it was inserted (and any insertions/deletions that have occurred before or after it in the sequence).
    /// Within the sequence, the start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// The above applies separately to both the X side sequence and the Y side sequence.
    /// All ranges must have a lengths of at least 1, on both sides.
    /// </summary>
    public interface IRange2List : IEnumerable<EntryRange2List>
    {
        /// <summary>
        /// Returns the number of range pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue range pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all range pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range pair in the collection starting at the index specified, with respect to the side specified.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(int start, Side side);

        /// <summary>
        /// Attempt to insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue on either side</exception>
        bool TryInsert(int start, Side side, int xLength, int yLength);
        /// <summary>
        /// Attempt to delete the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <returns>true if a range pair was successfully deleted</returns>
        bool TryDelete(int start, Side side);
        /// <summary>
        /// Attempt to query the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <param name="length">the length of the range from the specified side (X or Y)</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(int start, Side side, out int length);
        /// <summary>
        /// Attempt to change the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int32.MaxValue</exception>
        bool TrySetLength(int start, Side side, int length);
        /// <summary>
        /// Attempt to get the lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        bool TryGet(int start, Side side, out int otherStart, out int xLength, out int yLength);
        /// <summary>
        /// Attempt to change the lengths associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int32.MaxValue</returns>
        bool TrySet(int start, Side side, int xLength, int yLength);

        /// <summary>
        /// Insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue on either side</exception>
        void Insert(int start, Side side, int xLength, int yLength);
        /// <summary>
        /// Deletes the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Delete(int start, Side side);
        /// <summary>
        /// Retrieves the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <returns>the length of the range from the specified side (X or Y)</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        int GetLength(int start, Side side);
        /// <summary>
        /// Changes the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int32.MaxValue</exception>
        void SetLength(int start, Side side, int length);
        /// <summary>
        /// Attempt to get the lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Get(int start, Side side, out int otherStart, out int xLength, out int yLength);
        /// <summary>
        /// Changes the lengths associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int32.MaxValue</returns>
        void Set(int start, Side side, int xLength, int yLength);

        /// <summary>
        /// Adjust the lengths of the range starting at 'start' by adding xAdjust and yAdjust to the current lengths of the
        /// range. If the lengths would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="side">which side (X or Y) the start parameter applies</param>
        /// <param name="xAdjust">the amount to adjust the X length by. Value may be negative to shrink the length</param>
        /// <param name="yAdjust">the amount to adjust the Y length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start', or the length on
        /// one side would become 0 while the length on the other side would not be 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException">one or both of the lengths would become negative</exception>
        /// <exception cref="OverflowException">the X or Y extent would become larger than Int32.MaxValue</exception>
        void AdjustLength(int start, Side side, int xAdjust, int yAdjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges on the specified side. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <param name="side">the side (X or Y) to which the query applies.</param>
        /// <returns>the extent of the ranges on the specified side</returns>
        int GetExtent(Side side);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, Side side, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, Side side, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, Side side, out int nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, Side side, out int nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength);
    }

    /// <summary>
    /// Represents an sequenced collection of range-to-range pairs with associated values. Each range pair is defined by two lengths,
    /// one for the X sequence and one for the Y sequence.
    /// With regard to a particular sequence, each range occupies a particular position in the sequence, determined by the location
    /// where it was inserted (and any insertions/deletions that have occurred before or after it in the sequence).
    /// Within the sequence, the start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// The above applies separately to both the X side sequence and the Y side sequence.
    /// All ranges must have a lengths of at least 1, on both sides.
    /// </summary>
    /// <typeparam name="ValueType">type of the value associated with each range pair</typeparam>
    public interface IRange2MapLong<ValueType> : IEnumerable<EntryRange2MapLong<ValueType>>
    {
        /// <summary>
        /// Returns the number of range pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue range pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all range pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range pair in the collection starting at the index specified, with respect to the side specified.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(long start, Side side);

        /// <summary>
        /// Attempt to insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <param name="value">the value to associate with the range pair</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        bool TryInsert(long start, Side side, long xLength, long yLength, ValueType value);
        /// <summary>
        /// Attempt to delete the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <returns>true if a range pair was successfully deleted</returns>
        bool TryDelete(long start, Side side);
        /// <summary>
        /// Attempt to query the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <param name="length">the length of the range from the specified side (X or Y)</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(long start, Side side, out long length);
        /// <summary>
        /// Attempt to change the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        bool TrySetLength(long start, Side side, long length);
        /// <summary>
        /// Attempt to query the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index on the specified side</returns>
        bool TryGetValue(long start, Side side, out ValueType value);
        /// <summary>
        /// Attempt to update the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        bool TrySetValue(long start, Side side, ValueType value);
        /// <summary>
        /// Attempt to get the value and lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        bool TryGet(long start, Side side, out long otherStart, out long xLength, out long yLength, out ValueType value);
        /// <summary>
        /// Attempt to change the lengths and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int64.MaxValue</returns>
        bool TrySet(long start, Side side, long xLength, long yLength, ValueType value);

        /// <summary>
        /// Insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <param name="value">the value to associate with the range pair</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        void Insert(long start, Side side, long xLength, long yLength, ValueType value);
        /// <summary>
        /// Deletes the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Delete(long start, Side side);
        /// <summary>
        /// Retrieves the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <returns>the length of the range from the specified side (X or Y)</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        long GetLength(long start, Side side);
        /// <summary>
        /// Changes the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        void SetLength(long start, Side side, long length);
        /// <summary>
        /// Retrieves the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <returns>the value associated with the range</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        ValueType GetValue(long start, Side side);
        /// <summary>
        /// Updates the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void SetValue(long start, Side side, ValueType value);
        /// <summary>
        /// Attempt to get the value and lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Get(long start, Side side, out long otherStart, out long xLength, out long yLength, out ValueType value);
        /// <summary>
        /// Changes the lengths and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">sum of lengths would have exceeded Int64.MaxValue</exception>
        void Set(long start, Side side, long xLength, long yLength, ValueType value);

        /// <summary>
        /// Adjust the lengths of the range starting at 'start' by adding xAdjust and yAdjust to the current lengths of the
        /// range. If the lengths would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="side">which side (X or Y) the start parameter applies</param>
        /// <param name="xAdjust">the amount to adjust the X length by. Value may be negative to shrink the length</param>
        /// <param name="yAdjust">the amount to adjust the Y length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start', or the length on
        /// one side would become 0 while the length on the other side would not be 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException">one or both of the lengths would become negative</exception>
        /// <exception cref="OverflowException">the X or Y extent would become larger than Int64.MaxValue</exception>
        void AdjustLength(long start, Side side, long xAdjust, long yAdjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges on the specified side. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <param name="side">the side (X or Y) to which the query applies.</param>
        /// <returns>the extent of the ranges on the specified side</returns>
        long GetExtent(Side side);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, Side side, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, Side side, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, Side side, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, Side side, out long nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength, out ValueType value);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength, out ValueType value);
    }

    /// <summary>
    /// Represents an sequenced collection of range-to-range pairs (without associated values). Each range pair is defined by two lengths,
    /// one for the X sequence and one for the Y sequence.
    /// With regard to a particular sequence, each range occupies a particular position in the sequence, determined by the location
    /// where it was inserted (and any insertions/deletions that have occurred before or after it in the sequence).
    /// Within the sequence, the start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// The above applies separately to both the X side sequence and the Y side sequence.
    /// All ranges must have a lengths of at least 1, on both sides.
    /// </summary>
    public interface IRange2ListLong : IEnumerable<EntryRange2ListLong>
    {
        /// <summary>
        /// Returns the number of range pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue range pairs.</exception>
        uint Count { get; }
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        long LongCount { get; }

        /// <summary>
        /// Removes all range pairs from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines if there is a range pair in the collection starting at the index specified, with respect to the side specified.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        bool Contains(long start, Side side);

        /// <summary>
        /// Attempt to insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        bool TryInsert(long start, Side side, long xLength, long yLength);
        /// <summary>
        /// Attempt to delete the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <returns>true if a range pair was successfully deleted</returns>
        bool TryDelete(long start, Side side);
        /// <summary>
        /// Attempt to query the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <param name="length">the length of the range from the specified side (X or Y)</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        bool TryGetLength(long start, Side side, out long length);
        /// <summary>
        /// Attempt to change the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        bool TrySetLength(long start, Side side, long length);
        /// <summary>
        /// Attempt to get the lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        bool TryGet(long start, Side side, out long otherStart, out long xLength, out long yLength);
        /// <summary>
        /// Attempt to change the lengths associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int32.MaxValue</returns>
        bool TrySet(long start, Side side, long xLength, long yLength);

        /// <summary>
        /// Insert a range pair defined by the given pair of lengths at the specified start index with respect to
        /// the specified side.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// The sequence of the non-specified side is also updated, by inserting the other length of the pair at the same
        /// rank in the sequence as on the specified side.
        /// </summary>
        /// <param name="start">the specified start index to insert before</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <param name="xLength">the length of the X side of the range pair. the length must be at least 1.</param>
        /// <param name="yLength">the length of the Y side of the range pair. the length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        void Insert(long start, Side side, long xLength, long yLength);
        /// <summary>
        /// Deletes the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Delete(long start, Side side);
        /// <summary>
        /// Retrieves the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <returns>the length of the range from the specified side (X or Y)</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        long GetLength(long start, Side side);
        /// <summary>
        /// Changes the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        void SetLength(long start, Side side, long length);
        /// <summary>
        /// Attempt to get the lengths associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="otherStart">out parameter receiving the start index of the range from the opposite side of that specified</param>
        /// <param name="xLength">out parameter receiving the length of the range on the X side</param>
        /// <param name="yLength">out parameter receiving the length f the range on the Y side</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        void Get(long start, Side side, out long otherStart, out long xLength, out long yLength);
        /// <summary>
        /// Changes the lengths associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="side">the side with which the start parameter applies</param>
        /// <param name="xLength">new X-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <param name="yLength">new Y-side length for the range. The length must be at least 0. If equal to 0, no change is made
        /// to this side.</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int32.MaxValue</returns>
        void Set(long start, Side side, long xLength, long yLength);

        /// <summary>
        /// Adjust the lengths of the range starting at 'start' by adding xAdjust and yAdjust to the current lengths of the
        /// range. If the lengths would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="side">which side (X or Y) the start parameter applies</param>
        /// <param name="xAdjust">the amount to adjust the X length by. Value may be negative to shrink the length</param>
        /// <param name="yAdjust">the amount to adjust the Y length by. Value may be negative to shrink the length</param>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start', or the length on
        /// one side would become 0 while the length on the other side would not be 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException">one or both of the lengths would become negative</exception>
        /// <exception cref="OverflowException">the X or Y extent would become larger than Int32.MaxValue</exception>
        void AdjustLength(long start, Side side, long xAdjust, long yAdjust);

        /// <summary>
        /// Retrieves the extent of the sequence of ranges on the specified side. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <param name="side">the side (X or Y) to which the query applies.</param>
        /// <returns>the extent of the ranges on the specified side</returns>
        long GetExtent(Side side);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, Side side, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, Side side, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, Side side, out long nearestStart);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, Side side, out long nearestStart);

        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index with respect to the specified side.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        bool NearestLessOrEqual(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength);
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        bool NearestLess(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        bool NearestGreaterOrEqual(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength);
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index with respect to the specified side.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies.</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        bool NearestGreater(long position, Side side, out long nearestStart, out long otherStart, out long xLength, out long yLength);
    }
}
