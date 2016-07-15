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

#pragma warning disable CS1572 // silence warning: XML comment has a param tag for '...', but there is no parameter by that name
#pragma warning disable CS1573 // silence warning: Parameter '...' has no matching param tag in the XML comment
#pragma warning disable CS1587 // silence warning: XML comment is not placed on a valid language element
#pragma warning disable CS1591 // silence warning: Missing XML comment for publicly visible type or member

//
// Implementation of top-down splay tree written by Daniel Sleator <sleator@cs.cmu.edu>.
// Taken from http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c
//
// An overview of splay trees can be found here: https://en.wikipedia.org/wiki/Splay_tree
//

namespace TreeLib
{

    /// <summary>
    /// Implements a map, list or range collection using a splay tree. 
    /// </summary>
    
    /// <summary>
    /// Represents an ordered key collection, augmented with multi-rank information. The rank of a key is the index it would
    /// be located in if all the keys in the tree were placed into a sorted array. Each key also has a count
    /// associated with it, which models sorted arrays containing multiple instances of a key. It is equivalent to the number of times
    /// the key appears in the array. Rank index values account for such multiple occurrences. In this case, the rank
    /// index is the index at which the first instance of a particular key would occur in a sorted array containing all keys.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public class SplayTreeMultiRankListLong<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType> :
        /*[Feature(Feature.RankMulti)]*//*[Payload(Payload.None)]*//*[Widen]*/IMultiRankListLong<KeyType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Rank, Feature.RankMulti)]*//*[Widen]*/INonInvasiveMultiRankMapInspectionLong,

        IEnumerable<EntryMultiRankListLong<KeyType>>,
        IEnumerable,
        ITreeEnumerable<EntryMultiRankListLong<KeyType>>,
        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/IKeyedTreeEnumerable<KeyType, EntryMultiRankListLong<KeyType>>,

        ICloneable

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

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;

            public static Node CreateNil()
            {
                Node nil = new Node();
                nil.left = nil;
                nil.right = nil;
                return nil;
            }
        }

        // TODO: ensure fields of the Nil object are not written to, then make it a shared static.
        // (Offsets and left/right pointers never change, but there is a chance there are no-op writes to them during the
        // processing. If so, it can't be shared since it would incur a large penalty in concurrent scenarios.)
        [Storage(Storage.Object)]
        private readonly Node Nil = Node.CreateNil();
        [Storage(Storage.Object)]
        private readonly Node N = new Node();

        //
        // State for both array & object form
        //

        private Node root;
        [Count]
        private ulong count;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private uint version;


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
        public SplayTreeMultiRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity, AllocationMode allocationMode)
        {
            this.comparer = comparer;
            this.root = Nil;

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
        public SplayTreeMultiRankListLong(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTreeMultiRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public SplayTreeMultiRankListLong()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public SplayTreeMultiRankListLong(SplayTreeMultiRankListLong<KeyType> original)
        {
            this.comparer = original.comparer;

            this.count = original.count;
            this.xExtent = original.xExtent;

            this.allocationMode = original.allocationMode;
            this.freelist = this.Nil;
            {
                Node nodeOriginal = original.freelist;
                while (nodeOriginal != original.Nil)
                {
                    nodeOriginal = nodeOriginal.left;
                    Node nodeCopy = new Node();
                    nodeCopy.left = this.freelist;
                    this.freelist = nodeCopy;
                }
            }
#if DEBUG
            this.allocateCount = original.allocateCount;
#endif

            // TODO: performance and memory usage
            // Cloning a Splay tree is problematic because of the worst-case O(N) depth. Here we are using a breadth-first
            // traversal to clone, as it is likely to use less memory in a typically "leggy" splay tree (vs. other types)
            // than depth-first. Need to determine if this is sufficient or should be parameterized to give caller control,
            // with the option of an O(N lg N) traversal instead.
            this.root = this.Nil;
            if (original.root != original.Nil)
            {
                this.root = new Node();

                Queue<STuple<Node, Node>> worklist = new Queue<STuple<Node, Node>>();
                worklist.Enqueue(new STuple<Node, Node>(this.root, original.root));
                while (worklist.Count != 0)
                {
                    STuple<Node, Node> item = worklist.Dequeue();

                    Node nodeThis = item.Item1;
                    Node nodeOriginal = item.Item2;

                    nodeThis.key = nodeOriginal.key;
                    nodeThis.xOffset = nodeOriginal.xOffset;
                    nodeThis.left = this.Nil;
                    nodeThis.right = this.Nil;

                    if (nodeOriginal.left != original.Nil)
                    {
                        nodeThis.left = new Node();
                        worklist.Enqueue(new STuple<Node, Node>(nodeThis.left, nodeOriginal.left));
                    }
                    if (nodeOriginal.right != original.Nil)
                    {
                        nodeThis.right = new Node();
                        worklist.Enqueue(new STuple<Node, Node>(nodeThis.right, nodeOriginal.right));
                    }
                }
            }
        }


        //
        // IOrderedMap, IOrderedList
        //

        
        /// <summary>
        /// Returns the number of keys in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue keys.</exception>
        public uint Count { get { return checked((uint)this.count); } }

        
        /// <summary>
        /// Returns the number of keys in the collection.
        /// </summary>
        public long LongCount { get { return unchecked((long)this.count); } }

        
        /// <summary>
        /// Removes all keys from the collection.
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
                /*[Storage(Storage.Object)]*/
                {
#if DEBUG
                    allocateCount = 0;
#endif
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
                Splay(ref root, key, comparer);
                return 0 == comparer.Compare(key, root.key);
            }
            return false;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool TrySetOrAddInternal(
            KeyType key,
            [Feature(Feature.RankMulti)][Const(1, Feature.Dict, Feature.Rank)][SuppressConst(Feature.RankMulti)][Widen]long rankCount,
            bool updateExisting)
        {
            unchecked
            {
                if (rankCount <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                Splay(ref root, key, comparer);
                int c = comparer.Compare(key, root.key);

                if ((root == Nil) || (c < 0))
                {
                    // insert item just in front of root

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + rankCount) ;

                    Node i = Allocate();
                    i.key = key;
                    i.xOffset = root.xOffset;

                    i.left = root.left;
                    i.right = root;
                    if (root != Nil)
                    {
                        root.xOffset = rankCount;
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
                    long xExtentNew = checked(this.xExtent + rankCount) ;

                    Node i = Allocate();
                    i.key = key;

                    /*[Feature(Feature.RankMulti)]*/
                    Splay(ref root.right, key, comparer);
                    /*[Feature(Feature.RankMulti)]*/
                    Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                    /*[Widen]*/
                    long rootLength = 1 ;
                    /*[Feature(Feature.RankMulti)]*/
                    rootLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;

                    i.xOffset = root.xOffset + rootLength;
                    if (root.right != Nil)
                    {
                        root.right.xOffset += -rootLength + rankCount;
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
                    if (updateExisting)
                        /*[Payload(Payload.None)]*/
                        {
                            Debug.Assert(0 == comparer.Compare(key, root.key));
                            root.key = key;
                        }
                    return false;
                }
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool TrySetOrRemoveInternal(
            KeyType key,
            bool updateExisting)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay(ref root, key, comparer);
                    int c = comparer.Compare(key, root.key);

                    if (c == 0)
                        {
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            Splay(ref root.right, key, comparer);
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            /*[Widen]*/
                            long xLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset ;

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
                                Splay(ref x, key, comparer);
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
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// The entire key is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return TrySetOrRemoveInternal(
                key,
                false/*updateExisting*/);
        }

        
        /// <summary>
        /// Attempts to get the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <returns>true if they key was found</returns>
        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key, out KeyType keyOut)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                if (0 == comparer.Compare(key, root.key))
                {
                    keyOut = root.key;
                    return true;
                }
            }
            keyOut = default(KeyType);
            return false;
        }

        
        /// <summary>
        /// Attempts to update the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetKey(KeyType key)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                if (0 == comparer.Compare(key, root.key))
                {
                    root.key = key;
                    return true;
                }
            }
            return false;
        }

        
        /// <summary>
        /// Removes a key from the collection. If the key is not present, no change is made to the collection.
        /// The entire key is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void Remove(KeyType key)
        {
            if (!TryRemove(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public KeyType GetKey(KeyType key)
        {
            KeyType keyOut;
            if (!TryGetKey(key, out keyOut))
            {
                throw new ArgumentException("item not in tree");
            }
            return keyOut;
        }

        
        /// <summary>
        /// Updates the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void SetKey(KeyType key)
        {
            if (!TrySetKey(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool LeastInternal(out KeyType keyOut)
        {
            if (root != Nil)
            {
                Splay(ref root, default(KeyType), FixedComparer.Minimum);
                keyOut = root.key;
                return true;
            }
            keyOut = default(KeyType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType keyOut)
        {
            return LeastInternal(out keyOut);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool GreatestInternal(out KeyType keyOut)
        {
            if (root != Nil)
            {
                Splay(ref root, default(KeyType), FixedComparer.Maximum);
                keyOut = root.key;
                return true;
            }
            keyOut = default(KeyType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut)
        {
            return GreatestInternal(out keyOut);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestLess(KeyType key,out KeyType nearestKey,bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                int rootComparison = comparer.Compare(key, root.key);
                if ((rootComparison > 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = root.key;
                    return true;
                }
                else if (root.left != Nil)
                {
                    KeyType rootKey = root.key;
                    Splay(ref root.left, rootKey, comparer);
                    nearestKey = root.left.key;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            return NearestLess(key, out nearestKey, true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank, [Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = 0;
            rankCount = 0;
            bool f = NearestLess(key, out nearestKey, true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            return NearestLess(key, out nearestKey, false/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank, [Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = 0;
            rankCount = 0;
            bool f = NearestLess(key, out nearestKey, false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestGreater(KeyType key,out KeyType nearestKey,bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                int rootComparison = comparer.Compare(key, root.key);
                if ((rootComparison < 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = root.key;
                    return true;
                }
                else if (root.right != Nil)
                {
                    KeyType rootKey = root.key;
                    Splay(ref root.right, rootKey, comparer);
                    nearestKey = root.right.key;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            return false;
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            return NearestGreater(key, out nearestKey, true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank, [Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = this.xExtent;
            rankCount = 0;
            bool f = NearestGreater(key, out nearestKey, true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            return NearestGreater(key, out nearestKey, false/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank, [Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = this.xExtent;
            rankCount = 0;
            bool f = NearestGreater(key, out nearestKey, false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] long start,[Widen] out long xLength)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start);
                    if (start == Start(root))
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        if (root.right != Nil)
                        {
                            xLength = root.right.xOffset;
                        }
                        else
                        {
                            xLength = this.xExtent - root.xOffset;
                        }
                        return true;
                    }
                }
                xLength = 0;
                return false;
            }
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestLess([Widen] long position,[Feature(Feature.RankMulti)] out KeyType nearestKey,[Widen] out long nearestStart, bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position);
                /*[Widen]*/
                long start = Start(root) ;
                if ((position < start) || (!orEqual && (position == start)))
                {
                    if (root.left != Nil)
                    {
                        Splay2(ref root.left, 0);
                        Debug.Assert(root.left.right == Nil);
                        nearestKey = root.left.key;
                        nearestStart = start + (root.left.xOffset);
                        return true;
                    }
                    nearestKey = default(KeyType);
                    nearestStart = 0;
                    return false;
                }
                else
                {
                    nearestKey = root.key;
                    nearestStart = Start(root);
                    return true;
                }
            }
            nearestKey = default(KeyType);
            nearestStart = 0;
            return false;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqualByRank([Widen] long position,[Feature(Feature.RankMulti)] out KeyType nearestKey,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            bool f = NearestLess(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqualByRank([Widen] long position,[Widen] out long nearestStart)
        {
            KeyType nearestKey;
            return NearestLess(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLessByRank([Widen] long position,[Feature(Feature.RankMulti)] out KeyType nearestKey,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            bool f = NearestLess(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLessByRank([Widen] long position,[Widen] out long nearestStart)
        {
            KeyType nearestKey;
            return NearestLess(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestGreater([Widen] long position,[Feature(Feature.RankMulti)] out KeyType nearestKey,[Widen] out long nearestStart, bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position);
                /*[Widen]*/
                long start = Start(root) ;
                if ((position > start) || (!orEqual && (position == start)))
                {
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert(root.right.left == Nil);
                        nearestKey = root.right.key;
                        nearestStart = start + (root.right.xOffset);
                        return true;
                    }
                    nearestKey = default(KeyType);
                    nearestStart = this.xExtent;
                    return false;
                }
                else
                {
                    nearestKey = root.key;
                    nearestStart = start;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            nearestStart = 0;
            return false;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqualByRank([Widen] long position,[Feature(Feature.RankMulti)] out KeyType nearestKey,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            bool f = NearestGreater(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqualByRank([Widen] long position,[Widen] out long nearestStart)
        {
            KeyType nearestKey;
            return NearestGreater(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreaterByRank([Widen] long position,[Feature(Feature.RankMulti)] out KeyType nearestKey,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            bool f = NearestGreater(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreaterByRank([Widen] long position,[Widen] out long nearestStart)
        {
            KeyType nearestKey;
            return NearestGreater(position, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
        }


        //
        // IRankMap, IMultiRankMap, IRankList, IMultiRankList
        //

        // Count { get; } - reuses Feature.Dict implementation

        
        /// <summary>
        /// Returns the total size of an array containing all keys, where each key occurs one or more times, determined by
        /// the 'count' associated with each key.
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public long RankCount { get { return this.xExtent; } }

        // ContainsKey() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Attempts to add a key to the collection.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="count">number of instances to repeat this key if the collection were converted to a sorted array</param>
        /// <returns>true if key was not present and key was added; false if key was already present</returns>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryAdd(KeyType key,[Feature(Feature.RankMulti)][Widen] long rankCount)
        {
            return TrySetOrAddInternal(
                key,
                /*[Feature(Feature.RankMulti)]*/rankCount,
                false/*updateExisting*/);
        }

        // TryRemove() - reuses Feature.Dict implementation

        // TryGetValue() - reuses Feature.Dict implementation

        // TrySetValue() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Attempts to get the actual key, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">out parameter that returns the actual key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key if the collection were converted to a sorted array</param>
        /// <returns>true if they key was found</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGet(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank, [Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay(ref root, key, comparer);
                    if (0 == comparer.Compare(key, root.key))
                    {
                        /*[Feature(Feature.RankMulti)]*/
                        Splay(ref root.right, key, comparer);
                        /*[Feature(Feature.RankMulti)]*/
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        rank = root.xOffset;
                        /*[Feature(Feature.RankMulti)]*/
                        rankCount = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;
                        keyOut = root.key;
                        return true;
                    }
                }
                rank = 0;
                /*[Feature(Feature.RankMulti)]*/
                rankCount = 0;
                keyOut = default(KeyType);
                return false;
            }
        }

        
        /// <summary>
        /// Attempts to update the key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for and also update</param>
        /// <param name="rank">the new rank count</param>
        /// <returns>true if they key was found and the rank was a valid value or false if the rank count was not at least 1
        /// or the sum of counts would have exceeded Int64.MaxValue</returns>
        [Feature(Feature.RankMulti)]
        public bool TrySet(KeyType key,[Widen] long rankCount)
        {
            unchecked
            {
                Splay(ref root, key, comparer);
                int c;
                if ((root != Nil) && ((c = comparer.Compare(key, root.key)) == 0))
                {
                    Splay(ref root.right, key, comparer);
                    Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                    /*[Widen]*/
                    long oldLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset ;

                    if (rankCount > 0)
                    {
                        /*[Widen]*/
                        long countAdjust = checked(rankCount - oldLength) ;
                        this.xExtent = checked(this.xExtent + countAdjust);

                        if (root.right != Nil)
                        {
                            unchecked
                            {
                                root.right.xOffset += countAdjust;
                            }
                        }
                        /*[Payload(Payload.None)]*/
                        root.key = key;

                        return true;
                    }
                }

                return false;
            }
        }

        
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
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGetKeyByRank([Widen] long rank, out KeyType key)
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
                    long length = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset ;
                    if (/*(rank >= Start(root, Side.X)) && */rank < Start(root) + length)
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
        /// Adds a key to the collection with an associated count.
        /// If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <param name="count">number of instances to repeat this key if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Add(KeyType key,[Feature(Feature.RankMulti)][Widen] long rankCount)
        {
            if (!TryAdd(key, /*[Feature(Feature.RankMulti)]*/rankCount))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        // Remove() - reuses Feature.Dict implementation

        // GetValue() - reuses Feature.Dict implementation

        // SetValue() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Retrieves the actual key, rank index, and count associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">out parameter that returns the actual key</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key</param>
        /// <param name="count">out parameter that returns the count, where count is the number of instances to repeat
        /// this key if the collection were converted to a sorted array</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Get(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank, [Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            if (!TryGet(key, /*[Payload(Payload.None)]*/out keyOut, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Updates the key and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for and also update</param>
        /// <param name="rank">the new rank count</param>
        /// <exception cref="ArgumentException">the rank count was not at least 1</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.RankMulti)]
        public void Set(KeyType key,[Widen] long rankCount)
        {
            if (rankCount < 1)
            {
                throw new ArgumentException("rankCount");
            }
            if (!TrySet(key, rankCount))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the key at the specified rank index. If all keys in the collection were
        /// converted to a sorted array of keys, this would be the equivalent of array[rank], subject to the
        /// constraint that only the first occurrence of each key can be indexed.
        /// </summary>
        /// <param name="rank">the rank index to query; the rank must be of the first occurrence of the key in a virtual
        /// sorted array where each key occurs 'count' times.</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public KeyType GetKeyByRank([Widen] long rank)
        {
            KeyType key;
            if (!TryGetKeyByRank(rank, out key))
            {
                throw new ArgumentException("index not in tree");
            }
            return key;
        }

        
        /// <summary>
        /// Adjusts the rank count associated with the key. The countAdjust added to the existing count.
        /// If the countAdjust is equal to the negative value of the current count, the key will be removed.
        /// </summary>
        /// <param name="key">key identifying the key to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <returns>The adjusted count</returns>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public long AdjustCount(KeyType key, [Widen] long countAdjust)
        {
            unchecked
            {
                Splay(ref root, key, comparer);
                int c;
                if ((root != Nil) && ((c = comparer.Compare(key, root.key)) == 0))
                {
                    // update and possibly remove

                    Splay(ref root.right, key, comparer);
                    Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                    /*[Widen]*/
                    long oldLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset ;

                    /*[Widen]*/
                    long adjustedLength = checked(oldLength + countAdjust) ;
                    if (adjustedLength > 0)
                    {

                        this.xExtent = checked(this.xExtent + countAdjust);

                        if (root.right != Nil)
                        {
                            unchecked
                            {
                                root.right.xOffset += countAdjust;
                            }
                        }

                        return adjustedLength;
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
                            Splay(ref x, key, comparer);
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

                        return 0;
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

                        // TODO: suboptimal - inline Add and remove duplicate Splay()

                        Add(key, /*[Feature(Feature.RankMulti)]*/countAdjust);

                        return countAdjust;
                    }
                    else
                    {
                        // allow non-adding case
                        Debug.Assert(countAdjust == 0);
                        return 0;
                    }
                }
            }
        }


        //
        // Internals
        //

        // Object allocation

#if DEBUG
        [Storage(Storage.Object)]
        [Count]
        private ulong allocateCount;
#endif

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
                allocateCount = checked(allocateCount + 1);
#endif
            }

            return node;
        }

        [Storage(Storage.Object)]
        private void Free(Node node)
        {
#if DEBUG
            allocateCount = checked(allocateCount - 1);
            Debug.Assert(allocateCount == count);

            node.left = null;
            node.right = null;
            node.xOffset = Int32.MinValue;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                node.key = default(KeyType); // clear any references for GC

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

        // use FixedComparer for finding the first or last in a tree
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private class FixedComparer : IComparer<KeyType>
        {
            private readonly int fixedResult;

            public readonly static FixedComparer Minimum = new FixedComparer(-1);
            public readonly static FixedComparer Maximum = new FixedComparer(1);

            public FixedComparer(int fixedResult)
            {
                this.fixedResult = fixedResult;
            }

            public int Compare(KeyType x, KeyType y)
            {
                return fixedResult;
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private void Splay(ref Node root,KeyType leftComparand, IComparer<KeyType> comparer)
        {
            unchecked
            {
                this.version = unchecked(this.version + 1);

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
                long lxOffset = 0 ;
                Node r = N;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long rxOffset = 0 ;

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
                            long uXPosition = t.xOffset + u.xOffset ;
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
                            long uXPosition = t.xOffset + u.xOffset ;
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
        private long Start(Node n)
        {
            return n.xOffset;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void Splay2(ref Node root,[Widen] long position)
        {
            unchecked
            {
                this.version = unchecked(this.version + 1);

                if (root == Nil)
                {
                    return;
                }

                Node t = root;

                N.left = Nil;
                N.right = Nil;

                Node l = N;
                /*[Widen]*/
                long lxOffset = 0 ;
                Node r = N;
                /*[Widen]*/
                long rxOffset = 0 ;

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
                            long uXPosition = t.xOffset + u.xOffset ;
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
                        c = position.CompareTo(t.xOffset + t.right.xOffset);
                        if (c > 0)
                        {
                            // rotate left
                            Node u = t.right;
                            /*[Widen]*/
                            long uXPosition = t.xOffset + u.xOffset ;
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
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long offset = 0 ;
                /*[Widen]*/
                long leftEdge = 0 ;
                /*[Widen]*/
                long rightEdge = this.xExtent ;

                Node node = root;
                while (node != Nil)
                {
                    offset += node.xOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                    rightEdge = offset;
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long> t = stack.Pop();
                    node = t.Item1;
                    offset = t.Item2;
                    leftEdge = t.Item3;
                    rightEdge = t.Item4;

                    Check.Assert((offset >= leftEdge) && (offset < rightEdge), "range containment invariant");

                    leftEdge = offset + 1;
                    node = node.right;
                    while (node != Nil)
                    {
                        offset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = node.left;
                    }
                }
            }
        }

        // INonInvasiveTreeInspection

        /// <summary>
        /// INonInvasiveTreeInspection.Root is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.Root { get { return root != Nil ? (object)root : null; } }

        /// <summary>
        /// INonInvasiveTreeInspection.GetLeftChild() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left != Nil ? (object)n.left : null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetRightChild() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            Node n = (Node)node;
            return n.right != Nil ? (object)n.right : null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetKey() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetKey(object node)
        {
            Node n = (Node)node;
            object key = null;
            key = n.key;
            return key;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetValue() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetValue(object node)
        {
            return null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetMetadata() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            return null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
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

                    Check.Assert(!visited.ContainsKey(node), "cycle");
                    visited.Add(node, false);

                    if (node.left != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        Check.Assert(comparer.Compare(node.left.key, node.key) < 0, "ordering invariant");
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        Check.Assert(comparer.Compare(node.key, node.right.key) < 0, "ordering invariant");
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges();
        }

        // INonInvasiveMultiRankMapInspection

        /// <summary>
        /// INonInvasiveMultiRankMapInspection.GetRanks() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        MultiRankMapEntryLong[] /*[Widen]*/INonInvasiveMultiRankMapInspectionLong.GetRanks()
        {
            /*[Widen]*/
            MultiRankMapEntryLong[] ranks = new /*[Widen]*/MultiRankMapEntryLong[Count];
            int i = 0;

            if (root != Nil)
            {
                Stack<STuple<Node,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long>>();

                /*[Widen]*/
                long xOffset = 0 ;

                Node node = root;
                while (node != Nil)
                {
                    xOffset += node.xOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long>(node, xOffset));
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node,/*[Widen]*/long> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;

                    object key = null;
                    key = node.key;
                    object value = null;

                    ranks[i++] = new /*[Widen]*/MultiRankMapEntryLong(key, new /*[Widen]*/RangeLong(xOffset, 0), value);

                    node = node.right;
                    while (node != Nil)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long>(node, xOffset));
                        node = node.left;
                    }
                }
                Check.Assert(i == ranks.Length, "count invariant");

                for (i = 1; i < ranks.Length; i++)
                {
                    Check.Assert(ranks[i - 1].rank.start < ranks[i].rank.start, "range sequence invariant");
                    ranks[i - 1].rank.length = ranks[i].rank.start - ranks[i - 1].rank.start;
                }

                ranks[i - 1].rank.length = this.xExtent - ranks[i - 1].rank.start;
            }

            return ranks;
        }

        /// <summary>
        /// INonInvasiveMultiRankMapInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti)]
        void /*[Widen]*/INonInvasiveMultiRankMapInspectionLong.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // IEnumerable
        //

        /// <summary>
        /// Get the default enumerator, which is the robust enumerator for splay trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryMultiRankListLong<KeyType>> GetEnumerator()
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

        //
        // ITreeEnumerable
        //

        
        /// <summary>
        /// Create a new instance of the default enumerator. Equivalent to IEnumerable&lt;&gt;.GetEnumerator()
        /// </summary>
        /// <returns>A new instance of the default enumerator</returns>
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the default enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the default enumerator</returns>
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable(bool forward)
        {
            return new RobustEnumerableSurrogate(this, forward);
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
        
        /// <summary>
        /// Create a new instance of the robust enumerator.
        /// </summary>
        /// <returns>A new instance of the robust enumerator</returns>
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable(bool forward)
        {
            return new RobustEnumerableSurrogate(this, forward);
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
        
        /// <summary>
        /// Create a new instance of the fast enumerator.
        /// </summary>
        /// <returns>A new instance of the fast enumerator</returns>
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        //
        // IKeyedTreeEnumerable
        //

        
        /// <summary>
        /// Create a new instance of the default enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the default enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable(KeyType startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/); // default
        }

        
        /// <summary>
        /// Create a new instance of the default enumerator, starting the enumeration at the specified key.
        /// </summary>
        /// <param name="startAt">The key to start enumeration at. If the key is not present in the collection, enumeration
        /// starts as follows: for forward enumeration, the next key higher in sort order; for reverse enumeration, the next lower
        /// (i.e. previous) key in sort order</param>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the default enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward); // default
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable(KeyType startAt)
        {
            return new FastEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator, starting the enumeration at the specified key.
        /// </summary>
        /// <param name="startAt">The key to start enumeration at. If the key is not present in the collection, enumeration
        /// starts as follows: for forward enumeration, the next key higher in sort order; for reverse enumeration, the next lower
        /// (i.e. previous) key in sort order</param>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, forward);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable(KeyType startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator, starting the enumeration at the specified key.
        /// </summary>
        /// <param name="startAt">The key to start enumeration at. If the key is not present in the collection, enumeration
        /// starts as follows: for forward enumeration, the next key higher in sort order; for reverse enumeration, the next lower
        /// (i.e. previous) key in sort order</param>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<EntryMultiRankListLong<KeyType>>
        {
            private readonly SplayTreeMultiRankListLong<KeyType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            // Construction

            public RobustEnumerableSurrogate(SplayTreeMultiRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerableSurrogate(SplayTreeMultiRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;
            }

            // IEnumerable

            public IEnumerator<EntryMultiRankListLong<KeyType>> GetEnumerator()
            {
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                if (startKeyed)
                {
                    return new RobustEnumerator(tree, startKey, forward);
                }

                return new RobustEnumerator(tree, forward);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryMultiRankListLong<KeyType>>
        {
            private readonly SplayTreeMultiRankListLong<KeyType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            // Construction

            public FastEnumerableSurrogate(SplayTreeMultiRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerableSurrogate(SplayTreeMultiRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;
            }

            // IEnumerable

            public IEnumerator<EntryMultiRankListLong<KeyType>> GetEnumerator()
            {
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                if (startKeyed)
                {
                    return new FastEnumerator(tree, startKey, forward);
                }

                return new FastEnumerator(tree, forward);
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
        public class RobustEnumerator :
            IEnumerator<EntryMultiRankListLong<KeyType>>        {
            private readonly SplayTreeMultiRankListLong<KeyType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            private bool started;
            private bool valid;
            private uint enumeratorVersion;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(SplayTreeMultiRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerator(SplayTreeMultiRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;

                Reset();
            }

            public EntryMultiRankListLong<KeyType> Current
            {
                get
                {

                    if (valid)
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        {
                            KeyType key = currentKey;
                            /*[Widen]*/
                            long rank = 0 ;
                            /*[Widen]*/
                            long count = 1 ;
                            // OR
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            tree.Get(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey,
                                /*[Payload(Payload.None)]*/out key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/out rank,
                                /*[Feature(Feature.RankMulti)]*/out count);

                            return new EntryMultiRankListLong<KeyType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/rank,
                                /*[Feature(Feature.RankMulti)]*/count);
                        }
                    return new EntryMultiRankListLong<KeyType>();
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

                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);

                if (!started)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    if (!startKeyed)
                    {
                        if (forward)
                        {
                            valid = tree.Least(out currentKey);
                        }
                        else
                        {
                            valid = tree.Greatest(out currentKey);
                        }
                    }
                    else
                    {
                        if (forward)
                        {
                            valid = tree.NearestGreaterOrEqual(startKey, out currentKey);
                        }
                        else
                        {
                            valid = tree.NearestLessOrEqual(startKey, out currentKey);
                        }
                    }

                    started = true;
                }
                else if (valid)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    if (forward)
                    {
                        valid = tree.NearestGreater(currentKey, out currentKey);
                    }
                    else
                    {
                        valid = tree.NearestLess(currentKey, out currentKey);
                    }
                }

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;
                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any change to the tree invalidates it, and that *includes queries* since a query causes a splay
        /// operation that changes the structure of the tree.
        /// Worse, this enumerator also uses a stack that can be as deep as the tree, and since the depth of a splay
        /// tree is in the worst case n (number of nodes), the stack can potentially be size n.
        /// </summary>
        public class FastEnumerator :
            IEnumerator<EntryMultiRankListLong<KeyType>>        {
            private readonly SplayTreeMultiRankListLong<KeyType> tree;
            private readonly bool forward;

            private readonly bool startKeyedOrIndexed;
            //
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            private uint treeVersion;
            private uint enumeratorVersion;

            private Node currentNode;
            private Node leadingNode;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart, nextXStart;
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long previousXStart;

            private STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>[] stack;
            private int stackIndex;

            public FastEnumerator(SplayTreeMultiRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerator(SplayTreeMultiRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startKey = startKey;

                Reset();
            }

            public EntryMultiRankListLong<KeyType> Current
            {
                get
                {
                    if (currentNode != tree.Nil)
                    {


                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long currentXLength = 0 ;

                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        if (forward)
                        {
                            currentXLength = nextXStart - currentXStart;
                        }
                        else
                        {
                            currentXLength = previousXStart - currentXStart;
                        }

                        return new EntryMultiRankListLong<KeyType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                            currentNode.key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXLength);
                    }
                    return new EntryMultiRankListLong<KeyType>();
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

            private void Push(STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long> item)
            {
                if (stackIndex >= stack.Length)
                {
                    Array.Resize(ref stack, stack.Length * 2);
                }
                stack[stackIndex++] = item;
            }

            private STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long> Pop()
            {
                return stack[--stackIndex];
            }

            public void Reset()
            {
                unchecked
                {
                    const int MinStackSize = 32;
                    int stackSize = Math.Max(MinStackSize, 2 * Log2.CeilLog2(tree.count)); // estimate of no theoretical significance, actual case is usually much worse
                    if ((stack == null) || (stackSize > stack.Length))
                    {
                        stack = new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>[
                            stackSize];
                    }
                    stackIndex = 0;

                    currentNode = tree.Nil;
                    leadingNode = tree.Nil;

                    this.treeVersion = tree.version;

                    // push search path to starting item

                    Node node = tree.root;
                    /*[Widen]*/
                    long xPosition = 0 ;
                    /*[Widen]*/
                    long yPosition = 0 ;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool foundMatch = false;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool lastGreaterAncestorValid = false;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xPositionLastGreaterAncestor = 0 ;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool successorValid = false;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xPositionSuccessor = 0 ;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long yPositionSuccessor = 0 ;
                    while (node != tree.Nil)
                    {
                        xPosition += node.xOffset;

                        bool foundMatch1 = false;
                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        foundMatch1 = foundMatch;

                        if (foundMatch1)
                        {
                            successorValid = true;
                            xPositionSuccessor = xPosition;
                            yPositionSuccessor = yPosition;
                        }

                        int c;
                        if (foundMatch1)
                        {
                            // don't compare anymore after finding match - only descend successor path
                            c = -1;
                        }
                        else
                        {
                            if (!startKeyedOrIndexed)
                            {
                                c = forward ? -1 : 1;
                            }
                            else
                            {
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                                c = tree.comparer.Compare(startKey, node.key);
                            }
                        }

                        if (!foundMatch1 && (forward && (c <= 0)) || (!forward && (c >= 0)))
                        {
                            Push(new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
                                node,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
                        }

                        if (c == 0)
                        {
                            foundMatch = true;

                            // successor not needed for forward traversal
                            if (forward)
                            {
                                break;
                            }
                        }

                        if (c < 0)
                        {
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            if (!foundMatch)
                            {
                                lastGreaterAncestorValid = true;
                                xPositionLastGreaterAncestor = xPosition;
                            }

                            node = node.left;
                        }
                        else
                        {
                            Debug.Assert(c >= 0);
                            node = node.right;
                        }
                    }

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    if (!forward)
                    {
                        // get position of successor for length of initial item
                        if (successorValid)
                        {
                            nextXStart = xPositionSuccessor;
                        }
                        else if (lastGreaterAncestorValid)
                        {
                            nextXStart = xPositionLastGreaterAncestor;
                        }
                        else
                        {
                            nextXStart = tree.xExtent;
                        }
                    }

                    Advance();
                }
            }

            private void Advance()
            {
                unchecked
                {
                    if (this.treeVersion != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

                    this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);

                    previousXStart = currentXStart;
                    currentNode = leadingNode;
                    currentXStart = nextXStart;

                    leadingNode = tree.Nil;

                    if (stackIndex == 0)
                    {
                        if (forward)
                        {
                            nextXStart = tree.xExtent;
                        }
                        else
                        {
                            nextXStart = 0;
                        }
                        return;
                    }

                    STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long> cursor
                        = Pop();

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;

                    Node node = forward ? leadingNode.right : leadingNode.left;
                    /*[Widen]*/
                    long xPosition = nextXStart ;
                    while (node != tree.Nil)
                    {
                        xPosition += node.xOffset;

                        Push(new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
                        node = forward ? node.left : node.right;
                    }
                }
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new SplayTreeMultiRankListLong<KeyType>(this);
        }
    }
}
