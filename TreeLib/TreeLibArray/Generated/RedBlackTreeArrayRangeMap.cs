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

    public class RedBlackTreeArrayRangeMap<[Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Range)]*//*[Payload(Payload.Value)]*//*[Widen]*/IRangeMap<ValueType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Range, Feature.Range2)]*//*[Widen]*/INonInvasiveRange2MapInspection,

        IEnumerable<EntryRangeMap<ValueType>>,
        IEnumerable
    {

        //
        // Array form data structure
        //

        [Storage(Storage.Array)]
        [StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        private struct Node
        {
            public NodeRef left;
            public NodeRef right;
            [Payload(Payload.Value)]
            public ValueType value;

            public bool isRed;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;

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

            public static bool operator ==(NodeRef left,NodeRef right)
            {
                return left.node == right.node;
            }

            public static bool operator !=(NodeRef left,NodeRef right)
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

        private NodeRef Null { get { return RedBlackTreeArrayRangeMap<ValueType>._Null; } } // allow tree.Null or this.Null in all cases

        private NodeRef root;
        [Count]
        private uint count;
        private ushort version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xExtent;

        private readonly AllocationMode allocationMode;
        private NodeRef freelist;

        // Array

        [Storage(Storage.Array)]
        public RedBlackTreeArrayRangeMap(uint capacity,AllocationMode allocationMode)
        {
            if (allocationMode == AllocationMode.DynamicDiscard)
            {
                throw new ArgumentException();
            }
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        [Storage(Storage.Array)]
        public RedBlackTreeArrayRangeMap(uint capacity)
            : this(capacity, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        public RedBlackTreeArrayRangeMap()
            : this(0, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        public RedBlackTreeArrayRangeMap(RedBlackTreeArrayRangeMap<ValueType> original)
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

            root = Null;
            this.count = 0;
            this.xExtent = 0;
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] int start)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            return FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition));
        }

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
                /*[Payload(Payload.Value)]*/ value,
                start,
                xLength,
                true/*add*/,
                false/*update*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] int start)
        {
            return DeleteInternal(
                start);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] int start,[Widen] out int length)
        {
            NodeRef node;
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

        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] int start,[Widen] int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            NodeRef node;
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

                this.xExtent = checked(this.xExtent + xAdjust);

                ShiftRightOfPath(start + 1, xAdjust);

                return true;
            }
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] int start,out ValueType value)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                value = nodes[node].value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] int start,ValueType value)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition, xLength;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                nodes[node].value = value;
                return true;
            }
            value = default(ValueType);
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] int start,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            NodeRef node;
            /*[Widen]*/
            int xPosition;
            if (FindPosition(start, out node, out xPosition, out xLength)
                && (start == (xPosition)))
            {
                value = nodes[node].value;
                return true;
            }
            xLength = 0;
            value = default(ValueType);
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Insert([Widen] int start,[Widen] int xLength,[Payload(Payload.Value)] ValueType value)
        {
            if (!TryInsert(start, xLength, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Delete([Widen] int start)
        {
            if (!TryDelete(start))
            {
                throw new ArgumentException("item not in tree");
            }
        }

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

        [Feature(Feature.Range, Feature.Range2)]
        public void SetLength([Widen] int start,[Widen] int length)
        {
            if (!TrySetLength(start, length))
            {
                throw new ArgumentException("item not in tree");
            }
        }

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

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public void SetValue([Widen] int start,ValueType value)
        {
            if (!TrySetValue(start, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public void Get([Widen] int start,[Widen] out int xLength,[Payload(Payload.Value)] out ValueType value)
        {
            if (!TryGet(start, out xLength, /*[Payload(Payload.Value)]*/out value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int GetExtent()
        {
            return this.xExtent;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLessOrEqual([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestLess(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLess([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestLess(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreaterOrEqual([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestGreater(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreater([Widen] int position,[Widen] out int nearestStart)
        {
            return NearestGreater(
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ position,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
        }

        // Array allocation

        [Storage(Storage.Array)]
        private NodeRef Allocate([Payload(Payload.Value)] ValueType value,bool isRed)
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
            nodes[node].value = value;
            nodes[node].isRed = isRed;
            nodes[node].left = Null;
            nodes[node].right = Null;
            nodes[node].xOffset = 0;

            return node;
        }

        [Storage(Storage.Array)]
        private NodeRef Allocate([Payload(Payload.Value)] ValueType value)
        {
            return Allocate(/*[Payload(Payload.Value)]*/value, true/*isRed*/);
        }

        [Storage(Storage.Array)]
        private void Free(NodeRef node)
        {
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
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,            bool orEqual)
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
                nearestStart = xPositionLastLess;
                return true;
            }
            nearestStart = 0;
            return false;
        }

        private bool NearestGreater(
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,            bool orEqual)
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
            [Payload(Payload.Value)] ValueType value,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,            bool add,            bool update)
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
                Debug.Assert(nodes[root].xOffset == 0);
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
            NodeRef current = root;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            int xPositionCurrent = 0;
            NodeRef parent = Null;
            NodeRef grandParent = Null;
            NodeRef greatGrandParent = Null;

            NodeRef successor = Null;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            int xPositionSuccessor = 0;

            //even if we don't actually add to the set, we may be altering its structure (by doing rotations
            //and such). so update version to disable any enumerators/subsets working on it
            this.version = unchecked((ushort)(this.version + 1));

            int order = 0;
            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            /*[Widen]*/
            int xPositionParent = 0;
            while (current != Null)
            {
                unchecked
                {
                    xPositionCurrent += nodes[current].xOffset;
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
                    nodes[root].isRed = false;
                    if (update)
                    {
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
                if (order < 0)
                {
                    successor = parent;
                    xPositionSuccessor = xPositionParent;

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
                    nodes[root].isRed = false;
                    return false;
                }

                // compute here to throw before modifying tree
                /*[Widen]*/
                int xExtentNew;
uint countNew;
                try
                {
                    xExtentNew = checked(this.xExtent + xLength);
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    nodes[root].isRed = false;
                    throw;
                }

                node = Allocate(/*[Payload(Payload.Value)]*/value);

                ShiftRightOfPath(unchecked(xPositionParent + 1), xLength);

                nodes[parent].right = node;

                nodes[node].xOffset = xLengthParent;

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
                        nodes[root].isRed = false;
                        return false;
                    }
                }

                // compute here to throw before modifying tree
                /*[Widen]*/
                int xExtentNew;
uint countNew;
                try
                {
                    xExtentNew = checked(this.xExtent + xLength);
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    nodes[root].isRed = false;
                    throw;
                }

                Debug.Assert(parent == successor);

                node = Allocate(/*[Payload(Payload.Value)]*/value);

                ShiftRightOfPath(xPositionParent, xLength);

                nodes[parent].left = node;

                nodes[node].xOffset = -xLength;

                this.xExtent = xExtentNew;
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
            [Widen] int position,            [Widen] int xAdjust)
        {
            unchecked
            {
                /*[Widen]*/
                int xPositionCurrent = 0;
                NodeRef current = root;
                while (current != Null)
                {
                    xPositionCurrent += nodes[current].xOffset;

                    int order = position.CompareTo(xPositionCurrent);
                    if (order <= 0)
                    {
                        xPositionCurrent += xAdjust;
                        nodes[current].xOffset += xAdjust;
                        if (nodes[current].left != Null)
                        {
                            nodes[nodes[current].left].xOffset -= xAdjust;
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
                this.version = unchecked((ushort)(this.version + 1));

                NodeRef current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionCurrent = 0;

                NodeRef parent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParent = 0;

                NodeRef grandParent = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionGrandparent = 0;

                NodeRef match = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionMatch = 0;

                NodeRef parentOfMatch = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParentOfMatch = 0;

                bool foundMatch = false;

                NodeRef lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionLastGreaterAncestor = 0;
                while (current != Null)
                {
                    xPositionCurrent += nodes[current].xOffset;

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
                                if (parent == match)
                                {
                                    parentOfMatch = sibling;
                                    xPositionParentOfMatch = xPositionSibling;
                                }

                                // update sibling, this is necessary for following processing
                                sibling = (nodes[parent].left == current) ? nodes[parent].right : nodes[parent].left;
                                xPositionSibling += nodes[sibling].xOffset;
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

                                nodes[newGrandParent].isRed = nodes[parent].isRed;
                                nodes[parent].isRed = false;
                                nodes[current].isRed = true;
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
                    nodes[root].isRed = false;
                }
                return foundMatch;
            }
        }

        // Replace the matching node with its successor.
        private void ReplaceNode(
            NodeRef match,            NodeRef parentOfMatch,            NodeRef successor,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xOffsetMatchSuccessor,            NodeRef parentOfsuccessor)
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
                        }
                        nodes[successor].right = nodes[match].right;
                        if (nodes[match].right != Null)
                        {
                            nodes[nodes[match].right].xOffset -= xOffsetMatchSuccessor;
                        }
                    }

                    nodes[successor].left = nodes[match].left;
                    if (nodes[match].left != Null)
                    {
                        nodes[nodes[match].left].xOffset -= xOffsetMatchSuccessor;
                    }
                }

                if (successor != Null)
                {
                    nodes[successor].isRed = nodes[match].isRed;

                    nodes[successor].xOffset = nodes[match].xOffset + xOffsetMatchSuccessor;
                }

                ReplaceChildOfNodeOrRoot(parentOfMatch/*parent*/, match/*child*/, successor/*new child*/);
            }
        }

        // Replace the child of a parent node. 
        // If the parent node is null, replace the root.        
        private void ReplaceChildOfNodeOrRoot(NodeRef parent,NodeRef child,NodeRef newChild)
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

        private NodeRef GetSibling(NodeRef node,NodeRef parent)
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
        private void InsertionBalance(NodeRef current,ref NodeRef parent,NodeRef grandParent,NodeRef greatGrandParent)
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

        private void Merge2Nodes(NodeRef parent,NodeRef child1,NodeRef child2)
        {
            Debug.Assert(IsRed(parent), "parent must be red");
            // combing two 2-nodes into a 4-node
            nodes[parent].isRed = false;
            nodes[child1].isRed = true;
            nodes[child2].isRed = true;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(
            [Widen] int position,            out NodeRef lastLessEqual,            [Widen] out int xPositionLastLessEqual,            [Feature(Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int xLength)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;
                NodeRef successor = Null;
                xLength = 0;

                NodeRef current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionCurrent = 0;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;
                while (current != Null)
                {
                    xPositionCurrent += nodes[current].xOffset;

                    if (position < (xPositionCurrent))
                    {
                        successor = current;
                        xPositionSuccessor = xPositionCurrent;

                        current = nodes[current].left;
                    }
                    else
                    {
                        lastLessEqual = current;
                        xPositionLastLessEqual = xPositionCurrent;

                        current = nodes[current].right; // try to find successor
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

        private NodeRef RotateLeft(NodeRef node)
        {
            unchecked
            {
                NodeRef r = nodes[node].right;

                if (nodes[r].left != Null)
                {
                    nodes[nodes[r].left].xOffset += nodes[r].xOffset;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetR = nodes[r].xOffset;
                nodes[r].xOffset += nodes[node].xOffset;
                nodes[node].xOffset = -xOffsetR;

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
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetLChild = nodes[lChild].xOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetLRGrandchild = nodes[lrGrandChild].xOffset;

                nodes[lrGrandChild].xOffset = xOffsetLRGrandchild + xOffsetLChild + xOffsetNode;
                nodes[lChild].xOffset = -xOffsetLRGrandchild;
                nodes[node].xOffset = -xOffsetLRGrandchild - xOffsetLChild;
                if (nodes[lrGrandChild].left != Null)
                {
                    nodes[nodes[lrGrandChild].left].xOffset += xOffsetLRGrandchild;
                }
                if (nodes[lrGrandChild].right != Null)
                {
                    nodes[nodes[lrGrandChild].right].xOffset += xOffsetLRGrandchild + xOffsetLChild;
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
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetL = nodes[l].xOffset;
                nodes[l].xOffset += nodes[node].xOffset;
                nodes[node].xOffset = -xOffsetL;

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
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetRChild = nodes[rChild].xOffset;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xOffsetRLGrandchild = nodes[rlGrandChild].xOffset;

                nodes[rlGrandChild].xOffset = xOffsetRLGrandchild + xOffsetRChild + xOffsetNode;
                nodes[rChild].xOffset = -xOffsetRLGrandchild;
                nodes[node].xOffset = -xOffsetRLGrandchild - xOffsetRChild;
                if (nodes[rlGrandChild].left != Null)
                {
                    nodes[nodes[rlGrandChild].left].xOffset += xOffsetRLGrandchild + xOffsetRChild;
                }
                if (nodes[rlGrandChild].right != Null)
                {
                    nodes[nodes[rlGrandChild].right].xOffset += xOffsetRLGrandchild;
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

        private TreeRotation RotationNeeded(NodeRef parent,NodeRef current,NodeRef sibling)
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
        private void ValidateRanges()
        {
            if (root != Null)
            {
                Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>> stack = new Stack<STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>>();

                /*[Widen]*/
                int offset = 0;
                /*[Widen]*/
                int leftEdge = 0;
                /*[Widen]*/
                int rightEdge = this.xExtent;

                NodeRef node = root;
                while (node != Null)
                {
                    offset += nodes[node].xOffset;
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
                    while (node != Null)
                    {
                        offset += nodes[node].xOffset;
                        stack.Push(new STuple<NodeRef, /*[Widen]*/int, /*[Widen]*/int, /*[Widen]*/int>(node, offset, leftEdge, rightEdge));
                        rightEdge = offset;
                        node = nodes[node].left;
                    }
                }
            }
        }

        // INonInvasiveTreeInspection

        object INonInvasiveTreeInspection.Root { get { return root != Null ? (object)root : null; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].left != Null ? (object)nodes[n].left : null;
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].right != Null ? (object)nodes[n].right : null;
        }

        object INonInvasiveTreeInspection.GetKey(object node)
        {
            object key = null;
            return key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            NodeRef n = (NodeRef)node;
            object value = null;
            value = nodes[n].value;
            return value;
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].isRed ? "red" : "black";
        }

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

                    if (visited.ContainsKey(node))
                    {
                        throw new InvalidOperationException("cycle");
                    }
                    visited.Add(node, false);

                    if (nodes[node].left != Null)
                    {
                        worklist.Enqueue(nodes[node].left);
                    }
                    if (nodes[node].right != Null)
                    {
                        worklist.Enqueue(nodes[node].right);
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

        private int MaxDepth(NodeRef root)
        {
            return (root == Null) ? 0 : (1 + Math.Max(MaxDepth(nodes[root].left), MaxDepth(nodes[root].right)));
        }

        private void MinDepth(NodeRef root,int depth,ref int min)
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
                    ranges[i - 1].x.length = ranges[i].x.start - ranges[i - 1].x.start;
                }

                ranges[i - 1].x.length = this.xExtent - ranges[i - 1].x.start;
            }

            return ranges;
        }

        [Feature(Feature.Range, Feature.Range2)]
        void INonInvasiveRange2MapInspection.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

        public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
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

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRangeMap<ValueType>>
        {
            private readonly RedBlackTreeArrayRangeMap<ValueType> tree;

            public RobustEnumerableSurrogate(RedBlackTreeArrayRangeMap<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
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

        public struct FastEnumerableSurrogate : IEnumerable<EntryRangeMap<ValueType>>
        {
            private readonly RedBlackTreeArrayRangeMap<ValueType> tree;

            public FastEnumerableSurrogate(RedBlackTreeArrayRangeMap<ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRangeMap<ValueType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryRangeMap<ValueType>>
        {
            private readonly RedBlackTreeArrayRangeMap<ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(RedBlackTreeArrayRangeMap<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRangeMap<ValueType> Current
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
                            int xStart = 0, xLength = 0;
                            xStart = currentXStart;

                            tree.Get(
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/out xLength,
                                /*[Payload(Payload.Value)]*/out value);

                            return new EntryRangeMap<ValueType>(
                                /*[Payload(Payload.Value)]*/value,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xStart,
                                /*[Feature(Feature.Range, Feature.Range2)]*/xLength);
                        }
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
        public class FastEnumerator : IEnumerator<EntryRangeMap<ValueType>>
        {
            private readonly RedBlackTreeArrayRangeMap<ValueType> tree;
            private ushort version;
            private NodeRef currentNode;
            private NodeRef nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;

            private readonly Stack<STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>> stack
                = new Stack<STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>>();

            public FastEnumerator(RedBlackTreeArrayRangeMap<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRangeMap<ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {

                        return new EntryRangeMap<ValueType>(
                            /*[Payload(Payload.Value)]*/tree.nodes[currentNode].value,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart);
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
                NodeRef node,                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xPosition)
            {
                while (node != tree.Null)
                {
                    xPosition += tree.nodes[node].xOffset;

                    stack.Push(new STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>(
                        node,
                        /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/xPosition));
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

                nextNode = tree.Null;
                nextXStart = tree.xExtent;

                if (stack.Count == 0)
                {
                    nextXStart = tree.xExtent;
                    return;
                }

                STuple<NodeRef, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;
                nextXStart = cursor.Item2;

                PushSuccessor(
                    tree.nodes[nextNode].right,
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart);
            }
        }
    }
}
