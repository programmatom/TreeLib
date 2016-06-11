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
// Implementation of top-down splay tree written by Daniel Sleator <sleator@cs.cmu.edu>.
// Taken from http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c
//
// An overview of splay trees can be found here: https://en.wikipedia.org/wiki/Splay_tree
//

namespace TreeLib
{
    // Placeholder, used temporarily to facilitate code transforms more easily, then references
    // renamed to shared enumeration entry.
    public struct SplayTreeEntry<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType>
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

        public SplayTreeEntry(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Payload(Payload.Value)] ValueType value,
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
        }
    }

    /// <summary>
    /// Implements a map, list or range collection using a splay tree. 
    /// </summary>
    public class SplayTree<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType> :

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

        IEnumerable<SplayTreeEntry<KeyType, ValueType>>,
        IEnumerable,

        ICloneable

        where KeyType : IComparable<KeyType>
    {
        //
        // Object form data structure
        //

        [Storage(Storage.Object)]
        private sealed class Node
        {
            public NodeRef left;
            public NodeRef right;

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

            public static Node CreateNil()
            {
                Node nil = new Node();
                nil.left = new NodeRef(nil);
                nil.right = new NodeRef(nil);
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
                    Debug.Assert((new NodeRef(this) == left) == (new NodeRef(this) == right));
                    return new NodeRef(this) == left;
                }
            }
        }

        [ArrayIndexing]
        [Storage(Storage.Object)]
        private nodesStruct nodes;

        [ArrayIndexing]
        [Storage(Storage.Object)]
        private struct nodesStruct
        {
            public Node this[Node node] { get { return node; } }
        }

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

            public override bool Equals(object obj)
            {
                return node.Equals((NodeRef)obj);
            }

            public override int GetHashCode()
            {
                return node.GetHashCode();
            }

            public override string ToString()
            {
                return node != null ? node.ToString() : "null";
            }
        }

        // TODO: ensure fields of the Nil object are not written to, then make it a shared static.
        // (Offsets and left/right pointers never change, but there is a chance there are no-op writes to them during the
        // processing. If so, it can't be shared since it would incur a large penalty in concurrent scenarios.)
        [Storage(Storage.Object)]
        private readonly NodeRef Nil = new NodeRef(Node.CreateNil());
        [Storage(Storage.Object)]
        private readonly NodeRef N = new NodeRef(new Node());

        //
        // Array form data structure
        //

        //[Storage(Storage.Array)]
        //[StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        //private struct Node2
        //{
        //    [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        //    public KeyType key;
        //    [Payload(Payload.Value)]
        //    public ValueType value;
        //}

        [Storage(Storage.Array)]
        [StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        private struct Node
        {
            public NodeRef left;
            public NodeRef right;

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

            //#if DEBUG
            //            public bool scratch_debug;
            //#endif
            //            public override string ToString()
            //            {
            //#if DEBUG
            //                if (scratch_debug)
            //                {
            //                    return "Scratch";
            //                }
            //#endif

            //                string keyText = null;
            //                /*[Storage(Storage.Object)]*/
            //                try
            //                {
            //                    keyText = key.ToString();
            //                }
            //                catch (NullReferenceException)
            //                {
            //                }

            //                string valueText = null;
            //                /*[Storage(Storage.Object)]*/
            //                try
            //                {
            //                    valueText = value.ToString();
            //                }
            //                catch (NullReferenceException)
            //                {
            //                }

            //                string leftText = left == Nil ? "Nil" : left.ToString();

            //                string rightText = right == Nil ? "Nil" : right.ToString();

            //                return String.Format("[{0}]*{2}={3}*[{1}])", leftText, rightText, keyText, valueText);
            //            }
        }

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

            public override bool Equals(object obj)
            {
                return node == (uint)obj;
            }

            public override int GetHashCode()
            {
                return node.GetHashCode();
            }

            public override string ToString()
            {
                return node.ToString();
            }
        }

        [Storage(Storage.Array)]
        private const uint ReservedElements = 2;
        [Storage(Storage.Array)]
        private readonly static NodeRef _Nil = new NodeRef(0);
        [Storage(Storage.Array)]
        private readonly static NodeRef N = new NodeRef(1);

        [Storage(Storage.Array)]
        private NodeRef Nil { get { return SplayTree<KeyType, ValueType>._Nil; } } // allow tree.Nil or this.Nil in all cases

        [Storage(Storage.Array)]
        private Node[] nodes;
        //[Storage(Storage.Array)]
        //private Node2[] nodes2;

        //
        // State for both array & object form
        //

        private NodeRef root;
        [Count]
        private ulong count;

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
        public SplayTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer, uint capacity, AllocationMode allocationMode)
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
        public SplayTree(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public SplayTree()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public SplayTree(SplayTree<KeyType, ValueType> original)
        {
            this.comparer = original.comparer;

            this.count = original.count;
            this.xExtent = original.xExtent;
            this.yExtent = original.yExtent;

            this.allocationMode = original.allocationMode;
            this.freelist = this.Nil;
            {
                NodeRef nodeOriginal = original.freelist;
                while (nodeOriginal != original.Nil)
                {
                    nodeOriginal = nodes[nodeOriginal].left;
                    NodeRef nodeCopy = new NodeRef(new Node());
                    this.nodes[nodeCopy].left = this.freelist;
                    this.freelist = nodeCopy;
                }
            }
#if DEBUG
            this.allocateHelper.allocateCount = original.allocateHelper.allocateCount;
#endif

            // TODO: performance and memory usage
            // Cloning a Splay tree is problematic because of the worst-case O(N) depth. Here we are using a breadth-first
            // traversal to clone, as it is likely to use less memory in a typically "leggy" splay tree (vs. other types)
            // than depth-first. Need to determine if this is sufficient or should be parameterized to give caller control,
            // with the option of an O(N lg N) traversal instead.
            this.root = this.Nil;
            if (original.root != original.Nil)
            {
                this.root = new NodeRef(new Node());

                Queue<STuple<NodeRef, NodeRef>> worklist = new Queue<STuple<NodeRef, NodeRef>>();
                worklist.Enqueue(new STuple<NodeRef, NodeRef>(this.root, original.root));
                while (worklist.Count != 0)
                {
                    STuple<NodeRef, NodeRef> item = worklist.Dequeue();

                    NodeRef nodeThis = item.Item1;
                    NodeRef nodeOriginal = item.Item2;

                    this.nodes[nodeThis].key = original.nodes[nodeOriginal].key;
                    this.nodes[nodeThis].value = original.nodes[nodeOriginal].value;
                    this.nodes[nodeThis].xOffset = original.nodes[nodeOriginal].xOffset;
                    this.nodes[nodeThis].yOffset = original.nodes[nodeOriginal].yOffset;
                    this.nodes[nodeThis].left = this.Nil;
                    this.nodes[nodeThis].right = this.Nil;

                    if (original.nodes[nodeOriginal].left != original.Nil)
                    {
                        this.nodes[nodeThis].left = new NodeRef(new Node());
                        worklist.Enqueue(new STuple<NodeRef, NodeRef>(this.nodes[nodeThis].left, original.nodes[nodeOriginal].left));
                    }
                    if (original.nodes[nodeOriginal].right != original.Nil)
                    {
                        this.nodes[nodeThis].right = new NodeRef(new Node());
                        worklist.Enqueue(new STuple<NodeRef, NodeRef>(this.nodes[nodeThis].right, original.nodes[nodeOriginal].right));
                    }
                }
            }
        }

        // Array

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a splay tree, explicitly configured.
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
        public SplayTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer, uint capacity, AllocationMode allocationMode)
        {
            if (allocationMode == AllocationMode.DynamicDiscard)
            {
                throw new ArgumentException();
            }

            this.comparer = comparer;
            this.root = Nil;

            this.allocationMode = allocationMode;
            this.freelist = Nil;
            EnsureFree(capacity);
            //#if DEBUG
            //            nodes[N].scratch_debug = true;
            //#endif
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a splay tree, with the specified capacity and allocation mode and using
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
        public SplayTree(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a splay tree, with the specified capacity and using
        /// the default comparer (applicable only for keyed collections). The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity of the tree, the memory for which is preallocated at construction time;
        /// if the capacity is exceeded, the internal array will be resized to make more nodes available.
        /// </param>
        [Storage(Storage.Array)]
        public SplayTree(uint capacity)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a splay tree, using
        /// the specified comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTree(IComparer<KeyType> comparer)
            : this(comparer, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a splay tree, using
        /// the default comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        [Storage(Storage.Array)]
        public SplayTree()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Array)]
        public SplayTree(SplayTree<KeyType, ValueType> original)
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
                // Since splay trees can be deep and thready, breadth-first is likely to use less memory than
                // depth-first. The worst-case is still N/2, so still risks being expensive.

                Queue<NodeRef> queue = new Queue<NodeRef>();
                if (root != Nil)
                {
                    queue.Enqueue(root);
                    while (queue.Count != 0)
                    {
                        NodeRef node = queue.Dequeue();
                        if (nodes[node].left != Nil)
                        {
                            queue.Enqueue(nodes[node].left);
                        }
                        if (nodes[node].right != Nil)
                        {
                            queue.Enqueue(nodes[node].right);
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
            this.yExtent = 0;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                return 0 == comparer.Compare(key, nodes[root].key);
            }
            return false;
        }

        [Feature(Feature.Dict)]
        private bool SetOrAddValue(KeyType key, [Payload(Payload.Value)] ValueType value, bool add)
        {
            Splay(ref root, key, comparer);
            int c;
            if ((root == Nil) || ((c = comparer.Compare(key, nodes[root].key)) < 0))
            {
                // insert item just in front of root

                /*[Count]*/
                ulong countNew = checked(this.count + 1);

                NodeRef i = Allocate();
                nodes[i].key = key;
                nodes[i].value = value;

                nodes[i].left = nodes[root].left;
                nodes[i].right = root;
                nodes[root].left = Nil;

                root = i;

                this.count = countNew;

                return true;
            }
            else if (c > 0)
            {
                // insert item just after root

                /*[Count]*/
                ulong countNew = checked(this.count + 1);

                NodeRef i = Allocate();
                nodes[i].key = key;
                nodes[i].value = value;

                nodes[i].right = nodes[root].right;
                nodes[i].left = root;
                nodes[root].right = Nil;

                root = i;

                this.count = countNew;

                return true;
            }
            else
            {
                Debug.Assert(c == 0);
                /*[Payload(Payload.Value)]*/
                if (add)
                {
                    nodes[root].value = value;
                }
                return false;
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict)]
        public bool SetOrAddValue(KeyType key, ValueType value)
        {
            return SetOrAddValue(key, value, true/*add*/);
        }

        [Feature(Feature.Dict)]
        public bool TryAdd(KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            return SetOrAddValue(key, /*[Payload(Payload.Value)]*/value, false/*add*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay(ref root, key, comparer);
                    int c = comparer.Compare(key, nodes[root].key);
                    if (c == 0)
                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                        Splay(ref nodes[root].right, key, comparer);
                        /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                        Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                        /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                        /*[Widen]*/
                        int xLength = nodes[root].right != Nil ? nodes[nodes[root].right].xOffset : this.xExtent - nodes[root].xOffset;

                        NodeRef dead, x;

                        dead = root;
                        if (nodes[root].left == Nil)
                        {
                            x = nodes[root].right;
                            if (x != Nil)
                            {
                                nodes[x].xOffset += nodes[root].xOffset - xLength;
                            }
                        }
                        else
                        {
                            x = nodes[root].left;
                            nodes[x].xOffset += nodes[root].xOffset;
                            Splay(ref x, key, comparer);
                            Debug.Assert(nodes[x].right == Nil);
                            if (nodes[root].right != Nil)
                            {
                                nodes[nodes[root].right].xOffset += nodes[root].xOffset - nodes[x].xOffset - xLength;
                            }
                            nodes[x].right = nodes[root].right;
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

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key, out KeyType keyOut)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                if (0 == comparer.Compare(key, nodes[root].key))
                {
                    keyOut = nodes[root].key;
                    return true;
                }
            }
            keyOut = default(KeyType);
            return false;
        }

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetKey(KeyType key)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                if (0 == comparer.Compare(key, nodes[root].key))
                {
                    nodes[root].key = key;
                    return true;
                }
            }
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetValue(KeyType key, out ValueType value)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                if (0 == comparer.Compare(key, nodes[root].key))
                {
                    value = nodes[root].value;
                    return true;
                }
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetValue(KeyType key, ValueType value)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                if (0 == comparer.Compare(key, nodes[root].key))
                {
                    nodes[root].value = value;
                    return true;
                }
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
            if (!TryGetValue(key, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void SetValue(KeyType key, ValueType value)
        {
            if (!TrySetValue(key, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool LeastInternal(out KeyType keyOut, [Payload(Payload.Value)] out ValueType valueOut)
        {
            if (root != Nil)
            {
                Splay(ref root, default(KeyType), FixedComparer.Minimum);
                keyOut = nodes[root].key;
                valueOut = nodes[root].value;
                return true;
            }
            keyOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
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
            if (root != Nil)
            {
                Splay(ref root, default(KeyType), FixedComparer.Maximum);
                keyOut = nodes[root].key;
                valueOut = nodes[root].value;
                return true;
            }
            keyOut = default(KeyType);
            valueOut = default(ValueType);
            return false;
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

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestLess(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                int rootComparison = comparer.Compare(key, nodes[root].key);
                if ((rootComparison > 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = nodes[root].key;
                    valueOut = nodes[root].value;
                    return true;
                }
                else if (nodes[root].left != Nil)
                {
                    KeyType rootKey = nodes[root].key;
                    Splay(ref nodes[root].left, rootKey, comparer);
                    nearestKey = nodes[nodes[root].left].key;
                    valueOut = nodes[nodes[root].left].value;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, true/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            rank = 0;
            rankCount = 0;
            bool f = NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                ValueType duplicateValue;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out duplicateValue, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, false/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, false/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            rank = 0;
            rankCount = 0;
            bool f = NearestLess(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                ValueType duplicateValue;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out duplicateValue, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestGreater(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key, comparer);
                int rootComparison = comparer.Compare(key, nodes[root].key);
                if ((rootComparison < 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = nodes[root].key;
                    valueOut = nodes[root].value;
                    return true;
                }
                else if (nodes[root].right != Nil)
                {
                    KeyType rootKey = nodes[root].key;
                    Splay(ref nodes[root].right, rootKey, comparer);
                    nearestKey = nodes[nodes[root].right].key;
                    valueOut = nodes[nodes[root].right].value;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            valueOut = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, true/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            rank = this.xExtent;
            rankCount = 0;
            bool f = NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, true/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                ValueType duplicateValue;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out duplicateValue, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g);
                Debug.Assert(0 == comparer.Compare(nearestKey, duplicateKey));
            }
            return f;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut)
        {
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, false/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key, out KeyType nearestKey)
        {
            ValueType value;
            return NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out value, false/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType valueOut, [Feature(Feature.Rank, Feature.RankMulti)][Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            rank = this.xExtent;
            rankCount = 0;
            bool f = NearestGreater(key, out nearestKey, /*[Payload(Payload.Value)]*/out valueOut, false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                ValueType duplicateValue;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out duplicateValue, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
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
        public bool Contains([Widen] int start, [Feature(Feature.Range2)] Side side)
        {
            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                return start == Start(root, /*[Feature(Feature.Range2)]*/side);
            }
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xLength, [Feature(Feature.Range2)][Widen] int yLength, [Payload(Payload.Value)] ValueType value)
        {
            unchecked
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

                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                {
                    // insert item just in front of root

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);
                    /*[Widen]*/
                    int yExtentNew = checked(this.yExtent + yLength);

                    NodeRef i = Allocate();
                    nodes[i].value = value;
                    nodes[i].xOffset = nodes[root].xOffset;
                    nodes[i].yOffset = nodes[root].yOffset;

                    nodes[i].left = nodes[root].left;
                    nodes[i].right = root;
                    if (root != Nil)
                    {
                        nodes[root].xOffset = xLength;
                        nodes[root].yOffset = yLength;
                        nodes[root].left = Nil;
                    }

                    root = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;

                    return true;
                }

                Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                int length = nodes[root].right != Nil
                    ? (side == Side.X ? nodes[nodes[root].right].xOffset : nodes[nodes[root].right].yOffset)
                    : (side == Side.X ? this.xExtent - nodes[root].xOffset : this.yExtent - nodes[root].yOffset);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side) + length)
                {
                    // append

                    Debug.Assert(nodes[root].right == Nil);

                    /*[Widen]*/
                    int xLengthRoot = this.xExtent - nodes[root].xOffset;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yLengthRoot = this.yExtent - nodes[root].yOffset;

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);
                    /*[Widen]*/
                    int yExtentNew = checked(this.yExtent + yLength);

                    NodeRef i = Allocate();
                    nodes[i].value = value;
                    nodes[i].xOffset = xLengthRoot;
                    nodes[i].yOffset = yLengthRoot;

                    nodes[i].left = Nil;
                    nodes[i].right = Nil;
                    Debug.Assert(root != Nil);
                    Debug.Assert(nodes[root].right == Nil);
                    nodes[root].right = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;

                    return true;
                }

                return false;
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] int start, [Feature(Feature.Range2)] Side side)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                    if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                    {
                        Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                        /*[Widen]*/
                        int xLength = nodes[root].right != Nil ? nodes[nodes[root].right].xOffset : this.xExtent - nodes[root].xOffset;
                        /*[Feature(Feature.Range2)]*/
                        /*[Widen]*/
                        int yLength = nodes[root].right != Nil ? nodes[nodes[root].right].yOffset : this.yExtent - nodes[root].yOffset;

                        NodeRef dead, x;

                        dead = root;
                        if (nodes[root].left == Nil)
                        {
                            x = nodes[root].right;
                            if (x != Nil)
                            {
                                nodes[x].xOffset += nodes[root].xOffset - xLength;
                                nodes[x].yOffset += nodes[root].yOffset - yLength;
                            }
                        }
                        else
                        {
                            x = nodes[root].left;
                            nodes[x].xOffset += nodes[root].xOffset;
                            nodes[x].yOffset += nodes[root].yOffset;
                            Splay2(ref x, start, /*[Feature(Feature.Range2)]*/side);
                            Debug.Assert(nodes[x].right == Nil);
                            if (nodes[root].right != Nil)
                            {
                                nodes[nodes[root].right].xOffset += nodes[root].xOffset - nodes[x].xOffset - xLength;
                                nodes[nodes[root].right].yOffset += nodes[root].yOffset - nodes[x].yOffset - yLength;
                            }
                            nodes[x].right = nodes[root].right;
                        }
                        root = x;

                        this.count = unchecked(this.count - 1);
                        this.xExtent = unchecked(this.xExtent - xLength);
                        this.yExtent = unchecked(this.yExtent - yLength);
                        Free(dead);

                        return true;
                    }
                }
                return false;
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out int length)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                    if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                    {
                        Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/side);
                        if (nodes[root].right != Nil)
                        {
                            Debug.Assert(nodes[nodes[root].right].left == Nil);
                            length = side == Side.X ? nodes[nodes[root].right].xOffset : nodes[nodes[root].right].yOffset;
                        }
                        else
                        {
                            length = side == Side.X ? this.xExtent - start : this.yExtent - start;
                        }
                        return true;
                    }
                }
                length = 0;
                return false;
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                {
                    /*[Widen]*/
                    int oldLength;
                    if (nodes[root].right != Nil)
                    {
                        Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(nodes[nodes[root].right].left == Nil);
                        oldLength = side == Side.X ? nodes[nodes[root].right].xOffset : nodes[nodes[root].right].yOffset;
                    }
                    else
                    {
                        oldLength = side == Side.X ? unchecked(this.xExtent - nodes[root].xOffset) : unchecked(this.yExtent - nodes[root].yOffset);
                    }
                    /*[Widen]*/
                    int delta = length - oldLength;
                    if (side == Side.X)
                    {
                        this.xExtent = checked(this.xExtent + delta);

                        if (nodes[root].right != Nil)
                        {
                            unchecked
                            {
                                nodes[nodes[root].right].xOffset += delta;
                            }
                        }
                    }
                    else
                    {
                        this.yExtent = checked(this.yExtent + delta);

                        if (nodes[root].right != Nil)
                        {
                            unchecked
                            {
                                nodes[nodes[root].right].yOffset += delta;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] int start, [Feature(Feature.Range2)] Side side, out ValueType value)
        {
            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                {
                    value = nodes[root].value;
                    return true;
                }
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] int start, [Feature(Feature.Range2)] Side side, ValueType value)
        {
            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                {
                    nodes[root].value = value;
                    return true;
                }
            }
            return false;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                    if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                    {
                        Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                        otherStart = side != Side.X ? nodes[root].xOffset : nodes[root].yOffset;
                        if (nodes[root].right != Nil)
                        {
                            xLength = nodes[nodes[root].right].xOffset;
                            yLength = nodes[nodes[root].right].yOffset;
                        }
                        else
                        {
                            xLength = this.xExtent - nodes[root].xOffset;
                            yLength = this.yExtent - nodes[root].yOffset;
                        }
                        value = nodes[root].value;
                        return true;
                    }
                }
                otherStart = 0;
                xLength = 0;
                yLength = 0;
                value = default(ValueType);
                return false;
            }
        }

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

            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                {
                    /*[Widen]*/
                    int xLengthOld, yLengthOld;
                    if (nodes[root].right != Nil)
                    {
                        Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(nodes[nodes[root].right].left == Nil);
                        xLengthOld = nodes[nodes[root].right].xOffset;
                        yLengthOld = nodes[nodes[root].right].yOffset;
                    }
                    else
                    {
                        xLengthOld = unchecked(this.xExtent - nodes[root].xOffset);
                        yLengthOld = unchecked(this.yExtent - nodes[root].yOffset);
                    }

                    /*[Widen]*/
                    int xAdjust = xLength != 0 ? xLength - xLengthOld : 0;
                    /*[Widen]*/
                    int yAdjust = yLength != 0 ? yLength - yLengthOld : 0;

                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xAdjust);
                    /*[Widen]*/
                    int yExtentNew = checked(this.yExtent + yAdjust);
                    // throw overflow before updating anything
                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;

                    if (nodes[root].right != Nil)
                    {
                        unchecked
                        {
                            nodes[nodes[root].right].xOffset += xAdjust;
                        }
                    }
                    if (nodes[root].right != Nil)
                    {
                        unchecked
                        {
                            nodes[nodes[root].right].yOffset += yAdjust;
                        }
                    }

                    nodes[root].value = value;

                    return true;
                }
            }
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] int start, [Feature(Feature.Range2)] Side side, [Widen] int xLength, [Feature(Feature.Range2)][Widen] int yLength, [Payload(Payload.Value)] ValueType value)
        {
            if (!TryInsert(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] int start, [Feature(Feature.Range2)] Side side)
        {
            if (!TryDelete(start, /*[Feature(Feature.Range2)]*/side))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetLength([Widen] int start, [Feature(Feature.Range2)] Side side)
        {
            /*[Widen]*/
            int length;
            if (!TryGetLength(start, /*[Feature(Feature.Range2)]*/side, out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] int start, [Feature(Feature.Range2)] Side side, [Widen] int length)
        {
            if (!TrySetLength(start, /*[Feature(Feature.Range2)]*/side, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public ValueType GetValue([Widen] int start, [Feature(Feature.Range2)] Side side)
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
        public void SetValue([Widen] int start, [Feature(Feature.Range2)] Side side, ValueType value)
        {
            if (!TrySetValue(start, /*[Feature(Feature.Range2)]*/side, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] int start, [Feature(Feature.Range2)] Side side, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            if (!TryGet(start, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

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
        private bool NearestLess([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                int start = Start(root, /*[Feature(Feature.Range2)]*/side);
                if ((position < start) || (!orEqual && (position == start)))
                {
                    if (nodes[root].left != Nil)
                    {
                        Splay2(ref nodes[root].left, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(nodes[nodes[root].left].right == Nil);
                        nearestKey = nodes[nodes[root].left].key;
                        nearestStart = start + (side == Side.X ? nodes[nodes[root].left].xOffset : nodes[nodes[root].left].yOffset);
                        return true;
                    }
                    nearestKey = default(KeyType);
                    nearestStart = 0;
                    return false;
                }
                else
                {
                    nearestKey = nodes[root].key;
                    nearestStart = Start(root, /*[Feature(Feature.Range2)]*/side);
                    return true;
                }
            }
            nearestKey = default(KeyType);
            nearestStart = 0;
            return false;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position, [Feature(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestLess(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position, [Feature(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            return NearestLess(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position, [Feature(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestLess(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position, [Feature(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            return NearestLess(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestGreater([Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                int start = Start(root, /*[Feature(Feature.Range2)]*/side);
                if ((position > start) || (!orEqual && (position == start)))
                {
                    if (nodes[root].right != Nil)
                    {
                        Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(nodes[nodes[root].right].left == Nil);
                        nearestKey = nodes[nodes[root].right].key;
                        nearestStart = start + (side == Side.X ? nodes[nodes[root].right].xOffset : nodes[nodes[root].right].yOffset);
                        return true;
                    }
                    nearestKey = default(KeyType);
                    nearestStart = side == Side.X ? this.xExtent : this.yExtent;
                    return false;
                }
                else
                {
                    nearestKey = nodes[root].key;
                    nearestStart = start;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            nearestStart = 0;
            return false;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position, [Feature(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestGreater(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position, [Feature(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            return NearestGreater(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, true/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position, [Feature(Feature.Range2)] Side side, [Feature(Feature.RankMulti)] out KeyType nearestKey, [Widen] out int nearestStart, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestGreater(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position, [Feature(Feature.Range2)] Side side, [Widen] out int nearestStart)
        {
            KeyType nearestKey;
            return NearestGreater(position, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.RankMulti)]*/out nearestKey, out nearestStart, false/*orEqual*/);
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
            unchecked
            {
                if (rankCount <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                Splay(ref root, key, comparer);
                int c;
                if ((root == Nil) || ((c = comparer.Compare(key, nodes[root].key)) < 0))
                {
                    // insert item just in front of root

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + rankCount);

                    NodeRef i = Allocate();
                    nodes[i].key = key;
                    nodes[i].value = value;
                    nodes[i].xOffset = nodes[root].xOffset;

                    nodes[i].left = nodes[root].left;
                    nodes[i].right = root;
                    if (root != Nil)
                    {
                        nodes[root].xOffset = rankCount;
                        nodes[root].left = Nil;
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
                    int xExtentNew = checked(this.xExtent + rankCount);

                    NodeRef i = Allocate();
                    nodes[i].key = key;
                    nodes[i].value = value;

                    /*[Feature(Feature.RankMulti)]*/
                    Splay(ref nodes[root].right, key, comparer);
                    /*[Feature(Feature.RankMulti)]*/
                    Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                    /*[Widen]*/
                    int rootLength = 1;
                    /*[Feature(Feature.RankMulti)]*/
                    rootLength = nodes[root].right != Nil ? nodes[nodes[root].right].xOffset : this.xExtent - nodes[root].xOffset;

                    nodes[i].xOffset = nodes[root].xOffset + rootLength;
                    if (nodes[root].right != Nil)
                    {
                        nodes[nodes[root].right].xOffset += -rootLength + rankCount;
                    }
                    nodes[root].xOffset = -rootLength;

                    nodes[i].left = root;
                    nodes[i].right = nodes[root].right;
                    nodes[root].right = Nil;

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

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGet(KeyType key, [Payload(Payload.None)] out KeyType keyOut, [Payload(Payload.Value)] out ValueType value, [Widen] out int rank, [Feature(Feature.RankMulti)][Widen] out int rankCount)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay(ref root, key, comparer);
                    if (0 == comparer.Compare(key, nodes[root].key))
                    {
                        /*[Feature(Feature.RankMulti)]*/
                        Splay(ref nodes[root].right, key, comparer);
                        /*[Feature(Feature.RankMulti)]*/
                        Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                        rank = nodes[root].xOffset;
                        /*[Feature(Feature.RankMulti)]*/
                        rankCount = nodes[root].right != Nil ? nodes[nodes[root].right].xOffset : this.xExtent - nodes[root].xOffset;
                        keyOut = nodes[root].key;
                        value = nodes[root].value;
                        return true;
                    }
                }
                rank = 0;
                /*[Feature(Feature.RankMulti)]*/
                rankCount = 0;
                keyOut = default(KeyType);
                value = default(ValueType);
                return false;
            }
        }

        [Feature(Feature.RankMulti)]
        public bool TrySet(KeyType key, [Payload(Payload.Value)] ValueType value, [Widen] int rankCount)
        {
            unchecked
            {
                Splay(ref root, key, comparer);
                int c;
                if ((root != Nil) && ((c = comparer.Compare(key, nodes[root].key)) == 0))
                {
                    Splay(ref nodes[root].right, key, comparer);
                    Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                    /*[Widen]*/
                    int oldLength = nodes[root].right != Nil ? nodes[nodes[root].right].xOffset : this.xExtent - nodes[root].xOffset;

                    if (rankCount > 0)
                    {
                        /*[Widen]*/
                        int countAdjust = checked(rankCount - oldLength);
                        this.xExtent = checked(this.xExtent + countAdjust);

                        if (nodes[root].right != Nil)
                        {
                            unchecked
                            {
                                nodes[nodes[root].right].xOffset += countAdjust;
                            }
                        }

                        nodes[root].value = value;
                        /*[Payload(Payload.None)]*/
                        nodes[root].key = key;

                        return true;
                    }
                }

                return false;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGetKeyByRank([Widen] int rank, out KeyType key)
        {
            unchecked
            {
                if (rank < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (root != Nil)
                {
                    Splay2(ref root, rank, /*[Feature(Feature.Range2)]*/Side.X);
                    if (rank < Start(root, /*[Feature(Feature.Range2)]*/Side.X))
                    {
                        Debug.Assert(rank >= 0);
                        Debug.Assert(nodes[root].left != Nil); // because rank >= 0 and tree starts at 0
                        Splay2(ref nodes[root].left, 0, /*[Feature(Feature.Range2)]*/Side.X);
                        key = nodes[nodes[root].left].key;
                        return true;
                    }

                    Splay2(ref nodes[root].right, 0, /*[Feature(Feature.Range2)]*/Side.X);
                    Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                    /*[Widen]*/
                    int length = nodes[root].right != Nil ? nodes[nodes[root].right].xOffset : this.xExtent - nodes[root].xOffset;
                    if (/*(rank >= Start(root, Side.X)) && */(rank < Start(root, /*[Feature(Feature.Range2)]*/Side.X) + length))
                    {
                        Debug.Assert(rank >= Start(root, /*[Feature(Feature.Range2)]*/Side.X));
                        key = nodes[root].key;
                        return true;
                    }
                }
                key = default(KeyType);
                return false;
            }
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
        public void AdjustCount(KeyType key, [Widen] int countAdjust)
        {
            unchecked
            {
                Splay(ref root, key, comparer);
                int c;
                if ((root != Nil) && ((c = comparer.Compare(key, nodes[root].key)) == 0))
                {
                    // update and possibly remove

                    Splay(ref nodes[root].right, key, comparer);
                    Debug.Assert((nodes[root].right == Nil) || (nodes[nodes[root].right].left == Nil));
                    /*[Widen]*/
                    int oldLength = nodes[root].right != Nil ? nodes[nodes[root].right].xOffset : this.xExtent - nodes[root].xOffset;

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

                        if (nodes[root].right != Nil)
                        {
                            unchecked
                            {
                                nodes[nodes[root].right].xOffset += countAdjust;
                            }
                        }
                    }
                    else if (oldLength + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        NodeRef dead, x;

                        dead = root;
                        if (nodes[root].left == Nil)
                        {
                            x = nodes[root].right;
                            if (x != Nil)
                            {
                                Debug.Assert(nodes[root].xOffset == 0);
                                nodes[x].xOffset = 0; //nodes[x].xOffset = nodes[root].xOffset;
                            }
                        }
                        else
                        {
                            x = nodes[root].left;
                            nodes[x].xOffset += nodes[root].xOffset;
                            Splay(ref x, key, comparer);
                            Debug.Assert(nodes[x].right == Nil);
                            if (nodes[root].right != Nil)
                            {
                                nodes[nodes[root].right].xOffset += nodes[root].xOffset - nodes[x].xOffset + countAdjust;
                            }
                            nodes[x].right = nodes[root].right;
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

                        Add(key, /*[Payload(Payload.Value)]*/default(ValueType), /*[Feature(Feature.RankMulti)]*/countAdjust);
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
        private NodeRef Allocate()
        {
            NodeRef node = freelist;
            if (node != Nil)
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
                allocateHelper.allocateCount = checked(allocateHelper.allocateCount + 1);
#endif
            }

            return node;
        }

        [Storage(Storage.Object)]
        private void Free(NodeRef node)
        {
#if DEBUG
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == count);

            nodes[node].left = new NodeRef(null);
            nodes[node].right = new NodeRef(null);
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
                Debug.Assert(freelist == Nil);
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
        private NodeRef Allocate()
        {
            if (freelist == Nil)
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
                if (freelist == Nil)
                {
                    throw new OutOfMemoryException();
                }
            }
            NodeRef node = freelist;
            freelist = nodes[freelist].left;

            return node;
        }

        [Storage(Storage.Array)]
        private void Free(NodeRef node)
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
                Debug.Assert(freelist == Nil);
                Debug.Assert((nodes == null) || (nodes.Length >= ReservedElements));

                uint oldLength = nodes != null ? unchecked((uint)nodes.Length) : ReservedElements;
                uint newLength = checked(capacity + ReservedElements);

                Array.Resize(ref nodes, unchecked((int)newLength));

                // TODO:was attempt at reducing array bounds checks
                //Node[] nodesNew = nodes;
                //Node2[] nodes2New = nodes2;
                //Array.Resize(ref nodesNew, unchecked((int)newCount));
                //bool key = false, payload = false;
                ///*[Payload(Payload.Value)]*/
                //payload = true;
                ///*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                //key = true;
                //if (payload || key)
                //{
                //    Array.Resize(ref nodes2New, unchecked((int)newCount));
                //}
                //nodes = nodesNew;
                //nodes2 = nodes2New;

                for (long i = (long)newLength - 1; i >= oldLength; i--)
                {
                    Free(new NodeRef(unchecked((uint)i)));
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
        [EnableFixed]
        private void Splay(ref NodeRef root, KeyType leftComparand, IComparer<KeyType> comparer)
        {
            unchecked
            {
                this.version = unchecked((ushort)(this.version + 1));

                if (root == Nil)
                {
                    return;
                }

                NodeRef t = root;

                nodes[N].left = Nil;
                nodes[N].right = Nil;

                NodeRef l = N;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int lxOffset = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int lyOffset = 0;
                NodeRef r = N;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int rxOffset = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int ryOffset = 0;

                while (true)
                {
                    int c;

                    c = comparer.Compare(leftComparand, nodes[t].key);
                    if (c < 0)
                    {
                        if (nodes[t].left == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, nodes[nodes[t].left].key);
                        if (c < 0)
                        {
                            // rotate right
                            NodeRef u = nodes[t].left;
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            /*[Widen]*/
                            int uXPosition = nodes[t].xOffset + nodes[u].xOffset;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            int uYPosition = nodes[t].yOffset + nodes[u].yOffset;
                            if (nodes[u].right != Nil)
                            {
                                nodes[nodes[u].right].xOffset += uXPosition - nodes[t].xOffset;
                                nodes[nodes[u].right].yOffset += uYPosition - nodes[t].yOffset;
                            }
                            nodes[t].xOffset += -uXPosition;
                            nodes[t].yOffset += -uYPosition;
                            nodes[u].xOffset = uXPosition;
                            nodes[u].yOffset = uYPosition;
                            nodes[t].left = nodes[u].right;
                            nodes[u].right = t;
                            t = u;
                            if (nodes[t].left == Nil)
                            {
                                break;
                            }
                        }
                        // link right
                        Debug.Assert(nodes[t].left != Nil);
                        nodes[nodes[t].left].xOffset += nodes[t].xOffset;
                        nodes[nodes[t].left].yOffset += nodes[t].yOffset;
                        nodes[t].xOffset -= rxOffset;
                        nodes[t].yOffset -= ryOffset;
                        nodes[r].left = t;
                        r = t;
                        rxOffset += nodes[r].xOffset;
                        ryOffset += nodes[r].yOffset;
                        t = nodes[t].left;
                    }
                    else if (c > 0)
                    {
                        if (nodes[t].right == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, nodes[nodes[t].right].key);
                        if (c > 0)
                        {
                            // rotate left
                            NodeRef u = nodes[t].right;
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            /*[Widen]*/
                            int uXPosition = nodes[t].xOffset + nodes[u].xOffset;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            int uYPosition = nodes[t].yOffset + nodes[u].yOffset;
                            if (nodes[u].left != Nil)
                            {
                                nodes[nodes[u].left].xOffset += uXPosition - nodes[t].xOffset;
                                nodes[nodes[u].left].yOffset += uYPosition - nodes[t].yOffset;
                            }
                            nodes[t].xOffset += -uXPosition;
                            nodes[t].yOffset += -uYPosition;
                            nodes[u].xOffset = uXPosition;
                            nodes[u].yOffset = uYPosition;
                            nodes[t].right = nodes[u].left;
                            nodes[u].left = t;
                            t = u;
                            if (nodes[t].right == Nil)
                            {
                                break;
                            }
                        }
                        // link left
                        Debug.Assert(nodes[t].right != Nil);
                        nodes[nodes[t].right].xOffset += nodes[t].xOffset;
                        nodes[nodes[t].right].yOffset += nodes[t].yOffset;
                        nodes[t].xOffset -= lxOffset;
                        nodes[t].yOffset -= lyOffset;
                        nodes[l].right = t;
                        l = t;
                        lxOffset += nodes[l].xOffset;
                        lyOffset += nodes[l].yOffset;
                        t = nodes[t].right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                nodes[l].right = nodes[t].left;
                if (nodes[l].right != Nil)
                {
                    nodes[nodes[l].right].xOffset += nodes[t].xOffset - lxOffset;
                    nodes[nodes[l].right].yOffset += nodes[t].yOffset - lyOffset;
                }
                nodes[r].left = nodes[t].right;
                if (nodes[r].left != Nil)
                {
                    nodes[nodes[r].left].xOffset += nodes[t].xOffset - rxOffset;
                    nodes[nodes[r].left].yOffset += nodes[t].yOffset - ryOffset;
                }
                nodes[t].left = nodes[N].right;
                if (nodes[t].left != Nil)
                {
                    nodes[nodes[t].left].xOffset -= nodes[t].xOffset;
                    nodes[nodes[t].left].yOffset -= nodes[t].yOffset;
                }
                nodes[t].right = nodes[N].left;
                if (nodes[t].right != Nil)
                {
                    nodes[nodes[t].right].xOffset -= nodes[t].xOffset;
                    nodes[nodes[t].right].yOffset -= nodes[t].yOffset;
                }
                root = t;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Widen]
        private int Start(NodeRef n, [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return side == Side.X ? nodes[n].xOffset : nodes[n].yOffset;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [EnableFixed]
        private void Splay2(ref NodeRef root, [Widen] int position, [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            unchecked
            {
                this.version = unchecked((ushort)(this.version + 1));

                if (root == Nil)
                {
                    return;
                }

                NodeRef t = root;

                nodes[N].left = Nil;
                nodes[N].right = Nil;

                NodeRef l = N;
                /*[Widen]*/
                int lxOffset = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int lyOffset = 0;
                NodeRef r = N;
                /*[Widen]*/
                int rxOffset = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int ryOffset = 0;

                while (true)
                {
                    int c;

                    c = position.CompareTo(side == Side.X ? nodes[t].xOffset : nodes[t].yOffset);
                    if (c < 0)
                    {
                        if (nodes[t].left == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(side == Side.X
                                ? nodes[t].xOffset + nodes[nodes[t].left].xOffset
                                : nodes[t].yOffset + nodes[nodes[t].left].yOffset);
                        if (c < 0)
                        {
                            // rotate right
                            NodeRef u = nodes[t].left;
                            /*[Widen]*/
                            int uXPosition = nodes[t].xOffset + nodes[u].xOffset;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            int uYPosition = nodes[t].yOffset + nodes[u].yOffset;
                            if (nodes[u].right != Nil)
                            {
                                nodes[nodes[u].right].xOffset += uXPosition - nodes[t].xOffset;
                                nodes[nodes[u].right].yOffset += uYPosition - nodes[t].yOffset;
                            }
                            nodes[t].xOffset += -uXPosition;
                            nodes[t].yOffset += -uYPosition;
                            nodes[u].xOffset = uXPosition;
                            nodes[u].yOffset = uYPosition;
                            nodes[t].left = nodes[u].right;
                            nodes[u].right = t;
                            t = u;
                            if (nodes[t].left == Nil)
                            {
                                break;
                            }
                        }
                        // link right
                        Debug.Assert(nodes[t].left != Nil);
                        nodes[nodes[t].left].xOffset += nodes[t].xOffset;
                        nodes[nodes[t].left].yOffset += nodes[t].yOffset;
                        nodes[t].xOffset -= rxOffset;
                        nodes[t].yOffset -= ryOffset;
                        nodes[r].left = t;
                        r = t;
                        rxOffset += nodes[r].xOffset;
                        ryOffset += nodes[r].yOffset;
                        t = nodes[t].left;
                    }
                    else if (c > 0)
                    {
                        if (nodes[t].right == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(side == Side.X
                                ? (nodes[t].xOffset + nodes[nodes[t].right].xOffset)
                                : (nodes[t].yOffset + nodes[nodes[t].right].yOffset));
                        if (c > 0)
                        {
                            // rotate left
                            NodeRef u = nodes[t].right;
                            /*[Widen]*/
                            int uXPosition = nodes[t].xOffset + nodes[u].xOffset;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            int uYPosition = nodes[t].yOffset + nodes[u].yOffset;
                            if (nodes[u].left != Nil)
                            {
                                nodes[nodes[u].left].xOffset += uXPosition - nodes[t].xOffset;
                                nodes[nodes[u].left].yOffset += uYPosition - nodes[t].yOffset;
                            }
                            nodes[t].xOffset += -uXPosition;
                            nodes[t].yOffset += -uYPosition;
                            nodes[u].xOffset = uXPosition;
                            nodes[u].yOffset = uYPosition;
                            nodes[t].right = nodes[u].left;
                            nodes[u].left = t;
                            t = u;
                            if (nodes[t].right == Nil)
                            {
                                break;
                            }
                        }
                        // link left
                        Debug.Assert(nodes[t].right != Nil);
                        nodes[nodes[t].right].xOffset += nodes[t].xOffset;
                        nodes[nodes[t].right].yOffset += nodes[t].yOffset;
                        nodes[t].xOffset -= lxOffset;
                        nodes[t].yOffset -= lyOffset;
                        nodes[l].right = t;
                        l = t;
                        lxOffset += nodes[l].xOffset;
                        lyOffset += nodes[l].yOffset;
                        t = nodes[t].right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                nodes[l].right = nodes[t].left;
                if (nodes[l].right != Nil)
                {
                    nodes[nodes[l].right].xOffset += nodes[t].xOffset - lxOffset;
                    nodes[nodes[l].right].yOffset += nodes[t].yOffset - lyOffset;
                }
                nodes[r].left = nodes[t].right;
                if (nodes[r].left != Nil)
                {
                    nodes[nodes[r].left].xOffset += nodes[t].xOffset - rxOffset;
                    nodes[nodes[r].left].yOffset += nodes[t].yOffset - ryOffset;
                }
                nodes[t].left = nodes[N].right;
                if (nodes[t].left != Nil)
                {
                    nodes[nodes[t].left].xOffset -= nodes[t].xOffset;
                    nodes[nodes[t].left].yOffset -= nodes[t].yOffset;
                }
                nodes[t].right = nodes[N].left;
                if (nodes[t].right != Nil)
                {
                    nodes[nodes[t].right].xOffset -= nodes[t].xOffset;
                    nodes[nodes[t].right].yOffset -= nodes[t].yOffset;
                }
                root = t;
            }
        }


        //
        // Non-invasive tree inspection support
        //

        // Helpers

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [ExcludeFromCodeCoverage]
        private void ValidateRanges([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            if (root != Nil)
            {
                Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int offset = 0;
                /*[Widen]*/
                int leftEdge = 0;
                /*[Widen]*/
                int rightEdge = side == Side.X ? this.xExtent : this.yExtent;

                NodeRef node = root;
                while (node != Nil)
                {
                    offset += side == Side.X ? nodes[node].xOffset : nodes[node].yOffset;
                    stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                    rightEdge = offset;
                    node = nodes[node].left;
                }
                while (stack.Count != 0)
                {
                    STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    offset = t.Item2;
                    leftEdge = t.Item3;
                    rightEdge = t.Item4;

                    if ((offset < leftEdge) || (offset >= rightEdge))
                    {
                        throw new InvalidOperationException("range containment invariant");
                    }

                    leftEdge = offset + 1;
                    node = nodes[node].right;
                    while (node != Nil)
                    {
                        offset += side == Side.X ? nodes[node].xOffset : nodes[node].yOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = nodes[node].left;
                    }
                }
            }
        }

        // INonInvasiveTreeInspection

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.Root { get { return root != Nil ? (object)root : null; } }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].left != Nil ? (object)nodes[n].left : null;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].right != Nil ? (object)nodes[n].right : null;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetKey(object node)
        {
            NodeRef n = (NodeRef)node;
            object key = null;
            key = nodes[n].key;
            return key;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetValue(object node)
        {
            NodeRef n = (NodeRef)node;
            object value = null;
            value = nodes[n].value;
            return value;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            return null;
        }

        [ExcludeFromCodeCoverage]
        void INonInvasiveTreeInspection.Validate()
        {
            if (root != Nil)
            {
                Dictionary<NodeRef, bool> visited = new Dictionary<NodeRef, bool>();
                Queue<NodeRef> worklist = new Queue<NodeRef>();
                worklist.Enqueue(root);
                while (worklist.Count != 0)
                {
                    NodeRef node = worklist.Dequeue();

                    if (visited.ContainsKey(node))
                    {
                        throw new InvalidOperationException("cycle");
                    }
                    visited.Add(node, false);

                    if (nodes[node].left != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(nodes[nodes[node].left].key, nodes[node].key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(nodes[node].left);
                    }
                    if (nodes[node].right != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(nodes[node].key, nodes[nodes[node].right].key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(nodes[node].right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.X);
            /*[Feature(Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.Y);
        }

        // INonInvasiveRange2MapInspection

        [Feature(Feature.Range, Feature.Range2)]
        [ExcludeFromCodeCoverage]
        [Widen]
        Range2MapEntry[] INonInvasiveRange2MapInspection.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntry[] ranges = new Range2MapEntry[Count];
            int i = 0;

            if (root != Nil)
            {
                Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;
                /*[Widen]*/
                int yOffset = 0;

                NodeRef node = root;
                while (node != Nil)
                {
                    xOffset += nodes[node].xOffset;
                    yOffset += nodes[node].yOffset;
                    stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                    node = nodes[node].left;
                }
                while (stack.Count != 0)
                {
                    STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;
                    yOffset = t.Item3;

                    object value = null;
                    value = nodes[node].value;

                    /*[Widen]*/
                    ranges[i++] = new Range2MapEntry(new Range(xOffset, 0), new Range(yOffset, 0), value);

                    node = nodes[node].right;
                    while (node != Nil)
                    {
                        xOffset += nodes[node].xOffset;
                        yOffset += nodes[node].yOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                        node = nodes[node].left;
                    }
                }
                if (!(i == ranges.Length))
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                for (i = 1; i < ranges.Length; i++)
                {
                    if (!(ranges[i - 1].x.start < ranges[i].x.start))
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
                    /*[Feature(Feature.Range2)]*/
                    if (!(ranges[i - 1].y.start < ranges[i].y.start))
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }
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

        [Feature(Feature.Range, Feature.Range2)]
        [ExcludeFromCodeCoverage]
        void INonInvasiveRange2MapInspection.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }

        // INonInvasiveMultiRankMapInspection

        [Feature(Feature.Rank, Feature.RankMulti)]
        [ExcludeFromCodeCoverage]
        [Widen]
        MultiRankMapEntry[] INonInvasiveMultiRankMapInspection.GetRanks()
        {
            /*[Widen]*/
            MultiRankMapEntry[] ranks = new MultiRankMapEntry[Count];
            int i = 0;

            if (root != Nil)
            {
                Stack<STuple<NodeRef, /*[Widen]*/int>> stack = new Stack<STuple<NodeRef, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;

                NodeRef node = root;
                while (node != Nil)
                {
                    xOffset += nodes[node].xOffset;
                    stack.Push(new STuple<NodeRef, /*[Widen]*/int>(node, xOffset));
                    node = nodes[node].left;
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

                    /*[Widen]*/
                    ranks[i++] = new MultiRankMapEntry(key, new Range(xOffset, 0), value);

                    node = nodes[node].right;
                    while (node != Nil)
                    {
                        xOffset += nodes[node].xOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int>(node, xOffset));
                        node = nodes[node].left;
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
        [ExcludeFromCodeCoverage]
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
        public IEnumerator<SplayTreeEntry<KeyType, ValueType>> GetEnumerator()
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
        /// is inserted or removed and it comes before the enumerator’s current key in sorting order, it will have no affect on the
        /// enumerator. If a key is inserted or removed and it comes after the enumerator’s current key (i.e. in the portion of the
        /// collection the enumerator hasn’t visited yet), the enumerator will include the key if inserted or skip the key if removed.
        /// Because the enumerator queries the tree for each element it’s running time per element is O(lg N), or O(N lg N) to
        /// enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<SplayTreeEntry<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<SplayTreeEntry<KeyType, ValueType>>
        {
            private readonly SplayTree<KeyType, ValueType> tree;

            public RobustEnumerableSurrogate(SplayTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<SplayTreeEntry<KeyType, ValueType>> GetEnumerator()
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
        /// robust enumerator’s O(N lg N) complexity.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<SplayTreeEntry<KeyType, ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<SplayTreeEntry<KeyType, ValueType>>
        {
            private readonly SplayTree<KeyType, ValueType> tree;

            public FastEnumerableSurrogate(SplayTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<SplayTreeEntry<KeyType, ValueType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<SplayTreeEntry<KeyType, ValueType>>
        {
            private readonly SplayTree<KeyType, ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(SplayTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public SplayTreeEntry<KeyType, ValueType> Current
            {
                get
                {
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (version != tree.version)
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
                            // OR
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            tree.Get(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey,
                                /*[Payload(Payload.None)]*/out key,
                                /*[Payload(Payload.Value)]*/out value,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/out rank,
                                /*[Feature(Feature.RankMulti)]*/out count);

                            /*[Feature(Feature.Rank)]*/
                            Debug.Assert(count == 1);

                            return new SplayTreeEntry<KeyType, ValueType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/rank,
                                /*[Feature(Feature.RankMulti)]*/count,
                                /*[Feature(Feature.Range2)]*/0,
                                /*[Feature(Feature.Range2)]*/0);
                        }

                        // OR

                        /*[Feature(Feature.Range, Feature.Range2)]*/
                        {
                            ValueType value = default(ValueType);
                            /*[Widen]*/
                            int xStart = 0, xLength = 0;
                            /*[Widen]*/
                            int yStart = 0, yLength = 0;
                            xStart = currentXStart;

                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range2)]*/Side.X,
                                /*[Feature(Feature.Range2)]*/out yStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength,
                                /*[Feature(Feature.Range2)]*/out yLength,
                                /*[Payload(Payload.Value)]*/out value);

                            /*[Feature(Feature.Range, Feature.Range2)]*/
                            version = tree.version; // our query is ok

                            return new SplayTreeEntry<KeyType, ValueType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength,
                                /*[Feature(Feature.Range2)]*/yStart,
                                /*[Feature(Feature.Range2)]*/yLength);
                        }
                    }
                    return new SplayTreeEntry<KeyType, ValueType>();
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
                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (version != tree.version)
                {
                    throw new InvalidOperationException();
                }

                if (!started)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    valid = tree.Least(out currentKey);

                    // OR

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    valid = tree.xExtent != 0;
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    Debug.Assert(currentXStart == 0);

                    started = true;
                }
                else if (valid)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    valid = tree.NearestGreater(currentKey, out currentKey);

                    // OR

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    valid = tree.NearestGreater(currentXStart, /*[Feature(Feature.Range2)]*/Side.X, out currentXStart);
                }

                /*[Feature(Feature.Range, Feature.Range2)]*/
                version = tree.version; // our query is ok

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;
                currentKey = default(KeyType);
                currentXStart = 0;
                /*[Feature(Feature.Range, Feature.Range2)]*/
                version = tree.version;
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any change to the tree invalidates it, and that *includes queries* since a query causes a splay
        /// operation that changes the structure of the tree.
        /// Worse, this enumerator also uses a stack that can be as deep as the tree, and since the depth of a splay
        /// tree is in the worst case n (number of nodes), the stack can potentially be size n.
        /// </summary>
        public class FastEnumerator : IEnumerator<SplayTreeEntry<KeyType, ValueType>>
        {
            private readonly SplayTree<KeyType, ValueType> tree;
            private ushort version;
            private NodeRef currentNode;
            private NodeRef nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;
            [Feature(Feature.Range2)]
            [Widen]
            private int currentYStart, nextYStart;

            private readonly Stack<STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>> stack
                = new Stack<STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>>();

            public FastEnumerator(SplayTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public SplayTreeEntry<KeyType, ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Nil)
                    {
                        /*[Feature(Feature.Rank)]*/
                        Debug.Assert(nextXStart - currentXStart == 1);

                        return new SplayTreeEntry<KeyType, ValueType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/tree.nodes[currentNode].key,
                            /*[Payload(Payload.Value)]*/tree.nodes[currentNode].value,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart,
                            /*[Feature(Feature.Range2)]*/currentYStart,
                            /*[Feature(Feature.Range2)]*/nextYStart - currentYStart);
                    }
                    return new SplayTreeEntry<KeyType, ValueType>();
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
                currentYStart = 0;
                nextXStart = 0;
                nextYStart = 0;

                this.version = tree.version;

                PushSuccessor(
                    tree.root,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                    /*[Feature(Feature.Range2)]*/0);

                Advance();
            }

            private void PushSuccessor(
                NodeRef node,
                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xPosition,
                [Feature(Feature.Range2)][Widen] int yPosition)
            {
                while (node != tree.Nil)
                {
                    xPosition += tree.nodes[node].xOffset;
                    yPosition += tree.nodes[node].yOffset;

                    stack.Push(new STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>(
                        node,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                        /*[Feature(Feature.Range2)]*/yPosition));
                    node = tree.nodes[node].left;
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
                currentYStart = nextYStart;

                nextNode = tree.Nil;
                nextXStart = tree.xExtent;
                nextYStart = tree.yExtent;

                if (stack.Count == 0)
                {
                    nextXStart = tree.xExtent;
                    nextYStart = tree.yExtent;
                    return;
                }

                STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;
                nextXStart = cursor.Item2;
                nextYStart = cursor.Item3;

                PushSuccessor(
                    tree.nodes[nextNode].right,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart,
                    /*[Feature(Feature.Range2)]*/nextYStart);
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new SplayTree<KeyType, ValueType>(this);
        }
    }
}
