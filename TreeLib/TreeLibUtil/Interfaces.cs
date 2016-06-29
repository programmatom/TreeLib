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

using TreeLib.Internal;

namespace TreeLib
{
    //
    // IHugeList
    //

    /// <summary>
    /// Defines the signature for the iteration callback invoked by IHugeList&lt;&gt;.IterateRange(). The method is invoked
    /// for each item in the range of the collection being enumerated
    /// </summary>
    /// <typeparam name="T">Type of item contained in the collection</typeparam>
    /// <param name="v">An updatable ref to the collection item</param>
    /// <param name="x">An updatable ref to the item in the external array passed into IterateRange()</param>
    public delegate void IterateOperator<T>(ref T v, ref T x);

    /// <summary>
    /// Defines the signature for the iteration callback used by IHugeList&lt;&gt;.IterateRangeBatch(). The method is invoked
    /// in sequence for segments of the collection.
    /// </summary>
    /// <typeparam name="T">Type of item contained in the collection</typeparam>
    /// <param name="v">The array of items contained in the collection. Items in the array may be modified.</param>
    /// <param name="vOffset">The offset into the array 'v' of the first item</param>
    /// <param name="x">The external array passed into IterateRangeBatch()</param>
    /// <param name="xOffset">The offset into the array 'x' of the first item</param>
    /// <param name="count">The number of items in array 'v' and 'x' to be processed during this invocation</param>
    public delegate void IterateOperatorBatch<T>(T[] v, int vOffset, T[] x, int xOffset, int count);

    /// <summary>
    /// Interface for Huge List collections. An implementation of this interface behaves as close as possible to IList&lt;T&gt;
    /// or List&lt;T&gt;
    /// </summary>
    /// <typeparam name="T">Type of item contained in the collection</typeparam>
    [DocumentationSource]
    public interface IHugeList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IChunkedEnumerable<EntryRangeMap<T[]>>
    {
        /// <summary>
        /// The maximum size of a single segment (or chunk) stored in the collection. There is a tradeoff in this value: smaller
        /// values reduce the array copying cost of insertion and deletion but increase the overhead of segment management.
        /// Larger values increase the array copying cost of insertion and deletion but reduce overhead of segment management.
        /// The default value is 512.
        /// </summary>
        int MaxBlockSize { get; }

        //int Count { get; }

        //bool IsReadOnly { get; }

        //T this[int index] { get; set; }

        /// <summary>
        /// Insert a range of default values into the collection. This is equivalent to
        /// <code>InsertRange(index, new T[count], 0, count)</code>
        /// but avoids the allocation.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="count">number of default-valued items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        void InsertRangeDefault(int index, int count);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="items">the array of items to insert into the colletion</param>
        /// <param name="offset">the index of the first item in 'items' to insert</param>
        /// <param name="count">number of default-valued items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        /// <exception cref="ArgumentException">index, count, or offset exceeds the length of the collection or array</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void InsertRange(int index, T[] items, int offset, int count);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="items">the array of items to insert into the colletion</param>
        /// <param name="offset">the index of the first item in 'items' to insert</param>
        /// <param name="count">number of default-valued items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        /// <exception cref="ArgumentException">index, count, or offset exceeds the length of the collection or array</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void InsertRange(int index, IHugeList<T> items, int offset, int count);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="items">the array of items to insert into the colletion</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        /// <exception cref="ArgumentException">index exceeds the length of the collection or</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void InsertRange(int index, T[] items);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="collection">an enumerable collection of items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">index is less than 0</exception>
        /// <exception cref="ArgumentException">index exceeds the length of the collection</exception>
        /// <exception cref="ArgumentNullException">'collection' is null</exception>
        void InsertRange(int index, IEnumerable<T> collection);

        //void Insert(int index, T item);

        //void Add(T item);

        /// <summary>
        /// Append a range of values to the end of the collection
        /// </summary>
        /// <param name="items">the array of values to append</param>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void AddRange(T[] items);

        /// <summary>
        /// Append a range of values to the end of the collection
        /// </summary>
        /// <param name="collection">an enumerable collection of items to insert</param>
        /// <exception cref="ArgumentNullException">'collection' is null</exception>
        void AddRange(IEnumerable<T> collection);

        /// <summary>
        /// Append a range of values to the end of the collection
        /// </summary>
        /// <param name="collection">an enumerable collection of items to insert</param>
        /// <exception cref="ArgumentNullException">'collection' is null</exception>
        void AddRange(IHugeList<T> collection);

        /// <summary>
        /// Remove a range of values from the collection
        /// </summary>
        /// <param name="index">the index of the first item to remove from the collection</param>
        /// <param name="count">the number of items to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">index or count is less than 0</exception>
        /// <exception cref="ArgumentException">index or count would exceed the end of the collection</exception>
        void RemoveRange(int index, int count);

