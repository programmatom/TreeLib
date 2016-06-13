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
    public enum Storage
    {
        Object,
        Array,
    };

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class StorageAttribute : Attribute
    {
        public StorageAttribute(params Storage[] facets)
        {
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class ArrayIndexingAttribute : Attribute
    {
        public ArrayIndexingAttribute()
        {
        }

        public ArrayIndexingAttribute(string substitution)
        {
        }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum Payload
    {
        Value,
        None,
    };

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class PayloadAttribute : Attribute
    {
        public PayloadAttribute(params Payload[] facets)
        {
        }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum Feature
    {
        Dict,
        Rank,
        RankMulti,
        Range,
        Range2,
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class FeatureAttribute : Attribute
    {
        public FeatureAttribute(params Feature[] facets)
        {
        }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class ConstAttribute : Attribute
    {
        public ConstAttribute(object value, params Feature[] facets)
        {
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class Const2Attribute : Attribute
    {
        public Const2Attribute(object value, params Feature[] facets)
        {
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class SuppressConstAttribute : Attribute
    {
        public SuppressConstAttribute(params Feature[] facets)
        {
        }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class CountAttribute : Attribute
    {
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class EnableFixedAttribute : Attribute
    {
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class WidenAttribute : Attribute
    {
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    public class RenameAttribute : Attribute
    {
        public RenameAttribute(string newName, params Feature[] facets)
        {
        }
    }
}
