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
    // Directive trivia such as #if DEBUG and #endif are associated with the following declaration/statement. It makes
    // more sense to associate #endif with the preceding syntax item, otherwise if that item is removed it leaves a
    // dangling #endif
    public class ReassociateDirectiveTriviaRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            for (int i = 0; i < node.Members.Count - 1; i++)
            {
                while (true)
                {
                    int index = node.Members[i + 1].GetLeadingTrivia().IndexOf(SyntaxKind.EndIfDirectiveTrivia);
                    if (index < 0)
                    {
                        break;
                    }
                    node = node.WithMembers(
                        node.Members.RemoveAt(i).Insert(
                            i,
                            node.Members[i].WithTrailingTrivia(
                                node.Members[i].GetTrailingTrivia().Add(node.Members[i + 1].GetLeadingTrivia()[index]))));
                    node = node.WithMembers(
                        node.Members.RemoveAt(i + 1).Insert(
                            i + 1,
                            node.Members[i + 1].WithLeadingTrivia(
                                node.Members[i + 1].GetLeadingTrivia().RemoveAt(index))));
                }
            }

            return node;
        }

        // INCOMPLETE - add more if needed
    }
}
