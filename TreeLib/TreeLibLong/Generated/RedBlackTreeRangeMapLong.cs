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

// Based on the .NET Framework Base Class Libarary implementation of red-black trees from here:
// https://github.com/dotnet/corefx/blob/master/src/System.Collections/src/System/Collections/Generic/SortedSet.cs

// An overview of red-black trees can be found here: https://en.wikipedia.org/wiki/Red%E2%80%93black_tree

namespace TreeLib
{

    public class RedBlackTreeRangeMapLong<[Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Range)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRangeMapLong<ValueType>,
        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspectionLong,
        IEnumerable<EntryRangeMapLong<ValueType>>,
        IEnumerable
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
            public long xOffset;

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

        [Storage(Storage.Object)]
        private readonly static Node _Null = null;

        //
        // State for both array & object form
        //

        private Node Null { get { return RedBlackTreeRangeMapLong<ValueType>._Null; } } // allow tree.Null or this.Null in all cases

        private Node root;
        [Count]
        private ulong count;
        private ushort version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;

        private readonly AllocationMode allocationMode;
        private Node freelist;


        //
        // Construction
        //

        // Object

        [Storage(Storage.Object)]
        public RedBlackTreeRangeMapLong(uint capacity,AllocationMode allocationMode)
        {
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        [Storage(Storage.Object)]
        public RedBlackTreeRangeMapLong()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public RedBlackTreeRangeMapLong(RedBlackTreeRangeMapLong<ValueType> original)
        {
            throw new NotImplementedException(); // TODO: clone
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
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] long start)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            return FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition));
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] long start,[Widen] long xLength,[Payload(Payload.Value)] ValueType value)
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
                /*[Payload(Payload.Value)]*/ value,
                start,
                xLength,
                true/*add*/,
                false/*update*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] long start)
        {
            return DeleteInternal(
                start);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] long start,[Widen] out long length)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                length = xLength;
                return true;
            }
            length = 0;
            return false;
        }

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
            if (FindPosition(start, out node, out xPosition, out xLength)
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

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] long start,out ValueType value)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                value = node.value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] long start,ValueType value)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                node.value = value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] long start,[Widen] out long xLength,[Payload(Payload.Value)] out ValueType value)
        {
            Node node;
            /*[Widen]*/
            long xPosition ;
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

        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] long start,[Widen] long xLength,[Payload(Payload.Value)] ValueType value)
        {
            if (!TryInsert(start, xLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] long start)
        {
            if (!TryDelete(start))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        public long GetLength([Widen] long start)
        {
            /*[Widen]*/
            long length ;
            if (!TryGetLength(start, out length))
            {
                throw new ArgumentException("item not in tree");
            }
            return length;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] long start,[Widen] long length)
        {
            if (!TrySetLength(start, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public ValueType GetValue([Widen] long start)
        {
            ValueType value;
            if (!TryGetValue(start, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public void SetValue([Widen] long start,ValueType value)
        {
            if (!TrySetValue(start, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] long start,[Widen] out long xLength,[Payload(Payload.Value)] out ValueType value)
        {
            if (!TryGet(start, out xLength, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public long GetExtent()
        {
            return this.xExtent;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLessOrEqual([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestLess(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLess([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestLess(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreaterOrEqual([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestGreater(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreater([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestGreater(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
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
        private Node Allocate([Payload(Payload.Value)] ValueType value,bool isRed)
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
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == this.count);

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
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                    }

                    int c;
                    {
                        Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                        c = position.CompareTo(xPosition);
                    }
                    if (orEqual && (c == 0))
                    {
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
                nearestStart = xPositionLastLess;
                return true;
            }
            nearestStart = 0;
            return false;
        }

        private bool NearestGreater(
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                    }

                    int c;
                    {
                        Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                        c = position.CompareTo(xPosition);
                    }
                    if (orEqual && (c == 0))
                    {
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
                nearestStart = xPositionLastGreater;
                return true;
            }
            nearestStart = this.xExtent;
            return false;
        }

        // Searches tree for key location.
        // If key is not present and add==true, node is inserted.
        // If key is preset and update==true, value is replaced.
        // Returns true if a node was added or if add==false and a node was updated.
        // NOTE: update mode does *not* adjust for xLength/yLength!
        private bool InsertUpdateInternal(
            [Payload(Payload.Value)] ValueType value,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xLength,            bool add,            bool update)
        {
            Debug.Assert((CompareKeyMode.Position == CompareKeyMode.Key) || (add != update));

            if (root == Null)
            {
                if (!add)
                {
                    return false;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
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
                this.version = unchecked((ushort)(this.version + 1));

                return true;
            }

            // Search for a node at bottom to insert the new node. 
            // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            Node current = root;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            long xPositionCurrent = 0 ;
            Node parent = Null;
            Node grandParent = Null;
            Node greatGrandParent = Null;

            Node successor = Null;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            long xPositionSuccessor = 0 ;

            //even if we don't actually add to the set, we may be altering its structure (by doing rotations
            //and such). so update version to disable any enumerators/subsets working on it
            this.version = unchecked((ushort)(this.version + 1));

            int order = 0;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            long xPositionParent = 0 ;
            while (current != Null)
            {
                unchecked
                {
                    xPositionCurrent += current.xOffset;
                }

                {
                    Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                    order = position.CompareTo(xPositionCurrent);
                    if (add && (order == 0))
                    {
                        order = -1; // node never found for sparse range mode
                    }
                }

                if (order == 0)
                {
                    // We could have changed root node to red during the search process.
                    // We need to set it to black before we return.
                    root.isRed = false;
                    if (update)
                    {
                        current.value = value;
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
                root.isRed = false;
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
                if (successor != Null)
                {
                    xLengthParent = xPositionSuccessor - xPositionParent;
                }
                else
                {
                    xLengthParent = this.xExtent - xPositionParent;
                }

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                if ((CompareKeyMode.Position == CompareKeyMode.Position)
                    && (position != unchecked(xPositionParent + xLengthParent)))
                {
                    root.isRed = false;
                    return false;
                }

                // compute here to throw before modifying tree
                /*[Widen]*/
                long xExtentNew;
                /*[Count]*/
                ulong countNew;
                try
                {
                    xExtentNew = checked(this.xExtent + xLength);
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    root.isRed = false;
                    throw;
                }

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
                    long positionParent = xPositionParent ;
                    if (position != positionParent)
                    {
                        root.isRed = false;
                        return false;
                    }
                }

                // compute here to throw before modifying tree
                /*[Widen]*/
                long xExtentNew;
                /*[Count]*/
                ulong countNew;
                try
                {
                    xExtentNew = checked(this.xExtent + xLength);
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    root.isRed = false;
                    throw;
                }

                Debug.Assert(parent == successor);

                node = Allocate(/*[Payload(Payload.Value)]*/value);

                ShiftRightOfPath(xPositionParent, xLength);

                parent.left = node;

                node.xOffset = -xLength;

                this.xExtent = xExtentNew;
                this.count = countNew;
            }

            // the new node will be red, so we will need to adjust the colors if parent node is also red
            if (parent.isRed)
            {
                InsertionBalance(node, ref parent, grandParent, greatGrandParent);
            }

            // Root node is always black
            root.isRed = false;

            return true;
        }

        // DOES NOT adjust xExtent and yExtent!
        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ShiftRightOfPath(
            [Widen] long position,            [Widen] long xAdjust)
        {
            unchecked
            {
                /*[Widen]*/
                long xPositionCurrent = 0 ;
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
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position)
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

                Node current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionCurrent = 0 ;

                Node parent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionParent = 0 ;

                Node grandParent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionGrandparent = 0 ;

                Node match = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionMatch = 0 ;

                Node parentOfMatch = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionParentOfMatch = 0 ;

                bool foundMatch = false;

                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionLastGreaterAncestor = 0 ;
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
                            long xPositionSibling = xPositionParent + sibling.xOffset ;
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
                                    xPositionParentOfMatch = xPositionSibling;
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
                                long xPositionNewGrandparent = xPositionParent - parent.xOffset ;
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
                                    xPositionParentOfMatch = xPositionNewGrandparent;
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
                        {
                            Debug.Assert(CompareKeyMode.Position == CompareKeyMode.Position);
                            order = position.CompareTo(xPositionCurrent);
                        }
                    }

                    if (order == 0)
                    {
                        // save the matching node
                        foundMatch = true;
                        match = current;
                        parentOfMatch = parent;

                        xPositionMatch = xPositionCurrent;
                        xPositionParentOfMatch = xPositionParent;
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
                    long xLength ;
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
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ xPositionParent - xPositionMatch,
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
            Node match,            Node parentOfMatch,            Node successor,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xOffsetMatchSuccessor,            Node parentOfsuccessor)
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
        private void ReplaceChildOfNodeOrRoot(Node parent,Node child,Node newChild)
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

        private Node GetSibling(Node node,Node parent)
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
        private void InsertionBalance(Node current,ref Node parent,Node grandParent,Node greatGrandParent)
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

        private void Merge2Nodes(Node parent,Node child1,Node child2)
        {
            Debug.Assert(IsRed(parent), "parent must be red");
            // combing two 2-nodes into a 4-node
            parent.isRed = false;
            child1.isRed = true;
            child2.isRed = true;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(
            [Widen] long position,            out Node lastLessEqual,            [Widen] out long xPositionLastLessEqual,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long xLength)
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
                long xPositionCurrent = 0 ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
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
                long xOffsetR = r.xOffset ;
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
                long xOffsetNode = node.xOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetLChild = lChild.xOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetLRGrandchild = lrGrandChild.xOffset ;

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
                long xOffsetL = l.xOffset ;
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
                long xOffsetNode = node.xOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetRChild = rChild.xOffset ;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xOffsetRLGrandchild = rlGrandChild.xOffset ;

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

        private TreeRotation RotationNeeded(Node parent,Node current,Node sibling)
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
                    node = node.left;
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
                    node = node.right;
                    while (node != Null)
                    {
                        offset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = node.left;
                    }
                }
            }
        }

        // INonInvasiveTreeInspection

        object INonInvasiveTreeInspection.Root { get { return root != Null ? (object)root : null; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left != Null ? (object)n.left : null;
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            Node n = (Node)node;
            return n.right != Null ? (object)n.right : null;
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            object key = null;
            return key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            Node n = (Node)node;
            object value = null;
            value = n.value;
            return value;
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            Node n = (Node)node;
            return n.isRed ? "red" : "black";
        }

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

            /*[Feature(Feature.Rank, Feature.MultiRank, Feature.Range, Feature.Range2)]*/
            ValidateRanges();

            ValidateDepthInvariant();
        }

        private void ValidateDepthInvariant()
        {
            int min = Int32.MaxValue;
            MinDepth(root, 0, ref min);
            int depth = MaxDepth(root);
            min++;
            int max = depth + 1;

            if ((2 * min < max) || (depth > 2 * Math.Log(this.count + 1) / Math.Log(2)))
            {
                throw new InvalidOperationException("depth invariant");
            }
        }

        private int MaxDepth(Node root)
        {
            return (root == Null) ? 0 : (1 + Math.Max(MaxDepth(root.left), MaxDepth(root.right)));
        }

        private void MinDepth(Node root,int depth,ref int min)
        {
            if (root == Null)
            {
                min = Math.Min(min, depth);
            }
            else
            {
                if (depth < min)
                {
                    MinDepth(root.left, depth + 1, ref min);
                }
                if (depth < min)
                {
                    MinDepth(root.right, depth + 1, ref min);
                }
            }
        }

        // INonInvasiveRange2MapInspection

        [Feature(Feature.Range, Feature.Range2)]
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
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long,/*[Widen]*/long>(node, xOffset, yOffset));
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
                    ranges[i - 1].x.length = ranges[i].x.start - ranges[i - 1].x.start;
                }

                ranges[i - 1].x.length = this.xExtent - ranges[i - 1].x.start;
            }

            return ranges;
        }

        [Feature(Feature.Range, Feature.Range2)]
        void INonInvasiveRange2MapInspectionLong.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

        public IEnumerator<EntryRangeMapLong<ValueType>> GetEnumerator()
        {
            return GetFastEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public RobustEnumerableSurrogate GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRangeMapLong<ValueType>>
        {
            private readonly RedBlackTreeRangeMapLong<ValueType> tree;

            public RobustEnumerableSurrogate(RedBlackTreeRangeMapLong<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRangeMapLong<ValueType>> GetEnumerator()
            {
                return new RobustEnumerator(tree);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public FastEnumerableSurrogate GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryRangeMapLong<ValueType>>
        {
            private readonly RedBlackTreeRangeMapLong<ValueType> tree;

            public FastEnumerableSurrogate(RedBlackTreeRangeMapLong<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRangeMapLong<ValueType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryRangeMapLong<ValueType>>
        {
            private readonly RedBlackTreeRangeMapLong<ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(RedBlackTreeRangeMapLong<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRangeMapLong<ValueType> Current
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

                        // OR

                        /*[Feature(Feature.Range, Feature.Range2)]*/
                        {
                            ValueType value = default(ValueType);
                            /*[Widen]*/
                            long xStart = 0, xLength = 0 ;
                            xStart = currentXStart;

                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength,
                                /*[Payload(Payload.Value)]*/out value);

                            return new EntryRangeMapLong<ValueType>(
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength);
                        }
                    }
                    return new EntryRangeMapLong<ValueType>();
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
        public class FastEnumerator : IEnumerator<EntryRangeMapLong<ValueType>>
        {
            private readonly RedBlackTreeRangeMapLong<ValueType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart, nextXStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>>();

            public FastEnumerator(RedBlackTreeRangeMapLong<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRangeMapLong<ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {

                        return new EntryRangeMapLong<ValueType>(
                            /*[Payload(Payload.Value)]*/currentNode.value,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart);
                    }
                    return new EntryRangeMapLong<ValueType>();
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

            private void PushSuccessor(
                Node node,                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xPosition)
            {
                while (node != tree.Null)
                {
                    xPosition += node.xOffset;

                    stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>(
                        node,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
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

                PushSuccessor(
                    nextNode.right,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart);
            }
        }
    }
}
