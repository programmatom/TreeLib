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
    public class SplayTreeRange2MapLong<[Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Range2)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRange2MapLong<ValueType>,
        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspectionLong,
        IEnumerable<EntryRange2MapLong<ValueType>>,
        IEnumerable,
        ITreeEnumerable<EntryRange2MapLong<ValueType>>,
        /*[Feature(Feature.Range2)]*//*[Widen]*/IIndexed2TreeEnumerableLong<EntryRange2MapLong<ValueType>>,
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
            public long xOffset;
            [Feature(Feature.Range2)]
            [Widen]
            public long yOffset;

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

                string valueText = null;
                try
                {
                    valueText = value.ToString();
                }
                catch (NullReferenceException)
                {
                }

                string leftKeyText = null;

                string rightKeyText = null;

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

        //
        // State for both array & object form
        //

        private Node root;
        [Count]
        private ulong count;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;
        [Feature(Feature.Range2)]
        [Widen]
        private long yExtent;

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
        public SplayTreeRange2MapLong(uint capacity,AllocationMode allocationMode)
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
        public SplayTreeRange2MapLong()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a splay tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public SplayTreeRange2MapLong(SplayTreeRange2MapLong<ValueType> original)
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
                    allocateCount = 0;
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
        public bool Contains([Widen] long start,[Feature(Feature.Range2)] Side side)
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
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength,[Payload(Payload.Value)] ValueType value)
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
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Widen]*/
                    long yExtentNew = checked(this.yExtent + yLength) ;

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
                long length = root.right != Nil
                    ? (side == Side.X ? root.right.xOffset : root.right.yOffset)
                    : (side == Side.X ? this.xExtent - root.xOffset : this.yExtent - root.yOffset) ;
                if (start == Start(root, /*[Feature(Feature.Range2)]*/side) + length)
                {
                    // append

                    Debug.Assert(root.right == Nil);

                    /*[Widen]*/
                    long xLengthRoot = this.xExtent - root.xOffset ;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    long yLengthRoot = this.yExtent - root.yOffset ;

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Widen]*/
                    long yExtentNew = checked(this.yExtent + yLength) ;

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
        public bool TryDelete([Widen] long start,[Feature(Feature.Range2)] Side side)
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
                        long xLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset ;
                        /*[Feature(Feature.Range2)]*/
                        /*[Widen]*/
                        long yLength = root.right != Nil ? root.right.yOffset : this.yExtent - root.yOffset ;

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
        public bool TryGetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long length)
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
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long length)
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
                    long oldLength ;
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
                    long delta = length - oldLength ;
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
        public bool TryGetValue([Widen] long start,[Feature(Feature.Range2)] Side side,out ValueType value)
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
        public bool TrySetValue([Widen] long start,[Feature(Feature.Range2)] Side side,ValueType value)
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
        public bool TryGet([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
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
        /// start was not found or the sum of lengths would have exceeded Int64.MaxValue</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySet([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength,[Payload(Payload.Value)] ValueType value)
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
                    long xLengthOld, yLengthOld ;
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
                    long xAdjust = xLength != 0 ? xLength - xLengthOld : 0 ;
                    /*[Widen]*/
                    long yAdjust = yLength != 0 ? yLength - yLengthOld : 0 ;

                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xAdjust) ;
                    /*[Widen]*/
                    long yExtentNew = checked(this.yExtent + yAdjust) ;
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
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue on either side</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] long start,[Feature(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength,[Payload(Payload.Value)] ValueType value)
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
        public void Delete([Widen] long start,[Feature(Feature.Range2)] Side side)
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
        public long GetLength([Widen] long start,[Feature(Feature.Range2)] Side side)
        {
            /*[Widen]*/
            long length ;
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
        /// <exception cref="OverflowException">the sum of lengths on the specified side would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] long start,[Feature(Feature.Range2)] Side side,[Widen] long length)
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
        public ValueType GetValue([Widen] long start,[Feature(Feature.Range2)] Side side)
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
        public void SetValue([Widen] long start,[Feature(Feature.Range2)] Side side,ValueType value)
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
        public void Get([Widen] long start,[Feature(Feature.Range2)] Side side,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
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
        /// <exception cref="OverflowException">sum of lengths would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Set([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength,[Payload(Payload.Value)] ValueType value)
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
        public long GetExtent([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return side == Side.X ? this.xExtent : this.yExtent;
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestLess([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                long start = Start(root, /*[Feature(Feature.Range2)]*/side) ;
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
        public bool NearestLessOrEqual([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
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
        public bool NearestLessOrEqual([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart)
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
        public bool NearestLess([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
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
        public bool NearestLess([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart)
        {
            return NearestLess(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, false/*orEqual*/);
        }

        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool NearestGreater([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position, /*[Feature(Feature.Range2)]*/side);
                /*[Widen]*/
                long start = Start(root, /*[Feature(Feature.Range2)]*/side) ;
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
        public bool NearestGreaterOrEqual([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
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
        public bool NearestGreaterOrEqual([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart)
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
        public bool NearestGreater([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength,[Payload(Payload.Value)] out ValueType value)
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
        public bool NearestGreater([Widen] long position,[Feature(Feature.Range2)] Side side,[Widen] out long nearestStart)
        {
            return NearestGreater(position, /*[Feature(Feature.Range2)]*/side, out nearestStart, false/*orEqual*/);
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public void AdjustLength([Widen] long startIndex,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xAdjust,[Feature(Feature.Range2)] [Widen] long yAdjust)
        {
            unchecked
            {
                Splay2(ref root, startIndex, /*[Feature(Feature.Range2)]*/side);
                if ((root == Nil) || (startIndex != (side == Side.X ? root.xOffset : root.yOffset)))
                {
                    throw new ArgumentException();
                }

                Splay2(ref root.right, 0, /*[Feature(Feature.Range2)]*/side);
                Debug.Assert((root.right == Nil) || (root.right.left == Nil));

                /*[Widen]*/
                long oldXLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset ;
                /*[Widen]*/
                long oldYLength = root.right != Nil ? root.right.yOffset : this.yExtent - root.yOffset ;

                /*[Widen]*/
                long newXLength = checked(oldXLength + xAdjust) ;
                /*[Widen]*/
                long newYLength = 0 ;
                newYLength = checked(oldYLength + yAdjust);

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
                    long newXExtent = checked(this.xExtent + xAdjust) ;
                    /*[Widen]*/
                    long newYExtent = checked(this.yExtent + yAdjust) ;
                    this.xExtent = newXExtent;
                    this.yExtent = newYExtent;

                    if (root.right != Nil)
                    {
                        root.right.xOffset += xAdjust;
                        root.right.yOffset += yAdjust;
                    }
                }
                else
                {
                    // delete

                    Debug.Assert(xAdjust < 0);
                    Debug.Assert(yAdjust < 0);
                    Debug.Assert(newXLength == 0);
                    /*[Feature(Feature.Range2)]*/
                    Debug.Assert(newYLength == 0);

                    Node dead, x;

                    dead = root;
                    if (root.left == Nil)
                    {
                        x = root.right;
                        if (x != Nil)
                        {
                            Debug.Assert(root.xOffset == 0);
                            x.xOffset = 0; //nodes[x].xOffset = nodes[root].xOffset;
                            x.yOffset = 0; //nodes[x].yOffset = nodes[root].yOffset;
                        }
                    }
                    else
                    {
                        x = root.left;
                        x.xOffset += root.xOffset;
                        x.yOffset += root.yOffset;
                        Splay2(ref x, startIndex, /*[Feature(Feature.Range2)]*/side);
                        Debug.Assert(x.right == Nil);
                        if (root.right != Nil)
                        {
                            root.right.xOffset += root.xOffset - x.xOffset + xAdjust;
                            root.right.yOffset += root.yOffset - x.yOffset + yAdjust;
                        }
                        x.right = root.right;
                    }
                    root = x;

                    this.count = unchecked(this.count - 1);
                    this.xExtent = unchecked(this.xExtent - oldXLength);
                    this.yExtent = unchecked(this.yExtent - oldYLength);
                    Free(dead);
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
        private long Start(Node n,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return side == Side.X ? n.xOffset : n.yOffset;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [EnableFixed]
        private void Splay2(ref Node root,[Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
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
                long lxOffset = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long lyOffset = 0 ;
                Node r = N;
                /*[Widen]*/
                long rxOffset = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long ryOffset = 0 ;

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
                            long uXPosition = t.xOffset + u.xOffset ;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            long uYPosition = t.yOffset + u.yOffset ;
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
                            long uXPosition = t.xOffset + u.xOffset ;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            long uYPosition = t.yOffset + u.yOffset ;
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
        private void ValidateRanges([Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            if (root != Nil)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long offset = 0 ;
                /*[Widen]*/
                long leftEdge = 0 ;
                /*[Widen]*/
                long rightEdge = side == Side.X ? this.xExtent : this.yExtent ;

                Node node = root;
                while (node != Nil)
                {
                    offset += side == Side.X ? node.xOffset : node.yOffset;
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
                        offset += side == Side.X ? node.xOffset : node.yOffset;
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
            return null;
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
        [Widen]
        Range2MapEntryLong[] INonInvasiveRange2MapInspectionLong.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntryLong[] ranges = new Range2MapEntryLong[Count];
            int i = 0;

            if (root != Nil)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long xOffset = 0 ;
                /*[Widen]*/
                long yOffset = 0 ;

                Node node = root;
                while (node != Nil)
                {
                    xOffset += node.xOffset;
                    yOffset += node.yOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long>(node, xOffset, yOffset));
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node,/*[Widen]*/long,/*[Widen]*/long> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;
                    yOffset = t.Item3;

                    object value = null;
                    value = node.value;

                    /*[Widen]*/
                    ranges[i++] = new Range2MapEntryLong(new RangeLong(xOffset, 0), new RangeLong(yOffset, 0), value);

                    node = node.right;
                    while (node != Nil)
                    {
                        xOffset += node.xOffset;
                        yOffset += node.yOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long>(node, xOffset, yOffset));
                        node = node.left;
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
        void INonInvasiveRange2MapInspectionLong.Validate()
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
        public IEnumerator<EntryRange2MapLong<ValueType>> GetEnumerator()
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

        public IEnumerable<EntryRange2MapLong<ValueType>> GetEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryRange2MapLong<ValueType>> GetEnumerable(bool forward)
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
        public IEnumerable<EntryRange2MapLong<ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryRange2MapLong<ValueType>> GetRobustEnumerable(bool forward)
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
        public IEnumerable<EntryRange2MapLong<ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        public IEnumerable<EntryRange2MapLong<ValueType>> GetFastEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        //
        // IIndexedTreeEnumerable/IIndexed2TreeEnumerable
        //

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2MapLong<ValueType>> GetEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/); // default
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2MapLong<ValueType>> GetEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward); // default
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2MapLong<ValueType>> GetFastEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new FastEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2MapLong<ValueType>> GetFastEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2MapLong<ValueType>> GetRobustEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2MapLong<ValueType>> GetRobustEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRange2MapLong<ValueType>>
        {
            private readonly SplayTreeRange2MapLong<ValueType> tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly long startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            // Construction

            public RobustEnumerableSurrogate(SplayTreeRange2MapLong<ValueType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerableSurrogate(SplayTreeRange2MapLong<ValueType> tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;
            }

            // IEnumerable

            public IEnumerator<EntryRange2MapLong<ValueType>> GetEnumerator()
            {

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

        public struct FastEnumerableSurrogate : IEnumerable<EntryRange2MapLong<ValueType>>
        {
            private readonly SplayTreeRange2MapLong<ValueType> tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly long startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            // Construction

            public FastEnumerableSurrogate(SplayTreeRange2MapLong<ValueType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerableSurrogate(SplayTreeRange2MapLong<ValueType> tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;
            }

            // IEnumerable

            public IEnumerator<EntryRange2MapLong<ValueType>> GetEnumerator()
            {

                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (startIndexed)
                {
                    return new FastEnumerator(tree, startStart, /*[Feature(Feature.Range2)]*/side, forward);
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
            IEnumerator<EntryRange2MapLong<ValueType>>,
            /*[Payload(Payload.Value)]*/ISetValue<ValueType>
        {
            private readonly SplayTreeRange2MapLong<ValueType> tree;
            private readonly bool forward;
            //
            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly long startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            private bool started;
            private bool valid;
            private ushort enumeratorVersion;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private long currentStart;
            //
            // saving the currentXStart with does not work well for range collections because it may shift, so making updates
            // is not permitted in range trees
            [Feature(Feature.Range, Feature.Range2)]
            private ushort treeVersion;

            public RobustEnumerator(SplayTreeRange2MapLong<ValueType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerator(SplayTreeRange2MapLong<ValueType> tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;

                Reset();
            }

            public EntryRange2MapLong<ValueType> Current
            {
                get
                {
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (treeVersion != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

                    if (valid)

                        // OR

                        /*[Feature(Feature.Range, Feature.Range2)]*/
                        {
                            ValueType value = default(ValueType);
                            /*[Widen]*/
                            long xStart = 0, xLength = 0 ;
                            /*[Widen]*/
                            long yStart = 0, yLength = 0 ;

                            /*[Widen]*/
                            long otherStart ;
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

                            /*[Feature(Feature.Range, Feature.Range2)]*/
                            treeVersion = tree.version; // our query is ok

                            return new EntryRange2MapLong<ValueType>(
                                /*[Payload(Payload.Value)]*/value,
                                /*[Payload(Payload.Value)]*/this,
                                /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength,
                                /*[Feature(Feature.Range2)]*/yStart,
                                /*[Feature(Feature.Range2)]*/yLength);
                        }
                    return new EntryRange2MapLong<ValueType>();
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
                if (this.treeVersion != tree.version)
                {
                    throw new InvalidOperationException();
                }

                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                if (!started)
                {

                    // OR

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

                    // OR

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

                /*[Feature(Feature.Range, Feature.Range2)]*/
                treeVersion = tree.version; // our query is ok

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;

                /*[Feature(Feature.Range, Feature.Range2)]*/
                treeVersion = tree.version;
                this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));
            }

            [Payload(Payload.Value)]
            public void SetValue(ValueType value,ushort requiredEnumeratorVersion)
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
                // OR
                tree.SetValue(currentStart, /*[Feature(Feature.Range2)]*/side, value);

                /*[Feature(Feature.Range, Feature.Range2)]*/
                treeVersion = tree.version; // the update we just made is acceptable since it doesn't change length (TODO: will be unneeded after doing TODO: above)
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
            IEnumerator<EntryRange2MapLong<ValueType>>,
            /*[Payload(Payload.Value)]*/ISetValue<ValueType>
        {
            private readonly SplayTreeRange2MapLong<ValueType> tree;
            private readonly bool forward;

            private readonly bool startKeyedOrIndexed;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly long startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            private ushort treeVersion;
            private ushort enumeratorVersion;

            private Node currentNode;
            private Node leadingNode;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart, nextXStart;
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long previousXStart;
            [Feature(Feature.Range2)]
            [Widen]
            private long currentYStart, nextYStart, previousYStart;

            private readonly Stack<STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>> stack
                = new Stack<STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>>();

            public FastEnumerator(SplayTreeRange2MapLong<ValueType> tree,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerator(SplayTreeRange2MapLong<ValueType> tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startStart = startStart;
                this.side = side;

                Reset();
            }

            public EntryRange2MapLong<ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Nil)
                    {


                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long currentXLength = 0 ;
                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long currentYLength = 0 ;

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

                        return new EntryRange2MapLong<ValueType>(
                            /*[Payload(Payload.Value)]*/currentNode.value,
                            /*[Payload(Payload.Value)]*/this,
                            /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXLength,
                            /*[Feature(Feature.Range2)]*/currentYStart,
                            /*[Feature(Feature.Range2)]*/currentYLength);
                    }
                    return new EntryRange2MapLong<ValueType>();
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
                unchecked
                {
                    stack.Clear();

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
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    long yPositionLastGreaterAncestor = 0 ;

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
                        yPosition += node.yOffset;

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
                                // OR
                                /*[Feature(Feature.Range2)]*/
                                c = startStart.CompareTo(side == Side.X ? xPosition : yPosition);
                            }
                        }

                        if (!foundMatch1 && (forward && (c <= 0)) || (!forward && (c >= 0)))
                        {
                            stack.Push(new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>(
                                node,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                                /*[Feature(Feature.Range2)]*/yPosition));
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
                                yPositionLastGreaterAncestor = yPosition;
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

                    this.enumeratorVersion = unchecked((ushort)(this.enumeratorVersion + 1));

                    previousXStart = currentXStart;
                    previousYStart = currentYStart;
                    currentNode = leadingNode;
                    currentXStart = nextXStart;
                    currentYStart = nextYStart;

                    leadingNode = tree.Nil;

                    if (stack.Count == 0)
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

                    STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long> cursor
                        = stack.Pop();

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;
                    nextYStart = cursor.Item3;

                    Node node = forward ? leadingNode.right : leadingNode.left;
                    /*[Widen]*/
                    long xPosition = nextXStart ;
                    /*[Widen]*/
                    long yPosition = nextYStart ;
                    while (node != tree.Nil)
                    {
                        xPosition += node.xOffset;
                        yPosition += node.yOffset;

                        stack.Push(new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                            /*[Feature(Feature.Range2)]*/yPosition));
                        node = forward ? node.left : node.right;
                    }
                }
            }

            [Payload(Payload.Value)]
            public void SetValue(ValueType value,ushort requiredEnumeratorVersion)
            {
                if ((this.enumeratorVersion != requiredEnumeratorVersion) || (this.treeVersion != tree.version))
                {
                    throw new InvalidOperationException();
                }

                currentNode.value = value;
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new SplayTreeRange2MapLong<ValueType>(this);
        }
    }
}
