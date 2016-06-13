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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class StochasticTestHugeList : UnitTestHugeList
    {
        public StochasticTestHugeList(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }

        private StochasticTestHugeList()
        {
        }


        private StringBuilder script = new StringBuilder();
        private bool scriptKilled;

        private void CheckScript(int extent)
        {
            if (extent == 0)
            {
                scriptKilled = false;
                script = new StringBuilder();
            }
            else
            {
                if (script.Length > 100000)
                {
                    scriptKilled = true;
                }
                if (scriptKilled)
                {
                    script = new StringBuilder("INVALID - WAS TRUNCATED BECAUSE IT BECAME TOO BIG");
                }
            }
        }

        private string lastDump;
        private int lastDumpListIndex;

        private void Validate<T>(IHugeList<T>[] lists) where T : IComparable<T>
        {
            T[] template = lists[0].ToArray();
            for (int listIndex = 1; listIndex < lists.Length; listIndex++)
            {
                IHugeList<T> list = lists[listIndex];
                TestTrue("Count", delegate () { return list.Count == template.Length; });
                TestTrue(
                    "Equality",
                    delegate ()
                    {
                        for (int i = 0; i < template.Length; i++)
                        {
                            if (0 != Comparer<T>.Default.Compare(list[i], template[i]))
                            {
                                return false;
                            }
                        }
                        return true;
                    });

                lastDumpListIndex = listIndex;
                TestNoThrow("Validate", delegate () { ((IHugeListValidation)list).Validate(out lastDump); });
            }
        }


        private static int InsertRangeCountGenerator(Random rnd, int extent)
        {
            int c = rnd.Next() % 20 + (int)(Math.Pow(rnd.NextDouble(), 30) * 5000);
            return c;
        }

        private static int RemoveRangeCountGenerator(Random rnd, int extent)
        {
            int c = rnd.Next() % 20 + (int)(Math.Pow(rnd.NextDouble(), 50) * extent);
            return Math.Min(c, extent);
        }

#if false // TODO: remove
        private static int InsertRangeCountGenerator(Random rnd, int extent)
        {
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    return rnd.Next() % 20 + 1;
                case 1:
                    return (int)(Math.Pow(rnd.NextDouble(), 5) * 100 + 1);
                case 2:
                    return (int)(Math.Pow(rnd.NextDouble(), 5) * 5000);
            }
        }

        private static int RemoveRangeCountGenerator(Random rnd, int extent)
        {
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    return Math.Min(rnd.Next() % 20 + 1, extent);
                case 1:
                    return Math.Min((int)(Math.Pow(rnd.NextDouble(), 5) * 100 + 1), extent);
                case 2:
                    return (int)(Math.Pow(rnd.NextDouble(), 5) * extent);
            }
        }
