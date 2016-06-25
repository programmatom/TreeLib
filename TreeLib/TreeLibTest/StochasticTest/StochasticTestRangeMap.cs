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
    public class StochasticTestRangeMap : UnitTestRangeMap
    {
        public StochasticTestRangeMap(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private void Validate<ValueType>(IRangeMap<ValueType>[] collections) where ValueType : IComparable<ValueType>
        {
            uint count = UInt32.MaxValue;
            Range2MapEntry[] items = null;
            for (int i = 0; i < collections.Length; i++)
            {
                Range2MapEntry[] items1 = null;

                INonInvasiveRange2MapInspection treeInspector;
                if ((items1 == null) && (treeInspector = collections[i] as INonInvasiveRange2MapInspection) != null)
                {
                    items1 = treeInspector.GetRanges();
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
                    ValidateRanges(items);
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
                    ValidateRanges(items1);
                    ValidateRangesEqual<ValueType>(items, items1);
                }
            }
        }


        private int Start(Range2MapEntry entry)
        {
            return entry.x.start;
        }

        private int Length(Range2MapEntry entry)
        {
            return entry.x.length;
        }


        private void ContainsAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int start = Start(items[rnd.Next(items.Length)]);
                    description = String.Format("Contains (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].Contains(start);
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
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("Contains (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].Contains(start);
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

        private void TryInsertAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            int xLength = rnd.Next(-1, 100);
            bool overflow = false;
            if ((rnd.Next(100) == 0) && (collections[0].GetExtent() > 0))
            {
                xLength = Int32.MaxValue;
                overflow = true;
            }
            float value = (float)rnd.NextDouble();
            if (rnd.Next(2) == 0)
            {
                // insert valid
                int index = rnd.Next(items.Length + 1);
                int start = index < items.Length ? Start(items[index]) : extent;
                description = String.Format("TryInsert (valid) [{0}:<{1}>, {2}]", start, xLength, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryInsert(start, xLength, value);
                        if (!f)
                        {
                            Fault(collections[i], description + " - returned false");
                        }
                    }
                    catch (ArgumentOutOfRangeException) when (xLength <= 0)
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
            else
            {
                // insert invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0));
                description = String.Format("TryInsert (invalid) [{0}:<{1}>, {2}]", start, xLength, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryInsert(start, xLength, value);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                    }
                    catch (ArgumentOutOfRangeException) when ((xLength <= 0) || (start < 0))
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

        private void TryDeleteAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // delete valid
                if (items.Length != 0)
                {
                    int start = Start(items[rnd.Next(items.Length)]);
                    description = String.Format("TryDelete (valid) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TryDelete(start);
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
                // delete invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("TryDelete (invalid) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryDelete(start);
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

        private void TryGetLengthAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("TryGetLength (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int length;
                            bool f = collections[i].TryGetLength(start, out length);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                            if (length != Length(items[index]))
                            {
                                Fault(collections[i], description + " - wrong result");
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
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("TryGetLength (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int length;
                        bool f = collections[i].TryGetLength(start, out length);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                        if (length != 0)
                        {
                            Fault(collections[i], description + " - wrong result");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TrySetLengthAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            int length = rnd.Next(-1, 100);
            bool overflow = false;
            if (rnd.Next(2) == 0)
            {
                // set valid
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    if ((rnd.Next(100) == 0) && (collections[0].GetExtent() - items[index].x.length > 0))
                    {
                        length = Int32.MaxValue;
                        overflow = true;
                    }
                    description = String.Format("TrySetLength (valid) [{0}:<{1}>]", start, length);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TrySetLength(start, length);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                        }
                        catch (ArgumentOutOfRangeException) when (length <= 0)
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
            else
            {
                // set invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0));
                if (rnd.Next(100) == 0)
                {
                    length = Int32.MaxValue;
                    overflow = true;
                }
                description = String.Format("TrySetLength (invalid) [{0}:<{1}>]", start, length);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TrySetLength(start, length);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                    }
                    catch (ArgumentOutOfRangeException) when (length <= 0)
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

        private void TryGetValueAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("TryGetValue (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            float value;
                            bool f = collections[i].TryGetValue(start, out value);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                            if (value != (float)items[index].value)
                            {
                                Fault(collections[i], description + " - wrong result");
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
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("TryGetValue (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        float value;
                        bool f = collections[i].TryGetValue(start, out value);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                        if (value != 0)
                        {
                            Fault(collections[i], description + " - wrong result");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void TrySetValueAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            float value = (float)rnd.NextDouble();
            if (rnd.Next(2) == 0)
            {
                // set valid
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("TrySetValue (valid) [{0}:<{1}>]", start, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TrySetValue(start, value);
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
                // set invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0));
                description = String.Format("TrySetValue (invalid) [{0}:<{1}>]", start, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TrySetValue(start, value);
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

        private void TryGetAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("TryGet (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int xLength;
                            float value;
                            bool f = collections[i].TryGet(start, out xLength, out value);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                            if (xLength != items[index].x.length)
                            {
                                Fault(collections[i], description + " - wrong result");
                            }
                            if (value != (float)items[index].value)
                            {
                                Fault(collections[i], description + " - wrong result");
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
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("TryGet (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int xLength;
                        float value;
                        bool f = collections[i].TryGet(start, out xLength, out value);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                        if (xLength != 0)
                        {
                            Fault(collections[i], description + " - wrong result");
                        }
                        if (value != 0)
                        {
                            Fault(collections[i], description + " - wrong result");
                        }
                    }
                    catch (Exception exception)
                    {
                        Fault(collections[i], description + " - threw exception", exception);
                    }
                }
            }
        }

        private void InsertAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            int xLength = rnd.Next(-1, 100);
            bool overflow = false;
            if ((rnd.Next(100) == 0) && (collections[0].GetExtent() > 0))
            {
                xLength = Int32.MaxValue;
                overflow = true;
            }
            float value = (float)rnd.NextDouble();
            if (rnd.Next(2) == 0)
            {
                // insert valid
                int index = rnd.Next(items.Length + 1);
                int start = index < items.Length ? Start(items[index]) : extent;
                description = String.Format("Insert (valid) [{0}:<{1}>, {2}]", start, xLength, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Insert(start, xLength, value);
                    }
                    catch (ArgumentOutOfRangeException) when (xLength <= 0)
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
            else
            {
                // insert invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0));
                description = String.Format("Insert (invalid) [{0}:<{1}>, {2}]", start, xLength, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Insert(start, xLength, value);
                        Fault(collections[i], description + " - did not throw exception");
                    }
                    catch (ArgumentOutOfRangeException) when ((xLength <= 0) || (start < 0))
                    {
                        // expected
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

        private void DeleteAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // delete valid
                if (items.Length != 0)
                {
                    int start = Start(items[rnd.Next(items.Length)]);
                    description = String.Format("Delete (valid) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].Delete(start);
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
                // delete invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("Delete (invalid) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Delete(start);
                        Fault(collections[i], description + " - did not throw exception");
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

        private void GetLengthAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("GetLength (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int length = collections[i].GetLength(start);
                            if (length != Length(items[index]))
                            {
                                Fault(collections[i], description + " - wrong result");
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
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("GetLength (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int length = collections[i].GetLength(start);
                        Fault(collections[i], description + " - did not throw exception");
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

        private void SetLengthAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            int length = rnd.Next(-1, 100);
            bool overflow = false;
            if (rnd.Next(2) == 0)
            {
                // set valid
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    if ((rnd.Next(100) == 0) && (collections[0].GetExtent() - items[index].x.length > 0))
                    {
                        length = Int32.MaxValue;
                        overflow = true;
                    }
                    description = String.Format("SetLength (valid) [{0}:<{1}>]", start, length);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].SetLength(start, length);
                        }
                        catch (ArgumentOutOfRangeException) when (length <= 0)
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
            else
            {
                // set invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0));
                if (rnd.Next(100) == 0)
                {
                    length = Int32.MaxValue;
                    overflow = true;
                }
                description = String.Format("SetLength (invalid) [{0}:<{1}>]", start, length);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].SetLength(start, length);
                        Fault(collections[i], description + " - did not throw exception");
                    }
                    catch (ArgumentOutOfRangeException) when (length <= 0)
                    {
                        // expected
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

        private void GetValueAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("GetValue (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            float value = collections[i].GetValue(start);
                            if (value != (float)items[index].value)
                            {
                                Fault(collections[i], description + " - wrong result");
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
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("GetValue (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        float value = collections[i].GetValue(start);
                        Fault(collections[i], description + " - did not throw exception");
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

        private void SetValueAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            float value = (float)rnd.NextDouble();
            if (rnd.Next(2) == 0)
            {
                // set valid
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("SetValue (valid) [{0}:<{1}>]", start, value);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].SetValue(start, value);
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
                // set invalid
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0));
                description = String.Format("SetValue (invalid) [{0}:<{1}>]", start, value);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].SetValue(start, value);
                        Fault(collections[i], description + " - did not throw exception");
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

        private void GetAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index]);
                    description = String.Format("Get (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int xLength;
                            float value;
                            collections[i].Get(start, out xLength, out value);
                            if (xLength != items[index].x.length)
                            {
                                Fault(collections[i], description + " - wrong result");
                            }
                            if (value != (float)items[index].value)
                            {
                                Fault(collections[i], description + " - wrong result");
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
                int start;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0);
                description = String.Format("Get (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int xLength;
                        float value;
                        collections[i].Get(start, out xLength, out value);
                        Fault(collections[i], description + " - did not throw exception");
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

        private void AdjustLengthAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? Start(items[items.Length - 1]) + Length(items[items.Length - 1]) : 0;

            int start, xLength;
            bool valid;
            bool remove = false;
            int index = -1;
            if ((rnd.Next() % 2 == 0) && (extent != 0))
            {
                // existing start
                valid = true;
                index = rnd.Next() % items.Length;
                start = items[index].x.start;
                remove = rnd.Next() % 4 == 0;
                if (!remove)
                {
                    xLength = rnd.Next(-items[index].x.length + 1, 100);
                }
                else
                {
                    xLength = -items[index].x.length;
                }
                if (rnd.Next() % 5 == 0)
                {
                    valid = false;
                    switch (rnd.Next() % 3)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case 0:
                            start = rnd.Next(-2, 0/*exclusive*/);
                            break;
                        case 1:
                            start = rnd.Next(extent, extent + 2/*exclusive*/);
                            break;
                        case 2:
                            xLength = Int32.MaxValue;
                            break;
                    }
                }
            }
            else
            {
                // non-existing start
                valid = false;
                do
                {
                    start = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while (((start == 0) && (extent != 0))
                    || (start == extent)
                    || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == start; }) >= 0));
                xLength = rnd.Next(-10, 100);
            }
            description = String.Format("AdjustLength {0} ({1}, {2})", valid ? "valid" : "invalid", start, xLength);

            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int newLength = collections[i].AdjustLength(start, xLength);
                    if (!valid)
                    {
                        Fault(collections[i], description + " - invalid input but did not throw exception");
                    }
                    if ((remove && (newLength != 0))
                        || (!remove && (newLength != items[index].x.length + xLength)))
                    {
                        Fault(collections[i], description + " - return value discrepancy");
                    }
                }
                catch (ArgumentException) when (!valid)
                {
                    // expected
                }
                catch (ArgumentOutOfRangeException) when (!valid)
                {
                    // expected
                }
                catch (OverflowException) when (!valid)
                {
                    // expected
                }
                catch (Exception exception)
                {
                    Fault(collections[i], description + " - threw exception", exception);
                }
            }
        }

        private void GetExtentAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            description = String.Format("GetExtent");
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int extent1 = collections[i].GetExtent();
                    if (extent1 != extent)
                    {
                        Fault(collections[i], description + " - wrong result");
                    }
                }
                catch (Exception exception)
                {
                    Fault(collections[i], description + " - threw exception", exception);
                }
            }
        }

        private delegate bool OrderingVariantMethod(IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float Value);
        private void OrderingActionBase(IRangeMap<float>[] collections, Random rnd, ref string description, OrderingVariantMethod variantAction)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            int extent = items.Length != 0 ? (Start(items[items.Length - 1]) + Length(items[items.Length - 1])) : 0;
            int position;
            if ((rnd.Next(2) == 0) && (items.Length != 0))
            {
                // valid start
                int index = rnd.Next(items.Length);
                position = Start(items[index]);
            }
            else
            {
                // non-start
                do
                {
                    position = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((position == 0) || (position == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate) == position; }) >= 0));
            }

            // test only equivalence -- rely on reference implementation being correct (assumed demonstrated during unit tests)
            description = description + String.Format(" {0}", position);
            bool f = false;
            int nearestStartModel = 0;
            int lengthModel = 0;
            float valueModel = 0;
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int nearestStartInstance;
                    int lengthInstance;
                    float valueInstance;
                    bool f1 = variantAction(collections[i], position, out nearestStartInstance, out lengthInstance, out valueInstance);
                    if (i == 0)
                    {
                        f = f1;
                        nearestStartModel = nearestStartInstance;
                        lengthModel = lengthInstance;
                        valueModel = valueInstance;
                    }
                    else
                    {
                        if (f != f1)
                        {
                            Fault(collections[i], description + " - return code discrepancy");
                        }
                        if (nearestStartModel != nearestStartInstance)
                        {
                            Fault(collections[i], description + " - nearestStart");
                        }
                        if (lengthModel != lengthInstance)
                        {
                            Fault(collections[i], description + " - length");
                        }
                        if (valueModel != valueInstance)
                        {
                            Fault(collections[i], description + " - value");
                        }
                    }
                }
                catch (Exception exception)
                {
                    Fault(collections[i], description + " - threw exception", exception);
                }
            }
        }

        private void NearestLessOrEqualAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLessOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { xLength = 0; value = default(float); return collection.NearestLessOrEqual(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { return collection.NearestLessOrEqual(position, out nearestStart, out xLength, out value); });
                    break;
            }
        }

        private void NearestLessAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            description = "NearestLess";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { xLength = 0; value = default(float); return collection.NearestLess(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { return collection.NearestLess(position, out nearestStart, out xLength, out value); });
                    break;
            }
        }

        private void NearestGreaterOrEqualAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreaterOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { xLength = 0; value = default(float); return collection.NearestGreaterOrEqual(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { return collection.NearestGreaterOrEqual(position, out nearestStart, out xLength, out value); });
                    break;
            }
        }

        private void NearestGreaterAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            description = "NearestGreater";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { xLength = 0; value = default(float); return collection.NearestGreater(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeMap<float> collection, int position, out int nearestStart, out int xLength, out float value)
                        { return collection.NearestGreater(position, out nearestStart, out xLength, out value); });
                    break;
            }
        }

        private void EnumerateAction(IRangeMap<float>[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] ranges = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            StartLength[] xStartLengths = Array.ConvertAll(ranges, delegate (Range2MapEntry item) { return new StartLength(item.x.start, item.x.length); });
            IndexedEnumerateAction<EntryRangeMap<float>>(collections, rnd, ref description, TreeKind.RangeMap, xStartLengths, null);
        }


        private delegate void Action(IRangeMap<float>[] collections, Random rnd, ref string description);

        public override bool Do(int seed, StochasticControls control)
        {
            IRangeMap<float>[] collections = new IRangeMap<float>[]
            {
                new ReferenceRangeMap< float>(), // must be first

                new SplayTreeRangeMap<float>(),
                new SplayTreeArrayRangeMap<float>(),
                new AdaptRangeMapToRangeMapLong<float>(new SplayTreeRangeMapLong<float>()),

                new RedBlackTreeRangeMap<float>(),
                new RedBlackTreeArrayRangeMap<float>(),
                new AdaptRangeMapToRangeMapLong<float>(new RedBlackTreeRangeMapLong<float>()),

                new AVLTreeRangeMap<float>(),
                new AVLTreeArrayRangeMap<float>(),
                new AdaptRangeMapToRangeMapLong<float>(new AVLTreeRangeMapLong<float>()),
            };

            Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>[] actions = new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>[]
            {
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), ContainsAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(300 - 90, 300 + 100), TryInsertAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(300     , 300      ), TryDeleteAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), TryGetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), TrySetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), TryGetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), TrySetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), TryGetAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(300 - 90, 300 + 100), InsertAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(300     , 300      ), DeleteAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), GetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), SetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), GetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), SetValueAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), GetAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(150     , 150      ), AdjustLengthAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(100     , 100      ), GetExtentAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(150     , 150      ), NearestLessOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(150     , 150      ), NearestLessAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(150     , 150      ), NearestGreaterOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(150     , 150      ), NearestGreaterAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeMap<float>>>(new Tuple<int, int>(75      , 75       ), EnumerateAction),
            };

            return StochasticDriver(
                "Range Map Stochastic Test",
                seed,
                control,
                collections,
                actions,
                delegate (IRangeMap<float> _collection) { return _collection.Count; },
                delegate (IRangeMap<float>[] _collections) { Validate<float>(_collections); });
        }
    }
}
