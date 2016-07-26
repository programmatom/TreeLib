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
    // Placeholder, used temporarily to facilitate code transforms more easily, then references
    // renamed to shared enumeration entry.
    public struct AVLTreeEntry<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType>
    {
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly KeyType key;
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public KeyType Key { get { return key; } }

        [Payload(Payload.Value)]
        private ValueType value;
        [Payload(Payload.Value)]
        public ValueType Value { get { return value; } }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xStart;
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int XStart { get { return xStart; } }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xLength;
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int XLength { get { return xLength; } }

        [Feature(Feature.Range2)]
        [Widen]
        private int yStart;
        [Feature(Feature.Range2)]
        [Widen]
        public int YStart { get { return yStart; } }

        [Feature(Feature.Range2)]
        [Widen]
        private int yLength;
        [Feature(Feature.Range2)]
        [Widen]
        public int YLength { get { return yLength; } }

        [Payload(Payload.Value)]
        private readonly ISetValue<ValueType> enumerator;
        [Payload(Payload.Value)]
        private readonly uint version;

        [Payload(Payload.Value)]
        public void SetValue(ValueType value)
        {
            enumerator.SetValue(value, version);
        }

        public AVLTreeEntry(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Payload(Payload.Value)] ValueType value,
            [Payload(Payload.Value)] ISetValue<ValueType> enumerator,
            [Payload(Payload.Value)] uint version,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xStart,
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,
            [Feature(Feature.Range2)][Widen] int yStart,
            [Feature(Feature.Range2)][Widen] int yLength)
        {
            this.key = key;
            this.value = value;
            this.xStart = xStart;
            this.xLength = xLength;
            this.yStart = yStart;
            this.yLength = yLength;

            this.enumerator = enumerator;
            this.version = version;
        }
    }

    /// <summary>
    /// Implements a map, list or range collection using an AVL tree. 
    /// </summary>
    public class AVLTree<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Dict)]*//*[Payload(Payload.Value)]*/IOrderedMap<KeyType, ValueType>,
        /*[Feature(Feature.Dict)]*//*[Payload(Payload.None)]*/IOrderedList<KeyType>,

        /*[Feature(Feature.Range2)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRange2Map<ValueType>,
        /*[Feature(Feature.Range2)]*//*[Payload(Payload.None)]*//*[Widen]*/IRange2List,

        /*[Feature(Feature.Range)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRangeMap<ValueType>,
        /*[Feature(Feature.Range)]*//*[Payload(Payload.None)]*//*[Widen]*/IRangeList,

        /*[Feature(Feature.RankMulti)]*//*[Payload(Payload.Value)]*//*[Widen]*/IMultiRankMap<KeyType, ValueType>,
        /*[Feature(Feature.RankMulti)]*//*[Payload(Payload.None)]*//*[Widen]*/IMultiRankList<KeyType>,

        /*[Feature(Feature.Rank)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRankMap<KeyType, ValueType>,
        /*[Feature(Feature.Rank)]*//*[Payload(Payload.None)]*//*[Widen]*/IRankList<KeyType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspection,
        /*[Feature(Feature.Rank, Feature.RankMulti)]*//*[Widen]*/INonInvasiveMultiRankMapInspection,

        IEnumerable<AVLTreeEntry<KeyType, ValueType>>,
        IEnumerable,
        ITreeEnumerable<AVLTreeEntry<KeyType, ValueType>>,
        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/IKeyedTreeEnumerable<KeyType, AVLTreeEntry<KeyType, ValueType>>,
        /*[Feature(Feature.Range)]*//*[Widen]*/IIndexedTreeEnumerable<AVLTreeEntry<KeyType, ValueType>>,
        /*[Feature(Feature.Range2)]*//*[Widen]*/IIndexed2TreeEnumerable<AVLTreeEntry<KeyType, ValueType>>,

        ICloneable

        where KeyType : IComparable<KeyType>
    {
        //
        // Object form data structure
        //

        [Storage(Storage.Object)]
        private sealed class Node
        {
            public NodeRef left, right;

            // tree is threaded: left_child/right_child indicate "non-null", if false, left/right point to predecessor/successor
            [Feature(Feature.Dict)]
            public bool left_child, right_child;
            // non-threaded for augmented versions
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public bool left_child { get { return left != Null; } }
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public bool right_child { get { return right != Null; } }

            [Feature(Feature.Dict)]
            public NodeRef leftOrNull { get { return left_child ? left : Null; } }
            [Feature(Feature.Dict)]
            public NodeRef rightOrNull { get { return right_child ? right : Null; } }
            //[OR]
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public NodeRef leftOrNull { get { return left; } }
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public NodeRef rightOrNull { get { return right; } }

            public sbyte balance;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;
            [Payload(Payload.Value)]
            public ValueType value;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;
            [Feature(Feature.Range2)]
            [Widen]
            public int yOffset;
        }

        [ArrayIndexing]
        [Storage(Storage.Object)]
        private Node this[Node node] { get { return node; } }

        [ArrayIndexing]
        [Storage(Storage.Object)]
        private struct nodesStruct
        {
            public Node this[Node node] { get { return node; } }
        }

        [ArrayIndexing]
        [Storage(Storage.Object)]
        private nodesStruct nodes;

        [Storage(Storage.Object)]
        private static NodeRef Null { get { return new NodeRef(null); } }

        [ArrayIndexing("Node")]
        [Storage(Storage.Object)]
        [StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        private struct NodeRef
        {
            public readonly Node node;

            public NodeRef(Node node)
            {
                this.node = node;
            }

            public static implicit operator Node(NodeRef nodeRef)
            {
                return nodeRef.node;
            }

            public static bool operator ==(NodeRef left, NodeRef right)
            {
                return left.node == right.node;
            }

            public static bool operator !=(NodeRef left, NodeRef right)
            {
                return left.node != right.node;
            }

            [ExcludeFromCodeCoverage]
            public override bool Equals(object obj)
            {
                return node.Equals((NodeRef)obj);
            }

            public override int GetHashCode()
            {
                return node.GetHashCode();
            }
        }

        //
        // Array form data structure
        //

        [Storage(Storage.Array)]
        [StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        private struct Node
        {
            public NodeRef left, right;

            // tree is threaded: left_child/right_child indicate "non-null", if false, left/right point to predecessor/successor
            [Feature(Feature.Dict)]
            public bool left_child, right_child;
            // non-threaded for augmented versions
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public bool left_child { get { return left != Null; } }
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public bool right_child { get { return right != Null; } }

            [Feature(Feature.Dict)]
            public NodeRef leftOrNull { get { return left_child ? left : Null; } }
            [Feature(Feature.Dict)]
            public NodeRef rightOrNull { get { return right_child ? right : Null; } }
            //[OR]
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public NodeRef leftOrNull { get { return left; } }
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            public NodeRef rightOrNull { get { return right; } }

            public sbyte balance;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;
            [Payload(Payload.Value)]
            public ValueType value;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;
            [Feature(Feature.Range2)]
            [Widen]
            public int yOffset;

        }

        [Storage(Storage.Array)]
        private static NodeRef Null { get { return new NodeRef(unchecked((uint)-1)); } }

        [Storage(Storage.Array)]
        [StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        private struct NodeRef
        {
            public readonly uint node;

            public NodeRef(uint node)
            {
                this.node = node;
            }

            public static implicit operator uint(NodeRef nodeRef)
            {
                return nodeRef.node;
            }

            public static bool operator ==(NodeRef left, NodeRef right)
            {
                return left.node == right.node;
            }

            public static bool operator !=(NodeRef left, NodeRef right)
            {
                return left.node != right.node;
            }

            [ExcludeFromCodeCoverage]
            public override bool Equals(object obj)
            {
                return node.Equals((NodeRef)obj);
            }

            public override int GetHashCode()
            {
                return node.GetHashCode();
            }
        }

        [Storage(Storage.Array)]
        private const int ReservedElements = 0;
        [Storage(Storage.Array)]
        private Node[] nodes;

        //
        // State for both array & object form
        //

        private NodeRef root;
        [Count]
        private ulong count;
        private uint version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xExtent;
        [Feature(Feature.Range2)]
        [Widen]
        private int yExtent;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private NodeRef freelist;

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
        private readonly WeakReference<NodeRef[]> path = new WeakReference<NodeRef[]>(null);


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
        public AVLTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer, uint capacity, AllocationMode allocationMode)
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
        public AVLTree(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public AVLTree()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public AVLTree(AVLTree<KeyType, ValueType> original)
        {
            this.comparer = original.comparer;

            this.count = original.count;
            this.xExtent = original.xExtent;
            this.yExtent = original.yExtent;

            this.allocationMode = original.allocationMode;
            this.freelist = Null;
            {
                NodeRef nodeOriginal = original.freelist;
                while (nodeOriginal != Null)
                {
                    nodeOriginal = nodes[nodeOriginal].left;
                    NodeRef nodeCopy = new NodeRef(new Node());
                    this.nodes[nodeCopy].left = this.freelist;
                    this.freelist = nodeCopy;
                }
            }
#if DEBUG
            this.allocateCount = original.allocateCount;
#endif

            this.root = Null;
            if (original.root != Null)
            {
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
                    Stack<STuple<NodeRef, NodeRef, NodeRef>> stack = new Stack<STuple<NodeRef, NodeRef, NodeRef>>();

                    NodeRef nodeOriginal = original.root;
                    NodeRef nodeThis = this.root;
                    while (nodeOriginal != Null)
                    {
                        NodeRef nodeChild = new NodeRef(new Node());
                        /*[Feature(Feature.Dict)]*/
                        this.nodes[nodeChild].right = nodeThis; // forward thread

                        if (this.root == Null)
                        {
                            this.root = nodeChild;
                        }
                        else
                        {
                            /*[Feature(Feature.Dict)]*/
                            this.nodes[nodeThis].left_child = true;
                            this.nodes[nodeThis].left = nodeChild;
                        }

                        NodeRef nodeControllingSuccessor = Null;
                        /*[Feature(Feature.Dict)]*/
                        nodeControllingSuccessor = nodeThis;
                        nodeThis = nodeChild;
                        stack.Push(new STuple<NodeRef, NodeRef, NodeRef>(nodeThis, nodeOriginal, nodeControllingSuccessor));
                        nodeOriginal = original.nodes[nodeOriginal].leftOrNull;
                    }
                    while (stack.Count != 0)
                    {
                        STuple<NodeRef, NodeRef, NodeRef> t = stack.Pop();
                        nodeThis = t.Item1;
                        nodeOriginal = t.Item2;
                        NodeRef nodeControllingSuccessorThis = Null;
                        /*[Feature(Feature.Dict)]*/
                        nodeControllingSuccessorThis = t.Item3;
                        /*[Feature(Feature.Dict)]*/
                        {
#if VALIDATE_CLONE_THREADING
                            originalSequence.Add(nodeOriginal, ++originalNumber);
                            thisSequence.Add(nodeThis, ++thisNumber);
#endif
                        } // stops Roslyn from removing leading trivia containing #endif in cases where the next line is removed

                        this.nodes[nodeThis].key = original.nodes[nodeOriginal].key;
                        this.nodes[nodeThis].value = original.nodes[nodeOriginal].value;
                        this.nodes[nodeThis].xOffset = original.nodes[nodeOriginal].xOffset;
                        this.nodes[nodeThis].yOffset = original.nodes[nodeOriginal].yOffset;
                        this.nodes[nodeThis].balance = original.nodes[nodeOriginal].balance;

                        if (original.nodes[nodeOriginal].right_child)
                        {
                            NodeRef nodeChild = new NodeRef(new Node());
                            /*[Feature(Feature.Dict)]*/
                            this.nodes[nodeChild].left = nodeThis; // back thread
                            /*[Feature(Feature.Dict)]*/
                            this.nodes[nodeChild].right = nodeControllingSuccessorThis; // forward thread

                            /*[Feature(Feature.Dict)]*/
                            this.nodes[nodeThis].right_child = true;
                            this.nodes[nodeThis].right = nodeChild;

                            NodeRef nodeControllingPredecessor = Null;
                            /*[Feature(Feature.Dict)]*/
                            nodeControllingPredecessor = nodeThis;

                            nodeOriginal = original.nodes[nodeOriginal].right;
                            nodeThis = nodeChild;
                            stack.Push(new STuple<NodeRef, NodeRef, NodeRef>(nodeThis, nodeOriginal, nodeControllingSuccessorThis));

                            nodeOriginal = original.nodes[nodeOriginal].leftOrNull;
                            while (nodeOriginal != Null)
                            {
                                nodeChild = new NodeRef(new Node());
                                /*[Feature(Feature.Dict)]*/
                                this.nodes[nodeChild].left = nodeControllingPredecessor; // back thread
                                /*[Feature(Feature.Dict)]*/
                                this.nodes[nodeChild].right = nodeThis; // forward thread

                                /*[Feature(Feature.Dict)]*/
                                this.nodes[nodeThis].left_child = true;
                                this.nodes[nodeThis].left = nodeChild;

                                NodeRef nodeControllingSuccessor = Null;
                                /*[Feature(Feature.Dict)]*/
                                nodeControllingSuccessor = nodeThis;
                                nodeThis = nodeChild;
                                stack.Push(new STuple<NodeRef, NodeRef, NodeRef>(nodeThis, nodeOriginal, nodeControllingSuccessor));
                                nodeOriginal = original.nodes[nodeOriginal].leftOrNull;
                            }
                        }
                    }
                }
                /*[Feature(Feature.Dict)]*/
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

        // Array

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, explicitly configured.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys (present only for keyed collections)</param>
        /// <param name="capacity">
        /// For PreallocatedFixed mode, the maximum capacity of the tree, the memory for which is
        /// preallocated at construction time; exceeding that capacity will result in an OutOfMemory exception.
        /// For DynamicRetainFreelist, the number of nodes to pre-allocate at construction time (the collection
        /// is permitted to exceed that capacity, in which case the internal array will be resized to increase the capacity).
        /// DynamicDiscard is not permitted for array storage trees.
        /// </param>
        /// <param name="allocationMode">The allocation mode (see capacity)</param>
        /// <exception cref="ArgumentException">an allocation mode of DynamicDiscard was specified</exception>
        [Storage(Storage.Array)]
        public AVLTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer, uint capacity, AllocationMode allocationMode)
        {
            if (allocationMode == AllocationMode.DynamicDiscard)
            {
                throw new ArgumentException();
            }

            this.comparer = comparer;
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, with the specified capacity and allocation mode and using
        /// the default comparer.
        /// </summary>
        /// <param name="capacity">
        /// For PreallocatedFixed mode, the maximum capacity of the tree, the memory for which is
        /// preallocated at construction time; exceeding that capacity will result in an OutOfMemory exception.
        /// For DynamicRetainFreelist, the number of nodes to pre-allocate at construction time (the collection
        /// is permitted to exceed that capacity, in which case the internal array will be resized to increase the capacity).
        /// DynamicDiscard is not permitted for array storage trees.
        /// </param>
        /// <param name="allocationMode">The allocation mode (see capacity)</param>
        /// <exception cref="ArgumentException">an allocation mode of DynamicDiscard was specified</exception>
        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTree(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL, with the specified capacity and using
        /// the default comparer (applicable only for keyed collections). The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity of the tree, the memory for which is preallocated at construction time;
        /// if the capacity is exceeded, the internal array will be resized to make more nodes available.
        /// </param>
        [Storage(Storage.Array)]
        public AVLTree(uint capacity)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, using
        /// the specified comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTree(IComparer<KeyType> comparer)
            : this(comparer, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, using
        /// the default comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        [Storage(Storage.Array)]
        public AVLTree()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Array)]
        public AVLTree(AVLTree<KeyType, ValueType> original)
        {
            this.comparer = original.comparer;

            this.nodes = (Node[])original.nodes.Clone();
            this.root = original.root;

            this.freelist = original.freelist;
            this.allocationMode = original.allocationMode;

            this.count = original.count;
            this.xExtent = original.xExtent;
            this.yExtent = original.yExtent;
        }


        //
        // IOrderedMap, IOrderedList
        //

        public uint Count { get { return checked((uint)this.count); } }

        public long LongCount { get { return unchecked((long)this.count); } }

        public void Clear()
        {
            // no need to do any work for DynamicDiscard mode
            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                /*[Feature(Feature.Dict)]*/
                {
                    // use threaded feature to traverse in O(1) per node with no stack

                    NodeRef node = g_tree_first_node();

                    while (node != Null)
                    {
                        NodeRef next = g_tree_node_next(node);

                        this.count = unchecked(this.count - 1);
                        g_node_free(node);

                        node = next;
                    }
                }
                //[OR]
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                {
                    // non-recusrive depth-first traversal (in-order, but doesn't matter here)

                    Stack<NodeRef> stack = new Stack<NodeRef>();

                    NodeRef node = root;
                    while (node != Null)
                    {
                        stack.Push(node);
                        node = nodes[node].leftOrNull;
                    }
                    while (stack.Count != 0)
                    {
                        node = stack.Pop();

                        NodeRef dead = node;

                        node = nodes[node].rightOrNull;
                        while (node != Null)
                        {
                            stack.Push(node);
                            node = nodes[node].leftOrNull;
                        }

                        this.count = unchecked(this.count - 1);
                        g_node_free(dead);
                    }
                }

                Debug.Assert(this.count == 0);
            }
            else
            {
                /*[Storage(Storage.Object)]*/
                {
#if DEBUG
                    allocateCount = 0;
#endif
                }
            }

            root = Null;
            this.count = 0;
            this.xExtent = 0;
            this.yExtent = 0;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            NodeRef node = g_tree_find_node(key);
            return node != Null;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict)]
        public bool SetOrAddValue(KeyType key, ValueType value)
        {
            return g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                true/*add*/,
                true/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        [Feature(Feature.Dict)]
        public bool TryAdd(KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            return g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                true/*add*/,
                false/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key, out KeyType keyOut)
        {
            NodeRef node = g_tree_find_node(key);
            if (node != Null)
            {
                keyOut = nodes[node].key;
                return true;
            }
            keyOut = default(KeyType);
            return false;
        }

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetKey(KeyType key)
        {
            return g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/default(ValueType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                false/*add*/,
                true/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetValue(KeyType key, out ValueType value)
        {
            NodeRef node = g_tree_find_node(key);
            if (node != Null)
            {
                value = nodes[node].value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetValue(KeyType key, ValueType value)
        {
            NodeRef node = g_tree_find_node(key);
            if (node != Null)
            {
                nodes[node].value = value;
                return true;
            }
            return false;
        }

        [Feature(Feature.Dict)]
        public void Add(KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            if (!TryAdd(key, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void Remove(KeyType key)
        {
            if (!TryRemove(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

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

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void SetKey(KeyType key)
        {
            if (!TrySetKey(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public ValueType GetValue(KeyType key)
        {
            ValueType value;
            if (!TryGetValue(key, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void SetValue(KeyType key, ValueType value)
        {
            if (!TrySetValue(key, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank)]
        public void ConditionalSetOrAdd(KeyType key, [Payload(Payload.Value)]UpdatePredicate<KeyType, ValueType> predicateMap, [Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            /*[Payload(Payload.Value)]*/
            if (predicateMap == null)
            {
                throw new ArgumentNullException();
            }
            /*[Payload(Payload.None)]*/
            if (predicateList == null)
            {
                throw new ArgumentNullException();
            }

            g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/default(ValueType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/1,
                /*[Feature(Feature.Range2)]*/0,
                false/*add - overridden by predicate*/,
                true/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/predicateMap,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/predicateList);
        }

        [Feature(Feature.Dict, Feature.Rank)]
        public void ConditionalSetOrRemove(KeyType key, [Payload(Payload.Value)]UpdatePredicate<KeyType, ValueType> predicateMap, [Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            /*[Payload(Payload.Value)]*/
            if (predicateMap == null)
            {
                throw new ArgumentNullException();
            }
            /*[Payload(Payload.None)]*/
            if (predicateList == null)
            {
                throw new ArgumentNullException();
            }

            g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/predicateMap,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/predicateList);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool LeastInternal(out KeyType keyOut, [Payload(Payload.Value)] out ValueType valueOut)
        {
            NodeRef node = g_tree_first_node();
            if (node == Null)
            {
                keyOut = default(KeyType);
                valueOut = default(ValueType);
                return false;
            }
            keyOut = nodes[node].key;
            valueOut = nodes[node].value;
            return true;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType keyOut, [Payload(Payload.Value)] out ValueType valueOut)
        {
            return LeastInternal(out keyOut, /*[Payload(Payload.Value)]*/out valueOut);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType keyOut)
        {
            ValueType value;
            return LeastInternal(out keyOut, /*[Payload(Payload.Value)]*/out value);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool GreatestInternal(out KeyType keyOut, [Payload(Payload.Value)] out ValueType valueOut)
        {
            NodeRef node = g_tree_last_node();
            if (node == Null)
            {
                keyOut = default(KeyType);
                valueOut = default(ValueType);
                return false;
            }
            keyOut = nodes[node].key;
            valueOut = nodes[node].value;
            return true;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut, [Payload(Payload.Value)] out ValueType valueOut)
        {
            return GreatestInternal(out keyOut, /*[Payload(Payload.Value)]*/out valueOut);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut)
        {
            ValueType value;
            return GreatestInternal(out keyOut, /*[Payload(Payload.Value)]*/out value);
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            valueOut = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
            return f;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            valueOut = default(ValueType);
            rank = 0;
            rankCount = 0;
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out valueOut, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            valueOut = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
            return f;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            valueOut = default(ValueType);
            rank = 0;
            rankCount = 0;
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out valueOut, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            valueOut = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
            return f;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            valueOut = default(ValueType);
            rank = this.xExtent;
            rankCount = 0;
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out valueOut, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            valueOut = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
            return f;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            valueOut = default(ValueType);
            rank = this.xExtent;
            rankCount = 0;
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out valueOut, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            /*[Widen]*/
            int yPosition, yLength;
            return FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition));
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xLength, [Feature(Feature.Range2)][Widen] int yLength, [Payload(Payload.Value)] ValueType value)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (xLength <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            /*[Feature(Feature.Range2)]*/
            if (yLength <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Payload(Payload.Value)]*/value,
                start,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature()]*/CompareKeyMode.Position,
                xLength,
                /*[Feature(Feature.Range2)]*/yLength,
                true/*add*/,
                false/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                start,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature()]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out int length)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            /*[Widen]*/
            int yPosition, yLength;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/
            side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                length = side == Side.X ? xLength : yLength;
                return true;
            }
            length = 0;
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            /*[Widen]*/
            int yPosition, yLength;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/
            side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                /*[Widen]*/
                int adjust = length - (side == Side.X ? xLength : yLength);
                /*[Widen]*/
                int xAdjust = 0;
                /*[Widen]*/
                int yAdjust = 0;
                if (side == Side.X)
                {
                    xAdjust = adjust;
                }
                else
                {
                    yAdjust = adjust;
                }

                // throw OverflowException before modifying anything
                /*[Widen]*/
                int newXExtent = checked(this.xExtent + xAdjust);
                /*[Widen]*/
                int newYExtent = checked(this.yExtent + yAdjust);
                this.xExtent = newXExtent;
                this.yExtent = newYExtent;

                ShiftRightOfPath(unchecked(start + 1), /*[Feature(Feature.Range2)]*/side, xAdjust, /*[Feature(Feature.Range2)]*/yAdjust);

                return true;
            }
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, out ValueType value)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            /*[Widen]*/
            int yPosition, yLength;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/
            side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                value = nodes[node].value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, ValueType value)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            /*[Widen]*/
            int yPosition, yLength;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/
            side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                nodes[node].value = value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.Range2)] [Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition;
            /*[Widen]*/
            int yPosition;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                otherStart = side != Side.X ? xPosition : yPosition;
                value = nodes[node].value;
                return true;
            }
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            return false;
        }

        [Exclude(Feature.Range, Payload.None)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySet([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xLength, [Feature(Feature.Range2)][Widen] int yLength, [Payload(Payload.Value)] ValueType value)
        {
            if (xLength < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            /*[Feature(Feature.Range2)]*/
            if (yLength < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            NodeRef node;
            /*[Widen]*/
            int xPosition, xLengthOld;
            /*[Widen]*/
            int yPosition, yLengthOld;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLengthOld, /*[Feature(Feature.Range2)]*/out yLengthOld)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                /*[Widen]*/
                int xAdjust = xLength != 0 ? xLength - xLengthOld : 0;
                /*[Widen]*/
                int yAdjust = yLength != 0 ? yLength - yLengthOld : 0;

                // throw OverflowException before modifying anything
                /*[Widen]*/
                int newXExtent = checked(this.xExtent + xAdjust);
                /*[Widen]*/
                int newYExtent = checked(this.yExtent + yAdjust);
                this.xExtent = newXExtent;
                this.yExtent = newYExtent;

                ShiftRightOfPath(unchecked(start + 1), /*[Feature(Feature.Range2)]*/side, xAdjust, /*[Feature(Feature.Range2)]*/yAdjust);

                nodes[node].value = value;

                return true;
            }
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xLength, [Feature(Feature.Range2)] [Widen] int yLength, [Payload(Payload.Value)] ValueType value)
        {
            if (!TryInsert(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            if (!TryDelete(start, /*[Feature(Feature.Range2)]*/side))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetLength([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            /*[Widen]*/
            int length;
            if (!TryGetLength(start, /*[Feature(Feature.Range2)]*/
            side, out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int length)
        {
            if (!TrySetLength(start, /*[Feature(Feature.Range2)]*/side, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public ValueType GetValue([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            ValueType value;
            if (!TryGetValue(start, /*[Feature(Feature.Range2)]*/side, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public void SetValue([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, ValueType value)
        {
            if (!TrySetValue(start, /*[Feature(Feature.Range2)]*/side, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            if (!TryGet(start, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Exclude(Feature.Range, Payload.None)]
        [Feature(Feature.Range, Feature.Range2)]
        public void Set([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xLength, [Feature(Feature.Range2)][Widen] int yLength, [Payload(Payload.Value)] ValueType value)
        {
            if (!TrySet(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetExtent([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return side == Side.X ? this.xExtent : this.yExtent;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            NodeRef nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                nearestKey = nodes[nearestNode].key;
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            NodeRef nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                nearestKey = nodes[nearestNode].key;
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            NodeRef nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                nearestKey = nodes[nearestNode].key;
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            NodeRef nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Position,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                nearestKey = nodes[nearestNode].key;
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int AdjustLength([Widen] int startIndex, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xAdjust, [Feature(Feature.Range2)] [Widen] int yAdjust)
        {
            unchecked
            {
                NodeRef node;
                /*[Widen]*/
                int xPosition, yPosition;
                /*[Widen]*/
                int xLength = 1, yLength = 1;
                if (!FindPosition(startIndex, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                    || (startIndex != (side == Side.X ? xPosition : yPosition)))
                {
                    throw new ArgumentException();
                }

                /*[Widen]*/
                int newXLength = checked(xLength + xAdjust);
                /*[Widen]*/
                int newYLength = 0;
                newYLength = checked(yLength + yAdjust);

                if ((newXLength < 0) || (newYLength < 0))
                {
                    throw new ArgumentOutOfRangeException();
                }
                /*[Feature(Feature.Range2)]*/
                if ((newXLength == 0) != (newYLength == 0))
                {
                    throw new ArgumentException();
                }

                if (newXLength != 0)
                {
                    // adjust

                    // throw OverflowException before modifying anything
                    /*[Widen]*/
                    int newXExtent = checked(this.xExtent + xAdjust);
                    /*[Widen]*/
                    int newYExtent = checked(this.yExtent + yAdjust);
                    this.xExtent = newXExtent;
                    this.yExtent = newYExtent;

                    ShiftRightOfPath(startIndex + 1, /*[Feature(Feature.Range2)]*/side, xAdjust, /*[Feature(Feature.Range2)]*/yAdjust);

                    return side == Side.X ? newXLength : newYLength;
                }
                else
                {
                    // delete

                    Debug.Assert(xAdjust < 0);
                    Debug.Assert(yAdjust < 0);
                    Debug.Assert(newXLength == 0);
                    /*[Feature(Feature.Range2)]*/
                    Debug.Assert(newYLength == 0);

                    g_tree_remove_internal(
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                        startIndex,
                        /*[Feature(Feature.Range2)]*/side,
                        /*[Feature()]*/CompareKeyMode.Position,
                        /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                        /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);

                    return 0;
                }
            }
        }


        //
        // IRankMap, IMultiRankMap, IRankList, IMultiRankList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public int RankCount { get { return this.xExtent; } }

        // ContainsKey() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryAdd(KeyType key, [Payload(Payload.Value)] ValueType value, [Feature(Feature.RankMulti)] [Const(1, Feature.Rank)] [SuppressConst(Feature.RankMulti)][Widen] int rankCount)
        {
            if (rankCount <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return g_tree_insert_internal(
                key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                rankCount,
                /*[Feature(Feature.Range2)]*/0,
                true/*add*/,
                false/*update*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
                /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.None)]*/null/*predicateList*/);
        }

        // TryRemove() - reuses Feature.Dict implementation

        // TryGetValue() - reuses Feature.Dict implementation

        // TrySetValue() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGet(KeyType key, [Payload(Payload.None)] out KeyType keyOut, [Payload(Payload.Value)] out ValueType value, [Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            /*[Widen]*/
            int yPosition, yLength;
            if (Find(key, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, /*[Feature(Feature.RankMulti)]*/out xLength, /*[Feature(Feature.Range2)]*/out yLength))
            {
                keyOut = nodes[node].key;
                value = nodes[node].value;
                rank = xPosition;
                rankCount = xLength;
                return true;
            }
            keyOut = default(KeyType);
            value = default(ValueType);
            rank = 0;
            rankCount = 0;
            return false;
        }

        [Feature(Feature.RankMulti)]
        public bool TrySet(KeyType key, [Payload(Payload.Value)] ValueType value, [Widen] int rankCount)
        {
            unchecked
            {
                NodeRef node;
                /*[Widen]*/
                int xPosition;
                /*[Widen]*/
                int xLength;
                /*[Widen]*/
                int yPosition, yLength;
                if (Find(key, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, /*[Feature(Feature.RankMulti)]*/out xLength, /*[Feature(Feature.Range2)]*/out yLength))
                {
                    /*[Widen]*/
                    if (rankCount > 0)
                    {
                        /*[Widen]*/
                        int countAdjust = checked(rankCount - xLength);
                        this.xExtent = checked(this.xExtent + countAdjust);

                        ShiftRightOfPath(unchecked(xPosition + 1), /*[Feature(Feature.Range2)]*/Side.X, countAdjust, /*[Feature(Feature.Range2)]*/0);

                        nodes[node].value = value;
                        /*[Payload(Payload.None)]*/
                        nodes[node].key = key;

                        return true;
                    }
                }

                return false;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGetKeyByRank([Widen] int rank, out KeyType key)
        {
            if (rank < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            NodeRef node;
            /*[Widen]*/
            int xPosition;
            /*[Feature(Feature.RankMulti)]*/
            /*[Widen]*/
            int xLength;
            /*[Widen]*/
            int yPosition, yLength;
            if (FindPosition(rank, /*[Feature(Feature.Range2)]*/
            Side.X, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, /*[Feature(Feature.RankMulti)]*/out xLength, /*[Feature(Feature.Range2)]*/out yLength))
            {
                key = nodes[node].key;
                return true;
            }
            key = default(KeyType);
            return false;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Add(KeyType key, [Payload(Payload.Value)] ValueType value, [Feature(Feature.RankMulti)][Widen] int rankCount)
        {
            if (!TryAdd(key, /*[Payload(Payload.Value)]*/value, /*[Feature(Feature.RankMulti)]*/rankCount))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        // Remove() - reuses Feature.Dict implementation

        // GetValue() - reuses Feature.Dict implementation

        // SetValue() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Get(KeyType key, [Payload(Payload.None)] out KeyType keyOut, [Payload(Payload.Value)] out ValueType value, [Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            if (!TryGet(key, /*[Payload(Payload.None)]*/out keyOut, /*[Payload(Payload.Value)]*/out value, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.RankMulti)]
        public void Set(KeyType key, [Payload(Payload.Value)] ValueType value, [Widen] int rankCount)
        {
            if (rankCount < 1)
            {
                throw new ArgumentException("rankCount");
            }
            if (!TrySet(key, /*[Payload(Payload.Value)]*/value, rankCount))
            {
                throw new ArgumentException("item not in tree");
            }
        }

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

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public int AdjustCount(KeyType key, [Widen] int countAdjust)
        {
            unchecked
            {
                NodeRef node;
                /*[Widen]*/
                int xPosition;
                /*[Widen]*/
                int xLength = 1;
                /*[Widen]*/
                int yPosition, yLength;
                if (Find(key, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, /*[Feature(Feature.RankMulti)]*/out xLength, /*[Feature(Feature.Range2)]*/out yLength))
                {
                    // update and possibly remove

                    /*[Widen]*/
                    int adjustedLength = checked(xLength + countAdjust);
                    if (adjustedLength > 0)
                    {
                        /*[Feature(Feature.Rank)]*/
                        if (adjustedLength > 1)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        this.xExtent = checked(this.xExtent + countAdjust);

                        ShiftRightOfPath(unchecked(xPosition + 1), /*[Feature(Feature.Range2)]*/Side.X, countAdjust, /*[Feature(Feature.Range2)]*/0);

                        return adjustedLength;
                    }
                    else if (xLength + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        this.g_tree_remove_internal(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                            /*[Feature(Feature.Range2)]*/(Side)0,
                            /*[Feature()]*/CompareKeyMode.Key,
                            /*[Feature(Feature.Dict, Feature.Rank)]*//*[Payload(Payload.Value)]*/null/*predicateMap*/,
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

                        Add(key, /*[Payload(Payload.Value)]*/default(ValueType), /*[Feature(Feature.RankMulti)]*/countAdjust);

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
        private NodeRef g_tree_node_new([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            NodeRef node = freelist;
            if (node != Null)
            {
                freelist = nodes[freelist].left;
            }
            else if (allocationMode == AllocationMode.PreallocatedFixed)
            {
                const string Message = "Tree capacity exhausted but is locked";
                throw new OutOfMemoryException(Message);
            }
            else
            {
                node = new NodeRef(new Node());
            }

            {
#if DEBUG
                allocateCount = checked(allocateCount + 1);
#endif
            }

            nodes[node].key = key;
            nodes[node].value = value;
            nodes[node].left = Null;
            /*[Feature(Feature.Dict)]*/
            nodes[node].left_child = false;
            nodes[node].right = Null;
            /*[Feature(Feature.Dict)]*/
            nodes[node].right_child = false;
            nodes[node].balance = 0;
            nodes[node].xOffset = 0;
            nodes[node].yOffset = 0;

            return node;
        }

        [Storage(Storage.Object)]
        private void g_node_free(NodeRef node)
        {
#if DEBUG
            allocateCount = checked(allocateCount - 1);
            Debug.Assert(allocateCount == this.count);

            nodes[node].left = Null;
            /*[Feature(Feature.Dict)]*/
            nodes[node].left_child = true;
            nodes[node].right = Null;
            /*[Feature(Feature.Dict)]*/
            nodes[node].right_child = true;
            nodes[node].balance = SByte.MinValue;
            nodes[node].xOffset = Int32.MinValue;
            nodes[node].yOffset = Int32.MinValue;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                nodes[node].key = default(KeyType); // clear any references for GC
                nodes[node].value = default(ValueType); // clear any references for GC

                nodes[node].left = freelist;
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
                    NodeRef node = new NodeRef(new Node());
                    nodes[node].left = freelist;
                    freelist = node;
                }
            }
        }

        // Array allocation

        [Storage(Storage.Array)]
        private NodeRef g_tree_node_new([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            if (freelist == Null)
            {
                if (allocationMode == AllocationMode.PreallocatedFixed)
                {
                    const string Message = "Tree capacity exhausted but is locked";
                    throw new OutOfMemoryException(Message);
                }
                Debug.Assert(unchecked((uint)nodes.Length) >= ReservedElements);
                long oldCapacity = unchecked((uint)nodes.Length - ReservedElements);
                uint newCapacity = unchecked((uint)Math.Min(Math.Max(oldCapacity * 2L, 1L), UInt32.MaxValue - ReservedElements));
                EnsureFree(newCapacity);
                if (freelist == Null)
                {
                    throw new OutOfMemoryException();
                }
            }
            NodeRef node = freelist;
            freelist = nodes[freelist].left;

            nodes[node].key = key;
            nodes[node].value = value;
            nodes[node].left = Null;
            /*[Feature(Feature.Dict)]*/
            nodes[node].left_child = false;
            nodes[node].right = Null;
            /*[Feature(Feature.Dict)]*/
            nodes[node].right_child = false;
            nodes[node].balance = 0;
            nodes[node].xOffset = 0;
            nodes[node].yOffset = 0;

            return node;
        }

        [Storage(Storage.Array)]
        private void g_node_free(NodeRef node)
        {
            nodes[node].key = default(KeyType); // zero any contained references for garbage collector
            nodes[node].value = default(ValueType); // zero any contained references for garbage collector

            nodes[node].left = freelist;
            freelist = node;
        }

        [Storage(Storage.Array)]
        private void EnsureFree(uint capacity)
        {
            unchecked
            {
                Debug.Assert(freelist == Null);
                Debug.Assert((nodes == null) || (nodes.Length >= ReservedElements));

                uint oldLength = nodes != null ? unchecked((uint)nodes.Length) : ReservedElements;
                uint newLength = checked(capacity + ReservedElements);

                Array.Resize(ref nodes, unchecked((int)newLength));

                for (long i = (long)newLength - 1; i >= oldLength; i--)
                {
                    g_node_free(new NodeRef(unchecked((uint)i)));
                }
            }
        }


        private NodeRef g_tree_first_node()
        {
            if (root == Null)
            {
                return Null;
            }

            NodeRef tmp = root;

            while (nodes[tmp].left_child)
            {
                tmp = nodes[tmp].left;
            }

            return tmp;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private NodeRef g_tree_last_node()
        {
            if (root == Null)
            {
                return Null;
            }

            NodeRef tmp = root;

            while (nodes[tmp].right_child)
            {
                tmp = nodes[tmp].right;
            }

            return tmp;
        }

        private bool NearestLess(
            out NodeRef nearestNode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature(Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.RankMulti)] CompareKeyMode mode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,
            bool orEqual)
        {
            unchecked
            {
                NodeRef lastLess = Null;
                /*[Widen]*/
                int xPositionLastLess = 0;
                /*[Widen]*/
                int yPositionLastLess = 0;
                if (root != Null)
                {
                    NodeRef node = root;
                    /*[Widen]*/
                    int xPosition = 0;
                    /*[Widen]*/
                    int yPosition = 0;
                    while (true)
                    {
                        xPosition += nodes[node].xOffset;
                        yPosition += nodes[node].yOffset;

                        int c;
                        if (mode == CompareKeyMode.Key)
                        {
                            c = comparer.Compare(key, nodes[node].key);
                        }
                        else
                        {
                            Debug.Assert(mode == CompareKeyMode.Position);
                            c = position.CompareTo(side == Side.X ? xPosition : yPosition);
                        }
                        if (orEqual && (c == 0))
                        {
                            nearestNode = node;
                            nearestKey = nodes[node].key;
                            nearestStart = side == Side.X ? xPosition : yPosition;
                            return true;
                        }
                        NodeRef next;
                        if (c <= 0)
                        {
                            if (!nodes[node].left_child)
                            {
                                break;
                            }
                            next = nodes[node].left;
                        }
                        else
                        {
                            lastLess = node;
                            xPositionLastLess = xPosition;
                            yPositionLastLess = yPosition;

                            if (!nodes[node].right_child)
                            {
                                break;
                            }
                            next = nodes[node].right;
                        }
                        Debug.Assert(next != Null);
                        node = next;
                    }
                }
                if (lastLess != Null)
                {
                    nearestNode = lastLess;
                    nearestKey = nodes[lastLess].key;
                    nearestStart = side == Side.X ? xPositionLastLess : yPositionLastLess;
                    return true;
                }
                nearestNode = Null;
                nearestKey = default(KeyType);
                nearestStart = 0;
                return false;
            }
        }

        private bool NearestGreater(
            out NodeRef nearestNode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature(Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.RankMulti)] CompareKeyMode mode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,
            bool orEqual)
        {
            unchecked
            {
                NodeRef lastGreater = Null;
                /*[Widen]*/
                int xPositionLastGreater = 0;
                /*[Widen]*/
                int yPositionLastGreater = 0;
                if (root != Null)
                {
                    NodeRef node = root;
                    /*[Widen]*/
                    int xPosition = 0;
                    /*[Widen]*/
                    int yPosition = 0;
                    while (true)
                    {
                        xPosition += nodes[node].xOffset;
                        yPosition += nodes[node].yOffset;

                        int c;
                        if (mode == CompareKeyMode.Key)
                        {
                            c = comparer.Compare(key, nodes[node].key);
                        }
                        else
                        {
                            Debug.Assert(mode == CompareKeyMode.Position);
                            c = position.CompareTo(side == Side.X ? xPosition : yPosition);
                        }
                        if (orEqual && (c == 0))
                        {
                            nearestNode = node;
                            nearestKey = nodes[node].key;
                            nearestStart = side == Side.X ? xPosition : yPosition;
                            return true;
                        }
                        NodeRef next;
                        if (c < 0)
                        {
                            lastGreater = node;
                            xPositionLastGreater = xPosition;
                            yPositionLastGreater = yPosition;

                            if (!nodes[node].left_child)
                            {
                                break;
                            }
                            next = nodes[node].left;
                        }
                        else
                        {
                            if (!nodes[node].right_child)
                            {
                                break;
                            }
                            next = nodes[node].right;
                        }
                        Debug.Assert(next != Null);
                        node = next;
                    }
                }
                if (lastGreater != Null)
                {
                    nearestNode = lastGreater;
                    nearestKey = nodes[lastGreater].key;
                    nearestStart = side == Side.X ? xPositionLastGreater : yPositionLastGreater;
                    return true;
                }
                nearestNode = Null;
                nearestKey = default(KeyType);
                nearestStart = side == Side.X ? this.xExtent : this.yExtent;
                return false;
            }
        }

        [Feature(Feature.Dict)]
        private NodeRef g_tree_node_previous(NodeRef node)
        {
            NodeRef tmp = nodes[node].left;

            if (nodes[node].left_child)
            {
                while (nodes[tmp].right_child)
                {
                    tmp = nodes[tmp].right;
                }
            }

            return tmp;
        }

        [Feature(Feature.Dict)]
        private NodeRef g_tree_node_next(NodeRef node)
        {
            NodeRef tmp = nodes[node].right;

            if (nodes[node].right_child)
            {
                while (nodes[tmp].left_child)
                {
                    tmp = nodes[tmp].left;
                }
            }

            return tmp;
        }

        private NodeRef[] RetrievePathWorkspace()
        {
            NodeRef[] path;
            this.path.TryGetTarget(out path);
            if (path == null)
            {
                path = new NodeRef[MAX_GTREE_HEIGHT];
                this.path.SetTarget(path);
            }
            return path;
        }

        [Feature(Feature.Dict, Feature.Rank)]
        private bool PredicateAddRemoveOverrideCore(
            bool initial,
            bool resident,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]ref KeyType key,
            [Payload(Payload.Value)]ref ValueType value,
            [Payload(Payload.Value)]UpdatePredicate<KeyType, ValueType> predicateMap,
            [Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            uint version = this.version;

            // very crude protection against completely trashing the tree if the predicate tries to modify it
            NodeRef savedRoot = this.root;
            this.root = Null;
            /*[Count]*/
            ulong savedCount = this.count;
            this.count = 0;
            /*[Widen]*/
            int savedXExtent = this.xExtent;
            this.xExtent = 0;
            /*[Widen]*/
            int savedYExtent = this.yExtent;
            this.yExtent = 0;
            try
            {
                /*[Payload(Payload.Value)]*/
                initial = predicateMap(key, ref value, resident);
                //[OR]
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
            }
            finally
            {
                this.root = savedRoot;
                this.count = savedCount;
                this.xExtent = savedXExtent;
                this.yExtent = savedYExtent;
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
            [Payload(Payload.Value)]ref ValueType value,
            [Payload(Payload.Value)]UpdatePredicate<KeyType, ValueType> predicateMap,
            [Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            bool predicateExists = false;
            /*[Payload(Payload.Value)]*/
            predicateExists = predicateMap != null;
            /*[Payload(Payload.None)]*/
            predicateExists = predicateList != null;
            if (predicateExists)
            {
                initial = PredicateAddRemoveOverrideCore(
                    initial,
                    resident,
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ref key,
                    /*[Payload(Payload.Value)]*/ref value,
                    /*[Payload(Payload.Value)]*/predicateMap,
                    /*[Payload(Payload.None)]*/predicateList);
            }

            return initial;
        }

        // NOTE: replace mode does *not* adjust for xLength/yLength!
        private bool g_tree_insert_internal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Payload(Payload.Value)] ValueType value,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature()] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank, Feature.RankMulti)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] CompareKeyMode mode,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,
            [Feature(Feature.Range2)][Widen] int yLength,
            bool add,
            bool update,
            [Feature(Feature.Dict, Feature.Rank)][Payload(Payload.Value)]UpdatePredicate<KeyType, ValueType> predicateMap,
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
                        /*[Payload(Payload.Value)]*/ref value,
                        /*[Payload(Payload.Value)]*/predicateMap,
                        /*[Payload(Payload.None)]*/predicateList);

                    if (!add)
                    {
                        return false;
                    }

                    if (mode == CompareKeyMode.Position)
                    {
                        if (position != 0)
                        {
                            return false;
                        }
                    }

                    root = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value);
                    Debug.Assert(nodes[root].xOffset == 0);
                    Debug.Assert(nodes[root].yOffset == 0);
                    Debug.Assert(this.xExtent == 0);
                    Debug.Assert(this.yExtent == 0);
                    this.xExtent = xLength;
                    this.yExtent = yLength;

                    Debug.Assert(this.count == 0);
                    this.count = 1;
                    this.version = unchecked(this.version + 1);

                    return true;
                }

                NodeRef[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;
                NodeRef node = root;

                NodeRef successor = Null;
                /*[Widen]*/
                int xPositionSuccessor = 0;
                /*[Widen]*/
                int yPositionSuccessor = 0;
                /*[Widen]*/
                int xPositionNode = 0;
                /*[Widen]*/
                int yPositionNode = 0;
                bool addleft = false;
                while (true)
                {
                    xPositionNode += nodes[node].xOffset;
                    yPositionNode += nodes[node].yOffset;

                    int cmp;
                    if (mode == CompareKeyMode.Key)
                    {
                        cmp = comparer.Compare(key, nodes[node].key);
                    }
                    else
                    {
                        Debug.Assert(mode == CompareKeyMode.Position);
                        cmp = position.CompareTo(side == Side.X ? xPositionNode : yPositionNode);
                        if (add && (cmp == 0))
                        {
                            cmp = -1; // node never found for sparse range mode
                        }
                    }

                    if (cmp == 0)
                    {
                        bool predicateExists = false;
                        /*[Payload(Payload.Value)]*/
                        predicateExists = predicateMap != null;
                        /*[Payload(Payload.None)]*/
                        predicateExists = predicateList != null;
                        if (predicateExists)
                        {
                            value = nodes[node].value;
                            /*[Feature(Feature.Dict, Feature.Rank)]*/
                            PredicateAddRemoveOverride(
                                false/*initial*/,
                                true/*resident*/,
                                ref key,
                                /*[Payload(Payload.Value)]*/ref value,
                                /*[Payload(Payload.Value)]*/predicateMap,
                                /*[Payload(Payload.None)]*/predicateList);
                        }

                        if (update)
                        {
                            /*[Payload(Payload.None)]*/
                            {
                                Debug.Assert(0 == comparer.Compare(nodes[node].key, key));
                                nodes[node].key = key;
                            }
                            nodes[node].value = value;
                        }
                        return !add;
                    }

                    if (cmp < 0)
                    {
                        successor = node;
                        xPositionSuccessor = xPositionNode;
                        yPositionSuccessor = yPositionNode;

                        if (nodes[node].left_child)
                        {
                            path[idx++] = node;
                            node = nodes[node].left;
                        }
                        else
                        {
                            // precedes node

                            /*[Feature(Feature.Dict, Feature.Rank)]*/
                            add = PredicateAddRemoveOverride(
                                add,
                                false/*resident*/,
                                ref key,
                                /*[Payload(Payload.Value)]*/ref value,
                                /*[Payload(Payload.Value)]*/predicateMap,
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

                        if (nodes[node].right_child)
                        {
                            path[idx++] = node;
                            node = nodes[node].right;
                        }
                        else
                        {
                            // follows node

                            /*[Feature(Feature.Dict, Feature.Rank)]*/
                            add = PredicateAddRemoveOverride(
                                add,
                                false/*resident*/,
                                ref key,
                                /*[Payload(Payload.Value)]*/ref value,
                                /*[Payload(Payload.Value)]*/predicateMap,
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

                    if (mode == CompareKeyMode.Position)
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        int positionNode = side == Side.X ? xPositionNode : yPositionNode;
                        if (position != positionNode)
                        {
                            return false;
                        }
                    }

                    this.version = unchecked(this.version + 1);

                    // throw here before modifying tree
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);
                    /*[Widen]*/
                    int yExtentNew = checked(this.yExtent + yLength);
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    NodeRef child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value);

                    ShiftRightOfPath(xPositionNode, /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                    /*[Feature(Feature.Dict)]*/
                    nodes[child].left = nodes[node].left; // back thread
                    /*[Feature(Feature.Dict)]*/
                    nodes[child].right = node; // forward thread
                    nodes[node].left = child;
                    /*[Feature(Feature.Dict)]*/
                    nodes[node].left_child = true;
                    nodes[node].balance--;

                    nodes[child].xOffset = -xLength;
                    nodes[child].yOffset = -yLength;

                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;
                    this.count = countNew;
                }
                else
                {
                    // follows node

                    Debug.Assert(!nodes[node].right_child);

                    /*[Widen]*/
                    int xLengthNode;
                    /*[Widen]*/
                    int yLengthNode;
                    if (successor != Null)
                    {
                        xLengthNode = xPositionSuccessor - xPositionNode;
                        yLengthNode = yPositionSuccessor - yPositionNode;
                    }
                    else
                    {
                        xLengthNode = this.xExtent - xPositionNode;
                        yLengthNode = this.yExtent - yPositionNode;
                    }

                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    if ((mode == CompareKeyMode.Position)
                        && (position != (side == Side.X ? xPositionNode + xLengthNode : yPositionNode + yLengthNode)))
                    {
                        return false;
                    }

                    this.version = unchecked(this.version + 1);

                    // throw here before modifying tree
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);
                    /*[Widen]*/
                    int yExtentNew = checked(this.yExtent + yLength);
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    NodeRef child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value);

                    ShiftRightOfPath(xPositionNode + 1, /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                    /*[Feature(Feature.Dict)]*/
                    nodes[child].right = nodes[node].right; // forward thread
                    /*[Feature(Feature.Dict)]*/
                    nodes[child].left = node; // back thread
                    nodes[node].right = child;
                    /*[Feature(Feature.Dict)]*/
                    nodes[node].right_child = true;
                    nodes[node].balance++;

                    nodes[child].xOffset = xLengthNode;
                    nodes[child].yOffset = yLengthNode;

                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;
                    this.count = countNew;
                }

                // Restore balance. This is the goodness of a non-recursive
                // implementation, when we are done with balancing we 'break'
                // the loop and we are done.
                while (true)
                {
                    NodeRef bparent = path[--idx];
                    bool left_node = (bparent != Null) && (node == nodes[bparent].left);
                    Debug.Assert((bparent == Null) || (nodes[bparent].left == node) || (nodes[bparent].right == node));

                    if ((nodes[node].balance < -1) || (nodes[node].balance > 1))
                    {
                        node = g_tree_node_balance(node);
                        if (bparent == Null)
                        {
                            root = node;
                        }
                        else if (left_node)
                        {
                            nodes[bparent].left = node;
                        }
                        else
                        {
                            nodes[bparent].right = node;
                        }
                    }

                    if ((nodes[node].balance == 0) || (bparent == Null))
                    {
                        break;
                    }

                    if (left_node)
                    {
                        nodes[bparent].balance--;
                    }
                    else
                    {
                        nodes[bparent].balance++;
                    }

                    node = bparent;
                }

                return true;
            }
        }

        private bool g_tree_remove_internal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature()] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank, Feature.RankMulti)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] CompareKeyMode mode,
            [Feature(Feature.Dict, Feature.Rank)][Payload(Payload.Value)]UpdatePredicate<KeyType, ValueType> predicateMap,
            [Feature(Feature.Dict, Feature.Rank)][Payload(Payload.None)]UpdatePredicate<KeyType> predicateList)
        {
            unchecked
            {
                if (root == Null)
                {
                    return false;
                }

                NodeRef[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;

                NodeRef node = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionNode = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionNode = 0;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParent = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionParent = 0;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                NodeRef lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionLastGreaterAncestor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionLastGreaterAncestor = 0;
                while (true)
                {
                    Debug.Assert(node != Null);

                    xPositionNode += nodes[node].xOffset;
                    yPositionNode += nodes[node].yOffset;

                    int cmp;
                    if (mode == CompareKeyMode.Key)
                    {
                        cmp = comparer.Compare(key, nodes[node].key);
                    }
                    else
                    {
                        Debug.Assert(mode == CompareKeyMode.Position);
                        cmp = position.CompareTo(side == Side.X ? xPositionNode : yPositionNode);
                    }

                    if (cmp == 0)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank)] */
                        {
                            ValueType value = nodes[node].value;
                            bool remove = PredicateAddRemoveOverride(
                                true/*initial*/,
                                true/*resident*/,
                                ref key,
                                /*[Payload(Payload.Value)]*/ref value,
                                /*[Payload(Payload.Value)]*/predicateMap,
                                /*[Payload(Payload.None)]*/predicateList);

                            if (!remove)
                            {
                                /*[Payload(Payload.None)]*/
                                {
                                    Debug.Assert(0 == comparer.Compare(nodes[node].key, key));
                                    nodes[node].key = key;
                                }
                                nodes[node].value = value;

                                return false;
                            }
                        }

                        break;
                    }

                    xPositionParent = xPositionNode;
                    yPositionParent = yPositionNode;

                    if (cmp < 0)
                    {
                        if (!nodes[node].left_child)
                        {
                            return false;
                        }

                        lastGreaterAncestor = node;
                        xPositionLastGreaterAncestor = xPositionNode;
                        yPositionLastGreaterAncestor = yPositionNode;

                        path[idx++] = node;
                        node = nodes[node].left;
                    }
                    else
                    {
                        if (!nodes[node].right_child)
                        {
                            return false;
                        }

                        path[idx++] = node;
                        node = nodes[node].right;
                    }
                }

                this.version = unchecked(this.version + 1);

                NodeRef successor;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionSuccessor;

                // The following code is almost equal to g_tree_remove_node,
                // except that we do not have to call g_tree_node_parent.
                NodeRef parent, balance;
                balance = parent = path[--idx];
                Debug.Assert((parent == Null) || (nodes[parent].left == node) || (nodes[parent].right == node));
                bool left_node = (parent != Null) && (node == nodes[parent].left);

                if (!nodes[node].left_child)
                {
                    if (!nodes[node].right_child) // node has no children
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = lastGreaterAncestor;
                            xPositionSuccessor = xPositionLastGreaterAncestor;
                            yPositionSuccessor = yPositionLastGreaterAncestor;
                            /*[Feature(Feature.Dict)]*/
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        if (parent == Null)
                        {
                            root = Null;
                        }
                        else if (left_node)
                        {
                            /*[Feature(Feature.Dict)]*/
                            nodes[parent].left_child = false;
                            /*[Feature(Feature.Dict)]*/
                            nodes[parent].left = nodes[node].left; // back thread
                            //[OR]
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            nodes[parent].left = Null;
                            nodes[parent].balance++;
                        }
                        else
                        {
                            /*[Feature(Feature.Dict)]*/
                            nodes[parent].right_child = false;
                            /*[Feature(Feature.Dict)]*/
                            nodes[parent].right = nodes[node].right; // forward thread
                            //[OR]
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            nodes[parent].right = Null;
                            nodes[parent].balance--;
                        }
                    }
                    else // node has a right child
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        xPositionSuccessor = xPositionNode;
                        /*[Feature(Feature.Range2)]*/
                        yPositionSuccessor = yPositionNode;

                        /*[Feature(Feature.Dict)]*/
                        successor = g_tree_node_next(node);
                        //[OR]
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = nodes[node].right;
                            xPositionSuccessor += nodes[successor].xOffset;
                            yPositionSuccessor += nodes[successor].yOffset;
                            while (nodes[successor].left_child)
                            {
                                successor = nodes[successor].left;
                                xPositionSuccessor += nodes[successor].xOffset;
                                yPositionSuccessor += nodes[successor].yOffset;
                            }
                            /*[Feature(Feature.Dict)]*/
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        /*[Feature(Feature.Dict)]*/
                        nodes[successor].left = nodes[node].left; // back thread

                        NodeRef rightChild = nodes[node].right;
                        nodes[rightChild].xOffset += nodes[node].xOffset;
                        nodes[rightChild].yOffset += nodes[node].yOffset;
                        if (parent == Null)
                        {
                            root = rightChild;
                        }
                        else if (left_node)
                        {
                            nodes[parent].left = rightChild;
                            nodes[parent].balance++;
                        }
                        else
                        {
                            nodes[parent].right = rightChild;
                            nodes[parent].balance--;
                        }
                    }
                }
                else // node has a left child
                {
                    if (!nodes[node].right_child)
                    {
                        /*[Feature(Feature.Dict)]*/
                        {
                            NodeRef predecessor = g_tree_node_previous(node);
                            nodes[predecessor].right = nodes[node].right; // forward thread
                        }

                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        {
                            successor = lastGreaterAncestor;
                            xPositionSuccessor = xPositionLastGreaterAncestor;
                            yPositionSuccessor = yPositionLastGreaterAncestor;
                            /*[Feature(Feature.Dict)]*/
                            Debug.Assert(successor == g_tree_node_next(node));
                        }

                        NodeRef leftChild = nodes[node].left;
                        nodes[leftChild].xOffset += nodes[node].xOffset;
                        nodes[leftChild].yOffset += nodes[node].yOffset;
                        if (parent == Null)
                        {
                            root = leftChild;
                        }
                        else if (left_node)
                        {
                            nodes[parent].left = leftChild;
                            nodes[parent].balance++;
                        }
                        else
                        {
                            nodes[parent].right = leftChild;
                            nodes[parent].balance--;
                        }
                    }
                    else // node has a both children (pant, pant!)
                    {
                        NodeRef predecessor = nodes[node].left;
                        successor = nodes[node].right;
                        NodeRef successorParent = node;
                        int old_idx = ++idx;
                        xPositionSuccessor = xPositionNode + nodes[successor].xOffset;
                        yPositionSuccessor = yPositionNode + nodes[successor].yOffset;

                        // path[idx] == parent
                        // find the immediately next node (and its parent)
                        while (nodes[successor].left_child)
                        {
                            path[++idx] = successorParent = successor;
                            successor = nodes[successor].left;

                            xPositionSuccessor += nodes[successor].xOffset;
                            yPositionSuccessor += nodes[successor].yOffset;
                        }

                        path[old_idx] = successor;
                        balance = path[idx];

                        /* remove 'successor' from the tree */
                        if (successorParent != node)
                        {
                            if (nodes[successor].right_child)
                            {
                                NodeRef successorRightChild = nodes[successor].right;

                                nodes[successorParent].left = successorRightChild;

                                nodes[successorRightChild].xOffset += nodes[successor].xOffset;
                                nodes[successorRightChild].yOffset += nodes[successor].yOffset;
                            }
                            else
                            {
                                /*[Feature(Feature.Dict)]*/
                                nodes[successorParent].left_child = false; // 'left' remains as back thread
                                //[OR]
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                nodes[successorParent].left = Null;
                            }
                            nodes[successorParent].balance++;

                            /*[Feature(Feature.Dict)]*/
                            nodes[successor].right_child = true;
                            nodes[successor].right = nodes[node].right;

                            nodes[nodes[node].right].xOffset += xPositionNode - xPositionSuccessor;
                            nodes[nodes[node].right].yOffset += yPositionNode - yPositionSuccessor;
                        }
                        else
                        {
                            nodes[node].balance--;
                        }

                        // set the predecessor's successor link to point to the right place
                        /*[Feature(Feature.Dict)]*/
                        {
                            while (nodes[predecessor].right_child)
                            {
                                predecessor = nodes[predecessor].right;
                            }
                            nodes[predecessor].right = successor;
                        }

                        /* prepare 'successor' to replace 'node' */
                        NodeRef leftChild = nodes[node].left;
                        /*[Feature(Feature.Dict)]*/
                        nodes[successor].left_child = true;
                        nodes[successor].left = leftChild;
                        nodes[successor].balance = nodes[node].balance;
                        nodes[leftChild].xOffset += xPositionNode - xPositionSuccessor;
                        nodes[leftChild].yOffset += yPositionNode - yPositionSuccessor;

                        if (parent == Null)
                        {
                            root = successor;
                        }
                        else if (left_node)
                        {
                            nodes[parent].left = successor;
                        }
                        else
                        {
                            nodes[parent].right = successor;
                        }

                        nodes[successor].xOffset = xPositionSuccessor - xPositionParent;
                        nodes[successor].yOffset = yPositionSuccessor - yPositionParent;
                    }
                }

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                {
                    /*[Widen]*/
                    int xLength;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yLength;

                    if (successor != Null)
                    {
                        xLength = xPositionSuccessor - xPositionNode;
                        yLength = yPositionSuccessor - yPositionNode;
                    }
                    else
                    {
                        xLength = this.xExtent - xPositionNode;
                        yLength = this.yExtent - yPositionNode;
                    }

                    ShiftRightOfPath(xPositionNode + 1, /*[Feature(Feature.Range2)]*/Side.X, -xLength, /*[Feature(Feature.Range2)]*/-yLength);

                    this.xExtent = unchecked(this.xExtent - xLength);
                    this.yExtent = unchecked(this.yExtent - yLength);
                }

                /* restore balance */
                if (balance != Null)
                {
                    while (true)
                    {
                        NodeRef bparent = path[--idx];
                        Debug.Assert((bparent == Null) || (nodes[bparent].left == balance) || (nodes[bparent].right == balance));
                        left_node = (bparent != Null) && (balance == nodes[bparent].left);

                        if ((nodes[balance].balance < -1) || (nodes[balance].balance > 1))
                        {
                            balance = g_tree_node_balance(balance);
                            if (bparent == Null)
                            {
                                root = balance;
                            }
                            else if (left_node)
                            {
                                nodes[bparent].left = balance;
                            }
                            else
                            {
                                nodes[bparent].right = balance;
                            }
                        }

                        if ((nodes[balance].balance != 0) || (bparent == Null))
                        {
                            break;
                        }

                        if (left_node)
                        {
                            nodes[bparent].balance++;
                        }
                        else
                        {
                            nodes[bparent].balance--;
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
            [Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Widen] int xAdjust,
            [Feature(Feature.Range2)][Widen] int yAdjust)
        {
            unchecked
            {
                this.version = unchecked(this.version + 1);

                if (root != Null)
                {
                    /*[Widen]*/
                    int xPositionCurrent = 0;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yPositionCurrent = 0;
                    NodeRef current = root;
                    while (true)
                    {
                        xPositionCurrent += nodes[current].xOffset;
                        yPositionCurrent += nodes[current].yOffset;

                        int order = position.CompareTo(side == Side.X ? xPositionCurrent : yPositionCurrent);
                        if (order <= 0)
                        {
                            xPositionCurrent += xAdjust;
                            yPositionCurrent += yAdjust;
                            nodes[current].xOffset += xAdjust;
                            nodes[current].yOffset += yAdjust;
                            if (nodes[current].left_child)
                            {
                                nodes[nodes[current].left].xOffset -= xAdjust;
                                nodes[nodes[current].left].yOffset -= yAdjust;
                            }

                            if (order == 0)
                            {
                                break;
                            }
                            if (!nodes[current].left_child)
                            {
                                break;
                            }
                            current = nodes[current].left;
                        }
                        else
                        {
                            if (!nodes[current].right_child)
                            {
                                break;
                            }
                            current = nodes[current].right;
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

        private NodeRef g_tree_node_balance(NodeRef node)
        {
            unchecked
            {
                if (nodes[node].balance < -1)
                {
                    if (nodes[nodes[node].left].balance > 0)
                    {
                        nodes[node].left = g_tree_node_rotate_left(nodes[node].left);
                    }
                    node = g_tree_node_rotate_right(node);
                }
                else if (nodes[node].balance > 1)
                {
                    if (nodes[nodes[node].right].balance < 0)
                    {
                        nodes[node].right = g_tree_node_rotate_right(nodes[node].right);
                    }
                    node = g_tree_node_rotate_left(node);
                }

                return node;
            }
        }

        private NodeRef g_tree_node_rotate_left(NodeRef node)
        {
            unchecked
            {
                NodeRef right = nodes[node].right;

                /*[Widen]*/
                int xOffsetNode = nodes[node].xOffset;
                /*[Widen]*/
                int yOffsetNode = nodes[node].yOffset;
                /*[Widen]*/
                int xOffsetRight = nodes[right].xOffset;
                /*[Widen]*/
                int yOffsetRight = nodes[right].yOffset;
                nodes[node].xOffset = -xOffsetRight;
                nodes[node].yOffset = -yOffsetRight;
                nodes[right].xOffset += xOffsetNode;
                nodes[right].yOffset += yOffsetNode;

                if (nodes[right].left_child)
                {
                    nodes[nodes[right].left].xOffset += xOffsetRight;
                    nodes[nodes[right].left].yOffset += yOffsetRight;

                    nodes[node].right = nodes[right].left;
                }
                else
                {
                    /*[Feature(Feature.Dict)]*/
                    nodes[node].right_child = false;
                    //[OR]
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    nodes[node].right = Null;
                    /*[Feature(Feature.Dict)]*/
                    nodes[right].left_child = true;
                }
                nodes[right].left = node;

                int a_bal = nodes[node].balance;
                int b_bal = nodes[right].balance;

                if (b_bal <= 0)
                {
                    if (a_bal >= 1)
                    {
                        nodes[right].balance = (sbyte)(b_bal - 1);
                    }
                    else
                    {
                        nodes[right].balance = (sbyte)(a_bal + b_bal - 2);
                    }
                    nodes[node].balance = (sbyte)(a_bal - 1);
                }
                else
                {
                    if (a_bal <= b_bal)
                    {
                        nodes[right].balance = (sbyte)(a_bal - 2);
                    }
                    else
                    {
                        nodes[right].balance = (sbyte)(b_bal - 1);
                    }
                    nodes[node].balance = (sbyte)(a_bal - b_bal - 1);
                }

                return right;
            }
        }

        private NodeRef g_tree_node_rotate_right(NodeRef node)
        {
            unchecked
            {
                NodeRef left = nodes[node].left;

                /*[Widen]*/
                int xOffsetNode = nodes[node].xOffset;
                /*[Widen]*/
                int yOffsetNode = nodes[node].yOffset;
                /*[Widen]*/
                int xOffsetLeft = nodes[left].xOffset;
                /*[Widen]*/
                int yOffsetLeft = nodes[left].yOffset;
                nodes[node].xOffset = -xOffsetLeft;
                nodes[node].yOffset = -yOffsetLeft;
                nodes[left].xOffset += xOffsetNode;
                nodes[left].yOffset += yOffsetNode;

                if (nodes[left].right_child)
                {
                    nodes[nodes[left].right].xOffset += xOffsetLeft;
                    nodes[nodes[left].right].yOffset += yOffsetLeft;

                    nodes[node].left = nodes[left].right;
                }
                else
                {
                    /*[Feature(Feature.Dict)]*/
                    nodes[node].left_child = false;
                    //[OR]
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    nodes[node].left = Null;
                    /*[Feature(Feature.Dict)]*/
                    nodes[left].right_child = true;
                }
                nodes[left].right = node;

                int a_bal = nodes[node].balance;
                int b_bal = nodes[left].balance;

                if (b_bal <= 0)
                {
                    if (b_bal > a_bal)
                    {
                        nodes[left].balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        nodes[left].balance = (sbyte)(a_bal + 2);
                    }
                    nodes[node].balance = (sbyte)(a_bal - b_bal + 1);
                }
                else
                {
                    if (a_bal <= -1)
                    {
                        nodes[left].balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        nodes[left].balance = (sbyte)(a_bal + b_bal + 2);
                    }
                    nodes[node].balance = (sbyte)(a_bal + 1);
                }

                return left;
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private NodeRef g_tree_find_node(KeyType key) // TODO: consolidate with Find()
        {
            NodeRef node = root;
            if (node == Null)
            {
                return Null;
            }

            while (true)
            {
                int cmp = comparer.Compare(key, nodes[node].key);
                if (cmp == 0)
                {
                    return node;
                }
                else if (cmp < 0)
                {
                    if (!nodes[node].left_child)
                    {
                        return Null;
                    }

                    node = nodes[node].left;
                }
                else
                {
                    if (!nodes[node].right_child)
                    {
                        return Null;
                    }

                    node = nodes[node].right;
                }
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool Find(
            KeyType key,
            out NodeRef match,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int xPositionMatch,
            [Feature(Feature.Range2)][Widen] out int yPositionMatch,
            [Feature(Feature.RankMulti)][Widen] out int xLengthMatch,
            [Feature(Feature.Range2)][Widen] out int yLengthMatch)
        {
            unchecked
            {
                match = Null;
                xPositionMatch = 0;
                yPositionMatch = 0;
                xLengthMatch = 0;
                yLengthMatch = 0;

                NodeRef successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionSuccessor = 0;
                NodeRef lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionLastGreaterAncestor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionLastGreaterAncestor = 0;
                if (root != Null)
                {
                    NodeRef current = root;
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int xPositionCurrent = 0;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yPositionCurrent = 0;
                    while (true)
                    {
                        xPositionCurrent += nodes[current].xOffset;
                        yPositionCurrent += nodes[current].yOffset;

                        int order = (match != Null) ? -1 : comparer.Compare(key, nodes[current].key);

                        if (order == 0)
                        {
                            xPositionMatch = xPositionCurrent;
                            yPositionMatch = yPositionCurrent;
                            match = current;

                            // successor not needed for dict mode only
                            /*[Feature(Feature.Dict)]*/
                            break;
                        }

                        successor = current;
                        xPositionSuccessor = xPositionCurrent;
                        yPositionSuccessor = yPositionCurrent;

                        if (order < 0)
                        {
                            if (match == Null)
                            {
                                lastGreaterAncestor = current;
                                xPositionLastGreaterAncestor = xPositionCurrent;
                                yPositionLastGreaterAncestor = yPositionCurrent;
                            }
                            if (!nodes[current].left_child)
                            {
                                break;
                            }
                            current = nodes[current].left;
                        }
                        else
                        {
                            if (!nodes[current].right_child)
                            {
                                break;
                            }
                            current = nodes[current].right; // continue the search in right sub tree after we find a match
                        }
                    }
                }

                if (match != Null)
                {
                    Debug.Assert(successor != Null);
                    if (successor != match)
                    {
                        xLengthMatch = xPositionSuccessor - xPositionMatch;
                        yLengthMatch = yPositionSuccessor - yPositionMatch;
                    }
                    else if (lastGreaterAncestor != Null)
                    {
                        xLengthMatch = xPositionLastGreaterAncestor - xPositionMatch;
                        yLengthMatch = yPositionLastGreaterAncestor - yPositionMatch;
                    }
                    else
                    {
                        xLengthMatch = this.xExtent - xPositionMatch;
                        yLengthMatch = this.yExtent - yPositionMatch;
                    }
                }

                return match != Null;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(
            [Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            out NodeRef lastLessEqual,
            [Widen] out int xPositionLastLessEqual,
            [Feature(Feature.Range2)][Widen] out int yPositionLastLessEqual,
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int xLength,
            [Feature(Feature.Range2)][Widen] out int yLength)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;
                /*[Feature(Feature.Range2)]*/
                yPositionLastLessEqual = 0;
                xLength = 0;
                yLength = 0;

                NodeRef successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionSuccessor = 0;
                if (root != Null)
                {
                    NodeRef current = root;
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int xPositionCurrent = 0;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yPositionCurrent = 0;
                    while (true)
                    {
                        xPositionCurrent += nodes[current].xOffset;
                        yPositionCurrent += nodes[current].yOffset;

                        if (position < (side == Side.X ? xPositionCurrent : yPositionCurrent))
                        {
                            successor = current;
                            xPositionSuccessor = xPositionCurrent;
                            yPositionSuccessor = yPositionCurrent;

                            if (!nodes[current].left_child)
                            {
                                break;
                            }
                            current = nodes[current].left;
                        }
                        else
                        {
                            lastLessEqual = current;
                            xPositionLastLessEqual = xPositionCurrent;
                            yPositionLastLessEqual = yPositionCurrent;

                            if (!nodes[current].right_child)
                            {
                                break;
                            }
                            current = nodes[current].right; // try to find successor
                        }
                    }
                }
                if ((successor != Null) && (successor != lastLessEqual))
                {
                    xLength = xPositionSuccessor - xPositionLastLessEqual;
                    yLength = yPositionSuccessor - yPositionLastLessEqual;
                }
                else
                {
                    xLength = this.xExtent - xPositionLastLessEqual;
                    yLength = this.yExtent - yPositionLastLessEqual;
                }

                return (position >= 0) && (position < (side == Side.X ? this.xExtent : this.yExtent));
            }
        }


        //
        // Non-invasive tree inspection support
        //

        // Helpers

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ValidateRanges([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            if (root != Null)
            {
                Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int offset = 0;
                /*[Widen]*/
                int leftEdge = 0;
                /*[Widen]*/
                int rightEdge = side == Side.X ? this.xExtent : this.yExtent;

                NodeRef node = root;
                while (node != Null)
                {
                    offset += side == Side.X ? nodes[node].xOffset : nodes[node].yOffset;
                    stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                    rightEdge = offset;
                    node = nodes[node].leftOrNull;
                }
                while (stack.Count != 0)
                {
                    STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    offset = t.Item2;
                    leftEdge = t.Item3;
                    rightEdge = t.Item4;

                    Check.Assert((offset >= leftEdge) && (offset < rightEdge), "range containment invariant");

                    leftEdge = offset + 1;
                    node = nodes[node].rightOrNull;
                    while (node != Null)
                    {
                        offset += side == Side.X ? nodes[node].xOffset : nodes[node].yOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = nodes[node].leftOrNull;
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
            NodeRef n = (NodeRef)node;
            return nodes[n].left_child ? (object)nodes[n].left : null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetRightChild() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].right_child ? (object)nodes[n].right : null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetKey() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetKey(object node)
        {
            NodeRef n = (NodeRef)node;
            object key = null;
            key = nodes[n].key;
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
            NodeRef n = (NodeRef)node;
            object value = null;
            value = nodes[n].value;
            return value;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.GetMetadata() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].balance;
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
                Dictionary<NodeRef, bool> visited = new Dictionary<NodeRef, bool>();
                Queue<NodeRef> worklist = new Queue<NodeRef>();
                worklist.Enqueue(root);
                while (worklist.Count != 0)
                {
                    NodeRef node = worklist.Dequeue();

                    Check.Assert(!visited.ContainsKey(node), "cycle");
                    visited.Add(node, false);

                    if (nodes[node].left_child)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        Check.Assert(comparer.Compare(nodes[nodes[node].left].key, nodes[node].key) < 0, "ordering invariant");
                        worklist.Enqueue(nodes[node].left);
                    }
                    if (nodes[node].right_child)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        Check.Assert(comparer.Compare(nodes[node].key, nodes[nodes[node].right].key) < 0, "ordering invariant");
                        worklist.Enqueue(nodes[node].right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.X);
            /*[Feature(Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.Y);

            g_tree_node_check(root);
            ValidateDepthInvariant();
        }

        private void ValidateDepthInvariant()
        {
            double max = TheoreticalMaxDepth(count); // includes epsilon
            int depth = root != Null ? ActualDepth(root) : 0;
            Check.Assert(depth <= max, "max depth invariant");
        }

        private int ActualDepth(NodeRef node)
        {
            if (node != Null)
            {
                int ld = ActualDepth(nodes[node].leftOrNull);
                int rd = ActualDepth(nodes[node].rightOrNull);
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

        private void g_tree_node_check(NodeRef node)
        {
            if (node != Null)
            {
                /*[Feature(Feature.Dict)]*/
                if (nodes[node].left_child)
                {
                    NodeRef tmp = g_tree_node_previous(node);
                    Check.Assert(nodes[tmp].right == node, "predecessor invariant");
                }
                /*[Feature(Feature.Dict)]*/
                if (nodes[node].right_child)
                {
                    NodeRef tmp = g_tree_node_next(node);
                    Check.Assert(nodes[tmp].left == node, "successor invariant");
                }

                int left_height = g_tree_node_height(nodes[node].leftOrNull);
                int right_height = g_tree_node_height(nodes[node].rightOrNull);

                int balance = right_height - left_height;
                Check.Assert(balance == nodes[node].balance, "balance invariant");

                g_tree_node_check(nodes[node].leftOrNull);
                g_tree_node_check(nodes[node].rightOrNull);
            }
        }

        private int g_tree_node_height(NodeRef node)
        {
            if (node != Null)
            {
                int left_height = 0;
                int right_height = 0;

                if (nodes[node].left_child)
                {
                    left_height = g_tree_node_height(nodes[node].left);
                }

                if (nodes[node].right_child)
                {
                    right_height = g_tree_node_height(nodes[node].right);
                }

                return Math.Max(left_height, right_height) + 1;
            }

            return 0;
        }

        // INonInvasiveRange2MapInspection

        /// <summary>
        /// INonInvasiveRange2MapInspection.GetRanges() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        Range2MapEntry[] /*[Widen]*/INonInvasiveRange2MapInspection.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntry[] ranges = new /*[Widen]*/Range2MapEntry[Count];
            int i = 0;

            if (root != Null)
            {
                Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;
                /*[Widen]*/
                int yOffset = 0;

                NodeRef node = root;
                while (node != Null)
                {
                    xOffset += nodes[node].xOffset;
                    yOffset += nodes[node].yOffset;
                    stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                    node = nodes[node].leftOrNull;
                }
                while (stack.Count != 0)
                {
                    STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;
                    yOffset = t.Item3;

                    object value = null;
                    value = nodes[node].value;

                    ranges[i++] = new /*[Widen]*/Range2MapEntry(new /*[Widen]*/Range(xOffset, 0), new /*[Widen]*/Range(yOffset, 0), value);

                    node = nodes[node].rightOrNull;
                    while (node != Null)
                    {
                        xOffset += nodes[node].xOffset;
                        yOffset += nodes[node].yOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                        node = nodes[node].leftOrNull;
                    }
                }
                Check.Assert(i == ranges.Length, "count invariant");

                for (i = 1; i < ranges.Length; i++)
                {
                    Check.Assert(ranges[i - 1].x.start < ranges[i].x.start, "range sequence invariant (X)");
                    /*[Feature(Feature.Range2)]*/
                    Check.Assert(ranges[i - 1].y.start < ranges[i].y.start, "range sequence invariant (Y)");
                    ranges[i - 1].x.length = ranges[i].x.start - ranges[i - 1].x.start;
                    /*[Feature(Feature.Range2)]*/
                    ranges[i - 1].y.length = ranges[i].y.start - ranges[i - 1].y.start;
                }

                ranges[i - 1].x.length = this.xExtent - ranges[i - 1].x.start;
                /*[Feature(Feature.Range2)]*/
                ranges[i - 1].y.length = this.yExtent - ranges[i - 1].y.start;
            }

            return ranges;
        }

        /// <summary>
        /// INonInvasiveRange2MapInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Range, Feature.Range2)]
        void /*[Widen]*/INonInvasiveRange2MapInspection.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }

        // INonInvasiveMultiRankMapInspection

        /// <summary>
        /// INonInvasiveMultiRankMapInspection.GetRanks() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        MultiRankMapEntry[] /*[Widen]*/INonInvasiveMultiRankMapInspection.GetRanks()
        {
            /*[Widen]*/
            MultiRankMapEntry[] ranks = new /*[Widen]*/MultiRankMapEntry[Count];
            int i = 0;

            if (root != Null)
            {
                Stack<STuple<NodeRef, /*[Widen]*/int>> stack = new Stack<STuple<NodeRef, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;

                NodeRef node = root;
                while (node != Null)
                {
                    xOffset += nodes[node].xOffset;
                    stack.Push(new STuple<NodeRef, /*[Widen]*/int>(node, xOffset));
                    node = nodes[node].leftOrNull;
                }
                while (stack.Count != 0)
                {
                    STuple<NodeRef, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;

                    object key = null;
                    key = nodes[node].key;
                    object value = null;
                    value = nodes[node].value;

                    ranks[i++] = new /*[Widen]*/MultiRankMapEntry(key, new /*[Widen]*/Range(xOffset, 0), value);

                    node = nodes[node].rightOrNull;
                    while (node != Null)
                    {
                        xOffset += nodes[node].xOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int>(node, xOffset));
                        node = nodes[node].leftOrNull;
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
        void /*[Widen]*/INonInvasiveMultiRankMapInspection.Validate()
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
        public IEnumerator<AVLTreeEntry<KeyType, ValueType>> GetEnumerator()
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

        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetEnumerable(bool forward)
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
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetRobustEnumerable(bool forward)
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
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetFastEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        //
        // IKeyedTreeEnumerable
        //

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetEnumerable(KeyType startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/); // default
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetEnumerable(KeyType startAt, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward); // default
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetFastEnumerable(KeyType startAt)
        {
            return new FastEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetFastEnumerable(KeyType startAt, bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, forward);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetRobustEnumerable(KeyType startAt, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward);
        }

        //
        // IIndexedTreeEnumerable/IIndexed2TreeEnumerable
        //

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetEnumerable([Widen] int startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/); // default
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetEnumerable([Widen] int startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward); // default
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetFastEnumerable([Widen] int startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new FastEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetFastEnumerable([Widen] int startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetRobustEnumerable([Widen] int startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<AVLTreeEntry<KeyType, ValueType>> GetRobustEnumerable([Widen] int startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<AVLTreeEntry<KeyType, ValueType>>
        {
            private readonly AVLTree<KeyType, ValueType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            // Construction

            public RobustEnumerableSurrogate(AVLTree<KeyType, ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerableSurrogate(AVLTree<KeyType, ValueType> tree, KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerableSurrogate(AVLTree<KeyType, ValueType> tree, [Widen] int startStart, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;
            }

            // IEnumerable

            public IEnumerator<AVLTreeEntry<KeyType, ValueType>> GetEnumerator()
            {
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                if (startKeyed)
                {
                    return new RobustEnumerator(tree, startKey, forward);
                }

                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (startIndexed)
                {
                    return new RobustEnumerator(tree, startStart, /*[Feature(Feature.Range2)]*/side, forward);
                }

                return new RobustEnumerator(tree, forward);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public struct FastEnumerableSurrogate : IEnumerable<AVLTreeEntry<KeyType, ValueType>>
        {
            private readonly AVLTree<KeyType, ValueType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            // Construction

            public FastEnumerableSurrogate(AVLTree<KeyType, ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerableSurrogate(AVLTree<KeyType, ValueType> tree, KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerableSurrogate(AVLTree<KeyType, ValueType> tree, [Widen] int startStart, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = false;
                this.startKey = default(KeyType);

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;
            }

            // IEnumerable

            public IEnumerator<AVLTreeEntry<KeyType, ValueType>> GetEnumerator()
            {
                /*[Feature(Feature.Dict)]*/
                if (startKeyed)
                {
                    return new FastEnumeratorThreaded(tree, startKey, forward);
                }
                //[OR]
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                if (startKeyed)
                {
                    return new FastEnumerator(tree, startKey, forward);
                }
                //[OR]
                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (startIndexed)
                {
                    return new FastEnumerator(tree, startStart, /*[Feature(Feature.Range2)]*/side, forward);
                }

                /*[Feature(Feature.Dict)]*/
                return new FastEnumeratorThreaded(tree, forward);
                //[OR]
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
            IEnumerator<AVLTreeEntry<KeyType, ValueType>>,
            /*[Payload(Payload.Value)]*/ISetValue<ValueType>
        {
            private readonly AVLTree<KeyType, ValueType> tree;
            private readonly bool forward;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly bool startKeyed;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;
            //
            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            private bool started;
            private bool valid;
            private uint enumeratorVersion;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private int currentStart;
            //
            // saving the currentXStart with does not work well for range collections because it may shift, so making updates
            // is not permitted in range trees
            [Feature(Feature.Range, Feature.Range2)]
            private uint treeVersion;

            public RobustEnumerator(AVLTree<KeyType, ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public RobustEnumerator(AVLTree<KeyType, ValueType> tree, KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerator(AVLTree<KeyType, ValueType> tree, [Widen] int startStart, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;

                Reset();
            }

            public AVLTreeEntry<KeyType, ValueType> Current
            {
                get
                {
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (treeVersion != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

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

                            /*[Feature(Feature.Dict)]*/
                            value = tree.GetValue(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey);
                            //[OR]
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            tree.Get(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey,
                                /*[Payload(Payload.None)]*/out key,
                                /*[Payload(Payload.Value)]*/out value,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/out rank,
                                /*[Feature(Feature.RankMulti)]*/out count);

                            /*[Feature(Feature.Rank)]*/
                            Debug.Assert(count == 1);

                            return new AVLTreeEntry<KeyType, ValueType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Payload(Payload.Value)]*/value,
                                /*[Payload(Payload.Value)]*/this,
                                /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/rank,
                                /*[Feature(Feature.RankMulti)]*/count,
                                /*[Feature(Feature.Range2)]*/0,
                                /*[Feature(Feature.Range2)]*/0);
                        }

                        //[OR]

                        /*[Feature(Feature.Range, Feature.Range2)]*/
                        {
                            ValueType value = default(ValueType);
                            /*[Widen]*/
                            int xStart = 0, xLength = 0;
                            /*[Widen]*/
                            int yStart = 0, yLength = 0;

                            /*[Feature(Feature.Range)]*/
                            xStart = currentStart;

                            /*[Widen]*/
                            int otherStart;
                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/currentStart,
                                /*[Feature(Feature.Range2)]*/side,
                                /*[Feature(Feature.Range2)]*/out otherStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength,
                                /*[Feature(Feature.Range2)]*/out yLength,
                                /*[Payload(Payload.Value)]*/out value);
                            /*[Feature(Feature.Range2)]*/
                            if (side == Side.X)
                            {
                                xStart = currentStart;
                                yStart = otherStart;
                            }
                            else
                            {
                                xStart = otherStart;
                                yStart = currentStart;
                            }

                            return new AVLTreeEntry<KeyType, ValueType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                                /*[Payload(Payload.Value)]*/value,
                                /*[Payload(Payload.Value)]*/this,
                                /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength,
                                /*[Feature(Feature.Range2)]*/yStart,
                                /*[Feature(Feature.Range2)]*/yLength);
                        }
                    }
                    return new AVLTreeEntry<KeyType, ValueType>();
                }
            }

            object IEnumerator.Current { get { return this.Current; } }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (treeVersion != tree.version)
                {
                    throw new InvalidOperationException();
                }

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

                    //[OR]

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    {
                        if (!startIndexed)
                        {
                            if (forward)
                            {
                                currentStart = 0;
                                valid = tree.xExtent != 0;
                            }
                            else
                            {
                                /*[Feature(Feature.Range)]*/
                                valid = tree.NearestLess(tree.xExtent, /*[Feature(Feature.Range2)]*/Side.X, out currentStart);
                                /*[Feature(Feature.Range2)]*/
                                valid = tree.NearestLess(side == Side.X ? tree.xExtent : tree.yExtent, side, out currentStart);
                            }
                        }
                        else
                        {
                            if (forward)
                            {
                                valid = tree.NearestGreaterOrEqual(startStart, /*[Feature(Feature.Range2)]*/side, out currentStart);
                            }
                            else
                            {
                                valid = tree.NearestLessOrEqual(startStart, /*[Feature(Feature.Range2)]*/side, out currentStart);
                            }
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

                    //[OR]

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (forward)
                    {
                        valid = tree.NearestGreater(currentStart, /*[Feature(Feature.Range2)]*/side, out currentStart);
                    }
                    else
                    {
                        valid = tree.NearestLess(currentStart, /*[Feature(Feature.Range2)]*/side, out currentStart);
                    }
                }

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;

                /*[Feature(Feature.Range, Feature.Range2)]*/
                treeVersion = tree.version;
                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);
            }

            [Payload(Payload.Value)]
            public void SetValue(ValueType value, uint requiredEnumeratorVersion)
            {
                if (this.enumeratorVersion != requiredEnumeratorVersion)
                {
                    throw new InvalidOperationException();
                }
                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (this.treeVersion != tree.version)
                {
                    throw new InvalidOperationException();
                }

                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                tree.SetValue(currentKey, value);
                //[OR]
                /*[Feature(Feature.Range, Feature.Range2)]*/
                tree.SetValue(currentStart, /*[Feature(Feature.Range2)]*/side, value);
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        public class FastEnumerator :
            IEnumerator<AVLTreeEntry<KeyType, ValueType>>,
            /*[Payload(Payload.Value)]*/ISetValue<ValueType>
        {
            private readonly AVLTree<KeyType, ValueType> tree;
            private readonly bool forward;

            private readonly bool startKeyedOrIndexed;
            //
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private readonly KeyType startKey;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            private uint treeVersion;
            private uint enumeratorVersion;

            private NodeRef currentNode;
            private NodeRef leadingNode;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int previousXStart;
            [Feature(Feature.Range2)]
            [Widen]
            private int currentYStart, nextYStart, previousYStart;

            private STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>[] stack;
            private int stackIndex;

            public FastEnumerator(AVLTree<KeyType, ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public FastEnumerator(AVLTree<KeyType, ValueType> tree, KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startKey = startKey;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerator(AVLTree<KeyType, ValueType> tree, [Widen] int startStart, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startStart = startStart;
                this.side = side;

                Reset();
            }

            public AVLTreeEntry<KeyType, ValueType> Current
            {
                get
                {
                    if (currentNode != Null)
                    {
                        /*[Feature(Feature.Rank)]*/
                        Debug.Assert((forward && (nextXStart - currentXStart == 1))
                            || (!forward && ((nextXStart - currentXStart == -1) || ((currentXStart == 0) && (nextXStart == 0)))));


                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        int currentXLength = 0;
                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        int currentYLength = 0;

                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        if (forward)
                        {
                            currentXLength = nextXStart - currentXStart;
                            currentYLength = nextYStart - currentYStart;
                        }
                        else
                        {
                            currentXLength = previousXStart - currentXStart;
                            currentYLength = previousYStart - currentYStart;
                        }

                        return new AVLTreeEntry<KeyType, ValueType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                            tree.nodes[currentNode].key,
                            /*[Payload(Payload.Value)]*/tree.nodes[currentNode].value,
                            /*[Payload(Payload.Value)]*/this,
                            /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXLength,
                            /*[Feature(Feature.Range2)]*/currentYStart,
                            /*[Feature(Feature.Range2)]*/currentYLength);
                    }
                    return new AVLTreeEntry<KeyType, ValueType>();
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
                        stack = new STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>[
                            stackSize];
                    }
                    stackIndex = 0;

                    currentNode = Null;
                    leadingNode = Null;

                    this.treeVersion = tree.version;

                    // push search path to starting item

                    NodeRef node = tree.root;
                    /*[Widen]*/
                    int xPosition = 0;
                    /*[Widen]*/
                    int yPosition = 0;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool foundMatch = false;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool lastGreaterAncestorValid = false;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int xPositionLastGreaterAncestor = 0;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yPositionLastGreaterAncestor = 0;

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    bool successorValid = false;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int xPositionSuccessor = 0;
                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int yPositionSuccessor = 0;
                    while (node != Null)
                    {
                        xPosition += tree.nodes[node].xOffset;
                        yPosition += tree.nodes[node].yOffset;

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
                                c = tree.comparer.Compare(startKey, tree.nodes[node].key);
                                //[OR]
                                /*[Feature(Feature.Range)]*/
                                c = startStart.CompareTo(xPosition);
                                //[OR]
                                /*[Feature(Feature.Range2)]*/
                                c = startStart.CompareTo(side == Side.X ? xPosition : yPosition);
                            }
                        }

                        if (!foundMatch1 && (forward && (c <= 0)) || (!forward && (c >= 0)))
                        {
                            stack[stackIndex++] = new STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>(
                                node,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                                /*[Feature(Feature.Range2)]*/yPosition);
                        }

                        if (c == 0)
                        {
                            foundMatch = true;

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
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            if (!foundMatch)
                            {
                                lastGreaterAncestorValid = true;
                                xPositionLastGreaterAncestor = xPosition;
                                yPositionLastGreaterAncestor = yPosition;
                            }

                            node = tree.nodes[node].leftOrNull;
                        }
                        else
                        {
                            Debug.Assert(c >= 0);
                            node = tree.nodes[node].rightOrNull;
                        }
                    }

                    /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    if (!forward)
                    {
                        // get position of successor for length of initial item
                        if (successorValid)
                        {
                            nextXStart = xPositionSuccessor;
                            nextYStart = yPositionSuccessor;
                        }
                        else if (lastGreaterAncestorValid)
                        {
                            nextXStart = xPositionLastGreaterAncestor;
                            nextYStart = yPositionLastGreaterAncestor;
                        }
                        else
                        {
                            nextXStart = tree.xExtent;
                            nextYStart = tree.yExtent;
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
                    previousYStart = currentYStart;
                    currentNode = leadingNode;
                    currentXStart = nextXStart;
                    currentYStart = nextYStart;

                    leadingNode = Null;

                    if (stackIndex == 0)
                    {
                        if (forward)
                        {
                            nextXStart = tree.xExtent;
                            nextYStart = tree.yExtent;
                        }
                        else
                        {
                            nextXStart = 0;
                            nextYStart = 0;
                        }
                        return;
                    }

                    STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int> cursor
                        = stack[--stackIndex];

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;
                    nextYStart = cursor.Item3;

                    NodeRef node = forward
                        ? (tree.nodes[leadingNode].rightOrNull)
                        : (tree.nodes[leadingNode].leftOrNull);
                    /*[Widen]*/
                    int xPosition = nextXStart;
                    /*[Widen]*/
                    int yPosition = nextYStart;
                    while (node != Null)
                    {
                        xPosition += tree.nodes[node].xOffset;
                        yPosition += tree.nodes[node].yOffset;

                        stack[stackIndex++] = new STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                            /*[Feature(Feature.Range2)]*/yPosition);
                        node = forward
                            ? (tree.nodes[node].leftOrNull)
                            : (tree.nodes[node].rightOrNull);
                    }
                }
            }

            [Payload(Payload.Value)]
            public void SetValue(ValueType value, uint requiredEnumeratorVersion)
            {
                if ((this.enumeratorVersion != requiredEnumeratorVersion) || (this.treeVersion != tree.version))
                {
                    throw new InvalidOperationException();
                }

                tree.nodes[currentNode].value = value;
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        [Feature(Feature.Dict)]
        public class FastEnumeratorThreaded :
            IEnumerator<AVLTreeEntry<KeyType, ValueType>>,
            /*[Payload(Payload.Value)]*/ISetValue<ValueType>
        {
            private readonly AVLTree<KeyType, ValueType> tree;
            private readonly bool forward;

            private readonly bool startKeyed;
            //
            private readonly KeyType startKey;

            private uint treeVersion;
            private uint enumeratorVersion;

            private NodeRef currentNode;

            private bool started;

            public FastEnumeratorThreaded(AVLTree<KeyType, ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            public FastEnumeratorThreaded(AVLTree<KeyType, ValueType> tree, KeyType startKey, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyed = true;
                this.startKey = startKey;

                Reset();
            }

            public AVLTreeEntry<KeyType, ValueType> Current
            {
                get
                {
                    if (currentNode != Null)
                    {
                        return new AVLTreeEntry<KeyType, ValueType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                            tree.nodes[currentNode].key,
                            /*[Payload(Payload.Value)]*/tree.nodes[currentNode].value,
                            /*[Payload(Payload.Value)]*/this,
                            /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                            /*[Feature(Feature.Range2)]*/0,
                            /*[Feature(Feature.Range2)]*/0);
                    }
                    return new AVLTreeEntry<KeyType, ValueType>();
                }
            }

            object IEnumerator.Current { get { return this.Current; } }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.treeVersion != tree.version)
                {
                    throw new InvalidOperationException();
                }

                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);

                if (!started)
                {
                    started = true;

                    if (!startKeyed)
                    {
                        if (forward)
                        {
                            currentNode = tree.g_tree_first_node();
                        }
                        else
                        {
                            currentNode = tree.g_tree_last_node();
                        }
                    }
                    else
                    {
                        if (forward)
                        {
                            KeyType nearestKey;
                            /*[Widen]*/
                            int nearestStart;
                            tree.NearestGreater(
                                out currentNode,
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/startKey,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                                /*[Feature(Feature.Range2)]*/(Side)0,
                                /*[Feature(Feature.RankMulti)]*/(CompareKeyMode)0,
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                                true/*orEqual*/);
                        }
                        else
                        {
                            KeyType nearestKey;
                            /*[Widen]*/
                            int nearestStart;
                            tree.NearestLess(
                                out currentNode,
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/startKey,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                                /*[Feature(Feature.Range2)]*/(Side)0,
                                /*[Feature(Feature.RankMulti)]*/(CompareKeyMode)0,
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                                true/*orEqual*/);
                        }
                        started = true;
                    }
                }
                else
                {
                    if (currentNode != Null)
                    {
                        if (forward)
                        {
                            currentNode = tree.g_tree_node_next(currentNode);
                        }
                        else
                        {
                            currentNode = tree.g_tree_node_previous(currentNode);
                        }
                    }
                }

                return currentNode != Null;
            }

            public void Reset()
            {
                this.treeVersion = tree.version;

                this.currentNode = Null;
                this.started = false;
            }

            [Payload(Payload.Value)]
            public void SetValue(ValueType value, uint requiredEnumeratorVersion)
            {
                if ((this.enumeratorVersion != requiredEnumeratorVersion) || (this.treeVersion != tree.version))
                {
                    throw new InvalidOperationException();
                }

                tree.nodes[currentNode].value = value;
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new AVLTree<KeyType, ValueType>(this);
        }
    }
}
