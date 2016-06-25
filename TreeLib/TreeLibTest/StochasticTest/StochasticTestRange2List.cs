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
    public class StochasticTestRange2List : UnitTestRange2Map
    {
        public StochasticTestRange2List(long[] breakIterations, long startIteration)
            : base(breakIterations, startIteration)
        {
        }


        private void Validate(IRange2List[] collections)
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


        private int Start(Range2MapEntry entry, Side side)
        {
            return side == Side.X ? entry.x.start : entry.y.start;
        }

        private int Length(Range2MapEntry entry, Side side)
        {
            return side == Side.X ? entry.x.length : entry.y.length;
        }


        private void ContainsAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int start = Start(items[rnd.Next(items.Length)], side);
                    description = String.Format("Contains (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].Contains(start, side);
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
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0);
                description = String.Format("Contains (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].Contains(start, side);
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

        private void TryInsertAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            int xLength = rnd.Next(-1, 100);
            int yLength = rnd.Next(-1, 100);
            bool overflow = false;
            if (rnd.Next(100) == 0)
            {
                if ((rnd.Next(2) == 0) && (collections[0].GetExtent(Side.X) > 0))
                {
                    xLength = Int32.MaxValue;
                    overflow = true;
                }
                else if (collections[0].GetExtent(Side.Y) > 0)
                {
                    yLength = Int32.MaxValue;
                    overflow = true;
                }
            }
            if (rnd.Next(2) == 0)
            {
                // insert valid
                int index = rnd.Next(items.Length + 1);
                int start = index < items.Length ? Start(items[index], side) : extent;
                description = String.Format("TryInsert (valid) [{0}:<{1},{2}>]", start, xLength, yLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryInsert(start, side, xLength, yLength);
                        if (!f)
                        {
                            Fault(collections[i], description + " - returned false");
                        }
                    }
                    catch (ArgumentOutOfRangeException) when ((xLength <= 0) || (yLength <= 0))
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
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0));
                description = String.Format("TryInsert (invalid) [{0}:<{1},{2}>]", start, xLength, yLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryInsert(start, side, xLength, yLength);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                    }
                    catch (ArgumentOutOfRangeException) when ((xLength <= 0) || (yLength <= 0) || (start < 0))
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

        private void TryDeleteAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            if (rnd.Next(2) == 0)
            {
                // delete valid
                if (items.Length != 0)
                {
                    int start = Start(items[rnd.Next(items.Length)], side);
                    description = String.Format("TryDelete (valid) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TryDelete(start, side);
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
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0);
                description = String.Format("TryDelete (invalid) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        bool f = collections[i].TryDelete(start, side);
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

        private void TryGetLengthAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index], side);
                    description = String.Format("TryGetLength (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int length;
                            bool f = collections[i].TryGetLength(start, side, out length);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                            if (length != Length(items[index], side))
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
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0);
                description = String.Format("TryGetLength (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int length;
                        bool f = collections[i].TryGetLength(start, side, out length);
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

        private void TrySetLengthAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            int length = rnd.Next(-1, 100);
            bool overflow = false;
            if (rnd.Next(2) == 0)
            {
                // set valid
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index], side);
                    if ((rnd.Next(100) == 0) && (collections[0].GetExtent(side) - (side == Side.X ? items[index].x.length : items[index].y.length) > 0))
                    {
                        length = Int32.MaxValue;
                        overflow = true;
                    }
                    description = String.Format("TrySetLength (valid) [{0}:<{1}>]", start, length);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            bool f = collections[i].TrySetLength(start, side, length);
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
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0));
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
                        bool f = collections[i].TrySetLength(start, side, length);
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

        private void TryGetAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index], side);
                    description = String.Format("TryGet (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int otherSide, xLength, yLength;
                            bool f = collections[i].TryGet(start, side, out otherSide, out xLength, out yLength);
                            if (!f)
                            {
                                Fault(collections[i], description + " - returned false");
                            }
                            if (otherSide != Start(items[index], (Side)((int)side ^ 1)))
                            {
                                Fault(collections[i], description + " - wrong result");
                            }
                            if (xLength != items[index].x.length)
                            {
                                Fault(collections[i], description + " - wrong result");
                            }
                            if (yLength != items[index].y.length)
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
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0);
                description = String.Format("TryGet (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int otherSide, xLength, yLength;
                        bool f = collections[i].TryGet(start, side, out otherSide, out xLength, out yLength);
                        if (f)
                        {
                            Fault(collections[i], description + " - returned true");
                        }
                        if (otherSide != 0)
                        {
                            Fault(collections[i], description + " - wrong result");
                        }
                        if (xLength != 0)
                        {
                            Fault(collections[i], description + " - wrong result");
                        }
                        if (yLength != 0)
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

        private void InsertAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            int xLength = rnd.Next(-1, 100);
            int yLength = rnd.Next(-1, 100);
            bool overflow = false;
            if (rnd.Next(100) == 0)
            {
                if ((rnd.Next(2) == 0) && (collections[0].GetExtent(Side.X) > 0))
                {
                    xLength = Int32.MaxValue;
                    overflow = true;
                }
                else if (collections[0].GetExtent(Side.Y) > 0)
                {
                    yLength = Int32.MaxValue;
                    overflow = true;
                }
            }
            if (rnd.Next(2) == 0)
            {
                // insert valid
                int index = rnd.Next(items.Length + 1);
                int start = index < items.Length ? Start(items[index], side) : extent;
                description = String.Format("Insert (valid) [{0}:<{1},{2}>]", start, xLength, yLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Insert(start, side, xLength, yLength);
                    }
                    catch (ArgumentOutOfRangeException) when ((xLength <= 0) || (yLength <= 0))
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
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0));
                description = String.Format("Insert (invalid) [{0}:<{1},{2}>]", start, xLength, yLength);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Insert(start, side, xLength, yLength);
                        Fault(collections[i], description + " - did not throw exception");
                    }
                    catch (ArgumentOutOfRangeException) when ((xLength <= 0) || (yLength <= 0) || (start < 0))
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

        private void DeleteAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            if (rnd.Next(2) == 0)
            {
                // delete valid
                if (items.Length != 0)
                {
                    int start = Start(items[rnd.Next(items.Length)], side);
                    description = String.Format("Delete (valid) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].Delete(start, side);
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
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0);
                description = String.Format("Delete (invalid) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        collections[i].Delete(start, side);
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

        private void GetLengthAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index], side);
                    description = String.Format("GetLength (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int length = collections[i].GetLength(start, side);
                            if (length != Length(items[index], side))
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
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0);
                description = String.Format("GetLength (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int length = collections[i].GetLength(start, side);
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

        private void SetLengthAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            int length = rnd.Next(-1, 100);
            bool overflow = false;
            if (rnd.Next(2) == 0)
            {
                // set valid
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index], side);
                    if ((rnd.Next(100) == 0) && (collections[0].GetExtent(side) - (side == Side.X ? items[index].x.length : items[index].y.length) > 0))
                    {
                        length = Int32.MaxValue;
                        overflow = true;
                    }
                    description = String.Format("SetLength (valid) [{0}:<{1}>]", start, length);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            collections[i].SetLength(start, side, length);
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
                while ((start == 0) || (start == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0));
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
                        collections[i].SetLength(start, side, length);
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

        private void GetAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            if (rnd.Next(2) == 0)
            {
                // existing
                if (items.Length != 0)
                {
                    int index = rnd.Next(items.Length);
                    int start = Start(items[index], side);
                    description = String.Format("Get (existing) [{0}]", start);
                    for (int i = 0; i < collections.Length; i++)
                    {
                        try
                        {
                            int otherSide, xLength, yLength;
                            collections[i].Get(start, side, out otherSide, out xLength, out yLength);
                            if (otherSide != Start(items[index], (Side)((int)side ^ 1)))
                            {
                                Fault(collections[i], description + " - wrong result");
                            }
                            if (xLength != items[index].x.length)
                            {
                                Fault(collections[i], description + " - wrong result");
                            }
                            if (yLength != items[index].y.length)
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
                while (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0);
                description = String.Format("Get (missing) [{0}]", start);
                for (int i = 0; i < collections.Length; i++)
                {
                    try
                    {
                        int otherSide, xLength, yLength;
                        collections[i].Get(start, side, out otherSide, out xLength, out yLength);
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

        private void AdjustLengthAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;

            int start, xLength, yLength;
            bool valid;
            bool remove = false;
            int index = -1;
            if ((rnd.Next() % 2 == 0) && (extent != 0))
            {
                // existing start
                valid = true;
                index = rnd.Next() % items.Length;
                start = side == Side.X ? items[index].x.start : items[index].y.start;
                remove = rnd.Next() % 4 == 0;
                if (!remove)
                {
                    xLength = rnd.Next(-items[index].x.length + 1, 100);
                    yLength = rnd.Next(-items[index].y.length + 1, 100);
                }
                else
                {
                    xLength = -items[index].x.length;
                    yLength = -items[index].y.length;
                }
                if (rnd.Next() % 5 == 0)
                {
                    valid = false;
                    switch (rnd.Next() % 4)
                    {
                        default:
                            Debug.Assert(false);
                            throw new InvalidOperationException();
                        case 0:
                            if (!remove)
                            {
                                // make one side become zero-length
                                if (rnd.Next() % 2 == 0)
                                {
                                    xLength = -items[index].x.length;
                                }
                                else
                                {
                                    yLength = -items[index].y.length;
                                }
                            }
                            else
                            {
                                // make one side become non-zero length
                                if (rnd.Next() % 2 == 0)
                                {
                                    xLength += rnd.Next() % 2 == 0 ? 1 : -1;
                                }
                                else
                                {
                                    yLength += rnd.Next() % 2 == 0 ? 1 : -1;
                                }
                            }
                            break;
                        case 1:
                            start = rnd.Next(-2, 0/*exclusive*/);
                            break;
                        case 2:
                            start = rnd.Next(extent, extent + 2/*exclusive*/);
                            break;
                        case 3:
                            if (rnd.Next() % 2 == 0)
                            {
                                xLength = Int32.MaxValue;
                            }
                            else
                            {
                                yLength = Int32.MaxValue;
                            }
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
                    || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == start; }) >= 0));
                xLength = rnd.Next(-10, 100);
                yLength = rnd.Next(-10, 100);
            }
            description = String.Format("AdjustLength {0} ({1}, {2}, {3}, {4})", valid ? "valid" : "invalid", start, side, xLength, yLength);

            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int newLength = collections[i].AdjustLength(start, side, xLength, yLength);
                    if (!valid)
                    {
                        Fault(collections[i], description + " - invalid input but did not throw exception");
                    }
                    if ((remove && (newLength != 0))
                        || (!remove && (newLength != (side == Side.X ? items[index].x.length + xLength : items[index].y.length + yLength))))
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

        private void GetExtentAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            description = String.Format("GetExtent");
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int extent1 = collections[i].GetExtent(side);
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

        private delegate bool OrderingVariantMethod(IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength);
        private void OrderingActionBase(IRange2List[] collections, Random rnd, ref string description, OrderingVariantMethod variantAction)
        {
            Range2MapEntry[] items = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            Side side = rnd.Next(2) == 0 ? Side.X : Side.Y;
            int extent = items.Length != 0 ? (Start(items[items.Length - 1], side) + Length(items[items.Length - 1], side)) : 0;
            int position;
            if ((rnd.Next(2) == 0) && (items.Length != 0))
            {
                // valid start
                int index = rnd.Next(items.Length);
                position = Start(items[index], side);
            }
            else
            {
                // non-start
                do
                {
                    position = rnd.Next(Math.Min(-1, -extent / 40), extent + Math.Max(1 + 1/*upper bound is exclusive*/, extent / 40));
                }
                while ((position == 0) || (position == extent) || (Array.FindIndex(items, delegate (Range2MapEntry candidate) { return Start(candidate, side) == position; }) >= 0));
            }

            // test only equivalence -- rely on reference implementation being correct (assumed demonstrated during unit tests)
            description = description + String.Format(" {0}", position);
            bool f = false;
            int nearestStartModel = 0;
            int otherStartModel = 0;
            int xLengthModel = 0;
            int yLengthModel = 0;
            for (int i = 0; i < collections.Length; i++)
            {
                try
                {
                    int nearestStartInstance;
                    int otherStartInstance;
                    int xLengthInstance;
                    int yLengthInstance;
                    bool f1 = variantAction(collections[i], position, side, out nearestStartInstance, out otherStartInstance, out xLengthInstance, out yLengthInstance);
                    if (i == 0)
                    {
                        f = f1;
                        nearestStartModel = nearestStartInstance;
                        otherStartModel = otherStartInstance;
                        xLengthModel = xLengthInstance;
                        yLengthModel = yLengthInstance;
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
                        if (otherStartModel != otherStartInstance)
                        {
                            Fault(collections[i], description + " - otherStart");
                        }
                        if (xLengthModel != xLengthInstance)
                        {
                            Fault(collections[i], description + " - xLength");
                        }
                        if (yLengthModel != yLengthInstance)
                        {
                            Fault(collections[i], description + " - yLength");
                        }
                    }
                }
                catch (Exception exception)
                {
                    Fault(collections[i], description + " - threw exception", exception);
                }
            }
        }

        private void NearestLessOrEqualAction(IRange2List[] collections, Random rnd, ref string description)
        {
            description = "NearestLessOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { otherStart = 0; xLength = 0; yLength = 0; return collection.NearestLessOrEqual(position, side, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { return collection.NearestLessOrEqual(position, side, out nearestStart, out otherStart, out xLength, out yLength); });
                    break;
            }
        }

        private void NearestLessAction(IRange2List[] collections, Random rnd, ref string description)
        {
            description = "NearestLess";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { otherStart = 0; xLength = 0; yLength = 0; return collection.NearestLess(position, side, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { return collection.NearestLess(position, side, out nearestStart, out otherStart, out xLength, out yLength); });
                    break;
            }
        }

        private void NearestGreaterOrEqualAction(IRange2List[] collections, Random rnd, ref string description)
        {
            description = "NearestGreaterOrEqual";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { otherStart = 0; xLength = 0; yLength = 0; return collection.NearestGreaterOrEqual(position, side, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { return collection.NearestGreaterOrEqual(position, side, out nearestStart, out otherStart, out xLength, out yLength); });
                    break;
            }
        }

        private void NearestGreaterAction(IRange2List[] collections, Random rnd, ref string description)
        {
            description = "NearestGreater";
            switch (rnd.Next() % 2)
            {
                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                case 0:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { otherStart = 0; xLength = 0; yLength = 0; return collection.NearestGreater(position, side, out nearestStart); });
                    break;
                case 1:
                    OrderingActionBase(collections, rnd, ref description,
                        delegate (IRange2List collection, int position, Side side, out int nearestStart, out int otherStart, out int xLength, out int yLength)
                        { return collection.NearestGreater(position, side, out nearestStart, out otherStart, out xLength, out yLength); });
                    break;
            }
        }

        private void EnumerateAction(IRange2List[] collections, Random rnd, ref string description)
        {
            Range2MapEntry[] ranges = ((INonInvasiveRange2MapInspection)collections[0]).GetRanges();
            StartLength[] xStartLengths = Array.ConvertAll(ranges, delegate (Range2MapEntry item) { return new StartLength(item.x.start, item.x.length); });
            StartLength[] yStartLengths = Array.ConvertAll(ranges, delegate (Range2MapEntry item) { return new StartLength(item.y.start, item.y.length); });
            IndexedEnumerateAction<EntryRange2List>(collections, rnd, ref description, TreeKind.Range2List, xStartLengths, yStartLengths);
        }


        public override bool Do(int seed, StochasticControls control)
        {
            IRange2List[] collections = new IRange2List[]
            {
                new ReferenceRange2List(), // must be first

                new SplayTreeRange2List(),
                new SplayTreeArrayRange2List(),
                new AdaptRange2ListToRange2ListLong(new SplayTreeRange2ListLong()),

                new RedBlackTreeRange2List(),
                new RedBlackTreeArrayRange2List(),
                new AdaptRange2ListToRange2ListLong(new RedBlackTreeRange2ListLong()),

                new AVLTreeRange2List(),
                new AVLTreeArrayRange2List(),
                new AdaptRange2ListToRange2ListLong(new AVLTreeRange2ListLong()),
            };

            Tuple<Tuple<int, int>, InvokeAction<IRange2List>>[] actions = new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>[]
            {
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), ContainsAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(300 - 80 , 300 + 100), TryInsertAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(300      , 300      ), TryDeleteAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), TryGetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), TrySetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), TryGetAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(300 - 80 , 300 + 100), InsertAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(300      , 300      ), DeleteAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), GetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), SetLengthAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), GetAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(150      , 150      ), AdjustLengthAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(100      , 100      ), GetExtentAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(150      , 150      ), NearestLessOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(150      , 150      ), NearestLessAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(150      , 150      ), NearestGreaterOrEqualAction),
                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(150      , 150      ), NearestGreaterAction),

                new Tuple<Tuple<int, int>, InvokeAction<IRange2List>>(new Tuple<int, int>(75       , 75       ), EnumerateAction),
            };

            return StochasticDriver(
                "Range2 Map Stochastic Test",
                seed,
                control,
                collections,
                actions,
                delegate (IRange2List _collection) { return _collection.Count; },
                delegate (IRange2List[] _collections) { Validate(_collections); });
        }
    }
}
