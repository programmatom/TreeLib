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
    /// Represents an sequenced collection of ranges (without associated values). Each range is defined by it's length, and occupies
    /// a particular position in the sequence, determined by the location where it was inserted (and any insertions/deletions that
    /// have occurred before or after it in the sequence). The start indices of each range are determined as follows:
    /// The first range in the sequence starts at 0 and each subsequent range starts at the starting index of the previous range
    /// plus the length of the previous range. The 'extent' of the range collection is the sum of all lengths.
    /// All ranges must have a length of at least 1.
    /// </summary>
    public class AVLTreeRangeListLong:
        /*[Feature(Feature.Range)]*//*[Payload(Payload.None)]*//*[Widen]*/IRangeListLong,
        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspectionLong,
        IEnumerable<EntryRangeListLong>,
        IEnumerable,

        ICloneable
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

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;
        }

        [Storage(Storage.Object)]
        private readonly static Node _Null = null;

        //
        // State for both array & object form
        //

        private Node Null { get { return AVLTreeRangeListLong._Null; } } // allow tree.Null or this.Null in all cases

        private Node root;
        [Count]
        private ulong count;
        private ushort version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private const int MAX_GTREE_HEIGHT = 40; // TODO: not valid for greater than 32 bits addressing
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
        public AVLTreeRangeListLong(uint capacity,AllocationMode allocationMode)
        {
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        /// <summary>
        /// Create a new collection based on an AVL tree, with default allocation options and allocation mode and using
        /// the default comparer (applicable only to keyed collections).
        /// </summary>
        [Storage(Storage.Object)]
        public AVLTreeRangeListLong()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Object)]
        public AVLTreeRangeListLong(AVLTreeRangeListLong original)
        {

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
            this.allocateHelper.allocateCount = original.allocateHelper.allocateCount;
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
                    allocateHelper.allocateCount = 0;
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
        public bool Contains([Widen] long start)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            return FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition));
        }

        
        /// <summary>
        /// Attempt to insert a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <returns>true if the range was successfully inserted</returns>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] long start,[Widen] long xLength)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (xLength <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return g_tree_insert_internal(
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
        public bool TryDelete([Widen] long start)
        {
            return g_tree_remove_internal(
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
        public bool TryGetLength([Widen] long start,[Widen] out long length)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            if (FindPosition(
start,
out node,
out xPosition,
out xLength)
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
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] long start,[Widen] long length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            if (FindPosition(
start,
out node,
out xPosition,
out xLength)
                && (start == (xPosition)))
            {
                /*[Widen]*/
                long adjust = length - (xLength) ;
                /*[Widen]*/
                long xAdjust = 0 ;
                {
                    xAdjust = adjust;
                }

                this.xExtent = checked(this.xExtent + xAdjust);

                ShiftRightOfPath(start + 1, xAdjust);

                return true;
            }
            return false;
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

        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySet([Widen] long start,[Widen] long xLength)
        {
            if (xLength < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            long xPosition, xLengthOld ;
            if (FindPosition(start, out node, out xPosition, out xLengthOld)
                && (start == (xPosition)))
            {
                /*[Widen]*/
                long xAdjust = xLength != 0 ? xLength - xLengthOld : 0 ;

                this.xExtent = checked(this.xExtent + xAdjust);

                ShiftRightOfPath(start + 1, xAdjust);

                return true;
            }
            return false;
        }

        
        /// <summary>
        /// Inserts a range of a given length at the specified start index.
        /// If the range can't be inserted, the collection is left unchanged. In order to insert at the specified start
        /// index, there must be an existing range starting at that index (where the new range will be inserted immediately
        /// before the existing range at that start index), or the index must be equal to the extent of
        /// the collection (wherein the range will be added at the end of the sequence).
        /// </summary>
        /// <param name="start">starting index to attempt to insert the new range at</param>
        /// <param name="length">length of the new range. The length must be at least 1.</param>
        /// <exception cref="ArgumentException">there is no range starting at the specified index</exception>
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] long start,[Widen] long xLength)
        {
            if (!TryInsert(start, xLength))
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
        public void Delete([Widen] long start)
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
        public long GetLength([Widen] long start)
        {
            /*[Widen]*/
            long length ;
            if (!TryGetLength(
start,
out length))
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
        /// <exception cref="OverflowException">the sum of lengths would have exceeded Int64.MaxValue</exception>
        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] long start,[Widen] long length)
        {
            if (!TrySetLength(start, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] long start,[Widen] out long xLength)
        {
            if (!TryGet(start, out xLength))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Set([Widen] long start,[Widen] long xLength)
        {
            if (!TrySet(start, xLength))
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
        public long GetExtent()
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
        public bool NearestLessOrEqual([Widen] long position,[Widen] out long nearestStart)
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
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessOrEqualByRank", Feature.RankMulti)]
        public bool NearestLessOrEqual([Widen] long position,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
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
        public bool NearestLess([Widen] long position,[Widen] out long nearestStart)
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
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index less than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestLessByRank", Feature.RankMulti)]
        public bool NearestLess([Widen] long position,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            Node nearestNode;
            bool f = NearestLess(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
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
        public bool NearestGreaterOrEqual([Widen] long position,[Widen] out long nearestStart)
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
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than or equal to the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterOrEqualByRank", Feature.RankMulti)]
        public bool NearestGreaterOrEqual([Widen] long position,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                true/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
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
        public bool NearestGreater([Widen] long position,[Widen] out long nearestStart)
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
        /// <param name="length">an out parameter receiving the length of the range that was found</param>
        /// <returns>true if a range was found with a starting index greater than the specified index</returns>
        [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Rename("NearestGreaterByRank", Feature.RankMulti)]
        public bool NearestGreater([Widen] long position,[Widen] out long nearestStart,[Widen] out long xLength)
        {
            xLength = 0;
            Node nearestNode;
            bool f = NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/out nearestStart,
                false/*orEqual*/);
            if (f)
            {
                bool g = TryGet(nearestStart, out xLength);
                Debug.Assert(g);
            }
            return f;
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
        private Node g_tree_node_new()
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
                allocateHelper.allocateCount = checked(allocateHelper.allocateCount + 1);
#endif
            }
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
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == this.count);

            node.left = Null;
            node.left_child = true;
            node.right = Null;
            node.right_child = true;
            node.balance = SByte.MinValue;
            node.xOffset = Int32.MinValue;
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

        private bool NearestLess(            out Node nearestNode,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                    {
                        /*[Widen]*/
                        long xPosition = 0 ;
                        /*[Widen]*/
                        long yPosition = 0 ;
                        while (true)
                        {
                            xPosition += node.xOffset;

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
                            if (next == Null)
                            {
                                break;
                            }
                            node = next;
                        }
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
        }

        private bool NearestGreater(            out Node nearestNode,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                    if (node != Null)
                    {
                        /*[Widen]*/
                        long xPosition = 0 ;
                        /*[Widen]*/
                        long yPosition = 0 ;
                        while (true)
                        {
                            xPosition += node.xOffset;

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
                            if (next == Null)
                            {
                                break;
                            }
                            node = next;
                        }
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
        private bool g_tree_insert_internal(            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xLength,            bool add,            bool update)
        {
            unchecked
            {
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

                    root = g_tree_node_new();
                    Debug.Assert(root.xOffset == 0);
                    Debug.Assert(this.xExtent == 0);
                    this.xExtent = xLength;

                    Debug.Assert(this.count == 0);
                    this.count = 1;
                    this.version = unchecked((ushort)(this.version + 1));

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
                        cmp = position.CompareTo(xPositionNode);
                        if (add && (cmp == 0))
                        {
                            cmp = -1; // node never found for sparse range mode
                        }
                    }

                    if (cmp == 0)
                    {
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

                    {
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                        /*[Widen]*/
                        long positionNode = xPositionNode ;
                        if (position != positionNode)
                        {
                            return false;
                        }
                    }

                    this.version = unchecked((ushort)(this.version + 1));

                    // throw here before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Node child = g_tree_node_new();

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
                    if (position != (xPositionNode + xLengthNode))
                    {
                        return false;
                    }

                    this.version = unchecked((ushort)(this.version + 1));

                    // throw here before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Node child = g_tree_node_new();

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

        private bool g_tree_remove_internal(            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position)
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
                        cmp = position.CompareTo(xPositionNode);
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

                this.version = unchecked((ushort)(this.version + 1));

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
                        int old_idx = idx + 1;
                        idx++;
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
                this.version = unchecked((ushort)(this.version + 1));

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
        [ExcludeFromCodeCoverage]
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

                    if ((offset < leftEdge) || (offset >= rightEdge))
                    {
                        throw new InvalidOperationException("range containment invariant");
                    }

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
            object value = null;
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
            return n.balance;
        }

        /// <summary>
        /// INonInvasiveTreeInspection.Validate() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [ExcludeFromCodeCoverage]
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

                    if (visited.ContainsKey(node))
                    {
                        throw new InvalidOperationException("cycle");
                    }
                    visited.Add(node, false);

                    if (node.left_child)
                    {
                        worklist.Enqueue(node.left);
                    }
                    if (node.right_child)
                    {
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.MultiRank, Feature.Range, Feature.Range2)]*/
            ValidateRanges();

            g_tree_node_check(root);
            ValidateDepthInvariant();
        }

        [ExcludeFromCodeCoverage]
        private void ValidateDepthInvariant()
        {
            const double phi = 1.618033988749894848204;
            const double epsilon = .001;

            double max = Math.Log((count + 2) * Math.Sqrt(5)) / Math.Log(phi) - 2;
            int depth = root != Null ? MaxDepth(root) : 0;
            if (depth > max + epsilon)
            {
                throw new InvalidOperationException("max depth invariant");
            }
        }

        [ExcludeFromCodeCoverage]
        private int MaxDepth(Node node)
        {
            int ld = node.left_child ? MaxDepth(node.left) : 0;
            int rd = node.right_child ? MaxDepth(node.right) : 0;
            return 1 + Math.Max(ld, rd);
        }

        [ExcludeFromCodeCoverage]
        private void g_tree_node_check(Node node)
        {
            if (node != Null)
            {
                if (node.left_child)
                {
                    Node tmp = g_tree_node_previous(node);
                    if (!(tmp.right == node))
                    {
                        Debug.Assert(false, "program defect");
                        throw new InvalidOperationException("invariant");
                    }
                }

                if (node.right_child)
                {
                    Node tmp = g_tree_node_next(node);
                    if (!(tmp.left == node))
                    {
                        Debug.Assert(false, "program defect");
                        throw new InvalidOperationException("invariant");
                    }
                }

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

                int balance = right_height - left_height;
                if (!(balance == node.balance))
                {
                    Debug.Assert(false, "program defect");
                    throw new InvalidOperationException("invariant");
                }

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

        [ExcludeFromCodeCoverage]
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

        // INonInvasiveRange2MapInspection

        /// <summary>
        /// INonInvasiveRange2MapInspection.GetRanges() is a diagnostic method intended to be used ONLY for validation of trees
        /// during unit testing. It is not intended for consumption by users of the library and there is no
        /// guarrantee that it will be supported in future versions.
        /// </summary>
        [Feature(Feature.Range, Feature.Range2)]
        [ExcludeFromCodeCoverage]
        [Widen]
        Range2MapEntryLong[] INonInvasiveRange2MapInspectionLong.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntryLong[] ranges = new Range2MapEntryLong[Count];
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
                    stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long>(node, xOffset, yOffset));
                    node = node.left_child ? node.left : Null;
                }
                while (stack.Count != 0)
                {
                    STuple<Node,/*[Widen]*/long,/*[Widen]*/long> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;
                    yOffset = t.Item3;

                    object value = null;

                    /*[Widen]*/
                    ranges[i++] = new Range2MapEntryLong(new RangeLong(xOffset, 0), new RangeLong(yOffset, 0), value);

                    node = node.right_child ? node.right : Null;
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long>(node, xOffset, yOffset));
                        node = node.left_child ? node.left : Null;
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
        [ExcludeFromCodeCoverage]
        void INonInvasiveRange2MapInspectionLong.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

        /// <summary>
        /// Get the default enumerator, which is the fast enumerator for AVL trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryRangeListLong> GetEnumerator()
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
        public IEnumerable<EntryRangeListLong> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRangeListLong>
        {
            private readonly AVLTreeRangeListLong tree;

            public RobustEnumerableSurrogate(AVLTreeRangeListLong tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRangeListLong> GetEnumerator()
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
        /// deletion and will throw an InvalidOperationException when next advanced. The complexity of the fast enumerator
        /// is O(1) per element, or O(N) to enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryRangeListLong> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryRangeListLong>
        {
            private readonly AVLTreeRangeListLong tree;

            public FastEnumerableSurrogate(AVLTreeRangeListLong tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRangeListLong> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryRangeListLong>
        {
            private readonly AVLTreeRangeListLong tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(AVLTreeRangeListLong tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRangeListLong Current
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
                            /*[Widen]*/
                            long xStart = 0, xLength = 0 ;
                            xStart = currentXStart;

                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength);

                            return new EntryRangeListLong(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength);
                        }
                    return new EntryRangeListLong();
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
                    valid = tree.NearestGreater(currentXStart, out currentXStart);
                }

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
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        public class FastEnumerator : IEnumerator<EntryRangeListLong>
        {
            private readonly AVLTreeRangeListLong tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart, nextXStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>>();

            public FastEnumerator(AVLTreeRangeListLong tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRangeListLong Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {

                        return new EntryRangeListLong(
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart);
                    }
                    return new EntryRangeListLong();
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
                nextXStart = 0;

                this.version = tree.version;

                PushSuccessor(
                    tree.root,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/0);

                Advance();
            }

            private void PushSuccessor(                Node node,                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xPosition)
            {
                while (node != tree.Null)
                {
                    xPosition += node.xOffset;

                    stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
                        node,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
                    node = node.left_child ? node.left : tree.Null;
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

                nextNode = tree.Null;
                nextXStart = tree.xExtent;

                if (stack.Count == 0)
                {
                    nextXStart = tree.xExtent;
                    return;
                }

                STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;
                nextXStart = cursor.Item2;

                if (nextNode.right_child)
                {
                    PushSuccessor(
                        nextNode.right,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart);
                }
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new AVLTreeRangeListLong(this);
        }
    }
}