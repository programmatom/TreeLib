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
    public interface IHugeListValidation
    {
        void Validate();
        void Validate(out string dump);
        string Metadata { get; }
    }

    [ExcludeFromCodeCoverage]
    public class InvalidHugeListException : Exception
    {
        public InvalidHugeListException(string message)
            : base(message)
        {
        }
    }
}
