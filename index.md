TreeLib: Balanced Binary Trees - Rank Augmented, for .NET
===

[TOC]

What's in TreeLib?
---
TreeLib provides three self-balancing binary tree implementations ([Red-Black][1], [AVL][2], and [Splay][3]), optionally augmented with rank information. Each tree specialization can be configured as a key collection or a collection of key-value pairs. Rank information provides the ability to quickly compute distribution information at any time (e.g. median or Nth percentile), as well as providing the basis for sparse collections and range mappings.

The source code is written in C# and the assemblies will work with any .NET language.

Where did the implementations come from?
---
 The Splay tree comes from code written by the inventor, Daniel Sleator, available in basic form from http://www.link.cs.cmu.edu/link/ftp-site/splaying/top-down-splay.c.

The Red-Black tree comes from the now-open-sourced Microsoft .NET Framework Base Class Library implementation of SortedSet, at https://github.com/dotnet/corefx/blob/master/src/System.Collections/src/System/Collections/Generic/SortedSet.cs

The AVL tree comes from the Glib library maintained by GNOME project, at https://github.com/GNOME/glib/blob/master/glib/gtree.c

The rank-augmentation code was written by [Thomas R. Lawrence][4].

How do I get TreeLib?
---
Coming soon: NuGet

The project is available in source form at https://github.com/programmatom/TreeLib.

How is it licensed?
---
TreeLib is made available under the GNU Lesser General Public License (LGPL). It may be used,, unmodified, in proprietary software without forcing the source to that software to be disclosed. If it is modified, per the license, the source of the modification must be made available (source for other parts of the software that use the modified library do not need to be made available).

How do I use it?
---
TreeLib comes as four assemblies:
Assembly | Description
---|---
*TreeLibInterface.dll*|The public interfaces implemented by specializations.
*TreeLib.dll*|All implementations of all the trees in a "standard" form.
*TreeLibLong.dll*|All the rank-augmented trees modified to use 64-bit integers for rank information rather than 32-bit integers.
*TreeLibArray.dll*|Variants of the trees that store their internal nodes in a single array rather than linked heap objects.

Of these, *TreeLibInterface.dll* is the only one that is required. Of the other assemblies, you can include only the ones containing implementations you need.

What's in those assemblies? How do I actually create a tree?
---
The implementations of the trees exist as specializations that enable specific features. The tree classes are named using a hierarchical naming scheme that identifies what features the tree supports. To understand what specializations are available and how to select them, read through this sequence which describes how the classes are named, left to right.
### Base Tree Type
The name starts with the base type of the tree, selected from the three types provided:
Base Type|
---|
`AVLTree`|
`RedBlackTree`|
`SplayTree`|
### Storage Mechanism
The next step indicates the storage mechanism for the underlying tree. The standard mechanism (heap-allocated nodes) does not add anything to the name.
Storage Mechanism|Description
---|---
<empty>|Heap-allocated nodes.
`Array`|Nodes embedded as structs in a single array with an embedded free list.
### Tree Specialization
The specialization is where most of the details get specified. This identifies the particular style of mapping, based around the three types, collection of key-value pairs, collection of keys, and collection of ranges. Given these three basic types, there are some additional flavors, identified below. Trees that take a key and/or value type are specified in the standard way for generic types.
Specialization|Description
---|---
`Map<TKey, TValue>`|A collection of key-value pairs
`List<TKey>`|A collection of keys
`RankMap<TKey, TValue>`|A collection of key-value pairs incorporating rank information. The rank information allows elements to be indexed as if in a sorted array, as well as by key.
`RankList<TKey>`|A collection of keys incorporating rank information. The rank information is as for RankMap.
`MultiRankMap<TKey, TValue>`|A collection of key-value pairs incorporating rank information. The `count` may be a number greater than or equal to 1. The indexing is equivalent to that of a sorted array where each key-value pair is present multiple times, as identified by the *count* for each key-value pair.
`MultiRankList<TKey>`|A collection of keys incorporating rank information. The rank information is as for MultiRankMap.
`RangeMap<TValue>`|A collection of ranges with each range associated with a value. The ranges are defined by their lengths, which must be at least 1. Conceptually, the tree assigns start indices for each range, beginning with 0, adding the length from each range to derive the start index for the next range. The ranges do not have keys, but are instead inserted and deleted based on their start index. The tree maintains the ranges in the sequence resulting from the insertions and deletions.
`RangeList`|A collection of ranges with no associated values. The indexing of the ranges is as for RangeMap.
`Range2Map<TValue>`|A mapping of one set of ranges to another set of ranges, with each map entry associated with a value. This is similar to RangeMap, except there are two sides to the collection, called X and Y. The sequence of the range pairs is maintained on both sides as a result of the insertions and deletions performed on the tree.
`Range2List`|A mapping of one set of ranges to another, with no associated values. The indexing of the ranges is as for Range2Map.