        //void RemoveAt(int index);

        //bool Remove(T item);

        /// <summary>
        /// Remove all items from the collection for which the 'match' function returns true.
        /// </summary>
        /// <param name="match">a callback function that tests each item</param>
        /// <returns>the number of items removed</returns>
        /// <exception cref="ArgumentNullException">match is null</exception>
        int RemoveAll(Predicate<T> match);

        /// <summary>
        /// Replace a range of items in the collecction with another range of items. It is equivalent to 
        /// <code>RemoveRange(index, count)</code> followed by <code>InsertRange(index, items, offset, count2)</code>
        /// </summary>
        /// <param name="index">the index of the first item in the collection to remove</param>
        /// <param name="count">the number of items in the collection to remove</param>
        /// <param name="items">the array of items to insert into the collection</param>
        /// <param name="offset">the index in 'items' of the first item to insert</param>
        /// <param name="count2">the number of items to insert from 'items'</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the index parameters are less than 0</exception>
        /// <exception cref="ArgumentException">index and count would exceed the end of the collection or
        /// offset and count2 would exceed the end of 'items'</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void ReplaceRange(int index, int count, T[] items, int offset, int count2);

        /// <summary>
        /// Replace a range of items in the collecction with another range of items. It is equivalent to 
        /// <code>RemoveRange(index, count)</code> followed by <code>InsertRange(index, items, 0, items.Length)</code>
        /// </summary>
        /// <param name="index">the index of the first item in the collection to remove</param>
        /// <param name="count">the number of items in the collection to remove</param>
        /// <param name="items">the array of items to insert into the collection</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the index parameters are less than 0</exception>
        /// <exception cref="ArgumentException">index and count would exceed the end of the collection</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void ReplaceRange(int index, int count, T[] items);

        //void Clear();

        /// <summary>
        /// Copy a range of items from the collection to an array.
        /// </summary>
        /// <param name="index">the index of the first item in the collection that will by copied</param>
        /// <param name="array">the array to copy items into</param>
        /// <param name="arrayIndex">the index of the first position in 'array' to copy into</param>
        /// <param name="count">the number of items to copy</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the index parameters are less than 0</exception>
        /// <exception cref="ArgumentException">index and count would exceed the end of the collection or
        /// arrayIndex and count would exceed the end of 'array'</exception>
        /// <exception cref="ArgumentNullException">'array' is null</exception>
        void CopyTo(int index, T[] array, int arrayIndex, int count);

        //void CopyTo(T[] items, int arrayIndex);

        /// <summary>
        /// Copy the entire collection into an array.
        /// </summary>
        /// <param name="array">the array to copy into</param>
        /// <exception cref="ArgumentException">the array is not large enough to contain all items from the collecion</exception>
        /// <exception cref="ArgumentNullException">'array' is null</exception>
        void CopyTo(T[] array);

        /// <summary>
        /// Iterate over a range of items in the collection, optionally updating the value of each item.
        /// </summary>
        /// <param name="index">the position of the first item to iterate</param>
        /// <param name="external">an external array that is passed through to the callback</param>
        /// <param name="externalOffset">an offset into 'external' that is passed through to the callback</param>
        /// <param name="count">the number of items to iterate over</param>
        /// <param name="op">the callback function</param>
        /// <exception cref="ArgumentOutOfRangeException">index or count are less than 0</exception>
        /// <exception cref="ArgumentException">index or count would exceed the end of the collection</exception>
        void IterateRange(int index, T[] external, int externalOffset, int count, IterateOperator<T> op);

        /// <summary>
        /// Iterate over a range of items in the collection in batches, optionally updating the value of each item.
        /// </summary>
        /// <param name="index">the position of the first item to iterate</param>
        /// <param name="external">an external array that is passed through to the callback</param>
        /// <param name="externalOffset">an offset into 'external' that is passed through to the callback</param>
        /// <param name="count">the number of items to iterate over</param>
        /// <param name="op">the callback function</param>
        /// <exception cref="ArgumentOutOfRangeException">index or count are less than 0</exception>
        /// <exception cref="ArgumentException">index or count would exceed the end of the collection</exception>
        void IterateRangeBatch(int index, T[] external, int externalOffset, int count, IterateOperatorBatch<T> op);

        /// <summary>
        /// Searches a range of items in the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item. If multi is true, the index of the first item in a run of
        /// duplicates is returned.
        /// </summary>
        /// <param name="start">the index of the first item in the range of items to search</param>
        /// <param name="count">the number of items in the range to search</param>
        /// <param name="item">the value of an item to search for</param>
        /// <param name="comparer">the comparer to use for searching. If null, Comparer&lt;T&gt;.Default</param>
        /// <param name="multi">specifies that the index of the first item in a run of duplicates should be returned</param>
        /// <returns>The position of the item in the sorted collection if item is found, otherwise, a negative number that
        /// is the bitwise complement of the index of the next item that is larger than item or, if there is no
        /// larger item, the bitwise complement of Count</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="InvalidOperationException">comparer is null and there is no Comparer&lt;T&gt;.Default
        /// available for type T</exception>
        int BinarySearch(int start, int count, T item, IComparer<T> comparer, bool multi);

