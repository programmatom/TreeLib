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
    public class RemoveBrokenStatementsRewriter : CSharpSyntaxRewriter
    {
        private readonly List<Diagnostic> errors;
        private const bool trace = false;

        public RemoveBrokenStatementsRewriter(List<Diagnostic> errors)
        {
            this.errors = errors;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            Location loc = node.GetLocation();
            bool suppress = false;
            bool wroteHeader = false;
            foreach (Diagnostic error in errors)
            {
                if ((loc.SourceSpan.Start <= error.Location.SourceSpan.Start)
                    && (loc.SourceSpan.End >= error.Location.SourceSpan.End))
                {
                    if (trace)
                    {
                        if (!wroteHeader)
                        {
                            Console.WriteLine("REMOVING BROKEN: {0}", node.ToFullString().Replace(Environment.NewLine, " "));
                            wroteHeader = true;
                        }
                        Console.WriteLine("  ERROR: {0}: {1}", error.Id, error.GetMessage());
                    }
                    suppress = true;
                    break;
                }
            }
            SyntaxNode updated = base.VisitExpressionStatement(node);
            if (suppress)
            {
                updated = SyntaxFactory.EmptyStatement(
                    SyntaxFactory.Token(
                        node.GetLeadingTrivia(),
                        node.SemicolonToken.Kind(),
                        node.GetTrailingTrivia()));
            }
            suppress = false;
            return updated;
        }

        private bool RemoveArgument(ArgumentSyntax argument)
        {
            foreach (SyntaxTrivia trivium in argument.GetLeadingTrivia())
            {
                if (trivium.Kind() == SyntaxKind.SingleLineCommentTrivia)
                {
                    // not doing this for now - requiring explicit annotation of arguments
                }
            }
            return false;
        }

        public override SyntaxNode VisitArgumentList(ArgumentListSyntax node)
        {
            node = (ArgumentListSyntax)base.VisitArgumentList(node);
            SeparatedSyntaxList<ArgumentSyntax> updated = node.Arguments;
            int i = 0;
            foreach (ArgumentSyntax argument in node.Arguments)
            {
                if (RemoveArgument(argument))
                {
                    updated = updated.RemoveAt(i);
                    continue;
                }
                i++;
            }
            return node.Update(
                node.OpenParenToken,
                updated,
                node.CloseParenToken);
        }
    }
}
