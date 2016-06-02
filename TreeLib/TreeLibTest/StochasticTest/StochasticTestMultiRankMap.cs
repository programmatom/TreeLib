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

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public class StochasticTestMultiRankMap : UnitTestMultiRankMap
    {
        public StochasticTestMultiRankMap(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private void Validate<KeyType, ValueType>(IMultiRankMap<KeyType, ValueType>[] collections) where KeyType : IComparable<KeyType> where ValueType : IComparable<ValueType>
        {
            uint count = UInt32.MaxValue;
            MultiRankMapEntry[] items = null;
            for (int i = 0; i < collections.Length; i++)
            {
                MultiRankMapEntry[] items1 = null;

                INonInvasiveMultiRankMapInspection treeInspector;
                if ((items1 == null) && (treeInspector = collections[i] as INonInvasiveMultiRankMapInspection) != null)
                {
                    items1 = treeInspector.GetRanks();
                    try
                    {
                        treeInspector.Validate();
                        //WriteLine(TreeInspection.Dump(treeInspector));
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], "validate", exception);
                    }
                }

                uint count1 = collections[i].Count;

                if (items == null)
                {
                    count = count1;
                    items = items1;
                    ValidateRanks<KeyType, ValueType>(items);
                }
                else
                {
                    if (count != count1)
                    {
                        Fault(collections[i], "count");
                    }
                    if (items.Length != items1.Length)
                    {
                        Fault(collections[i], "length");
                    }
                    ValidateRanks<KeyType, ValueType>(items1);
                    ValidateRanksEqual<KeyType, ValueType>(items, items1);
                }
            }
        }

        private readonly CompoundComparer<int, float> Comparer = new CompoundComparer<int, float>();
        private class CompoundComparer<KeyType, ValueType> : IComparer<MultiRankMapEntry> where KeyType : IComparable<KeyType>
        {
            public int Compare(MultiRankMapEntry x, MultiRankMapEntry y)
            {
                return Comparer<KeyType>.Default.Compare((KeyType)x.key, (KeyType)y.key);
            }
        }


        private int GetExtent(MultiRankMapEntry[] items)
        {
            return items.Length != 0 ? (items[items.Length - 1].rank.start + items[items.Length - 1].rank.length) : 0;
        }

        private void RankCountAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            int extent = GetExtent(items);
            for (int i = 0; i < collections.Length; i++)
            {
                description = String.Format("RankCount");
            }
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int c = collections[i].RankCount;
                    if (c != extent)
                    {
                        Fault(collections[i], description + " - returned wrong value");
                    }
                }
                catch (Exception exception)
                {
                    Fault(collections[i], description + " - threw exception", exception);
                }
            }
        }

        private void ContainsKeyAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int key = (int)items[rnd.Next(items.Length)].key;
                    description = String.Format("ContainsKey (existing) [{0}]", key);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].ContainsKey(key);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // missing
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("ContainsKey (missing) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].ContainsKey(key);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TryAddAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            int count = rnd.Next(-1, 100);
            bool overflow = false;
            if ((rnd.Next(100) == 0) && (collections[0].RankCount > 0))
            {
                count = Int32.MaxValue;
                overflow = true;
            }
            if (rnd.Next(2) == 0)
            {
                // add existing
                if (items.Length != 0)
                {
                    int key = (int)items[rnd.Next(items.Length)].key;
                    float value = (float)rnd.NextDouble();
                    description = String.Format("TryAdd (existing) [{0}, {1}, {2}]", key, value, count);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TryAdd(key, value, count);
                            if (f)
                            {
                                Fault(collections[i], description + " - returned true");
                            }
                        }
                        catch (ArgumentOutOfRangeException) when (count <= 0)
                        {
                            // expected
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // add new
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                float value = (float)rnd.NextDouble();
                description = String.Format("TryAdd (new) [{0}, {1}, {2}]", key, value, count);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryAdd(key, value, count);
                        if (!f)
                        {
                            Fault(collections[i], description + " - returned false");
                        }
                    }
                    catch (ArgumentOutOfRangeException) when (count <= 0)
                    {
                        // expected
                    }
                    catch (OverflowException) when (overflow)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TryRemoveAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            if (rnd.Next(2) == 0)
            {
                // remove existing
                if (items.Length != 0)
                {
                    int key = (int)items[rnd.Next(items.Length)].key;
                    description = String.Format("TryRemove (existing) [{0}]", key);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TryRemove(key);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // remove non-existing
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("TryRemove (nonexisting) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryRemove(key);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TryGetValueAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            if (rnd.Next(2) == 0)
            {
                // get existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int key = (int)items[index].key;
                    float existingValue = (float)items[index].value;
                    description = String.Format("TryGetValue (existing) [{0},{1}]", key, existingValue);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            float actualValue;
                            bool f = collections[i].TryGetValue(key, out actualValue);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                            if (existingValue != actualValue)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // get non-existing
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("TryGetValue (nonexisting) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        float actualValue;
                        bool f = collections[i].TryGetValue(key, out actualValue);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                        if (default(float) != actualValue)
                        {
                            Fault(collections[i], description + " - value incorrect");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TrySetValueAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            float value = (float)rnd.NextDouble();
            if (rnd.Next(2) == 0)
            {
                // set existing
                if (items.Length != 0)
                {
                    int key = (int)items[rnd.Next(items.Length)].key;
                    description = String.Format("TrySetValue (existing) [{0}, {1}]", key, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TrySetValue(key, value);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // set nonexistent
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("TrySetValue (nonexistent) [{0}, {1}]", key, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TrySetValue(key, value);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TryGetAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            if (rnd.Next(2) == 0)
            {
                // get existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int key = (int)items[index].key;
                    float existingValue = (float)items[index].value;
                    int existingRank = items[index].rank.start;
                    int existingCount = items[index].rank.length;
                    description = String.Format("TryGet (existing) [{0}]", key);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            float actualValue;
                            int actualRank, actualCount;
                            bool f = collections[i].TryGet(key, out actualValue, out actualRank, out actualCount);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                            if (existingValue != actualValue)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                            if (existingRank != actualRank)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                            if (existingCount != actualCount)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // get non-existing
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("TryGet (nonexisting) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        float actualValue;
                        int actualRank, actualCount;
                        bool f = collections[i].TryGet(key, out actualValue, out actualRank, out actualCount);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                        if (default(float) != actualValue)
                        {
                            Fault(collections[i], description + " - value incorrect");
                        }
                        if (0 != actualRank)
                        {
                            Fault(collections[i], description + " - value incorrect");
                        }
                        if (0 != actualCount)
                        {
                            Fault(collections[i], description + " - value incorrect");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TryGetKeyByRankAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            int extent = GetExtent(items);
            int position = rnd.Next(-Math.Max(1, extent / 40), extent + Math.Max(1, extent / 40) + 1);
            if ((position >= 0) && (position < extent))
            {
                // get valid
                int index = -1;
                for (int i = 0; i < items.Length; i++)
                {
                    if ((position >= items[i].rank.start) && (position < items[i].rank.start + items[i].rank.length))
                    {
                        index = i;
                        break;
                    }
                }
                Debug.Assert(index >= 0);
                description = String.Format("TryGetKeyByRank (valid) [{0}]", position);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int key;
                        bool f = collections[i].TryGetKeyByRank(position, out key);
                        if (!f)
                        {
                            Fault(collections[i], description + " - returned false");
                        }
                        if (key != (int)items[index].key)
                        {
                            Fault(collections[i], description + " - returned wrong value");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
            else
            {
                // get invalid
                description = String.Format("TryGetKeyByRank (invalid) [{0}]", position);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int key;
                        bool f = collections[i].TryGetKeyByRank(position, out key);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                        if (default(int) != key)
                        {
                            Fault(collections[i], description + " - value incorrect");
                        }
                    }
                    catch (ArgumentOutOfRangeException) when (position < 0)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void AddAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            int count = rnd.Next(-1, 100);
            bool overflow = false;
            if ((rnd.Next(100) == 0) && (collections[0].RankCount > 0))
            {
                count = Int32.MaxValue;
                overflow = true;
            }
            if (rnd.Next(2) == 0)
            {
                // add existing
                if (items.Length != 0)
                {
                    int key = (int)items[rnd.Next(items.Length)].key;
                    float value = (float)rnd.NextDouble();
                    description = String.Format("Add (existing) [{0}, {1}, {2}]", key, value, count);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].Add(key, value, count);
                            Fault(collections[i], description + " - didn't throw");
                        }
                        catch (ArgumentOutOfRangeException) when (count <= 0)
                        {
                            // expected
                        }
                        catch (ArgumentException) when (!(count <= 0))
                        {
                            // expected
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // add new
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                float value = (float)rnd.NextDouble();
                description = String.Format("Add (new) [{0}, {1}, {2}]", key, value, count);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Add(key, value, count);
                    }
                    catch (ArgumentOutOfRangeException) when (count <= 0)
                    {
                        // expected
                    }
                    catch (OverflowException) when (overflow)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void RemoveAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            if (rnd.Next(2) == 0)
            {
                // remove existing
                if (items.Length != 0)
                {
                    int key = (int)items[rnd.Next(items.Length)].key;
                    description = String.Format("Remove (existing) [{0}]", key);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].Remove(key);
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // remove non-existing
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("Remove (nonexisting) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Remove(key);
                        Fault(collections[i], description + " - didn't throw");
                    }
                    catch (ArgumentException)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void GetValueAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            if (rnd.Next(2) == 0)
            {
                // get existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int key = (int)items[index].key;
                    float existingValue = (float)items[index].value;
                    description = String.Format("GetValue (existing) [{0},{1}]", key, existingValue);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            float actualValue = collections[i].GetValue(key);
                            if (existingValue != actualValue)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // get non-existing
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("GetValue (nonexisting) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        float actualValue = collections[i].GetValue(key);
                        Fault(collections[i], description + " - didn't throw");
                    }
                    catch (ArgumentException)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void SetValueAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            float value = (float)rnd.NextDouble();
            if (rnd.Next(2) == 0)
            {
                // set existing
                if (items.Length != 0)
                {
                    int key = (int)items[rnd.Next(items.Length)].key;
                    description = String.Format("SetValue (existing) [{0}, {1}]", key, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].SetValue(key, value);
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // set nonexistent
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("SetValue (nonexistent) [{0}, {1}]", key, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].SetValue(key, value);
                        Fault(collections[i], description + " - didn't throw");
                    }
                    catch (ArgumentException)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void GetAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            if (rnd.Next(2) == 0)
            {
                // get existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int key = (int)items[index].key;
                    float existingValue = (float)items[index].value;
                    int existingRank = items[index].rank.start;
                    int existingCount = items[index].rank.length;
                    description = String.Format("Get (existing) [{0}]", key);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            float actualValue;
                            int actualRank, actualCount;
                            collections[i].Get(key, out actualValue, out actualRank, out actualCount);
                            if (existingValue != actualValue)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                            if (existingRank != actualRank)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                            if (existingCount != actualCount)
                            {
                                Fault(collections[i], description + " - value incorrect");
                            }
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw exception", exception);
                        }
                    }
                }
            }
            else
            {
                // get non-existing
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
                description = String.Format("Get (nonexisting) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        float actualValue;
                        int actualRank, actualCount;
                        collections[i].Get(key, out actualValue, out actualRank, out actualCount);
                        Fault(collections[i], description + " - didn't throw");
                    }
                    catch (ArgumentException)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void GetKeyByRankAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();
            int extent = GetExtent(items);
            int position = rnd.Next(-Math.Max(1, extent / 40), extent + Math.Max(1, extent / 40) + 1);
            if ((position >= 0) && (position < extent))
            {
                // get valid
                int index = -1;
                for (int i = 0; i < items.Length; i++)
                {
                    if ((position >= items[i].rank.start) && (position < items[i].rank.start + items[i].rank.length))
                    {
                        index = i;
                        break;
                    }
                }
                Debug.Assert(index >= 0);
                description = String.Format("GetKeyByRank (valid) [{0}]", position);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int key = collections[i].GetKeyByRank(position);
                        if (key != (int)items[index].key)
                        {
                            Fault(collections[i], description + " - returned wrong value");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
            else
            {
                // get invalid
                description = String.Format("GetKeyByRank (invalid) [{0}]", position);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int key = collections[i].GetKeyByRank(position);
                        Fault(collections[i], description + " - didn't throw");
                    }
                    catch (ArgumentOutOfRangeException) when (position < 0)
                    {
                        // expected
                    }
                    catch (ArgumentException) when (!(position < 0))
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void AdjustCountAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            MultiRankMapEntry[] items = ((INonInvasiveMultiRankMapInspection)collections[0]).GetRanks();

            int countAdjust = rnd.Next(-2, 3/*exclusive*/);

            int key;
            bool keyExists = (rnd.Next(2) == 0) && (items.Length != 0);
            int index = -1;
            if (keyExists)
            {
                index = rnd.Next(items.Length);
                key = (int)items[index].key;
            }
            else
            {
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankMapEntry(key, new Range(), 0), Comparer) >= 0);
            }

            bool overflow = false;
            if ((rnd.Next(100) == 0) && (collections[0].RankCount - (keyExists ? items[index].rank.length : 0) > 0))
            {
                countAdjust = Int32.MaxValue;
                overflow = true;
            }

            bool shouldThrowOutOfRange = (keyExists && (countAdjust < -items[index].rank.length))
                || (!keyExists && (countAdjust < 0));
            bool shouldCreate = !keyExists && (countAdjust > 0);
            bool shouldDelete = keyExists && (countAdjust == -items[index].rank.length);

            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    collections[i].AdjustCount(key, countAdjust);
                    if (shouldThrowOutOfRange)
                    {
                        Fault(collections[i], description + " - didn't throw");
                    }
                    if (shouldCreate && (collections[i].Count != items.Length + 1))
                    {
                        Fault(collections[i], description + " - didn't create");
                    }
                    if (shouldDelete && (collections[i].Count != items.Length - 1))
                    {
                        Fault(collections[i], description + " - didn't delete");
                    }
                    if (!shouldCreate && !shouldDelete && (collections[i].Count != items.Length))
                    {
                        Fault(collections[i], description + " - shouldn't change count");
                    }
                    if (collections[i].RankCount != GetExtent(items) + countAdjust)
                    {
                        Fault(collections[i], description + " - RankCount discrepancy");
                    }
                }
                catch (ArgumentOutOfRangeException) when (shouldThrowOutOfRange)
                {
                    // expected
                }
                catch (OverflowException) when (overflow)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(collections[i], description + " - threw exception", exception);
                }
            }
        }


        public override bool Do(int seed, StochasticControls control)
        {
            IMultiRankMap<int, float>[] collections = new IMultiRankMap<int, float>[]
            {
                new ReferenceMultiRankMap<int, float>(), // must be first

                new SplayTreeMultiRankMap<int, float>(),
                new SplayTreeArrayMultiRankMap<int, float>(),
                new AdaptMultiRankListToMultiRankMap<int, float>(new SplayTreeMultiRankList<KeyValue<int, float>>()),
                new AdaptMultiRankListToMultiRankMap<int, float>(new SplayTreeArrayMultiRankList<KeyValue<int, float>>()),
                new AdaptMultiRankMapToMultiRankMapLong<int, float>(new SplayTreeMultiRankMapLong<int, float>()),
                new AdaptMultiRankListToMultiRankMap<int, float>(new AdaptMultiRankListToMultiRankListLong<KeyValue<int, float>>(new SplayTreeMultiRankListLong<KeyValue<int, float>>())),

                new RedBlackTreeMultiRankMap<int, float>(),
                new RedBlackTreeArrayMultiRankMap<int, float>(),
                new AdaptMultiRankListToMultiRankMap<int, float>(new RedBlackTreeMultiRankList<KeyValue<int, float>>()),
                new AdaptMultiRankListToMultiRankMap<int, float>(new RedBlackTreeArrayMultiRankList<KeyValue<int, float>>()),
                new AdaptMultiRankMapToMultiRankMapLong<int, float>(new RedBlackTreeMultiRankMapLong<int, float>()),
                new AdaptMultiRankListToMultiRankMap<int, float>(new AdaptMultiRankListToMultiRankListLong<KeyValue<int, float>>(new RedBlackTreeMultiRankListLong<KeyValue<int, float>>())),

                new AVLTreeMultiRankMap<int, float>(),
                new AVLTreeArrayMultiRankMap<int, float>(),
                new AdaptMultiRankListToMultiRankMap<int, float>(new AVLTreeMultiRankList<KeyValue<int, float>>()),
                new AdaptMultiRankListToMultiRankMap<int, float>(new AVLTreeArrayMultiRankList<KeyValue<int, float>>()),
                new AdaptMultiRankMapToMultiRankMapLong<int, float>(new AVLTreeMultiRankMapLong<int, float>()),
                new AdaptMultiRankListToMultiRankMap<int, float>(new AdaptMultiRankListToMultiRankListLong<KeyValue<int, float>>(new AVLTreeMultiRankListLong<KeyValue<int, float>>())),
            };

            Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>[] actions = new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>[]
            {
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), RankCountAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), ContainsKeyAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(400 - 35, 400 + 75), TryAddAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(460     , 460     ), TryRemoveAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), TryGetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), TrySetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), TryGetAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), TryGetKeyByRankAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(400 - 35, 400 + 75), AddAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(460     , 460     ), RemoveAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), GetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), SetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), GetAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200     , 200     ), GetKeyByRankAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(280     , 280     ), AdjustCountAction),
            };

            return StochasticDriver(
                "MultiRank Map Stochastic Test",
                seed,
                control,
                collections,
                actions,
                delegate (IMultiRankMap<int, float>[] _collections) { Validate<int, float>(_collections); });
        }
    }
}
