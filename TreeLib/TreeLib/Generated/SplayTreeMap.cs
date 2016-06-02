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

    public class SplayTreeMap<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType> :

        /*[Feature(Feature.Dict)]*//*[Payload(Payload.Value)]*/IOrderedMap<KeyType, ValueType>,

        INonInvasiveTreeInspection,

        IEnumerable<EntryMap<KeyType, ValueType>>,
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

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;
            [Payload(Payload.Value)]
            public ValueType value;

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
                try
                {
                    keyText = key.ToString();
                }
                catch (NullReferenceException)
                {
                }

                string valueText = null;
                try
                {
                    valueText = value.ToString();
                }
                catch (NullReferenceException)
                {
                }

                string leftKeyText = null;
                try
                {
                    leftKeyText = left == null ? "null" : (((Node)left).IsNil ? "Nil" : ((Node)left).key.ToString());
                }
                catch (NullReferenceException)
                {
                }

                string rightKeyText = null;
                try
                {
                    rightKeyText = right == null ? "null" : (((Node)right).IsNil ? "Nil" : ((Node)right).key.ToString());
                }
                catch (NullReferenceException)
                {
                }

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
        //[Storage(Storage.Array)]
        //private Node2[] nodes2;

        //
        // State for both array & object form
        //

        private Node root;
        [Count]
        private ulong count;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private ushort version;


        //
        // Construction
        //

        // Object

        [Storage(Storage.Object)]
        public SplayTreeMap([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
        {
            this.comparer = comparer;
            root = Nil;

            this.allocationMode = allocationMode;
            this.freelist = Nil;
            EnsureFree(capacity);
        }

        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTreeMap(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        [Storage(Storage.Object)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public SplayTreeMap([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/comparer, 0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public SplayTreeMap()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public SplayTreeMap(SplayTreeMap<KeyType, ValueType> original)
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
            {
                /*[Storage(Storage.Object)]*/
                {
#if DEBUG
                    allocateHelper.allocateCount = 0;
#endif
                }
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
                return 0 == comparer.Compare(key, root.key);
            }
            return false;
        }

        [Feature(Feature.Dict)]
        private bool SetOrAddValue(KeyType key,[Payload(Payload.Value)] ValueType value,bool add)
        {
            Splay(ref root, key);
            int c;
            if ((root == Nil) || ((c = comparer.Compare(key, root.key)) < 0))
            {
                // insert item just in front of root

                /*[Count]*/
                ulong countNew = checked(this.count + 1);

                Node i = Allocate();
                i.key = key;
                i.value = value;

                i.left = root.left;
                i.right = root;
                root.left = Nil;

                root = i;

                this.count = countNew;

                return true;
            }
            else if (c > 0)
            {
                // insert item just after root

                /*[Count]*/
                ulong countNew = checked(this.count + 1);

                Node i = Allocate();
                i.key = key;
                i.value = value;

                i.right = root.right;
                i.left = root;
                root.right = Nil;

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
                    root.value = value;
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
                    int c = comparer.Compare(key, root.key);
                    if (c == 0)
                    {

                        Node dead, x;

                        dead = root;
                        if (root.left == Nil)
                        {
                            x = root.right;
                        }
                        else
                        {
                            x = root.left;
                            Splay(ref x, key);
                            Debug.Assert(x.right == Nil);
                            x.right = root.right;
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
                if (0 == comparer.Compare(key, root.key))
                {
                    value = root.value;
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
                if (0 == comparer.Compare(key, root.key))
                {
                    root.value = value;
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
                Node node = root;
                KeyType least = node.key;
                while (node.left != Nil)
                {
                    node = node.left;
                    least = node.key;
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
                Node node = root;
                KeyType greatest = node.key;
                while (node.right != Nil)
                {
                    node = node.right;
                    greatest = node.key;
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
                int rootComparison = comparer.Compare(key, root.key);
                if ((rootComparison > 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = root.key;
                    return true;
                }
                else if (root.left != Nil)
                {
                    KeyType rootKey = root.key;
                    Splay(ref root.left, rootKey);
                    nearestKey = root.left.key;
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
                int rootComparison = comparer.Compare(key, root.key);
                if ((rootComparison < 0) || (orEqual && (rootComparison == 0)))
                {
                    nearestKey = root.key;
                    return true;
                }
                else if (root.right != Nil)
                {
                    KeyType rootKey = root.key;
                    Splay(ref root.right, rootKey);
                    nearestKey = root.right.key;
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
                allocateHelper.allocateCount = checked(allocateHelper.allocateCount + 1);
#endif
            }

            return node;
        }

        [Storage(Storage.Object)]
        private void Free(Node node)
        {
#if DEBUG
            allocateHelper.allocateCount = checked(allocateHelper.allocateCount - 1);
            Debug.Assert(allocateHelper.allocateCount == count);

            node.left = null;
            node.right = null;
#endif

            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                node.key = default(KeyType); // clear any references for GC
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
        private void Splay(ref Node root,KeyType leftComparand)
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
                Node r = N;

                while (true)
                {
                    int c;

                    c = comparer.Compare(leftComparand, t.key);
                    if (c < 0)
                    {
                        if (t.left == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, t.left.key);
                        if (c < 0)
                        {
                            // rotate right
                            Node u = t.left;
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
                        r.left = t;
                        r = t;
                        t = t.left;
                    }
                    else if (c > 0)
                    {
                        if (t.right == Nil)
                        {
                            break;
                        }
                        c = comparer.Compare(leftComparand, t.right.key);
                        if (c > 0)
                        {
                            // rotate left
                            Node u = t.right;
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
                        l.right = t;
                        l = t;
                        t = t.right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                l.right = t.left;
                r.left = t.right;
                t.left = N.right;
                t.right = N.left;
                root = t;
            }
        }

        // INonInvasiveTreeInspection

        object INonInvasiveTreeInspection.Root { get { return root != Nil ? (object)root : null; } }

        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            Node n = (Node)node;
            return n.left != Nil ? (object)n.left : null;
        }

        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            Node n = (Node)node;
            return n.right != Nil ? (object)n.right : null;
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
            Node n = (Node)node;
            object value = null;
            value = n.value;
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

                    if (node.left != Nil)
                    {
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        if (!(comparer.Compare(node.left.key, node.key) < 0))
                        {
                            throw new InvalidOperationException("ordering invariant");
                        }
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Nil)
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
            private readonly SplayTreeMap<KeyType, ValueType> tree;

            public RobustEnumerableSurrogate(SplayTreeMap<KeyType, ValueType> tree)
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
            private readonly SplayTreeMap<KeyType, ValueType> tree;

            public FastEnumerableSurrogate(SplayTreeMap<KeyType, ValueType> tree)
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
            private readonly SplayTreeMap<KeyType, ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(SplayTreeMap<KeyType, ValueType> tree)
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
            private readonly SplayTreeMap<KeyType, ValueType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;

            private readonly Stack<STuple<Node>> stack
                = new Stack<STuple<Node>>();

            public FastEnumerator(SplayTreeMap<KeyType, ValueType> tree)
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
                            /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/currentNode.key,
                            /*[Payload(Payload.Value)]*/currentNode.value);
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
                Node node)
            {
                while (node != tree.Nil)
                {

                    stack.Push(new STuple<Node>(
                        node));
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

                nextNode = tree.Nil;

                if (stack.Count == 0)
                {
                    return;
                }

                STuple<Node> cursor
                    = stack.Pop();

                nextNode = cursor.Item1;

                PushSuccessor(
                    nextNode.right);
            }
        }
    }
}
