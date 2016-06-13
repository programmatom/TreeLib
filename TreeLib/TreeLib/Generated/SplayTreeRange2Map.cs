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
    public class SplayTreeRange2Map<[Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Range2)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRange2Map<ValueType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspection,

        IEnumerable<EntryRange2Map<ValueType>>,
        IEnumerable,

        ICloneable
    {
        //
        // Object form data structure
        //

        [Storage(Storage.Object)]
        private sealed class Node
        {
            public Node left;
            public Node right;
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
                }
                catch (NullReferenceException)
                {
                }

                string rightKeyText = null;
                try
                {
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
        [Feature(Feature.Range2)]
        [Widen]
        private int yExtent;

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
        public SplayTreeRange2Map(uint capacity,AllocationMode allocationMode)
        {
            this.root = Nil;

            this.allocationMode = allocationMode;
            this.freelist = Nil;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on a splay tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public SplayTreeRange2Map()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public SplayTreeRange2Map(SplayTreeRange2Map<ValueType> original)
        {

            this.count = original.count;
            this.xExtent = original.xExtent;
            this.yExtent = original.yExtent;

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
                this.root = new Node();

                Queue<STuple<Node, Node>> worklist = new Queue<STuple<Node, Node>>();
                worklist.Enqueue(new STuple<Node, Node>(this.root, original.root));
                while (worklist.Count != 0)
                {
                    STuple<Node, Node> item = worklist.Dequeue();

                    Node nodeThis = item.Item1;
                    Node nodeOriginal = item.Item2;
                    nodeThis.value = nodeOriginal.value;
                    nodeThis.xOffset = nodeOriginal.xOffset;
                    nodeThis.yOffset = nodeOriginal.yOffset;
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
        /// Returns the number of range pairs in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue range pairs.</exception>
        public uint Count { get { return checked((uint)this.count); } }

        
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        public long LongCount { get { return unchecked((long)this.count); } }

        
        /// <summary>
        /// Removes all range pairs from the collection.
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
                    allocateHelper.allocateCount = 0;
#endif
                }

            root = Nil;
            this.count = 0;
            this.xExtent = 0;
            this.yExtent = 0;
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        
        /// <summary>
        /// Determines if there is a range pair in the collection starting at the index specified, with respect to the side specified.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <param name="side">the side (X or Y) to which the specified index applies</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] int start,[Feature(Feature.Range2)] Side side)
        {
            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                return start == Start(root, /*[Feature(Feature.Range2)]*/side);
            }
            return false;
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] int start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] int xLength,[Feature(Feature.Range2)][Widen] int yLength,[Payload(Payload.Value)] ValueType value)
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

                    Node i = Allocate();
                    i.value = value;
                    i.xOffset = root.xOffset;
                    i.yOffset = root.yOffset;

                    i.left = root.left;
                    i.right = root;
                    if (root != Nil)
                    {
                        root.xOffset = xLength;
                        root.yOffset = yLength;
                        root.left = Nil;
                    }

                    root = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;

                    return true;
                }

                Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                int length = root.right != Nil
                    ? (side == Side.X ? root.right.xOffset : root.right.yOffset)
                    : (side == Side.X ? this.xExtent - root.xOffset : this.yExtent - root.yOffset);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side) + length)
                {
                    // append

                    Debug.Assert(root.right == Nil);

                    /*[Widen]*/
                    int xLengthRoot = this.xExtent - root.xOffset;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    int yLengthRoot = this.yExtent - root.yOffset;

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);
                    /*[Widen]*/
                    int yExtentNew = checked(this.yExtent + yLength);

                    Node i = Allocate();
                    i.value = value;
                    i.xOffset = xLengthRoot;
                    i.yOffset = yLengthRoot;

                    i.left = Nil;
                    i.right = Nil;
                    Debug.Assert(root != Nil);
                    Debug.Assert(root.right == Nil);
                    root.right = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;

                    return true;
                }

                return false;
            }
        }

        
        /// <summary>
        /// Attempt to delete the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <returns>true if a range pair was successfully deleted</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] int start,[Feature(Feature.Range2)] Side side)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                    if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                    {
                        Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        /*[Widen]*/
                        int xLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset;
                        /*[Feature(Feature.Range2)]*/
                        /*[Widen]*/
                        int yLength = root.right != Nil ? root.right.yOffset : this.yExtent - root.yOffset;

                        Node dead, x;

                        dead = root;
                        if (root.left == Nil)
                        {
                            x = root.right;
                            if (x != Nil)
                            {
                                x.xOffset += root.xOffset - xLength;
                                x.yOffset += root.yOffset - yLength;
                            }
                        }
                        else
                        {
                            x = root.left;
                            x.xOffset += root.xOffset;
                            x.yOffset += root.yOffset;
                            Splay2(ref x, start, /*[Feature(Feature.Range2)]*/side);
                            Debug.Assert(x.right == Nil);
                            if (root.right != Nil)
                            {
                                root.right.xOffset += root.xOffset - x.xOffset - xLength;
                                root.right.yOffset += root.yOffset - x.yOffset - yLength;
                            }
                            x.right = root.right;
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

        
        /// <summary>
        /// Attempt to query the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <param name="length">the length of the range from the specified side (X or Y)</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] int start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out int length)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                    if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                    {
                        Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                        if (root.right != Nil)
                        {
                            Debug.Assert(root.right.left == Nil);
                            length = side == Side.X ? root.right.xOffset : root.right.yOffset;
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

        
        /// <summary>
        /// Attempt to change the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to modify</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="length">the new length to apply on the specified side (X or Y) of the range pair. The new length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] int start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] int length)
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
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(root.right.left == Nil);
                        oldLength = side == Side.X ? root.right.xOffset : root.right.yOffset;
                    }
                    else
                    {
                        oldLength = side == Side.X ? unchecked(this.xExtent - root.xOffset) : unchecked(this.yExtent - root.yOffset);
                    }
                    /*[Widen]*/
                    int delta = length - oldLength;
                    if (side == Side.X)
                    {
                        this.xExtent = checked(this.xExtent + delta);

                        if (root.right != Nil)
                        {
                            unchecked
                            {
                                root.right.xOffset += delta;
                            }
                        }
                    }
                    else
                    {
                        this.yExtent = checked(this.yExtent + delta);

                        if (root.right != Nil)
                        {
                            unchecked
                            {
                                root.right.yOffset += delta;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        
        /// <summary>
        /// Attempt to query the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index on the specified side</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] int start,[Feature(Feature.Range2)] Side side,out ValueType value)
        {
            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                {
                    value = root.value;
                    return true;
                }
            }
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Attempt to update the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] int start,[Feature(Feature.Range2)] Side side,ValueType value)
        {
            if (root != Nil)
            {
                Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                {
                    root.value = value;
                    return true;
                }
            }
            return false;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] int start,[Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Feature(Feature.Range2)][Widen] out int otherStart,[Widen] out int xLength,[Feature(Feature.Range2)][Widen] out int yLength,[Payload(Payload.Value)] out ValueType value)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start, /*[Feature(Feature.Range2)]*/side);
                    if (start == Start(root, /*[Feature(Feature.Range2)]*/side))
                    {
                        Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        otherStart = side != Side.X ? root.xOffset : root.yOffset;
                        if (root.right != Nil)
                        {
                            xLength = root.right.xOffset;
                            yLength = root.right.yOffset;
                        }
                        else
                        {
                            xLength = this.xExtent - root.xOffset;
                            yLength = this.yExtent - root.yOffset;
                        }
                        value = root.value;
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
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySet([Widen] int start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] int xLength,[Feature(Feature.Range2)][Widen] int yLength,[Payload(Payload.Value)] ValueType value)
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
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(root.right.left == Nil);
                        xLengthOld = root.right.xOffset;
                        yLengthOld = root.right.yOffset;
                    }
                    else
                    {
                        xLengthOld = unchecked(this.xExtent - root.xOffset);
                        yLengthOld = unchecked(this.yExtent - root.yOffset);
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

                    if (root.right != Nil)
                    {
                        unchecked
                        {
                            root.right.xOffset += xAdjust;
                        }
                    }
                    if (root.right != Nil)
                    {
                        unchecked
                        {
                            root.right.yOffset += yAdjust;
                        }
                    }

                    root.value = value;

                    return true;
                }
            }
            return false;
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] int start,[Feature(Feature.Range2)] Side side,[Widen] int xLength,[Feature(Feature.Range2)][Widen] int yLength,[Payload(Payload.Value)] ValueType value)
        {
            if (!TryInsert(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        
        /// <summary>
        /// Deletes the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] int start,[Feature(Feature.Range2)] Side side)
        {
            if (!TryDelete(start, /*[Feature(Feature.Range2)]*/side))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the length associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies. The side also determines which length is returned</param>
        /// <returns>the length of the range from the specified side (X or Y)</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetLength([Widen] int start,[Feature(Feature.Range2)] Side side)
        {
            /*[Widen]*/
            int length;
            if (!TryGetLength(start, /*[Feature(Feature.Range2)]*/side, out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] int start,[Feature(Feature.Range2)] Side side,[Widen] int length)
        {
            if (!TrySetLength(start, /*[Feature(Feature.Range2)]*/side, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to query</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <returns>the value associated with the range</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public ValueType GetValue([Widen] int start,[Feature(Feature.Range2)] Side side)
        {
            ValueType value;
            if (!TryGetValue(start, /*[Feature(Feature.Range2)]*/side, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        
        /// <summary>
        /// Updates the value associated with the range pair starting at the specified start index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to update</param>
        /// <param name="side">the side (X or Y) to which the start index applies.</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index on the specified side</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public void SetValue([Widen] int start,[Feature(Feature.Range2)] Side side,ValueType value)
        {
            if (!TrySetValue(start, /*[Feature(Feature.Range2)]*/side, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] int start,[Feature(Feature.Range2)] Side side,[Feature(Feature.Range2)][Widen] out int otherStart,[Widen] out int xLength,[Feature(Feature.Range2)][Widen] out int yLength,[Payload(Payload.Value)] out ValueType value)
        {
            if (!TryGet(start, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public void Set([Widen] int start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] int xLength,[Feature(Feature.Range2)][Widen] int yLength,[Payload(Payload.Value)] ValueType value)
        {
            if (!TrySet(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the extent of the sequence of ranges on the specified side. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <param name="side">the side (X or Y) to which the query applies.</param>
        /// <returns>the extent of the ranges on the specified side</returns>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetExtent([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return side == Side.X ? this.xExtent : this.yExtent;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestLess([Widen] int position,[Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out int nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                int start = Start(root, /*[Feature(Feature.Range2)]*/side);
                if ((position < start) || (!orEqual && (position == start)))
                {
                    if (root.left != Nil)
                    {
                        Splay2(ref root.left, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(root.left.right == Nil);
                        nearestStart = start + (side == Side.X ? root.left.xOffset : root.left.yOffset);
                        return true;
                    }
                    nearestStart = 0;
                    return false;
                }
                else
                {
                    nearestStart = Start(root, /*[Feature(Feature.Range2)]*/side);
                    return true;
                }
            }
            nearestStart = 0;
            return false;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart,[Feature(Feature.Range2)][Widen] out int otherStart,[Widen] out int xLength,[Feature(Feature.Range2)][Widen] out int yLength,[Payload(Payload.Value)] out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestLess(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart)
        {
            return NearestLess(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, true/*orEqual*/);
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart,[Feature(Feature.Range2)][Widen] out int otherStart,[Widen] out int xLength,[Feature(Feature.Range2)][Widen] out int yLength,[Payload(Payload.Value)] out ValueType value)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestLess(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart)
        {
            return NearestLess(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, false/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestGreater([Widen] int position,[Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out int nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                int start = Start(root, /*[Feature(Feature.Range2)]*/side);
                if ((position > start) || (!orEqual && (position == start)))
                {
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(root.right.left == Nil);
                        nearestStart = start + (side == Side.X ? root.right.xOffset : root.right.yOffset);
                        return true;
                    }
                    nearestStart = side == Side.X ? this.xExtent : this.yExtent;
                    return false;
                }
                else
                {
                    nearestStart = start;
                    return true;
                }
            }
            nearestStart = 0;
            return false;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart,[Feature(Feature.Range2)][Widen] out int otherStart,[Widen] out int xLength,[Feature(Feature.Range2)][Widen] out int yLength,[Payload(Payload.Value)] out ValueType value)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestGreater(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart)
        {
            return NearestGreater(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, true/*orEqual*/);
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart,[Feature(Feature.Range2)][Widen] out int otherStart,[Widen] out int xLength,[Feature(Feature.Range2)][Widen] out int yLength,[Payload(Payload.Value)] out ValueType value)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            value = default(ValueType);
            bool f = NearestGreater(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position,[Feature(Feature.Range2)] Side side,[Widen] out int nearestStart)
        {
            return NearestGreater(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, false/*orEqual*/);
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
            node.yOffset = Int32.MinValue;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {
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

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Widen]
        private int Start(Node n,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return side == Side.X ? n.xOffset : n.yOffset;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [EnableFixed]
        private void Splay2(ref Node root,[Widen] int position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
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
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int lyOffset = 0;
                Node r = N;
                /*[Widen]*/
                int rxOffset = 0;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                int ryOffset = 0;

                while (true)
                {
                    int c;

                    c = position.CompareTo(side == Side.X ? t.xOffset : t.yOffset);
                    if (c < 0)
                    {
                        if (t.left == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(side == Side.X
                                ? t.xOffset + t.left.xOffset
                                : t.yOffset + t.left.yOffset);
                        if (c < 0)
                        {
                            // rotate right
                            Node u = t.left;
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            int uYPosition = t.yOffset + u.yOffset;
                            if (u.right != Nil)
                            {
                                u.right.xOffset += uXPosition - t.xOffset;
                                u.right.yOffset += uYPosition - t.yOffset;
                            }
                            t.xOffset += -uXPosition;
                            t.yOffset += -uYPosition;
                            u.xOffset = uXPosition;
                            u.yOffset = uYPosition;
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
                        t.left.yOffset += t.yOffset;
                        t.xOffset -= rxOffset;
                        t.yOffset -= ryOffset;
                        r.left = t;
                        r = t;
                        rxOffset += r.xOffset;
                        ryOffset += r.yOffset;
                        t = t.left;
                    }
                    else if (c > 0)
                    {
                        if (t.right == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(side == Side.X
                                ? (t.xOffset + t.right.xOffset)
                                : (t.yOffset + t.right.yOffset));
                        if (c > 0)
                        {
                            // rotate left
                            Node u = t.right;
                            /*[Widen]*/
                            int uXPosition = t.xOffset + u.xOffset;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            int uYPosition = t.yOffset + u.yOffset;
                            if (u.left != Nil)
                            {
                                u.left.xOffset += uXPosition - t.xOffset;
                                u.left.yOffset += uYPosition - t.yOffset;
                            }
                            t.xOffset += -uXPosition;
                            t.yOffset += -uYPosition;
                            u.xOffset = uXPosition;
                            u.yOffset = uYPosition;
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
                        t.right.yOffset += t.yOffset;
                        t.xOffset -= lxOffset;
                        t.yOffset -= lyOffset;
                        l.right = t;
                        l = t;
                        lxOffset += l.xOffset;
                        lyOffset += l.yOffset;
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
                    l.right.yOffset += t.yOffset - lyOffset;
                }
                r.left = t.right;
                if (r.left != Nil)
                {
                    r.left.xOffset += t.xOffset - rxOffset;
                    r.left.yOffset += t.yOffset - ryOffset;
                }
                t.left = N.right;
                if (t.left != Nil)
                {
                    t.left.xOffset -= t.xOffset;
                    t.left.yOffset -= t.yOffset;
                }
                t.right = N.left;
                if (t.right != Nil)
                {
                    t.right.xOffset -= t.xOffset;
                    t.right.yOffset -= t.yOffset;
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
                Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int offset = 0;
                /*[Widen]*/
                int leftEdge = 0;
                /*[Widen]*/
                int rightEdge = side == Side.X ? this.xExtent : this.yExtent;

                Node node = root;
                while (node != Nil)
                {
                    offset += side == Side.X ? node.xOffset : node.yOffset;
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
                        offset += side == Side.X ? node.xOffset : node.yOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
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
            object key = null;
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
            Node n = (Node)node;
            object value = null;
            value = n.value;
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
            return null;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
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
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Nil)
                    {
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.X);
            /*[Feature(Feature.Range2)]*/
            ValidateRanges(/*[Feature(Feature.Range2)]*/Side.Y);
        }

        // INonInvasiveRange2MapInspection

        /// <summary>
        /// INonInvasiveRange2MapInspection.GetRanges() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
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
                Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;
                /*[Widen]*/
                int yOffset = 0;

                Node node = root;
                while (node != Nil)
                {
                    xOffset += node.xOffset;
                    yOffset += node.yOffset;
                    stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node, /*[Widen]*/int, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;
                    yOffset = t.Item3;

                    object value = null;
                    value = node.value;

                    /*[Widen]*/
                    ranges[i++] = new Range2MapEntry(new Range(xOffset, 0), new Range(yOffset, 0), value);

                    node = node.right;
                    while (node != Nil)
                    {
                        xOffset += node.xOffset;
                        yOffset += node.yOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                        node = node.left;
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

        /// <summary>
        /// INonInvasiveRange2MapInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Range, Feature.Range2)]
        [ExcludeFromCodeCoverage]
        void INonInvasiveRange2MapInspection.Validate()
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
        public IEnumerator<EntryRange2Map<ValueType>> GetEnumerator()
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
        public IEnumerable<EntryRange2Map<ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRange2Map<ValueType>>
        {
            private readonly SplayTreeRange2Map<ValueType> tree;

            public RobustEnumerableSurrogate(SplayTreeRange2Map<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRange2Map<ValueType>> GetEnumerator()
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
        public IEnumerable<EntryRange2Map<ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryRange2Map<ValueType>>
        {
            private readonly SplayTreeRange2Map<ValueType> tree;

            public FastEnumerableSurrogate(SplayTreeRange2Map<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRange2Map<ValueType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryRange2Map<ValueType>>
        {
            private readonly SplayTreeRange2Map<ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(SplayTreeRange2Map<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRange2Map<ValueType> Current
            {
                get
                {
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (version != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

                    if (valid)

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

                            return new EntryRange2Map<ValueType>(
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength,
                                /*[Feature(Feature.Range2)]*/yStart,
                                /*[Feature(Feature.Range2)]*/yLength);
                        }
                    return new EntryRange2Map<ValueType>();
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

                    // OR

                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    valid = tree.xExtent != 0;
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    Debug.Assert(currentXStart == 0);

                    started = true;
                }
                else if (valid)
                {

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
        public class FastEnumerator : IEnumerator<EntryRange2Map<ValueType>>
        {
            private readonly SplayTreeRange2Map<ValueType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;
            [Feature(Feature.Range2)]
            [Widen]
            private int currentYStart, nextYStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>>();

            public FastEnumerator(SplayTreeRange2Map<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRange2Map<ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Nil)
                    {

                        return new EntryRange2Map<ValueType>(
                            /*[Payload(Payload.Value)]*/currentNode.value,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart,
                            /*[Feature(Feature.Range2)]*/currentYStart,
                            /*[Feature(Feature.Range2)]*/nextYStart - currentYStart);
                    }
                    return new EntryRange2Map<ValueType>();
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

            private void PushSuccessor(                Node node,                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xPosition,                [Feature(Feature.Range2)][Widen] int yPosition)
            {
                while (node != tree.Nil)
                {
                    xPosition += node.xOffset;
                    yPosition += node.yOffset;

                    stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int>(
                        node,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                        /*[Feature(Feature.Range2)]*/yPosition));
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

                STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int, /*[Feature(Feature.Range2)]*//*[Widen]*/int> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;
                nextXStart = cursor.Item2;
                nextYStart = cursor.Item3;

                PushSuccessor(
                    nextNode.right,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart,
                    /*[Feature(Feature.Range2)]*/nextYStart);
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new SplayTreeRange2Map<ValueType>(this);
        }
    }
}
