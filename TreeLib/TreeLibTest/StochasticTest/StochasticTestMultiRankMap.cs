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

        private delegate bool EquivalenceActionVariant0<KeyType, ValueType>(IMultiRankMap<KeyType, ValueType> collection, out KeyType keyOut, out ValueType valueOut) where KeyType : IComparable<KeyType>;
        private void EquivalenceActionVariant0Util(IMultiRankMap<int, float>[] collections, Random rnd, ref string description, EquivalenceActionVariant0<int, float> variantMethod)
        {
            int key = 0;
            float value = 0;
            bool f = false;
            for (int i = 0; i < collections.Length; i++)
            {
                int key1;
                float value1;
                bool f1 = variantMethod(collections[i], out key1, out value1);
                if (i == 0)
                {
                    key = key1;
                    value = value1;
                    f = f1;
                }
                else
                {
                    if (key != key1)
                    {
                        Fault(collections[i], description + "key");
                    }
                    if (value != value1)
                    {
                        Fault(collections[i], description + "value");
                    }
                    if (f != f1)
                    {
                        Fault(collections[i], description + "result");
                    }
                }
            }
        }

        private void LeastAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "Least";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { instanceValue = default(float); return collection.Least(out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { return collection.Least(out instanceKey, out instanceValue); });
                    break;
            }
        }

        private void GreatestAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "Greatest";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { instanceValue = default(float); return collection.Greatest(out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { return collection.Greatest(out instanceKey, out instanceValue); });
                    break;
            }
        }

        private delegate bool EquivalenceActionVariant1<KeyType, ValueType>(IMultiRankMap<KeyType, ValueType> collection, KeyType key, out KeyType keyOut, out ValueType valueOut, out int rank, out int rankCount) where KeyType : IComparable<KeyType>;
        private void EquivalenceActionVariant1Util(IMultiRankMap<int, float>[] collections, Random rnd, ref string description, EquivalenceActionVariant1<int, float> variantMethod)
        {
            string descriptionPrefix = description;

            int queryKey;
            float queryValue;

            MultiRankInfo<int, float>[] items = FlattenAnyRankTree<int, float>(collections[0], true/*multi*/);
            if ((rnd.Next(2) == 0) && (items.Length != 0))
            {
                // with existing
                int index = rnd.Next(items.Length);
                queryKey = items[index].Key;
                queryValue = items[index].Value;
                description = String.Format("{0} (existing) [{1}]", descriptionPrefix, queryKey);
            }
            else
            {
                // with non-existing
                queryValue = default(float);
                do
                {
                    queryKey = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new MultiRankInfo<int, float>(queryKey), Comparer<MultiRankInfo<int, float>>.Default) >= 0);
                description = String.Format("{0} (nonexisting) [{1}]", descriptionPrefix, queryKey);
            }

            int modelKey = 0;
            float modelValue = 0;
            int modelRank = 0;
            int modelRankCount = 0;
            bool f = false;
            for (int i = 0; i < collections.Length; i++)
            {
                int instanceKey;
                float instanceValue;
                int instanceRank, instanceRankCount;
                bool f1 = variantMethod(collections[i], queryKey, out instanceKey, out instanceValue, out instanceRank, out instanceRankCount);
                if (i == 0)
                {
                    modelKey = instanceKey;
                    modelValue = instanceValue;
                    modelRank = instanceRank;
                    modelRankCount = instanceRankCount;
                    f = f1;
                }
                else
                {
                    if (modelKey != instanceKey)
                    {
                        Fault(collections[i], description + "key");
                    }
                    if (modelValue != instanceValue)
                    {
                        Fault(collections[i], description + "value");
                    }
                    if (modelRank != instanceRank)
                    {
                        Fault(collections[i], description + "rank");
                    }
                    if (modelRankCount != instanceRankCount)
                    {
                        Fault(collections[i], description + "rankCount");
                    }
                    if (f != f1)
                    {
                        Fault(collections[i], description + "result");
                    }
                }
            }
        }

        private void NearestLessOrEqualAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLessOrEqual";
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { valueOut = default(float); rank = 0; rankCount = 0; return collection.NearestLessOrEqual(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { rank = 0; rankCount = 0; return collection.NearestLessOrEqual(queryKey, out instanceKey, out valueOut); });
                    break;
                case 2:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { return collection.NearestLessOrEqual(queryKey, out instanceKey, out valueOut, out rank, out rankCount); });
                    break;
            }
        }

        private void NearestLessAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLess";
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { valueOut = default(float); rank = 0; rankCount = 0; return collection.NearestLess(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { rank = 0; rankCount = 0; return collection.NearestLess(queryKey, out instanceKey, out valueOut); });
                    break;
                case 2:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { return collection.NearestLess(queryKey, out instanceKey, out valueOut, out rank, out rankCount); });
                    break;
            }
        }

        private void NearestGreaterOrEqualAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreaterOrEqual";
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { valueOut = default(float); rank = 0; rankCount = 0; return collection.NearestGreaterOrEqual(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { rank = 0; rankCount = 0; return collection.NearestGreaterOrEqual(queryKey, out instanceKey, out valueOut); });
                    break;
                case 2:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { return collection.NearestGreaterOrEqual(queryKey, out instanceKey, out valueOut, out rank, out rankCount); });
                    break;
            }
        }

        private void NearestGreaterAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreater";
            switch (rnd.Next() % 3)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { valueOut = default(float); rank = 0; rankCount = 0; return collection.NearestGreater(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { rank = 0; rankCount = 0; return collection.NearestGreater(queryKey, out instanceKey, out valueOut); });
                    break;
                case 2:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut, out int rank, out int rankCount)
                        { return collection.NearestGreaterOrEqual(queryKey, out instanceKey, out valueOut, out rank, out rankCount); });
                    break;
            }
        }

        private delegate bool RankEquivalenceActionVariant1<KeyType, ValueType>(IMultiRankMap<KeyType, ValueType> collection, int position, out KeyType keyOut, out ValueType valueOut, out int nearestStart, out int length) where KeyType : IComparable<KeyType>;
        private void RankEquivalenceActionVariant1Util(IMultiRankMap<int, float>[] collections, Random rnd, ref string description, RankEquivalenceActionVariant1<int, float> variantMethod)
        {
            string descriptionPrefix = description;

            int extent = collections[0].RankCount;
            int position = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));

            int modelKey = 0;
            float modelValue = 0;
            int modelNearestStart = 0;
            int modelLength = 0;
            bool f = false;
            for (int i = 0; i < collections.Length; i++)
            {
                int instanceKey;
                float instanceValue;
                int instanceNearestStart, instanceLength;
                bool f1 = variantMethod(collections[i], position, out instanceKey, out instanceValue, out instanceNearestStart, out instanceLength);
                if (i == 0)
                {
                    modelKey = instanceKey;
                    modelValue = instanceValue;
                    modelNearestStart = instanceNearestStart;
                    modelLength = instanceLength;
                    f = f1;
                }
                else
                {
                    if (modelNearestStart != instanceNearestStart)
                    {
                        Fault(collections[i], description + "nearestStart");
                    }
                    if (modelKey != instanceKey)
                    {
                        Fault(collections[i], description + "key");
                    }
                    if (modelValue != instanceValue)
                    {
                        Fault(collections[i], description + "value");
                    }
                    if (modelLength != instanceLength)
                    {
                        Fault(collections[i], description + "length");
                    }
                    if (f != f1)
                    {
                        Fault(collections[i], description + "result");
                    }
                }
            }
        }

        private void NearestLessOrEqualByRankAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLessOrEqualByRank";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { valueOut = default(float); keyOut = 0; length = 0; return collection.NearestLessOrEqualByRank(position, out nearestStart); });
                    break;
                case 1:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { return collection.NearestLessOrEqualByRank(position, out keyOut, out nearestStart, out length, out valueOut); });
                    break;
            }
        }

        private void NearestLessByRankAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLessByRank";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { valueOut = default(float); keyOut = 0; length = 0; return collection.NearestLessByRank(position, out nearestStart); });
                    break;
                case 1:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { return collection.NearestLessByRank(position, out keyOut, out nearestStart, out length, out valueOut); });
                    break;
            }
        }

        private void NearestGreaterOrEqualByRankAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreaterOrEqualByRank";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { valueOut = default(float); keyOut = 0; length = 0; return collection.NearestGreaterOrEqualByRank(position, out nearestStart); });
                    break;
                case 1:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { return collection.NearestGreaterOrEqualByRank(position, out keyOut, out nearestStart, out length, out valueOut); });
                    break;
            }
        }

        private void NearestGreaterByRankAction(IMultiRankMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreaterByRank";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { valueOut = default(float); keyOut = 0; length = 0; return collection.NearestGreaterByRank(position, out nearestStart); });
                    break;
                case 1:
                    RankEquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IMultiRankMap<int, float> collection, int position, out int keyOut, out float valueOut, out int nearestStart, out int length)
                        { return collection.NearestGreaterByRank(position, out keyOut, out nearestStart, out length, out valueOut); });
                    break;
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
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), RankCountAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), ContainsKeyAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(400 - 130, 400 + 200), TryAddAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(460      , 460      ), TryRemoveAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), TryGetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), TrySetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), TryGetAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), TryGetKeyByRankAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(400 - 130, 400 + 200), AddAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(460      , 460      ), RemoveAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), GetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), SetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), GetAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), GetKeyByRankAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(280      , 280      ), AdjustCountAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), LeastAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), GreatestAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(300      , 300      ), NearestLessOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(300      , 300      ), NearestLessAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(300      , 300      ), NearestGreaterOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(300      , 300      ), NearestGreaterAction),

                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), NearestLessOrEqualByRankAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), NearestLessByRankAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), NearestGreaterOrEqualByRankAction),
                new Tuple<Tuple<int, int>, InvokeAction<IMultiRankMap<int, float>>>(new Tuple<int, int>(200      , 200      ), NearestGreaterByRankAction),
            };

            return StochasticDriver(
                "MultiRank Map Stochastic Test",
                seed,
                control,
                collections,
                actions,
                delegate (IMultiRankMap<int, float> _collection) { return _collection.Count; },
                delegate (IMultiRankMap<int, float>[] _collections) { Validate<int, float>(_collections); });
        }
    }
}
