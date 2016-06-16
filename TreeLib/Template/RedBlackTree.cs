// IMPORTANT: The TreeLib package is licensed under GNU Lesser GPL. However, this file is based on
// code that was licensed under the MIT license. Therefore, at your option, you may apply
// the MIT license to THIS FILE and it's automatically-generated derivatives only.

// adapted from .NET CoreFX BCL: https://github.com/dotnet/corefx/blob/master/src/System.Collections/src/System/Collections/Generic/SortedSet.cs

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
// Based on the .NET Framework Base Class Libarary implementation of red-black trees from here:
// https://github.com/dotnet/corefx/blob/master/src/System.Collections/src/System/Collections/Generic/SortedSet.cs
//
// An overview of red-black trees can be found here: https://en.wikipedia.org/wiki/Red%E2%80%93black_tree
//

namespace TreeLib
{
    // Placeholder, used temporarily to facilitate code transforms more easily, then references
    // renamed to shared enumeration entry.
    public struct RedBlackTreeEntry<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType>
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

        public RedBlackTreeEntry(
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
    /// Implements a map, list or range collection using a red-black tree. 
    /// </summary>
    public class RedBlackTree<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType> :

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

        IEnumerable<RedBlackTreeEntry<KeyType, ValueType>>,
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

            public bool isRed;

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

            //public override string ToString()
            //{
            //    return (left == null) && (right == null)
            //        ? "Nil"
            //        : String.Format("({0})*{2}={3}*({1})", left.node.left == null ? "Nil" : left.node.key.ToString(), right.node.left == null ? "Nil" : right.node.key.ToString(), key, value);
            //}
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

        [Storage(Storage.Object)]
        private readonly static NodeRef _Null = new NodeRef(null);

        //
        // Array form data structure
        //

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

            public bool isRed;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;
            [Feature(Feature.Range2)]
            [Widen]
            public int yOffset;

            //public override string ToString()
            //{
            //    return String.Format("({0})*{2}={3}*({1})", left == Null ? "null" : left.ToString(), right == Null ? "null" : right.ToString(), key, value);
            //}
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
                return node != _Null ? node.ToString() : "null";
            }
        }

        [Storage(Storage.Array)]
        private readonly static NodeRef _Null = new NodeRef(unchecked((uint)-1));

        [Storage(Storage.Array)]
        private const int ReservedElements = 0;
        [Storage(Storage.Array)]
        private Node[] nodes;

        //
        // State for both array & object form
        //

        private NodeRef Null { get { return RedBlackTree<KeyType, ValueType>._Null; } } // allow tree.Null or this.Null in all cases

        private NodeRef root;
        [Count]
        private ulong count;
        private ushort version;

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


        //
        // Construction
        //

        // Object

        /// <summary>
        /// Create a new collection based on a red-black tree, explicitly configured.
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
        public RedBlackTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer, uint capacity, AllocationMode allocationMode)
        {
            this.comparer = comparer;
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on a red-black tree, with the specified capacity and allocation mode and using
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
        public RedBlackTree(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection based on a red-black tree, with default allocation options and using the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public RedBlackTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a red-black tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public RedBlackTree()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a red-blacck tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public RedBlackTree(RedBlackTree<KeyType, ValueType> original)
        {
            this.comparer = original.comparer;

            this.count = original.count;
            this.xExtent = original.xExtent;
            this.yExtent = original.yExtent;

            this.allocationMode = original.allocationMode;
            this.freelist = this.Null;
            {
                NodeRef nodeOriginal = original.freelist;
                while (nodeOriginal != original.Null)
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

            this.root = this.Null;
            if (original.root != original.Null)
            {
                Stack<STuple<NodeRef, NodeRef>> stack = new Stack<STuple<NodeRef, NodeRef>>();

                NodeRef nodeOriginal = original.root;
                NodeRef nodeThis = this.root;
                while (nodeOriginal != original.Null)
                {
                    NodeRef nodeChild = new NodeRef(new Node());
                    this.nodes[nodeChild].left = this.Null;
                    this.nodes[nodeChild].right = this.Null;
                    if (this.root == this.Null)
                    {
                        this.root = nodeChild;
                    }
                    else
                    {
                        this.nodes[nodeThis].left = nodeChild;
                    }
                    nodeThis = nodeChild;
                    stack.Push(new STuple<NodeRef, NodeRef>(nodeThis, nodeOriginal));
                    nodeOriginal = original.nodes[nodeOriginal].left;
                }
                while (stack.Count != 0)
                {
                    STuple<NodeRef, NodeRef> t = stack.Pop();
                    nodeThis = t.Item1;
                    nodeOriginal = t.Item2;

                    this.nodes[nodeThis].key = original.nodes[nodeOriginal].key;
                    this.nodes[nodeThis].value = original.nodes[nodeOriginal].value;
                    this.nodes[nodeThis].xOffset = original.nodes[nodeOriginal].xOffset;
                    this.nodes[nodeThis].yOffset = original.nodes[nodeOriginal].yOffset;
                    this.nodes[nodeThis].isRed = original.nodes[nodeOriginal].isRed;

                    if (original.nodes[nodeOriginal].right != original.Null)
                    {
                        bool first = true;
                        nodeOriginal = original.nodes[nodeOriginal].right;
                        while (nodeOriginal != original.Null)
                        {
                            NodeRef nodeChild = new NodeRef(new Node());
                            this.nodes[nodeChild].left = this.Null;
                            this.nodes[nodeChild].right = this.Null;
                            if (first)
                            {
                                first = false;
                                this.nodes[nodeThis].right = nodeChild;
                            }
                            else
                            {
                                this.nodes[nodeThis].left = nodeChild;
                            }
                            nodeThis = nodeChild;
                            stack.Push(new STuple<NodeRef, NodeRef>(nodeThis, nodeOriginal));
                            nodeOriginal = original.nodes[nodeOriginal].left;
                        }
                    }
                }
            }
        }

        // Array

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a red-black tree, explicitly configured.
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
        public RedBlackTree([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer, uint capacity, AllocationMode allocationMode)
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
        /// Create a new collection using an array storage mechanism, based on a red-black tree, with the specified capacity and allocation mode and using
        /// the default comparer (applicable only for keyed collections). The allocation mode is PreallocatedFixed.
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
        public RedBlackTree(uint capacity, AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a red-black, with the specified capacity and using
        /// the default comparer (applicable only for keyed collections). The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity of the tree, the memory for which is preallocated at construction time;
        /// if the capacity is exceeded, the internal array will be resized to make more nodes available.
        /// </param>
        [Storage(Storage.Array)]
        public RedBlackTree(uint capacity)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a red-black tree, using
        /// the specified comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public RedBlackTree(IComparer<KeyType> comparer)
            : this(comparer, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on a red-black tree, using
        /// the default comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        [Storage(Storage.Array)]
        public RedBlackTree()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection based on a red-black tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Array)]
        public RedBlackTree(RedBlackTree<KeyType, ValueType> original)
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
                // non-recusrive depth-first traversal (in-order, but doesn't matter here)

                Stack<NodeRef> stack = new Stack<NodeRef>();

                NodeRef node = root;
                while (node != Null)
                {
                    stack.Push(node);
                    node = nodes[node].left;
                }
                while (stack.Count != 0)
                {
                    node = stack.Pop();

                    NodeRef dead = node;

                    node = nodes[node].right;
                    while (node != Null)
                    {
                        stack.Push(node);
                        node = nodes[node].left;
                    }

                    this.count = unchecked(this.count - 1);
                    Free(dead);
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

            root = Null;
            this.count = 0;
            this.xExtent = 0;
            this.yExtent = 0;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            return FindNode(key) != Null;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict)]
        public bool SetOrAddValue(KeyType key, ValueType value)
        {
            return InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                true/*add*/,
                true/*update*/);
        }

        [Feature(Feature.Dict)]
        public bool TryAdd(KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            return InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                true/*add*/,
                false/*update*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return DeleteInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key);
        }

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key, out KeyType keyOut)
        {
            NodeRef node = FindNode(key);
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
            return InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/default(ValueType),
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                false/*add*/,
                true/*update*/);
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetValue(KeyType key, out ValueType value)
        {
            NodeRef node = FindNode(key);
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
            return InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                false/*add*/,
                true/*update*/);
        }

        [Feature(Feature.Dict)]
        public void Add(KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            if (!InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/0,
                true/*add*/,
                false/*update*/))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void Remove(KeyType key)
        {
            if (!DeleteInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key))
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
            NodeRef node = root;
            if (node == Null)
            {
                keyOut = default(KeyType);
                valueOut = default(ValueType);
                return false;
            }
            while (nodes[node].left != Null)
            {
                node = nodes[node].left;
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
            NodeRef node = root;
            if (node == Null)
            {
                keyOut = default(KeyType);
                valueOut = default(ValueType);
                return false;
            }
            while (nodes[node].right != Null)
            {
                node = nodes[node].right;
            }
            keyOut = nodes[node].key;
            valueOut = nodes[node].value;
            return true;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut, [Payload(Payload.Value)] out ValueType value)
        {
            return GreatestInternal(out keyOut, /*[Payload(Payload.Value)]*/out value);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut)
        {
            ValueType value;
            return GreatestInternal(out keyOut, /*[Payload(Payload.Value)]*/out value);
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType value)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            value = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
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
        public bool NearestLess(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType value)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            value = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
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
        public bool NearestGreaterOrEqual(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType value)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            value = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
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
        public bool NearestGreater(KeyType key, out KeyType nearestKey, [Payload(Payload.Value)] out ValueType value)
        {
            /*[Widen]*/
            int nearestStart;
            NodeRef nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            value = nearestNode != Null ? nodes[nearestNode].value : default(ValueType);
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
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
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                /*[Payload(Payload.None)]*/
                KeyType duplicateKey;
                bool g = TryGet(nearestKey, /*[Payload(Payload.None)]*/out duplicateKey, /*[Payload(Payload.Value)]*/out valueOut, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount);
                Debug.Assert(g && (0 == comparer.Compare(nearestKey, duplicateKey)));
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

            return InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                /*[Payload(Payload.Value)]*/value,
                start,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature()]*/CompareKeyMode.Position,
                xLength,
                /*[Feature(Feature.Range2)]*/yLength,
                true/*add*/,
                false/*update*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return DeleteInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                start,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature()]*/CompareKeyMode.Position);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out int length)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            /*[Widen]*/
            int yPosition, yLength;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
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
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
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
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
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
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                nodes[node].value = value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Feature(Feature.Range2)][Widen] out int otherStart, [Widen] out int xLength, [Feature(Feature.Range2)][Widen] out int yLength, [Payload(Payload.Value)] out ValueType value)
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
        public void Insert([Widen] int start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xLength, [Feature(Feature.Range2)][Widen] int yLength, [Payload(Payload.Value)] ValueType value)
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
            if (!TryGetLength(start, /*[Feature(Feature.Range2)]*/side, out length))
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
        public void AdjustLength([Widen] int startIndex, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] int xAdjust, [Feature(Feature.Range2)] [Widen] int yAdjust)
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

                    ShiftRightOfPath(unchecked(startIndex + 1), /*[Feature(Feature.Range2)]*/side, xAdjust, /*[Feature(Feature.Range2)]*/yAdjust);
                }
                else
                {
                    // delete

                    Debug.Assert(xAdjust < 0);
                    Debug.Assert(yAdjust < 0);
                    Debug.Assert(newXLength == 0);
                    /*[Feature(Feature.Range2)]*/
                    Debug.Assert(newYLength == 0);

                    DeleteInternal(
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                        startIndex,
                        /*[Feature(Feature.Range2)]*/side,
                        /*[Feature()]*/CompareKeyMode.Position);
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

            return InsertUpdateInternal(
                key,
                /*[Payload(Payload.Value)]*/value,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                /*[Feature(Feature.Range2)]*/(Side)0,
                /*[Feature()]*/CompareKeyMode.Key,
                rankCount,
                /*[Feature(Feature.Range2)]*/0,
                true/*add*/,
                false/*update*/);
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
            if (FindPosition(rank, /*[Feature(Feature.Range2)]*/Side.X, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, /*[Feature(Feature.RankMulti)]*/out xLength, /*[Feature(Feature.Range2)]*/out yLength))
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
        public void AdjustCount(KeyType key, [Widen] int countAdjust)
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

                        ShiftRightOfPath(xPosition + 1, /*[Feature(Feature.Range2)]*/Side.X, countAdjust, /*[Feature(Feature.Range2)]*/0);
                    }
                    else if (xLength + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        DeleteInternal(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0,
                            /*[Feature(Feature.Range2)]*/(Side)0,
                            /*[Feature()]*/CompareKeyMode.Key);
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
        private NodeRef Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key, [Payload(Payload.Value)] ValueType value, bool isRed)
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
                allocateHelper.allocateCount = checked(allocateHelper.allocateCount + 1);
