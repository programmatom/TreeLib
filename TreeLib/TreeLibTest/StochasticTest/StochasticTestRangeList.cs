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
    public class StochasticTestRangeList : UnitTestRangeMap
    {
        public StochasticTestRangeList(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private void Validate(IRangeList[] collections)
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
                    ValidateRangesEqual<string>(items, items1); // no actual Values - pass nullable comparable type as placeholder
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


        private void ContainsAction(IRangeList[] collections, Random rnd, ref string description)
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

        private void TryInsertAction(IRangeList[] collections, Random rnd, ref string description)
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
            if (rnd.Next(2) == 0)
            {
                // insert valid
                int index = rnd.Next(items.Length + 1);
                int start = index < items.Length ? Start(items[index]) : extent;
                description = String.Format("TryInsert (valid) [{0}:<{1}>]", start, xLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryInsert(start, xLength);
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
                description = String.Format("TryInsert (invalid) [{0}:<{1}>]", start, xLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryInsert(start, xLength);
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

        private void TryDeleteAction(IRangeList[] collections, Random rnd, ref string description)
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

        private void TryGetLengthAction(IRangeList[] collections, Random rnd, ref string description)
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

        private void TrySetLengthAction(IRangeList[] collections, Random rnd, ref string description)
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

        private void InsertAction(IRangeList[] collections, Random rnd, ref string description)
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
            if (rnd.Next(2) == 0)
            {
                // insert valid
                int index = rnd.Next(items.Length + 1);
                int start = index < items.Length ? Start(items[index]) : extent;
                description = String.Format("Insert (valid) [{0}:<{1}>]", start, xLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Insert(start, xLength);
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
                description = String.Format("Insert (invalid) [{0}:<{1}>]", start, xLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Insert(start, xLength);
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

        private void DeleteAction(IRangeList[] collections, Random rnd, ref string description)
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

        private void GetLengthAction(IRangeList[] collections, Random rnd, ref string description)
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

        private void SetLengthAction(IRangeList[] collections, Random rnd, ref string description)
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

        private void GetExtentAction(IRangeList[] collections, Random rnd, ref string description)
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

        private delegate bool OrderingVariantMethod(IRangeList collection, int position, out int nearestStart, out int length);
        private void OrderingActionBase(IRangeList[] collections, Random rnd, ref string description, OrderingVariantMethod variantAction)
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
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int nearestStartInstance;
                    int lengthInstance;
                    bool f1 = variantAction(collections[i], position, out nearestStartInstance, out lengthInstance);
                    if (i == 0)
                    {
                        f = f1;
                        nearestStartModel = nearestStartInstance;
                        lengthModel = lengthInstance;
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
                    }
                }
                catch (Exception exception)
                {
                    Fault(collections[i], description + " - threw exception", exception);
                }
            }
        }

        private void NearestLessOrEqualAction(IRangeList[] collections, Random rnd, ref string description)
        {
            description = "NearestLessOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { length = 0; return collection.NearestLessOrEqual(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { return collection.NearestLessOrEqual(position, out nearestStart, out length); });
                    break;
            }
        }

        private void NearestLessAction(IRangeList[] collections, Random rnd, ref string description)
        {
            description = "NearestLess";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { length = 0; return collection.NearestLess(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { return collection.NearestLess(position, out nearestStart, out length); });
                    break;
            }
        }

        private void NearestGreaterOrEqualAction(IRangeList[] collections, Random rnd, ref string description)
        {
            description = "NearestGreaterOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { length = 0; return collection.NearestGreaterOrEqual(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { return collection.NearestGreaterOrEqual(position, out nearestStart, out length); });
                    break;
            }
        }

        private void NearestGreaterAction(IRangeList[] collections, Random rnd, ref string description)
        {
            description = "NearestGreater";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { length = 0; return collection.NearestGreater(position, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRangeList collection, int position, out int nearestStart, out int length)
                        { return collection.NearestGreater(position, out nearestStart, out length); });
                    break;
            }
        }


        public override bool Do(int seed, StochasticControls control)
        {
            IRangeList[] collections = new IRangeList[]
            {
                new ReferenceRangeList(), // must be first

                new SplayTreeRangeList(),
                new SplayTreeArrayRangeList(),
                new AdaptRangeListToRangeListLong(new SplayTreeRangeListLong()),

                new RedBlackTreeRangeList(),
                new RedBlackTreeArrayRangeList(),
                new AdaptRangeListToRangeListLong(new RedBlackTreeRangeListLong()),

                new AVLTreeRangeList(),
                new AVLTreeArrayRangeList(),
                new AdaptRangeListToRangeListLong(new AVLTreeRangeListLong()),
            };

            Tuple<Tuple<int, int>, InvokeAction<IRangeList>>[] actions = new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>[]
            {
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(100     , 100      ), ContainsAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(300 - 90, 300 + 100), TryInsertAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(300     , 300      ), TryDeleteAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(100     , 100      ), TryGetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(100     , 100      ), TrySetLengthAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(300 - 90, 300 + 100), InsertAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(300     , 300      ), DeleteAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(100     , 100      ), GetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(100     , 100      ), SetLengthAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(100     , 100      ), GetExtentAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(150     , 150      ), NearestLessOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(150     , 150      ), NearestLessAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(150     , 150      ), NearestGreaterOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRangeList>>(new Tuple<int, int>(150     , 150      ), NearestGreaterAction),
            };

            return StochasticDriver(
                "Range Map Stochastic Test",
                seed,
                control,
                collections,
                actions,
                delegate (IRangeList _collection) { return _collection.Count; },
                delegate (IRangeList[] _collections) { Validate(_collections); });
        }
    }
}