# Width of Rank/Range numbers
The rank or range index values can be either an integer or a long. The integer case is specified by no additional qualifier.
Qualifier|Width
---|---
<empty>|Signed 32-bit integer (`int`)
`Long`|Signed 64-bit integer (`long`)

How do I create a tree?
---
Each tree has a set of constructors allowing the allocation of the internal data structures to be controlled. First, it is necessary to understand the allocation behaviors available. These are specified with the `AllocationMode` enumeration.
AllocationMode|Description
---|---
`DynamicDiscard`|Nodes are allocated on the heap and discarded upon removal. At that point, the garbage collector can immediately reclaim the memory used by the node.
`DynamicRetainFreelist`|Nodes are allocated from a free list, if not empty, with fallback to the heap. Upon removal, the node object is returned to the free list.
`PreallocatedFixed`|The specified number of nodes is allocated upon tree creation and added to the free list. Allocations obtain node objects from the free list and deletions return the objects to the free list. If the free list is exhausted (i.e. the tree already contains the specified number of elements and an insert is attempted) an `OutOfMemory` exception is thrown.
The mode typically used in most scenarios would be `DynamicDiscard`. The other modes are provided to allow control over timing of allocations. `PreallocatedFixed` is used when the size of the tree is known to be limited and allocations during use of the tree cannot be tolerated, such as in the case where the application must meet hard deadlines (e.g. real-time scenarios). This is also the only storage mode permitted for the `Array` storage mechanism. `DynamicRetainFreelist` can be used make a certain number of allocations occur up front during tree construction while permitting the tree to grow beyond the initial capacity.

The following constructors are available:
- **`new Tree<...>`** - create an empty tree using `DynamicDiscard` as it's allocation mode (except for the `Array` storage mechanism, which must use `PreallocatedFixed`). Keys are compared using the default comparer (`Comparer<TKey>.Default`).
- **`new Tree<TKey, ...>(IComparer<TKey> comparer)`** - create an empty tree using the specified comparer for keys and the default allocation mode described above.
- **`new Tree<>(uint capacity, AllocationMode allocationMode)`** - create an empty tree, preallocating `capacity` nodes using the specified allocation mode and with the default comparer.
- **`new Tree<TKey, ...>(IComparer<TKey> comparer, uint capacity, AllocationMode allocationMode)`** - create an empty tree, preallocating `capacity` nodes using the specified allocation mode and with the specified comparer.
- **`new Tree<>(Tree<> original)`** - create a tree that is an exact clone of the provided tree.
Any type parameters for key or value (if applicable) are specified in the type name of the construction. Constructors taking an explicit comparer are not available for the range collections, as they do not have keys.

