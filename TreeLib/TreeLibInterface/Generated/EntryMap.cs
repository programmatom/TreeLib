// NOTE: This file is auto-generated. DO NOT MAKE CHANGES HERE! They will be overwritten on rebuild.

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

using TreeLib.Internal;

namespace TreeLib
{
    public struct EntryMap<[Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType, [Payload(Payload.Value)] ValueType>
    {
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        private readonly KeyType key;
        [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)]
        public KeyType Key { get { return key; } }

        [Payload(Payload.Value)]
        private ValueType value;
        [Payload(Payload.Value)]
        public ValueType Value { get { return value; } }

        public EntryMap(
            [Feature(Feature.Dict, Feature.Rank, Feature.RankMulti)] KeyType key,            [Payload(Payload.Value)] ValueType value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
