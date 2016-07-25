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
// This implementation is adapted from Glib's AVL tree: https://github.com/GNOME/glib/blob/master/glib/gtree.c
// which is attributed to Maurizio Monge.
// NOTE: this (and the original) is a threaded implementation
//
// An overview of AVL trees can be found here: https://en.wikipedia.org/wiki/AVL_tree
//

namespace TreeLib
{

    /// <summary>
    /// Implements a map, list or range collection using an AVL tree. 
    /// </summary>
    
    /// <summary>
    /// Represents an ordered key collection, augmented with rank information. The rank of a key is the index it would
    /// be located in if all the keys in the tree were placed into a sorted array.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public class AVLTreeRankListLong<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType> :
        /*[Feature(Feature.Rank)]*//*[Payload(Payload.None)]*//*[Widen]*/IRankListLong<KeyType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Rank, Feature.RankMulti)]*//*[Widen]*/INonInvasiveMultiRankMapInspectionLong,

        IEnumerable<EntryRankListLong<KeyType>>,
        IEnumerable,
        ITreeEnumerable<EntryRankListLong<KeyType>>,
        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/IKeyedTreeEnumerable<KeyType, EntryRankListLong<KeyType>>,

        ICloneable

        where KeyType : IComparable<KeyType>
    {
        //
        // Object form data structure
        //

        [Storage(Storage.Object)]
        private sealed class Node
        {
            public Node left, right;
            // non-threaded for augmented versions
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public bool left_child { get { return left != Null; } }
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public bool right_child { get { return right != Null; } }
            
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public Node leftOrNull { get { return left; } }
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public Node rightOrNull { get { return right; } }

            public sbyte balance;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;
        }

        [Storage(Storage.Object)]
        private static Node Null { get { return null; } }

        //
        // State for both array & object form
        //

        private Node root;
        [Count]
        private ulong count;
        private uint version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private const int MAX_GTREE_HEIGHT = 92;
        // 'path' is a stack of nodes used during insert and delete in lieu of recursion.
        // Rationale for weak reference:
        // - After insertion or deletion, 'path' will contain references to nodes, which may cause the garbage collector to keep
        //   alive arbitrary amounts of memory referenced from key/value field of now-dead nodes. It has been observed that zeroing
        //   the used parts of 'path' causes a 15% loss of performance. By making this weak, 'path' itself will be collected on
        //   the next GC, so the references do not cause memory leaks, and we can avoid having to zero 'path' after an operation.
        // - If the tree is infrequently used, the 'path' array does not need to be kept around. This is especially useful if there
        //   are many trees, as each tree instance has it's own instance of 'path'.
        // - It is very cheap to recreate and consumes only approx. 750 bytes.
        private readonly WeakReference<Node[]> path = new WeakReference<Node[]>(null);


        //
        // Construction
        //

        // Object

        /// <summary>
        /// Create a new collection based on an AVL tree, explicitly configured.
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
        public AVLTreeRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity, AllocationMode allocationMode)
        {
            this.comparer = comparer;
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with the specified capacity and allocation mode and using
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
        public AVLTreeRankListLong(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTreeRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public AVLTreeRankListLong()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public AVLTreeRankListLong(AVLTreeRankListLong<KeyType> original)
        {
            this.comparer = original.comparer;

            this.count = original.count;
            this.xExtent = original.xExtent;

            this.allocationMode = original.allocationMode;
            this.freelist = Null;
            {
                Node nodeOriginal = original.freelist;
                while (nodeOriginal != Null)
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

            this.root = Null;
            if (original.root != Null)
#if VALIDATE_CLONE_THREADING
                /*[Feature(Feature.Dict)]*/
                int originalNumber = 0;
                /*[Feature(Feature.Dict)]*/
                Dictionary<NodeRef, int> originalSequence = new Dictionary<NodeRef, int>();
                /*[Feature(Feature.Dict)]*/
                int thisNumber = 0;
                /*[Feature(Feature.Dict)]*/
                Dictionary<NodeRef, int> thisSequence = new Dictionary<NodeRef, int>();
#endif
                {
                    Stack<STuple<Node, Node, Node>> stack = new Stack<STuple<Node, Node, Node>>();

                    Node nodeOriginal = original.root;
                    Node nodeThis = this.root;
                    while (nodeOriginal != Null)
                    {
                        Node nodeChild = new Node();

                        if (this.root == Null)
                        {
                            this.root = nodeChild;
                        }
                        else
                        {
                            nodeThis.left = nodeChild;
                        }

                        Node nodeControllingSuccessor = Null;
                        nodeThis = nodeChild;
                        stack.Push(new STuple<Node, Node, Node>(nodeThis, nodeOriginal, nodeControllingSuccessor));
                        nodeOriginal = nodeOriginal.leftOrNull;
                    }
                    while (stack.Count != 0)
                    {
                        STuple<Node, Node, Node> t = stack.Pop();
                        nodeThis = t.Item1;
                        nodeOriginal = t.Item2;
                        Node nodeControllingSuccessorThis = Null;

                        nodeThis.key = nodeOriginal.key;
                        nodeThis.xOffset = nodeOriginal.xOffset;
                        nodeThis.balance = nodeOriginal.balance;

                        if (nodeOriginal.right_child)
                        {
                            Node nodeChild = new Node();
                            nodeThis.right = nodeChild;

                            nodeOriginal = nodeOriginal.right;
                            nodeThis = nodeChild;
                            stack.Push(new STuple<Node, Node, Node>(nodeThis, nodeOriginal, nodeControllingSuccessorThis));

                            nodeOriginal = nodeOriginal.leftOrNull;
                            while (nodeOriginal != Null)
                            {
                                nodeChild = new Node();
                                nodeThis.left = nodeChild;

                                Node nodeControllingSuccessor = Null;
                                nodeThis = nodeChild;
                                stack.Push(new STuple<Node, Node, Node>(nodeThis, nodeOriginal, nodeControllingSuccessor));
                                nodeOriginal = nodeOriginal.leftOrNull;
                            }
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
                
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                {
                    // non-recusrive depth-first traversal (in-order, but doesn't matter here)

                    Stack<Node> stack = new Stack<Node>();

                    Node node = root;
                    while (node != Null)
                    {
                        stack.Push(node);
                        node = node.leftOrNull;
                    }
                    while (stack.Count != 0)
                    {
                        node = stack.Pop();

                        Node dead = node;

                        node = node.rightOrNull;
                        while (node != Null)
                        {
                            stack.Push(node);
                            node = node.leftOrNull;
                        }

                        this.count = unchecked(this.count - 1);
                        g_node_free(dead);
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

            root = Null;
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
            Node node = g_tree_find_node(key);
            return node != Null;
        }

        
        /// <summary>
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
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
            Node node = g_tree_find_node(key);
            if (node != Null)
            {
                keyOut = node.key;
                return true;
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
            return g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                false/*add*/,
                true/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        
        /// <summary>
        /// Removes a key from the collection.
        /// </summary>
        /// <param name="key">key to remove</param>
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

        
        /// <summary>
        /// Conditionally update or add a key, based on the return value from the predicate.
        /// ConditionalSetOrAdd is more efficient when the decision to add or update depends on the value of the item.
        /// </summary>
        /// <param name="key">The key to update or add</param>
        /// <param name="predicate">The predicate to invoke. If the predicate returns true, the key will be added to the
        /// collection if it is not already in the collection. Whether true or false, if the key is in the collection, the
        /// ref key upon return will be used to update the key data.</param>
        /// <exception cref="ArgumentException">The sort order of the key was changed by the predicate</exception>
        /// <exception cref="InvalidOperationException">The tree was modified while the predicate was invoked. If this happens,
        /// the tree may be left in an unstable state.</exception>
        [Feature(Feature.Dict, Feature.Rank)]
        public void ConditionalSetOrAdd(KeyType key,[Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            /*[Payload(Payload.None)]*/
            if (predicateList == null)
            {
                throw new ArgumentNullException();
            }

            g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/1,
                false/*add - overridden by predicate*/,
                true/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/predicateList);
        }

        
        /// <summary>
        /// Conditionally update or add a key, based on the return value from the predicate.
        /// ConditionalSetOrRemove is more efficient when the decision to remove or update depends on the value of the item.
        /// </summary>
        /// <param name="key">The key to update or remove</param>
        /// <param name="predicate">The predicate to invoke. If the predicate returns true, the key will be removed from the
        /// collection if it is in the collection. If the key remains in the collection, the ref key upon return will be used
        /// to update the key data.</param>
        /// <exception cref="ArgumentException">The sort order of the key was changed by the predicate</exception>
        /// <exception cref="InvalidOperationException">The tree was modified while the predicate was invoked. If this happens,
        /// the tree may be left in an unstable state.</exception>
        [Feature(Feature.Dict, Feature.Rank)]
        public void ConditionalSetOrRemove(KeyType key,[Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            /*[Payload(Payload.None)]*/
            if (predicateList == null)
            {
                throw new ArgumentNullException();
            }

            g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/predicateList);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool LeastInternal(out KeyType keyOut)
        {
            Node node = g_tree_first_node();
            if (node == Null)
            {
                keyOut = default(KeyType);
                return false;
            }
            keyOut = node.key;
            return true;
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType keyOut)
        {
            return LeastInternal(out keyOut);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool GreatestInternal(out KeyType keyOut)
        {
            Node node = g_tree_last_node();
            if (node == Null)
            {
                keyOut = default(KeyType);
                return false;
            }
            keyOut = node.key;
            return true;
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut)
        {
            return GreatestInternal(out keyOut);
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
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank)
        {
            rank = 0;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank);
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
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank)
        {
            rank = 0;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
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
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank)
        {
            rank = this.xExtent;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank);
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
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <param name="rank">the rank of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank)
        {
            rank = this.xExtent;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, out rank);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }


        //
        // IRankMap, IMultiRankMap, IRankList, IMultiRankList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public long RankCount { get { return this.xExtent; } }

        // ContainsKey() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Attempts to add a key to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <returns>true if the key was added; false if the key was already present</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryAdd(KeyType key)
        {

            return g_tree_insert_internal(
                key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                1,
                true/*add*/,
                false/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        // TryRemove() - reuses Feature.Dict implementation

        // TryGetValue() - reuses Feature.Dict implementation

        // TrySetValue() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Attempts to get the actual key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGet(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank)
        {
            Node node;
            /*[Widen]*/
            long xPosition;
            if (Find(key, out node, out xPosition))
            {
                keyOut = node.key;
                rank = xPosition;
                return true;
            }
            keyOut = default(KeyType);
            rank = 0;
            return false;
        }

        
        /// <summary>
        /// Retrieves the key at the specified rank index.
        /// If all keys in the collection were converted to a sorted array, this would be the equivalent of array[rank].
        /// </summary>
        /// <param name="rank">the rank index to query</param>
        /// <param name="key">the key located at the specified rank index</param>
        /// <returns>the key located at that index</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGetKeyByRank([Widen] long rank, out KeyType key)
        {
            if (rank < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            long xPosition ;
            if (FindPosition(
rank,
out node,
out xPosition))
            {
                key = node.key;
                return true;
            }
            key = default(KeyType);
            return false;
        }

        
        /// <summary>
        /// Adds a key to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Add(KeyType key)
        {
            if (!TryAdd(key))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        // Remove() - reuses Feature.Dict implementation

        // GetValue() - reuses Feature.Dict implementation

        // SetValue() - reuses Feature.Dict implementation

        
        /// <summary>
        /// Retrieves the actual key data and rank index associated with a key in the collection.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <param name="rank">out pararmeter that returns the rank index associated with the key-value pair</param>
        /// <returns>true if they key was found</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Get(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank)
        {
            if (!TryGet(key, /*[Payload(Payload.None)]*/out keyOut, out rank))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the key at the specified rank index.
        /// If all keys in the collection were converted to a sorted array, this would be the equivalent of array[rank].
        /// </summary>
        /// <param name="rank">the rank index to query</param>
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
        /// Adjusts the rank count associated with the key-value pair. The countAdjust added to the existing count.
        /// For a RankMap, the only valid values are 0 (which does nothing) and -1 (which removes the key-value pair).
        /// </summary>
        /// <param name="key">key identifying the key-value pair to update</param>
        /// <param name="countAdjust">adjustment that is added to the count</param>
        /// <returns>The adjusted count</returns>
        /// <exception cref="ArgumentException">if the count is an invalid value or the key does not exist in the collection</exception>
        /// <exception cref="OverflowException">the sum of counts would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public long AdjustCount(KeyType key, [Widen] long countAdjust)
        {
            unchecked
            {
                Node node;
                /*[Widen]*/
                long xPosition ;
                if (Find(key, out node, out xPosition))
                {
                    // update and possibly remove

                    /*[Widen]*/
                    long adjustedLength = checked(1 + countAdjust) ;
                    if (adjustedLength > 0)
                    {
                        /*[Feature(Feature.Rank)]*/
                        if (adjustedLength > 1)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        this.xExtent = checked(this.xExtent + countAdjust);

                        ShiftRightOfPath(unchecked(xPosition + 1), countAdjust);

                        return adjustedLength;
                    }
                    else if (1 + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        this.g_tree_remove_internal(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                            /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);

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
                        /*[Feature(Feature.Rank)]*/
                        if (countAdjust > 1)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        Add(key);

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
        private Node g_tree_node_new([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
        {
            Node node = freelist;
            if (node != Null)
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

            node.key = key;
            node.left = Null;
            node.right = Null;
            node.balance = 0;
            node.xOffset = 0;

            return node;
        }

        [Storage(Storage.Object)]
        private void g_node_free(Node node)
        {
#if DEBUG
            allocateCount = checked(allocateCount - 1);
            Debug.Assert(allocateCount == this.count);

            node.left = Null;
            node.right = Null;
            node.balance = SByte.MinValue;
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
                Debug.Assert(freelist == Null);
                for (uint i = 0; i < capacity - this.count; i++)
                {
                    Node node = new Node();
                    node.left = freelist;
                    freelist = node;
                }
            }
        }


        private Node g_tree_first_node()
        {
            if (root == Null)
            {
                return Null;
            }

            Node tmp = root;

            while (tmp.left_child)
            {
                tmp = tmp.left;
            }

            return tmp;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private Node g_tree_last_node()
        {
            if (root == Null)
            {
                return Null;
            }

            Node tmp = root;

            while (tmp.right_child)
            {
                tmp = tmp.right;
            }

            return tmp;
        }

        private bool NearestLess(
            out Node nearestNode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,
            bool orEqual)
        {
            unchecked
            {
                Node lastLess = Null;
                /*[Widen]*/
                long xPositionLastLess = 0 ;
                /*[Widen]*/
                long yPositionLastLess = 0 ;
                if (root != Null)
                {
                    Node node = root;
                    /*[Widen]*/
                    long xPosition = 0 ;
                    /*[Widen]*/
                    long yPosition = 0 ;
                    while (true)
                    {
                        xPosition += node.xOffset;

                        int c;
                        {
                            c = comparer.Compare(key, node.key);
                        }
                        if (orEqual && (c == 0))
                        {
                            nearestNode = node;
                            nearestKey = node.key;
                            nearestStart = xPosition;
                            return true;
                        }
                        Node next;
                        if (c <= 0)
                        {
                            if (!node.left_child)
                            {
                                break;
                            }
                            next = node.left;
                        }
                        else
                        {
                            lastLess = node;
                            xPositionLastLess = xPosition;
                            yPositionLastLess = yPosition;

                            if (!node.right_child)
                            {
                                break;
                            }
                            next = node.right;
                        }
                        Debug.Assert(next != Null);
                        node = next;
                    }
                }
                if (lastLess != Null)
                {
                    nearestNode = lastLess;
                    nearestKey = lastLess.key;
                    nearestStart = xPositionLastLess;
                    return true;
                }
                nearestNode = Null;
                nearestKey = default(KeyType);
                nearestStart = 0;
                return false;
            }
        }

        private bool NearestGreater(
            out Node nearestNode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,
            bool orEqual)
        {
            unchecked
            {
                Node lastGreater = Null;
                /*[Widen]*/
                long xPositionLastGreater = 0 ;
                /*[Widen]*/
                long yPositionLastGreater = 0 ;
                if (root != Null)
                {
                    Node node = root;
                    /*[Widen]*/
                    long xPosition = 0 ;
                    /*[Widen]*/
                    long yPosition = 0 ;
                    while (true)
                    {
                        xPosition += node.xOffset;

                        int c;
                        {
                            c = comparer.Compare(key, node.key);
                        }
                        if (orEqual && (c == 0))
                        {
                            nearestNode = node;
                            nearestKey = node.key;
                            nearestStart = xPosition;
                            return true;
                        }
                        Node next;
                        if (c < 0)
                        {
                            lastGreater = node;
                            xPositionLastGreater = xPosition;
                            yPositionLastGreater = yPosition;

                            if (!node.left_child)
                            {
                                break;
                            }
                            next = node.left;
                        }
                        else
                        {
                            if (!node.right_child)
                            {
                                break;
                            }
                            next = node.right;
                        }
                        Debug.Assert(next != Null);
                        node = next;
                    }
                }
                if (lastGreater != Null)
                {
                    nearestNode = lastGreater;
                    nearestKey = lastGreater.key;
                    nearestStart = xPositionLastGreater;
                    return true;
                }
                nearestNode = Null;
                nearestKey = default(KeyType);
                nearestStart = this.xExtent;
                return false;
            }
        }

        private Node[] RetrievePathWorkspace()
        {
            Node[] path;
            this.path.TryGetTarget(out path);
            if (path == null)
            {
                path = new Node[MAX_GTREE_HEIGHT];
                this.path.SetTarget(path);
            }
            return path;
        }

        [Feature(Feature.Dict, Feature.Rank)]
        private bool PredicateAddRemoveOverrideCore(
            bool initial,
            bool resident,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]ref KeyType key,
            [Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            uint version = this.version;

            // very crude protection against completely trashing the tree if the predicate tries to modify it
            Node savedRoot = this.root;
            this.root = Null;
            /*[Count]*/
            ulong savedCount = this.count;
            this.count = 0;
            /*[Widen]*/
            long savedXExtent = this.xExtent ;
            this.xExtent = 0;
            try
                
                /*[Payload(Payload.None)]*/
                {
                    KeyType localKey = key;
                    initial = predicateList(ref localKey, resident);
                    if (0 != comparer.Compare(key, localKey))
                    {
                        throw new ArgumentException();
                    }
                    key = localKey;
                }
            finally
            {
                this.root = savedRoot;
                this.count = savedCount;
                this.xExtent = savedXExtent;
            }

            if (version != this.version)
            {
                throw new InvalidOperationException();
            }

            return initial;
        }

        [Feature(Feature.Dict, Feature.Rank)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PredicateAddRemoveOverride(
            bool initial,
            bool resident,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]ref KeyType key,
            [Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            bool predicateExists = false;
            /*[Payload(Payload.None)]*/
            predicateExists = predicateList != null;
            if (predicateExists)
            {
                initial = PredicateAddRemoveOverrideCore(
                    initial,
                    resident,
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ref key,
                    /*[Payload(Payload.None)]*/predicateList);
            }

            return initial;
        }

        // NOTE: replace mode does *not* adjust for xLength/yLength!
        private bool g_tree_insert_internal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xLength,
            bool add,
            bool update,
            [Feature(Feature.Dict, Feature.Rank)][Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            unchecked
            {
                if (root == Null)
                {
                    /*[Feature(Feature.Dict, Feature.Rank)]*/
                    add = PredicateAddRemoveOverride(
                        add,
                        false/*resident*/,
                        ref key,
                        /*[Payload(Payload.None)]*/predicateList);

                    if (!add)
                    {
                        return false;
                    }

                    root = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);
                    Debug.Assert(root.xOffset == 0);
                    Debug.Assert(this.xExtent == 0);
                    this.xExtent = xLength;

                    Debug.Assert(this.count == 0);
                    this.count = 1;
                    this.version = unchecked(this.version + 1);

                    return true;
                }

                Node[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;
                Node node = root;

                Node successor = Null;
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                /*[Widen]*/
                long yPositionSuccessor = 0 ;
                /*[Widen]*/
                long xPositionNode = 0 ;
                /*[Widen]*/
                long yPositionNode = 0 ;
                bool addleft = false;
                while (true)
                {
                    xPositionNode += node.xOffset;

                    int cmp;
                    {
                        cmp = comparer.Compare(key, node.key);
                    }

                    if (cmp == 0)
                    {
                        bool predicateExists = false;
                        /*[Payload(Payload.None)]*/
                        predicateExists = predicateList != null;
                        if (predicateExists)
                        {
                            /*[Feature(Feature.Dict, Feature.Rank)]*/
                            PredicateAddRemoveOverride(
                                false/*initial*/,
                                true/*resident*/,
                                ref key,
                                /*[Payload(Payload.None)]*/predicateList);
                        }

                        if (update)
                            /*[Payload(Payload.None)]*/
                            {
                                Debug.Assert(0 == comparer.Compare(node.key, key));
                                node.key = key;
                            }
                        return !add;
                    }

                    if (cmp < 0)
                    {
                        successor = node;
                        xPositionSuccessor = xPositionNode;
                        yPositionSuccessor = yPositionNode;

                        if (node.left_child)
                        {
                            path[idx++] = node;
                            node = node.left;
                        }
                        else
                        {
                            // precedes node

                            /*[Feature(Feature.Dict, Feature.Rank)]*/
                            add = PredicateAddRemoveOverride(
                                add,
                                false/*resident*/,
                                ref key,
                                /*[Payload(Payload.None)]*/predicateList);

                            if (!add)
                            {
                                return false;
                            }

                            addleft = true;
                            break;
                        }
                    }
                    else
                    {
                        Debug.Assert(cmp > 0);

                        if (node.right_child)
                        {
                            path[idx++] = node;
                            node = node.right;
                        }
                        else
                        {
                            // follows node

                            /*[Feature(Feature.Dict, Feature.Rank)]*/
                            add = PredicateAddRemoveOverride(
                                add,
                                false/*resident*/,
                                ref key,
                                /*[Payload(Payload.None)]*/predicateList);

                            if (!add)
                            {
                                return false;
                            }

                            addleft = false;
                            break;
                        }
                    }
                }

                if (addleft)
                {
                    // precedes node

                    Debug.Assert(node == successor);

                    this.version = unchecked(this.version + 1);

                    // throw here before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Node child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                    ShiftRightOfPath(xPositionNode, xLength);
                    node.left = child;
                    node.balance--;

                    child.xOffset = -xLength;

                    this.xExtent = xExtentNew;
                    this.count = countNew;
                }
                else
                {
                    // follows node

                    Debug.Assert(!node.right_child);

                    /*[Widen]*/
                    long xLengthNode ;
                    /*[Widen]*/
                    long yLengthNode ;
                    if (successor != Null)
                    {
                        xLengthNode = xPositionSuccessor - xPositionNode;
                        yLengthNode = yPositionSuccessor - yPositionNode;
                    }
                    else
                    {
                        xLengthNode = this.xExtent - xPositionNode;
                    }

                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    if ((CompareKeyMode.Key == CompareKeyMode.Position)
                        && (position != (xPositionNode + xLengthNode)))
                    {
                        return false;
                    }

                    this.version = unchecked(this.version + 1);

                    // throw here before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Node child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                    ShiftRightOfPath(xPositionNode + 1, xLength);
                    node.right = child;
                    node.balance++;

                    child.xOffset = xLengthNode;

                    this.xExtent = xExtentNew;
                    this.count = countNew;
                }

                // Restore balance. This is the goodness of a non-recursive
                // implementation, when we are done with balancing we 'break'
                // the loop and we are done.
                while (true)
                {
                    Node bparent = path[--idx];
                    bool left_node = (bparent != Null) && (node == bparent.left);
                    Debug.Assert((bparent == Null) || (bparent.left == node) || (bparent.right == node));

                    if ((node.balance < -1) || (node.balance > 1))
                    {
                        node = g_tree_node_balance(node);
                        if (bparent == Null)
                        {
                            root = node;
                        }
                        else if (left_node)
                        {
                            bparent.left = node;
                        }
                        else
                        {
                            bparent.right = node;
                        }
                    }

                    if ((node.balance == 0) || (bparent == Null))
                    {
                        break;
                    }

                    if (left_node)
                    {
                        bparent.balance--;
                    }
                    else
                    {
                        bparent.balance++;
                    }

                    node = bparent;
                }

                return true;
            }
        }

        private bool g_tree_remove_internal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Dict, Feature.Rank)][Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            unchecked
            {
                if (root == Null)
                {
                    return false;
                }

                Node[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;

                Node node = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionNode = 0 ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionParent = 0 ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionLastGreaterAncestor = 0 ;
                while (true)
                {
                    Debug.Assert(node != Null);

                    xPositionNode += node.xOffset;

                    int cmp;
                    {
                        cmp = comparer.Compare(key, node.key);
                    }

                    if (cmp == 0)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank)] */
                        {
                            bool remove = PredicateAddRemoveOverride(
                                true/*initial*/,
                                true/*resident*/,
                                ref key,
                                /*[Payload(Payload.None)]*/predicateList);

                            if (!remove)
                            {
                                /*[Payload(Payload.None)]*/
                                {
                                    Debug.Assert(0 == comparer.Compare(node.key, key));
                                    node.key = key;
                                }

                                return false;
                            }
                        }

                        break;
                    }

                    xPositionParent = xPositionNode;

                    if (cmp < 0)
                    {
                        if (!node.left_child)
                        {
                            return false;
                        }

                        lastGreaterAncestor = node;
                        xPositionLastGreaterAncestor = xPositionNode;

                        path[idx++] = node;
                        node = node.left;
                    }
                    else
                    {
                        if (!node.right_child)
                        {
                            return false;
                        }

                        path[idx++] = node;
                        node = node.right;
                    }
                }

                this.version = unchecked(this.version + 1);

                Node successor;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor ;

                // The following code is almost equal to g_tree_remove_node,
                // except that we do not have to call g_tree_node_parent.
                Node parent, balance;
                balance = parent = path[--idx];
                Debug.Assert((parent == Null) || (parent.left == node) || (parent.right == node));
                bool left_node = (parent != Null) && (node == parent.left);

                if (!node.left_child)
                {
                    if (!node.right_child) // node has no children
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = lastGreaterAncestor;
                            xPositionSuccessor = xPositionLastGreaterAncestor;
                        }

                        if (parent == Null)
                        {
                            root = Null;
                        }
                        else if (left_node)
                        {
                            
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            parent.left = Null;
                            parent.balance++;
                        }
                        else
                        {
                            
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            parent.right = Null;
                            parent.balance--;
                        }
                    }
                    else // node has a right child
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        xPositionSuccessor = xPositionNode;
                        
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = node.right;
                            xPositionSuccessor += successor.xOffset;
                            while (successor.left_child)
                            {
                                successor = successor.left;
                                xPositionSuccessor += successor.xOffset;
                            }
                        }

                        Node rightChild = node.right;
                        rightChild.xOffset += node.xOffset;
                        if (parent == Null)
                        {
                            root = rightChild;
                        }
                        else if (left_node)
                        {
                            parent.left = rightChild;
                            parent.balance++;
                        }
                        else
                        {
                            parent.right = rightChild;
                            parent.balance--;
                        }
                    }
                }
                else // node has a left child
                {
                    if (!node.right_child)
                    {

                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = lastGreaterAncestor;
                            xPositionSuccessor = xPositionLastGreaterAncestor;
                        }

                        Node leftChild = node.left;
                        leftChild.xOffset += node.xOffset;
                        if (parent == Null)
                        {
                            root = leftChild;
                        }
                        else if (left_node)
                        {
                            parent.left = leftChild;
                            parent.balance++;
                        }
                        else
                        {
                            parent.right = leftChild;
                            parent.balance--;
                        }
                    }
                    else // node has a both children (pant, pant!)
                    {
                        successor = node.right;
                        Node successorParent = node;
                        int old_idx = ++idx;
                        xPositionSuccessor = xPositionNode + successor.xOffset;

                        // path[idx] == parent
                        // find the immediately next node (and its parent)
                        while (successor.left_child)
                        {
                            path[++idx] = successorParent = successor;
                            successor = successor.left;

                            xPositionSuccessor += successor.xOffset;
                        }

                        path[old_idx] = successor;
                        balance = path[idx];

                        /* remove 'successor' from the tree */
                        if (successorParent != node)
                        {
                            if (successor.right_child)
                            {
                                Node successorRightChild = successor.right;

                                successorParent.left = successorRightChild;

                                successorRightChild.xOffset += successor.xOffset;
                            }
                            else
                            {
                                
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                successorParent.left = Null;
                            }
                            successorParent.balance++;
                            successor.right = node.right;

                            node.right.xOffset += xPositionNode - xPositionSuccessor;
                        }
                        else
                        {
                            node.balance--;
                        }

                        /* prepare 'successor' to replace 'node' */
                        Node leftChild = node.left;
                        successor.left = leftChild;
                        successor.balance = node.balance;
                        leftChild.xOffset += xPositionNode - xPositionSuccessor;

                        if (parent == Null)
                        {
                            root = successor;
                        }
                        else if (left_node)
                        {
                            parent.left = successor;
                        }
                        else
                        {
                            parent.right = successor;
                        }

                        successor.xOffset = xPositionSuccessor - xPositionParent;
                    }
                }

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                {
                    /*[Widen]*/
                    long xLength ;

                    if (successor != Null)
                    {
                        xLength = xPositionSuccessor - xPositionNode;
                    }
                    else
                    {
                        xLength = this.xExtent - xPositionNode;
                    }

                    ShiftRightOfPath(xPositionNode + 1, -xLength);

                    this.xExtent = unchecked(this.xExtent - xLength);
                }

                /* restore balance */
                if (balance != Null)
                {
                    while (true)
                    {
                        Node bparent = path[--idx];
                        Debug.Assert((bparent == Null) || (bparent.left == balance) || (bparent.right == balance));
                        left_node = (bparent != Null) && (balance == bparent.left);

                        if ((balance.balance < -1) || (balance.balance > 1))
                        {
                            balance = g_tree_node_balance(balance);
                            if (bparent == Null)
                            {
                                root = balance;
                            }
                            else if (left_node)
                            {
                                bparent.left = balance;
                            }
                            else
                            {
                                bparent.right = balance;
                            }
                        }

                        if ((balance.balance != 0) || (bparent == Null))
                        {
                            break;
                        }

                        if (left_node)
                        {
                            bparent.balance++;
                        }
                        else
                        {
                            bparent.balance--;
                        }

                        balance = bparent;
                    }
                }


                this.count = unchecked(this.count - 1);
                Debug.Assert((this.count == 0) == (root == Null));

                g_node_free(node);

                return true;
            }
        }

        // DOES NOT adjust xExtent and yExtent!
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ShiftRightOfPath(
            [Widen] long position,
            [Widen] long xAdjust)
        {
            unchecked
            {
                this.version = unchecked(this.version + 1);

                if (root != Null)
                {
                    /*[Widen]*/
                    long xPositionCurrent = 0 ;
                    Node current = root;
                    while (true)
                    {
                        xPositionCurrent += current.xOffset;

                        int order = position.CompareTo(xPositionCurrent);
                        if (order <= 0)
                        {
                            xPositionCurrent += xAdjust;
                            current.xOffset += xAdjust;
                            if (current.left_child)
                            {
                                current.left.xOffset -= xAdjust;
                            }

                            if (order == 0)
                            {
                                break;
                            }
                            if (!current.left_child)
                            {
                                break;
                            }
                            current = current.left;
                        }
                        else
                        {
                            if (!current.right_child)
                            {
                                break;
                            }
                            current = current.right;
                        }
                    }
                }
            }
        }

        //private int g_tree_height()
        //{
        //    unchecked
        //    {
        //        if (root == Null)
        //        {
        //            return 0;
        //        }
        //
        //        int height = 0;
        //        NodeRef node = root;
        //
        //        while (true)
        //        {
        //            height += 1 + Math.Max((int)nodes[node].balance, 0);
        //
        //            if (!nodes[node].left_child)
        //            {
        //                return height;
        //            }
        //
        //            node = nodes[node].left;
        //        }
        //    }
        //}

        private Node g_tree_node_balance(Node node)
        {
            unchecked
            {
                if (node.balance < -1)
                {
                    if (node.left.balance > 0)
                    {
                        node.left = g_tree_node_rotate_left(node.left);
                    }
                    node = g_tree_node_rotate_right(node);
                }
                else if (node.balance > 1)
                {
                    if (node.right.balance < 0)
                    {
                        node.right = g_tree_node_rotate_right(node.right);
                    }
                    node = g_tree_node_rotate_left(node);
                }

                return node;
            }
        }

        private Node g_tree_node_rotate_left(Node node)
        {
            unchecked
            {
                Node right = node.right;

                /*[Widen]*/
                long xOffsetNode = node.xOffset ;
                /*[Widen]*/
                long xOffsetRight = right.xOffset ;
                node.xOffset = -xOffsetRight;
                right.xOffset += xOffsetNode;

                if (right.left_child)
                {
                    right.left.xOffset += xOffsetRight;

                    node.right = right.left;
                }
                else
                {
                    
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    node.right = Null;
                }
                right.left = node;

                int a_bal = node.balance;
                int b_bal = right.balance;

                if (b_bal <= 0)
                {
                    if (a_bal >= 1)
                    {
                        right.balance = (sbyte)(b_bal - 1);
                    }
                    else
                    {
                        right.balance = (sbyte)(a_bal + b_bal - 2);
                    }
                    node.balance = (sbyte)(a_bal - 1);
                }
                else
                {
                    if (a_bal <= b_bal)
                    {
                        right.balance = (sbyte)(a_bal - 2);
                    }
                    else
                    {
                        right.balance = (sbyte)(b_bal - 1);
                    }
                    node.balance = (sbyte)(a_bal - b_bal - 1);
                }

                return right;
            }
        }

        private Node g_tree_node_rotate_right(Node node)
        {
            unchecked
            {
                Node left = node.left;

                /*[Widen]*/
                long xOffsetNode = node.xOffset ;
                /*[Widen]*/
                long xOffsetLeft = left.xOffset ;
                node.xOffset = -xOffsetLeft;
                left.xOffset += xOffsetNode;

                if (left.right_child)
                {
                    left.right.xOffset += xOffsetLeft;

                    node.left = left.right;
                }
                else
                {
                    
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    node.left = Null;
                }
                left.right = node;

                int a_bal = node.balance;
                int b_bal = left.balance;

                if (b_bal <= 0)
                {
                    if (b_bal > a_bal)
                    {
                        left.balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        left.balance = (sbyte)(a_bal + 2);
                    }
                    node.balance = (sbyte)(a_bal - b_bal + 1);
                }
                else
                {
                    if (a_bal <= -1)
                    {
                        left.balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        left.balance = (sbyte)(a_bal + b_bal + 2);
                    }
                    node.balance = (sbyte)(a_bal + 1);
                }

                return left;
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private Node g_tree_find_node(KeyType key) // TODO: consolidate with Find()
        {
            Node node = root;
            if (node == Null)
            {
                return Null;
            }

            while (true)
            {
                int cmp = comparer.Compare(key, node.key);
                if (cmp == 0)
                {
                    return node;
                }
                else if (cmp < 0)
                {
                    if (!node.left_child)
                    {
                        return Null;
                    }

                    node = node.left;
                }
                else
                {
                    if (!node.right_child)
                    {
                        return Null;
                    }

                    node = node.right;
                }
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool Find(
            KeyType key,
            out Node match,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long xPositionMatch)
        {
            unchecked
            {
                match = Null;
                xPositionMatch = 0;

                Node successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionLastGreaterAncestor = 0 ;
                if (root != Null)
                {
                    Node current = root;
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xPositionCurrent = 0 ;
                    while (true)
                    {
                        xPositionCurrent += current.xOffset;

                        int order = (match != Null) ? -1 : comparer.Compare(key, current.key);

                        if (order == 0)
                        {
                            xPositionMatch = xPositionCurrent;
                            match = current;
                        }

                        successor = current;
                        xPositionSuccessor = xPositionCurrent;

                        if (order < 0)
                        {
                            if (match == Null)
                            {
                                lastGreaterAncestor = current;
                                xPositionLastGreaterAncestor = xPositionCurrent;
                            }
                            if (!current.left_child)
                            {
                                break;
                            }
                            current = current.left;
                        }
                        else
                        {
                            if (!current.right_child)
                            {
                                break;
                            }
                            current = current.right; // continue the search in right sub tree after we find a match
                        }
                    }
                }

                if (match != Null)
                {
                    Debug.Assert(successor != Null);
                }

                return match != Null;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(
            [Widen] long position,
            out Node lastLessEqual,
            [Widen] out long xPositionLastLessEqual)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;

                Node successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                if (root != Null)
                {
                    Node current = root;
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xPositionCurrent = 0 ;
                    while (true)
                    {
                        xPositionCurrent += current.xOffset;

                        if (position < (xPositionCurrent))
                        {
                            successor = current;
                            xPositionSuccessor = xPositionCurrent;

                            if (!current.left_child)
                            {
                                break;
                            }
                            current = current.left;
                        }
                        else
                        {
                            lastLessEqual = current;
                            xPositionLastLessEqual = xPositionCurrent;

                            if (!current.right_child)
                            {
                                break;
                            }
                            current = current.right; // try to find successor
                        }
                    }
                }

                return (position >= 0) && (position < (this.xExtent));
            }
        }


        //
        // Non-invasive tree inspection support
        //

        // Helpers

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ValidateRanges()
        {
            if (root != Null)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long offset = 0 ;
                /*[Widen]*/
                long leftEdge = 0 ;
                /*[Widen]*/
                long rightEdge = this.xExtent ;

                Node node = root;
                while (node != Null)
                {
                    offset += node.xOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                    rightEdge = offset;
                    node = node.leftOrNull;
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
                    node = node.rightOrNull;
                    while (node != Null)
                    {
                        offset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = node.leftOrNull;
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
        object INonInvasiveTreeInspection.Root { get { return root != Null ? (object)root : null; } }

        /// <summary>
        /// INonInvasiveTreeInspection.GetLeftChild() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left_child ? (object)n.left : null;
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
            return n.right_child ? (object)n.right : null;
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
            Node n = (Node)node;
            return n.balance;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        void INonInvasiveTreeInspection.Validate()
        {
            if (root != Null)
            {
                Dictionary<Node, bool> visited = new Dictionary<Node, bool>();
                Queue<Node> worklist = new Queue<Node>();
                worklist.Enqueue(root);
                while (worklist.Count != 0)
                {
                    Node node = worklist.Dequeue();

                    Check.Assert(!visited.ContainsKey(node), "cycle");
                    visited.Add(node, false);

                    if (node.left_child)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        Check.Assert(comparer.Compare(node.left.key, node.key) < 0, "ordering invariant");
                        worklist.Enqueue(node.left);
                    }
                    if (node.right_child)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        Check.Assert(comparer.Compare(node.key, node.right.key) < 0, "ordering invariant");
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges();

            g_tree_node_check(root);
            ValidateDepthInvariant();
        }

        private void ValidateDepthInvariant()
        {
            double max = TheoreticalMaxDepth(count); // includes epsilon
            int depth = root != Null ? ActualDepth(root) : 0;
            Check.Assert(depth <= max, "max depth invariant");
        }

        private int ActualDepth(Node node)
        {
            if (node != Null)
            {
                int ld = ActualDepth(node.leftOrNull);
                int rd = ActualDepth(node.rightOrNull);
                return 1 + Math.Max(ld, rd);
            }
            return 0;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static int TheoreticalMaxDepth(ulong c)
        {
            // from https://en.wikipedia.org/wiki/AVL_tree#Comparison_to_other_structures
            return (int)Math.Ceiling(Math.Log(c + 1.0652475842498528) * 2.0780869212350273 + (-0.32772406181544556 + .001/*epsilon*/));
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static int EstimateMaxDepth(ulong c)
        {
            unchecked
            {
                int h = 36 * (Log2.CeilLog2(c + 1) + 1) / 25;

                Debug.Assert(h >= TheoreticalMaxDepth(c));
                return h;
            }
        }

        private void g_tree_node_check(Node node)
        {
            if (node != Null)
            {

                int left_height = g_tree_node_height(node.leftOrNull);
                int right_height = g_tree_node_height(node.rightOrNull);

                int balance = right_height - left_height;
                Check.Assert(balance == node.balance, "balance invariant");

                g_tree_node_check(node.leftOrNull);
                g_tree_node_check(node.rightOrNull);
            }
        }

        private int g_tree_node_height(Node node)
        {
            if (node != Null)
            {
                int left_height = 0;
                int right_height = 0;

                if (node.left_child)
                {
                    left_height = g_tree_node_height(node.left);
                }

                if (node.right_child)
                {
                    right_height = g_tree_node_height(node.right);
                }

                return Math.Max(left_height, right_height) + 1;
            }

            return 0;
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

            if (root != Null)
            {
                Stack<STuple<Node,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long>>();

                /*[Widen]*/
                long xOffset = 0 ;

                Node node = root;
                while (node != Null)
                {
                    xOffset += node.xOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long>(node, xOffset));
                    node = node.leftOrNull;
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

                    node = node.rightOrNull;
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long>(node, xOffset));
                        node = node.leftOrNull;
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
        /// Get the default enumerator, which is the fast enumerator for AVL trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryRankListLong<KeyType>> GetEnumerator()
        {
            return GetFastEnumerable().GetEnumerator();
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
        public IEnumerable<EntryRankListLong<KeyType>> GetEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the default enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the default enumerator</returns>
        public IEnumerable<EntryRankListLong<KeyType>> GetEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        /// <summary>
        /// Get the robust enumerator. The robust enumerator uses an internal key cursor and queries the tree using the NextGreater()
        /// method to advance the enumerator. This enumerator is robust because it tolerates changes to the underlying tree. If a key
        /// is inserted or removed and it comes before the enumerator’s current key in sorting order, it will have no affect on the
        /// enumerator. If a key is inserted or removed and it comes after the enumerator’s current key (i.e. in the portion of the
        /// collection the enumerator hasn’t visited yet), the enumerator will include the key if inserted or skip the key if removed.
        /// Because the enumerator queries the tree for each element it’s running time per element is O(lg N), or O(N lg N) to
        /// enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        
        /// <summary>
        /// Create a new instance of the robust enumerator.
        /// </summary>
        /// <returns>A new instance of the robust enumerator</returns>
        public IEnumerable<EntryRankListLong<KeyType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        public IEnumerable<EntryRankListLong<KeyType>> GetRobustEnumerable(bool forward)
        {
            return new RobustEnumerableSurrogate(this, forward);
        }

        /// <summary>
        /// Get the fast enumerator. The fast enumerator uses an internal stack of nodes to peform in-order traversal of the
        /// tree structure. Because it uses the tree structure, it is invalidated if the tree is modified by an insertion or
        /// deletion and will throw an InvalidOperationException when next advanced. The complexity of the fast enumerator
        /// is O(1) per element, or O(N) to enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        
        /// <summary>
        /// Create a new instance of the fast enumerator.
        /// </summary>
        /// <returns>A new instance of the fast enumerator</returns>
        public IEnumerable<EntryRankListLong<KeyType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        public IEnumerable<EntryRankListLong<KeyType>> GetFastEnumerable(bool forward)
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
        public IEnumerable<EntryRankListLong<KeyType>> GetEnumerable(KeyType startAt)
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
        public IEnumerable<EntryRankListLong<KeyType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward); // default
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryRankListLong<KeyType>> GetFastEnumerable(KeyType startAt)
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
        public IEnumerable<EntryRankListLong<KeyType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, forward);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryRankListLong<KeyType>> GetRobustEnumerable(KeyType startAt)
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
        public IEnumerable<EntryRankListLong<KeyType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRankListLong<KeyType>>
        {
            private readonly AVLTreeRankListLong<KeyType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            // Construction

            public RobustEnumerableSurrogate(AVLTreeRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerableSurrogate(AVLTreeRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;
            }

            // IEnumerable

            public IEnumerator<EntryRankListLong<KeyType>> GetEnumerator()
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

        public struct FastEnumerableSurrogate : IEnumerable<EntryRankListLong<KeyType>>
        {
            private readonly AVLTreeRankListLong<KeyType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            // Construction

            public FastEnumerableSurrogate(AVLTreeRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerableSurrogate(AVLTreeRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;
            }

            // IEnumerable

            public IEnumerator<EntryRankListLong<KeyType>> GetEnumerator()
            {
                
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                if (startKeyed)
                {
                    return new FastEnumerator(tree, startKey, forward);
                }
                
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                return new FastEnumerator(tree, forward);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// This enumerator is robust in that it can continue to walk the tree even in the face of changes, because
        /// it keeps a current key and uses NearestGreater to find the next one. However, since it uses queries it
        /// is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator :
            IEnumerator<EntryRankListLong<KeyType>>        {
            private readonly AVLTreeRankListLong<KeyType> tree;
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

            public RobustEnumerator(AVLTreeRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerator(AVLTreeRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;

                Reset();
            }

            public EntryRankListLong<KeyType> Current
            {
                get
                {

                    if (valid)
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        {
                            KeyType key = currentKey;
                            /*[Widen]*/
                            long rank = 0 ;
                            
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            tree.Get(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey,
                                /*[Payload(Payload.None)]*/out key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/out rank);

                            return new EntryRankListLong<KeyType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/rank);
                        }
                    return new EntryRankListLong<KeyType>();
                }
            }

            object IEnumerator.Current { get { return this.Current; } }

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
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        public class FastEnumerator :
            IEnumerator<EntryRankListLong<KeyType>>        {
            private readonly AVLTreeRankListLong<KeyType> tree;
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

            private STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>[] stack;
            private int stackIndex;

            public FastEnumerator(AVLTreeRankListLong<KeyType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerator(AVLTreeRankListLong<KeyType> tree,KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startKey = startKey;

                Reset();
            }

            public EntryRankListLong<KeyType> Current
            {
                get
                {
                    if (currentNode != Null)
                    {
                        /*[Feature(Feature.Rank)]*/
                        Debug.Assert((forward && (nextXStart - currentXStart == 1))
                            || (!forward && ((nextXStart - currentXStart == -1) || ((currentXStart == 0) && (nextXStart == 0)))));

                        return new EntryRankListLong<KeyType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                            currentNode.key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart);
                    }
                    return new EntryRankListLong<KeyType>();
                }
            }

            object IEnumerator.Current { get { return this.Current; } }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                Advance();
                return currentNode != Null;
            }

            public void Reset()
            {
                unchecked
                {
                    int stackSize = EstimateMaxDepth(tree.count);
                    if ((stack == null) || (stackSize > stack.Length))
                    {
                        stack = new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>[
                            stackSize];
                    }
                    stackIndex = 0;

                    currentNode = Null;
                    leadingNode = Null;

                    this.treeVersion = tree.version;

                    // push search path to starting item

                    Node node = tree.root;
                    /*[Widen]*/
                    long xPosition = 0 ;
                    while (node != Null)
                    {
                        xPosition += node.xOffset;

                        int c;
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

                        if ( (forward && (c <= 0)) || (!forward && (c >= 0)))
                        {
                            stack[stackIndex++] = new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
                                node,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition);
                        }

                        if (c == 0)
                        {

                            // successor not needed for forward traversal
                            if (forward)
                            {
                                break;
                            }
                            // successor not needed for case where xLength always == 1
                            /*[Feature(Feature.Dict, Feature.Rank)]*/
                            break;
                        }

                        if (c < 0)
                        {

                            node = node.leftOrNull;
                        }
                        else
                        {
                            Debug.Assert(c >= 0);
                            node = node.rightOrNull;
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
                    currentNode = leadingNode;
                    currentXStart = nextXStart;

                    leadingNode = Null;

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
                        = stack[--stackIndex];

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;

                    Node node = forward
                        ? (leadingNode.rightOrNull)
                        : (leadingNode.leftOrNull);
                    /*[Widen]*/
                    long xPosition = nextXStart ;
                    while (node != Null)
                    {
                        xPosition += node.xOffset;

                        stack[stackIndex++] = new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition);
                        node = forward
                            ? (node.leftOrNull)
                            : (node.rightOrNull);
                    }
                }
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new AVLTreeRankListLong<KeyType>(this);
        }
    }
}