How do the interfaces work?
---
Each specialization implements a specific interface appropriate to the specialization. The interface identifies the accessors that make sense for that particular type of tree. Here are the interfaces associated with each specialization type:
Specialization Qualifier|Interface Name
---|---
`Map<TKey, TValue>`|`IOrderedMap<TKey, TValue>`
`List<TKey>`|`IOrderedList<TKey>`
`RankMap<TKey, TValue>`|`IRankMap<TKey, TValue>`
`RankList<TKey>`|`IRankList<TKey>`
`MultiRankMap<TKey, TValue>`|`IMultiRankMap<TKey, TValue>`
`MultiRankList<TKey>`|`IMultiRankList<TKey>`
`RangeMap<TValue>`|`IRangeMap<TValue>`
`RangeList`|`IRangeList`
`Range2Map<TValue>`|`IRange2Map<TValue>`
`Range2List`|`IRange2List`
`RankMapLong<TKey, TValue>`|`IRankMapLong<TKey, TValue>`
`RankListLong<TKey>`|`IRankListLong<TKey>`
`MultiRankMapLong<TKey, TValue>`|`IMultiRankMapLong<TKey, TValue>`
`MultiRankListLong<TKey>`|`IMultiRankListLong<TKey>`
`RangeMapLong<TValue>`|`IRangeMapLong<TValue>`
`RangeListLong`|`IRangeListLong`
`Range2Map<TValue>`|`IRange2MapLong<TValue>`
`Range2ListLong`|`IRange2ListLong`
The interfaces exist to allow consumers of the collections to be decoupled from the specific base type (AVL, Red-Black, or Splay) being used. The interfaces are not required - the tree type may be used directly, if desired.

What about enumeration?
---
Each basic tree type provides two enumerators, the *robust* enumerator and the *fast* enumerator.
Enumerator|Description
---|---
Robust|The robust enumerator uses an internal key cursor and queries the tree using the `NextGreater()` method to advance the enumerator. This enumerator is robust because it tolerates changes to the underlying tree. If a key is inserted or removed and it comes *before* the enumerator's current key in sorting order, it will have no affect on the enumerator. If a key is inserted or removed and it comes *after* the enumerator's current key (i.e. in the portion of the collection the enumerator hasn't visited yet), the enumerator will include the key if inserted or skip the key if removed. Because the enumerator queries the tree for each element it's running time per element is O(lg N), or O(N lg N) to enumerate the entire tree.
Fast|The fast enumerator uses an internal stack of nodes to peform in-order traversal of the tree structure. Because it uses the tree structure, it is invalidated if the tree is modified by an insertion or deletion and will throw an `InvalidOperationException` when next advanced. For some types of trees (Red-Black), a failed insertion or deletion will still invalidate the enumerator, as failed operations may still have performed rotations in the tree. For the Splay tree, all operations modify the tree structure, include queries, and will invalidate the enumerator. The complexity of the fast enumerator is O(1) per element, or O(N) to enumerate the entire tree.
**A note about splay trees and enumeration**: Enumeration of splay trees is generally problematic, for two reasons. First, every operation on a splay tree modifies the structure of the tree, including queries. Second, splay trees may have depth of N in the worst case (as compared to other trees which are guaranteed to be less deep than approximately two times the optimal depth, or 2 lg N). The first property makes fast enumeration less useful, and the second property means fast enumeration may consume up to memory proportional to N for the internal stack used for traversal. Therefore, the *robust* enumerator is recommended for splay trees. The drawback is robust's O(N lg N) complexity.

The default enumerators for the tree base types are listed below. These are the enumerators returned from the `IEnumerable<>` interface implemented by each tree:
Tree Type|Default Enumerator
---|---
AVL|Fast
Red-Black|Fast
Splay|Robust
Each type of enumerator is explicitly available on the tree by calling either the `GetRobustEnumerable()` method or the `GetFastEnumerable()` method.

[1]: https://en.wikipedia.org/wiki/Red-black_tree
[2]: https://en.wikipedia.org/wiki/AVL_tree
[3]: https://en.wikipedia.org/wiki/Splay_tree
[4]: https://github.com/programmatom