#endif
            }

            nodes[node].key = key;
            nodes[node].value = value;
            nodes[node].left = Null;
            nodes[node].right = Null;
            nodes[node].isRed = isRed;
            nodes[node].xOffset = 0;
            nodes[node].yOffset = 0;

            return node;
        }

        [Storage(Storage.Object)]
        private NodeRef Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            return Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value, true/*isRed*/);
        }

        [Storage(Storage.Object)]
        private void Free(NodeRef node)
        {
#if DEBUG
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == this.count);

            nodes[node].left = Null;
            nodes[node].right = Null;
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
        private NodeRef Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key, [Payload(Payload.Value)] ValueType value, bool isRed)
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
            nodes[node].isRed = isRed;
            nodes[node].left = Null;
            nodes[node].right = Null;
            nodes[node].xOffset = 0;
            nodes[node].yOffset = 0;

            return node;
        }

        [Storage(Storage.Array)]
        private NodeRef Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key, [Payload(Payload.Value)] ValueType value)
        {
            return Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value, true/*isRed*/);
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
                Debug.Assert(freelist == Null);
                Debug.Assert((nodes == null) || (nodes.Length >= ReservedElements));

                uint oldLength = nodes != null ? unchecked((uint)nodes.Length) : ReservedElements;
                uint newLength = checked(capacity + ReservedElements);

                Array.Resize(ref nodes, unchecked((int)newLength));

                for (long i = (long)newLength - 1; i >= oldLength; i--)
                {
                    Free(new NodeRef(unchecked((uint)i)));
                }
            }
        }


        private bool NearestLess(
            out NodeRef nearestNode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,
            bool orEqual)
        {
            NodeRef lastLess = Null;
            /*[Widen]*/
            int xPositionLastLess = 0;
            /*[Widen]*/
            int yPositionLastLess = 0;
            NodeRef node = root;
            if (node != Null)
            {
                /*[Widen]*/
                int xPosition = 0;
                /*[Widen]*/
                int yPosition = 0;
                while (true)
                {
                    unchecked
                    {
                        xPosition += nodes[node].xOffset;
                        yPosition += nodes[node].yOffset;
                    }

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
                        next = nodes[node].left;
                    }
                    else
                    {
                        lastLess = node;
                        xPositionLastLess = xPosition;
                        yPositionLastLess = yPosition;
                        next = nodes[node].right;
                    }
                    if (next == Null)
                    {
                        break;
                    }
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

        private bool NearestGreater(
            out NodeRef nearestNode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,
            bool orEqual)
        {
            NodeRef lastGreater = Null;
            /*[Widen]*/
            int xPositionLastGreater = 0;
            /*[Widen]*/
            int yPositionLastGreater = 0;
            NodeRef node = root;
            if (node != Null)
            {
                /*[Widen]*/
                int xPosition = 0;
                /*[Widen]*/
                int yPosition = 0;
                while (true)
                {
                    unchecked
                    {
                        xPosition += nodes[node].xOffset;
                        yPosition += nodes[node].yOffset;
                    }

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
                        next = nodes[node].left;
                    }
                    else
                    {
                        next = nodes[node].right;
                    }
                    if (next == Null)
                    {
                        break;
                    }
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

        // Searches tree for key location.
        // If key is not present and add==true, node is inserted.
        // If key is preset and update==true, value is replaced.
        // Returns true if a node was added or if add==false and a node was updated.
        // NOTE: update mode does *not* adjust for xLength/yLength!
        private bool InsertUpdateInternal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Payload(Payload.Value)] ValueType value,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature()] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank, Feature.RankMulti)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] CompareKeyMode mode,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,
            [Feature(Feature.Range2)][Widen] int yLength,
            bool add,
            bool update)
        {
            Debug.Assert((mode == CompareKeyMode.Key) || (add != update));

            if (root == Null)
            {
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

                root = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value, false);
                Debug.Assert(nodes[root].xOffset == 0);
                Debug.Assert(nodes[root].yOffset == 0);
                Debug.Assert(this.xExtent == 0);
                Debug.Assert(this.yExtent == 0);
                this.xExtent = xLength;
                this.yExtent = yLength;

                Debug.Assert(this.count == 0);
                this.count = 1;
                this.version = unchecked((ushort)(this.version + 1));

                return true;
            }

            // Search for a node at bottom to insert the new node. 
            // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            NodeRef current = root;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            int xPositionCurrent = 0;
            /*[Feature(Feature.Range2)]*/
            /*[Widen]*/
            int yPositionCurrent = 0;
            NodeRef parent = Null;
            NodeRef grandParent = Null;
            NodeRef greatGrandParent = Null;

            NodeRef successor = Null;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            int xPositionSuccessor = 0;
            /*[Feature(Feature.Range2)]*/
            /*[Widen]*/
            int yPositionSuccessor = 0;

            //even if we don't actually add to the set, we may be altering its structure (by doing rotations
            //and such). so update version to disable any enumerators/subsets working on it
            this.version = unchecked((ushort)(this.version + 1));

            int order = 0;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            int xPositionParent = 0;
            /*[Feature(Feature.Range2)]*/
            /*[Widen]*/
            int yPositionParent = 0;
            while (current != Null)
            {
                unchecked
                {
                    xPositionCurrent += nodes[current].xOffset;
                    yPositionCurrent += nodes[current].yOffset;
                }

                if (mode == CompareKeyMode.Key)
                {
                    order = comparer.Compare(key, nodes[current].key);
                }
                else
                {
                    Debug.Assert(mode == CompareKeyMode.Position);
                    order = position.CompareTo(side == Side.X ? xPositionCurrent : yPositionCurrent);
                    if (add && (order == 0))
                    {
                        order = -1; // node never found for sparse range mode
                    }
                }

                if (order == 0)
                {
                    // We could have changed root node to red during the search process.
                    // We need to set it to black before we return.
                    nodes[root].isRed = false;
                    if (update)
                    {
                        nodes[current].key = key;
                        nodes[current].value = value;
                    }
                    return !add;
                }

                // split a 4-node into two 2-nodes                
                if (Is4Node(current))
                {
                    Split4Node(current);
                    // We could have introduced two consecutive red nodes after split. Fix that by rotation.
                    if (IsRed(parent))
                    {
                        InsertionBalance(current, ref parent, grandParent, greatGrandParent);
                    }
                }
                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current;
                xPositionParent = xPositionCurrent;
                yPositionParent = yPositionCurrent;
                if (order < 0)
                {
                    successor = parent;
                    xPositionSuccessor = xPositionParent;
                    yPositionSuccessor = yPositionParent;

                    current = nodes[current].left;
                }
                else
                {
                    current = nodes[current].right;
                }
            }
            Debug.Assert(current == Null);

            Debug.Assert(parent != Null, "Parent node cannot be null here!");
            // ready to insert the new node
            if (!add)
            {
                nodes[root].isRed = false;
                return false;
            }

            NodeRef node;
            if (order > 0)
            {
                // follows parent

                Debug.Assert(nodes[parent].right == Null);
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xLengthParent;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yLengthParent;
                if (successor != Null)
                {
                    xLengthParent = xPositionSuccessor - xPositionParent;
                    yLengthParent = yPositionSuccessor - yPositionParent;
                }
                else
                {
                    xLengthParent = this.xExtent - xPositionParent;
                    yLengthParent = this.yExtent - yPositionParent;
                }

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                if ((mode == CompareKeyMode.Position)
                    && (position != unchecked(side == Side.X ? xPositionParent + xLengthParent : yPositionParent + yLengthParent)))
                {
                    nodes[root].isRed = false;
                    return false;
                }

                // compute here to throw before modifying tree
                /*[Widen]*/
                int xExtentNew, yExtentNew;
                /*[Count]*/
                ulong countNew;
                try
                {
                    xExtentNew = checked(this.xExtent + xLength);
                    yExtentNew = checked(this.yExtent + yLength);
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    nodes[root].isRed = false;
                    throw;
                }

                node = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value);

                ShiftRightOfPath(unchecked(xPositionParent + 1), /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                nodes[parent].right = node;

                nodes[node].xOffset = xLengthParent;
                nodes[node].yOffset = yLengthParent;

                this.xExtent = xExtentNew;
                this.yExtent = yExtentNew;
                this.count = countNew;
            }
            else
            {
                // precedes parent

                if (mode == CompareKeyMode.Position)
                {
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int positionParent = side == Side.X ? xPositionParent : yPositionParent;
                    if (position != positionParent)
                    {
                        nodes[root].isRed = false;
                        return false;
                    }
                }

                // compute here to throw before modifying tree
                /*[Widen]*/
                int xExtentNew, yExtentNew;
                /*[Count]*/
                ulong countNew;
                try
                {
                    xExtentNew = checked(this.xExtent + xLength);
                    yExtentNew = checked(this.yExtent + yLength);
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    nodes[root].isRed = false;
                    throw;
                }

                Debug.Assert(parent == successor);

                node = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, /*[Payload(Payload.Value)]*/value);

                ShiftRightOfPath(xPositionParent, /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                nodes[parent].left = node;

                nodes[node].xOffset = -xLength;
                nodes[node].yOffset = -yLength;

                this.xExtent = xExtentNew;
                this.yExtent = yExtentNew;
                this.count = countNew;
            }

            // the new node will be red, so we will need to adjust the colors if parent node is also red
            if (nodes[parent].isRed)
            {
                InsertionBalance(node, ref parent, grandParent, greatGrandParent);
            }

            // Root node is always black
            nodes[root].isRed = false;

            return true;
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
                /*[Widen]*/
                int xPositionCurrent = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionCurrent = 0;
                NodeRef current = root;
                while (current != Null)
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
                        if (nodes[current].left != Null)
                        {
                            nodes[nodes[current].left].xOffset -= xAdjust;
                            nodes[nodes[current].left].yOffset -= yAdjust;
                        }

                        if (order == 0)
                        {
                            break;
                        }

                        current = nodes[current].left;
                    }
                    else
                    {
                        current = nodes[current].right;
                    }
                }
            }
        }

        private bool DeleteInternal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature()] [Const(CompareKeyMode.Key, Feature.Dict, Feature.Rank, Feature.RankMulti)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] CompareKeyMode mode)
        {
            unchecked
            {
                if (root == Null)
                {
                    return false;
                }

                // Search for a node and then find its successor. 
                // Then copy the item from the successor to the matching node and delete the successor. 
                // If a node doesn't have a successor, we can replace it with its left child (if not empty.) 
                // or delete the matching node.
                // 
                // In top-down implementation, it is important to make sure the node to be deleted is not a 2-node.
                // Following code will make sure the node on the path is not a 2 Node. 

                //even if we don't actually remove from the set, we may be altering its structure (by doing rotations
                //and such). so update version to disable any enumerators/subsets working on it
                this.version = unchecked((ushort)(this.version + 1));

                NodeRef current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionCurrent = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionCurrent = 0;

                NodeRef parent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParent = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionParent = 0;

                NodeRef grandParent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionGrandparent = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionGrandparent = 0;

                NodeRef match = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionMatch = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionMatch = 0;

                NodeRef parentOfMatch = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParentOfMatch = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionParentOfMatch = 0;

                bool foundMatch = false;

                NodeRef lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionLastGreaterAncestor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionLastGreaterAncestor = 0;
                while (current != Null)
                {
                    xPositionCurrent += nodes[current].xOffset;
                    yPositionCurrent += nodes[current].yOffset;

                    if (Is2Node(current))
                    {
                        // fix up 2-Node
                        if (parent == Null)
                        {
                            // current is root. Mark it as red
                            nodes[current].isRed = true;
                        }
                        else
                        {
                            NodeRef sibling = GetSibling(current, parent);
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            /*[Widen]*/
                            int xPositionSibling = xPositionParent + nodes[sibling].xOffset;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            int yPositionSibling = yPositionParent + nodes[sibling].yOffset;
                            if (nodes[sibling].isRed)
                            {
                                // If parent is a 3-node, flip the orientation of the red link. 
                                // We can achieve this by a single rotation        
                                // This case is converted to one of other cases below.
                                Debug.Assert(!nodes[parent].isRed, "parent must be a black node!");
                                NodeRef newTop;
                                if (nodes[parent].right == sibling)
                                {
                                    newTop = RotateLeft(parent);
                                }
                                else
                                {
                                    newTop = RotateRight(parent);
                                }
                                Debug.Assert(newTop == sibling);

                                nodes[parent].isRed = true;
                                nodes[sibling].isRed = false;    // parent's color
                                                                 // sibling becomes child of grandParent or root after rotation. Update link from grandParent or root
                                ReplaceChildOfNodeOrRoot(grandParent, parent, sibling);
                                // sibling will become grandParent of current node 
                                grandParent = sibling;
                                xPositionGrandparent = xPositionSibling;
                                yPositionGrandparent = yPositionSibling;
                                if (parent == match)
                                {
                                    parentOfMatch = sibling;
                                    xPositionParentOfMatch = xPositionSibling;
                                    yPositionParentOfMatch = yPositionSibling;
                                }

                                // update sibling, this is necessary for following processing
                                sibling = (nodes[parent].left == current) ? nodes[parent].right : nodes[parent].left;
                                xPositionSibling += nodes[sibling].xOffset;
                                yPositionSibling += nodes[sibling].yOffset;
                            }
                            Debug.Assert(sibling != Null || nodes[sibling].isRed == false, "sibling must not be null and it must be black!");

                            if (Is2Node(sibling))
                            {
                                Merge2Nodes(parent, current, sibling);
                            }
                            else
                            {
                                // current is a 2-node and sibling is either a 3-node or a 4-node.
                                // We can change the color of current to red by some rotation.
                                TreeRotation rotation = RotationNeeded(parent, current, sibling);
                                NodeRef newGrandParent = Null;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                /*[Widen]*/
                                int xPositionNewGrandparent = xPositionParent - nodes[parent].xOffset;
                                /*[Feature(Feature.Range2)]*/
                                /*[Widen]*/
                                int yPositionNewGrandparent = yPositionParent - nodes[parent].yOffset;
                                switch (rotation)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();

                                    case TreeRotation.RightRotation:
                                        Debug.Assert(nodes[parent].left == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(nodes[nodes[sibling].left].isRed, "Left child of sibling must be red!");
                                        nodes[nodes[sibling].left].isRed = false;
                                        newGrandParent = RotateRight(parent);
                                        break;
                                    case TreeRotation.LeftRotation:
                                        Debug.Assert(nodes[parent].right == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(nodes[nodes[sibling].right].isRed, "Right child of sibling must be red!");
                                        nodes[nodes[sibling].right].isRed = false;
                                        newGrandParent = RotateLeft(parent);
                                        break;

                                    case TreeRotation.RightLeftRotation:
                                        Debug.Assert(nodes[parent].right == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(nodes[nodes[sibling].left].isRed, "Left child of sibling must be red!");
                                        newGrandParent = RotateRightLeft(parent);
                                        break;

                                    case TreeRotation.LeftRightRotation:
                                        Debug.Assert(nodes[parent].left == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(nodes[nodes[sibling].right].isRed, "Right child of sibling must be red!");
                                        newGrandParent = RotateLeftRight(parent);
                                        break;
                                }
                                xPositionNewGrandparent += nodes[newGrandParent].xOffset;
                                yPositionNewGrandparent += nodes[newGrandParent].yOffset;

                                nodes[newGrandParent].isRed = nodes[parent].isRed;
                                nodes[parent].isRed = false;
                                nodes[current].isRed = true;
                                ReplaceChildOfNodeOrRoot(grandParent, parent, newGrandParent);
                                if (parent == match)
                                {
                                    parentOfMatch = newGrandParent;
                                    xPositionParentOfMatch = xPositionNewGrandparent;
                                    yPositionParentOfMatch = yPositionNewGrandparent;
                                }
                                grandParent = newGrandParent;
                                xPositionGrandparent = xPositionNewGrandparent;
                                yPositionGrandparent = yPositionNewGrandparent;
                            }
                        }
                    }

                    int order;
                    if (foundMatch)
                    {
                        order = -1; // we don't need to compare any more once we found the match
                    }
                    else
                    {
                        if (mode == CompareKeyMode.Key)
                        {
                            order = comparer.Compare(key, nodes[current].key);
                        }
                        else
                        {
                            Debug.Assert(mode == CompareKeyMode.Position);
                            order = position.CompareTo(side == Side.X ? xPositionCurrent : yPositionCurrent);
                        }
                    }

                    if (order == 0)
                    {
                        // save the matching node
                        foundMatch = true;
                        match = current;
                        parentOfMatch = parent;

                        xPositionMatch = xPositionCurrent;
                        yPositionMatch = yPositionCurrent;
                        xPositionParentOfMatch = xPositionParent;
                        yPositionParentOfMatch = yPositionParent;
                    }

                    grandParent = parent;
                    parent = current;

                    xPositionGrandparent = xPositionParent;
                    yPositionGrandparent = yPositionParent;
                    xPositionParent = xPositionCurrent;
                    yPositionParent = yPositionCurrent;

                    if (order < 0)
                    {
                        if (!foundMatch)
                        {
                            lastGreaterAncestor = current;
                            xPositionLastGreaterAncestor = xPositionCurrent;
                            yPositionLastGreaterAncestor = yPositionCurrent;
                        }

                        current = nodes[current].left;
                    }
                    else
                    {
                        current = nodes[current].right; // continue the search in right sub tree after we find a match (to find successor)
                    }
                }

                // move successor to the matching node position and replace links
                if (match != Null)
                {
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    int xLength;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yLength;
                    Debug.Assert(parent != Null);
                    if (parent != match)
                    {
                        xLength = xPositionParent - xPositionMatch;
                        yLength = yPositionParent - yPositionMatch;
                    }
                    else if (lastGreaterAncestor != Null)
                    {
                        xLength = xPositionLastGreaterAncestor - xPositionMatch;
                        yLength = yPositionLastGreaterAncestor - yPositionMatch;
                    }
                    else
                    {
                        xLength = this.xExtent - xPositionMatch;
                        yLength = this.yExtent - yPositionMatch;
                    }

                    ReplaceNode(
                        match,
                        parentOfMatch,
                        parent/*successor*/,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPositionParent - xPositionMatch,
                        /*[Feature(Feature.Range2)]*/yPositionParent - yPositionMatch,
                        grandParent/*parentOfSuccessor*/);

                    ShiftRightOfPath(xPositionMatch + 1, /*[Feature(Feature.Range2)]*/Side.X, -xLength, /*[Feature(Feature.Range2)]*/-yLength);

                    this.xExtent = unchecked(this.xExtent - xLength);
                    this.yExtent = unchecked(this.yExtent - yLength);
                    this.count = unchecked(this.count - 1);

                    Free(match);
                }

                if (root != Null)
                {
                    nodes[root].isRed = false;
                }
                return foundMatch;
            }
        }

        // Replace the matching node with its successor.
        private void ReplaceNode(
            NodeRef match,
            NodeRef parentOfMatch,
            NodeRef successor,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xOffsetMatchSuccessor,
            [Feature(Feature.Range2)][Widen] int yOffsetMatchSuccessor,
            NodeRef parentOfsuccessor)
        {
            unchecked
            {
                if (successor == match)
                {
                    // this node has no successor, should only happen if right child of matching node is null.
                    Debug.Assert(nodes[match].right == Null, "Right child must be null!");
                    successor = nodes[match].left;

                    if (successor != Null)
                    {
                        xOffsetMatchSuccessor = nodes[successor].xOffset;
                        yOffsetMatchSuccessor = nodes[successor].yOffset;
                    }
                }
                else
                {
                    Debug.Assert(parentOfsuccessor != Null, "parent of successor cannot be null!");
                    Debug.Assert(nodes[successor].left == Null, "Left child of successor must be null!");
                    Debug.Assert((nodes[successor].right == Null && nodes[successor].isRed)
                        || (nodes[nodes[successor].right].isRed && !nodes[successor].isRed), "Successor must be in valid state");
                    if (nodes[successor].right != Null)
                    {
                        nodes[nodes[successor].right].isRed = false;
                    }

                    if (parentOfsuccessor != match)
                    {
                        // detach successor from its parent and set its right child
                        nodes[parentOfsuccessor].left = nodes[successor].right;
                        if (nodes[successor].right != Null)
                        {
                            nodes[nodes[successor].right].xOffset += nodes[successor].xOffset;
                            nodes[nodes[successor].right].yOffset += nodes[successor].yOffset;
                        }
                        nodes[successor].right = nodes[match].right;
                        if (nodes[match].right != Null)
                        {
                            nodes[nodes[match].right].xOffset -= xOffsetMatchSuccessor;
                            nodes[nodes[match].right].yOffset -= yOffsetMatchSuccessor;
                        }
                    }

                    nodes[successor].left = nodes[match].left;
                    if (nodes[match].left != Null)
                    {
                        nodes[nodes[match].left].xOffset -= xOffsetMatchSuccessor;
                        nodes[nodes[match].left].yOffset -= yOffsetMatchSuccessor;
                    }
                }

                if (successor != Null)
                {
                    nodes[successor].isRed = nodes[match].isRed;

                    nodes[successor].xOffset = nodes[match].xOffset + xOffsetMatchSuccessor;
                    nodes[successor].yOffset = nodes[match].yOffset + yOffsetMatchSuccessor;
                }

                ReplaceChildOfNodeOrRoot(parentOfMatch/*parent*/, match/*child*/, successor/*new child*/);
            }
        }

        // Replace the child of a parent node. 
        // If the parent node is null, replace the root.        
        private void ReplaceChildOfNodeOrRoot(NodeRef parent, NodeRef child, NodeRef newChild)
        {
            if (parent != Null)
            {
                if (nodes[parent].left == child)
                {
                    nodes[parent].left = newChild;
                }
                else
                {
                    nodes[parent].right = newChild;
                }
            }
            else
            {
                root = newChild;
            }
        }

        private NodeRef GetSibling(NodeRef node, NodeRef parent)
        {
            if (nodes[parent].left == node)
            {
                return nodes[parent].right;
            }
            return nodes[parent].left;
        }

        // After calling InsertionBalance, we need to make sure current and parent up-to-date.
        // It doesn't matter if we keep grandParent and greatGrantParent up-to-date 
        // because we won't need to split again in the next node.
        // By the time we need to split again, everything will be correctly set.
        private void InsertionBalance(NodeRef current, ref NodeRef parent, NodeRef grandParent, NodeRef greatGrandParent)
        {
            Debug.Assert(grandParent != Null, "Grand parent cannot be null here!");
            bool parentIsOnRight = (nodes[grandParent].right == parent);
            bool currentIsOnRight = (nodes[parent].right == current);

            NodeRef newChildOfGreatGrandParent;
            if (parentIsOnRight == currentIsOnRight)
            {
                // same orientation, single rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeft(grandParent) : RotateRight(grandParent);
            }
            else
            {
                // different orientation, double rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeftRight(grandParent) : RotateRightLeft(grandParent);
                // current node now becomes the child of greatgrandparent 
                parent = greatGrandParent;
            }
            // grand parent will become a child of either parent of current.
            nodes[grandParent].isRed = true;
            nodes[newChildOfGreatGrandParent].isRed = false;

            ReplaceChildOfNodeOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
        }

        private bool Is2Node(NodeRef node)
        {
            Debug.Assert(node != Null, "node cannot be null!");
            return IsBlack(node) && IsNullOrBlack(nodes[node].left) && IsNullOrBlack(nodes[node].right);
        }

        private bool Is4Node(NodeRef node)
        {
            return IsRed(nodes[node].left) && IsRed(nodes[node].right);
        }

        private bool IsBlack(NodeRef node)
        {
            return (node != Null && !nodes[node].isRed);
        }

        private bool IsNullOrBlack(NodeRef node)
        {
            return (node == Null || !nodes[node].isRed);
        }

        private bool IsRed(NodeRef node)
        {
            return (node != Null && nodes[node].isRed);
        }

        private void Merge2Nodes(NodeRef parent, NodeRef child1, NodeRef child2)
        {
            Debug.Assert(IsRed(parent), "parent must be red");
            // combing two 2-nodes into a 4-node
            nodes[parent].isRed = false;
            nodes[child1].isRed = true;
            nodes[child2].isRed = true;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private NodeRef FindNode(KeyType item)
        {
            NodeRef current = root;
            while (current != Null)
            {
                int order = comparer.Compare(item, nodes[current].key);
                if (order == 0)
                {
                    return current;
                }
                else
                {
                    current = (order < 0) ? nodes[current].left : nodes[current].right;
                }
            }

            return Null;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        private bool Find(
            KeyType key,
            out NodeRef match,
            [Widen] out int xPositionMatch,
            [Feature(Feature.Range2)][Widen] out int yPositionMatch,
            [Feature(Feature.RankMulti)][Widen] out int xLengthMatch,
            [Feature(Feature.Range2)][Widen] out int yLengthMatch)
        {
            unchecked
            {
                match = Null;
                NodeRef successor = Null;
                xPositionMatch = 0;
                yPositionMatch = 0;
                xLengthMatch = 0;
                yLengthMatch = 0;

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionSuccessor = 0;
                NodeRef current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionCurrent = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionCurrent = 0;
                NodeRef lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionLastGreaterAncestor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionLastGreaterAncestor = 0;
                while (current != Null)
                {
                    xPositionCurrent += nodes[current].xOffset;
                    yPositionCurrent += nodes[current].yOffset;

                    int order = (match != Null) ? -1 : comparer.Compare(key, nodes[current].key);

                    if (order == 0)
                    {
                        xPositionMatch = xPositionCurrent;
                        yPositionMatch = yPositionCurrent;
                        match = current;
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
                        current = nodes[current].left;
                    }
                    else
                    {
                        current = nodes[current].right; // continue the search in right sub tree after we find a match
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
                NodeRef successor = Null;
                xLength = 0;
                yLength = 0;

                NodeRef current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionCurrent = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionCurrent = 0;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yPositionSuccessor = 0;
                while (current != Null)
                {
                    xPositionCurrent += nodes[current].xOffset;
                    yPositionCurrent += nodes[current].yOffset;

                    if (position < (side == Side.X ? xPositionCurrent : yPositionCurrent))
                    {
                        successor = current;
                        xPositionSuccessor = xPositionCurrent;
                        yPositionSuccessor = yPositionCurrent;

                        current = nodes[current].left;
                    }
                    else
                    {
                        lastLessEqual = current;
                        xPositionLastLessEqual = xPositionCurrent;
                        yPositionLastLessEqual = yPositionCurrent;

                        current = nodes[current].right; // try to find successor
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

        private NodeRef RotateLeft(NodeRef node)
        {
            unchecked
            {
                NodeRef r = nodes[node].right;

                if (nodes[r].left != Null)
                {
                    nodes[nodes[r].left].xOffset += nodes[r].xOffset;
                    nodes[nodes[r].left].yOffset += nodes[r].yOffset;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetR = nodes[r].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetR = nodes[r].yOffset;
                nodes[r].xOffset += nodes[node].xOffset;
                nodes[r].yOffset += nodes[node].yOffset;
                nodes[node].xOffset = -xOffsetR;
                nodes[node].yOffset = -yOffsetR;

                nodes[node].right = nodes[r].left;
                nodes[r].left = node;

                return r;
            }
        }

        private NodeRef RotateLeftRight(NodeRef node)
        {
            unchecked
            {
                NodeRef lChild = nodes[node].left;
                NodeRef lrGrandChild = nodes[lChild].right;

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetNode = nodes[node].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetNode = nodes[node].yOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetLChild = nodes[lChild].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetLChild = nodes[lChild].yOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetLRGrandchild = nodes[lrGrandChild].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetLRGrandchild = nodes[lrGrandChild].yOffset;

                nodes[lrGrandChild].xOffset = xOffsetLRGrandchild + xOffsetLChild + xOffsetNode;
                nodes[lrGrandChild].yOffset = yOffsetLRGrandchild + yOffsetLChild + yOffsetNode;
                nodes[lChild].xOffset = -xOffsetLRGrandchild;
                nodes[lChild].yOffset = -yOffsetLRGrandchild;
                nodes[node].xOffset = -xOffsetLRGrandchild - xOffsetLChild;
                nodes[node].yOffset = -yOffsetLRGrandchild - yOffsetLChild;
                if (nodes[lrGrandChild].left != Null)
                {
                    nodes[nodes[lrGrandChild].left].xOffset += xOffsetLRGrandchild;
                    nodes[nodes[lrGrandChild].left].yOffset += yOffsetLRGrandchild;
                }
                if (nodes[lrGrandChild].right != Null)
                {
                    nodes[nodes[lrGrandChild].right].xOffset += xOffsetLRGrandchild + xOffsetLChild;
                    nodes[nodes[lrGrandChild].right].yOffset += yOffsetLRGrandchild + yOffsetLChild;
                }

                nodes[node].left = nodes[lrGrandChild].right;
                nodes[lrGrandChild].right = node;
                nodes[lChild].right = nodes[lrGrandChild].left;
                nodes[lrGrandChild].left = lChild;

                return lrGrandChild;
            }
        }

        private NodeRef RotateRight(NodeRef node)
        {
            unchecked
            {
                NodeRef l = nodes[node].left;

                if (nodes[l].right != Null)
                {
                    nodes[nodes[l].right].xOffset += nodes[l].xOffset;
                    nodes[nodes[l].right].yOffset += nodes[l].yOffset;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetL = nodes[l].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetL = nodes[l].yOffset;
                nodes[l].xOffset += nodes[node].xOffset;
                nodes[l].yOffset += nodes[node].yOffset;
                nodes[node].xOffset = -xOffsetL;
                nodes[node].yOffset = -yOffsetL;

                nodes[node].left = nodes[l].right;
                nodes[l].right = node;

                return l;
            }
        }

        private NodeRef RotateRightLeft(NodeRef node)
        {
            unchecked
            {
                NodeRef rChild = nodes[node].right;
                NodeRef rlGrandChild = nodes[rChild].left;

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetNode = nodes[node].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetNode = nodes[node].yOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetRChild = nodes[rChild].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetRChild = nodes[rChild].yOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetRLGrandchild = nodes[rlGrandChild].xOffset;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int yOffsetRLGrandchild = nodes[rlGrandChild].yOffset;

                nodes[rlGrandChild].xOffset = xOffsetRLGrandchild + xOffsetRChild + xOffsetNode;
                nodes[rlGrandChild].yOffset = yOffsetRLGrandchild + yOffsetRChild + yOffsetNode;
                nodes[rChild].xOffset = -xOffsetRLGrandchild;
                nodes[rChild].yOffset = -yOffsetRLGrandchild;
                nodes[node].xOffset = -xOffsetRLGrandchild - xOffsetRChild;
                nodes[node].yOffset = -yOffsetRLGrandchild - yOffsetRChild;
                if (nodes[rlGrandChild].left != Null)
                {
                    nodes[nodes[rlGrandChild].left].xOffset += xOffsetRLGrandchild + xOffsetRChild;
                    nodes[nodes[rlGrandChild].left].yOffset += yOffsetRLGrandchild + yOffsetRChild;
                }
                if (nodes[rlGrandChild].right != Null)
                {
                    nodes[nodes[rlGrandChild].right].xOffset += xOffsetRLGrandchild;
                    nodes[nodes[rlGrandChild].right].yOffset += yOffsetRLGrandchild;
                }

                nodes[node].right = nodes[rlGrandChild].left;
                nodes[rlGrandChild].left = node;
                nodes[rChild].left = nodes[rlGrandChild].right;
                nodes[rlGrandChild].right = rChild;

                return rlGrandChild;
            }
        }

        private enum TreeRotation
        {
            LeftRotation = 1,
            RightRotation = 2,
            RightLeftRotation = 3,
            LeftRightRotation = 4,
        }

        private TreeRotation RotationNeeded(NodeRef parent, NodeRef current, NodeRef sibling)
        {
            Debug.Assert(IsRed(nodes[sibling].left) || IsRed(nodes[sibling].right), "sibling must have at least one red child");
            if (IsRed(nodes[sibling].left))
            {
                if (nodes[parent].left == current)
                {
                    return TreeRotation.RightLeftRotation;
                }
                return TreeRotation.RightRotation;
            }
            else
            {
                if (nodes[parent].left == current)
                {
                    return TreeRotation.LeftRotation;
                }
                return TreeRotation.LeftRightRotation;
            }
        }

        private void Split4Node(NodeRef node)
        {
            nodes[node].isRed = true;
            nodes[nodes[node].left].isRed = false;
            nodes[nodes[node].right].isRed = false;
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
                    node = nodes[node].left;
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
                    node = nodes[node].right;
                    while (node != Null)
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
            return nodes[n].left != Null ? (object)nodes[n].left : null;
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
            return nodes[n].right != Null ? (object)nodes[n].right : null;
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
            return nodes[n].isRed ? "red" : "black";
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

                    if (nodes[node].left != Null)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        Check.Assert(comparer.Compare(nodes[nodes[node].left].key, nodes[node].key) < 0, "ordering invariant");
                        worklist.Enqueue(nodes[node].left);
                    }
                    if (nodes[node].right != Null)
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

            ValidateDepthInvariant();
        }

        private void ValidateDepthInvariant()
        {
            int min = Int32.MaxValue;
            MinDepth(root, 0, ref min);
            int depth = MaxDepth(root);
            min++;
            int max = depth + 1;

            Check.Assert((2 * min >= max) && (depth <= 2 * Math.Log(this.count + 1) / Math.Log(2)), "depth invariant");
        }

        private int MaxDepth(NodeRef root)
        {
            return (root == Null) ? 0 : (1 + Math.Max(MaxDepth(nodes[root].left), MaxDepth(nodes[root].right)));
        }

        private void MinDepth(NodeRef root, int depth, ref int min)
        {
            if (root == Null)
            {
                min = Math.Min(min, depth);
            }
            else
            {
                if (depth < min)
                {
                    MinDepth(nodes[root].left, depth + 1, ref min);
                }
                if (depth < min)
                {
                    MinDepth(nodes[root].right, depth + 1, ref min);
                }
            }
        }

        // INonInvasiveRange2MapInspection

        /// <summary>
        /// INonInvasiveRange2MapInspection.GetRanges() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        Range2MapEntry[] INonInvasiveRange2MapInspection.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntry[] ranges = new Range2MapEntry[Count];
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
                    while (node != Null)
                    {
                        xOffset += nodes[node].xOffset;
                        yOffset += nodes[node].yOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                        node = nodes[node].left;
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
        void INonInvasiveRange2MapInspection.Validate()
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
        MultiRankMapEntry[] INonInvasiveMultiRankMapInspection.GetRanks()
        {
            /*[Widen]*/
            MultiRankMapEntry[] ranks = new MultiRankMapEntry[Count];
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
                    while (node != Null)
                    {
                        xOffset += nodes[node].xOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int>(node, xOffset));
                        node = nodes[node].left;
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
        void INonInvasiveMultiRankMapInspection.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

        /// <summary>
        /// Get the default enumerator, which is the fast enumerator for red-black trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<RedBlackTreeEntry<KeyType, ValueType>> GetEnumerator()
        {
            return GetFastEnumerable().GetEnumerator();
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
        public IEnumerable<RedBlackTreeEntry<KeyType, ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<RedBlackTreeEntry<KeyType, ValueType>>
        {
            private readonly RedBlackTree<KeyType, ValueType> tree;

            public RobustEnumerableSurrogate(RedBlackTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<RedBlackTreeEntry<KeyType, ValueType>> GetEnumerator()
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
        /// deletion and will throw an InvalidOperationException when next advanced. For red-black trees, a
        /// failed insertion or deletion will still invalidate the enumerator, as failed operations may still have performed
        /// rotations in the tree. The complexity of the fast enumerator is O(1) per element, or O(N) to enumerate the
        /// entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<RedBlackTreeEntry<KeyType, ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<RedBlackTreeEntry<KeyType, ValueType>>
        {
            private readonly RedBlackTree<KeyType, ValueType> tree;

            public FastEnumerableSurrogate(RedBlackTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<RedBlackTreeEntry<KeyType, ValueType>> GetEnumerator()
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
        /// it keeps a current key and uses NearestGreater to find the next one. However, since it uses queries it
        /// is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator : IEnumerator<RedBlackTreeEntry<KeyType, ValueType>>
        {
            private readonly RedBlackTree<KeyType, ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(RedBlackTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public RedBlackTreeEntry<KeyType, ValueType> Current
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

                            return new RedBlackTreeEntry<KeyType, ValueType>(
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

                            return new RedBlackTreeEntry<KeyType, ValueType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/default(KeyType),
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength,
                                /*[Feature(Feature.Range2)]*/yStart,
                                /*[Feature(Feature.Range2)]*/yLength);
                        }
                    }
                    return new RedBlackTreeEntry<KeyType, ValueType>();
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
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        public class FastEnumerator : IEnumerator<RedBlackTreeEntry<KeyType, ValueType>>
        {
            private readonly RedBlackTree<KeyType, ValueType> tree;
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

            public FastEnumerator(RedBlackTree<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public RedBlackTreeEntry<KeyType, ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {
                        /*[Feature(Feature.Rank)]*/
                        Debug.Assert(nextXStart - currentXStart == 1);

                        return new RedBlackTreeEntry<KeyType, ValueType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/tree.nodes[currentNode].key,
                            /*[Payload(Payload.Value)]*/tree.nodes[currentNode].value,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart,
                            /*[Feature(Feature.Range2)]*/currentYStart,
                            /*[Feature(Feature.Range2)]*/nextYStart - currentYStart);
                    }
                    return new RedBlackTreeEntry<KeyType, ValueType>();
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
                stack.Clear();
                currentNode = tree.Null;
                nextNode = tree.Null;
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
                while (node != tree.Null)
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

                nextNode = tree.Null;
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
            return new RedBlackTree<KeyType, ValueType>(this);
        }
    }
}
