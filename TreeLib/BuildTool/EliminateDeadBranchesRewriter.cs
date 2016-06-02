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
    public class EliminateDeadBranchesRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel semanticModel;
        private const bool trace = false;

        public EliminateDeadBranchesRewriter(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public bool Changed { get; private set; }


        public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node)
        {
            Changed = true;
            return null;
        }

        public override SyntaxNode VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            ConditionalExpressionSyntax original = node;
            node = (ConditionalExpressionSyntax)base.VisitConditionalExpression(node);

            Optional<object> value = semanticModel.GetConstantValue(original.Condition);
            if (value.HasValue)
            {
                if (value.Value is bool)
                {
                    SyntaxNode replacement;
                    if ((bool)value.Value)
                    {
                        Changed = true;
                        replacement = node.WhenTrue.WithTriviaFrom(node);
                    }
                    else
                    {
                        Changed = true;
                        replacement = node.WhenFalse.WithTriviaFrom(node);
                    }
                    if (trace)
                    {
                        Console.WriteLine("DEAD: {0} ==> {1}", node.ToFullString(), replacement.ToFullString());
                    }
                    return replacement;
                }
            }

            return node;
        }

        private SyntaxNode EliminateIfUseless(IfStatementSyntax node)
        {
            if ((node.Else != null) && ((node.Else.Statement is EmptyStatementSyntax)
                || ((node.Else.Statement is BlockSyntax) && (((BlockSyntax)node.Else.Statement).Statements.Count == 0))))
            {
                Changed = true;

                IfStatementSyntax original = node;
                node = node.WithElse(null);

                if (trace)
                {
                    Console.WriteLine("DEAD: {0} ==> {1}", original.ToFullString(), node.ToFullString());
                }
            }

            if ((node.Else == null) && ((node.Statement is EmptyStatementSyntax)
                || ((node.Statement is BlockSyntax) && (((BlockSyntax)node.Statement).Statements.Count == 0))))
            {
                Changed = true;

                IfStatementSyntax original = node;
                node = null;

                if (trace)
                {
                    Console.WriteLine("DEAD: {0} ==> {1}", original.ToFullString(), "<null>");
                }
            }

            return node != null ? node : (SyntaxNode)SyntaxFactory.EmptyStatement();
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            ExpressionSyntax originalCondition = node.Condition;

            node = (IfStatementSyntax)base.VisitIfStatement(node);

            Optional<object> value = semanticModel.GetConstantValue(originalCondition);
            if (value.HasValue)
            {
                if (value.Value is bool)
                {
                    SyntaxNode replacement;

                    if ((bool)value.Value)
                    {
                        Changed = true;
                        replacement = node.Statement.WithTriviaFrom(node);
                    }
                    else
                    {
                        Changed = true;
                        if (node.Else != null)
                        {
                            replacement = node.Else.Statement.WithTriviaFrom(node);
                        }
                        else
                        {
                            replacement = null;
                        }
                    }

                    if (trace)
                    {
                        Console.WriteLine("DEAD: {0} ==> {1}", node.ToFullString(), replacement != null ? replacement.ToFullString() : "<null>");
                    }

                    if (replacement is IfStatementSyntax)
                    {
                        replacement = EliminateIfUseless((IfStatementSyntax)replacement);
                    }

                    return replacement;
                }
            }

            return EliminateIfUseless(node);
        }
    }
}
