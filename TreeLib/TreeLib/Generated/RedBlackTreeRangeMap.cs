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
    /// Represents an sequenced collection of ranges with associated values. Each range is defined by it's length, and occupies
    /// a particular position in the sequence, determined by the location where it was inserted (and any insertions/deletions that
    /// have occurred before or after it in the sequence). The start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// All ranges must have a length of at least 1.
    /// </summary>
    /// <typeparam name="ValueType">type of the value associated with each range</typeparam>
    public class RedBlackTreeRangeMap<[Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Range)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRangeMap<ValueType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspection,

        IEnumerable<EntryRangeMap<ValueType>>,
        IEnumerable,
        ITreeEnumerable<EntryRangeMap<ValueType>>,
        /*[Feature(Feature.Range)]*//*[Widen]*/IIndexedTreeEnumerable<EntryRangeMap<ValueType>>,

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
            [Payload(Payload.Value)]
            public ValueType value;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;
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
        private int xExtent;

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
        public RedBlackTreeRangeMap(uint capacity, AllocationMode allocationMode)
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
        public RedBlackTreeRangeMap()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on a red-blacck tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public RedBlackTreeRangeMap(RedBlackTreeRangeMap<ValueType> original)
        {

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
                    nodeThis.value = nodeOriginal.value;
                    nodeThis.xOffset = nodeOriginal.xOffset;
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
        /// Returns the number of ranges in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue ranges.</exception>
        public uint Count { get { return checked((uint)this.count); } }

        
        /// <summary>
        /// Returns the number of ranges in the collection.
        /// </summary>
        public long LongCount { get { return unchecked((long)this.count); } }

        
        /// <summary>
        /// Removes all ranges from the collection.
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
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        
        /// <summary>
        /// Determines if there is a range in the collection starting at the specified index.
        /// </summary>
        /// <param name="start">index to look for the start of a range at</param>
        /// <returns>true if there is a range starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] int start)
        {
            Node node;
            /*[Widen]*/
            int xPosition, xLength;
            return FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition));
        }

        
        /// <summary>
        /// Attempt to insert a range of a given length at the specified start index and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <param name="value">value to associate with the range</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] int start,[Widen] int xLength,[Payload(Payload.Value)] ValueType value)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (xLength <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return InsertUpdateInternal(
                /*[Payload(Payload.Value)]*/value,
                start,
                xLength,
                true/*add*/,
                false/*update*/);
        }

        
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] int start)
        {
            return DeleteInternal(
                start);
        }

        
        /// <summary>
        /// Attempt to query the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] int start,[Widen] out int length)
        {
            Node node;
            /*[Widen]*/
            int xPosition, xLength;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                length = xLength;
                return true;
            }
            length = 0;
            return false;
        }

        
        /// <summary>
        /// Attempt to change the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <returns>true if a range was found starting at the specified index and updated</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] int start,[Widen] int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            int xPosition, xLength;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                /*[Widen]*/
                int adjust = length - (xLength);
                /*[Widen]*/
                int xAdjust = 0;
                {
                    xAdjust = adjust;
                }

                // throw OverflowException before modifying anything
                /*[Widen]*/
                int newXExtent = checked(this.xExtent + xAdjust);
                this.xExtent = newXExtent;

                ShiftRightOfPath(unchecked(start + 1), xAdjust);

                return true;
            }
            return false;
        }

        
        /// <summary>
        /// Attempt to query the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] int start,out ValueType value)
        {
            Node node;
            /*[Widen]*/
            int xPosition, xLength;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                value = node.value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Attempt to update the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] int start,ValueType value)
        {
            Node node;
            /*[Widen]*/
            int xPosition, xLength;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                node.value = value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Attempt to get the value and length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] int start,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            Node node;
            /*[Widen]*/
            int xPosition;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                value = node.value;
                return true;
            }
            xLength = 0;
            value = default(ValueType);
            return false;
        }

        
        /// <summary>
        /// Attempt to change the length and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <returns>true if a range was found starting at the specified index and updated; false if the
        /// start was not found or the sum of lengths would have exceeded Int32.MaxValue</returns>
        [Exclude(Feature.Range, Payload.None)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySet([Widen] int start,[Widen] int xLength,[Payload(Payload.Value)] ValueType value)
        {
            if (xLength < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            int xPosition, xLengthOld;
            if (FindPosition(start, out node, out xPosition, out xLengthOld)
                && (start == (xPosition)))
            {
                /*[Widen]*/
                int xAdjust = xLength != 0 ? xLength - xLengthOld : 0;

                // throw OverflowException before modifying anything
                /*[Widen]*/
                int newXExtent = checked(this.xExtent + xAdjust);
                this.xExtent = newXExtent;

                ShiftRightOfPath(unchecked(start + 1), xAdjust);

                node.value = value;

                return true;
            }
            return false;
        }

        
        /// <summary>
        /// Inserts a range of a given length at the specified start index and with an associated value.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <param name="value">value to associate with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] int start,[Widen] int xLength,[Payload(Payload.Value)] ValueType value)
        {
            if (!TryInsert(start, xLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        
        /// <summary>
        /// Attempt to delete the range starting at the specified index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of a range to attempt to delete</param>
        /// <returns>true if a range was successfully deleted</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] int start)
        {
            if (!TryDelete(start))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the length of the range found at the specified start index</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetLength([Widen] int start)
        {
            /*[Widen]*/
            int length;
            if (!TryGetLength(start, out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        
        /// <summary>
        /// Changes the length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] int start,[Widen] int length)
        {
            if (!TrySetLength(start, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <returns>the value associated with the range</returns>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public ValueType GetValue([Widen] int start)
        {
            ValueType value;
            if (!TryGetValue(start, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        
        /// <summary>
        /// Updates the value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="value">new value that replaces the old value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public void SetValue([Widen] int start,ValueType value)
        {
            if (!TrySetValue(start, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the value and length associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to query</param>
        /// <param name="length">out parameter receiving the length of the range</param>
        /// <param name="value">out parameter receiving the value associated with the range</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] int start,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            if (!TryGet(start, out xLength, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Changes the length and value associated with the range starting at the specified start index.
        /// The index must refer to the start of a range; an index to the interior of a range is not permitted.
        /// </summary>
        /// <param name="start">start of the range to update</param>
        /// <param name="length">new length for the range. The length must be at least 1.</param>
        /// <param name="value">the value to replace the old value associated with the range</param>
        /// <exception cref="ArgumentException">the start was not the beginning of a range</exception>
        /// <exception cref="OverflowException">sum of lengths would have exceeded Int32.MaxValue</exception>
        [Exclude(Feature.Range, Payload.None)]
        [Feature(Feature.Range, Feature.Range2)]
        public void Set([Widen] int start,[Widen] int xLength,[Payload(Payload.Value)] ValueType value)
        {
            if (!TrySet(start, xLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the extent of the sequence of ranges. The extent is the sum of the lengths of all the ranges.
        /// </summary>
        /// <returns>the extent of the ranges</returns>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int GetExtent()
        {
            return this.xExtent;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position,[Widen] out int nearestStart)
        {
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than or equal to the specified index.
        /// Use this method to convert an index to the interior of a range into the start index of a range.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// This may be a range starting at the specified index or the range containing the index if the index refers
        /// to the interior of a range.
        /// If the value is greater than or equal to the extent it will return the start of the last range of the collection.
        /// If there are no ranges in the collection or position is less than 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            xLength = 0;
            value = default(ValueType);
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position,[Widen] out int nearestStart)
        {
            Node nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index less than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the specified index is an interior index, the start of the containing range will be returned.
        /// If the index is at the start of a range, the start of the previous range will be returned.
        /// If the value is greater than or equal to the extent it will return the start of last range of the collection.
        /// If there are no ranges in the collection or position is less than or equal to 0, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            xLength = 0;
            value = default(ValueType);
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position,[Widen] out int nearestStart)
        {
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than or equal to the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range, that index will be returned.
        /// If the index refers to the interior index for a range, the start of the next range in the sequence will be returned.
        /// If the index is less than or equal to 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            xLength = 0;
            value = default(ValueType);
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position,[Widen] out int nearestStart)
        {
            Node nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
        }

        
        /// <summary>
        /// Search for the nearest range that starts at an index greater than the specified index.
        /// </summary>
        /// <param name="position">the index to begin searching at</param>
        /// <param name="nearestStart">an out parameter receiving the start index of the range that was found.
        /// If the index refers to the start of a range or is an interior index for a range, the next range in the
        /// sequence will be returned.
        /// If the index is less than 0, the index 0 will be returned, which is the start of the first range.
        /// If the index is greater than or equal to the start of the last range, no range will be found.
        /// </param>
        /// <param name="value">an out parameter receiving the value associated with the range that was found</param>
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] int position,[Widen] out int nearestStart,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            xLength = 0;
            value = default(ValueType);
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength, /*[Payload(Payload.Value)]*/out value);
                Debug.Assert(g);
            }
            return f;
        }

        
        /// <summary>
        /// Adjust the length of the range starting at 'start' by adding 'adjust' to the current length of the
        /// range. If the length would become 0, the range is removed.
        /// </summary>
        /// <param name="start">the start index of the range to adjust</param>
        /// <param name="adjust">the amount to adjust the length by. Value may be negative to shrink the length</param>
        /// <returns>The adjusted length</returns>
        /// <exception cref="ArgumentException">There is no range starting at the index specified by 'start'.</exception>
        /// <exception cref="ArgumentOutOfRangeException">the length would become negative</exception>
        /// <exception cref="OverflowException">the extent would become larger than Int32.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public int AdjustLength([Widen] int startIndex,[Widen] int xAdjust)
        {
            unchecked
            {
                Node node;
                /*[Widen]*/
                int xPosition;
                /*[Widen]*/
                int xLength = 1;
                if (!FindPosition(startIndex, out node, out xPosition, out xLength)
                    || (startIndex != (xPosition)))
                {
                    throw new ArgumentException();
                }

                /*[Widen]*/
                int newXLength = checked(xLength + xAdjust);

                if (newXLength < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (newXLength != 0)
                {
                    // adjust

                    // throw OverflowException before modifying anything
                    /*[Widen]*/
                    int newXExtent = checked(this.xExtent + xAdjust);
                    this.xExtent = newXExtent;

                    ShiftRightOfPath(unchecked(startIndex + 1), xAdjust);

                    return newXLength;
                }
                else
                {
                    // delete

                    Debug.Assert(xAdjust < 0);
                    Debug.Assert(newXLength == 0);

                    DeleteInternal(
                        startIndex);

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
        private Node Allocate([Payload(Payload.Value)] ValueType value, bool isRed)
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
            node.value = value;
            node.left = Null;
            node.right = Null;
            node.isRed = isRed;
            node.xOffset = 0;

            return node;
        }

        [Storage(Storage.Object)]
        private Node Allocate([Payload(Payload.Value)] ValueType value)
        {
            return Allocate(/*[Payload(Payload.Value)]*/value, true/*isRed*/);
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
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,
            bool orEqual)
        {
            Node lastLess = Null;
            /*[Widen]*/
            int xPositionLastLess = 0;
            /*[Widen]*/
            int yPositionLastLess = 0;
            Node node = root;
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
                        xPosition += node.xOffset;
                    }

                    int c;
                    {
                        c = position.CompareTo(xPosition);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestNode = node;
                        nearestStart = xPosition;
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
                nearestStart = xPositionLastLess;
                return true;
            }
            nearestNode = Null;
            nearestStart = 0;
            return false;
        }

        private bool NearestGreater(
            out Node nearestNode,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,
            bool orEqual)
        {
            Node lastGreater = Null;
            /*[Widen]*/
            int xPositionLastGreater = 0;
            /*[Widen]*/
            int yPositionLastGreater = 0;
            Node node = root;
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
                        xPosition += node.xOffset;
                    }

                    int c;
                    {
                        c = position.CompareTo(xPosition);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestNode = node;
                        nearestStart = xPosition;
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
                nearestStart = xPositionLastGreater;
                return true;
            }
            nearestNode = Null;
            nearestStart = this.xExtent;
            return false;
        }

        // Searches tree for key location.
        // If key is not present and add==true, node is inserted.
        // If key is preset and update==true, value is replaced.
        // Returns true if a node was added or if add==false and a node was updated.
        // NOTE: update mode does *not* adjust for xLength/yLength!
        private bool InsertUpdateInternal(
            [Payload(Payload.Value)] ValueType value,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,
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

                root = Allocate(/*[Payload(Payload.Value)]*/value, false);
                Debug.Assert(root.xOffset == 0);
                Debug.Assert(this.xExtent == 0);
                this.xExtent = xLength;

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
                int xPositionCurrent = 0;
                Node parent = Null;
                Node grandParent = Null;
                Node greatGrandParent = Null;

                Node successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;

                //even if we don't actually add to the set, we may be altering its structure (by doing rotations
                //and such). so update version to disable any enumerators/subsets working on it
                this.version = unchecked(this.version + 1);

                int order = 0;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParent = 0;
                while (current != Null)
                {
                    unchecked
                    {
                        xPositionCurrent += current.xOffset;
                    }

                    {
                        order = position.CompareTo(xPositionCurrent);
                        if (add && (order == 0))
                        {
                            order = -1; // node never found for sparse range mode
                        }
                    }

                    if (order == 0)
                    {

                        if (update)
                        {
                            current.value = value;
                        }

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
                    if (order < 0)
                    {
                        successor = parent;
                        xPositionSuccessor = xPositionParent;

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
                    int xLengthParent;
                    if (successor != Null)
                    {
                        xLengthParent = xPositionSuccessor - xPositionParent;
                    }
                    else
                    {
                        xLengthParent = this.xExtent - xPositionParent;
                    }

                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    if (position != unchecked(xPositionParent + xLengthParent))
                    {
                        root.isRed = false;
                        return false;
                    }

                    // compute here to throw before modifying tree
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    node = Allocate(/*[Payload(Payload.Value)]*/value);

                    ShiftRightOfPath(unchecked(xPositionParent + 1), xLength);

                    parent.right = node;

                    node.xOffset = xLengthParent;

                    this.xExtent = xExtentNew;
                    this.count = countNew;
                }
                else
                {
                    // precedes parent

                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        int positionParent = xPositionParent;
                        if (position != positionParent)
                        {
                            root.isRed = false;
                            return false;
                        }
                    }

                    // compute here to throw before modifying tree
                    /*[Widen]*/
                    int xExtentNew = checked(this.xExtent + xLength);
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Debug.Assert(parent == successor);

                    node = Allocate(/*[Payload(Payload.Value)]*/value);

                    ShiftRightOfPath(xPositionParent, xLength);

                    parent.left = node;

                    node.xOffset = unchecked(-xLength);

                    this.xExtent = xExtentNew;
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
            [Widen] int position,
            [Widen] int xAdjust)
        {
            unchecked
            {
                /*[Widen]*/
                int xPositionCurrent = 0;
                Node current = root;
                while (current != Null)
                {
                    xPositionCurrent += current.xOffset;

                    int order = position.CompareTo(xPositionCurrent);
                    if (order <= 0)
                    {
                        xPositionCurrent += xAdjust;
                        current.xOffset += xAdjust;
                        if (current.left != Null)
                        {
                            current.left.xOffset -= xAdjust;
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
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position)
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
                int xPositionCurrent = 0;

                Node parent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParent = 0;

                Node grandParent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionGrandparent = 0;

                Node match = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionMatch = 0;

                Node parentOfMatch = Null;

                bool foundMatch = false;

                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionLastGreaterAncestor = 0;
                while (current != Null)
                {
                    xPositionCurrent += current.xOffset;

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
                            int xPositionSibling = xPositionParent + sibling.xOffset;
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
                                if (parent == match)
                                {
                                    parentOfMatch = sibling;
                                }

                                // update sibling, this is necessary for following processing
                                sibling = (parent.left == current) ? parent.right : parent.left;
                                xPositionSibling += sibling.xOffset;
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
                                int xPositionNewGrandparent = xPositionParent - parent.xOffset;
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
                            order = position.CompareTo(xPositionCurrent);
                        }

                    if (order == 0)
                    {

                        // save the matching node
                        foundMatch = true;
                        match = current;
                        parentOfMatch = parent;

                        xPositionMatch = xPositionCurrent;
                    }

                    grandParent = parent;
                    parent = current;

                    xPositionGrandparent = xPositionParent;
                    xPositionParent = xPositionCurrent;

                    if (order < 0)
                    {
                        if (!foundMatch)
                        {
                            lastGreaterAncestor = current;
                            xPositionLastGreaterAncestor = xPositionCurrent;
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
                    int xLength;
                    Debug.Assert(parent != Null);
                    if (parent != match)
                    {
                        xLength = xPositionParent - xPositionMatch;
                    }
                    else if (lastGreaterAncestor != Null)
                    {
                        xLength = xPositionLastGreaterAncestor - xPositionMatch;
                    }
                    else
                    {
                        xLength = this.xExtent - xPositionMatch;
                    }

                    ReplaceNode(
                        match,
                        parentOfMatch,
                        parent/*successor*/,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPositionParent - xPositionMatch,
                        grandParent/*parentOfSuccessor*/);

                    ShiftRightOfPath(xPositionMatch + 1, -xLength);

                    this.xExtent = unchecked(this.xExtent - xLength);
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
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xOffsetMatchSuccessor,
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
                        }
                        successor.right = match.right;
                        if (match.right != Null)
                        {
                            match.right.xOffset -= xOffsetMatchSuccessor;
                        }
                    }

                    successor.left = match.left;
                    if (match.left != Null)
                    {
                        match.left.xOffset -= xOffsetMatchSuccessor;
                    }
                }

                if (successor != Null)
                {
                    successor.isRed = match.isRed;

                    successor.xOffset = match.xOffset + xOffsetMatchSuccessor;
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
            [Widen] int position,
            out Node lastLessEqual,
            [Widen] out int xPositionLastLessEqual,
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int xLength)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;
                Node successor = Null;
                xLength = 0;

                Node current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionCurrent = 0;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;
                while (current != Null)
                {
                    xPositionCurrent += current.xOffset;

                    if (position < (xPositionCurrent))
                    {
                        successor = current;
                        xPositionSuccessor = xPositionCurrent;

                        current = current.left;
                    }
                    else
                    {
                        lastLessEqual = current;
                        xPositionLastLessEqual = xPositionCurrent;

                        current = current.right; // try to find successor
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

        private Node RotateLeft(Node node)
        {
            unchecked
            {
                Node r = node.right;

                if (r.left != Null)
                {
                    r.left.xOffset += r.xOffset;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetR = r.xOffset;
                r.xOffset += node.xOffset;
                node.xOffset = -xOffsetR;

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
                int xOffsetNode = node.xOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetLChild = lChild.xOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetLRGrandchild = lrGrandChild.xOffset;

                lrGrandChild.xOffset = xOffsetLRGrandchild + xOffsetLChild + xOffsetNode;
                lChild.xOffset = -xOffsetLRGrandchild;
                node.xOffset = -xOffsetLRGrandchild - xOffsetLChild;
                if (lrGrandChild.left != Null)
                {
                    lrGrandChild.left.xOffset += xOffsetLRGrandchild;
                }
                if (lrGrandChild.right != Null)
                {
                    lrGrandChild.right.xOffset += xOffsetLRGrandchild + xOffsetLChild;
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
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetL = l.xOffset;
                l.xOffset += node.xOffset;
                node.xOffset = -xOffsetL;

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
                int xOffsetNode = node.xOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetRChild = rChild.xOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetRLGrandchild = rlGrandChild.xOffset;

                rlGrandChild.xOffset = xOffsetRLGrandchild + xOffsetRChild + xOffsetNode;
                rChild.xOffset = -xOffsetRLGrandchild;
                node.xOffset = -xOffsetRLGrandchild - xOffsetRChild;
                if (rlGrandChild.left != Null)
                {
                    rlGrandChild.left.xOffset += xOffsetRLGrandchild + xOffsetRChild;
                }
                if (rlGrandChild.right != Null)
                {
                    rlGrandChild.right.xOffset += xOffsetRLGrandchild;
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
        private void ValidateRanges()
        {
            if (root != Null)
            {
                Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int offset = 0;
                /*[Widen]*/
                int leftEdge = 0;
                /*[Widen]*/
                int rightEdge = this.xExtent;

                Node node = root;
                while (node != Null)
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

                    Check.Assert((offset >= leftEdge) && (offset < rightEdge), "range containment invariant");

                    leftEdge = offset + 1;
                    node = node.right;
                    while (node != Null)
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
            ValidateRanges();

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
        Range2MapEntry[] /*[Widen]*/INonInvasiveRange2MapInspection.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntry[] ranges = new /*[Widen]*/Range2MapEntry[Count];
            int i = 0;

            if (root != Null)
            {
                Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;
                /*[Widen]*/
                int yOffset = 0;

                Node node = root;
                while (node != Null)
                {
                    xOffset += node.xOffset;
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

                    ranges[i++] = new /*[Widen]*/Range2MapEntry(new /*[Widen]*/Range(xOffset, 0), new /*[Widen]*/Range(yOffset, 0), value);

                    node = node.right;
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int, /*[Widen]*/int>(node, xOffset, yOffset));
                        node = node.left;
                    }
                }
                Check.Assert(i == ranges.Length, "count invariant");

                for (i = 1; i < ranges.Length; i++)
                {
                    Check.Assert(ranges[i - 1].x.start < ranges[i].x.start, "range sequence invariant (X)");
                    ranges[i - 1].x.length = ranges[i].x.start - ranges[i - 1].x.start;
                }

                ranges[i - 1].x.length = this.xExtent - ranges[i - 1].x.start;
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


        //
        // IEnumerable
        //

        /// <summary>
        /// Get the default enumerator, which is the fast enumerator for red-black trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
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
        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the default enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the default enumerator</returns>
        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable(bool forward)
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
        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable(bool forward)
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
        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable(bool forward)
        {
            return new FastEnumerableSurrogate(this, forward);
        }

        //
        // IIndexedTreeEnumerable/IIndexed2TreeEnumerable
        //

        
        /// <summary>
        /// Create a new instance of the default enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the default enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable([Widen] int startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/); // default
        }

        
        /// <summary>
        /// Create a new instance of the default enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration
        /// starts as follows: for forward enumeration, the range that follows; for reverse enumeration, the range containing
        /// the specified index</param>
        /// <param name="forward">True to move from ranges in order of increasing start indexes; False to move backwards
        /// from the last range through decreasing start indexes</param>
        /// <returns>A new instance of the default enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeMap<ValueType>> GetEnumerable([Widen] int startAt,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward); // default
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the fast enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable([Widen] int startAt)
        {
            return new FastEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the fast enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration
        /// starts as follows: for forward enumeration, the range that follows; for reverse enumeration, the range containing
        /// the specified index</param>
        /// <param name="forward">True to move from ranges in order of increasing start indexes; False to move backwards
        /// from the last range through decreasing start indexes</param>
        /// <returns>A new instance of the fast enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeMap<ValueType>> GetFastEnumerable([Widen] int startAt,bool forward)
        {
            return new FastEnumerableSurrogate(this, startAt, forward);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator traversing in the specified direction.
        /// </summary>
        /// <param name="forward">True to move from first to last in sort order; False to move backwards, from last to first, in sort order</param>
        /// <returns>A new instance of the robust enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable([Widen] int startAt)
        {
            return new RobustEnumerableSurrogate(this, startAt, true/*forward*/);
        }

        
        /// <summary>
        /// Create a new instance of the robust enumerator, starting the enumeration at the specified index.
        /// </summary>
        /// <param name="startAt">The index to start enumeration at. If the index is interior to a range, enumeration
        /// starts as follows: for forward enumeration, the range that follows; for reverse enumeration, the range containing
        /// the specified index</param>
        /// <param name="forward">True to move from ranges in order of increasing start indexes; False to move backwards
        /// from the last range through decreasing start indexes</param>
        /// <returns>A new instance of the robust enumerator</returns>
        [Feature(Feature.Range, Feature.Range2)]
        public IEnumerable<EntryRangeMap<ValueType>> GetRobustEnumerable([Widen] int startAt,bool forward)
        {
            return new RobustEnumerableSurrogate(this, startAt, forward);
        }

        //
        // Surrogates
        //

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRangeMap<ValueType>>
        {
            private readonly RedBlackTreeRangeMap<ValueType> tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            // Construction

            public RobustEnumerableSurrogate(RedBlackTreeRangeMap<ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerableSurrogate(RedBlackTreeRangeMap<ValueType> tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
            }

            // IEnumerable

            public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
            {

                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (startIndexed)
                {
                    return new RobustEnumerator(tree, startStart, forward);
                }

                return new RobustEnumerator(tree, forward);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryRangeMap<ValueType>>
        {
            private readonly RedBlackTreeRangeMap<ValueType> tree;
            private readonly bool forward;

            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            // Construction

            public FastEnumerableSurrogate(RedBlackTreeRangeMap<ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = false;
                this.startStart = 0;
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerableSurrogate(RedBlackTreeRangeMap<ValueType> tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;
            }

            // IEnumerable

            public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
            {

                /*[Feature(Feature.Range, Feature.Range2)]*/
                if (startIndexed)
                {
                    return new FastEnumerator(tree, startStart, forward);
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
            IEnumerator<EntryRangeMap<ValueType>>,
            /*[Payload(Payload.Value)]*/ISetValue<ValueType>
        {
            private readonly RedBlackTreeRangeMap<ValueType> tree;
            private readonly bool forward;
            //
            [Feature(Feature.Range, Feature.Range2)]
            private readonly bool startIndexed;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            private bool started;
            private bool valid;
            private uint enumeratorVersion;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private int currentStart;
            //
            // saving the currentXStart with does not work well for range collections because it may shift, so making updates
            // is not permitted in range trees
            [Feature(Feature.Range, Feature.Range2)]
            private uint treeVersion;

            public RobustEnumerator(RedBlackTreeRangeMap<ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public RobustEnumerator(RedBlackTreeRangeMap<ValueType> tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startIndexed = true;
                this.startStart = startStart;

                Reset();
            }

            public EntryRangeMap<ValueType> Current
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
                            ValueType value = default(ValueType);
                            /*[Widen]*/
                            int xStart = 0, xLength = 0;

                            /*[Feature(Feature.Range)]*/
                            xStart = currentStart;
                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/currentStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength,
                                /*[Payload(Payload.Value)]*/out value);

                            return new EntryRangeMap<ValueType>(
                                /*[Payload(Payload.Value)]*/value,
                                /*[Payload(Payload.Value)]*/this,
                                /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength);
                        }
                    return new EntryRangeMap<ValueType>();
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
                                /*[Feature(Feature.Range)]*/
                                valid = tree.NearestLess(tree.xExtent, out currentStart);
                            }
                        }
                        else
                        {
                            if (forward)
                            {
                                valid = tree.NearestGreaterOrEqual(startStart, out currentStart);
                            }
                            else
                            {
                                valid = tree.NearestLessOrEqual(startStart, out currentStart);
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
                        valid = tree.NearestGreater(currentStart, out currentStart);
                    }
                    else
                    {
                        valid = tree.NearestLess(currentStart, out currentStart);
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
                
                /*[Feature(Feature.Range, Feature.Range2)]*/
                tree.SetValue(currentStart, value);
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        public class FastEnumerator :
            IEnumerator<EntryRangeMap<ValueType>>,
            /*[Payload(Payload.Value)]*/ISetValue<ValueType>
        {
            private readonly RedBlackTreeRangeMap<ValueType> tree;
            private readonly bool forward;

            private readonly bool startKeyedOrIndexed;
            //
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private readonly int startStart;

            private uint treeVersion;
            private uint enumeratorVersion;

            private Node currentNode;
            private Node leadingNode;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;
            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int previousXStart;

            private STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>[] stack;
            private int stackIndex;

            public FastEnumerator(RedBlackTreeRangeMap<ValueType> tree, bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                Reset();
            }

            [Feature(Feature.Range, Feature.Range2)]
            public FastEnumerator(RedBlackTreeRangeMap<ValueType> tree,[Widen] int startStart,bool forward)
            {
                this.tree = tree;
                this.forward = forward;

                this.startKeyedOrIndexed = true;
                this.startStart = startStart;

                Reset();
            }

            public EntryRangeMap<ValueType> Current
            {
                get
                {
                    if (currentNode != Null)
                    {


                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        int currentXLength = 0;

                        /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        if (forward)
                        {
                            currentXLength = nextXStart - currentXStart;
                        }
                        else
                        {
                            currentXLength = previousXStart - currentXStart;
                        }

                        return new EntryRangeMap<ValueType>(
                            /*[Payload(Payload.Value)]*/currentNode.value,
                            /*[Payload(Payload.Value)]*/this,
                            /*[Payload(Payload.Value)]*/this.enumeratorVersion,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXLength);
                    }
                    return new EntryRangeMap<ValueType>();
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
                        stack = new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>[
                            stackSize];
                    }
                    stackIndex = 0;

                    currentNode = Null;
                    leadingNode = Null;

                    this.treeVersion = tree.version;

                    // push search path to starting item

                    Node node = tree.root;
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
                                
                                /*[Feature(Feature.Range)]*/
                                c = startStart.CompareTo(xPosition);
                            }
                        }

                        if (!foundMatch1 && (forward && (c <= 0)) || (!forward && (c >= 0)))
                        {
                            stack[stackIndex++] = new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>(
                                node,
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition);
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

                    STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int> cursor
                        = stack[--stackIndex];

                    leadingNode = cursor.Item1;
                    nextXStart = cursor.Item2;

                    Node node = forward ? leadingNode.right : leadingNode.left;
                    /*[Widen]*/
                    int xPosition = nextXStart;
                    while (node != Null)
                    {
                        xPosition += node.xOffset;

                        stack[stackIndex++] = new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>(
                            node,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition);
                        node = forward ? node.left : node.right;
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

                currentNode.value = value;
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new RedBlackTreeRangeMap<ValueType>(this);
        }
    }
}