#endif


        private int index;

        private void ContainsAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            if ((rnd.Next(2) == 0) && (reference.Count != 0))
            {
                // existing
                int item = reference[rnd.Next(reference.Count)];
                description = String.Format("Contains (existing) [{0}]", item);
                for (int i = 0; i < lists.Length; i++)
                {
                    try
                    {
                        bool f = lists[i].Contains(item);
                        if (!f)
                        {
                            Fault(lists[i], description + " - returned false");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(lists[i], description + " - threw exception", exception);
                    }
                }
            }
            else
            {
                // missing
                int item;
                do
                {
                    item = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (reference.Contains(item));
                description = String.Format("Contains (missing) [{0}]", item);
                for (int i = 0; i < lists.Length; i++)
                {
                    try
                    {
                        bool f = lists[i].Contains(item);
                        if (f)
                        {
                            Fault(lists[i], description + " - returned true");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(lists[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void InsertAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int item = rnd.Next();

            if (rnd.Next() % 3 != 0)
            {
                index = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
            }

            bool valid = (index >= 0) && (index <= extent);

            description = String.Format("Insert [{0}, {1}: {2}]", index, valid ? "valid" : "invalid", item);
            script.AppendLine(String.Format("new OpInsert<T>({0}, default(T)),", index));
            for (int i = 0; i < lists.Length; i++)
            {
                try
                {
                    lists[i].Insert(index, item);
                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid && (index < 0))
                {
                    // expected
                }
                catch (ArgumentException) when (!valid && !(index < 0))
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void RemoveAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            if (rnd.Next() % 3 != 0)
            {
                index = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
            }

            bool valid = (index >= 0) && (index < extent);

            description = String.Format("Remove [{0}, {1}]", index, valid ? "valid" : "invalid");
            script.AppendLine(String.Format("new OpRemoveAt<T>({0}),", index));
            for (int i = 0; i < lists.Length; i++)
            {
                try
                {
                    lists[i].RemoveAt(index);
                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid && (index < 0))
                {
                    // expected
                }
                catch (ArgumentException) when (!valid && !(index < 0))
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void InsertRangeAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int count = InsertRangeCountGenerator(rnd, extent);

            int[] items = new int[count];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = rnd.Next();
            }
            int offset = 0;

            if ((rnd.Next() % 3 != 0) || (index == 0) && (index == extent))
            {
                switch (rnd.Next() % 10)
                {
                    case 0: // insert at starting edge
                        index = 0;
                        break;
                    case 1: // insert at trailing edge
                        index = extent;
                        break;
                    default: // at random position in middle
                        index = rnd.Next(0, extent);
                        break;
                }
            }
            index = Math.Min(extent, Math.Max(0, index));

            bool valid = true;
            if (rnd.Next() % 10 == 0)
            {
                // make invalid inputs
                valid = false;
                switch (rnd.Next() % 7)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        index = -1;
                        break;
                    case 1:
                        index = extent + 1;
                        break;
                    case 2:
                        count = -1;
                        break;
                    case 3:
                        count++;
                        break;
                    case 4:
                        offset = -1;
                        break;
                    case 5:
                        offset++;
                        break;
                    case 6:
                        items = null;
                        break;
                }
            }
            else
            {
                // array subset
                if ((rnd.Next() % 3 == 0) && (count > 0))
                {
                    count--;
                    if (rnd.Next() % 2 == 0)
                    {
                        offset++;
                    }
                }
            }

            description = String.Format("InsertRange [{0}, {1}: {2}]", index, count, valid ? "valid" : "invalid");
            script.AppendLine(String.Format("new OpInsertRange<T>({0}, null, {1}, {2}),", index, offset, count));
            for (int i = 0; i < lists.Length; i++)
            {
                try
                {
                    lists[i].InsertRange(index, items, offset, count);
                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentNullException) when (!valid)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void RemoveRangeAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int count = RemoveRangeCountGenerator(rnd, extent);

            if ((rnd.Next() % 3 != 0) || (index == 0) && (index == extent))
            {
                switch (rnd.Next() % 10)
                {
                    case 0: // remove at starting edge
                        index = 0;
                        break;
                    case 1: // remove at trailing edge
                        index = extent - count;
                        break;
                    default: // at random position in middle
                        index = rnd.Next(0, extent - count);
                        break;
                }
            }
            index = Math.Min(extent - count, Math.Max(0, index));

            bool valid = true;
            if (rnd.Next() % 10 == 0)
            {
                // make invalid inputs
                valid = false;
                switch (rnd.Next() % 4)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        index = -1;
                        break;
                    case 1:
                        index = extent - count + 1;
                        break;
                    case 2:
                        count = -1;
                        break;
                    case 3:
                        count = extent + 1;
                        break;
                }
            }

            description = String.Format("RemoveRange [{0}, {1}: {2}]", index, count, valid ? "valid" : "invalid");
            script.AppendLine(String.Format("new OpRemoveRange<T>({0}, {1}),", index, count));
            for (int i = 0; i < lists.Length; i++)
            {
                try
                {
                    lists[i].RemoveRange(index, count);
                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentException) when (!valid)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void ReplaceRangeAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int removeCount = RemoveRangeCountGenerator(rnd, extent);
            int insertCount = InsertRangeCountGenerator(rnd, extent);

            int[] items = new int[insertCount];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = rnd.Next();
            }
            int offset = 0;

            if ((rnd.Next() % 3 != 0) || (index == 0) && (index == extent))
            {
                switch (rnd.Next() % 10)
                {
                    case 0: // insert at starting edge
                        index = 0;
                        break;
                    case 1: // insert at trailing edge
                        index = extent - removeCount;
                        break;
                    default: // at random position in middle
                        index = rnd.Next(0, extent - removeCount);
                        break;
                }
            }
            index = Math.Min(extent - removeCount, Math.Max(0, index));

            bool valid = true;
            if (rnd.Next() % 10 == 0)
            {
                // make invalid inputs
                valid = false;
                switch (rnd.Next() % 7)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        index = -1;
                        break;
                    case 1:
                        index = extent + 1;
                        break;
                    case 2:
                        insertCount = -1;
                        break;
                    case 3:
                        insertCount++;
                        break;
                    case 4:
                        offset = -1;
                        break;
                    case 5:
                        offset++;
                        break;
                    case 6:
                        items = null;
                        break;
                    case 7:
                        removeCount = -1;
                        break;
                    case 8:
                        removeCount = extent + 1;
                        break;
                }
            }
            else
            {
                // array subset
                if ((rnd.Next() % 3 == 0) && (insertCount > 0))
                {
                    insertCount--;
                    if (rnd.Next() % 2 == 0)
                    {
                        offset++;
                    }
                }
            }

            description = String.Format("ReplaceRange [{0}, {1}: {2}]", index, insertCount, valid ? "valid" : "invalid");
            script.AppendLine(String.Format("new OpReplaceRange<T>({0}, {1}, null, {2}, {3}),", index, removeCount, offset, insertCount));
            for (int i = 0; i < lists.Length; i++)
            {
                try
                {
                    lists[i].ReplaceRange(index, removeCount, items, offset, insertCount);
                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentNullException) when (!valid)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void RemoveAllAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int modulus, item;
            switch ((int)(4 * Math.Pow(rnd.NextDouble(), 10))) // reaches '3' in just under 3% of cases
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0: // remove exact item
                    if (extent == 0)
                    {
                        return;
                    }
                    modulus = 0;
                    item = reference[rnd.Next() % extent];
                    break;
                case 1: // remove non-match
                    modulus = 0;
                    do
                    {
                        item = rnd.Next();
                    } while ((extent != 0) && reference.Contains(item));
                    break;
                case 2: // remove sparse pattern
                    modulus = rnd.Next() % 100 + 100;
                    item = 0;
                    break;
                case 3: // remove dense pattern
                    modulus = rnd.Next() % 20 + 1;
                    item = 0;
                    break;
            }

            string delegateText = String.Format("delegate (int candidate) {{ return ({1} == 0) ? (candidate == {0}) : (candidate % {1} == {0}); }}", item, modulus);
            description = String.Format("RemoveAll [{0}]", delegateText);
            script.AppendLine(String.Format("new RemoveAll<T>({0}),", delegateText));
            for (int i = 0; i < lists.Length; i++)
            {
                try
                {
                    lists[i].RemoveAll(delegate (int candidate) { return (modulus == 0) ? (candidate == item) : (candidate % modulus == item); });
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void CopyToAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int count;
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    count = Math.Min(rnd.Next() % 20 + 1, extent);
                    break;
                case 1:
                    count = Math.Min((int)(Math.Pow(rnd.NextDouble(), 5) * 100 + 1), extent);
                    break;
                case 2:
                    count = (int)(Math.Pow(rnd.NextDouble(), 5) * extent);
                    break;
            }

            int arrayIndex = rnd.Next() % 4 != 0 ? 0 : 1;
            int arrayLength = count + arrayIndex;

            int index;
            switch (rnd.Next() % 10)
            {
                case 0: // insert at starting edge
                    index = 0;
                    break;
                case 1: // insert at trailing edge
                    index = extent - count;
                    break;
                default: // at random position in middle
                    index = rnd.Next(0, extent - count);
                    break;
            }

            bool valid = true;
            if (rnd.Next() % 10 == 0)
            {
                // make invalid inputs
                valid = false;
                switch (rnd.Next() % 6)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        index = -1;
                        break;
                    case 1:
                        index = extent + 1;
                        break;
                    case 2:
                        count = -1;
                        break;
                    case 3:
                        count = extent + 1;
                        break;
                    case 4:
                        arrayIndex = -1;
                        break;
                    case 5:
                        arrayIndex += 1;
                        break;
                }
            }

            description = String.Format("CopyTo [{0}, {1}, {2}: {3}]", index, count, arrayIndex, valid ? "valid" : "invalid");
            int[] refArray = null;
            for (int i = 0; i < lists.Length; i++)
            {
                int[] instanceArray = new int[arrayLength];
                try
                {
                    lists[i].CopyTo(index, instanceArray, arrayIndex, count);
                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }

                    if (i == 0)
                    {
                        refArray = instanceArray;
                    }
                    else
                    {
                        for (int j = 0; j < refArray.Length; j++)
                        {
                            if (refArray[j] != instanceArray[j])
                            {
                                Fault(lists[i], description + " - resulting array data differs");
                            }
                        }
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentNullException) when (!valid)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void IndexSearchAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int method = rnd.Next() % 3;
            const int IndexOfAnyMethod = 2;

            int count;
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    count = Math.Min(rnd.Next() % 20 + 1, extent);
                    break;
                case 1:
                    count = Math.Min((int)(Math.Pow(rnd.NextDouble(), 5) * 100 + 1), extent);
                    break;
                case 2:
                    count = (int)(Math.Pow(rnd.NextDouble(), 5) * extent);
                    break;
            }

            int index;
            switch (rnd.Next() % 10)
            {
                case 0: // insert at starting edge
                    index = 0;
                    break;
                case 1: // insert at trailing edge
                    index = extent - count;
                    break;
                default: // at random position in middle
                    index = rnd.Next(0, extent - count);
                    break;
            }

            bool existing = (rnd.Next(2) == 0) && (count != 0);
            int item = 0;
            List<int> itemsForAny = new List<int>();
            int countForAny = rnd.Next() % 4;
            for (int i = 0; i < (method != IndexOfAnyMethod ? 1 : countForAny); i++)
            {
                if (existing)
                {
                    // existing
                    itemsForAny.Add(reference[index + rnd.Next(count)]);
                }
                else
                {
                    // missing
                    do
                    {
                        item = rnd.Next(Int32.MinValue, Int32.MaxValue);
                    }
                    while (reference.IndexOf(item, index, count) >= 0);
                    itemsForAny.Add(item);
                }
            }
            if (method != IndexOfAnyMethod)
            {
                Debug.Assert(itemsForAny.Count == 1);
                item = itemsForAny[0];
                itemsForAny = null;
            }

            bool valid = true;
            if (rnd.Next() % 10 == 0)
            {
                // make invalid inputs
                valid = false;
                switch (rnd.Next() % 4)
                {
                    default:
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    case 0:
                        index = -1;
                        break;
                    case 1:
                        index = extent + 1;
                        break;
                    case 2:
                        count = -1;
                        break;
                    case 3:
                        count = extent + 1;
                        break;
                }
            }

            description = String.Format(
                "{0} ({1}) [{2}, {3}, {4}: {5}]",
                method == 0 ? "IndexOf" : (method == 1 ? "FindIndex" : "IndexOfAny"),
                existing ? "existing" : "missing",
                itemsForAny != null ? itemsForAny.ToString() : item.ToString(),
                index,
                count,
                valid ? "valid" : "invalid");
            int modelFoundIndex = 0;
            for (int i = 0; i < lists.Length; i++)
            {
                int instanceFoundIndex = 0;
                try
                {
                    switch (method)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case 0:
                            Debug.Assert(itemsForAny == null);
                            instanceFoundIndex = lists[i].IndexOf(item, index, count);
                            break;
                        case 1:
                            Debug.Assert(itemsForAny == null);
                            instanceFoundIndex = lists[i].FindIndex(index, count, delegate (int candidate) { return candidate == item; });
                            break;
                        case 2:
                            Debug.Assert(itemsForAny != null);
                            instanceFoundIndex = lists[i].IndexOfAny(itemsForAny.ToArray(), index, count);
                            break;
                    }

                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }

                    if (i == 0)
                    {
                        modelFoundIndex = instanceFoundIndex;
                    }
                    else
                    {
                        if (modelFoundIndex != instanceFoundIndex)
                        {
                            Fault(lists[i], description + " - found index differs");
                        }
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentNullException) when (!valid)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }

        private void LastIndexSearchAction(IHugeList<int>[] lists, Random rnd, ref string description)
        {
            IHugeList<int> reference = lists[0];
            int extent = reference.Count;

            int method = rnd.Next() % 3;
            const int IndexOfAnyMethod = 2;

            int count;
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    count = Math.Min(rnd.Next() % 20 + 1, extent);
                    break;
                case 1:
                    count = Math.Min((int)(Math.Pow(rnd.NextDouble(), 5) * 100 + 1), extent);
                    break;
                case 2:
                    count = (int)(Math.Pow(rnd.NextDouble(), 5) * extent);
                    break;
            }

            int index;
            switch (rnd.Next() % 10)
            {
                case 0: // insert at ending edge
                    index = extent - 1;
                    break;
                case 1: // insert at leading edge
                    index = count - 1;
                    if ((index == -1) && (extent != 0))
                    {
                        index = 0; // List<>.LastIndexOf doesn't permit the vacuous case unless empty
                    }
                    break;
                default: // at random position in middle
                    index = extent - 1 - rnd.Next(0, extent - count);
                    break;
            }

            bool existing = (rnd.Next(2) == 0) && (count != 0) && (extent != 0);
            int item = 0;
            List<int> itemsForAny = new List<int>();
            int countForAny = rnd.Next() % 4;
            for (int i = 0; i < (method != IndexOfAnyMethod ? 1 : countForAny); i++)
            {
                if (existing)
                {
                    // existing
                    int j = rnd.Next(count);
                    itemsForAny.Add(reference[index - j]);
                }
                else
                {
                    // missing
                    do
                    {
                        item = rnd.Next(Int32.MinValue, Int32.MaxValue);
                    }
                    while (reference.LastIndexOf(item, index, count) >= 0);
                    itemsForAny.Add(item);
                }
            }
            if (method != IndexOfAnyMethod)
            {
                Debug.Assert(itemsForAny.Count == 1);
                item = itemsForAny[0];
                itemsForAny = null;
            }

            bool valid = true;
            // TODO: disabled because LastIndexOf and FindLastIndex have different invalid parameter boundaries
            //if (rnd.Next() % 10 == 0)
            //{
            //    // make invalid inputs
            //    valid = false;
            //    switch (rnd.Next() % 4)
            //    {
            //        default:
            //            Debug.Assert(false);
            //            throw new InvalidOperationException();
            //        case 0:
            //            index = -2;
            //            break;
            //        case 1:
            //            index = extent + 1;
            //            break;
            //        case 2:
            //            count = -1;
            //            break;
            //        case 3:
            //            count = extent + 2;
            //            if (extent == 0)
            //            {
            //                valid = true; // special case: count not validated when extent is 0
            //            }
            //            break;
            //    }
            //}

            description = String.Format(
                "{0} ({1}) [{2}, {3}, {4}: {5}]",
                method == 0 ? "LastIndexOf" : (method == 1 ? "FindLastIndex" : "LastIndexOfAny"),
                existing ? "existing" : "missing",
                itemsForAny != null ? itemsForAny.ToString() : item.ToString(),
                index,
                count,
                valid ? "valid" : "invalid");
            int modelFoundIndex = 0;
            for (int i = 0; i < lists.Length; i++)
            {
                int instanceFoundIndex = 0;
                try
                {
                    switch (method)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case 0:
                            Debug.Assert(itemsForAny == null);
                            instanceFoundIndex = lists[i].LastIndexOf(item, index, count);
                            break;
                        case 1:
                            Debug.Assert(itemsForAny == null);
                            instanceFoundIndex = lists[i].FindLastIndex(index, count, delegate (int candidate) { return candidate == item; });
                            break;
                        case 2:
                            Debug.Assert(itemsForAny != null);
                            instanceFoundIndex = lists[i].LastIndexOfAny(itemsForAny.ToArray(), index, count);
                            break;
                    }

                    if (!valid)
                    {
                        Fault(lists[i], description + " - should have thrown exception");
                    }

                    if (i == 0)
                    {
                        modelFoundIndex = instanceFoundIndex;
                    }
                    else
                    {
                        if (modelFoundIndex != instanceFoundIndex)
                        {
                            Fault(lists[i], description + " - found index differs");
                        }
                    }
                }
                catch (ArgumentOutOfRangeException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentNullException) when (!valid)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(lists[i], description + " - threw exception", exception);
                }
            }
        }


        public override bool Do(int seed, StochasticControls control)
        {
            script.AppendLine(String.Format("Seed: {0}", seed));

            IHugeList<int>[] lists = new IHugeList<int>[]
            {
                new ReferenceHugeList<int>(), // must be first

                new HugeList<int>(typeof(SplayTreeRangeMap<>), 47),
                new HugeList<int>(typeof(AVLTreeRangeMap<>), 512),

                new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(RedBlackTreeRangeMapLong<>), 47)),
                new AdaptHugeListToHugeListLong<int>(new HugeListLong<int>(typeof(SplayTreeRangeMapLong<>), 512)),
            };

            Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>[] actions = new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>[]
            {
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 400,  400), ContainsAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 400,  400), InsertAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 400,  400), RemoveAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>(1000, 2000), InsertRangeAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 400,  400), RemoveRangeAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 400,  400), ReplaceRangeAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 400,  200), RemoveAllAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 200,  200), CopyToAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 200,  200), IndexSearchAction),
                new Tuple<Tuple<int, int>, InvokeAction<IHugeList<int>>>(new Tuple<int, int>( 200,  200), LastIndexSearchAction),
            };

            return StochasticDriver(
                "HugeList Stochastic Test",
                seed,
                control,
                lists,
                actions,
                delegate (IHugeList<int> _list) { return (uint)_list.Count; },
                delegate (IHugeList<int>[] _lists) { Validate(_lists); CheckScript(lists[0].Count); });
        }
    }
}
