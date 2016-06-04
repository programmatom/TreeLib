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

#pragma warning disable CS1591

namespace TreeLib.Internal
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum Storage
    {
        Object,
        Array,
    };

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class StorageAttribute : Attribute
    {
        //private readonly Storage[] facets;

        public StorageAttribute(params Storage[] facets)
        {
            //this.facets = facets;
        }

        //public Storage[] Facets { get { return facets; } }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ArrayIndexingAttribute : Attribute
    {
        //private readonly string substitution;

        public ArrayIndexingAttribute()
        {
        }

        public ArrayIndexingAttribute(string substitution)
        {
            //this.substitution = substitution;
        }

        //public string Substitution { get { return substitution; } }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum Payload
    {
        Value,
        None,
    };

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class PayloadAttribute : Attribute
    {
        //private readonly Payload[] facets;

        public PayloadAttribute(params Payload[] facets)
        {
            //this.facets = facets;
        }

        //public Payload[] Facets { get { return facets; } }
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
    public class FeatureAttribute : Attribute
    {
        //private readonly Feature[] facets;

        public FeatureAttribute(params Feature[] facets)
        {
            //this.facets = facets;
        }

        //public Feature[] Facets { get { return facets; } }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ConstAttribute : Attribute
    {
        //private readonly Feature[] facets;
        //private readonly object value;

        public ConstAttribute(object value, params Feature[] facets)
        {
            //this.facets = facets;
            //this.value = value;
        }

        //public Feature[] Facents { get { return facets; } }
        //public object Value { get { return value; } }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class Const2Attribute : Attribute
    {
        //private readonly Feature[] facets;
        //private readonly object value;

        public Const2Attribute(object value, params Feature[] facets)
        {
            //this.facets = facets;
            //this.value = value;
        }

        //public Feature[] Facents { get { return facets; } }
        //public object Value { get { return value; } }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class SuppressConstAttribute : Attribute
    {
        //private readonly Feature[] facets;

        public SuppressConstAttribute(params Feature[] facets)
        {
            //this.facets = facets;
        }

        //public Feature[] Facets { get { return facets; } }
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class EnableFixedAttribute : Attribute
    {
    }


    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class CountAttribute : Attribute
    {
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class WidenAttribute : Attribute
    {
    }
}
