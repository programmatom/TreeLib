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

// This implementation is adapted from Glib's AVL tree: https://github.com/GNOME/glib/blob/master/glib/gtree.c
// which is attributed to Maurizio Monge.

// An overview of AVL trees can be found here: https://en.wikipedia.org/wiki/AVL_tree

namespace TreeLib
{

    public class AVLTreeMultiRankListLong<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType> :
        /*[Feature(Feature.RankMulti)]*//*[Payload(Payload.None)]*//*[Widen]*/IMultiRankListLong<KeyType>,
        INonInvasiveTreeInspection,
        /*[Feature(Feature.Rank, Feature.RankMulti)]*//*[Widen]*/INonInvasiveMultiRankMapInspectionLong,
        IEnumerable<EntryMultiRankListLong<KeyType>>,
        IEnumerable

        where KeyType : IComparable<KeyType>
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

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;
        }

        [ArrayIndexing]
        [Storage(Storage.Object)]
        private Node this[Node node] { get { return node; } }

        [Storage(Storage.Object)]
        private readonly static Node _Null = null;

        //
        // State for both array & object form
        //

        private Node Null { get { return AVLTreeMultiRankListLong<KeyType>._Null; } } // allow tree.Null or this.Null in all cases

        private Node root;
        [Count]
        private ulong count;
        private ushort version;

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private const int MAX_GTREE_HEIGHT = 40; // TODO: not valid for greater than 32 bits addressing
        private readonly WeakReference<Node[]> path = new WeakReference<Node[]>(null);


        //
        // Construction
        //

        // Object

        [Storage(Storage.Object)]
        public AVLTreeMultiRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
        {
            this.comparer = comparer;
            this.root = Null;

            this.allocationMode = allocationMode;
            this.freelist = Null;
            EnsureFree(capacity);
        }

        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTreeMultiRankListLong(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTreeMultiRankListLong([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public AVLTreeMultiRankListLong()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public AVLTreeMultiRankListLong(AVLTreeMultiRankListLong<KeyType> original)
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
            Node node = g_tree_find_node(key);
            return node != Null;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/ key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key);
        }

        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key,out KeyType keyOut)
        {
            Node node = g_tree_find_node(key);
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
            return g_tree_insert_internal(
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
            if (!TryRemove(key))
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
            Node node = g_tree_first_node();
            if (node == Null)
            {
                leastOut = default(KeyType);
                return false;
            }
            leastOut = node.key;
            return true;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType greatestOut)
        {
            Node node = g_tree_last_node();
            if (node == Null)
            {
                greatestOut = default(KeyType);
                return false;
            }
            greatestOut = node.key;
            return true;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey)
        {
            /*[Widen]*/
            long nearestStart ;
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
            long nearestStart ;
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
            long nearestStart ;
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
            long nearestStart ;
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
        public long GetExtent()
        {
            return this.xExtent;
        }


        //
        // IRankMap, IMultiRankMap, IRankList, IMultiRankList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        public long RankCount { get { return this.xExtent; } }

        // ContainsKey() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryAdd(KeyType key,[Feature(Feature.RankMulti)] [Const(1, Feature.Rank)] [SuppressConst(Feature.RankMulti)][Widen] long rankCount)
        {
            if (rankCount <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return g_tree_insert_internal(
                key,
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/ 0,
                /*[Feature(Feature.Rank, Feature.RankMulti)]*/ CompareKeyMode.Key,
                rankCount,
                true/*add*/,
                false/*update*/);
        }

        // TryRemove() - reuses Feature.Dict implementation

        // TryGetValue() - reuses Feature.Dict implementation

        // TrySetValue() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGet(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            Node node;
            /*[Widen]*/
            long xPosition, xLength ;
            if (Find(key, out node, out xPosition, /*[Feature(Feature.RankMulti)]*/out xLength))
            {
                keyOut = node.key;
                rank = xPosition;
                rankCount = xLength;
                return true;
            }
            keyOut = default(KeyType);
            rank = 0;
            rankCount = 0;
            return false;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public bool TryGetKeyByRank([Widen] long rank,out KeyType key)
        {
            if (rank < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            Node node;
            /*[Widen]*/
            long xPosition ;
            /*[Feature(Feature.RankMulti)]*/
            /*[Widen]*/
            long xLength ;
            if (FindPosition(
rank,
out node,
out xPosition,
/*[Feature(Feature.RankMulti)]*/out xLength))
            {
                key = node.key;
                return true;
            }
            key = default(KeyType);
            return false;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Add(KeyType key,[Feature(Feature.RankMulti)][Widen] long rankCount)
        {
            if (!TryAdd(key, /*[Feature(Feature.RankMulti)]*/rankCount))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        // Remove() - reuses Feature.Dict implementation

        // GetValue() - reuses Feature.Dict implementation

        // SetValue() - reuses Feature.Dict implementation

        [Feature(Feature.Rank, Feature.RankMulti)]
        public void Get(KeyType key,[Payload(Payload.None)] out KeyType keyOut,[Widen] out long rank,[Feature(Feature.RankMulti)][Widen] out long rankCount)
        {
            if (!TryGet(key, /*[Payload(Payload.None)]*/out keyOut, out rank, /*[Feature(Feature.RankMulti)]*/out rankCount))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public KeyType GetKeyByRank([Widen] long rank)
        {
            KeyType key;
            if (!TryGetKeyByRank(rank, out key))
            {
                throw new ArgumentException("index not in tree");
            }
            return key;
        }

        [Feature(Feature.Rank, Feature.RankMulti)]
        public void AdjustCount(KeyType key,[Widen] long countAdjust)
        {
            unchecked
            {
                Node node;
                /*[Widen]*/
                long xPosition ;
                /*[Widen]*/
                long xLength = 1 ;
                if (Find(key, out node, out xPosition, /*[Feature(Feature.RankMulti)]*/out xLength))
                {
                    // update and possibly remove

                    /*[Widen]*/
                    long adjustedLength = checked(xLength + countAdjust) ;
                    if (adjustedLength > 0)
                    {

                        this.xExtent = checked(this.xExtent + countAdjust);

                        ShiftRightOfPath(unchecked(xPosition + 1), countAdjust);
                    }
                    else if (xLength + countAdjust == 0)
                    {
                        Debug.Assert(countAdjust < 0);

                        this.g_tree_remove_internal(
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

                        Add(key, /*[Feature(Feature.RankMulti)]*/countAdjust);
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
        private Node g_tree_node_new([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
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

        private bool NearestLess(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                    nearestKey = lastLess.key;
                    nearestStart = xPositionLastLess;
                    return true;
                }
                nearestKey = default(KeyType);
                nearestStart = 0;
                return false;
            }
        }

        private bool NearestGreater(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] out long nearestStart,            bool orEqual)
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
                    nearestKey = lastGreater.key;
                    nearestStart = xPositionLastGreater;
                    return true;
                }
                nearestKey = default(KeyType);
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
        private bool g_tree_insert_internal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long xLength,            bool add,            bool update)
        {
            unchecked
            {
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

                    root = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);
                    Debug.Assert(root.xOffset == 0);
                    Debug.Assert(this.xExtent == 0);
                    this.xExtent = xLength;

                    Debug.Assert(this.count == 0);
                    this.count = 1;
                    // TODO: this.version = unchecked(this.version + 1);

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
                {
                    Node addBelow = Null;
                    /*[Widen]*/
                    long xPositionAddBelow = 0 ;
                    /*[Widen]*/
                    long yPositionAddBelow = 0 ;
                    while (true)
                    {
                        xPositionNode += node.xOffset;

                        int cmp;
                        if (addBelow != Null)
                        {
                            cmp = -1; // we don't need to compare any more once we found the match
                        }
                        else
                        {
                            if (mode == CompareKeyMode.Key)
                            {
                                cmp = comparer.Compare(key, node.key);
                            }
                            else
                            {
                                Debug.Assert(mode == CompareKeyMode.Position);
                                cmp = position.CompareTo(xPositionNode);
                                if (add && (cmp == 0))
                                {
                                    cmp = -1; // node never found for sparse range mode
                                }
                            }
                        }

                        if (cmp == 0)
                        {
                            if (update)
                            {
                                node.key = key;
                            }
                            return !add;
                        }

                        if (cmp < 0)
                        {
                            successor = node;
                            xPositionSuccessor = xPositionNode;
                            yPositionSuccessor = yPositionNode;

                            if (node.left_child)
                            {
                                bool push = true;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                push = addBelow == Null;
                                if (push)
                                {
                                    path[idx++] = node;
                                }
                                node = node.left;
                            }
                            else
                            {
                                // precedes node

                                if (!add)
                                {
                                    return false;
                                }

                                bool setAddBelow = true;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                setAddBelow = addBelow == Null;
                                if (setAddBelow)
                                {
                                    addBelow = node;
                                    xPositionAddBelow = xPositionNode;
                                    yPositionAddBelow = yPositionNode;
                                    addleft = true;
                                }

                                // always break:
                                // if inserting as left child of node, node is successor
                                // if ending right subtree successor search, node is successor
                                break;
                            }
                        }
                        else
                        {
                            Debug.Assert(cmp > 0);

                            if (node.right_child)
                            {
                                bool push = true;
                                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                                push = addBelow == Null;
                                if (push)
                                {
                                    path[idx++] = node;
                                }
                                node = node.right;
                            }
                            else
                            {
                                // follows node

                                if (!add)
                                {
                                    return false;
                                }

                                addBelow = node;
                                xPositionAddBelow = xPositionNode;
                                yPositionAddBelow = yPositionNode;
                                addleft = false;

                                /*Feature(Feature.Dict)*/
                                break; // truncate search early if no augmentation, else...

                                // continue the search in right sub tree after we find a match (to find successor)
                                if (!node.right_child)
                                {
                                    break;
                                }
                                node = node.right;
                            }
                        }
                    }

                    node = addBelow;
                    xPositionNode = xPositionAddBelow;
                    yPositionNode = yPositionAddBelow;
                }

                if (addleft)
                {
                    // precedes node

                    Debug.Assert(node == successor);

                    if (mode == CompareKeyMode.Position)
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

                    Node child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

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
                    if ((mode == CompareKeyMode.Position)
                        && (position != (xPositionNode + xLengthNode)))
                    {
                        return false;
                    }

                    this.version = unchecked((ushort)(this.version + 1));

                    // throw here before modifying tree
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;
                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);

                    Node child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

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

        private bool g_tree_remove_internal(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)][Widen] long position,            [Feature(Feature.Rank, Feature.RankMulti)] [Const(CompareKeyMode.Key, Feature.Dict)] [Const2(CompareKeyMode.Position, Feature.Range, Feature.Range2)] [SuppressConst(Feature.Rank, Feature.RankMulti)] CompareKeyMode mode)
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
                    if (mode == CompareKeyMode.Key)
                    {
                        cmp = comparer.Compare(key, node.key);
                    }
                    else
                    {
                        Debug.Assert(mode == CompareKeyMode.Position);
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

                        /*Feature(Feature.Dict)*/
                        successor = g_tree_node_next(node);
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

                        /*Feature(Feature.Dict)*/
                        predecessor = g_tree_node_previous(node);
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
        private void ShiftRightOfPath(
            [Widen] long position,            [Widen] long xAdjust)
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

        private int g_tree_height()
        {
            unchecked
            {
                if (root == Null)
                {
                    return 0;
                }

                int height = 0;
                Node node = root;

                while (true)
                {
                    height += 1 + Math.Max((int)node.balance, 0);

                    if (!node.left_child)
                    {
                        return height;
                    }

                    node = node.left;
                }
            }
        }

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

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private Node g_tree_find_node(KeyType key)
        {
            Node node = root;
            if (node == Null)
            {
                return Null;
            }

            while (true)
            {
                int cmp = comparer.Compare(key, node.key);
                if (cmp == 0)
                {
                    return node;
                }
                else if (cmp < 0)
                {
                    if (!node.left_child)
                    {
                        return Null;
                    }

                    node = node.left;
                }
                else
                {
                    if (!node.right_child)
                    {
                        return Null;
                    }

                    node = node.right;
                }
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool Find(
            KeyType key,            out Node match,            [Widen] out long xPositionMatch,            [Feature(Feature.RankMulti)][Widen] out long xLengthMatch)
        {
            unchecked
            {
                match = Null;
                xPositionMatch = 0;
                xLengthMatch = 0;

                Node successor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionSuccessor = 0 ;
                Node lastGreaterAncestor = Null;
                /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                /*[Widen]*/
                long xPositionLastGreaterAncestor = 0 ;
                if (root != Null)
                {
                    Node current = root;
                    /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
                    /*[Widen]*/
                    long xPositionCurrent = 0 ;
                    while (true)
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
                            current = current.right; // continue the search in right sub tree after we find a match
                        }
                    }
                }

                if (match != Null)
                {
                    Debug.Assert(successor != Null);
                    if (successor != match)
                    {
                        xLengthMatch = xPositionSuccessor - xPositionMatch;
                    }
                    else if (lastGreaterAncestor != Null)
                    {
                        xLengthMatch = xPositionLastGreaterAncestor - xPositionMatch;
                    }
                    else
                    {
                        xLengthMatch = this.xExtent - xPositionMatch;
                    }
                }

                return match != Null;
            }
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

        object INonInvasiveTreeInspection.Root { get { return root != Null ? (object)root : null; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left_child ? (object)n.left : null;
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            Node n = (Node)node;
            return n.right_child ? (object)n.right : null;
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
            return n.balance;
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

            g_tree_node_check(root);
            ValidateDepthInvariant();
        }

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

        private int MaxDepth(Node node)
        {
            int ld = node.left_child ? MaxDepth(node.left) : 0;
            int rd = node.right_child ? MaxDepth(node.right) : 0;
            return 1 + Math.Max(ld, rd);
        }

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

        // INonInvasiveMultiRankMapInspection

        [Feature(Feature.Rank, Feature.RankMulti)]
        [Widen]
        MultiRankMapEntryLong[] INonInvasiveMultiRankMapInspectionLong.GetRanks()
        {
            /*[Widen]*/
            MultiRankMapEntryLong[] ranks = new MultiRankMapEntryLong[Count];
            int i = 0;

            if (root != Null)
            {
                Stack<STuple<Node,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long>>();

                /*[Widen]*/
                long xOffset = 0 ;

                Node node = root;
                while (node != Null)
                {
                    xOffset += node.xOffset;
                    stack.Push(new STuple<Node,/*[Widen]*/long>(node, xOffset));
                    node = node.left_child ? node.left : Null;
                }
                while (stack.Count != 0)
                {
                    STuple<Node,/*[Widen]*/long> t = stack.Pop();
                    node = t.Item1;
                    xOffset = t.Item2;

                    object key = null;
                    key = node.key;
                    object value = null;

                    /*[Widen]*/
                    ranks[i++] = new MultiRankMapEntryLong(key, new RangeLong(xOffset, 0), value);

                    node = node.right_child ? node.right : Null;
                    while (node != Null)
                    {
                        xOffset += node.xOffset;
                        stack.Push(new STuple<Node,/*[Widen]*/long>(node, xOffset));
                        node = node.left_child ? node.left : Null;
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
        void INonInvasiveMultiRankMapInspectionLong.Validate()
        {
            ((INonInvasiveTreeInspection)this).Validate();
        }


        //
        // Enumeration
        //

        public IEnumerator<EntryMultiRankListLong<KeyType>> GetEnumerator()
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

        public struct RobustEnumerableSurrogate : IEnumerable<EntryMultiRankListLong<KeyType>>
        {
            private readonly AVLTreeMultiRankListLong<KeyType> tree;

            public RobustEnumerableSurrogate(AVLTreeMultiRankListLong<KeyType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryMultiRankListLong<KeyType>> GetEnumerator()
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

        public struct FastEnumerableSurrogate : IEnumerable<EntryMultiRankListLong<KeyType>>
        {
            private readonly AVLTreeMultiRankListLong<KeyType> tree;

            public FastEnumerableSurrogate(AVLTreeMultiRankListLong<KeyType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryMultiRankListLong<KeyType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryMultiRankListLong<KeyType>>
        {
            private readonly AVLTreeMultiRankListLong<KeyType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(AVLTreeMultiRankListLong<KeyType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryMultiRankListLong<KeyType> Current
            {
                get
                {

                    if (valid)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        {
                            KeyType key = currentKey;
                            /*[Widen]*/
                            long rank = 0 ;
                            /*[Widen]*/
                            long count = 1 ;
                            // OR
                            /*[Feature(Feature.Rank, Feature.RankMulti)]*/
                            tree.Get(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey,
                                /*[Payload(Payload.None)]*/out key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/out rank,
                                /*[Feature(Feature.RankMulti)]*/out count);

                            return new EntryMultiRankListLong<KeyType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Feature(Feature.Rank, Feature.RankMulti)]*/rank,
                                /*[Feature(Feature.RankMulti)]*/count);
                        }
                    }
                    return new EntryMultiRankListLong<KeyType>();
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
        public class FastEnumerator : IEnumerator<EntryMultiRankListLong<KeyType>>
        {
            private readonly AVLTreeMultiRankListLong<KeyType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart, nextXStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>>();

            public FastEnumerator(AVLTreeMultiRankListLong<KeyType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryMultiRankListLong<KeyType> Current
            {
                get
                {
                    if (currentNode != tree.Null)
                    {

                        return new EntryMultiRankListLong<KeyType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentNode.key,
                            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/currentXStart,
                            /*[Feature(Feature.RankMulti, Feature.Range, Feature.Range2)]*/nextXStart - currentXStart);
                    }
                    return new EntryMultiRankListLong<KeyType>();
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
    }
}
