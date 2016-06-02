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

    public class RedBlackTreeArrayList<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType> :
        /*[Feature(Feature.Dict)]*//*[Payload(Payload.None)]*/IOrderedList<KeyType>,

        INonInvasiveTreeInspection,

        IEnumerable<EntryList<KeyType>>,
        IEnumerable

        where KeyType : IComparable<KeyType>
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

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;

            public bool isRed;

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

        private NodeRef Null { get { return RedBlackTreeArrayList<KeyType>._Null; } } // allow tree.Null or this.Null in all cases

        private NodeRef root;
        [Count]
        private uint count;
        private ushort version;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private NodeRef freelist;

        // Array

        [Storage(Storage.Array)]
        public RedBlackTreeArrayList([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
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

        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public RedBlackTreeArrayList(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        [Storage(Storage.Array)]
        public RedBlackTreeArrayList(uint capacity)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public RedBlackTreeArrayList(IComparer<KeyType> comparer)
            : this(comparer, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        public RedBlackTreeArrayList()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        public RedBlackTreeArrayList(RedBlackTreeArrayList<KeyType> original)
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
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            return FindNode(key) != Null;
        }

        [Feature(Feature.Dict)]
        public bool TryAdd(KeyType key)
        {
            return InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                true/*add*/,
                false/*update*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return DeleteInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key);
        }

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key,out KeyType keyOut)
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
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                false/*add*/,
                true/*update*/);
        }

        [Feature(Feature.Dict)]
        public void Add(KeyType key)
        {
            if (!InsertUpdateInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
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
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key))
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

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType leastOut)
        {
            NodeRef node = root;
            if (node == Null)
            {
                leastOut = default(KeyType);
                return false;
            }
            while (nodes[node].left != Null)
            {
                node = nodes[node].left;
            }
            leastOut = nodes[node].key;
            return true;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType greatestOut)
        {
            NodeRef node = root;
            if (node == Null)
            {
                greatestOut = default(KeyType);
                return false;
            }
            while (nodes[node].right != Null)
            {
                node = nodes[node].right;
            }
            greatestOut = nodes[node].key;
            return true;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey)
        {
            return NearestLess(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey)
        {
            return NearestLess(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                false/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey)
        {
            return NearestGreater(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey)
        {
            return NearestGreater(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                false/*orEqual*/);
        }

        // Array allocation

        [Storage(Storage.Array)]
        private NodeRef Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,bool isRed)
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
            nodes[node].isRed = isRed;
            nodes[node].left = Null;
            nodes[node].right = Null;

            return node;
        }

        [Storage(Storage.Array)]
        private NodeRef Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
        {
            return Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, true/*isRed*/);
        }

        [Storage(Storage.Array)]
        private void Free(NodeRef node)
        {
            nodes[node].key = default(KeyType); // zero any contained references for garbage collector

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
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            bool orEqual)
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
                    }

                    int c;
                    {
                        c = comparer.Compare(key, nodes[node].key);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestKey = nodes[node].key;
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
                nearestKey = nodes[lastLess].key;
                return true;
            }
            nearestKey = default(KeyType);
            return false;
        }

        private bool NearestGreater(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            bool orEqual)
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
                    }

                    int c;
                    {
                        c = comparer.Compare(key, nodes[node].key);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestKey = nodes[node].key;
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
                nearestKey = nodes[lastGreater].key;
                return true;
            }
            nearestKey = default(KeyType);
            return false;
        }

        // Searches tree for key location.
        // If key is not present and add==true, node is inserted.
        // If key is preset and update==true, value is replaced.
        // Returns true if a node was added or if add==false and a node was updated.
        // NOTE: update mode does *not* adjust for xLength/yLength!
        private bool InsertUpdateInternal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            bool add,            bool update)
        {
            Debug.Assert((CompareKeyMode.Key == CompareKeyMode.Key) || (add != update));

            if (root == Null)
            {
                if (!add)
                {
                    return false;
                }

                root = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, false);

                Debug.Assert(this.count == 0);
                this.count = 1;
                this.version = unchecked((ushort)(this.version + 1));

                return true;
            }

            // Search for a node at bottom to insert the new node. 
            // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            NodeRef current = root;
            NodeRef parent = Null;
            NodeRef grandParent = Null;
            NodeRef greatGrandParent = Null;

            NodeRef successor = Null;

            //even if we don't actually add to the set, we may be altering its structure (by doing rotations
            //and such). so update version to disable any enumerators/subsets working on it
            this.version = unchecked((ushort)(this.version + 1));

            int order = 0;
            while (current != Null)
            {
                unchecked
                {
                }

                {
                    order = comparer.Compare(key, nodes[current].key);
                }

                if (order == 0)
                {
                    // We could have changed root node to red during the search process.
                    // We need to set it to black before we return.
                    nodes[root].isRed = false;
                    if (update)
                    {
                        nodes[current].key = key;
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
                if (order < 0)
                {
                    successor = parent;

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
uint countNew;
                try
                {
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    nodes[root].isRed = false;
                    throw;
                }

                node = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                nodes[parent].right = node;
                this.count = countNew;
            }
            else
            {
uint countNew;
                try
                {
                    countNew = checked(this.count + 1);
                }
                catch (OverflowException)
                {
                    nodes[root].isRed = false;
                    throw;
                }

                Debug.Assert(parent == successor);

                node = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                nodes[parent].left = node;
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

        private bool DeleteInternal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
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

                NodeRef parent = Null;

                NodeRef grandParent = Null;

                NodeRef match = Null;

                NodeRef parentOfMatch = Null;

                bool foundMatch = false;

                NodeRef lastGreaterAncestor = Null;
                while (current != Null)
                {

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
                                if (parent == match)
                                {
                                    parentOfMatch = sibling;
                                }

                                // update sibling, this is necessary for following processing
                                sibling = (nodes[parent].left == current) ? nodes[parent].right : nodes[parent].left;
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

                                nodes[newGrandParent].isRed = nodes[parent].isRed;
                                nodes[parent].isRed = false;
                                nodes[current].isRed = true;
                                ReplaceChildOfNodeOrRoot(grandParent, parent, newGrandParent);
                                if (parent == match)
                                {
                                    parentOfMatch = newGrandParent;
                                }
                                grandParent = newGrandParent;
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
                            order = comparer.Compare(key, nodes[current].key);
                        }
                    }

                    if (order == 0)
                    {
                        // save the matching node
                        foundMatch = true;
                        match = current;
                        parentOfMatch = parent;
                    }

                    grandParent = parent;
                    parent = current;

                    if (order < 0)
                    {
                        if (!foundMatch)
                        {
                            lastGreaterAncestor = current;
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
                    Debug.Assert(parent != Null);

                    ReplaceNode(
                        match,
                        parentOfMatch,
                        parent/*successor*/,
                        grandParent/*parentOfSuccessor*/);
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
            NodeRef match,            NodeRef parentOfMatch,            NodeRef successor,            NodeRef parentOfsuccessor)
        {
            unchecked
            {
                if (successor == match)
                {
                    // this node has no successor, should only happen if right child of matching node is null.
                    Debug.Assert(nodes[match].right == Null, "Right child must be null!");
                    successor = nodes[match].left;
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
                        nodes[successor].right = nodes[match].right;
                    }

                    nodes[successor].left = nodes[match].left;
                }

                if (successor != Null)
                {
                    nodes[successor].isRed = nodes[match].isRed;
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

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool Find(
            KeyType key,            out NodeRef match,            [Widen] out int xPositionMatch)
        {
            unchecked
            {
                match = Null;
                NodeRef successor = Null;
                xPositionMatch = 0;
                NodeRef current = root;
                NodeRef lastGreaterAncestor = Null;
                while (current != Null)
                {

                    int order = (match != Null) ? -1 : comparer.Compare(key, nodes[current].key);

                    if (order == 0)
                    {
                        match = current;
                    }

                    successor = current;

                    if (order < 0)
                    {
                        if (match == Null)
                        {
                            lastGreaterAncestor = current;
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
                }

                return match != Null;
            }
        }

        private NodeRef RotateLeft(NodeRef node)
        {
            unchecked
            {
                NodeRef r = nodes[node].right;

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
            NodeRef n = (NodeRef)node;
            object key = null;
            key = nodes[n].key;
            return key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            object value = null;
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
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(nodes[nodes[node].left].key, nodes[node].key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(nodes[node].left);
                    }
                    if (nodes[node].right != Null)
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


        //
        // Enumeration
        //

        public IEnumerator<EntryList<KeyType>> GetEnumerator()
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

        public struct RobustEnumerableSurrogate : IEnumerable<EntryList<KeyType>>
        {
            private readonly RedBlackTreeArrayList<KeyType> tree;

            public RobustEnumerableSurrogate(RedBlackTreeArrayList<KeyType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryList<KeyType>> GetEnumerator()
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

        public struct FastEnumerableSurrogate : IEnumerable<EntryList<KeyType>>
        {
            private readonly RedBlackTreeArrayList<KeyType> tree;

            public FastEnumerableSurrogate(RedBlackTreeArrayList<KeyType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryList<KeyType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryList<KeyType>>
        {
            private readonly RedBlackTreeArrayList<KeyType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(RedBlackTreeArrayList<KeyType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryList<KeyType> Current
            {
                get
                {

                    if (valid)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        {
                            KeyType key = currentKey;

                            return new EntryList<KeyType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);
                        }
                    }
                    return new EntryList<KeyType>();
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

                if (!started)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    valid = tree.Least(out currentKey);

                    started = true;
                }
                else if (valid)
                {
                    /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    valid = tree.NearestGreater(currentKey, out currentKey);
                }

                return valid;
            }

            public void Reset()
            {
                started = false;
                valid = false;
                currentKey = default(KeyType);
            }
        }

        /// <summary>
        /// This enumerator is fast because it uses an in-order traversal of the tree that has O(1) cost per element.
        /// However, any Add or Remove to the tree invalidates it.
        /// </summary>
        public class FastEnumerator : IEnumerator<EntryList<KeyType>>
        {
            private readonly RedBlackTreeArrayList<KeyType> tree;
            private ushort version;
            private NodeRef currentNode;
            private NodeRef nextNode;

            private readonly Stack<STuple<NodeRef>> stack
                = new Stack<STuple<NodeRef>>();

            public FastEnumerator(RedBlackTreeArrayList<KeyType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryList<KeyType> Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {

                        return new EntryList<KeyType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/tree.nodes[currentNode].key);
                    }
                    return new EntryList<KeyType>();
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

                this.version = tree.version;

                PushSuccessor(
                    tree.root);

                Advance();
            }

            private void PushSuccessor(
                NodeRef node)
            {
                while (node != tree.Null)
                {

                    stack.Push(new STuple<NodeRef>(
                        node));
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

                nextNode = tree.Null;

                if (stack.Count == 0)
                {
                    return;
                }

                STuple<NodeRef> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;

                PushSuccessor(
                    tree.nodes[nextNode].right);
            }
        }
    }
}
