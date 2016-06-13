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
    public class StochasticTestMap : UnitTestMap
    {
        public StochasticTestMap(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private void Validate<KeyType, ValueType>(IOrderedMap<int, float>[] collections) where KeyType : IComparable<KeyType> where ValueType : IComparable<ValueType>
        {
            uint count = UInt32.MaxValue;
            KeyValuePair<KeyType, ValueType>[] items = null;
            for (int i = 0; i < collections.Length; i++)
            {
                KeyValuePair<KeyType, ValueType>[] items1 = null;

                ISimpleTreeInspection<KeyType, ValueType> simpleInspector;
                if ((items1 == null) && (simpleInspector = collections[i] as ISimpleTreeInspection<KeyType, ValueType>) != null)
                {
                    items1 = simpleInspector.ToArray();
                    try
                    {
                        simpleInspector.Validate();
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], "validate", exception);
                    }
                }
                INonInvasiveTreeInspection treeInspector;
                if ((items1 == null) && (treeInspector = collections[i] as INonInvasiveTreeInspection) != null)
                {
                    items1 = TreeInspection.Flatten<KeyType, ValueType>(treeInspector);
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
                    for (int j = 0; j < items.Length; j++)
                    {
                        if (0 != Comparer<KeyType>.Default.Compare(items[j].Key, items1[j].Key))
                        {
                            Fault(collections[i], "key");
                        }
                        if (0 != Comparer<ValueType>.Default.Compare(items[j].Value, items1[j].Value))
                        {
                            Fault(collections[i], "value");
                        }
                    }
                }
            }
        }


        private void ContainsKeyAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
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

        private void SetOrAddValueAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // add existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
                    float value = (float)rnd.NextDouble();
                    description = String.Format("SetOrAddValue (existing) [{0}, {1}]", key, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].SetOrAddValue(key, value);
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
            else
            {
                // add new
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
                float value = (float)rnd.NextDouble();
                description = String.Format("SetOrAddValue (new) [{0}, {1}]", key, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].SetOrAddValue(key, value);
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

        private void TryAddAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // add existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
                    float value = (float)rnd.NextDouble();
                    description = String.Format("TryAdd (existing) [{0}, {1}]", key, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TryAdd(key, value);
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
            else
            {
                // add new
                int key;
                do
                {
                    key = rnd.Next(Int32.MinValue, Int32.MaxValue);
                }
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
                float value = (float)rnd.NextDouble();
                description = String.Format("TryAdd (new) [{0}, {1}]", key, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryAdd(key, value);
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

        private void TryRemoveAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // remove existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
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

        private void TryGetValueAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // get existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int key = items[index].Key;
                    float existingValue = items[index].Value;
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
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

        private void TrySetValueAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // set existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
                    float value = (float)rnd.NextDouble();
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
                float value = (float)rnd.NextDouble();
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

        private void AddAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // add existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
                    float value = (float)rnd.NextDouble();
                    description = String.Format("Add (existing) [{0}, {1}]", key, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].Add(key, value);
                            Fault(collections[i], description + " - didn't throw");
                        }
                        catch (ArgumentException)
                        {
                            // expected
                        }
                        catch (Exception exception)
                        {
                            Fault(collections[i], description + " - threw wrong type of exception", exception);
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
                float value = (float)rnd.NextDouble();
                description = String.Format("Add (new) [{0}, {1}]", key, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Add(key, value);
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void RemoveAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // remove existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
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
                        Fault(collections[i], description + " - threw wrong type of exception", exception);
                    }
                }
            }
        }

        private void GetValueAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // get existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int key = items[index].Key;
                    float existingValue = items[index].Value;
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
                description = String.Format("GetValue (nonexisting) [{0}]", key);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        float actualValue = collections[i].GetValue(key);
                        Fault(collections[i], description + " - did not throw exception");
                    }
                    catch (ArgumentException)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw wrong type of exception", exception);
                    }
                }
            }
        }

        private void SetValueAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
            if (rnd.Next(2) == 0)
            {
                // set existing
                if (items.Length != 0)
                {
                    int key = items[rnd.Next(items.Length)].Key;
                    float value = (float)rnd.NextDouble();
                    description = String.Format("SetValue (existing) [{0}, {1}]", key, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].TrySetValue(key, value);
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(key, 0), ReferenceMap<int, float>.Comparer) >= 0);
                float value = (float)rnd.NextDouble();
                description = String.Format("SetValue (nonexistent) [{0}, {1}]", key, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].SetValue(key, value);
                        Fault(collections[i], description + " - did not throw");
                    }
                    catch (ArgumentException)
                    {
                        // expected
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw wrong type of exception", exception);
                    }
                }
            }
        }

        private delegate bool EquivalenceActionVariant0<KeyType, ValueType>(IOrderedMap<KeyType, ValueType> collection, out KeyType keyOut, out ValueType valueOut) where KeyType : IComparable<KeyType>;
        private void EquivalenceActionVariant0Util(IOrderedMap<int, float>[] collections, Random rnd, ref string description, EquivalenceActionVariant0<int, float> variantMethod)
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

        private void LeastAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "Least";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { instanceValue = default(float); return collection.Least(out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { return collection.Least(out instanceKey, out instanceValue); });
                    break;
            }
        }

        private void GreatestAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "Greatest";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { instanceValue = default(float); return collection.Greatest(out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant0Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, out int instanceKey, out float instanceValue)
                        { return collection.Greatest(out instanceKey, out instanceValue); });
                    break;
            }
        }

        private delegate bool EquivalenceActionVariant1<KeyType, ValueType>(IOrderedMap<KeyType, ValueType> collection, KeyType key, out KeyType keyOut, out ValueType valueOut) where KeyType : IComparable<KeyType>;
        private void EquivalenceActionVariant1Util(IOrderedMap<int, float>[] collections, Random rnd, ref string description, EquivalenceActionVariant1<int, float> variantMethod)
        {
            string descriptionPrefix = description;

            int queryKey;
            float queryValue;

            KeyValuePair<int, float>[] items = ((ISimpleTreeInspection<int, float>)collections[0]).ToArray();
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
                while (Array.BinarySearch(items, new KeyValuePair<int, float>(queryKey, 0), ReferenceMap<int, float>.Comparer) >= 0);
                description = String.Format("{0} (nonexisting) [{1}]", descriptionPrefix, queryKey);
            }

            int modelKey = 0;
            float modelValue = 0;
            bool f = false;
            for (int i = 0; i < collections.Length; i++)
            {
                int instanceKey;
                float instanceValue;
                bool f1 = variantMethod(collections[i], queryKey, out instanceKey, out instanceValue);
                if (i == 0)
                {
                    modelKey = instanceKey;
                    modelValue = instanceValue;
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
                    if (f != f1)
                    {
                        Fault(collections[i], description + "result");
                    }
                }
            }
        }

        private void NearestLessOrEqualAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLessOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { valueOut = default(float); return collection.NearestLessOrEqual(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { return collection.NearestLessOrEqual(queryKey, out instanceKey, out valueOut); });
                    break;
            }
        }

        private void NearestLessAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLess";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { valueOut = default(float); return collection.NearestLess(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { return collection.NearestLess(queryKey, out instanceKey, out valueOut); });
                    break;
            }
        }

        private void NearestGreaterOrEqualAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreaterOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { valueOut = default(float); return collection.NearestGreaterOrEqual(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { return collection.NearestGreaterOrEqual(queryKey, out instanceKey, out valueOut); });
                    break;
            }
        }

        private void NearestGreaterAction(IOrderedMap<int, float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreater";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { valueOut = default(float); return collection.NearestGreater(queryKey, out instanceKey); });
                    break;
                case 1:
                    EquivalenceActionVariant1Util(collections, rnd, ref description,
                        delegate (IOrderedMap<int, float> collection, int queryKey, out int instanceKey, out float valueOut)
                        { return collection.NearestGreater(queryKey, out instanceKey, out valueOut); });
                    break;
            }
        }


        public override bool Do(int seed, StochasticControls control)
        {
            IOrderedMap<int, float>[] collections = new IOrderedMap<int, float>[]
            {
                new ReferenceMap<int, float>(), // must be first

                new SplayTreeMap<int, float>(),
                new SplayTreeArrayMap<int, float>(),
                new AdaptListToMap<int, float>(new SplayTreeList<KeyValue<int, float>>()),
                new AdaptListToMap<int, float>(new SplayTreeArrayList<KeyValue<int, float>>()),

                new RedBlackTreeMap<int, float>(),
                new RedBlackTreeArrayMap<int, float>(),
                new AdaptListToMap<int, float>(new RedBlackTreeList<KeyValue<int, float>>()),
                new AdaptListToMap<int, float>(new RedBlackTreeArrayList<KeyValue<int, float>>()),

                new AVLTreeMap<int, float>(),
                new AVLTreeArrayMap<int, float>(),
                new AdaptListToMap<int, float>(new AVLTreeList<KeyValue<int, float>>()),
                new AdaptListToMap<int, float>(new AVLTreeArrayList<KeyValue<int, float>>()),
            };

            Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>[] actions = new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>[]
            {
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), ContainsKeyAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(200     , 200     ), SetOrAddValueAction),

                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(200 - 50, 200 + 50), TryAddAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(300     , 300     ), TryRemoveAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), TryGetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), TrySetValueAction),

                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(200 - 50, 200 + 50), AddAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(300     , 300     ), RemoveAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), GetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), SetValueAction),

                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), LeastAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), GreatestAction),

                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), NearestLessOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), NearestLessAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), NearestGreaterOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IOrderedMap<int, float>>>(new Tuple<int, int>(100     , 100     ), NearestGreaterAction),
            };

            return StochasticDriver(
                "Map Stochastic Test",
                seed,
                control,
                collections,
                actions,
                delegate (IOrderedMap<int, float> _collection) { return _collection.Count; },
                delegate (IOrderedMap<int, float>[] _collections) { Validate<int, float>(_collections); });
        }
    }
}
