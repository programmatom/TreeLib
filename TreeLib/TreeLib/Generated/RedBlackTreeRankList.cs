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

    public class RedBlackTreeRankList<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType> :
        /*[Feature(Feature.Rank)]*//*[Payload(Payload.None)]*//*[Widen]*/IRankList<KeyType>,

        INonInvasiveTreeInspection,
        /*[Feature(Feature.Rank, Feature.RankMulti)]*//*[Widen]*/INonInvasiveMultiRankMapInspection,

        IEnumerable<EntryRankList<KeyType>>,
        IEnumerable

        where KeyType : IComparable<KeyType>
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

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public int xOffset;

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

        private Node Null { get { return RedBlackTreeRankList<KeyType>._Null; } } // allow tree.Null or this.Null in all cases

        private Node root;
        [Count]
        private ulong count;
        private ushort version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private int xExtent;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private Node freelist;


        //
        // Construction
        //

        // Object

        [Storage(Storage.Object)]
        public RedBlackTreeRankList([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
        {
            this.comparer = comparer;
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public RedBlackTreeRankList(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public RedBlackTreeRankList([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public RedBlackTreeRankList()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public RedBlackTreeRankList(RedBlackTreeRankList<KeyType> original)
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

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            return FindNode(key) != Null;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return DeleteInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key);
        }

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key,out KeyType keyOut)
        {
            Node node = FindNode(key);
            if (node != Null)
            {
                keyOut = node.key;
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
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                false/*add*/,
                true/*update*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void Remove(KeyType key)
        {
            if (!DeleteInternal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key))
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
            Node node = root;
            if (node == Null)
            {
                leastOut = default(KeyType);
                return false;
            }
            while (node.left != Null)
            {
                node = node.left;
            }
            leastOut = node.key;
            return true;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType greatestOut)
        {
            Node node = root;
            if (node == Null)
            {
                greatestOut = default(KeyType);
                return false;
            }
            while (node.right != Null)
            {
                node = node.right;
            }
            greatestOut = node.key;
            return true;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            return NearestLess(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            return NearestLess(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            return NearestGreater(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            int nearestStart;
            return NearestGreater(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ out nearestKey,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ out nearestStart,
                false/*orEqual*/);
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        public int GetExtent()
        {
            return this.xExtent;
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
        public bool TryAdd(KeyType key)
        {

            return InsertUpdateInternal(
                key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key,
                1,
                true/*add*/,
                false/*update*/);
        }

        // TryRemove() - reuses Feature.Dict implementation

        // TryGetValue() - reuses Feature.Dict implementation

        // TrySetValue() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGet(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out int rank)
        {
            Node node;
            /*[Widen]*/
            int xPosition;
            if (Find(key, out node, out xPosition))
            {
                keyOut = node.key;
                rank = xPosition;
                return true;
            }
            keyOut = default(KeyType);
            rank = 0;
            return false;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGetKeyByRank([Widen] int rank,out KeyType key)
        {
            if (rank < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            int xPosition;
            if (FindPosition(rank, out node, out xPosition))
            {
                key = node.key;
                return true;
            }
            key = default(KeyType);
            return false;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Add(KeyType key)
        {
            if (!TryAdd(key))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        // Remove() - reuses Feature.Dict implementation

        // GetValue() - reuses Feature.Dict implementation

        // SetValue() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Get(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out int rank)
        {
            if (!TryGet(key, /*[Payload(Payload.None)]*/out keyOut, out rank))
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
        public void AdjustCount(KeyType key,[Widen] int countAdjust)
        {
            unchecked
            {
                Node node;
                /*[Widen]*/
                int xPosition;
                /*[Widen]*/
                int xLength = 1;
                if (Find(key, out node, out xPosition))
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

                        ShiftRightOfPath(xPosition + 1, countAdjust);
                    }
                    else if (xLength + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        DeleteInternal(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key);
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

                        Add(key);
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
        private Node Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,bool isRed)
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

            node.key = key;
            node.left = Null;
            node.right = Null;
            node.isRed = isRed;
            node.xOffset = 0;

            return node;
        }

        [Storage(Storage.Object)]
        private Node Allocate([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
        {
            return Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, true/*isRed*/);
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
                node.key = default(KeyType); // clear any references for GC

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
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,            bool orEqual)
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
                    if (mode == CompareKeyMode.Key)
                    {
                        c = comparer.Compare(key, node.key);
                    }
                    else
                    {
                        Debug.Assert(mode == CompareKeyMode.Position);
                        c = position.CompareTo(xPosition);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestKey = node.key;
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
                nearestKey = lastLess.key;
                nearestStart = xPositionLastLess;
                return true;
            }
            nearestKey = default(KeyType);
            nearestStart = 0;
            return false;
        }

        private bool NearestGreater(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out int nearestStart,            bool orEqual)
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
                    if (mode == CompareKeyMode.Key)
                    {
                        c = comparer.Compare(key, node.key);
                    }
                    else
                    {
                        Debug.Assert(mode == CompareKeyMode.Position);
                        c = position.CompareTo(xPosition);
                    }
                    if (orEqual && (c == 0))
                    {
                        nearestKey = node.key;
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
                nearestKey = lastGreater.key;
                nearestStart = xPositionLastGreater;
                return true;
            }
            nearestKey = default(KeyType);
            nearestStart = this.xExtent;
            return false;
        }

        // Searches tree for key location.
        // If key is not present and add==true, node is inserted.
        // If key is preset and update==true, value is replaced.
        // Returns true if a node was added or if add==false and a node was updated.
        // NOTE: update mode does *not* adjust for xLength/yLength!
        private bool InsertUpdateInternal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xLength,            bool add,            bool update)
        {
            Debug.Assert((mode == CompareKeyMode.Key) || (add != update));

            if (root == Null)
            {
                if (!add)
                {
                    return false;
                }
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                if (mode == CompareKeyMode.Position)
                {
                    if (position != 0)
                    {
                        return false;
                    }
                }

                root = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key, false);
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
            this.version = unchecked((ushort)(this.version + 1));

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

                if (mode == CompareKeyMode.Key)
                {
                    order = comparer.Compare(key, current.key);
                }
                else
                {
                    Debug.Assert(mode == CompareKeyMode.Position);
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
                        current.key = key;
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
                if ((mode == CompareKeyMode.Position)
                    && (position != unchecked(xPositionParent + xLengthParent)))
                {
                    root.isRed = false;
                    return false;
                }

                // compute here to throw before modifying tree
                /*[Widen]*/
                int xExtentNew;
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

                node = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                ShiftRightOfPath(unchecked(xPositionParent + 1), xLength);

                parent.right = node;

                node.xOffset = xLengthParent;

                this.xExtent = xExtentNew;
                this.count = countNew;
            }
            else
            {
                // precedes parent

                if (mode == CompareKeyMode.Position)
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
                int xExtentNew;
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

                node = Allocate(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

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
            [Widen] int position,            [Widen] int xAdjust)
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
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode)
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
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionParentOfMatch = 0;

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
                        if (mode == CompareKeyMode.Key)
                        {
                            order = comparer.Compare(key, current.key);
                        }
                        else
                        {
                            Debug.Assert(mode == CompareKeyMode.Position);
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
                    root.isRed = false;
                }
                return foundMatch;
            }
        }

        // Replace the matching node with its successor.
        private void ReplaceNode(
            Node match,            Node parentOfMatch,            Node successor,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xOffsetMatchSuccessor,            Node parentOfsuccessor)
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

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private Node FindNode(KeyType item)
        {
            Node current = root;
            while (current != Null)
            {
                int order = comparer.Compare(item, current.key);
                if (order == 0)
                {
                    return current;
                }
                else
                {
                    current = (order < 0) ? current.left : current.right;
                }
            }

            return Null;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool Find(
            KeyType key,            out Node match,            [Widen] out int xPositionMatch)
        {
            unchecked
            {
                match = Null;
                Node successor = Null;
                xPositionMatch = 0;

                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionSuccessor = 0;
                Node current = root;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionCurrent = 0;
                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                int xPositionLastGreaterAncestor = 0;
                while (current != Null)
                {
                    xPositionCurrent += current.xOffset;

                    int order = (match != Null) ? -1 : comparer.Compare(key, current.key);

                    if (order == 0)
                    {
                        xPositionMatch = xPositionCurrent;
                        match = current;
                    }

                    successor = current;
                    xPositionSuccessor = xPositionCurrent;

                    if (order < 0)
                    {
                        if (match == Null)
                        {
                            lastGreaterAncestor = current;
                            xPositionLastGreaterAncestor = xPositionCurrent;
                        }
                        current = current.left;
                    }
                    else
                    {
                        current = current.right; // continue the search in right sub tree after we find a match
                    }
                }

                if (match != Null)
                {
                    Debug.Assert(successor != Null);
                }

                return match != Null;
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private bool FindPosition(
            [Widen] int position,            out Node lastLessEqual,            [Widen] out int xPositionLastLessEqual)
        {
            unchecked
            {
                lastLessEqual = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                xPositionLastLessEqual = 0;
                Node successor = Null;

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

                    if ((offset < leftEdge) || (offset >= rightEdge))
                    {
                        throw new InvalidOperationException("range containment invariant");
                    }

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
            Node n = (Node)node;
            object key = null;
            key = n.key;
            return key;
        }

        object INonInvasiveTreeInspection.GetValue(object node)
        {
            object value = null;
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
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(node.left.key, node.key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Null)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(node.key, node.right.key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
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

        // INonInvasiveMultiRankMapInspection

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        MultiRankMapEntry[] INonInvasiveMultiRankMapInspection.GetRanks()
        {
            /*[Widen]*/
            MultiRankMapEntry[] ranks = new MultiRankMapEntry[Count];
            int i = 0;

            if (root != Null)
            {
                Stack<STuple<Node, /*[Widen]*/int>> stack = new Stack<STuple<Node, /*[Widen]*/int>>();

                /*[Widen]*/
                int xOffset = 0;

                Node node = root;
                while (node != Null)
                {
                    xOffset += node.xOffset;
                    stack.Push(new STuple<Node, /*[Widen]*/int>(node, xOffset));
                    node = node.left;
                }
                while (stack.Count != 0)
                {
                    STuple<Node, /*[Widen]*/int> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;

                    object key = null;
                    key = node.key;
                    object value = null;

                    /*[Widen]*/
                    ranks[i++] = new MultiRankMapEntry(key, new Range(xOffset, 0), value);

                    node = node.right;
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node, /*[Widen]*/int>(node, xOffset));
                        node = node.left;
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
        void INonInvasiveMultiRankMapInspection.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

        public IEnumerator<EntryRankList<KeyType>> GetEnumerator()
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

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRankList<KeyType>>
        {
            private readonly RedBlackTreeRankList<KeyType> tree;

            public RobustEnumerableSurrogate(RedBlackTreeRankList<KeyType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRankList<KeyType>> GetEnumerator()
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

        public struct FastEnumerableSurrogate : IEnumerable<EntryRankList<KeyType>>
        {
            private readonly RedBlackTreeRankList<KeyType> tree;

            public FastEnumerableSurrogate(RedBlackTreeRankList<KeyType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryRankList<KeyType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryRankList<KeyType>>
        {
            private readonly RedBlackTreeRankList<KeyType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(RedBlackTreeRankList<KeyType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRankList<KeyType> Current
            {
                get
                {

                    if (valid)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        {
                            KeyType key = currentKey;
                            /*[Widen]*/
                            int rank = 0;
                            /*[Widen]*/
                            int count = 1;
                            // OR
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            tree.Get(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey,
                                /*[Payload(Payload.None)]*/out key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/out rank);

                            /*[Feature(Feature.Rank)]*/
                            Debug.Assert(count == 1);

                            return new EntryRankList<KeyType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/rank);
                        }
                    }
                    return new EntryRankList<KeyType>();
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
        public class FastEnumerator : IEnumerator<EntryRankList<KeyType>>
        {
            private readonly RedBlackTreeRankList<KeyType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private int currentXStart, nextXStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>>();

            public FastEnumerator(RedBlackTreeRankList<KeyType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRankList<KeyType> Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {
                        /*[Feature(Feature.Rank)]*/
                        Debug.Assert(nextXStart - currentXStart == 1);

                        return new EntryRankList<KeyType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentNode.key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart);
                    }
                    return new EntryRankList<KeyType>();
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
                Node node,                [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] int xPosition)
            {
                while (node != tree.Null)
                {
                    xPosition += node.xOffset;

                    stack.Push(new STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int>(
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

                STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/int> cursor
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