        /// <summary>
        /// Searches a range of items in the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item.
        /// </summary>
        /// <param name="start">the index of the first item in the range of items to search</param>
        /// <param name="count">the number of items in the range to search</param>
        /// <param name="item">the value of an item to search for</param>
        /// <param name="comparer">the comparer to use for searching. If null, Comparer&lt;T&gt;.Default</param>
        /// <returns>The position of the item in the sorted collection if item is found, otherwise, a negative number that
        /// is the bitwise complement of the index of the next item that is larger than item or, if there is no
        /// larger item, the bitwise complement of Count</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="InvalidOperationException">comparer is null and there is no Comparer&lt;T&gt;.Default
        /// available for type T</exception>
        int BinarySearch(int start, int count, T item, IComparer<T> comparer);

        /// <summary>
        /// Searches the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item.
        /// </summary>
        /// <param name="item">the value of an item to search for</param>
        /// <param name="comparer">the comparer to use for searching. If null, Comparer&lt;T&gt;.Default</param>
        /// <returns>The position of the item in the sorted collection if item is found, otherwise, a negative number that
        /// is the bitwise complement of the index of the next item that is larger than item or, if there is no
        /// larger item, the bitwise complement of Count</returns>
        /// <exception cref="InvalidOperationException">comparer is null and there is no Comparer&lt;T&gt;.Default
        /// available for type T</exception>
        int BinarySearch(T item, IComparer<T> comparer);

        /// <summary>
        /// Searches the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item.
        /// </summary>
        /// <param name="item">the value of an item to search for</param>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int BinarySearch(T item);

        /// <summary>
        /// Searches for the first occurrence in the range of 'count' items starting at 'start' of any of the items in
        /// the array 'values'
        /// </summary>
        /// <param name="values">the array of items to search for</param>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="count">the number of items to search</param>
        /// <returns>the index of the first item of any of the items from 'values' found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="ArgumentNullException">values is null</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int IndexOfAny(T[] values, int start, int count);

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence in the range of 'count' items
        /// starting at 'start'.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="count">the number of items to search</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int IndexOf(T value, int start, int count);

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence in the range of items
        /// starting at 'start' through the end of the collection.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start is less than 0</exception>
        /// <exception cref="ArgumentException">start is greater than the size of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int IndexOf(T value, int start);

        //int IndexOf(T value);

        /// <summary>
        /// REturns the index of the first item for which 'match' returns true in the range of 'count' items
        /// starting at 'start'.
        /// </summary>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="count">the number of items to search</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        int FindIndex(int start, int count, Predicate<T> match);

        /// <summary>
        /// REturns the index of the first item for which 'match' returns true in the range of items
        /// starting at 'start' through the end of the collection
        /// </summary>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start is less than 0</exception>
        /// <exception cref="ArgumentException">start greater than the size of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        int FindIndex(int start, Predicate<T> match);

        /// <summary>
        /// REturns the index of the first item for which 'match' returns true in the entire collection
        /// </summary>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentNullException">match is null</exception>
        int FindIndex(Predicate<T> match);

        /// <summary>
        /// Returns the index of the last occurrence within the range of items in the collection of any of the
        /// items in 'values'
        /// </summary>
        /// <param name="values">the array of items to search for</param>
        /// <param name="start">The starting index of the backward search</param>
        /// <param name="count">the number of items to search backward through</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count exceeds the bounds of the collection</exception>
        /// <exception cref="ArgumentNullException">values is null</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int LastIndexOfAny(T[] values, int start, int count);

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the
        /// range of items in the collection that contains the specified number of items and ends at the specified index.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="index">The starting index of the backward search</param>
        /// <param name="count">the number of items to search backward through</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count exceeds the bounds of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int LastIndexOf(T value, int index, int count);

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the
        /// range of items in the collection from the beginning through the specified index.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="index">The starting index of the backward search</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start exceeds the bounds of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int LastIndexOf(T value, int index);

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the entire collection.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <returns>the index of the last item found in the collection, or -1 if no item was found</returns>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        int LastIndexOf(T value);

        /// <summary>
        /// Searches for the item for which 'match' returns true and returns the index of the last occurrence within the
        /// range of items in the collection that contains the specified number of items and ends at the specified index.
        /// </summary>
        /// <param name="start">The starting index of the backward search</param>
        /// <param name="count">the number of items to search backward through</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count exceeds the bounds of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        int FindLastIndex(int start, int count, Predicate<T> match);

