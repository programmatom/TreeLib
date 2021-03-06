// NOTE: This file is auto-generated. DO NOT MAKE CHANGES HERE! They will be overwritten on rebuild.

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

    /// <summary>
    /// Implements a map, list or range collection using a red-black tree. 
    /// </summary>
    
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
    public class RedBlackTreeRange2ListLong:
        /*[Feature(Feature.Range2)]*//*[Payload(Payload.None)]*//*[Widen]*/IRange2ListLong,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspectionLong,

        IEnumerable<EntryRange2ListLong>,
        IEnumerable,
        ITreeEnumerable<EntryRange2ListLong>,
        /*[Feature(Feature.Range2)]*//*[Widen]*/IIndexed2TreeEnumerableLong<EntryRange2ListLong>,

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

            public bool isRed;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;
            [Feature(Feature.Range2)]
            [Widen]
            public long yOffset;
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
        [Feature(Feature.Range2)]
        [Widen]
        private long yExtent;

        private readonly AllocationMode allocationMode;
        private Node freelist;


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
        public RedBlackTreeRange2ListLong(uint capacity, AllocationMode allocationMode)
        {
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on a red-black tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public RedBlackTreeRange2ListLong()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a red-blacck tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public RedBlackTreeRange2ListLong(RedBlackTreeRange2ListLong original)
        {

            this.count = original.count;
            this.xExtent = original.xExtent;
            this.yExtent = original.yExtent;

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
            {
                Stack<STuple<Node, Node>> stack = new Stack<STuple<Node, Node>>();

                Node nodeOriginal = original.root;
                Node nodeThis = this.root;
                while (nodeOriginal != Null)
                {
                    Node nodeChild = new Node();
                    nodeChild.left = Null;
                    nodeChild.right = Null;
                    if (this.root == Null)
                    {
                        this.root = nodeChild;
                    }
                    else
                    {
                        nodeThis.left = nodeChild;
                    }
                    nodeThis = nodeChild;
                    stack.Push(new STuple<Node, Node>(nodeThis, nodeOriginal));
                    nodeOriginal = nodeOriginal.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node, Node> t = stack.Pop();
                    nodeThis = t.Item1;
                    nodeOriginal = t.Item2;
                    nodeThis.xOffset = nodeOriginal.xOffset;
                    nodeThis.yOffset = nodeOriginal.yOffset;
                    nodeThis.isRed = nodeOriginal.isRed;

                    if (nodeOriginal.right != Null)
                    {
                        bool first = true;
                        nodeOriginal = nodeOriginal.right;
                        while (nodeOriginal != Null)
                        {
                            Node nodeChild = new Node();
                            nodeChild.left = Null;
                            nodeChild.right = Null;
                            if (first)
                            {
                                first = false;
                                nodeThis.right = nodeChild;
                            }
                            else
                            {
                                nodeThis.left = nodeChild;
                            }
                            nodeThis = nodeChild;
                            stack.Push(new STuple<Node, Node>(nodeThis, nodeOriginal));
                            nodeOriginal = nodeOriginal.left;
                        }
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
                // non-recusrive depth-first traversal (in-order, but doesn't matter here)

                Stack<Node> stack = new Stack<Node>();

                Node node = root;
                while (node != Null)
                {
                    stack.Push(node);
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    node = stack.Pop();

                    Node dead = node;

                    node = node.right;
                    while (node != Null)
                    {
                        stack.Push(node);
                        node = node.left;
                    }

                    this.count = unchecked(this.count - 1);
                    Free(dead);
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
        public bool Contains([Widen] long start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            return FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition));
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength)
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
                start,
                /*[Feature(Feature.Range2)]*/side,
                xLength,
                /*[Feature(Feature.Range2)]*/yLength,
                true/*add*/,
                false/*update*/);
        }

        
        /// <summary>
        /// Attempt to delete the range pair starting at the specified index with respect to the specified side.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">the start index of the range to delete</param>
        /// <param name="side">the side (X or Y) to which the start index applies</param>
        /// <returns>true if a range pair was successfully deleted</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] long start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return DeleteInternal(
                start,
                /*[Feature(Feature.Range2)]*/side);
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
        public bool TryGetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out long length)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                length = side == Side.X ? xLength : yLength;
                return true;
            }
            length = 0;
            return false;
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
        public bool TrySetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] long length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            /*[Widen]*/
            long yPosition, yLength ;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                /*[Widen]*/
                long adjust = length - (side == Side.X ? xLength : yLength) ;
                /*[Widen]*/
                long xAdjust = 0 ;
                /*[Widen]*/
                long yAdjust = 0 ;
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
                long newXExtent = checked(this.xExtent + xAdjust) ;
                /*[Widen]*/
                long newYExtent = checked(this.yExtent + yAdjust) ;
                this.xExtent = newXExtent;
                this.yExtent = newYExtent;

                ShiftRightOfPath(unchecked(start + 1), /*[Feature(Feature.Range2)]*/side, xAdjust, /*[Feature(Feature.Range2)]*/yAdjust);

                return true;
            }
            return false;
        }

        
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
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength)
        {
            Node node;
            /*[Widen]*/
            long xPosition ;
            /*[Widen]*/
            long yPosition ;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                otherStart = side != Side.X ? xPosition : yPosition;
                return true;
            }
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            return false;
        }

        
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
        [Exclude(Feature.Range, Payload.None)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySet([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength)
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

            Node node;
            /*[Widen]*/
            long xPosition, xLengthOld ;
            /*[Widen]*/
            long yPosition, yLengthOld ;
            if (FindPosition(start, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLengthOld, /*[Feature(Feature.Range2)]*/out yLengthOld)
                && (start == (side == Side.X ? xPosition : yPosition)))
            {
                /*[Widen]*/
                long xAdjust = xLength != 0 ? xLength - xLengthOld : 0 ;
                /*[Widen]*/
                long yAdjust = yLength != 0 ? yLength - yLengthOld : 0 ;

                // throw OverflowException before modifying anything
                /*[Widen]*/
                long newXExtent = checked(this.xExtent + xAdjust) ;
                /*[Widen]*/
                long newYExtent = checked(this.yExtent + yAdjust) ;
                this.xExtent = newXExtent;
                this.yExtent = newYExtent;

                ShiftRightOfPath(unchecked(start + 1), /*[Feature(Feature.Range2)]*/side, xAdjust, /*[Feature(Feature.Range2)]*/yAdjust);

                return true;
            }
            return false;
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength)
        {
            if (!TryInsert(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength))
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
        public void Delete([Widen] long start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
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
        public long GetLength([Widen] long start, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
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
        public void SetLength([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] long length)
        {
            if (!TrySetLength(start, /*[Feature(Feature.Range2)]*/side, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
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
        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength)
        {
            if (!TryGet(start, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
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
        [Exclude(Feature.Range, Payload.None)]
        [Feature(Feature.Range, Feature.Range2)]
        public void Set([Widen] long start,[Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xLength,[Feature(Feature.Range2)][Widen] long yLength)
        {
            if (!TrySet(start, /*[Feature(Feature.Range2)]*/side, xLength, /*[Feature(Feature.Range2)]*/yLength))
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
        public bool NearestLessOrEqual([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
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
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength);
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
        public bool NearestLess([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
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
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength)
        {
            otherStart = 0;
            xLength = 0;
            yLength = 0;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength);
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
        public bool NearestGreaterOrEqual([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
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
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength);
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
        public bool NearestGreater([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, [Widen] out long nearestStart)
        {
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
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
        /// <param name="otherStart">an out parameter receiving start of the range pair on the other side of the mapping</param>
        /// <param name="xLength">an out parameter receiving the length of the range on side X</param>
        /// <param name="yLength">an out parameter receiving the length of the range on side Y</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] long position,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] out long nearestStart,[Feature(Feature.Range2)][Widen] out long otherStart,[Widen] out long xLength,[Feature(Feature.Range2)][Widen] out long yLength)
        {
            otherStart = side == Side.X ? this.yExtent : this.xExtent;
            xLength = 0;
            yLength = 0;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Range2)]*/side,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, /*[Feature(Feature.Range2)]*/side, /*[Feature(Feature.Range2)]*/out otherStart, out xLength, /*[Feature(Feature.Range2)]*/out yLength);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Adjust the lengths of the range starting at 'start' by adding xAdjust and yAdjust to the current lengths of the
        /// range. If the lengths would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="side">which side (X or Y) the start parameter applies</param>
        /// <param name="xAdjust">the amount to adjust the X length by. Value may be negative to shrink the length</param>
        /// <param name="yAdjust">the amount to adjust the Y length by. Value may be negative to shrink the length</param>
        /// <returns>The adjusted length</returns>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start', or the length on
        /// one side would become 0 while the length on the other side would not be 0.</exception>
        /// <exception cref="ArgumentOutOfRangeException">one or both of the lengths would become negative</exception>
        /// <exception cref="OverflowException">the X or Y extent would become larger than Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public long AdjustLength([Widen] long startIndex,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,[Widen] long xAdjust, [Feature(Feature.Range2)] [Widen] long yAdjust)
        {
            unchecked
            {
                Node node;
                /*[Widen]*/
                long xPosition, yPosition ;
                /*[Widen]*/
                long xLength = 1, yLength = 1 ;
                if (!FindPosition(startIndex, /*[Feature(Feature.Range2)]*/side, out node, out xPosition, /*[Feature(Feature.Range2)]*/out yPosition, out xLength, /*[Feature(Feature.Range2)]*/out yLength)
                    || (startIndex != (side == Side.X ? xPosition : yPosition)))
                {
                    throw new ArgumentException();
                }

                /*[Widen]*/
                long newXLength = checked(xLength + xAdjust) ;
                /*[Widen]*/
                long newYLength = 0 ;
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
                    long newXExtent = checked(this.xExtent + xAdjust) ;
                    /*[Widen]*/
                    long newYExtent = checked(this.yExtent + yAdjust) ;
                    this.xExtent = newXExtent;
                    this.yExtent = newYExtent;

                    ShiftRightOfPath(unchecked(startIndex + 1), /*[Feature(Feature.Range2)]*/side, xAdjust, /*[Feature(Feature.Range2)]*/yAdjust);

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

                    DeleteInternal(
                        startIndex,
                        /*[Feature(Feature.Range2)]*/side);

                    return 0;
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
        private Node Allocate(bool isRed)
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
            node.left = Null;
            node.right = Null;
            node.isRed = isRed;
            node.xOffset = 0;
            node.yOffset = 0;

            return node;
        }

        [Storage(Storage.Object)]
        private Node Allocate()
        {
            return Allocate(true/*isRed*/);
        }

        [Storage(Storage.Object)]
        private void Free(Node node)
        {
#if DEBUG
            allocateCount = checked(allocateCount - 1);
            Debug.Assert(allocateCount == this.count);

            node.left = Null;
            node.right = Null;
            node.xOffset = Int32.MinValue;
            node.yOffset = Int32.MinValue;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {

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


        private bool NearestLess(
            out Node nearestNode,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,
            bool orEqual)
        {
            Node lastLess = Null;
            /*[Widen]*/
            long xPositionLastLess = 0 ;
            /*[Widen]*/
            long yPositionLastLess = 0 ;
            Node node = root;
            if (node != Null)
            {
                /*[Widen]*/
                long xPosition = 0 ;
                /*[Widen]*/
                long yPosition = 0 ;
                while (true)
                {
                    unchecked
                    {
                        xPosition += node.xOffset;
                        yPosition += node.yOffset;
                    }

                    int c;
                    {
                        c = position.CompareTo(side == Side.X ? xPosition : yPosition);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestNode = node;
                        nearestStart = side == Side.X ? xPosition : yPosition;
                        return true;
                    }
                    Node next;
                    if (c <= 0)
                    {
                        next = node.left;
                    }
                    else
                    {
                        lastLess = node;
                        xPositionLastLess = xPosition;
                        yPositionLastLess = yPosition;
                        next = node.right;
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
                nearestStart = side == Side.X ? xPositionLastLess : yPositionLastLess;
                return true;
            }
            nearestNode = Null;
            nearestStart = 0;
            return false;
        }

        private bool NearestGreater(
            out Node nearestNode,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,
            bool orEqual)
        {
            Node lastGreater = Null;
            /*[Widen]*/
            long xPositionLastGreater = 0 ;
            /*[Widen]*/
            long yPositionLastGreater = 0 ;
            Node node = root;
            if (node != Null)
            {
                /*[Widen]*/
                long xPosition = 0 ;
                /*[Widen]*/
                long yPosition = 0 ;
                while (true)
                {
                    unchecked
                    {
                        xPosition += node.xOffset;
                        yPosition += node.yOffset;
                    }

                    int c;
                    {
                        c = position.CompareTo(side == Side.X ? xPosition : yPosition);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestNode = node;
                        nearestStart = side == Side.X ? xPosition : yPosition;
                        return true;
                    }
                    Node next;
                    if (c < 0)
                    {
                        lastGreater = node;
                        xPositionLastGreater = xPosition;
                        yPositionLastGreater = yPosition;
                        next = node.left;
                    }
                    else
                    {
                        next = node.right;
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
                nearestStart = side == Side.X ? xPositionLastGreater : yPositionLastGreater;
                return true;
            }
            nearestNode = Null;
            nearestStart = side == Side.X ? this.xExtent : this.yExtent;
            return false;
        }

        // Searches tree for key location.
        // If key is not present and add==true, node is inserted.
        // If key is preset and update==true, value is replaced.
        // Returns true if a node was added or if add==false and a node was updated.
        // NOTE: update mode does *not* adjust for xLength/yLength!
        private bool InsertUpdateInternal(
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xLength,
            [Feature(Feature.Range2)][Widen] long yLength,
            bool add,
            bool update)
        {
            Debug.Assert(add != update);

            if (root == Null)
            {

                if (!add)
                {
                    return false;
                }

                {
                    if (position != 0)
                    {
                        return false;
                    }
                }

                root = Allocate(false);
                Debug.Assert(root.xOffset == 0);
                Debug.Assert(root.yOffset == 0);
                Debug.Assert(this.xExtent == 0);
                Debug.Assert(this.yExtent == 0);
                this.xExtent = xLength;
                this.yExtent = yLength;

                Debug.Assert(this.count == 0);
                this.count = 1;
                this.version = unchecked(this.version + 1);

                return true;
            }

            try
            {
                // Search for a node at bottom to insert the new node. 
                // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
                // We split 4-nodes along the search path.
                Node current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionCurrent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionCurrent = 0 ;
                Node parent = Null;
                Node grandParent = Null;
                Node greatGrandParent = Null;

                Node successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionSuccessor = 0 ;

                //even if we don't actually add to the set, we may be altering its structure (by doing rotations
                //and such). so update version to disable any enumerators/subsets working on it
                this.version = unchecked(this.version + 1);

                int order = 0;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionParent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionParent = 0 ;
                while (current != Null)
                {
                    unchecked
                    {
                        xPositionCurrent += current.xOffset;
                        yPositionCurrent += current.yOffset;
                    }

                    {
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
                        // nodes[root].isRed = false; -- moved to finally block
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

                        current = current.left;
                    }
                    else
                    {
                        current = current.right;
                    }
                }
                Debug.Assert(current == Null);

                Debug.Assert(parent != Null, "Parent node cannot be null here!");

                // ready to insert the new node
                if (!add)
                {
                    return false;
                }

                Node node;
                if (order > 0)
                {
                    // follows parent

                    Debug.Assert(parent.right == Null);
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xLengthParent ;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    long yLengthParent ;
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
                    if (position != unchecked(side == Side.X ? xPositionParent + xLengthParent : yPositionParent + yLengthParent))
                    {
                        root.isRed = false;
                        return false;
                    }

                    // compute here to throw before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Widen]*/
                    long yExtentNew = checked(this.yExtent + yLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    node = Allocate();

                    ShiftRightOfPath(unchecked(xPositionParent + 1), /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                    parent.right = node;

                    node.xOffset = xLengthParent;
                    node.yOffset = yLengthParent;

                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;
                    this.count = countNew;
                }
                else
                {
                    // precedes parent

                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long positionParent = side == Side.X ? xPositionParent : yPositionParent ;
                        if (position != positionParent)
                        {
                            root.isRed = false;
                            return false;
                        }
                    }

                    // compute here to throw before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Widen]*/
                    long yExtentNew = checked(this.yExtent + yLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Debug.Assert(parent == successor);

                    node = Allocate();

                    ShiftRightOfPath(xPositionParent, /*[Feature(Feature.Range2)]*/Side.X, xLength, /*[Feature(Feature.Range2)]*/yLength);

                    parent.left = node;

                    node.xOffset = unchecked(-xLength);
                    node.yOffset = unchecked(-yLength);

                    this.xExtent = xExtentNew;
                    this.yExtent = yExtentNew;
                    this.count = countNew;
                }

                // the new node will be red, so we will need to adjust the colors if parent node is also red
                if (parent.isRed)
                {
                    InsertionBalance(node, ref parent, grandParent, greatGrandParent);
                }
            }
            finally
            {
                // root node is always black
                root.isRed = false;
            }

            return true;
        }

        // DOES NOT adjust xExtent and yExtent!
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ShiftRightOfPath(
            [Widen] long position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            [Widen] long xAdjust,
            [Feature(Feature.Range2)][Widen] long yAdjust)
        {
            unchecked
            {
                /*[Widen]*/
                long xPositionCurrent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionCurrent = 0 ;
                Node current = root;
                while (current != Null)
                {
                    xPositionCurrent += current.xOffset;
                    yPositionCurrent += current.yOffset;

                    int order = position.CompareTo(side == Side.X ? xPositionCurrent : yPositionCurrent);
                    if (order <= 0)
                    {
                        xPositionCurrent += xAdjust;
                        yPositionCurrent += yAdjust;
                        current.xOffset += xAdjust;
                        current.yOffset += yAdjust;
                        if (current.left != Null)
                        {
                            current.left.xOffset -= xAdjust;
                            current.left.yOffset -= yAdjust;
                        }

                        if (order == 0)
                        {
                            break;
                        }

                        current = current.left;
                    }
                    else
                    {
                        current = current.right;
                    }
                }
            }
        }

        private bool DeleteInternal(
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,
            [Feature(Feature.Range2)][Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)][SuppressConst(Feature.Range2)] Side side)
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
                this.version = unchecked(this.version + 1);

                Node current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionCurrent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionCurrent = 0 ;

                Node parent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionParent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionParent = 0 ;

                Node grandParent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionGrandparent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionGrandparent = 0 ;

                Node match = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionMatch = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionMatch = 0 ;

                Node parentOfMatch = Null;

                bool foundMatch = false;

                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionLastGreaterAncestor = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionLastGreaterAncestor = 0 ;
                while (current != Null)
                {
                    xPositionCurrent += current.xOffset;
                    yPositionCurrent += current.yOffset;

                    if (Is2Node(current))
                    {
                        // fix up 2-Node
                        if (parent == Null)
                        {
                            // current is root. Mark it as red
                            current.isRed = true;
                        }
                        else
                        {
                            Node sibling = GetSibling(current, parent);
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                            /*[Widen]*/
                            long xPositionSibling = xPositionParent + sibling.xOffset ;
                            /*[Feature(Feature.Range2)]*/
                            /*[Widen]*/
                            long yPositionSibling = yPositionParent + sibling.yOffset ;
                            if (sibling.isRed)
                            {
                                // If parent is a 3-node, flip the orientation of the red link. 
                                // We can achieve this by a single rotation        
                                // This case is converted to one of other cases below.
                                Debug.Assert(!parent.isRed, "parent must be a black node!");
                                Node newTop;
                                if (parent.right == sibling)
                                {
                                    newTop = RotateLeft(parent);
                                }
                                else
                                {
                                    newTop = RotateRight(parent);
                                }
                                Debug.Assert(newTop == sibling);

                                parent.isRed = true;
                                sibling.isRed = false;    // parent's color
                                                                 // sibling becomes child of grandParent or root after rotation. Update link from grandParent or root
                                ReplaceChildOfNodeOrRoot(grandParent, parent, sibling);
                                // sibling will become grandParent of current node 
                                grandParent = sibling;
                                xPositionGrandparent = xPositionSibling;
                                yPositionGrandparent = yPositionSibling;
                                if (parent == match)
                                {
                                    parentOfMatch = sibling;
                                }

                                // update sibling, this is necessary for following processing
                                sibling = (parent.left == current) ? parent.right : parent.left;
                                xPositionSibling += sibling.xOffset;
                                yPositionSibling += sibling.yOffset;
                            }
                            Debug.Assert(sibling != Null || sibling.isRed == false, "sibling must not be null and it must be black!");

                            if (Is2Node(sibling))
                            {
                                Merge2Nodes(parent, current, sibling);
                            }
                            else
                            {
                                // current is a 2-node and sibling is either a 3-node or a 4-node.
                                // We can change the color of current to red by some rotation.
                                TreeRotation rotation = RotationNeeded(parent, current, sibling);
                                Node newGrandParent = Null;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                /*[Widen]*/
                                long xPositionNewGrandparent = xPositionParent - parent.xOffset ;
                                /*[Feature(Feature.Range2)]*/
                                /*[Widen]*/
                                long yPositionNewGrandparent = yPositionParent - parent.yOffset ;
                                switch (rotation)
                                {
                                    default:
                                        Debug.Assert(false);
                                        throw new InvalidOperationException();

                                    case TreeRotation.RightRotation:
                                        Debug.Assert(parent.left == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(sibling.left.isRed, "Left child of sibling must be red!");
                                        sibling.left.isRed = false;
                                        newGrandParent = RotateRight(parent);
                                        break;
                                    case TreeRotation.LeftRotation:
                                        Debug.Assert(parent.right == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(sibling.right.isRed, "Right child of sibling must be red!");
                                        sibling.right.isRed = false;
                                        newGrandParent = RotateLeft(parent);
                                        break;

                                    case TreeRotation.RightLeftRotation:
                                        Debug.Assert(parent.right == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(sibling.left.isRed, "Left child of sibling must be red!");
                                        newGrandParent = RotateRightLeft(parent);
                                        break;

                                    case TreeRotation.LeftRightRotation:
                                        Debug.Assert(parent.left == sibling, "sibling must be left child of parent!");
                                        Debug.Assert(sibling.right.isRed, "Right child of sibling must be red!");
                                        newGrandParent = RotateLeftRight(parent);
                                        break;
                                }
                                xPositionNewGrandparent += newGrandParent.xOffset;
                                yPositionNewGrandparent += newGrandParent.yOffset;

                                newGrandParent.isRed = parent.isRed;
                                parent.isRed = false;
                                current.isRed = true;
                                ReplaceChildOfNodeOrRoot(grandParent, parent, newGrandParent);
                                if (parent == match)
                                {
                                    parentOfMatch = newGrandParent;
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
                            order = position.CompareTo(side == Side.X ? xPositionCurrent : yPositionCurrent);
                        }

                    if (order == 0)
                    {

                        // save the matching node
                        foundMatch = true;
                        match = current;
                        parentOfMatch = parent;

                        xPositionMatch = xPositionCurrent;
                        yPositionMatch = yPositionCurrent;
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

                        current = current.left;
                    }
                    else
                    {
                        current = current.right; // continue the search in right sub tree after we find a match (to find successor)
                    }
                }

                // move successor to the matching node position and replace links
                if (match != Null)
                {
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xLength ;
                    /*[Feature(Feature.Range2)]*/
                    /*[Widen]*/
                    long yLength ;
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
                    root.isRed = false;
                }
                return foundMatch;
            }
        }

        // Replace the matching node with its successor.
        private void ReplaceNode(
            Node match,
            Node parentOfMatch,
            Node successor,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xOffsetMatchSuccessor,
            [Feature(Feature.Range2)][Widen] long yOffsetMatchSuccessor,
            Node parentOfsuccessor)
        {
            unchecked
            {
                if (successor == match)
                {
                    // this node has no successor, should only happen if right child of matching node is null.
                    Debug.Assert(match.right == Null, "Right child must be null!");
                    successor = match.left;

                    if (successor != Null)
                    {
                        xOffsetMatchSuccessor = successor.xOffset;
                        yOffsetMatchSuccessor = successor.yOffset;
                    }
                }
                else
                {
                    Debug.Assert(parentOfsuccessor != Null, "parent of successor cannot be null!");
                    Debug.Assert(successor.left == Null, "Left child of successor must be null!");
                    Debug.Assert((successor.right == Null && successor.isRed)
                        || (successor.right.isRed && !successor.isRed), "Successor must be in valid state");
                    if (successor.right != Null)
                    {
                        successor.right.isRed = false;
                    }

                    if (parentOfsuccessor != match)
                    {
                        // detach successor from its parent and set its right child
                        parentOfsuccessor.left = successor.right;
                        if (successor.right != Null)
                        {
                            successor.right.xOffset += successor.xOffset;
                            successor.right.yOffset += successor.yOffset;
                        }
                        successor.right = match.right;
                        if (match.right != Null)
                        {
                            match.right.xOffset -= xOffsetMatchSuccessor;
                            match.right.yOffset -= yOffsetMatchSuccessor;
                        }
                    }

                    successor.left = match.left;
                    if (match.left != Null)
                    {
                        match.left.xOffset -= xOffsetMatchSuccessor;
                        match.left.yOffset -= yOffsetMatchSuccessor;
                    }
                }

                if (successor != Null)
                {
                    successor.isRed = match.isRed;

                    successor.xOffset = match.xOffset + xOffsetMatchSuccessor;
                    successor.yOffset = match.yOffset + yOffsetMatchSuccessor;
                }

                ReplaceChildOfNodeOrRoot(parentOfMatch/*parent*/, match/*child*/, successor/*new child*/);
            }
        }

        // Replace the child of a parent node. 
        // If the parent node is null, replace the root.        
        private void ReplaceChildOfNodeOrRoot(Node parent,Node child, Node newChild)
        {
            if (parent != Null)
            {
                if (parent.left == child)
                {
                    parent.left = newChild;
                }
                else
                {
                    parent.right = newChild;
                }
            }
            else
            {
                root = newChild;
            }
        }

        private Node GetSibling(Node node, Node parent)
        {
            if (parent.left == node)
            {
                return parent.right;
            }
            return parent.left;
        }

        // After calling InsertionBalance, we need to make sure current and parent up-to-date.
        // It doesn't matter if we keep grandParent and greatGrantParent up-to-date 
        // because we won't need to split again in the next node.
        // By the time we need to split again, everything will be correctly set.
        private void InsertionBalance(Node current,ref Node parent,Node grandParent, Node greatGrandParent)
        {
            Debug.Assert(grandParent != Null, "Grand parent cannot be null here!");
            bool parentIsOnRight = (grandParent.right == parent);
            bool currentIsOnRight = (parent.right == current);

            Node newChildOfGreatGrandParent;
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
            grandParent.isRed = true;
            newChildOfGreatGrandParent.isRed = false;

            ReplaceChildOfNodeOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
        }

        private bool Is2Node(Node node)
        {
            Debug.Assert(node != Null, "node cannot be null!");
            return IsBlack(node) && IsNullOrBlack(node.left) && IsNullOrBlack(node.right);
        }

        private bool Is4Node(Node node)
        {
            return IsRed(node.left) && IsRed(node.right);
        }

        private bool IsBlack(Node node)
        {
            return (node != Null && !node.isRed);
        }

        private bool IsNullOrBlack(Node node)
        {
            return (node == Null || !node.isRed);
        }

        private bool IsRed(Node node)
        {
            return (node != Null && node.isRed);
        }

        private void Merge2Nodes(Node parent,Node child1, Node child2)
        {
            Debug.Assert(IsRed(parent), "parent must be red");
            // combing two 2-nodes into a 4-node
            parent.isRed = false;
            child1.isRed = true;
            child2.isRed = true;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(
            [Widen] long position,
            [Feature(Feature.Range2)] [Const(Side.X, Feature.Rank, Feature.RankMulti, Feature.Range)] [SuppressConst(Feature.Range2)] Side side,
            out Node lastLessEqual,
            [Widen] out long xPositionLastLessEqual,
            [Feature(Feature.Range2)][Widen] out long yPositionLastLessEqual,
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long xLength,
            [Feature(Feature.Range2)][Widen] out long yLength)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;
                /*[Feature(Feature.Range2)]*/
                yPositionLastLessEqual = 0;
                Node successor = Null;
                xLength = 0;
                yLength = 0;

                Node current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionCurrent = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionCurrent = 0 ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yPositionSuccessor = 0 ;
                while (current != Null)
                {
                    xPositionCurrent += current.xOffset;
                    yPositionCurrent += current.yOffset;

                    if (position < (side == Side.X ? xPositionCurrent : yPositionCurrent))
                    {
                        successor = current;
                        xPositionSuccessor = xPositionCurrent;
                        yPositionSuccessor = yPositionCurrent;

                        current = current.left;
                    }
                    else
                    {
                        lastLessEqual = current;
                        xPositionLastLessEqual = xPositionCurrent;
                        yPositionLastLessEqual = yPositionCurrent;

                        current = current.right; // try to find successor
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

        private Node RotateLeft(Node node)
        {
            unchecked
            {
                Node r = node.right;

                if (r.left != Null)
                {
                    r.left.xOffset += r.xOffset;
                    r.left.yOffset += r.yOffset;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetR = r.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetR = r.yOffset ;
                r.xOffset += node.xOffset;
                r.yOffset += node.yOffset;
                node.xOffset = -xOffsetR;
                node.yOffset = -yOffsetR;

                node.right = r.left;
                r.left = node;

                return r;
            }
        }

        private Node RotateLeftRight(Node node)
        {
            unchecked
            {
                Node lChild = node.left;
                Node lrGrandChild = lChild.right;

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetNode = node.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetNode = node.yOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetLChild = lChild.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetLChild = lChild.yOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetLRGrandchild = lrGrandChild.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetLRGrandchild = lrGrandChild.yOffset ;

                lrGrandChild.xOffset = xOffsetLRGrandchild + xOffsetLChild + xOffsetNode;
                lrGrandChild.yOffset = yOffsetLRGrandchild + yOffsetLChild + yOffsetNode;
                lChild.xOffset = -xOffsetLRGrandchild;
                lChild.yOffset = -yOffsetLRGrandchild;
                node.xOffset = -xOffsetLRGrandchild - xOffsetLChild;
                node.yOffset = -yOffsetLRGrandchild - yOffsetLChild;
                if (lrGrandChild.left != Null)
                {
                    lrGrandChild.left.xOffset += xOffsetLRGrandchild;
                    lrGrandChild.left.yOffset += yOffsetLRGrandchild;
                }
                if (lrGrandChild.right != Null)
                {
                    lrGrandChild.right.xOffset += xOffsetLRGrandchild + xOffsetLChild;
                    lrGrandChild.right.yOffset += yOffsetLRGrandchild + yOffsetLChild;
                }

                node.left = lrGrandChild.right;
                lrGrandChild.right = node;
                lChild.right = lrGrandChild.left;
                lrGrandChild.left = lChild;

                return lrGrandChild;
            }
        }

        private Node RotateRight(Node node)
        {
            unchecked
            {
                Node l = node.left;

                if (l.right != Null)
                {
                    l.right.xOffset += l.xOffset;
                    l.right.yOffset += l.yOffset;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetL = l.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetL = l.yOffset ;
                l.xOffset += node.xOffset;
                l.yOffset += node.yOffset;
                node.xOffset = -xOffsetL;
                node.yOffset = -yOffsetL;

                node.left = l.right;
                l.right = node;

                return l;
            }
        }

        private Node RotateRightLeft(Node node)
        {
            unchecked
            {
                Node rChild = node.right;
                Node rlGrandChild = rChild.left;

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetNode = node.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetNode = node.yOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetRChild = rChild.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetRChild = rChild.yOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetRLGrandchild = rlGrandChild.xOffset ;
                /*[Feature(Feature.Range2)]*/
                /*[Widen]*/
                long yOffsetRLGrandchild = rlGrandChild.yOffset ;

                rlGrandChild.xOffset = xOffsetRLGrandchild + xOffsetRChild + xOffsetNode;
                rlGrandChild.yOffset = yOffsetRLGrandchild + yOffsetRChild + yOffsetNode;
                rChild.xOffset = -xOffsetRLGrandchild;
                rChild.yOffset = -yOffsetRLGrandchild;
                node.xOffset = -xOffsetRLGrandchild - xOffsetRChild;
                node.yOffset = -yOffsetRLGrandchild - yOffsetRChild;
                if (rlGrandChild.left != Null)
                {
                    rlGrandChild.left.xOffset += xOffsetRLGrandchild + xOffsetRChild;
                    rlGrandChild.left.yOffset += yOffsetRLGrandchild + yOffsetRChild;
                }
                if (rlGrandChild.right != Null)
                {
                    rlGrandChild.right.xOffset += xOffsetRLGrandchild;
                    rlGrandChild.right.yOffset += yOffsetRLGrandchild;
                }

                node.right = rlGrandChild.left;
                rlGrandChild.left = node;
                rChild.left = rlGrandChild.right;
                rlGrandChild.right = rChild;

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

        private TreeRotation RotationNeeded(Node parent,Node current, Node sibling)
        {
            Debug.Assert(IsRed(sibling.left) || IsRed(sibling.right), "sibling must have at least one red child");
            if (IsRed(sibling.left))
            {
                if (parent.left == current)
                {
                    return TreeRotation.RightLeftRotation;
                }
                return TreeRotation.RightRotation;
            }
            else
            {
                if (parent.left == current)
                {
                    return TreeRotation.LeftRotation;
                }
                return TreeRotation.LeftRightRotation;
            }
        }

        private void Split4Node(Node node)
        {
            node.isRed = true;
            node.left.isRed = false;
            node.right.isRed = false;
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
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long offset = 0 ;
                /*[Widen]*/
                long leftEdge = 0 ;
                /*[Widen]*/
                long rightEdge = side == Side.X ? this.xExtent : this.yExtent ;

                Node node = root;
                while (node != Null)
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
                    while (node != Null)
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
            return n.left != Null ? (object)n.left : null;
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
            return n.right != Null ? (object)n.right : null;
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
            return n.isRed ? "red" : "black";
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

                    if (node.left != Null)
                    {
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Null)
                    {
                        worklist.Enqueue(node.right);
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
            ActualMinDepth(root, 0, ref min);
            int depth = ActualMaxDepth(root);
            min++;
            int max = depth + 1;

            Check.Assert((2 * min >= max) && (depth <= TheoreticalMaxDepth(this.count)), "depth invariant");
        }

        public static int TheoreticalMaxDepth(ulong c)
        {
            return (int)Math.Ceiling(2 * Math.Log(c + 1, 2) + .001/*epsilon*/);
        }

        public static int EstimateMaxDepth(ulong c)
        {
            unchecked
            {
                int h = 2 * Log2.CeilLog2(c + 1) + 1/*robust about rounding error in TheoreticalMaxDepth()*/;

                Debug.Assert(h >= TheoreticalMaxDepth(c));
                return h;
            }
        }

        private int ActualMaxDepth(Node root)
        {
            return (root == Null) ? 0 : (1 + Math.Max(ActualMaxDepth(root.left), ActualMaxDepth(root.right)));
        }

        private void ActualMinDepth(Node root,int depth, ref int min)
        {
            if (root == Null)
            {
                min = Math.Min(min, depth);
            }
            else
            {
                if (depth < min)
                {
                    ActualMinDepth(root.left, depth + 1, ref min);
                }
                if (depth < min)
                {
                    ActualMinDepth(root.right, depth + 1, ref min);
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
        Range2MapEntryLong[] /*[Widen]*/INonInvasiveRange2MapInspectionLong.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntryLong[] ranges = new /*[Widen]*/Range2MapEntryLong[Count];
            int i = 0;

            if (root != Null)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long xOffset = 0 ;
                /*[Widen]*/
                long yOffset = 0 ;

                Node node = root;
                while (node != Null)
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

                    ranges[i++] = new /*[Widen]*/Range2MapEntryLong(new /*[Widen]*/RangeLong(xOffset, 0), new /*[Widen]*/RangeLong(yOffset, 0), value);

                    node = node.right;
                    while (node != Null)
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
        void /*[Widen]*/INonInvasiveRange2MapInspectionLong.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // IEnumerable
        //

        /// <summary>
        /// Get the default enumerator, which is the fast enumerator for red-black trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryRange2ListLong> GetEnumerator()
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
        public IEnumerable<EntryRange2ListLong> GetEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the default enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the default enumerator</returns>
        public IEnumerable<EntryRange2ListLong> GetEnumerable(bool forward)
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
        public IEnumerable<EntryRange2ListLong> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        public IEnumerable<EntryRange2ListLong> GetRobustEnumerable(bool forward)
        {
            return new RobustEnumerableSurrogate(this, forward);
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
        
        /// <summary>
        /// Create a new instance of the fast enumerator.
        /// </summary>
        /// <returns>A new instance of the fast enumerator</returns>
        public IEnumerable<EntryRange2ListLong> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        public IEnumerable<EntryRange2ListLong> GetFastEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        //
        // IIndexedTreeEnumerable/IIndexed2TreeEnumerable
        //

        
        /// <summary>
        /// Create a new instance of the default enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration starts
        /// with the following range.</param>
        /// <param name="side">The side (X or Y) to which the index pertains</param>
        /// <returns>A new instance of the default enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2ListLong> GetEnumerable([Widen] long startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/); // default
        }

        
        /// <summary>
        /// Create a new instance of the default enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration
        /// starts as follows: for forward enumeration, the range that follows; for reverse enumeration, the range containing
        /// the specified index</param>
        /// <param name="side">The side (X or Y) to which the index pertains</param>
        /// <param name="forward">True to move from ranges in order of increasing start indexes; False to move backwards
        /// from the last range through decreasing start indexes</param>
        /// <returns>A new instance of the default enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2ListLong> GetEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward); // default
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration starts
        /// with the following range.</param>
        /// <param name="side">The side (X or Y) to which the index pertains</param>
        /// <returns>A new instance of the fast enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2ListLong> GetFastEnumerable([Widen] long startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new FastEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration
        /// starts as follows: for forward enumeration, the range that follows; for reverse enumeration, the range containing
        /// the specified index</param>
        /// <param name="side">The side (X or Y) to which the index pertains</param>
        /// <param name="forward">True to move from ranges in order of increasing start indexes; False to move backwards
        /// from the last range through decreasing start indexes</param>
        /// <returns>A new instance of the fast enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2ListLong> GetFastEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration starts
        /// with the following range.</param>
        /// <param name="side">The side (X or Y) to which the index pertains</param>
        /// <returns>A new instance of the robust enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2ListLong> GetRobustEnumerable([Widen] long startAt, [Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration
        /// starts as follows: for forward enumeration, the range that follows; for reverse enumeration, the range containing
        /// the specified index</param>
        /// <param name="side">The side (X or Y) to which the index pertains</param>
        /// <param name="forward">True to move from ranges in order of increasing start indexes; False to move backwards
        /// from the last range through decreasing start indexes</param>
        /// <returns>A new instance of the robust enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRange2ListLong> GetRobustEnumerable([Widen] long startAt,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, /*[Feature(Feature.Range2)]*/side, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRange2ListLong>
        {
            private readonly RedBlackTreeRange2ListLong tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly long startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            // Construction

            public RobustEnumerableSurrogate(RedBlackTreeRange2ListLong tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerableSurrogate(RedBlackTreeRange2ListLong tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;
            }

            // IEnumerable

            public IEnumerator<EntryRange2ListLong> GetEnumerator()
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

        public struct FastEnumerableSurrogate : IEnumerable<EntryRange2ListLong>
        {
            private readonly RedBlackTreeRange2ListLong tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly long startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

            // Construction

            public FastEnumerableSurrogate(RedBlackTreeRange2ListLong tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
                this.side = Side.X;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerableSurrogate(RedBlackTreeRange2ListLong tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;
            }

            // IEnumerable

            public IEnumerator<EntryRange2ListLong> GetEnumerator()
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
        /// it keeps a current key and uses NearestGreater to find the next one. However, since it uses queries it
        /// is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator :
            IEnumerator<EntryRange2ListLong>        {
            private readonly RedBlackTreeRange2ListLong tree;
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
            private uint enumeratorVersion;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private long currentStart;
            //
            // saving the currentXStart with does not work well for range collections because it may shift, so making updates
            // is not permitted in range trees
            [Feature(Feature.Range, Feature.Range2)]
            private uint treeVersion;

            public RobustEnumerator(RedBlackTreeRange2ListLong tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerator(RedBlackTreeRange2ListLong tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
                this.side = side;

                Reset();
            }

            public EntryRange2ListLong Current
            {
                get
                {
                    /*[Feature(Feature.Range, Feature.Range2)]*/
                    if (this.treeVersion != tree.version)
                    {
                        throw new InvalidOperationException();
                    }

                    if (valid)
                        /*[Feature(Feature.Range, Feature.Range2)]*/
                        {
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
                                /*[Feature(Feature.Range2)]*/out yLength);
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

                            return new EntryRange2ListLong(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength,
                                /*[Feature(Feature.Range2)]*/yStart,
                                /*[Feature(Feature.Range2)]*/yLength);
                        }
                    return new EntryRange2ListLong();
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

                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);

                if (!started)
                {

                    

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
                this.treeVersion = tree.version;
                this.enumeratorVersion = unchecked(this.enumeratorVersion + 1);
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        public class FastEnumerator :
            IEnumerator<EntryRange2ListLong>        {
            private readonly RedBlackTreeRange2ListLong tree;
            private readonly bool forward;

            private readonly bool startKeyedOrIndexed;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly long startStart;
            [Feature(Feature.Range2)]
            private readonly Side side;

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
            [Feature(Feature.Range2)]
            [Widen]
            private long currentYStart, nextYStart, previousYStart;

            private STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>[] stack;
            private int stackIndex;

            public FastEnumerator(RedBlackTreeRange2ListLong tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerator(RedBlackTreeRange2ListLong tree,[Widen] long startStart,[Feature(Feature.Range2)] [Const(Side.X, Feature.Range)] [SuppressConst(Feature.Range2)] Side side, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startStart = startStart;
                this.side = side;

                Reset();
            }

            public EntryRange2ListLong Current
            {
                get
                {
                    if (currentNode != Null)
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

                        return new EntryRange2ListLong(
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXLength,
                            /*[Feature(Feature.Range2)]*/currentYStart,
                            /*[Feature(Feature.Range2)]*/currentYLength);
                    }
                    return new EntryRange2ListLong();
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
                return currentNode != Null;
            }

            public void Reset()
            {
                unchecked
                {
                    int stackSize = EstimateMaxDepth(tree.count);
                    if ((stack == null) || (stackSize > stack.Length))
                    {
                        stack = new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>[
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
                    while (node != Null)
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
                                
                                /*[Feature(Feature.Range2)]*/
                                c = startStart.CompareTo(side == Side.X ? xPosition : yPosition);
                            }
                        }

                        if (!foundMatch1 && (forward && (c <= 0)) || (!forward && (c >= 0)))
                        {
                            stack[stackIndex++] = new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>(
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

                    STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long> cursor
                        = stack[--stackIndex];

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;
                    nextYStart = cursor.Item3;

                    Node node = forward ? leadingNode.right : leadingNode.left;
                    /*[Widen]*/
                    long xPosition = nextXStart ;
                    /*[Widen]*/
                    long yPosition = nextYStart ;
                    while (node != Null)
                    {
                        xPosition += node.xOffset;
                        yPosition += node.yOffset;

                        stack[stackIndex++] = new STuple<Node,/*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long,/*[Feature(Feature.Range2)]*//*[Widen]*/long>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition,
                            /*[Feature(Feature.Range2)]*/yPosition);
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
            return new RedBlackTreeRange2ListLong(this);
        }
    }
}
