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

#pragma warning disable CS1591 // silence warning about missing Xml documentation

namespace TreeLib.Internal
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum CompareKeyMode { Key, Position };

    //
    // While waiting for C# 7, we'll provide our own struct-STuple. (The built-in STuple is a class, which results in
    // unnecessary heap allocations)
    //

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct STuple<ItemType1>
    {
        public readonly ItemType1 Item1;

        public STuple(ItemType1 item1)
        {
            this.Item1 = item1;
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct STuple<ItemType1, ItemType2>
    {
        public readonly ItemType1 Item1;
        public readonly ItemType2 Item2;

        public STuple(ItemType1 item1, ItemType2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct STuple<ItemType1, ItemType2, ItemType3>
    {
        public readonly ItemType1 Item1;
        public readonly ItemType2 Item2;
        public readonly ItemType3 Item3;

        public STuple(ItemType1 item1, ItemType2 item2, ItemType3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct STuple<ItemType1, ItemType2, ItemType3, ItemType4>
    {
        public readonly ItemType1 Item1;
        public readonly ItemType2 Item2;
        public readonly ItemType3 Item3;
        public readonly ItemType4 Item4;

        public STuple(ItemType1 item1, ItemType2 item2, ItemType3 item3, ItemType4 item4)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
        }
    }


    public class ExcludeFromCodeCoverageAttribute : Attribute
    {
        public ExcludeFromCodeCoverageAttribute()
        {
        }
    }
}