        /// <summary>
        /// Searches for the item for which 'match' returns true and returns the index of the last occurrence within the
        /// range of items in the collection from the beginning through the specified index.
        /// </summary>
        /// <param name="start">The starting index of the backward search</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start is less than 0</exception>
        /// <exception cref="ArgumentException">start exceeds the bounds of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        int FindLastIndex(int start, Predicate<T> match);

        /// <summary>
        /// Searches for the item for which 'match' returns true and returns the index of the last occurrence within the
        /// entire collection.
        /// </summary>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentNullException">match is null</exception>
        int FindLastIndex(Predicate<T> match);

        //bool Contains(T value);

        /// <summary>
        /// Return an array containing all items in the collection in collection order
        /// </summary>
        /// <returns>the array containing all the items in collection order</returns>
        T[] ToArray();
    }

    /// <summary>
    /// Defines the signature for the iteration callback used by IHugeList&lt;&gt;.IterateRangeBatch(). The method is invoked
    /// in sequence for segments of the collection.
    /// </summary>
    /// <typeparam name="T">Type of item contained in the collection</typeparam>
    /// <param name="v">The array of items contained in the collection. Items in the array may be modified.</param>
    /// <param name="vOffset">The offset into the array 'v' of the first item</param>
    /// <param name="x">The external array passed into IterateRangeBatch()</param>
    /// <param name="xOffset">The offset into the array 'x' of the first item</param>
    /// <param name="count">The number of items in array 'v' and 'x' to be processed during this invocation</param>
    public delegate void IterateOperatorBatchLong<T>(T[] v, long vOffset, T[] x, long xOffset, long count);

    /// <summary>
    /// Interface for Huge List collections. An implementation of this interface behaves as close as possible to IList&lt;T&gt;
    /// or List&lt;T&gt;
    /// </summary>
    /// <typeparam name="T">Type of item contained in the collection</typeparam>
    [DocumentationSource]
    public interface IHugeListLong<T> : IListLong<T>, ICollectionLong<T>, IEnumerable<T>, IChunkedEnumerableLong<EntryRangeMapLong<T[]>>
    {
        /// <summary>
        /// The maximum size of a single segment (or chunk) stored in the collection. There is a tradeoff in this value: smaller
        /// values reduce the array copying cost of insertion and deletion but increase the overhead of segment management.
        /// Larger values increase the array copying cost of insertion and deletion but reduce overhead of segment management.
        /// The default value is 512.
        /// </summary>
        int MaxBlockSize { get; }

        //long Count { get; }

        //bool IsReadOnly { get; }

        //T this[long index] { get; set; }

        /// <summary>
        /// Insert a range of default values into the collection. This is equivalent to
        /// <code>InsertRange(index, new T[count], 0, count)</code>
        /// but avoids the allocation.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="count">number of default-valued items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        void InsertRangeDefault(long index, long count);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="items">the array of items to insert into the colletion</param>
        /// <param name="offset">the index of the first item in 'items' to insert</param>
        /// <param name="count">number of default-valued items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        /// <exception cref="ArgumentException">index, count, or offset exceeds the length of the collection or array</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void InsertRange(long index, T[] items, long offset, long count);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="items">the array of items to insert into the colletion</param>
        /// <param name="offset">the index of the first item in 'items' to insert</param>
        /// <param name="count">number of default-valued items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        /// <exception cref="ArgumentException">index, count, or offset exceeds the length of the collection or array</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void InsertRange(long index, IHugeListLong<T> items, long offset, long count);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="items">the array of items to insert into the colletion</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the arguments are negative</exception>
        /// <exception cref="ArgumentException">index exceeds the length of the collection or</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void InsertRange(long index, T[] items);

        /// <summary>
        /// Insert a range of values into the collection.
        /// </summary>
        /// <param name="index">index of the item in the collection to insert before</param>
        /// <param name="collection">an enumerable collection of items to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">index is less than 0</exception>
        /// <exception cref="ArgumentException">index exceeds the length of the collection</exception>
        /// <exception cref="ArgumentNullException">'collection' is null</exception>
        void InsertRange(long index, IEnumerable<T> collection);

        //void Insert(long index, T item);

        //void Add(T item);

        /// <summary>
        /// Append a range of values to the end of the collection
        /// </summary>
        /// <param name="items">the array of values to append</param>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void AddRange(T[] items);

        /// <summary>
        /// Append a range of values to the end of the collection
        /// </summary>
        /// <param name="collection">an enumerable collection of items to insert</param>
        /// <exception cref="ArgumentNullException">'collection' is null</exception>
        void AddRange(IEnumerable<T> collection);

