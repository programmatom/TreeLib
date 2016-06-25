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
//
// An overview of AVL trees can be found here: https://en.wikipedia.org/wiki/AVL_tree
//

namespace TreeLib
{

    /// <summary>
    /// Implements a map, list or range collection using an AVL tree. 
    /// </summary>
    
    /// <summary>
    /// Represents a ordered key collection, augmented with multi-rank information. The rank of a key is the index it would
    /// be located in if all the keys in the tree were placed into a sorted array. Each key also has a count
    /// associated with it, which models sorted arrays containing multiple instances of a key. It is equivalent to the number of times
    /// the key appears in the array. Rank index values account for such multiple occurrences. In this case, the rank
    /// index is the index at which the first instance of a particular key would occur in a sorted array containing all keys.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public class AVLTreeMultiRankListLong<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType> :
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
            public Node left, right;

            // tree is threaded: left_child/right_child indicate "non-null", if false, left/right point to predecessor/successor
            public bool left_child, right_child;
            public sbyte balance;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;
        }

        [Storage(Storage.Object)]
        private readonly static Node _Null = null;

        //
        // State for both array & object form
        //

        private Node Null { get { return AVLTreeMultiRankListLong<KeyType>._Null; } } // allow tree.Null or this.Null in all cases

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
        public AVLTreeMultiRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
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
        public AVLTreeMultiRankListLong(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTreeMultiRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public AVLTreeMultiRankListLong()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public AVLTreeMultiRankListLong(AVLTreeMultiRankListLong<KeyType> original)
        {
            this.comparer = original.comparer;

            this.count = original.count;
            this.xExtent = original.xExtent;

            this.allocationMode = original.allocationMode;
            this.freelist = this.Null;
            {
                Node nodeOriginal = original.freelist;
                while (nodeOriginal != original.Null)
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

            this.root = this.Null;
            if (original.root != original.Null)
            {
#if VALIDATE_CLONE_THREADING
                int originalNumber = 0;
                Dictionary<NodeRef, int> originalSequence = new Dictionary<NodeRef, int>();
                int thisNumber = 0;
                Dictionary<NodeRef, int> thisSequence = new Dictionary<NodeRef, int>();
#endif
                {
                    Stack<STuple<Node, Node, Node>> stack = new Stack<STuple<Node, Node, Node>>();

                    Node nodeOriginal = original.root;
                    Node nodeThis = this.root;
                    while (nodeOriginal != original.Null)
                    {
                        Node nodeChild = new Node();
                        nodeChild.left_child = false;
                        nodeChild.left = this.Null; // never a predecessor on the initial left chain
                        nodeChild.right_child = false;
                        nodeChild.right = nodeThis;

                        if (this.root == this.Null)
                        {
                            this.root = nodeChild;
                        }
                        else
                        {
                            nodeThis.left_child = true;
                            nodeThis.left = nodeChild;
                        }

                        Node nodeControllingSuccessor = nodeThis;
                        nodeThis = nodeChild;
                        stack.Push(new STuple<Node, Node, Node>(nodeThis, nodeOriginal, nodeControllingSuccessor));
                        nodeOriginal = nodeOriginal.left_child ? nodeOriginal.left : original.Null;
                    }
                    while (stack.Count != 0)
                    {
                        STuple<Node, Node, Node> t = stack.Pop();
                        nodeThis = t.Item1;
                        nodeOriginal = t.Item2;
                        Node nodeControllingSuccessorThis = t.Item3;
                        {
#if VALIDATE_CLONE_THREADING
                            originalSequence.Add(nodeOriginal, ++originalNumber);
                            thisSequence.Add(nodeThis, ++thisNumber);
#endif
                        } // stops Roslyn from removing leading trivia containing #endif in cases where the next line is removed

                        nodeThis.key = nodeOriginal.key;
                        nodeThis.xOffset = nodeOriginal.xOffset;
                        nodeThis.balance = nodeOriginal.balance;

                        if (nodeOriginal.right_child)
                        {
                            Node nodeChild = new Node();
                            nodeChild.left_child = false;
                            nodeChild.left = nodeThis;
                            nodeChild.right_child = false;
                            nodeChild.right = nodeControllingSuccessorThis;

                            nodeThis.right_child = true;
                            nodeThis.right = nodeChild;

                            Node nodeControllingPredecessor = nodeThis;

                            nodeOriginal = nodeOriginal.right;
                            nodeThis = nodeChild;
                            stack.Push(new STuple<Node, Node, Node>(nodeThis, nodeOriginal, nodeControllingSuccessorThis));

                            nodeOriginal = nodeOriginal.left_child ? nodeOriginal.left : original.Null;
                            while (nodeOriginal != original.Null)
                            {
                                nodeChild = new Node();
                                nodeChild.left_child = false;
                                nodeChild.left = nodeControllingPredecessor;
                                nodeChild.right_child = false;
                                nodeChild.right = nodeThis;

                                nodeThis.left_child = true;
                                nodeThis.left = nodeChild;

                                Node nodeControllingSuccessor = nodeThis;
                                nodeThis = nodeChild;
                                stack.Push(new STuple<Node, Node, Node>(nodeThis, nodeOriginal, nodeControllingSuccessor));
                                nodeOriginal = nodeOriginal.left_child ? nodeOriginal.left : original.Null;
                            }
                        }
                    }
                }
                {
#if VALIDATE_CLONE_THREADING
                    Debug.Assert((ulong)thisSequence.Count == original.count);
                    Debug.Assert((ulong)originalSequence.Count == original.count);
                    Queue<STuple<NodeRef, NodeRef>> worklist = new Queue<STuple<NodeRef, NodeRef>>();
                    worklist.Enqueue(new STuple<NodeRef, NodeRef>(this.root, original.root));
                    while (worklist.Count != 0)
                    {
                        STuple<NodeRef, NodeRef> item = worklist.Dequeue();

                        NodeRef nodeThis = item.Item1;
                        NodeRef nodeOriginal = item.Item2;

                        int sequenceThis = thisSequence[nodeThis];
                        int sequenceOriginal = originalSequence[nodeOriginal];
                        Debug.Assert(sequenceThis == sequenceOriginal);

                        Debug.Assert(original.nodes[nodeOriginal].left_child == this.nodes[nodeThis].left_child);
                        Debug.Assert(original.nodes[nodeOriginal].right_child == this.nodes[nodeThis].right_child);

                        if (!original.nodes[nodeOriginal].left_child)
                        {
                            Debug.Assert((original.nodes[nodeOriginal].left == null) == (this.nodes[nodeThis].left == null));
                            if (original.nodes[nodeOriginal].left != null)
                            {
                                int sequenceLeftThis = thisSequence[this.nodes[nodeThis].left];
                                int sequenceLeftOriginal = originalSequence[original.nodes[nodeOriginal].left];
                                Debug.Assert(sequenceLeftThis == sequenceLeftOriginal);
                            }
                        }
                        if (!original.nodes[nodeOriginal].right_child)
                        {
                            Debug.Assert((original.nodes[nodeOriginal].right == null) == (this.nodes[nodeThis].right == null));
                            if (original.nodes[nodeOriginal].right != null)
                            {
                                int sequenceRightThis = thisSequence[this.nodes[nodeThis].right];
                                int sequenceRightOriginal = originalSequence[original.nodes[nodeOriginal].right];
                                Debug.Assert(sequenceRightThis == sequenceRightOriginal);
                            }
                        }

                        if (original.nodes[nodeOriginal].left_child)
                        {
                            worklist.Enqueue(new STuple<NodeRef, NodeRef>(this.nodes[nodeThis].left, original.nodes[nodeOriginal].left));
                        }
                        if (original.nodes[nodeOriginal].right_child)
                        {
                            worklist.Enqueue(new STuple<NodeRef, NodeRef>(this.nodes[nodeThis].right, original.nodes[nodeOriginal].right));
                        }
                    }
#endif
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
                // use threaded feature to traverse in O(1) per node with no stack

                Node node = g_tree_first_node();

                while (node != Null)
                {
                    Node next = g_tree_node_next(node);

                    this.count = unchecked(this.count - 1);
                    g_node_free(node);

                    node = next;
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
        /// The entire key is removed, regardless of the rank count for it.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0);
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
        public bool TryGetKey(KeyType key,out KeyType keyOut)
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
                true/*update*/);
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
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
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
        /// <returns>true if a key was found (i.e. collection contains at least 1 key-value pair)</returns>
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
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
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
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = 0;
            rankCount = 0;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
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
        public bool NearestLess(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
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
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = 0;
            rankCount = 0;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
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
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
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
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = this.xExtent;
            rankCount = 0;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
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
        public bool NearestGreater(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
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
        /// <param name="count">the count of the returned key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey,[Feature(Feature.Rank, Feature.RankMulti)][Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            rank = this.xExtent;
            rankCount = 0;
            /*[Widen]*/
            long nearestStart ;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
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
            Node node;
            /*[Widen]*/
            long xPosition ;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                return true;
            }
            xLength = 0;
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
        /// <returns>true if a key was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqualByRank([Widen] long position,[Widen] out long nearestStart)
        {
            KeyType nearestKey;
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
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
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                nearestKey = nearestNode.key;
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
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
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
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                nearestKey = nearestNode.key;
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
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
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
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                nearestKey = nearestNode.key;
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
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
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
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                nearestKey = nearestNode.key;
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
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
        public bool TryAdd(KeyType key,[Feature(Feature.RankMulti)] [Const(1, Feature.Rank)] [SuppressConst(Feature.RankMulti)][Widen] long rankCount)
        {
            if (rankCount <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return g_tree_insert_internal(
                key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                rankCount,
                true/*add*/,
                false/*update*/);
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
        public bool TryGet(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            if (Find(key, out node, out xPosition, /*[Feature(Feature.RankMulti)]*/out xLength))
            {
                keyOut = node.key;
                rank = xPosition;
                rankCount = xLength;
                return true;
            }
            keyOut = default(KeyType);
            rank = 0;
            rankCount = 0;
            return false;
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
                Node node;
                /*[Widen]*/
                long xPosition ;
                /*[Widen]*/
                long xLength ;
                if (Find(key, out node, out xPosition, /*[Feature(Feature.RankMulti)]*/out xLength))
                {
                    /*[Widen]*/
                    if (rankCount > 0)
                    {
                        /*[Widen]*/
                        long countAdjust = checked(rankCount - xLength) ;
                        this.xExtent = checked(this.xExtent + countAdjust);

                        ShiftRightOfPath(unchecked(xPosition + 1), countAdjust);
                        /*[Payload(Payload.None)]*/
                        node.key = key;

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
        public bool TryGetKeyByRank([Widen] long rank,out KeyType key)
        {
            if (rank < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            long xPosition ;
            /*[Feature(Feature.RankMulti)]*/
            /*[Widen]*/
            long xLength ;
            if (FindPosition(
rank,
out node,
out xPosition,
/*[Feature(Feature.RankMulti)]*/out xLength))
            {
                key = node.key;
                return true;
            }
            key = default(KeyType);
            return false;
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
        public void Get(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
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
        public long AdjustCount(KeyType key,[Widen] long countAdjust)
        {
            unchecked
            {
                Node node;
                /*[Widen]*/
                long xPosition ;
                /*[Widen]*/
                long xLength = 1 ;
                if (Find(key, out node, out xPosition, /*[Feature(Feature.RankMulti)]*/out xLength))
                {
                    // update and possibly remove

                    /*[Widen]*/
                    long adjustedLength = checked(xLength + countAdjust) ;
                    if (adjustedLength > 0)
                    {

                        this.xExtent = checked(this.xExtent + countAdjust);

                        ShiftRightOfPath(unchecked(xPosition + 1), countAdjust);

                        return adjustedLength;
                    }
                    else if (xLength + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        this.g_tree_remove_internal(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0);

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
            node.left_child = false;
            node.right = Null;
            node.right_child = false;
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
            node.left_child = true;
            node.right = Null;
            node.right_child = true;
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

        private bool NearestLess(            out Node nearestNode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                        if (mode == CompareKeyMode.Key)
                        {
                            c = comparer.Compare(key, node.key);
                        }
                        else
                        {
                            Debug.Assert(mode == CompareKeyMode.Position);
                            c = position.CompareTo(xPosition);
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

        private bool NearestGreater(            out Node nearestNode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                        if (mode == CompareKeyMode.Key)
                        {
                            c = comparer.Compare(key, node.key);
                        }
                        else
                        {
                            Debug.Assert(mode == CompareKeyMode.Position);
                            c = position.CompareTo(xPosition);
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

        private Node g_tree_node_previous(Node node)
        {
            Node tmp = node.left;

            if (node.left_child)
            {
                while (tmp.right_child)
                {
                    tmp = tmp.right;
                }
            }

            return tmp;
        }

        private Node g_tree_node_next(Node node)
        {
            Node tmp = node.right;

            if (node.right_child)
            {
                while (tmp.left_child)
                {
                    tmp = tmp.left;
                }
            }

            return tmp;
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

        // NOTE: replace mode does *not* adjust for xLength/yLength!
        private bool g_tree_insert_internal(            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xLength,            bool add,            bool update)
        {
            unchecked
            {
                if (root == Null)
                {

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

                    child.left = node.left;
                    child.right = node;
                    node.left = child;
                    node.left_child = true;
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

                    child.right = node.right;
                    child.left = node;
                    node.right = child;
                    node.right_child = true;
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

        private bool g_tree_remove_internal(            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position)
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
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        if (parent == Null)
                        {
                            root = Null;
                        }
                        else if (left_node)
                        {
                            parent.left_child = false;
                            parent.left = node.left;
                            parent.balance++;
                        }
                        else
                        {
                            parent.right_child = false;
                            parent.right = node.right;
                            parent.balance--;
                        }
                    }
                    else // node has a right child
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        xPositionSuccessor = xPositionNode;
                        // OR
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = node.right;
                            xPositionSuccessor += successor.xOffset;
                            while (successor.left_child)
                            {
                                successor = successor.left;
                                xPositionSuccessor += successor.xOffset;
                            }
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        if (node.left_child)
                        {
                            node.left.xOffset += xPositionNode - xPositionSuccessor;
                        }
                        successor.left = node.left;

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
                        Node predecessor;
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long xPositionPredecessor = xPositionNode ;
                        // OR
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            predecessor = node;
                            xPositionPredecessor += predecessor.xOffset;
                            while (predecessor.left_child)
                            {
                                predecessor = predecessor.left;
                                xPositionPredecessor += predecessor.xOffset;
                            }
                            Debug.Assert(predecessor == g_tree_node_previous(node));
                        }

                        // and successor
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = lastGreaterAncestor;
                            xPositionSuccessor = xPositionLastGreaterAncestor;
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        if (node.right_child)
                        {
                            node.right.xOffset += xPositionNode - xPositionPredecessor;
                        }
                        predecessor.right = node.right;

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
                        Node predecessor = node.left;
                        successor = node.right;
                        Node successorParent = node;
                        int old_idx = ++idx;
                        xPositionSuccessor = xPositionNode + successor.xOffset;

                        /* path[idx] == parent */
                        /* find the immediately next node (and its parent) */
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
                                successorParent.left_child = false;
                            }
                            successorParent.balance++;

                            successor.right_child = true;
                            successor.right = node.right;

                            node.right.xOffset += xPositionNode - xPositionSuccessor;
                        }
                        else
                        {
                            node.balance--;
                        }

                        // set the predecessor's successor link to point to the right place
                        while (predecessor.right_child)
                        {
                            predecessor = predecessor.right;
                        }
                        predecessor.right = successor;

                        /* prepare 'successor' to replace 'node' */
                        Node leftChild = node.left;
                        successor.left_child = true;
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
        private void ShiftRightOfPath(            [Widen] long position,            [Widen] long xAdjust)
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
                    node.right_child = false;
                    right.left_child = true;
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
                    node.left_child = false;
                    left.right_child = true;
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
        private Node g_tree_find_node(KeyType key)
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

        [Feature(Feature.Rank, Feature.RankMulti)]
        private bool Find(            KeyType key,            out Node match,            [Widen] out long xPositionMatch,            [Feature(Feature.RankMulti)][Widen] out long xLengthMatch)
        {
            unchecked
            {
                match = Null;
                xPositionMatch = 0;
                xLengthMatch = 0;

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
                    if (successor != match)
                    {
                        xLengthMatch = xPositionSuccessor - xPositionMatch;
                    }
                    else if (lastGreaterAncestor != Null)
                    {
                        xLengthMatch = xPositionLastGreaterAncestor - xPositionMatch;
                    }
                    else
                    {
                        xLengthMatch = this.xExtent - xPositionMatch;
                    }
                }

                return match != Null;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(            [Widen] long position,            out Node lastLessEqual,            [Widen] out long xPositionLastLessEqual,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long xLength)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;
                xLength = 0;

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
                if ((successor != Null) && (successor != lastLessEqual))
                {
                    xLength = xPositionSuccessor - xPositionLastLessEqual;
                }
                else
                {
                    xLength = this.xExtent - xPositionLastLessEqual;
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
                    node = node.left_child ? node.left : Null;
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
                    node = node.right_child ? node.right : Null;
                    while (node != Null)
                    {
                        offset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = node.left_child ? node.left : Null;
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
            const double phi = 1.618033988749894848204;
            const double epsilon = .001;

            double max = Math.Log((count + 2) * Math.Sqrt(5)) / Math.Log(phi) - 2;
            int depth = root != Null ? MaxDepth(root) : 0;
            Check.Assert(depth <= max + epsilon, "max depth invariant");
        }

        private int MaxDepth(Node node)
        {
            int ld = node.left_child ? MaxDepth(node.left) : 0;
            int rd = node.right_child ? MaxDepth(node.right) : 0;
            return 1 + Math.Max(ld, rd);
        }

        private void g_tree_node_check(Node node)
        {
            if (node != Null)
            {
                if (node.left_child)
                {
                    Node tmp = g_tree_node_previous(node);
                    Check.Assert(tmp.right == node, "predecessor invariant");
                }

                if (node.right_child)
                {
                    Node tmp = g_tree_node_next(node);
                    Check.Assert(tmp.left == node, "successor invariant");
                }

                int left_height = g_tree_node_height(node.left_child ? node.left : Null);
                int right_height = g_tree_node_height(node.right_child ? node.right : Null);

                int balance = right_height - left_height;
                Check.Assert(balance == node.balance, "balance invariant");

                if (node.left_child)
                {
                    g_tree_node_check(node.left);
                }
                if (node.right_child)
                {
                    g_tree_node_check(node.right);
                }
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
                    node = node.left_child ? node.left : Null;
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

                    node = node.right_child ? node.right : Null;
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long>(node, xOffset));
                        node = node.left_child ? node.left : Null;
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
        public IEnumerator<EntryMultiRankListLong<KeyType>> GetEnumerator()
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

        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable(bool forward)
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
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable(bool forward)
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
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        //
        // IKeyedTreeEnumerable
        //

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable(KeyType startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/); // default
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetEnumerable(KeyType startAt,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward); // default
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable(KeyType startAt)
        {
            return new FastEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetFastEnumerable(KeyType startAt,bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, forward);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable(KeyType startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<EntryMultiRankListLong<KeyType>> GetRobustEnumerable(KeyType startAt,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<EntryMultiRankListLong<KeyType>>
        {
            private readonly AVLTreeMultiRankListLong<KeyType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            // Construction

            public RobustEnumerableSurrogate(AVLTreeMultiRankListLong<KeyType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerableSurrogate(AVLTreeMultiRankListLong<KeyType> tree,KeyType startKey,bool forward)
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
            private readonly AVLTreeMultiRankListLong<KeyType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            // Construction

            public FastEnumerableSurrogate(AVLTreeMultiRankListLong<KeyType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerableSurrogate(AVLTreeMultiRankListLong<KeyType> tree,KeyType startKey,bool forward)
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
        /// it keeps a current key and uses NearestGreater to find the next one. However, since it uses queries it
        /// is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator :
            IEnumerator<EntryMultiRankListLong<KeyType>>        {
            private readonly AVLTreeMultiRankListLong<KeyType> tree;
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

            public RobustEnumerator(AVLTreeMultiRankListLong<KeyType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerator(AVLTreeMultiRankListLong<KeyType> tree,KeyType startKey,bool forward)
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
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        public class FastEnumerator :
            IEnumerator<EntryMultiRankListLong<KeyType>>        {
            private readonly AVLTreeMultiRankListLong<KeyType> tree;
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

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>>();

            public FastEnumerator(AVLTreeMultiRankListLong<KeyType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerator(AVLTreeMultiRankListLong<KeyType> tree,KeyType startKey,bool forward)
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
                    if (currentNode != tree.Null)
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
                return currentNode != tree.Null;
            }

            public void Reset()
            {
                unchecked
                {
                    stack.Clear();

                    currentNode = tree.Null;
                    leadingNode = tree.Null;

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
                    while (node != tree.Null)
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
                            stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
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

                            node = node.left_child ? node.left : tree.Null;
                        }
                        else
                        {
                            Debug.Assert(c >= 0);
                            node = node.right_child ? node.right : tree.Null;
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

                    leadingNode = tree.Null;

                    if (stack.Count == 0)
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

                    STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long> cursor
                        = stack.Pop();

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;

                    Node node = forward
                        ? (leadingNode.right_child ? leadingNode.right : tree.Null)
                        : (leadingNode.left_child ? leadingNode.left : tree.Null);
                    /*[Widen]*/
                    long xPosition = nextXStart ;
                    while (node != tree.Null)
                    {
                        xPosition += node.xOffset;

                        stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
                        node = forward
                            ? (node.left_child ? node.left : tree.Null)
                            : (node.right_child ? node.right : tree.Null);
                    }
                }
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new AVLTreeMultiRankListLong<KeyType>(this);
        }
    }
}
