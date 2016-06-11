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
    /// Represents an ordered key collection.
    /// </summary>
    /// <typeparam name="KeyType">Type of key used to index collection. Must be comparable.</typeparam>
    public class AVLTreeArrayList<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType> :
        /*[Feature(Feature.Dict)]*//*[Payload(Payload.None)]*/IOrderedList<KeyType>,

        INonInvasiveTreeInspection,

        IEnumerable<EntryList<KeyType>>,
        IEnumerable,

        ICloneable

        where KeyType : IComparable<KeyType>
    {

        //
        // Array form data structure
        //

        [Storage(Storage.Array)]
        [StructLayout(LayoutKind.Auto)] // defaults to LayoutKind.Sequential; use .Auto to allow framework to pack key & value optimally
        private struct Node
        {
            public NodeRef left, right;

            // tree is threaded: left_child/right_child indicate "non-null", if false, left/right point to predecessor/successor
            public bool left_child, right_child;
            public sbyte balance;

            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            public KeyType key;

        }

        [Storage(Storage.Array)]
        private readonly static NodeRef _Null = new NodeRef(unchecked((uint)-1));

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
                return node.Equals((NodeRef)obj);
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
        private const int ReservedElements = 0;
        [Storage(Storage.Array)]
        private Node[] nodes;

        //
        // State for both array & object form
        //

        private NodeRef Null { get { return AVLTreeArrayList<KeyType>._Null; } } // allow tree.Null or this.Null in all cases

        private NodeRef root;
        [Count]
        private uint count;
        private ushort version;

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly IComparer<KeyType> comparer;

        private readonly AllocationMode allocationMode;
        private NodeRef freelist;

        private const int MAX_GTREE_HEIGHT = 40; // TODO: not valid for greater than 32 bits addressing
        private readonly WeakReference<NodeRef[]> path = new WeakReference<NodeRef[]>(null);

        // Array

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, explicitly configured.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys (present only for keyed collections)</param>
        /// <param name="capacity">
        /// For PreallocatedFixed mode, the maximum capacity of the tree, the memory for which is
        /// preallocated at construction time; exceeding that capacity will result in an OutOfMemory exception.
        /// For DynamicRetainFreelist, the number of nodes to pre-allocate at construction time (the collection
        /// is permitted to exceed that capacity, in which case the internal array will be resized to increase the capacity).
        /// DynamicDiscard is not permitted for array storage trees.
        /// </param>
        /// <param name="allocationMode">The allocation mode (see capacity)</param>
        /// <exception cref="ArgumentException">an allocation mode of DynamicDiscard was specified</exception>
        [Storage(Storage.Array)]
        public AVLTreeArrayList([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] IComparer<KeyType> comparer,uint capacity,AllocationMode allocationMode)
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

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, with the specified capacity and allocation mode and using
        /// the default comparer.
        /// </summary>
        /// <param name="capacity">
        /// For PreallocatedFixed mode, the maximum capacity of the tree, the memory for which is
        /// preallocated at construction time; exceeding that capacity will result in an OutOfMemory exception.
        /// For DynamicRetainFreelist, the number of nodes to pre-allocate at construction time (the collection
        /// is permitted to exceed that capacity, in which case the internal array will be resized to increase the capacity).
        /// DynamicDiscard is not permitted for array storage trees.
        /// </param>
        /// <param name="allocationMode">The allocation mode (see capacity)</param>
        /// <exception cref="ArgumentException">an allocation mode of DynamicDiscard was specified</exception>
        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTreeArrayList(uint capacity,AllocationMode allocationMode)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, allocationMode)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL, with the specified capacity and using
        /// the default comparer (applicable only for keyed collections). The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity of the tree, the memory for which is preallocated at construction time;
        /// if the capacity is exceeded, the internal array will be resized to make more nodes available.
        /// </param>
        [Storage(Storage.Array)]
        public AVLTreeArrayList(uint capacity)
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, capacity, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, using
        /// the specified comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys</param>
        [Storage(Storage.Array)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public AVLTreeArrayList(IComparer<KeyType> comparer)
            : this(comparer, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection using an array storage mechanism, based on an AVL tree, using
        /// the default comparer. The allocation mode is DynamicRetainFreelist.
        /// </summary>
        [Storage(Storage.Array)]
        public AVLTreeArrayList()
            : this(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/Comparer<KeyType>.Default, 0, AllocationMode.DynamicRetainFreelist)
        {
        }

        /// <summary>
        /// Create a new collection based on an AVL tree that is an exact clone of the provided collection, including in
        /// allocation mode, content, structure, capacity and free list state, and comparer.
        /// </summary>
        /// <param name="original">the tree to copy</param>
        [Storage(Storage.Array)]
        public AVLTreeArrayList(AVLTreeArrayList<KeyType> original)
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

        
        /// <summary>
        /// Returns the number of keys in the collection as an unsigned int.
        /// </summary>
        /// <exception cref="OverflowException">The collection contains more than UInt32.MaxValue keys.</exception>
        public uint Count { get { return checked((uint)this.count); } }

        
        /// <summary>
        /// Returns the number of keys in the collection.
        /// </summary>
        public long LongCount { get { return unchecked((long)this.count); } }

        
        /// <summary>
        /// Removes all keys from the collection.
        /// </summary>
        public void Clear()
        {
            // no need to do any work for DynamicDiscard mode
            if (allocationMode != AllocationMode.DynamicDiscard)
            {
                // use threaded feature to traverse in O(1) per node with no stack

                NodeRef node = g_tree_first_node();

                while (node != Null)
                {
                    NodeRef next = g_tree_node_next(node);

                    this.count = unchecked(this.count - 1);
                    g_node_free(node);

                    node = next;
                }

                Debug.Assert(this.count == 0);
            }

            root = Null;
            this.count = 0;
        }

        
        /// <summary>
        /// Determines whether the key is present in the collection.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>true if the key is present in the collection</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool ContainsKey(KeyType key)
        {
            NodeRef node = g_tree_find_node(key);
            return node != Null;
        }

        
        /// <summary>
        /// Attempts to add a key to the collection. If the key is already present, no change is made to the collection.
        /// </summary>
        /// <param name="key">key to search for and possibly insert</param>
        /// <returns>true if the key was added; false if the key was already present</returns>
        [Feature(Feature.Dict)]
        public bool TryAdd(KeyType key)
        {
            return g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                true/*add*/,
                false/*update*/);
        }

        
        /// <summary>
        /// Attempts to remove a key from the collection. If the key is not present, no change is made to the collection.
        /// </summary>
        /// <param name="key">the key to search for and possibly remove</param>
        /// <returns>true if the key was found and removed</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryRemove(KeyType key)
        {
            return g_tree_remove_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);
        }

        
        /// <summary>
        /// Attempts to get the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <param name="keyOut">the actual key contained in the collection</param>
        /// <returns>true if they key was found</returns>
        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TryGetKey(KeyType key,out KeyType keyOut)
        {
            NodeRef node = g_tree_find_node(key);
            if (node != Null)
            {
                keyOut = nodes[node].key;
                return true;
            }
            keyOut = default(KeyType);
            return false;
        }

        
        /// <summary>
        /// Attempts to update the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
        [Payload(Payload.None)]
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool TrySetKey(KeyType key)
        {
            return g_tree_insert_internal(
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                false/*add*/,
                true/*update*/);
        }

        
        /// <summary>
        /// Adds a key to the collection.
        /// </summary>
        /// <param name="key">key to insert</param>
        /// <exception cref="ArgumentException">key is already present in the collection</exception>
        [Feature(Feature.Dict)]
        public void Add(KeyType key)
        {
            if (!TryAdd(key))
            {
                throw new ArgumentException("item already in tree");
            }
        }

        
        /// <summary>
        /// Removes a key from the collection.
        /// </summary>
        /// <param name="key">key to remove</param>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public void Remove(KeyType key)
        {
            if (!TryRemove(key))
            {
                throw new ArgumentException("item not in tree");
            }
        }

        
        /// <summary>
        /// Retrieves the key stored in the collection that matches the provided key.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>the value associated with the key</returns>
        /// <exception cref="ArgumentException">the key is not present in the collection</exception>
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

        
        /// <summary>
        /// Updates the key data for a key in the collection. If the key is not present, no change is made to the collection.
        /// (This would be used if the KeyType is a compound type, with one portion being used as the comparable key and the
        /// remainder being a payload that does not participate in the comparison.)
        /// </summary>
        /// <param name="key">key to search for and possibly replace the existing key</param>
        /// <returns>true if the key was found and updated</returns>
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
        private bool LeastInternal(out KeyType keyOut)
        {
            NodeRef node = g_tree_first_node();
            if (node == Null)
            {
                keyOut = default(KeyType);
                return false;
            }
            keyOut = nodes[node].key;
            return true;
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection (in sort order)
        /// </summary>
        /// <param name="leastOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Least(out KeyType keyOut)
        {
            return LeastInternal(out keyOut);
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool GreatestInternal(out KeyType keyOut)
        {
            NodeRef node = g_tree_last_node();
            if (node == Null)
            {
                keyOut = default(KeyType);
                return false;
            }
            keyOut = nodes[node].key;
            return true;
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection (in sort order)
        /// </summary>
        /// <param name="greatestOut">out parameter receiving the key</param>
        /// <returns>true if a key was found (i.e. collection contains at least 1 key)</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool Greatest(out KeyType keyOut)
        {
            return GreatestInternal(out keyOut);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than or equal to provided key</param>
        /// <returns>true if there was a key less than or equal to the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLessOrEqual(KeyType key,out KeyType nearestKey)
        {
            NodeRef nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the highest key in the collection that is less than the provided key.
        /// </summary>
        /// <param name="key">key to search below</param>
        /// <param name="nearestKey">highest key less than the provided key</param>
        /// <returns>true if there was a key less than the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestLess(KeyType key,out KeyType nearestKey)
        {
            NodeRef nearestNode;
            return NearestLess(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                false/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than or equal to the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than or equal to provided key</param>
        /// <returns>true if there was a key greater than or equal to the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreaterOrEqual(KeyType key,out KeyType nearestKey)
        {
            NodeRef nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                true/*orEqual*/);
        }

        
        /// <summary>
        /// Retrieves the lowest key in the collection that is greater than the provided key.
        /// </summary>
        /// <param name="key">key to search above</param>
        /// <param name="nearestKey">lowest key greater than the provided key</param>
        /// <returns>true if there was a key greater than the provided key</returns>
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public bool NearestGreater(KeyType key,out KeyType nearestKey)
        {
            NodeRef nearestNode;
            return NearestGreater(
                out nearestNode,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key,
                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/out nearestKey,
                false/*orEqual*/);
        }

        // Array allocation

        [Storage(Storage.Array)]
        private NodeRef g_tree_node_new([Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
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
            nodes[node].left = Null;
            nodes[node].left_child = false;
            nodes[node].right = Null;
            nodes[node].right_child = false;
            nodes[node].balance = 0;

            return node;
        }

        [Storage(Storage.Array)]
        private void g_node_free(NodeRef node)
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
                    g_node_free(new NodeRef(unchecked((uint)i)));
                }
            }
        }


        private NodeRef g_tree_first_node()
        {
            if (root == Null)
            {
                return Null;
            }

            NodeRef tmp = root;

            while (nodes[tmp].left_child)
            {
                tmp = nodes[tmp].left;
            }

            return tmp;
        }

        private NodeRef g_tree_last_node()
        {
            if (root == Null)
            {
                return Null;
            }

            NodeRef tmp = root;

            while (nodes[tmp].right_child)
            {
                tmp = nodes[tmp].right;
            }

            return tmp;
        }

        private bool NearestLess(            out NodeRef nearestNode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            bool orEqual)
        {
            unchecked
            {
                NodeRef lastLess = Null;
                /*[Widen]*/
                int xPositionLastLess = 0;
                /*[Widen]*/
                int yPositionLastLess = 0;
                if (root != Null)
                {
                    NodeRef node = root;
                    {
                        /*[Widen]*/
                        int xPosition = 0;
                        /*[Widen]*/
                        int yPosition = 0;
                        while (true)
                        {

                            int c;
                            {
                                c = comparer.Compare(key, nodes[node].key);
                            }
                            if (orEqual && (c == 0))
                            {
                                nearestNode = node;
                                nearestKey = nodes[node].key;
                                return true;
                            }
                            NodeRef next;
                            if (c <= 0)
                            {
                                if (!nodes[node].left_child)
                                {
                                    break;
                                }
                                next = nodes[node].left;
                            }
                            else
                            {
                                lastLess = node;
                                xPositionLastLess = xPosition;
                                yPositionLastLess = yPosition;

                                if (!nodes[node].right_child)
                                {
                                    break;
                                }
                                next = nodes[node].right;
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
                    nearestKey = nodes[lastLess].key;
                    return true;
                }
                nearestNode = Null;
                nearestKey = default(KeyType);
                return false;
            }
        }

        private bool NearestGreater(            out NodeRef nearestNode,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] out KeyType nearestKey,            bool orEqual)
        {
            unchecked
            {
                NodeRef lastGreater = Null;
                /*[Widen]*/
                int xPositionLastGreater = 0;
                /*[Widen]*/
                int yPositionLastGreater = 0;
                if (root != Null)
                {
                    NodeRef node = root;
                    if (node != Null)
                    {
                        /*[Widen]*/
                        int xPosition = 0;
                        /*[Widen]*/
                        int yPosition = 0;
                        while (true)
                        {

                            int c;
                            {
                                c = comparer.Compare(key, nodes[node].key);
                            }
                            if (orEqual && (c == 0))
                            {
                                nearestNode = node;
                                nearestKey = nodes[node].key;
                                return true;
                            }
                            NodeRef next;
                            if (c < 0)
                            {
                                lastGreater = node;
                                xPositionLastGreater = xPosition;
                                yPositionLastGreater = yPosition;

                                if (!nodes[node].left_child)
                                {
                                    break;
                                }
                                next = nodes[node].left;
                            }
                            else
                            {
                                if (!nodes[node].right_child)
                                {
                                    break;
                                }
                                next = nodes[node].right;
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
                    nearestKey = nodes[lastGreater].key;
                    return true;
                }
                nearestNode = Null;
                nearestKey = default(KeyType);
                return false;
            }
        }

        private NodeRef g_tree_node_previous(NodeRef node)
        {
            NodeRef tmp = nodes[node].left;

            if (nodes[node].left_child)
            {
                while (nodes[tmp].right_child)
                {
                    tmp = nodes[tmp].right;
                }
            }

            return tmp;
        }

        private NodeRef g_tree_node_next(NodeRef node)
        {
            NodeRef tmp = nodes[node].right;

            if (nodes[node].right_child)
            {
                while (nodes[tmp].left_child)
                {
                    tmp = nodes[tmp].left;
                }
            }

            return tmp;
        }

        private NodeRef[] RetrievePathWorkspace()
        {
            NodeRef[] path;
            this.path.TryGetTarget(out path);
            if (path == null)
            {
                path = new NodeRef[MAX_GTREE_HEIGHT];
                this.path.SetTarget(path);
            }
            return path;
        }

        // NOTE: replace mode does *not* adjust for xLength/yLength!
        private bool g_tree_insert_internal(            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            bool add,            bool update)
        {
            unchecked
            {
                if (root == Null)
                {
                    if (!add)
                    {
                        return false;
                    }

                    root = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                    Debug.Assert(this.count == 0);
                    this.count = 1;
                    this.version = unchecked((ushort)(this.version + 1));

                    return true;
                }

                NodeRef[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;
                NodeRef node = root;

                NodeRef successor = Null;
                /*[Widen]*/
                int xPositionSuccessor = 0;
                /*[Widen]*/
                int yPositionSuccessor = 0;
                /*[Widen]*/
                int xPositionNode = 0;
                /*[Widen]*/
                int yPositionNode = 0;
                bool addleft = false;
                while (true)
                {

                    int cmp;
                    {
                        cmp = comparer.Compare(key, nodes[node].key);
                    }

                    if (cmp == 0)
                    {
                        if (update)
                        {
                            nodes[node].key = key;
                        }
                        return !add;
                    }

                    if (cmp < 0)
                    {
                        successor = node;
                        xPositionSuccessor = xPositionNode;
                        yPositionSuccessor = yPositionNode;

                        if (nodes[node].left_child)
                        {
                            path[idx++] = node;
                            node = nodes[node].left;
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

                        if (nodes[node].right_child)
                        {
                            path[idx++] = node;
                            node = nodes[node].right;
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

                    this.version = unchecked((ushort)(this.version + 1));
uint countNew = checked(this.count + 1);

                    NodeRef child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                    nodes[child].left = nodes[node].left;
                    nodes[child].right = node;
                    nodes[node].left = child;
                    nodes[node].left_child = true;
                    nodes[node].balance--;
                    this.count = countNew;
                }
                else
                {
                    // follows node

                    Debug.Assert(!nodes[node].right_child);

                    /*[Widen]*/
                    int xLengthNode;
                    /*[Widen]*/
                    int yLengthNode;
                    if (successor != Null)
                    {
                        xLengthNode = xPositionSuccessor - xPositionNode;
                        yLengthNode = yPositionSuccessor - yPositionNode;
                    }

                    this.version = unchecked((ushort)(this.version + 1));
uint countNew = checked(this.count + 1);

                    NodeRef child = g_tree_node_new(/*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);

                    nodes[child].right = nodes[node].right;
                    nodes[child].left = node;
                    nodes[node].right = child;
                    nodes[node].right_child = true;
                    nodes[node].balance++;
                    this.count = countNew;
                }

                // Restore balance. This is the goodness of a non-recursive
                // implementation, when we are done with balancing we 'break'
                // the loop and we are done.
                while (true)
                {
                    NodeRef bparent = path[--idx];
                    bool left_node = (bparent != Null) && (node == nodes[bparent].left);
                    Debug.Assert((bparent == Null) || (nodes[bparent].left == node) || (nodes[bparent].right == node));

                    if ((nodes[node].balance < -1) || (nodes[node].balance > 1))
                    {
                        node = g_tree_node_balance(node);
                        if (bparent == Null)
                        {
                            root = node;
                        }
                        else if (left_node)
                        {
                            nodes[bparent].left = node;
                        }
                        else
                        {
                            nodes[bparent].right = node;
                        }
                    }

                    if ((nodes[node].balance == 0) || (bparent == Null))
                    {
                        break;
                    }

                    if (left_node)
                    {
                        nodes[bparent].balance--;
                    }
                    else
                    {
                        nodes[bparent].balance++;
                    }

                    node = bparent;
                }

                return true;
            }
        }

        private bool g_tree_remove_internal(            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key)
        {
            unchecked
            {
                if (root == Null)
                {
                    return false;
                }

                NodeRef[] path = RetrievePathWorkspace();
                int idx = 0;
                path[idx++] = Null;

                NodeRef node = root;
                while (true)
                {
                    Debug.Assert(node != Null);

                    int cmp;
                    {
                        cmp = comparer.Compare(key, nodes[node].key);
                    }

                    if (cmp == 0)
                    {
                        break;
                    }

                    if (cmp < 0)
                    {
                        if (!nodes[node].left_child)
                        {
                            return false;
                        }

                        path[idx++] = node;
                        node = nodes[node].left;
                    }
                    else
                    {
                        if (!nodes[node].right_child)
                        {
                            return false;
                        }

                        path[idx++] = node;
                        node = nodes[node].right;
                    }
                }

                this.version = unchecked((ushort)(this.version + 1));

                NodeRef successor;

                // The following code is almost equal to g_tree_remove_node,
                // except that we do not have to call g_tree_node_parent.
                NodeRef parent, balance;
                balance = parent = path[--idx];
                Debug.Assert((parent == Null) || (nodes[parent].left == node) || (nodes[parent].right == node));
                bool left_node = (parent != Null) && (node == nodes[parent].left);

                if (!nodes[node].left_child)
                {
                    if (!nodes[node].right_child) // node has no children
                    {

                        if (parent == Null)
                        {
                            root = Null;
                        }
                        else if (left_node)
                        {
                            nodes[parent].left_child = false;
                            nodes[parent].left = nodes[node].left;
                            nodes[parent].balance++;
                        }
                        else
                        {
                            nodes[parent].right_child = false;
                            nodes[parent].right = nodes[node].right;
                            nodes[parent].balance--;
                        }
                    }
                    else // node has a right child
                    {

                        /*[Feature(Feature.Dict)]*/
                        successor = g_tree_node_next(node);
                        nodes[successor].left = nodes[node].left;

                        NodeRef rightChild = nodes[node].right;
                        if (parent == Null)
                        {
                            root = rightChild;
                        }
                        else if (left_node)
                        {
                            nodes[parent].left = rightChild;
                            nodes[parent].balance++;
                        }
                        else
                        {
                            nodes[parent].right = rightChild;
                            nodes[parent].balance--;
                        }
                    }
                }
                else // node has a left child
                {
                    if (!nodes[node].right_child)
                    {
                        NodeRef predecessor;

                        /*[Feature(Feature.Dict)]*/
                        predecessor = g_tree_node_previous(node);
                        nodes[predecessor].right = nodes[node].right;

                        NodeRef leftChild = nodes[node].left;
                        if (parent == Null)
                        {
                            root = leftChild;
                        }
                        else if (left_node)
                        {
                            nodes[parent].left = leftChild;
                            nodes[parent].balance++;
                        }
                        else
                        {
                            nodes[parent].right = leftChild;
                            nodes[parent].balance--;
                        }
                    }
                    else // node has a both children (pant, pant!)
                    {
                        NodeRef predecessor = nodes[node].left;
                        successor = nodes[node].right;
                        NodeRef successorParent = node;
                        int old_idx = idx + 1;
                        idx++;

                        /* path[idx] == parent */
                        /* find the immediately next node (and its parent) */
                        while (nodes[successor].left_child)
                        {
                            path[++idx] = successorParent = successor;
                            successor = nodes[successor].left;
                        }

                        path[old_idx] = successor;
                        balance = path[idx];

                        /* remove 'successor' from the tree */
                        if (successorParent != node)
                        {
                            if (nodes[successor].right_child)
                            {
                                NodeRef successorRightChild = nodes[successor].right;

                                nodes[successorParent].left = successorRightChild;
                            }
                            else
                            {
                                nodes[successorParent].left_child = false;
                            }
                            nodes[successorParent].balance++;

                            nodes[successor].right_child = true;
                            nodes[successor].right = nodes[node].right;
                        }
                        else
                        {
                            nodes[node].balance--;
                        }

                        // set the predecessor's successor link to point to the right place
                        while (nodes[predecessor].right_child)
                        {
                            predecessor = nodes[predecessor].right;
                        }
                        nodes[predecessor].right = successor;

                        /* prepare 'successor' to replace 'node' */
                        NodeRef leftChild = nodes[node].left;
                        nodes[successor].left_child = true;
                        nodes[successor].left = leftChild;
                        nodes[successor].balance = nodes[node].balance;

                        if (parent == Null)
                        {
                            root = successor;
                        }
                        else if (left_node)
                        {
                            nodes[parent].left = successor;
                        }
                        else
                        {
                            nodes[parent].right = successor;
                        }
                    }
                }

                /* restore balance */
                if (balance != Null)
                {
                    while (true)
                    {
                        NodeRef bparent = path[--idx];
                        Debug.Assert((bparent == Null) || (nodes[bparent].left == balance) || (nodes[bparent].right == balance));
                        left_node = (bparent != Null) && (balance == nodes[bparent].left);

                        if ((nodes[balance].balance < -1) || (nodes[balance].balance > 1))
                        {
                            balance = g_tree_node_balance(balance);
                            if (bparent == Null)
                            {
                                root = balance;
                            }
                            else if (left_node)
                            {
                                nodes[bparent].left = balance;
                            }
                            else
                            {
                                nodes[bparent].right = balance;
                            }
                        }

                        if ((nodes[balance].balance != 0) || (bparent == Null))
                        {
                            break;
                        }

                        if (left_node)
                        {
                            nodes[bparent].balance++;
                        }
                        else
                        {
                            nodes[bparent].balance--;
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

        private NodeRef g_tree_node_balance(NodeRef node)
        {
            unchecked
            {
                if (nodes[node].balance < -1)
                {
                    if (nodes[nodes[node].left].balance > 0)
                    {
                        nodes[node].left = g_tree_node_rotate_left(nodes[node].left);
                    }
                    node = g_tree_node_rotate_right(node);
                }
                else if (nodes[node].balance > 1)
                {
                    if (nodes[nodes[node].right].balance < 0)
                    {
                        nodes[node].right = g_tree_node_rotate_right(nodes[node].right);
                    }
                    node = g_tree_node_rotate_left(node);
                }

                return node;
            }
        }

        private NodeRef g_tree_node_rotate_left(NodeRef node)
        {
            unchecked
            {
                NodeRef right = nodes[node].right;

                if (nodes[right].left_child)
                {

                    nodes[node].right = nodes[right].left;
                }
                else
                {
                    nodes[node].right_child = false;
                    nodes[right].left_child = true;
                }
                nodes[right].left = node;

                int a_bal = nodes[node].balance;
                int b_bal = nodes[right].balance;

                if (b_bal <= 0)
                {
                    if (a_bal >= 1)
                    {
                        nodes[right].balance = (sbyte)(b_bal - 1);
                    }
                    else
                    {
                        nodes[right].balance = (sbyte)(a_bal + b_bal - 2);
                    }
                    nodes[node].balance = (sbyte)(a_bal - 1);
                }
                else
                {
                    if (a_bal <= b_bal)
                    {
                        nodes[right].balance = (sbyte)(a_bal - 2);
                    }
                    else
                    {
                        nodes[right].balance = (sbyte)(b_bal - 1);
                    }
                    nodes[node].balance = (sbyte)(a_bal - b_bal - 1);
                }

                return right;
            }
        }

        private NodeRef g_tree_node_rotate_right(NodeRef node)
        {
            unchecked
            {
                NodeRef left = nodes[node].left;

                if (nodes[left].right_child)
                {

                    nodes[node].left = nodes[left].right;
                }
                else
                {
                    nodes[node].left_child = false;
                    nodes[left].right_child = true;
                }
                nodes[left].right = node;

                int a_bal = nodes[node].balance;
                int b_bal = nodes[left].balance;

                if (b_bal <= 0)
                {
                    if (b_bal > a_bal)
                    {
                        nodes[left].balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        nodes[left].balance = (sbyte)(a_bal + 2);
                    }
                    nodes[node].balance = (sbyte)(a_bal - b_bal + 1);
                }
                else
                {
                    if (a_bal <= -1)
                    {
                        nodes[left].balance = (sbyte)(b_bal + 1);
                    }
                    else
                    {
                        nodes[left].balance = (sbyte)(a_bal + b_bal + 2);
                    }
                    nodes[node].balance = (sbyte)(a_bal + 1);
                }

                return left;
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private NodeRef g_tree_find_node(KeyType key)
        {
            NodeRef node = root;
            if (node == Null)
            {
                return Null;
            }

            while (true)
            {
                int cmp = comparer.Compare(key, nodes[node].key);
                if (cmp == 0)
                {
                    return node;
                }
                else if (cmp < 0)
                {
                    if (!nodes[node].left_child)
                    {
                        return Null;
                    }

                    node = nodes[node].left;
                }
                else
                {
                    if (!nodes[node].right_child)
                    {
                        return Null;
                    }

                    node = nodes[node].right;
                }
            }
        }

        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private bool Find(            KeyType key,            out NodeRef match,            [Widen] out int xPositionMatch)
        {
            unchecked
            {
                match = Null;
                xPositionMatch = 0;

                NodeRef successor = Null;
                NodeRef lastGreaterAncestor = Null;
                if (root != Null)
                {
                    NodeRef current = root;
                    while (true)
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
                            if (!nodes[current].left_child)
                            {
                                break;
                            }
                            current = nodes[current].left;
                        }
                        else
                        {
                            if (!nodes[current].right_child)
                            {
                                break;
                            }
                            current = nodes[current].right; // continue the search in right sub tree after we find a match
                        }
                    }
                }

                if (match != Null)
                {
                    Debug.Assert(successor != Null);
                }

                return match != Null;
            }
        }

        // INonInvasiveTreeInspection

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.Root { get { return root != Null ? (object)root : null; } }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetLeftChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].left_child ? (object)nodes[n].left : null;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetRightChild(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].right_child ? (object)nodes[n].right : null;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetKey(object node)
        {
            NodeRef n = (NodeRef)node;
            object key = null;
            key = nodes[n].key;
            return key;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetValue(object node)
        {
            object value = null;
            return value;
        }

        [ExcludeFromCodeCoverage]
        object INonInvasiveTreeInspection.GetMetadata(object node)
        {
            NodeRef n = (NodeRef)node;
            return nodes[n].balance;
        }

        [ExcludeFromCodeCoverage]
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

                    if (nodes[node].left_child)
                    {
                        worklist.Enqueue(nodes[node].left);
                    }
                    if (nodes[node].right_child)
                    {
                        worklist.Enqueue(nodes[node].right);
                    }
                }
            }

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
        private int MaxDepth(NodeRef node)
        {
            int ld = nodes[node].left_child ? MaxDepth(nodes[node].left) : 0;
            int rd = nodes[node].right_child ? MaxDepth(nodes[node].right) : 0;
            return 1 + Math.Max(ld, rd);
        }

        [ExcludeFromCodeCoverage]
        private void g_tree_node_check(NodeRef node)
        {
            if (node != Null)
            {
                if (nodes[node].left_child)
                {
                    NodeRef tmp = g_tree_node_previous(node);
                    if (!(nodes[tmp].right == node))
                    {
                        Debug.Assert(false, "program defect");
                        throw new InvalidOperationException("invariant");
                    }
                }

                if (nodes[node].right_child)
                {
                    NodeRef tmp = g_tree_node_next(node);
                    if (!(nodes[tmp].left == node))
                    {
                        Debug.Assert(false, "program defect");
                        throw new InvalidOperationException("invariant");
                    }
                }

                int left_height = 0;
                int right_height = 0;

                if (nodes[node].left_child)
                {
                    left_height = g_tree_node_height(nodes[node].left);
                }
                if (nodes[node].right_child)
                {
                    right_height = g_tree_node_height(nodes[node].right);
                }

                int balance = right_height - left_height;
                if (!(balance == nodes[node].balance))
                {
                    Debug.Assert(false, "program defect");
                    throw new InvalidOperationException("invariant");
                }

                if (nodes[node].left_child)
                {
                    g_tree_node_check(nodes[node].left);
                }
                if (nodes[node].right_child)
                {
                    g_tree_node_check(nodes[node].right);
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private int g_tree_node_height(NodeRef node)
        {
            if (node != Null)
            {
                int left_height = 0;
                int right_height = 0;

                if (nodes[node].left_child)
                {
                    left_height = g_tree_node_height(nodes[node].left);
                }

                if (nodes[node].right_child)
                {
                    right_height = g_tree_node_height(nodes[node].right);
                }

                return Math.Max(left_height, right_height) + 1;
            }

            return 0;
        }


        //
        // Enumeration
        //

        /// <summary>
        /// Get the default enumerator, which is the fast enumerator for AVL trees.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntryList<KeyType>> GetEnumerator()
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
        public IEnumerable<EntryList<KeyType>> GetRobustEnumerable()
        {
            return new RobustEnumerableSurrogate(this);
        }

        public struct RobustEnumerableSurrogate : IEnumerable<EntryList<KeyType>>
        {
            private readonly AVLTreeArrayList<KeyType> tree;

            public RobustEnumerableSurrogate(AVLTreeArrayList<KeyType> tree)
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

        /// <summary>
        /// Get the fast enumerator. The fast enumerator uses an internal stack of nodes to peform in-order traversal of the
        /// tree structure. Because it uses the tree structure, it is invalidated if the tree is modified by an insertion or
        /// deletion and will throw an InvalidOperationException when next advanced. The complexity of the fast enumerator
        /// is O(1) per element, or O(N) to enumerate the entire tree.
        /// </summary>
        /// <returns>An IEnumerable which can be used in a foreach statement</returns>
        public IEnumerable<EntryList<KeyType>> GetFastEnumerable()
        {
            return new FastEnumerableSurrogate(this);
        }

        public struct FastEnumerableSurrogate : IEnumerable<EntryList<KeyType>>
        {
            private readonly AVLTreeArrayList<KeyType> tree;

            public FastEnumerableSurrogate(AVLTreeArrayList<KeyType> tree)
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
            private readonly AVLTreeArrayList<KeyType> tree;
            private bool started;
            private bool valid;
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
            private KeyType currentKey;

            public RobustEnumerator(AVLTreeArrayList<KeyType> tree)
            {
                this.tree = tree;
                Reset();
            }

            public EntryList<KeyType> Current
            {
                get
                {

                    if (valid)
                        /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/
                        {
                            KeyType key = currentKey;

                            return new EntryList<KeyType>(
                                /*[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]*/key);
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
            private readonly AVLTreeArrayList<KeyType> tree;
            private ushort version;
            private NodeRef currentNode;
            private NodeRef nextNode;

            private readonly Stack<STuple<NodeRef>> stack
                = new Stack<STuple<NodeRef>>();

            public FastEnumerator(AVLTreeArrayList<KeyType> tree)
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

            private void PushSuccessor(                NodeRef node)
            {
                while (node != tree.Null)
                {

                    stack.Push(new STuple<NodeRef>(
                        node));
                    node = tree.nodes[node].left_child ? tree.nodes[node].left : tree.Null;
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

                if (tree.nodes[nextNode].right_child)
                {
                    PushSuccessor(
                        tree.nodes[nextNode].right);
                }
            }
        }


        //
        // Cloning
        //

        public object Clone()
        {
            return new AVLTreeArrayList<KeyType>(this);
        }
    }
}