        /// <summary>
        /// Append a range of values to the end of the collection
        /// </summary>
        /// <param name="collection">an enumerable collection of items to insert</param>
        /// <exception cref="ArgumentNullException">'collection' is null</exception>
        void AddRange(IHugeListLong<T> collection);

        /// <summary>
        /// Remove a range of values from the collection
        /// </summary>
        /// <param name="index">the index of the first item to remove from the collection</param>
        /// <param name="count">the number of items to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">index or count is less than 0</exception>
        /// <exception cref="ArgumentException">index or count would exceed the end of the collection</exception>
        void RemoveRange(long index, long count);

        //void RemoveAt(long index);

        //bool Remove(T item);

        /// <summary>
        /// Remove all items from the collection for which the 'match' function returns true.
        /// </summary>
        /// <param name="match">a callback function that tests each item</param>
        /// <returns>the number of items removed</returns>
        /// <exception cref="ArgumentNullException">match is null</exception>
        long RemoveAll(Predicate<T> match);

        /// <summary>
        /// Replace a range of items in the collecction with another range of items. It is equivalent to 
        /// <code>RemoveRange(index, count)</code> followed by <code>InsertRange(index, items, offset, count2)</code>
        /// </summary>
        /// <param name="index">the index of the first item in the collection to remove</param>
        /// <param name="count">the number of items in the collection to remove</param>
        /// <param name="items">the array of items to insert into the collection</param>
        /// <param name="offset">the index in 'items' of the first item to insert</param>
        /// <param name="count2">the number of items to insert from 'items'</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the index parameters are less than 0</exception>
        /// <exception cref="ArgumentException">index and count would exceed the end of the collection or
        /// offset and count2 would exceed the end of 'items'</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void ReplaceRange(long index, long count, T[] items, long offset, long count2);

        /// <summary>
        /// Replace a range of items in the collecction with another range of items. It is equivalent to 
        /// <code>RemoveRange(index, count)</code> followed by <code>InsertRange(index, items, 0, items.Length)</code>
        /// </summary>
        /// <param name="index">the index of the first item in the collection to remove</param>
        /// <param name="count">the number of items in the collection to remove</param>
        /// <param name="items">the array of items to insert into the collection</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the index parameters are less than 0</exception>
        /// <exception cref="ArgumentException">index and count would exceed the end of the collection</exception>
        /// <exception cref="ArgumentNullException">'items' is null</exception>
        void ReplaceRange(long index, long count, T[] items);

        //void Clear();

        /// <summary>
        /// Copy a range of items from the collection to an array.
        /// </summary>
        /// <param name="index">the index of the first item in the collection that will by copied</param>
        /// <param name="array">the array to copy items into</param>
        /// <param name="arrayIndex">the index of the first position in 'array' to copy into</param>
        /// <param name="count">the number of items to copy</param>
        /// <exception cref="ArgumentOutOfRangeException">any of the index parameters are less than 0</exception>
        /// <exception cref="ArgumentException">index and count would exceed the end of the collection or
        /// arrayIndex and count would exceed the end of 'array'</exception>
        /// <exception cref="ArgumentNullException">'array' is null</exception>
        void CopyTo(long index, T[] array, long arrayIndex, long count);

        //void CopyTo(T[] items, long arrayIndex);

        /// <summary>
        /// Copy the entire collection into an array.
        /// </summary>
        /// <param name="array">the array to copy into</param>
        /// <exception cref="ArgumentException">the array is not large enough to contain all items from the collecion</exception>
        /// <exception cref="ArgumentNullException">'array' is null</exception>
        void CopyTo(T[] array);

        /// <summary>
        /// Iterate over a range of items in the collection, optionally updating the value of each item.
        /// </summary>
        /// <param name="index">the position of the first item to iterate</param>
        /// <param name="external">an external array that is passed through to the callback</param>
        /// <param name="externalOffset">an offset into 'external' that is passed through to the callback</param>
        /// <param name="count">the number of items to iterate over</param>
        /// <param name="op">the callback function</param>
        /// <exception cref="ArgumentOutOfRangeException">index or count are less than 0</exception>
        /// <exception cref="ArgumentException">index or count would exceed the end of the collection</exception>
        void IterateRange(long index, T[] external, long externalOffset, long count, IterateOperator<T> op);

        /// <summary>
        /// Iterate over a range of items in the collection in batches, optionally updating the value of each item.
        /// </summary>
        /// <param name="index">the position of the first item to iterate</param>
        /// <param name="external">an external array that is passed through to the callback</param>
        /// <param name="externalOffset">an offset into 'external' that is passed through to the callback</param>
        /// <param name="count">the number of items to iterate over</param>
        /// <param name="op">the callback function</param>
        /// <exception cref="ArgumentOutOfRangeException">index or count are less than 0</exception>
        /// <exception cref="ArgumentException">index or count would exceed the end of the collection</exception>
        void IterateRangeBatch(long index, T[] external, long externalOffset, long count, IterateOperatorBatchLong<T> op);

