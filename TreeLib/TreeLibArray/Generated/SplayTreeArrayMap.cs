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

// Implementation of top-down splay tree written by Daniel Sleator <sleator@cs.cmu.edu>.
// Taken from http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c

// An overview of splay trees can be found here: https://en.wikipedia.org/wiki/Splay_tree

namespace TreeLib
{

    public class SplayTreeArrayMap<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Dict)]*//*[Payload(Payload.Value)]*/IOrderedMap<KeyType, ValueType>,

        INonInvasiveTreeInspection,

        IEnumerable<EntryMap<KeyType, ValueType>>,
        IEnumerable

        where KeyType : IComparable<KeyType>
    {

        //
        // Array form data structure
        //

        //[Storage(Storage.Array)]
        //[StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        //private struct Node2
        //{
        //    [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        //    public KeyType key;
        //    [Payload(Payload.Value)]
        //    public ValueType value;
        //}

        [Storage(Storage.Array)]
        [StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        private struct Node
        {
            public NodeRef left;
            public NodeRef right;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;
            [Payload(Payload.Value)]
            public ValueType value;

            //#if DEBUG
            //            public bool scratch_debug;
            //#endif
            //            public override string ToString()
            //            {
            //#if DEBUG
            //                if (scratch_debug)
            //                {
            //                    return "Scratch";
            //                }
            //#endif

            //                string keyText = null;
            //                /*[Storage(Storage.Object)]*/
            //                try
            //                {
            //                    keyText = key.ToString();
            //                }
            //                catch (NullReferenceException)
            //                {
            //                }

            //                string valueText = null;
            //                /*[Storage(Storage.Object)]*/
            //                try
            //                {
            //                    valueText = value.ToString();
            //                }
            //                catch (NullReferenceException)
            //                {
            //                }

            //                string leftText = left == Nil ? "Nil" : left.ToString();

            //                string rightText = right == Nil ? "Nil" : right.ToString();

            //                return String.Format("[{0}]*{2}={3}*[{1}])", leftText, rightText, keyText, valueText);
            //            }
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
                return node.ToString();
            }
        }

        [Storage(Storage.Array)]
        private const uint ReservedElements = 2;
        [Storage(Storage.Array)]
        private readonly static NodeRef _Nil = new NodeRef(0);
        [Storage(Storage.Array)]
        private readonly static NodeRef N = new NodeRef(1);

        [Storage(Storage.Array)]
        private NodeRef Nil { get { return SplayTreeArrayMap<KeyType, ValueType>._Nil; } } // allow tree.Nil or this.Nil in all cases

        [Storage(Storage.Array)]
        private Node[] nodes;
        //[Storage(Storage.Array)]
        //private Node2[] nodes2;

        //
        // State for both array & object form
        //

        private NodeRef root;
        [Count]
        private uint count;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private NodeRef freelist;

        private ushort version;

        // Array

        [Storage(Storage.Array)]
        public SplayTreeArrayMap([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
        {
            if (allocationMode == AllocationMode.DynamicDiscard)
            {
                throw new ArgumentException();
            }

            this.comparer = comparer;
            this.root = Nil;

            this.allocationMode = allocationMode;
            this.freelist = Nil;
            EnsureFree(capacity);
            //#if DEBUG
            //            nodes[N].scratch_debug = true;
            //#endif
        }

        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTreeArrayMap(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        [Storage(Storage.Array)]
        public SplayTreeArrayMap(uint capacity)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTreeArrayMap(IComparer<KeyType> comparer)
            : this(comparer, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        public SplayTreeArrayMap()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        [Storage(Storage.Array)]
        public SplayTreeArrayMap(SplayTreeArrayMap<KeyType, ValueType> original)
        {
            this.comparer = original.comparer;
            this.nodes = (Node[])original.nodes.Clone();
            this.root = original.root;
            this.freelist = original.freelist;
            this.allocationMode = original.allocationMode;
            this.count = original.count;
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
                // Since splay trees can be deep and thready, breadth-first is likely to use less memory than
                // depth-first. The worst-case is still N/2, so still risks being expensive.

                Queue<NodeRef> queue = new Queue<NodeRef>();
                if (root != Nil)
                {
                    queue.Enqueue(root);
                    while (queue.Count != 0)
                    {
                        NodeRef node = queue.Dequeue();
                        if (nodes[node].left != Nil)
                        {
                            queue.Enqueue(nodes[node].left);
                        }
                        if (nodes[node].right != Nil)
                        {
                            queue.Enqueue(nodes[node].right);
                        }

                        this.count = unchecked(this.count - 1);
                        Free(node);
                    }
                }

                Debug.Assert(this.count == 0);
            }

            root = Nil;
            this.count = 0;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                return 0 == comparer.Compare(key, nodes[root].key);
            }
            return false;
        }

        [Feature(Feature.Dict)]
        private bool SetOrAddValue(KeyType key,[Payload(Payload.Value)] ValueType value,bool add)
        {
            Splay(ref root, key);
            int c;
            if ((root == Nil) || ((c = comparer.Compare(key, nodes[root].key)) < 0))
            {
uint countNew = checked(this.count + 1);

                NodeRef i = Allocate();
                nodes[i].key = key;
                nodes[i].value = value;

                nodes[i].left = nodes[root].left;
                nodes[i].right = root;
                nodes[root].left = Nil;

                root = i;

                this.count = countNew;

                return true;
            }
            else if (c > 0)
            {
uint countNew = checked(this.count + 1);

                NodeRef i = Allocate();
                nodes[i].key = key;
                nodes[i].value = value;

                nodes[i].right = nodes[root].right;
                nodes[i].left = root;
                nodes[root].right = Nil;

                root = i;

                this.count = countNew;

                return true;
            }
            else
            {
                Debug.Assert(c == 0);
                /*[Payload(Payload.Value)]*/
                if (add)
                {
                    nodes[root].value = value;
                }
                return false;
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict)]
        public bool SetOrAddValue(KeyType key,ValueType value)
        {
            return SetOrAddValue(key, value, true/*add*/);
        }

        [Feature(Feature.Dict)]
        public bool TryAdd(KeyType key,[Payload(Payload.Value)] ValueType value)
        {
            return SetOrAddValue(key, /*[Payload(Payload.Value)]*/value, false/*add*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay(ref root, key);
                    int c = comparer.Compare(key, nodes[root].key);
                    if (c == 0)
                    {

                        NodeRef dead, x;

                        dead = root;
                        if (nodes[root].left == Nil)
                        {
                            x = nodes[root].right;
                        }
                        else
                        {
                            x = nodes[root].left;
                            Splay(ref x, key);
                            Debug.Assert(nodes[x].right == Nil);
                            nodes[x].right = nodes[root].right;
                        }
                        root = x;

                        this.count = unchecked(this.count - 1);
                        Free(dead);

                        return true;
                    }
                }
                return false;
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetValue(KeyType key,out ValueType value)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                if (0 == comparer.Compare(key, nodes[root].key))
                {
                    value = nodes[root].value;
                    return true;
                }
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetValue(KeyType key,ValueType value)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                if (0 == comparer.Compare(key, nodes[root].key))
                {
                    nodes[root].value = value;
                    return true;
                }
            }
            return false;
        }

        [Feature(Feature.Dict)]
        public void Add(KeyType key,[Payload(Payload.Value)] ValueType value)
        {
            if (!TryAdd(key, /*[Payload(Payload.Value)]*/value))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void Remove(KeyType key)
        {
            if (!TryRemove(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public ValueType GetValue(KeyType key)
        {
            ValueType value;
            if (!TryGetValue(key, out value))
            {
                throw new ArgumentException("item not in tree");
            }
            return value;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void SetValue(KeyType key,ValueType value)
        {
            if (!TrySetValue(key, value))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType leastOut) // slow; use NearestLessOrEqual() if KeyType.MinValue is available
        {
            if (root != Nil)
            {
                NodeRef node = root;
                KeyType least = nodes[node].key;
                while (nodes[node].left != Nil)
                {
                    node = nodes[node].left;
                    least = nodes[node].key;
                }
                Splay(ref root, least);
                leastOut = least;
                return true;
            }
            leastOut = default(KeyType);
            return false;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType greatestOut) // slow; use NearestGreaterOrEqual() if KeyType.MaxValue is available
        {
            if (root != Nil)
            {
                NodeRef node = root;
                KeyType greatest = nodes[node].key;
                while (nodes[node].right != Nil)
                {
                    node = nodes[node].right;
                    greatest = nodes[node].key;
                }
                Splay(ref root, greatest);
                greatestOut = greatest;
                return true;
            }
            greatestOut = default(KeyType);
            return false;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestLess(KeyType key,out KeyType nearestKey,bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                int rootComparison = comparer.Compare(key, nodes[root].key);
                if ((rootComparison > 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = nodes[root].key;
                    return true;
                }
                else if (nodes[root].left != Nil)
                {
                    KeyType rootKey = nodes[root].key;
                    Splay(ref nodes[root].left, rootKey);
                    nearestKey = nodes[nodes[root].left].key;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            return false;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey)
        {
            return NearestLess(key, out nearestKey, true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey)
        {
            return NearestLess(key, out nearestKey, false/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool NearestGreater(KeyType key,out KeyType nearestKey,bool orEqual)
        {
            if (root != Nil)
            {
                Splay(ref root, key);
                int rootComparison = comparer.Compare(key, nodes[root].key);
                if ((rootComparison < 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = nodes[root].key;
                    return true;
                }
                else if (nodes[root].right != Nil)
                {
                    KeyType rootKey = nodes[root].key;
                    Splay(ref nodes[root].right, rootKey);
                    nearestKey = nodes[nodes[root].right].key;
                    return true;
                }
            }
            nearestKey = default(KeyType);
            return false;
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey)
        {
            return NearestGreater(key, out nearestKey, true/*orEqual*/);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey)
        {
            return NearestGreater(key, out nearestKey, false/*orEqual*/);
        }

        // Array allocation

        [Storage(Storage.Array)]
        private NodeRef Allocate()
        {
            if (freelist == Nil)
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
                if (freelist == Nil)
                {
                    throw new OutOfMemoryException();
                }
            }
            NodeRef node = freelist;
            freelist = nodes[freelist].left;

            return node;
        }

        [Storage(Storage.Array)]
        private void Free(NodeRef node)
        {
            nodes[node].key = default(KeyType); // zero any contained references for garbage collector
            nodes[node].value = default(ValueType); // zero any contained references for garbage collector

            nodes[node].left = freelist;
            freelist = node;
        }

        [Storage(Storage.Array)]
        private void EnsureFree(uint capacity)
        {
            unchecked
            {
                Debug.Assert(freelist == Nil);
                Debug.Assert((nodes == null) || (nodes.Length >= ReservedElements));

                uint oldLength = nodes != null ? unchecked((uint)nodes.Length) : ReservedElements;
                uint newLength = checked(capacity + ReservedElements);

                Array.Resize(ref nodes, unchecked((int)newLength));

                // TODO:was attempt at reducing array bounds checks
                //Node[] nodesNew = nodes;
                //Node2[] nodes2New = nodes2;
                //Array.Resize(ref nodesNew, unchecked((int)newCount));
                //bool key = false, payload = false;
                ///*[Payload(Payload.Value)]*/
                //payload = true;
                ///*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                //key = true;
                //if (payload || key)
                //{
                //    Array.Resize(ref nodes2New, unchecked((int)newCount));
                //}
                //nodes = nodesNew;
                //nodes2 = nodes2New;

                for (long i = (long)newLength - 1; i >= oldLength; i--)
                {
                    Free(new NodeRef(unchecked((uint)i)));
                }
            }
        }


        // http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c
        //
        //            An implementation of top-down splaying
        //                D. Sleator <sleator@cs.cmu.edu>
        //                         March 1992
        //
        //"Splay trees", or "self-adjusting search trees" are a simple and
        //efficient data structure for storing an ordered set. The data
        //structure consists of a binary tree, without parent pointers, and no
        //additional fields. It allows searching, insertion, deletion,
        //deletemin, deletemax, splitting, joining, and many other operations,
        //all with amortized logarithmic performance. Since the trees adapt to
        //the sequence of requests, their performance on real access patterns is
        //typically even better. Splay trees are described in a number of texts
        //and papers [1,2,3,4,5].
        //
        //The code here is adapted from simple top-down splay, at the bottom of
        //page 669 of [3]. It can be obtained via anonymous ftp from
        //spade.pc.cs.cmu.edu in directory /usr/sleator/public.
        //
        //The chief modification here is that the splay operation works even if the
        //item being splayed is not in the tree, and even if the tree root of the
        //tree is NULL. So the line:
        //
        //                          t = splay(i, t);
        //
        //causes it to search for item with key i in the tree rooted at t. If it's
        //there, it is splayed to the root. If it isn't there, then the node put
        //at the root is the last one before NULL that would have been reached in a
        //normal binary search for i. (It's a neighbor of i in the tree.) This
        //allows many other operations to be easily implemented, as shown below.
        //
        //[1] "Fundamentals of data structures in C", Horowitz, Sahni,
        //   and Anderson-Freed, Computer Science Press, pp 542-547.
        //[2] "Data Structures and Their Algorithms", Lewis and Denenberg,
        //   Harper Collins, 1991, pp 243-251.
        //[3] "Self-adjusting Binary Search Trees" Sleator and Tarjan,
        //   JACM Volume 32, No 3, July 1985, pp 652-686.
        //[4] "Data Structure and Algorithm Analysis", Mark Weiss,
        //   Benjamin Cummins, 1992, pp 119-130.
        //[5] "Data Structures, Algorithms, and Performance", Derick Wood,
        //   Addison-Wesley, 1993, pp 367-375.

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        [EnableFixed]
        private void Splay(ref NodeRef root,KeyType leftComparand)
        {
            unchecked
            {
                this.version = unchecked((ushort)(this.version + 1));

                if (root == Nil)
                {
                    return;
                }

                NodeRef t = root;

                nodes[N].left = Nil;
                nodes[N].right = Nil;

                NodeRef l = N;
                NodeRef r = N;

                while (true)
                {
                    int c;

                    c = comparer.Compare(leftComparand, nodes[t].key);
                    if (c < 0)
                    {
                        if (nodes[t].left == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, nodes[nodes[t].left].key);
                        if (c < 0)
                        {
                            // rotate right
                            NodeRef u = nodes[t].left;
                            nodes[t].left = nodes[u].right;
                            nodes[u].right = t;
                            t = u;
                            if (nodes[t].left == Nil)
                            {
                                break;
                            }
                        }
                        // link right
                        Debug.Assert(nodes[t].left != Nil);
                        nodes[r].left = t;
                        r = t;
                        t = nodes[t].left;
                    }
                    else if (c > 0)
                    {
                        if (nodes[t].right == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, nodes[nodes[t].right].key);
                        if (c > 0)
                        {
                            // rotate left
                            NodeRef u = nodes[t].right;
                            nodes[t].right = nodes[u].left;
                            nodes[u].left = t;
                            t = u;
                            if (nodes[t].right == Nil)
                            {
                                break;
                            }
                        }
                        // link left
                        Debug.Assert(nodes[t].right != Nil);
                        nodes[l].right = t;
                        l = t;
                        t = nodes[t].right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                nodes[l].right = nodes[t].left;
                nodes[r].left = nodes[t].right;
                nodes[t].left = nodes[N].right;
                nodes[t].right = nodes[N].left;
                root = t;
            }
        }

        // INonInvasiveTreeInspection

        object INonInvasiveTreeInspection.Root { get { return root != Nil ? (object)root : null; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].left != Nil ? (object)nodes[n].left : null;
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].right != Nil ? (object)nodes[n].right : null;
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
            NodeRef n = (NodeRef)node;
            object value = null;
            value = nodes[n].value;
            return value;
        }

        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            return null;
        }

        void INonInvasiveTreeInspection.Validate()
        {
            if (root != Nil)
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

                    if (nodes[node].left != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(nodes[nodes[node].left].key, nodes[node].key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(nodes[node].left);
                    }
                    if (nodes[node].right != Nil)
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
        }


        //
        // Enumeration
        //

        public IEnumerator<EntryMap<KeyType, ValueType>> GetEnumerator()
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

        public RobustEnumerableSurrogate GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<EntryMap<KeyType, ValueType>>
        {
            private readonly SplayTreeArrayMap<KeyType, ValueType> tree;

            public RobustEnumerableSurrogate(SplayTreeArrayMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryMap<KeyType, ValueType>> GetEnumerator()
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

        public struct FastEnumerableSurrogate : IEnumerable<EntryMap<KeyType, ValueType>>
        {
            private readonly SplayTreeArrayMap<KeyType, ValueType> tree;

            public FastEnumerableSurrogate(SplayTreeArrayMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
            }

            public IEnumerator<EntryMap<KeyType, ValueType>> GetEnumerator()
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
        public class RobustEnumerator : IEnumerator<EntryMap<KeyType, ValueType>>
        {
            private readonly SplayTreeArrayMap<KeyType, ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(SplayTreeArrayMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryMap<KeyType, ValueType> Current
            {
                get
                {

                    if (valid)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                    {
                        KeyType key = currentKey;
                            ValueType value = default(ValueType);

                            /*[Feature(Feature.Dict)]*/
                            value = tree.GetValue(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentKey);

                            return new EntryMap<KeyType, ValueType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                                /*[Payload(Payload.Value)]*/value);
                        }
                    }
                    return new EntryMap<KeyType, ValueType>();
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
        /// However, any change to the tree invalidates it, and that *includes queries* since a query causes a splay
        /// operation that changes the structure of the tree.
        /// Worse, this enumerator also uses a stack that can be as deep as the tree, and since the depth of a splay
        /// tree is in the worst case n (number of nodes), the stack can potentially be size n.
        /// </summary>
        public class FastEnumerator : IEnumerator<EntryMap<KeyType, ValueType>>
        {
            private readonly SplayTreeArrayMap<KeyType, ValueType> tree;
            private ushort version;
            private NodeRef currentNode;
            private NodeRef nextNode;

            private readonly Stack<STuple<NodeRef>> stack
                = new Stack<STuple<NodeRef>>();

            public FastEnumerator(SplayTreeArrayMap<KeyType, ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryMap<KeyType, ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Nil)
                    {

                        return new EntryMap<KeyType, ValueType>(
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/tree.nodes[currentNode].key,
                            /*[Payload(Payload.Value)]*/tree.nodes[currentNode].value);
                    }
                    return new EntryMap<KeyType, ValueType>();
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

                this.version = tree.version;

                PushSuccessor(
                    tree.root);

                Advance();
            }

            private void PushSuccessor(
                NodeRef node)
            {
                while (node != tree.Nil)
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

                nextNode = tree.Nil;

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
