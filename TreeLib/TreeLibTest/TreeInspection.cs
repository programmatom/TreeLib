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
using System.IO;
using System.Text;

using TreeLib;
using TreeLib.Internal;

namespace TreeLibTest
{
    public static class TreeInspection
    {
        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree,
            out int maxDepth,
            bool propagateValue) where KeyType : IComparable<KeyType>
        {
            List<EntryMap<KeyType, ValueType>> items = new List<EntryMap<KeyType, ValueType>>();

            maxDepth = 0;
            Stack<object> stack = new Stack<object>();
            object current = tree.Root;
            while (current != null)
            {
                stack.Push(current);
                current = tree.GetLeftChild(current);
            }
            maxDepth = stack.Count;
            while (stack.Count != 0)
            {
                current = stack.Pop();
                KeyType key = (KeyType)tree.GetKey(current);
                ValueType value = propagateValue ? (ValueType)tree.GetValue(current) : default(ValueType);
                items.Add(new EntryMap<KeyType, ValueType>(key, value, null, 0));

                object node = tree.GetRightChild(current);
                while (node != null)
                {
                    stack.Push(node);
                    node = tree.GetLeftChild(node);
                }
                maxDepth = Math.Max(maxDepth, stack.Count);
            }

            return items.ToArray();
        }

        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree,
            out int maxDepth) where KeyType : IComparable<KeyType>
        {
            return Flatten<KeyType, ValueType>(tree, out maxDepth, true/*propagateValue*/);
        }

        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree,
            bool propagateValue) where KeyType : IComparable<KeyType>
        {
            int maxDepth;
            return Flatten<KeyType, ValueType>(tree, out maxDepth, propagateValue);
        }

        public static EntryMap<KeyType, ValueType>[] Flatten<KeyType, ValueType>(
            INonInvasiveTreeInspection tree) where KeyType : IComparable<KeyType>
        {
            int maxDepth;
            return Flatten<KeyType, ValueType>(tree, out maxDepth, true/*propagateValue*/);
        }


        private static void Dump(INonInvasiveTreeInspection tree, object root, int level, TextWriter writer)
        {
            if (root == null)
            {
                writer.WriteLine("{0}<NULL>", new string(' ', 4 * level));
            }
            else
            {
                Dump(tree, tree.GetLeftChild(root), level + 1, writer);
                object key = tree.GetKey(root);
                object value = tree.GetValue(root);
                writer.WriteLine("{0}<{1},{2}>:{3}", new string(' ', 4 * level), key, value, tree.GetMetadata(root));
                Dump(tree, tree.GetRightChild(root), level + 1, writer);
            }
        }

        public static StringBuilder Dump(INonInvasiveTreeInspection tree)
        {
            StringBuilder sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb))
            {
                Dump(tree, tree.Root, 0, writer);
            }
            return sb;
        }
    }
}