        /// <summary>
        /// Searches a range of items in the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item. If multi is true, the index of the first item in a run of
        /// duplicates is returned.
        /// </summary>
        /// <param name="start">the index of the first item in the range of items to search</param>
        /// <param name="count">the number of items in the range to search</param>
        /// <param name="item">the value of an item to search for</param>
        /// <param name="comparer">the comparer to use for searching. If null, Comparer&lt;T&gt;.Default</param>
        /// <param name="multi">specifies that the index of the first item in a run of duplicates should be returned</param>
        /// <returns>The position of the item in the sorted collection if item is found, otherwise, a negative number that
        /// is the bitwise complement of the index of the next item that is larger than item or, if there is no
        /// larger item, the bitwise complement of Count</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="InvalidOperationException">comparer is null and there is no Comparer&lt;T&gt;.Default
        /// available for type T</exception>
        long BinarySearch(long start, long count, T item, IComparer<T> comparer, bool multi);

        /// <summary>
        /// Searches a range of items in the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item.
        /// </summary>
        /// <param name="start">the index of the first item in the range of items to search</param>
        /// <param name="count">the number of items in the range to search</param>
        /// <param name="item">the value of an item to search for</param>
        /// <param name="comparer">the comparer to use for searching. If null, Comparer&lt;T&gt;.Default</param>
        /// <returns>The position of the item in the sorted collection if item is found, otherwise, a negative number that
        /// is the bitwise complement of the index of the next item that is larger than item or, if there is no
        /// larger item, the bitwise complement of Count</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="InvalidOperationException">comparer is null and there is no Comparer&lt;T&gt;.Default
        /// available for type T</exception>
        long BinarySearch(long start, long count, T item, IComparer<T> comparer);

        /// <summary>
        /// Searches the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item.
        /// </summary>
        /// <param name="item">the value of an item to search for</param>
        /// <param name="comparer">the comparer to use for searching. If null, Comparer&lt;T&gt;.Default</param>
        /// <returns>The position of the item in the sorted collection if item is found, otherwise, a negative number that
        /// is the bitwise complement of the index of the next item that is larger than item or, if there is no
        /// larger item, the bitwise complement of Count</returns>
        /// <exception cref="InvalidOperationException">comparer is null and there is no Comparer&lt;T&gt;.Default
        /// available for type T</exception>
        long BinarySearch(T item, IComparer<T> comparer);

        /// <summary>
        /// Searches the sorted collection for an item using the specified comparer and
        /// returning the zero-based index of the item.
        /// </summary>
        /// <param name="item">the value of an item to search for</param>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long BinarySearch(T item);

        /// <summary>
        /// Searches for the first occurrence in the range of 'count' items starting at 'start' of any of the items in
        /// the array 'values'
        /// </summary>
        /// <param name="values">the array of items to search for</param>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="count">the number of items to search</param>
        /// <returns>the index of the first item of any of the items from 'values' found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="ArgumentNullException">values is null</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long IndexOfAny(T[] values, long start, long count);

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence in the range of 'count' items
        /// starting at 'start'.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="count">the number of items to search</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long IndexOf(T value, long start, long count);

        /// <summary>
        /// Searches for the specified item and returns the index of the first occurrence in the range of items
        /// starting at 'start' through the end of the collection.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start is less than 0</exception>
        /// <exception cref="ArgumentException">start is greater than the size of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long IndexOf(T value, long start);

        //long IndexOf(T value);

        /// <summary>
        /// REturns the index of the first item for which 'match' returns true in the range of 'count' items
        /// starting at 'start'.
        /// </summary>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="count">the number of items to search</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count would exceed the end of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        long FindIndex(long start, long count, Predicate<T> match);

        /// <summary>
        /// REturns the index of the first item for which 'match' returns true in the range of items
        /// starting at 'start' through the end of the collection
        /// </summary>
        /// <param name="start">the index of the first item to begin the search at</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start is less than 0</exception>
        /// <exception cref="ArgumentException">start greater than the size of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        long FindIndex(long start, Predicate<T> match);

        /// <summary>
        /// REturns the index of the first item for which 'match' returns true in the entire collection
        /// </summary>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the first item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentNullException">match is null</exception>
        long FindIndex(Predicate<T> match);

        /// <summary>
        /// Returns the index of the last occurrence within the range of items in the collection of any of the
        /// items in 'values'
        /// </summary>
        /// <param name="values">the array of items to search for</param>
        /// <param name="start">The starting index of the backward search</param>
        /// <param name="count">the number of items to search backward through</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count exceeds the bounds of the collection</exception>
        /// <exception cref="ArgumentNullException">values is null</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long LastIndexOfAny(T[] values, long start, long count);

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the
        /// range of items in the collection that contains the specified number of items and ends at the specified index.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="index">The starting index of the backward search</param>
        /// <param name="count">the number of items to search backward through</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count exceeds the bounds of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long LastIndexOf(T value, long index, long count);

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the
        /// range of items in the collection from the beginning through the specified index.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <param name="index">The starting index of the backward search</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start exceeds the bounds of the collection</exception>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long LastIndexOf(T value, long index);

