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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BuildTool
{
    public class RemoveEnumerableRewriter : CSharpSyntaxRewriter
    {
        private const string BaseInterfaceToRemove = "IEnumerable";

        public RemoveEnumerableRewriter()
        {
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            node = (InterfaceDeclarationSyntax)base.VisitInterfaceDeclaration(node);

            int i = 0;
            foreach (BaseTypeSyntax baseType in node.BaseList.Types)
            {
                if ((baseType.Type.IsKind(SyntaxKind.GenericName) && String.Equals(((GenericNameSyntax)baseType.Type).Identifier.Text, BaseInterfaceToRemove))
                    || ((baseType.Type.IsKind(SyntaxKind.IdentifierName) && String.Equals(((IdentifierNameSyntax)baseType.Type).Identifier.Text, BaseInterfaceToRemove))))
                {
                    node = node.WithBaseList(node.BaseList.WithTypes(node.BaseList.Types.RemoveAt(i)));
                    continue;
                }
                i++;
            }
            // Fails: without it, leaves behind dangling colon, but we're not reparsing, so it's tolerated
            //if (node.BaseList.Types.Count == 0)
            //{
            //    node = node.WithBaseList(null);
            //}

            return node;
        }
    }
}
