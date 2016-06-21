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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace TreeLibTest
{
    public delegate OuterEntry ConvertEntry<OuterEntry, InnerEntry>(InnerEntry entry);

    public class AdaptEnumerator<OuterEntry, InnerEntry> : IEnumerator<OuterEntry>
    {
        private readonly IEnumerator<InnerEntry> inner;
        private readonly ConvertEntry<OuterEntry, InnerEntry> convert;

        public AdaptEnumerator(
            IEnumerator<InnerEntry> inner,
            ConvertEntry<OuterEntry, InnerEntry> convert)
        {
            this.inner = inner;
            this.convert = convert;
        }

        public OuterEntry Current { get { return convert(inner.Current); } }

        object IEnumerator.Current { get { return convert((InnerEntry)((IEnumerator)inner).Current); } }

        public void Dispose()
        {
            inner.Dispose();
        }

        public bool MoveNext()
        {
            return inner.MoveNext();
        }

        public void Reset()
        {
            inner.Reset();
        }
    }

    public class AdaptEnumeratorOld<OuterEntry, InnerEntry> : IEnumerator
    {
        private readonly IEnumerator inner;
        private readonly ConvertEntry<OuterEntry, InnerEntry> convert;

        public AdaptEnumeratorOld(
            IEnumerator inner,
            ConvertEntry<OuterEntry, InnerEntry> convert)
        {
            this.inner = inner;
            this.convert = convert;
        }

        public object Current { get { return convert((InnerEntry)inner.Current); } }

        public bool MoveNext()
        {
            return inner.MoveNext();
        }

        public void Reset()
        {
            inner.Reset();
        }
    }

    public class AdaptEnumerable<OuterEntry, InnerEntry> : IEnumerable<OuterEntry>
    {
        private readonly IEnumerable<InnerEntry> inner;
        private readonly ConvertEntry<OuterEntry, InnerEntry> convert;

        public AdaptEnumerable(IEnumerable<InnerEntry> inner, ConvertEntry<OuterEntry, InnerEntry> convert)
        {
            this.inner = inner;
            this.convert = convert;
        }

        public IEnumerator<OuterEntry> GetEnumerator()
        {
            return new AdaptEnumerator<OuterEntry, InnerEntry>(inner.GetEnumerator(), convert);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AdaptEnumeratorOld<OuterEntry, InnerEntry>(((IEnumerable)inner).GetEnumerator(), convert);
        }
    }
}