        /// <summary>
        /// Searches for the specified item and returns the index of the last occurrence within the entire collection.
        /// </summary>
        /// <param name="value">the item to search for</param>
        /// <returns>the index of the last item found in the collection, or -1 if no item was found</returns>
        /// <exception cref="InvalidOperationException">there is no Comparer&lt;T&gt;.Default available for type T</exception>
        long LastIndexOf(T value);

        /// <summary>
        /// Searches for the item for which 'match' returns true and returns the index of the last occurrence within the
        /// range of items in the collection that contains the specified number of items and ends at the specified index.
        /// </summary>
        /// <param name="start">The starting index of the backward search</param>
        /// <param name="count">the number of items to search backward through</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start or count are less than 0</exception>
        /// <exception cref="ArgumentException">start or count exceeds the bounds of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        long FindLastIndex(long start, long count, Predicate<T> match);

        /// <summary>
        /// Searches for the item for which 'match' returns true and returns the index of the last occurrence within the
        /// range of items in the collection from the beginning through the specified index.
        /// </summary>
        /// <param name="start">The starting index of the backward search</param>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentOutOfRangeException">start is less than 0</exception>
        /// <exception cref="ArgumentException">start exceeds the bounds of the collection</exception>
        /// <exception cref="ArgumentNullException">match is null</exception>
        long FindLastIndex(long start, Predicate<T> match);

        /// <summary>
        /// Searches for the item for which 'match' returns true and returns the index of the last occurrence within the
        /// entire collection.
        /// </summary>
        /// <param name="match">the callback function used to examine each item, which should return true when
        /// an item is found</param>
        /// <returns>the index of the last item found in the range, or -1 if no item was found</returns>
        /// <exception cref="ArgumentNullException">match is null</exception>
        long FindLastIndex(Predicate<T> match);

        //bool Contains(T value);

        /// <summary>
        /// Return an array containing all items in the collection in collection order
        /// </summary>
        /// <returns>the array containing all the items in collection order</returns>
        T[] ToArray();
    }


    //
    // Enumeration
    //

    /// <summary>
    /// Interface supporting chunked enumeration with options beyond that supported by the built-in IEnumerable&lt;&gt;
    /// </summary>
    /// <typeparam name="RangeT">Chunked enumerable type: EntryRangeMap&lt;T[]&gt;</typeparam>
    [DocumentationSource]
    public interface IChunkedEnumerable<RangeT>
    {
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <returns>Enumerable object that visits array chunks (segments) from beginning in forward order</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        IEnumerable<RangeT> GetEnumerableChunked();
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <param name="start">Enumeration will begin with the chunk (segment) containing the list item identified
        /// by the start index</param>
        /// <returns>Enumerable object that visits array chunks (segments) starting from the segment containing the start
        /// index in forward order</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">start is less than zero</exception>
        /// <exception cref="ArgumentException">start is greater than the number of items in the list</exception>
        IEnumerable<RangeT> GetEnumerableChunked(int start);
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <param name="forward">True to move toward end of collection; false to move toward beginning</param>
        /// <returns>Enumerable object that visits array chunks (segments) in the specified direction</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        IEnumerable<RangeT> GetEnumerableChunked(bool forward);
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <param name="start">Enumeration will begin with the chunk (segment) containing the list item identified
        /// by the start index</param>
        /// <param name="forward">True to move toward end of collection; false to move toward beginning</param>
        /// <returns>Enumerable object that visits array chunks (segments) in the specified direction, starting with
        /// the chunk (segment) containing the item identified by the start index</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">start is less than zero</exception>
        /// <exception cref="ArgumentException">start is greater than the number of items in the list</exception>
        IEnumerable<RangeT> GetEnumerableChunked(int start, bool forward);
    }

