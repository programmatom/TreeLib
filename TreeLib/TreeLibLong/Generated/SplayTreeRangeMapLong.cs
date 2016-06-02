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

    public class SplayTreeRangeMapLong<[Payload(Payload.Value)] ValueType> :

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
            [Payload(Payload.Value)]
            public ValueType value;

            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            public long xOffset;

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
                }
                catch (NullReferenceException)
                {
                }

                string rightKeyText = null;
                try
                {
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

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [Widen]
        private long xExtent;

        private readonly AllocationMode allocationMode;
        private Node freelist;

        private ushort version;


        //
        // Construction
        //

        // Object

        [Storage(Storage.Object)]
        public SplayTreeRangeMapLong(uint capacity,AllocationMode allocationMode)
        {
            root = Nil;

            this.allocationMode = allocationMode;
            this.freelist = Nil;
            EnsureFree(capacity);
        }

        [Storage(Storage.Object)]
        public SplayTreeRangeMapLong()
            : this(0, AllocationMode.DynamicDiscard)
        {
        }

        [Storage(Storage.Object)]
        public SplayTreeRangeMapLong(SplayTreeRangeMapLong<ValueType> original)
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
            this.xExtent = 0;
        }


        //
        // IRange2Map, IRange2List, IRangeMap, IRangeList
        //

        // Count { get; } - reuses Feature.Dict implementation

        [Feature(Feature.Range, Feature.Range2)]
        public bool Contains([Widen] long start)
        {
            if (root != Nil)
            {
                Splay2(ref root, start);
                return start == Start(root);
            }
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryInsert([Widen] long start,[Widen] long xLength,[Payload(Payload.Value)] ValueType value)
        {
            unchecked
            {
                if (start < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (xLength <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                Splay2(ref root, start);
                if (start == Start(root))
                {
                    // insert item just in front of root

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;

                    Node i = Allocate();
                    i.value = value;
                    i.xOffset = root.xOffset;

                    i.left = root.left;
                    i.right = root;
                    if (root != Nil)
                    {
                        root.xOffset = xLength;
                        root.left = Nil;
                    }

                    root = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;

                    return true;
                }

                Splay2(ref root.right, 0);
                /*[Widen]*/
                long length = root.right != Nil
                    ? (root.right.xOffset)
                    : (this.xExtent - root.xOffset) ;
                if (start == Start(root) + length)
                {
                    // append

                    Debug.Assert(root.right == Nil);

                    /*[Widen]*/
                    long xLengthRoot = this.xExtent - root.xOffset ;

                    /*[Count]*/
                    ulong countNew = checked(this.count + 1);
                    /*[Widen]*/
                    long xExtentNew = checked(this.xExtent + xLength) ;

                    Node i = Allocate();
                    i.value = value;
                    i.xOffset = xLengthRoot;

                    i.left = Nil;
                    i.right = Nil;
                    Debug.Assert(root != Nil);
                    Debug.Assert(root.right == Nil);
                    root.right = i;

                    this.count = countNew;
                    this.xExtent = xExtentNew;

                    return true;
                }

                return false;
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryDelete([Widen] long start)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start);
                    if (start == Start(root))
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        /*[Widen]*/
                        long xLength = root.right != Nil ? root.right.xOffset : this.xExtent - root.xOffset ;

                        Node dead, x;

                        dead = root;
                        if (root.left == Nil)
                        {
                            x = root.right;
                            if (x != Nil)
                            {
                                x.xOffset += root.xOffset - xLength;
                            }
                        }
                        else
                        {
                            x = root.left;
                            x.xOffset += root.xOffset;
                            Splay2(ref x, start);
                            Debug.Assert(x.right == Nil);
                            if (root.right != Nil)
                            {
                                root.right.xOffset += root.xOffset - x.xOffset - xLength;
                            }
                            x.right = root.right;
                        }
                        root = x;

                        this.count = unchecked(this.count - 1);
                        this.xExtent = unchecked(this.xExtent - xLength);
                        Free(dead);

                        return true;
                    }
                }
                return false;
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetLength([Widen] long start,[Widen] out long length)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start);
                    if (start == Start(root))
                    {
                        Splay2(ref root.right, 0);
                        if (root.right != Nil)
                        {
                            Debug.Assert(root.right.left == Nil);
                            length = root.right.xOffset;
                        }
                        else
                        {
                            length = this.xExtent - start;
                        }
                        return true;
                    }
                }
                length = 0;
                return false;
            }
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetLength([Widen] long start,[Widen] long length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (root != Nil)
            {
                Splay2(ref root, start);
                if (start == Start(root))
                {
                    /*[Widen]*/
                    long oldLength ;
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert(root.right.left == Nil);
                        oldLength = root.right.xOffset;
                    }
                    else
                    {
                        oldLength = unchecked(this.xExtent - root.xOffset);
                    }
                    /*[Widen]*/
                    long delta = length - oldLength ;
                    {
                        this.xExtent = checked(this.xExtent + delta);

                        if (root.right != Nil)
                        {
                            unchecked
                            {
                                root.right.xOffset += delta;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGetValue([Widen] long start,out ValueType value)
        {
            if (root != Nil)
            {
                Splay2(ref root, start);
                if (start == Start(root))
                {
                    value = root.value;
                    return true;
                }
            }
            value = default(ValueType);
            return false;
        }

        [Payload(Payload.Value)]
        [Feature(Feature.Range, Feature.Range2)]
        public bool TrySetValue([Widen] long start,ValueType value)
        {
            if (root != Nil)
            {
                Splay2(ref root, start);
                if (start == Start(root))
                {
                    root.value = value;
                    return true;
                }
            }
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool TryGet([Widen] long start,[Widen] out long xLength,[Payload(Payload.Value)] out ValueType value)
        {
            unchecked
            {
                if (root != Nil)
                {
                    Splay2(ref root, start);
                    if (start == Start(root))
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert((root.right == Nil) || (root.right.left == Nil));
                        if (root.right != Nil)
                        {
                            xLength = root.right.xOffset;
                        }
                        else
                        {
                            xLength = this.xExtent - root.xOffset;
                        }
                        value = root.value;
                        return true;
                    }
                }
                xLength = 0;
                value = default(ValueType);
                return false;
            }
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
        private bool NearestLess([Widen] long position,[Widen] out long nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position);
                /*[Widen]*/
                long start = Start(root) ;
                if ((position < start) || (!orEqual && (position == start)))
                {
                    if (root.left != Nil)
                    {
                        Splay2(ref root.left, 0);
                        Debug.Assert(root.left.right == Nil);
                        nearestStart = start + (root.left.xOffset);
                        return true;
                    }
                    nearestStart = 0;
                    return false;
                }
                else
                {
                    nearestStart = Start(root);
                    return true;
                }
            }
            nearestStart = 0;
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLessOrEqual([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestLess(position, out nearestStart, true/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestLess([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestLess(position, out nearestStart, false/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        private bool NearestGreater([Widen] long position,[Widen] out long nearestStart,bool orEqual)
        {
            if (root != Nil)
            {
                Splay2(ref root, position);
                /*[Widen]*/
                long start = Start(root) ;
                if ((position > start) || (!orEqual && (position == start)))
                {
                    if (root.right != Nil)
                    {
                        Splay2(ref root.right, 0);
                        Debug.Assert(root.right.left == Nil);
                        nearestStart = start + (root.right.xOffset);
                        return true;
                    }
                    nearestStart = this.xExtent;
                    return false;
                }
                else
                {
                    nearestStart = start;
                    return true;
                }
            }
            nearestStart = 0;
            return false;
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreaterOrEqual([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestGreater(position, out nearestStart, true/*orEqual*/);
        }

        [Feature(Feature.Range, Feature.Range2)]
        public bool NearestGreater([Widen] long position,[Widen] out long nearestStart)
        {
            return NearestGreater(position, out nearestStart, false/*orEqual*/);
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
                Debug.Assert(freelist == Nil);
                for (uint i = 0; i < capacity - this.count; i++)
                {
                    Node node = new Node();
                    node.left = freelist;
                    freelist = node;
                }
            }
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Widen]
        private long Start(Node n)
        {
            return n.xOffset;
        }

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        [EnableFixed]
        private void Splay2(ref Node root,[Widen] long position)
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
                /*[Widen]*/
                long lxOffset = 0 ;
                Node r = N;
                /*[Widen]*/
                long rxOffset = 0 ;

                while (true)
                {
                    int c;

                    c = position.CompareTo(t.xOffset);
                    if (c < 0)
                    {
                        if (t.left == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo(t.xOffset + t.left.xOffset);
                        if (c < 0)
                        {
                            // rotate right
                            Node u = t.left;
                            /*[Widen]*/
                            long uXPosition = t.xOffset + u.xOffset ;
                            if (u.right != Nil)
                            {
                                u.right.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
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
                        t.left.xOffset += t.xOffset;
                        t.xOffset -= rxOffset;
                        r.left = t;
                        r = t;
                        rxOffset += r.xOffset;
                        t = t.left;
                    }
                    else if (c > 0)
                    {
                        if (t.right == Nil)
                        {
                            break;
                        }
                        c = position.CompareTo((t.xOffset + t.right.xOffset));
                        if (c > 0)
                        {
                            // rotate left
                            Node u = t.right;
                            /*[Widen]*/
                            long uXPosition = t.xOffset + u.xOffset ;
                            if (u.left != Nil)
                            {
                                u.left.xOffset += uXPosition - t.xOffset;
                            }
                            t.xOffset += -uXPosition;
                            u.xOffset = uXPosition;
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
                        t.right.xOffset += t.xOffset;
                        t.xOffset -= lxOffset;
                        l.right = t;
                        l = t;
                        lxOffset += l.xOffset;
                        t = t.right;
                    }
                    else
                    {
                        break;
                    }
                }
                // reassemble
                l.right = t.left;
                if (l.right != Nil)
                {
                    l.right.xOffset += t.xOffset - lxOffset;
                }
                r.left = t.right;
                if (r.left != Nil)
                {
                    r.left.xOffset += t.xOffset - rxOffset;
                }
                t.left = N.right;
                if (t.left != Nil)
                {
                    t.left.xOffset -= t.xOffset;
                }
                t.right = N.left;
                if (t.right != Nil)
                {
                    t.right.xOffset -= t.xOffset;
                }
                root = t;
            }
        }


        //
        // Non-invasive tree inspection support
        //

        // Helpers

        [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
        private void ValidateRanges()
        {
            if (root != Nil)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long offset = 0 ;
                /*[Widen]*/
                long leftEdge = 0 ;
                /*[Widen]*/
                long rightEdge = this.xExtent ;

                Node node = root;
                while (node != Nil)
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
                    while (node != Nil)
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
                        worklist.Enqueue(node.left);
                    }
                    if (node.right != Nil)
                    {
                        worklist.Enqueue(node.right);
                    }
                }
            }

            /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*/
            ValidateRanges();
        }

        // INonInvasiveRange2MapInspection

        [Feature(Feature.Range, Feature.Range2)]
        [Widen]
        Range2MapEntryLong[] INonInvasiveRange2MapInspectionLong.GetRanges()
        {
            /*[Widen]*/
            Range2MapEntryLong[] ranges = new Range2MapEntryLong[Count];
            int i = 0;

            if (root != Nil)
            {
                Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>> stack = new Stack<STuple<Node,/*[Widen]*/long,/*[Widen]*/long>>();

                /*[Widen]*/
                long xOffset = 0 ;
                /*[Widen]*/
                long yOffset = 0 ;

                Node node = root;
                while (node != Nil)
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
                    while (node != Nil)
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

        public struct RobustEnumerableSurrogate : IEnumerable<EntryRangeMapLong<ValueType>>
        {
            private readonly SplayTreeRangeMapLong<ValueType> tree;

            public RobustEnumerableSurrogate(SplayTreeRangeMapLong<ValueType> tree)
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
            private readonly SplayTreeRangeMapLong<ValueType> tree;

            public FastEnumerableSurrogate(SplayTreeRangeMapLong<ValueType> tree)
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
        /// it keeps a current key and uses NearestGreater to find the next one. The enumerator also uses a constant
        /// amount of memory. However, since it uses queries it is slow, O(n lg(n)) to enumerate the entire tree.
        /// </summary>
        public class RobustEnumerator : IEnumerator<EntryRangeMapLong<ValueType>>
        {
            private readonly SplayTreeRangeMapLong<ValueType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart;
            [Feature(Feature.Range, Feature.Range2)]
            private ushort version; // saving the currentXStart does not work well range collections

            public RobustEnumerator(SplayTreeRangeMapLong<ValueType> tree)
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

                            /*[Feature(Feature.Range, Feature.Range2)]*/
                            version = tree.version; // our query is ok

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

                /*[Feature(Feature.Range, Feature.Range2)]*/
                version = tree.version; // our query is ok

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
        /// However, any change to the tree invalidates it, and that *includes queries* since a query causes a splay
        /// operation that changes the structure of the tree.
        /// Worse, this enumerator also uses a stack that can be as deep as the tree, and since the depth of a splay
        /// tree is in the worst case n (number of nodes), the stack can potentially be size n.
        /// </summary>
        public class FastEnumerator : IEnumerator<EntryRangeMapLong<ValueType>>
        {
            private readonly SplayTreeRangeMapLong<ValueType> tree;
            private ushort version;
            private Node currentNode;
            private Node nextNode;
            [Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]
            [Widen]
            private long currentXStart, nextXStart;

            private readonly Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>> stack
                = new Stack<STuple<Node, /*[Feature(Feature.Rank, Feature.RankMulti, Feature.Range, Feature.Range2)]*//*[Widen]*/long>>();

            public FastEnumerator(SplayTreeRangeMapLong<ValueType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryRangeMapLong<ValueType> Current
            {
                get
                {
                    if (currentNode != tree.Nil)
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
                return currentNode != tree.Nil;
            }

            public void Reset()
            {
                stack.Clear();
                currentNode = tree.Nil;
                nextNode = tree.Nil;
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
                while (node != tree.Nil)
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

                nextNode = tree.Nil;
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
