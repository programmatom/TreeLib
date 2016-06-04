// NOTE: This file is auto-generated. DO NOT MAKE CHANGES HERE! They will be overwritten on rebuild.

/*
 *  Copyright � 2016 Thomas R. Lawrence
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TreeLib.Internal;

// Implementation of top-down splay tree written by Daniel Sleator <sleator@cs.cmu.edu>.
// Taken from http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c

// An overview of splay trees can be found here: https://en.wikipedia.org/wiki/Splay_tree

namespace TreeLib
{

    /// <summary>
    /// Implements a map, list or range collection using a splay tree. 
    /// </summary>
    
    /// <summary>
    /// Represents a ordered key-value mapping, augmented with rank information. The rank of a key-value pair is the index it would
    /// be located in if all the key-value pairs in the tree were placed into a sorted array.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    /// <typeparam name="ValueType">Type of value associated with each entry.</typeparam>
    public class SplayTreeRankMap<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Rank)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRankMap<KeyType, ValueType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Rank, Feature.RankMulti)]*//*[Widen]*/INonInvasiveMultiRankMapInspection,

        IEnumerable<EntryRankMap<KeyType, ValueType>>,
        IEnumerable

        where KeyType : IComparable<KeyType>
    {
        //
        // Object form data structure
        //

        [Storage(Storage.Object)]
        private sealed class Node
        {
            public Node left;
            public Node right;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;
            [Payload(Payload.Value)]
            public ValueType value;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;

            public static Node CreateNil()
            {
                Node nil = new Node();
                nil.left = nil;
                nil.right = nil;
                return nil;
            }

            public override string ToString()
            {
                if (this.IsNil)
                {
                    return "Nil";
                }

                string keyText = null;
                try
                {
                    keyText = key.ToString();
                }
                catch (NullReferenceException)
                {
                }

                string valueText = null;
                try
                {
                    valueText = value.ToString();
                }
                catch (NullReferenceException)
                {
                }

                string leftKeyText = null;
                try
                {
                    leftKeyText = left == null ? "null" : (((Node)left).IsNil ? "Nil" : ((Node)left).key.ToString());
                }
                catch (NullReferenceException)
                {
                }

                string rightKeyText = null;
                try
                {
                    rightKeyText = right == null ? "null" : (((Node)right).IsNil ? "Nil" : ((Node)right).key.ToString());
                }
                catch (NullReferenceException)
                {
                }

                return String.Format("({0})*{2}={3}*({1})", leftKeyText, rightKeyText, keyText, valueText);
            }

            private bool IsNil
            {
                get
                {
                    Debug.Assert((this == left) == (this == right));
                    return this == left;
                }
            }
        }

        // TODO: ensure fields of the Nil object are not written to, then make it a shared static.
        // (Offsets and left/right pointers never change, but there is a chance there are no-op writes to them during the
        // processing. If so, it can't be shared since it would incur a large penalty in concurrent scenarios.)
        [Storage(Storage.Object)]
        private readonly Node Nil = Node.CreateNil();
        [Storage(Storage.Object)]
        private readonly Node N = new Node();
        //[Storage(Storage.Array)]
        //private Node2[] nodes2;

        //
        // State for both array & object form
        //

        private Node root;
        [Count]
        private ulong count;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xExtent;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private ushort version;


        //
        // Construction
        //

        // Object

        /// <summary>
        /// Create a new collection based on a splay tree, explicitly configured.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys (present only for keyed collections)</param>
        /// <param name="capacity">
        /// For PreallocatedFixed mode, the maximum capacity of the tree, the memory for which is
        /// preallocated at construction time; exceeding that capacity will result in an OutOfMemory exception.
        /// For DynamicDiscard or DynamicRetainFreelist, the number of nodes to pre-allocate at construction time (the collection
        /// is permitted to exceed that capacity, in which case additional nodes will be allocated from the heap).
        /// For DynamicDiscard, nodes are unreferenced upon removal, allowing the garbage collector to reclaim the memory at any time.
        /// For DynamicRetainFreelist or PreallocatedFixed, upon removal nodes are returned to a free list from which subsequent
        /// nodes will be allocated.
        /// </param>
        /// <param name="allocationMode">The allocation mode (see capacity)</param>
        [Storage(Storage.Object)]
        public SplayTreeRankMap([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
        {
            this.comparer = comparer;
            root = Nil;

            this.allocationMode = allocationMode;
            this.freelist = Nil;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with the specified capacity and allocation mode and using
        /// the default comparer.
        /// </summary>
        /// <param name="capacity">
        /// For PreallocatedFixed mode, the maximum capacity of the tree, the memory for which is
        /// preallocated at construction time; exceeding that capacity will result in an OutOfMemory exception.
        /// For DynamicDiscard or DynamicRetainFreelist, the number of nodes to pre-allocate at construction time (the collection
        /// is permitted to exceed that capacity, in which case additional nodes will be allocated from the heap).
        /// For DynamicDiscard, nodes are unreferenced upon removal, allowing the garbage collector to reclaim the memory at any time.
        /// For DynamicRetainFreelist or PreallocatedFixed, upon removal nodes are returned to a free list from which subsequent
        /// nodes will be allocated.
        /// </param>
        /// <param name="allocationMode">The allocation mode (see capacity)</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTreeRankMap(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTreeRankMap([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public SplayTreeRankMap()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public SplayTreeRankMap(SplayTreeRankMap<KeyType, ValueType> original)
        {
            throw new NotImplementedException(); // TODO: clone
        }


        //
        // IOrderedMap, IOrderedList
        //

        
        /// <summary>
        /// Returns the number of key-value pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue key-value pairs.</exception>
        public uint Count { get { return checked((uint)this.count); } }

        
        /// <summary>
        /// Returns the number of key-value pairs in the collection.
        /// </summary>
        public long LongCount { get { return unchecked((long)this.count); } }

        
        /// <summary>
        /// Removes all key-value pairs from the collection.
        /// </summary>
        public void Clear()
        {
            // no need to do any work for DynamicDiscard mode
            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                // Since splay trees can be deep and thready, breadth-first is likely to use less memory than
                // depth-first. The worst-case is still N/2, so still risks being expensive.

                Queue<Node> queue = new Queue<Node>();
                if (root != Nil)
                {
                    queue.Enqueue(root);
                    while (queue.Count != 0)
                    {
                        Node node = queue.Dequeue();
                        if (node.left != Nil)
                        {
                            queue.Enqueue(node.left);
                        }
                        if (node.right != Nil)
                        {
                            queue.Enqueue(node.right);
                        }

                        this.count = unchecked(this.count - 1);
                        Free(node);
                    }
                }

                Debug.Assert(this.count == 0);
            }
            else
            {
                /*[Storage(Storage.Object)]*/
                {
#if DEBUG
                    allocateHelper.allocateCount = 0;
#endif
                }
            }

            root = Nil;
            this.count = 0;
            this.xExtent = 0;
        }

        
        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                return 0 == comparer.Compare(key, root.key);
            }
            return false;
        }

        
        /// <summary>
        /// Attempts to remove a key-value pair from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key-value pair was found and removed</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay(ref root, key);
                    int c = comparer.Compare(key, root.key);
                    if (c == 0)
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                        Splay(ref root.right, key);
                        /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                        /*[Widen]*/
                        int xLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;

                        Node dead, x;

                        dead = root;
                        if (root.left == Nil)
                        {
                            x = root.right;
                            if (x != Nil)
                            {
                                x.xOffset += root.xOffset - xLength;
                            }
                        }
                        else
                        {
                            x = root.left;
                            x.xOffset += root.xOffset;
                            Splay(ref x, key);
                            Debug.Assert(x.right == Nil);
                            if (root.right != Nil)
                            {
                                root.right.xOffset += root.xOffset - x.xOffset - xLength;
                            }
                            x.right = root.right;
                        }
                        root = x;

                        this.count = unchecked(this.count - 1);
                        this.xExtent = unchecked(this.xExtent - xLength);
                        Free(dead);

                        return true;
                    }
                }
                return false;
            }
        }

        
        /// <summary>
        /// Attempts to get the value associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <returns>true if they key was found</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetValue(KeyType key,out ValueType value)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                if (0 == comparer.Compare(key, root.key))
                {
                    value = root.value;
                    return true;
                }
            }
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Attempts to set the value associated with a key in the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <returns>true if the key-value pair was found and the value was updated</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetValue(KeyType key,ValueType value)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                if (0 == comparer.Compare(key, root.key))
                {
                    root.value = value;
                    return true;
                }
            }
            return false;
        }

        
        /// <summary>
        /// Removes a key-value pair from the collection.
        /// </summary>
        /// <param name="key">key of the key-value pair to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void Remove(KeyType key)
        {
            if (!TryRemove(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public ValueType GetValue(KeyType key)
        {
            ValueType value;
            if (!TryGetValue(key, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        
        /// <summary>
        /// Updates the value associated with a key in the collection
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">replacement value to associate with the key</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void SetValue(KeyType key,ValueType value)
        {
            if (!TrySetValue(key, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool LeastInternal(out KeyType keyOut,[Payload(Payload.Value)] out ValueType valueOut) // slow; use NearestGreaterOrEqual() if KeyType.MinValue is available
        {
            if (root != Nil)
            {
                Node node = root;
                KeyType least = node.key;
                while (node.left != Nil)
                {
                    node = node.left;
                    least = node.key;
                }
                Splay(ref root, least);
                keyOut = least;
                valueOut = root.value;
                return true;
            }
            keyOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the lowest in the collection (in sort order) and the value associated with it.
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType keyOut,[Payload(Payload.Value)] out ValueType valueOut) // slow; use NearestGreaterOrEqual() if KeyType.MinValue is available
        {
            return LeastInternal(out keyOut, /*[Payload(Payload.Value)]*/out valueOut);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType keyOut) // slow; use NearestGreaterOrEqual() if KeyType.MinValue is available
        {
            ValueType value;
            return LeastInternal(out keyOut, /*[Payload(Payload.Value)]*/out value);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private  bool GreatestInternal(out KeyType keyOut,[Payload(Payload.Value)] out ValueType valueOut) // slow; use NearestLessOrEqual() if KeyType.MaxValue is available
        {
            if (root != Nil)
            {
                Node node = root;
                KeyType greatest = node.key;
                while (node.right != Nil)
                {
                    node = node.right;
                    greatest = node.key;
                }
                Splay(ref root, greatest);
                keyOut = greatest;
                valueOut = root.value;
                return true;
            }
            keyOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order) and the value associated with it.
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <param name="value">out parameter receiving the value associated with the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut,[Payload(Payload.Value)] out ValueType valueOut) // slow; use NearestLessOrEqual() if KeyType.MaxValue is available
        {
            return GreatestInternal(out keyOut, /*[Payload(Payload.Value)]*/out valueOut);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut) // slow; use NearestLessOrEqual() if KeyType.MaxValue is available
        {
            ValueType value;
            return GreatestInternal(out keyOut, /*[Payload(Payload.Value)]*/out value);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestLess(KeyType key,out KeyType nearestKey,[Payload(Payload.Value)] out ValueType valueOut,bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                int rootComparison = comparer.Compare(key, root.key);
                if ((rootComparison > 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = root.key;
                    valueOut = root.value;
                    return true;
                }
                else if (root.left != Nil)
                {
                    KeyType rootKey = root.key;
                    Splay(ref root.left, rootKey);
                    nearestKey = root.left.key;
                    valueOut = root.left.value;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey,[Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey)
        {
            ValueType value;
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey,[Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, false/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey)
        {
            ValueType value;
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, false/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestGreater(KeyType key,out KeyType nearestKey,[Payload(Payload.Value)] out ValueType valueOut,bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                int rootComparison = comparer.Compare(key, root.key);
                if ((rootComparison < 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = root.key;
                    valueOut = root.value;
                    return true;
                }
                else if (root.right != Nil)
                {
                    KeyType rootKey = root.key;
                    Splay(ref root.right, rootKey);
                    nearestKey = root.right.key;
                    valueOut = root.right.value;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey,[Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key and
        /// the value associated with it.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="value">out parameter receiving the value associated with the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey,[Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, false/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, false/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int GetExtent()
        {
            return this.xExtent;
        }


        //
        // IRankMap, IMultiRankMap, IRankList, IMultiRankList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public int RankCount { get { return GetExtent(); } }

        // ContainsKey() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Attempts to add a key-value pair to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <returns>true if the key-value pair was added; false if the key was already present</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryAdd(KeyType key,[Payload(Payload.Value)] ValueType value)
        {
            unchecked
            {

                Splay(ref root, key);
                int c;
                if ((root == Nil) || ((c = comparer.Compare(key, root.key)) < 0))
                {
                    // insert item just in front of root

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + 1);

                    Node i = Allocate();
                    i.key = key;
                    i.value = value;
                    i.xOffset = root.xOffset;

                    i.left = root.left;
                    i.right = root;
                    if (root != Nil)
                    {
                        root.xOffset = 1;
                        root.left = Nil;
                    }

                    root = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;

                    return true;
                }
                else if (c > 0)
                {
                    // insert item just after root

                    Debug.Assert(root != Nil);

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + 1);

                    Node i = Allocate();
                    i.key = key;
                    i.value = value;
                    /*[Widen]*/
                    int rootLength = 1;

                    i.xOffset = root.xOffset + rootLength;
                    if (root.right != Nil)
                    {
                        root.right.xOffset += -rootLength + 1;
                    }
                    root.xOffset = -rootLength;

                    i.left = root;
                    i.right = root.right;
                    root.right = Nil;

                    root = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;

                    return true;
                }
                else
                {
                    Debug.Assert(c == 0);
                    return false;
                }
            }
        }

        // TryRemove() - reuses Feature.Dict implementation

        // TryGetValue() - reuses Feature.Dict implementation

        // TrySetValue() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Attempts to get the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGet(KeyType key,[Payload(Payload.Value)] out ValueType value,[Widen] out int rank)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay(ref root, key);
                    if (0 == comparer.Compare(key, root.key))
                    {
                        rank = root.xOffset;
                        value = root.value;
                        return true;
                    }
                }
                rank = 0;
                value = default(ValueType);
                return false;
            }
        }

        
        /// <summary>
        /// Attempts to return the key of a key-value pair at the specified rank index.
        /// If all key-value pairs in the collection were converted to a sorted array, this would be the equivalent of array[rank].Key.
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <param name="key">the key located at that index</param>
        /// <returns>true if there is an element at the the specified index</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGetKeyByRank([Widen] int rank,out KeyType key)
        {
            unchecked
            {
                if (rank < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (root != Nil)
                {
                    Splay2(ref root, rank);
                    if (rank < Start(root))
                    {
                        Debug.Assert(rank >= 0);
                        Debug.Assert(root.left != Nil); // because rank >= 0 and tree starts at 0
                        Splay2(ref root.left, 0);
                        key = root.left.key;
                        return true;
                    }

                    Splay2(ref root.right, 0);
                    Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                    /*[Widen]*/
                    int length = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;
                    if (/*(rank >= Start(root, Side.X)) && */(rank < Start(root) + length))
                    {
                        Debug.Assert(rank >= Start(root));
                        key = root.key;
                        return true;
                    }
                }
                key = default(KeyType);
                return false;
            }
        }

        
        /// <summary>
        /// Adds a key-value pair to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <param name="value">value to associate with the key</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Add(KeyType key,[Payload(Payload.Value)] ValueType value)
        {
            if (!TryAdd(key, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        // Remove() - reuses Feature.Dict implementation

        // GetValue() - reuses Feature.Dict implementation

        // SetValue() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Retrieves the value and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="value">out parameter that returns the value associated with the key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Get(KeyType key,[Payload(Payload.Value)] out ValueType value,[Widen] out int rank)
        {
            if (!TryGet(key, /*[Payload(Payload.Value)]*/out value, out rank))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the key of a key-value pair at the specified rank index.
        /// If all key-value pairs in the collection were converted to a sorted array, this would be the equivalent of array[rank].Key.
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public KeyType GetKeyByRank([Widen] int rank)
        {
            KeyType key;
            if (!TryGetKeyByRank(rank, out key))
            {
                throw new ArgumentException("index not in tree");
            }
            return key;
        }

        
        /// <summary>
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// For a RankMap, the only valid values are 0 (which does nothing) and -1 (which removes the key-value pair).
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public void AdjustCount(KeyType key,[Widen] int countAdjust)
        {
            unchecked
            {
                Splay(ref root, key);
                int c;
                if ((root != Nil) && ((c = comparer.Compare(key, root.key)) == 0))
                {
                    // update and possibly remove

                    Splay(ref root.right, key);
                    Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                    /*[Widen]*/
                    int oldLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;

                    /*[Widen]*/
                    int adjustedLength = checked(oldLength + countAdjust);
                    if (adjustedLength > 0)
                    {
                        /*[Feature(Feature.Rank)]*/
                        if (adjustedLength > 1)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        this.xExtent = checked(this.xExtent + countAdjust);

                        if (root.right != Nil)
                        {
                            unchecked
                            {
                                root.right.xOffset += countAdjust;
                            }
                        }
                    }
                    else if (oldLength + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        Node dead, x;

                        dead = root;
                        if (root.left == Nil)
                        {
                            x = root.right;
                            if (x != Nil)
                            {
                                Debug.Assert(root.xOffset == 0);
                                x.xOffset = 0; //nodes[x].xOffset = nodes[root].xOffset;
                            }
                        }
                        else
                        {
                            x = root.left;
                            x.xOffset += root.xOffset;
                            Splay(ref x, key);
                            Debug.Assert(x.right == Nil);
                            if (root.right != Nil)
                            {
                                root.right.xOffset += root.xOffset - x.xOffset + countAdjust;
                            }
                            x.right = root.right;
                        }
                        root = x;

                        this.count = unchecked(this.count - 1);
                        this.xExtent = unchecked(this.xExtent - oldLength);
                        Free(dead);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // add

                    if (countAdjust < 0)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    else if (countAdjust > 0)
                    {
                        /*[Feature(Feature.Rank)]*/
                        if (countAdjust > 1)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        // TODO: suboptimal - inline Add and remove duplicate Splay()

                        Add(key, /*[Payload(Payload.Value)]*/default(ValueType));
                    }
                    else
                    {
                        // allow non-adding case
                        Debug.Assert(countAdjust == 0);
                    }
                }
            }
        }


        //
        // Internals
        //

        // Object allocation

        [Storage(Storage.Object)]
        private struct AllocateHelper // hack for Roslyn since member removal corrupts following conditional directives
        {
#if DEBUG
            [Count]
            public ulong allocateCount;
#endif
        }
        [Storage(Storage.Object)]
        private AllocateHelper allocateHelper;

        [Storage(Storage.Object)]
        private Node Allocate()
        {
            Node node = freelist;
            if (node != Nil)
            {
                freelist = freelist.left;
            }
            else if (allocationMode == AllocationMode.PreallocatedFixed)
            {
                const string Message = "Tree capacity exhausted but is locked";
                throw new OutOfMemoryException(Message);
            }
            else
            {
                node = new Node();
            }

            {
#if DEBUG
                allocateHelper.allocateCount = checked(allocateHelper.allocateCount + 1);
#endif
            }

            return node;
        }

        [Storage(Storage.Object)]
        private void Free(Node node)
        {
#if DEBUG
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == count);

            node.left = null;
            node.right = null;
            node.xOffset = Int32.MinValue;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                node.key = default(KeyType); // clear any references for GC
                node.value = default(ValueType); // clear any references for GC

                node.left = freelist;
                freelist = node;
            }
        }

        [Storage(Storage.Object)]
        private void EnsureFree(uint capacity)
        {
            unchecked
            {
                Debug.Assert(freelist == Nil);
                for (uint i = 0; i < capacity - this.count; i++)
                {
                    Node node = new Node();
                    node.left = freelist;
                    freelist = node;
                }
            }
        }


        // http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c
        //
        //            An implementation of top-down splaying
        //                D. Sleator <sleator@cs.cmu.edu>
        //                         March 1992
        //
        //"Splay trees", or "self-adjusting search trees" are a simple and
        //efficient data structure for storing an ordered set. The data
        //structure consists of a binary tree, without parent pointers, and no
        //additional fields. It allows searching, insertion, deletion,
        //deletemin, deletemax, splitting, joining, and many other operations,
        //all with amortized logarithmic performance. Since the trees adapt to
        //the sequence of requests, their performance on real access patterns is
        //typically even better. Splay trees are described in a number of texts
        //and papers [1,2,3,4,5].
        //
        //The code here is adapted from simple top-down splay, at the bottom of
        //page 669 of [3]. It can be obtained via anonymous ftp from
        //spade.pc.cs.cmu.edu in directory /usr/sleator/public.
        //
        //The chief modification here is that the splay operation works even if the
        //item being splayed is not in the tree, and even if the tree root of the
        //tree is NULL. So the line:
        //
        //                          t = splay(i, t);
        //
        //causes it to search for item with key i in the tree rooted at t. If it's
        //there, it is splayed to the root. If it isn't there, then the node put
        //at the root is the last one before NULL that would have been reached in a
        //normal binary search for i. (It's a neighbor of i in the tree.) This
        //allows many other operations to be easily implemented, as shown below.
        //
        //[1] "Fundamentals of data structures in C", Horowitz, Sahni,
        //   and Anderson-Freed, Computer Science Press, pp 542-547.
        //[2] "Data Structures and Their Algorithms", Lewis and Denenberg,
        //   Harper Collins, 1991, pp 243-251.
        //[3] "Self-adjusting Binary Search Trees" Sleator and Tarjan,
        //   JACM Volume 32, No 3, July 1985, pp 652-686.
        //[4] "Data Structure and Algorithm Analysis", Mark Weiss,
        //   Benjamin Cummins, 1992, pp 119-130.
        //[5] "Data Structures, Algorithms, and Performance", Derick Wood,
        //   Addison-Wesley, 1993, pp 367-375.

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        [EnableFixed]
        private void Splay(ref Node root,KeyType leftComparand)
        {
            unchecked
            {
                this.version = unchecked((ushort)(this.version + 1));

                if (root == Nil)
                {
                    return;
                }

                Node t = root;

                N.left = Nil;
                N.right = Nil;

                Node l = N;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int lxOffset = 0;
                Node r = N;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int rxOffset = 0;

                while (true)
                {
                    int c;

                    c = comparer.Compare(leftComparand, t.key);
                    if (c < 0)
                    {
                        if (t.left == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, t.left.key);
                        if (c < 0)
                        {
                            // rotate right
                            Node u = t.left;
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            if (u.right != Nil)
                            {
                                u.right.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
                            t.left = u.right;
                            u.right = t;
                            t = u;
                            if (t.left == Nil)
                            {
                                break;
                            }
                        }
                        // link right
                        Debug.Assert(t.left != Nil);
                        t.left.xOffset += t.xOffset;
                        t.xOffset -= rxOffset;
                        r.left = t;
                        r = t;
                        rxOffset += r.xOffset;
                        t = t.left;
                    }
                    else if (c > 0)
                    {
                        if (t.right == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, t.right.key);
                        if (c > 0)
                        {
                            // rotate left
                            Node u = t.right;
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            if (u.left != Nil)
                            {
                                u.left.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
                            t.right = u.left;
                            u.left = t;
                            t = u;
                            if (t.right == Nil)
                            {
                                break;
                            }
                        }
                        // link left
                        Debug.Assert(t.right != Nil);
                        t.right.xOffset += t.xOffset;
                        t.xOffset -= lxOffset;
                        l.right = t;
                        l = t;
                        lxOffset += l.xOffset;
                        t = t.right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                l.right = t.left;
                if (l.right != Nil)
                {
                    l.right.xOffset += t.xOffset - lxOffset;
                }
                r.left = t.right;
                if (r.left != Nil)
                {
                    r.left.xOffset += t.xOffset - rxOffset;
                }
                t.left = N.right;
                if (t.left != Nil)
                {
                    t.left.xOffset -= t.xOffset;
                }
                t.right = N.left;
                if (t.right != Nil)
                {
                    t.right.xOffset -= t.xOffset;
                }
                root = t;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Widen]
        private int Start(Node n)
        {
            return n.xOffset;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [EnableFixed]
        private void Splay2(ref Node root,[Widen] int position)
        {
            unchecked
            {
                this.version = unchecked((ushort)(this.version + 1));

                if (root == Nil)
                {
                    return;
                }

                Node t = root;

                N.left = Nil;
                N.right = Nil;

                Node l = N;
                /*[Widen]*/
                int lxOffset = 0;
                Node r = N;
                /*[Widen]*/
                int rxOffset = 0;

                while (true)
                {
                    int c;

                    c = position.CompareTo(t.xOffset);
                    if (c < 0)
                    {
                        if (t.left == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(t.xOffset + t.left.xOffset);
                        if (c < 0)
                        {
                            // rotate right
                            Node u = t.left;
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            if (u.right != Nil)
                            {
                                u.right.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
                            t.left = u.right;
                            u.right = t;
                            t = u;
                            if (t.left == Nil)
                            {
                                break;
                            }
                        }
                        // link right
                        Debug.Assert(t.left != Nil);
                        t.left.xOffset += t.xOffset;
                        t.xOffset -= rxOffset;
                        r.left = t;
                        r = t;
                        rxOffset += r.xOffset;
                        t = t.left;
                    }
                    else if (c > 0)
                    {
                        if (t.right == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo((t.xOffset + t.right.xOffset));
                        if (c > 0)
                        {
                            // rotate left
                            Node u = t.right;
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            if (u.left != Nil)
                            {
                                u.left.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
                            t.right = u.left;
                            u.left = t;
                            t = u;
                            if (t.right == Nil)
                            {
                                break;
                            }
                        }
                        // link left
                        Debug.Assert(t.right != Nil);
                        t.right.xOffset += t.xOffset;
                        t.xOffset -= lxOffset;
                        l.right = t;
                        l = t;
                        lxOffset += l.xOffset;
                        t = t.right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                l.right = t.left;
                if (l.right != Nil)
                {
                    l.right.xOffset += t.xOffset - lxOffset;
                }
                r.left = t.right;
                if (r.left != Nil)
                {
                    r.left.xOffset += t.xOffset - rxOffset;
                }
                t.left = N.right;
                if (t.left != Nil)
                {
                    t.left.xOffset -= t.xOffset;
                }
                t.right = N.left;
                if (t.right != Nil)
                {
                    t.right.xOffset -= t.xOffset;
                }
                root = t;
            }
        }


        //
        // Non-invasive tree inspection support
        //

        // Helpers

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ValidateRanges()
        {
            if (root != Nil)
            {
                Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int offset = 0;
                /*[Widen]*/
                int leftEdge = 0;
                /*[Widen]*/
                int rightEdge = this.xExtent;

                Node node = root;
                while (node != Nil)
                {
                    offset += node.xOffset;
                    stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                    rightEdge = offset;
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    offset = t.Item2;
                    leftEdge = t.Item3;
                    rightEdge = t.Item4;

                    if ((offset < leftEdge) || (offset >= rightEdge))
                    {
                        throw new InvalidOperationException("range containment invariant");
                    }

                    leftEdge = offset + 1;
                    node = node.right;
                    while (node != Nil)
                    {
                        offset += node.xOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = node.left;
                    }
                }
            }
        }

        // INonInvasiveTreeInspection

        object INonInvasiveTreeInspection.Root { get { return root != Nil ? (object)root : null; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left != Nil ? (object)n.left : null;
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            Node n = (Node)node;
            return n.right != Nil ? (object)n.right : null;
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            Node n = (Node)node;
            object key = null;
            key = n.key;
            return key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            Node n = (Node)node;
            object value = null;
            value = n.value;
            return value;
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            return null;
        }

        void INonInvasiveTreeInspection.Validate()
        {
            if (root != Nil)
            {
                Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
                Queue<Node> worklist = new Queue<Node>();
                worklist.Enqueue(root);
                while (worklist.Count != 0)
                {
                    Node node = worklist.Dequeue();

                    if (visited.ContainsKey(node))
                    {
                        throw new InvalidOperationException("cycle");
                    }
                    visited.Add(node, false);

                    if (node.left != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(node.left.key, node.key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(node.key, node.right.key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges();
        }

        // INonInvasiveMultiRankMapInspection

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        MultiRankMapEntry[] INonInvasiveMultiRankMapInspection.GetRanks()
        {
            /*[Widen]*/
            MultiRankMapEntry[] ranks = new MultiRankMapEntry[Count];
            int i = 0;

            if (root != Nil)
            {
                Stack<STuple<Node, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;

                Node node = root;
                while (node != Nil)
                {
                    xOffset += node.xOffset;
                    stack.Push(new STuple<Node, /*[Widen]*/int>(node, xOffset));
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;

                    object key = null;
                    key = node.key;
                    object value = null;
                    value = node.value;

                    /*[Widen]*/
                    ranks[i++] = new MultiRankMapEntry(key, new Range(xOffset, 0), value);

                    node = node.right;
                    while (node != Nil)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int>(node, xOffset));
                        node = node.left;
                    }
                }
                if (!(i == ranks.Length))
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                for (i = 1; i < ranks.Length; i++)
                {
                    if (!(ranks[i - 1].rank.start < ranks[i].rank.start))
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    ranks[i - 1].rank.length = ranks[i].rank.start - ranks[i - 1].rank.start;
                }

                ranks[i - 1].rank.length = this.xExtent - ranks[i - 1].rank.start;
            }

            return ranks;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        void INonInvasiveMultiRankMapInspection.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

            /// <summary>
            /// Get the default enumerator, which is the robust enumerator for splay trees.
            /// </summary>
            /// <returns></returns>
        public IEnumerator<EntryRankMap<KeyType, ValueType>> GetEnumerator()
        {
            // For splay trees, the default enumerator is Robust because the Fast enumerator is fragile
            // and potentially consumes huge amounts of memory. For clients that can handle it, the Fast
            // enumerator is available by explicitly calling GetFastEnumerable().
            return GetRobustEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Get the robust enumerator. The robust enumerator uses an internal key cursor and queries the tree using the NextGreater()
        /// method to advance the enumerator. This enumerator is robust because it tolerates changes to the underlying tree. If a key
        /// is inserted or removed and it comes before the enumerator�s current key in sorting order, it will have no affect on the
        /// enumerator. If a key is inserted or removed and it comes after the enumerator�s current key (i.e. in the portion of the
        /// collection the enumerator hasn�t visited yet), the enumerator will include the key if inserted or skip the key if removed.
        /// Because the enumerator queries the tree for each element it�s running time per element is O(lg N), or O(N lg N) to
        /// enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRankMap<KeyType, ValueType>>
        {
            private readonly SplayTreeRankMap<KeyType, ValueType> tree;

            public RobustEnumerableSurrogate(SplayTreeRankMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRankMap<KeyType, ValueType>> GetEnumerator()
            {
                return new RobustEnumerator(tree);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// Get the fast enumerator. The fast enumerator uses an internal stack of nodes to peform in-order traversal of the
        /// tree structure. Because it uses the tree structure, it is invalidated if the tree is modified by an insertion or
        /// deletion and will throw an InvalidOperationException when next advanced. For the Splay tree, all operations modify
        /// the tree structure, include queries, and will invalidate the enumerator. The complexity of the fast enumerator
        /// is O(1) per element, or O(N) to enumerate the entire tree.
        /// 
        /// A note about splay trees and enumeration: Enumeration of splay trees is generally problematic, for two reasons.
        /// First, every operation on a splay tree modifies the structure of the tree, including queries. Second, splay trees
        /// may have depth of N in the worst case (as compared to other trees which are guaranteed to be less deep than
        /// approximately two times the optimal depth, or 2 lg N). The first property makes fast enumeration less useful, and
        /// the second property means fast enumeration may consume up to memory proportional to N for the internal stack used
        /// for traversal. Therefore, the robust enumerator is recommended for splay trees. The drawback is the
        /// robust enumerator�s O(N lg N) complexity.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryRankMap<KeyType, ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryRankMap<KeyType, ValueType>>
        {
            private readonly SplayTreeRankMap<KeyType, ValueType> tree;

            public FastEnumerableSurrogate(SplayTreeRankMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRankMap<KeyType, ValueType>> GetEnumerator()
            {
                return new FastEnumerator(tree);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// This enumerator is robust in that it can continue to walk the tree even in the face of changes, because
        /// it keeps a current key and uses NearestGreater to find the next one. The enumerator also uses a constant
        /// amount of memory. However, since it uses queries it is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator : IEnumerator<EntryRankMap<KeyType, ValueType>>
        {
            private readonly SplayTreeRankMap<KeyType, ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(SplayTreeRankMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRankMap<KeyType, ValueType> Current
            {
                get
                {

                    if (valid)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    {
                        KeyType key = currentKey;
                            ValueType value = default(ValueType);
                            /*[Widen]*/
                            int rank = 0;
                            /*[Widen]*/
                            int count = 1;
                            // OR
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            tree.Get(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey,
                                /*[Payload(Payload.Value)]*/out value,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/out rank);

                            /*[Feature(Feature.Rank)]*/
                            Debug.Assert(count == 1);

                            return new EntryRankMap<KeyType, ValueType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/rank);
                        }
                    }
                    return new EntryRankMap<KeyType, ValueType>();
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {

                if (!started)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    valid = tree.Least(out currentKey);

                    started = true;
                }
                else if (valid)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    valid = tree.NearestGreater(currentKey, out currentKey);
                }

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;
                currentKey = default(KeyType);
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any change to the tree invalidates it, and that *includes queries* since a query causes a splay
        /// operation that changes the structure of the tree.
        /// Worse, this enumerator also uses a stack that can be as deep as the tree, and since the depth of a splay
        /// tree is in the worst case n (number of nodes), the stack can potentially be size n.
        /// </summary>
        public class FastEnumerator : IEnumerator<EntryRankMap<KeyType, ValueType>>
        {
            private readonly SplayTreeRankMap<KeyType, ValueType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>>();

            public FastEnumerator(SplayTreeRankMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRankMap<KeyType, ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Nil)
                    {
                        /*[Feature(Feature.Rank)]*/
                        Debug.Assert(nextXStart - currentXStart == 1);

                        return new EntryRankMap<KeyType, ValueType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentNode.key,
                            /*[Payload(Payload.Value)]*/currentNode.value,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart);
                    }
                    return new EntryRankMap<KeyType, ValueType>();
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                Advance();
                return currentNode != tree.Nil;
            }

            public void Reset()
            {
                stack.Clear();
                currentNode = tree.Nil;
                nextNode = tree.Nil;
                currentXStart = 0;
                nextXStart = 0;

                this.version = tree.version;

                PushSuccessor(
                    tree.root,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0);

                Advance();
            }

            private void PushSuccessor(
                Node node,                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xPosition)
            {
                while (node != tree.Nil)
                {
                    xPosition += node.xOffset;

                    stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>(
                        node,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
                    node = node.left;
                }
            }

            private void Advance()
            {
                if (this.version != tree.version)
                {
                    throw new InvalidOperationException();
                }

                currentNode = nextNode;
                currentXStart = nextXStart;

                nextNode = tree.Nil;
                nextXStart = tree.xExtent;

                if (stack.Count == 0)
                {
                    nextXStart = tree.xExtent;
                    return;
                }

                STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;
                nextXStart = cursor.Item2;

                PushSuccessor(
                    nextNode.right,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart);
            }
        }
    }
}