    /// <summary>
    /// Interface supporting chunked enumeration with options beyond that supported by the built-in IEnumerable&lt;&gt;
    /// </summary>
    /// <typeparam name="RangeT">Chunked enumerable type: EntryRangeMap&lt;T[]&gt;</typeparam>
    [DocumentationSource]
    public interface IChunkedEnumerableLong<RangeT>
    {
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <returns>Enumerable object that visits array chunks (segments) from beginning in forward order</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        IEnumerable<RangeT> GetEnumerableChunked();
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <param name="start">Enumeration will begin with the chunk (segment) containing the list item identified
        /// by the start index</param>
        /// <returns>Enumerable object that visits array chunks (segments) starting from the segment containing the start
        /// index in forward order</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">start is less than zero</exception>
        /// <exception cref="ArgumentException">start is greater than the number of items in the list</exception>
        IEnumerable<RangeT> GetEnumerableChunked(long start);
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <param name="forward">True to move toward end of collection; false to move toward beginning</param>
        /// <returns>Enumerable object that visits array chunks (segments) in the specified direction</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        IEnumerable<RangeT> GetEnumerableChunked(bool forward);
        /// <summary>
        /// Get default chunked enumerator which visits internal array chunks.
        /// </summary>
        /// <param name="start">Enumeration will begin with the chunk (segment) containing the list item identified
        /// by the start index</param>
        /// <param name="forward">True to move toward end of collection; false to move toward beginning</param>
        /// <returns>Enumerable object that visits array chunks (segments) in the specified direction, starting with
        /// the chunk (segment) containing the item identified by the start index</returns>
        /// <remarks>Be sure to use the Length field of the enumeration entry object rather than the length of the
        /// array contained in the Value field fo the entry object, since the array may contain unused padding.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">start is less than zero</exception>
        /// <exception cref="ArgumentException">start is greater than the number of items in the list</exception>
        IEnumerable<RangeT> GetEnumerableChunked(long start, bool forward);
    }


    //
    // Long list replacements for BCL list interfaces
    //

    /// <summary>
    /// Represents a collection of objects that can be individually accessed by index.
    /// </summary>
    /// <typeparam name="T">Type of item in the collection</typeparam>
    [DocumentationSource]
    public interface IListLong<T> : ICollectionLong<T>, IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Get or set the item at the specified index
        /// </summary>
        /// <param name="index">The index of the item to get or set</param>
        /// <returns>The item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is less than zero or beyond the last item in the list</exception>
        T this[long index] { get; set; }

        /// <summary>
        /// Find the index of the first occurrence of item in the list
        /// </summary>
        /// <param name="item">The item to find</param>
        /// <returns>The index of the first occurrence of item in the list, or -1 if no item was found</returns>
        long IndexOf(T item);

        /// <summary>
        /// Insert the specified item into the list at the specified index
        /// </summary>
        /// <param name="index">The index to insert before</param>
        /// <param name="item">The item value to insert</param>
        /// <exception cref="ArgumentOutOfRangeException">The index is less than zero or greater than the number of items</exception>
        void Insert(long index, T item);

        /// <summary>
        /// Remove the item at the specified index
        /// </summary>
        /// <param name="index">The index of the item to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">The index is less than zero or beyond the last item in the list</exception>
        void RemoveAt(long index);
    }

    /// <summary>
    /// Defines methods to manipulate generic collections.
    /// </summary>
    /// <typeparam name="T">Type of item in the collection</typeparam>
    [DocumentationSource]
    public interface ICollectionLong<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Returns the number of items in the collection
        /// </summary>
        long Count { get; }

        /// <summary>
        /// Returns true if the collection is read-only or false if the collection can be modified
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Add the specified item at the end of the collection
        /// </summary>
        /// <param name="item">The item to add</param>
        void Add(T item);

        /// <summary>
        /// Remove all items from the collection
        /// </summary>
        void Clear();

        /// <summary>
        /// Determine if an item is in the collection
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>Returns true if the item is in the collection or false if it is not</returns>
        bool Contains(T item);

        /// <summary>
        /// Copy the collection to an array
        /// </summary>
        /// <param name="array">The array to copy items into</param>
        /// <param name="arrayIndex">An offset into the array at which the first item will be copied</param>
        /// <exception cref="ArgumentNullException">array is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex is less than zero</exception>
        /// <exception cref="ArgumentException">The number of items in the collection is too large to fit into the
        /// array starting at arrayIndex</exception>
        void CopyTo(T[] array, long arrayIndex);

        /// <summary>
        /// Try to remove the first occurrence of an item from the collection
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>Returns true if the item was removed; false if the item was not found</returns>
        bool Remove(T item);
    }

    /// <summary>
    /// Represents a read-only collection of items that can be accessed by index.
    /// </summary>
    /// <typeparam name="T">The type of item in the list</typeparam>
    [DocumentationSource]
    public interface IReadOnlyListLong<out T> : IReadOnlyCollectionLong<T>, IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Get the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to get</param>
        /// <returns>The value of the item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is less than zero or beyond the last item in the list</exception>
        T this[long index] { get; }
    }

    /// <summary>
    /// Represents a read-only collection of items.
    /// </summary>
    /// <typeparam name="T">The type of item in the list</typeparam>
    [DocumentationSource]
    public interface IReadOnlyCollectionLong<out T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Get the number of items in the collection
        /// </summary>
        long Count { get; }
    }
}
