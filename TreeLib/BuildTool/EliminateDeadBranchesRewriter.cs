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


        private Optional<object> GetConstantValueSafe(SyntaxNode node)
        {
            try
            {
                return semanticModel.GetConstantValue(node);
            }
            catch (ArgumentException)
            {
                // If node is not in tree (because we've rewritten it, duh), GetConstantValue() throws.
                // Pretend it said "not const" and continue, since if we changed the tree we'll be back for another iteration
                Debug.Assert(Changed);
                return new Optional<object>();
            }
        }

        public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node)
        {
            Changed = true;
            return null;
        }

        public override SyntaxNode VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            ConditionalExpressionSyntax original = node;
            node = (ConditionalExpressionSyntax)base.VisitConditionalExpression(node);

            Optional<object> value = GetConstantValueSafe(original.Condition);
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
#pragma warning disable CS0162 // unreachable
                    if (trace)
                    {
                        Console.WriteLine("DEAD: {0} ==> {1}", node.ToFullString(), replacement.ToFullString());
                    }
#pragma warning restore CS0162
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

#pragma warning disable CS0162 // unreachable
                if (trace)
                {
                    Console.WriteLine("DEAD: {0} ==> {1}", original.ToFullString(), node.ToFullString());
                }
#pragma warning restore CS0162
            }

            if ((node.Else == null) && ((node.Statement is EmptyStatementSyntax)
                || ((node.Statement is BlockSyntax) && (((BlockSyntax)node.Statement).Statements.Count == 0))))
            {
                Changed = true;

                IfStatementSyntax original = node;
                node = null;

#pragma warning disable CS0162 // unreachable
                if (trace)
                {
                    Console.WriteLine("DEAD: {0} ==> {1}", original.ToFullString(), "<null>");
                }
#pragma warning restore CS0162
            }

            return node != null ? node : (SyntaxNode)SyntaxFactory.EmptyStatement();
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            ExpressionSyntax originalCondition = node.Condition;

            node = (IfStatementSyntax)base.VisitIfStatement(node);

            // remove double parens
            if (node.Condition.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                node = node.WithCondition(((ParenthesizedExpressionSyntax)node.Condition).Expression);
            }

            Optional<object> value = GetConstantValueSafe(originalCondition);
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

#pragma warning disable CS0162 // unreachable
                    if (trace)
                    {
                        Console.WriteLine("DEAD: {0} ==> {1}", node.ToFullString(), replacement != null ? replacement.ToFullString() : "<null>");
                    }
#pragma warning restore CS0162

                    if (replacement is IfStatementSyntax)
                    {
                        replacement = EliminateIfUseless((IfStatementSyntax)replacement);
                    }

                    return replacement;
                }
            }

            return EliminateIfUseless(node);
        }

        public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
        {
            if ((node.Block.Statements.Count == 0) && ((node.Finally == null) || (node.Finally.Block.Statements.Count == 0)))
            {
                return SyntaxFactory.EmptyStatement();
            }

            return base.VisitTryStatement(node);
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            node = (ExpressionStatementSyntax)base.VisitExpressionStatement(node);

            // remove useless asserts with 'true' constant after reduction
            if (node.Expression.IsKind(SyntaxKind.InvocationExpression))
            {
                InvocationExpressionSyntax call = (InvocationExpressionSyntax)node.Expression;

                if (call.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    MemberAccessExpressionSyntax callFunc = (MemberAccessExpressionSyntax)call.Expression;
                    if (callFunc.Expression.IsKind(SyntaxKind.IdentifierName))
                    {
                        if (String.Equals(((IdentifierNameSyntax)callFunc.Expression).Identifier.Text, "Debug")
                            && String.Equals(callFunc.Name.Identifier.Text, "Assert"))
                        {
                            if (call.ArgumentList.Arguments.Count >= 1)
                            {
                                Optional<object> value = GetConstantValueSafe(call.ArgumentList.Arguments[0].Expression);
                                if (value.HasValue && (value.Value is bool) && (bool)value.Value)
                                {
                                    Changed = true;
                                    return SyntaxFactory.EmptyStatement();
                                }
                            }
                        }
                    }
                }
            }


            return node;
        }

        // attempt simple equivalence detection (variables, enum constants, fields)
        // try to disallow anything with side effects
        private bool TestEquivalenceSimple(ExpressionSyntax left, ExpressionSyntax right)
        {
            if (left.Kind() != right.Kind())
            {
                return false;
            }

            if (left.IsKind(SyntaxKind.IdentifierName))
            {
                return String.Equals(((IdentifierNameSyntax)left).Identifier.Text, ((IdentifierNameSyntax)right).Identifier.Text);
            }

            if (left.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                MemberAccessExpressionSyntax maLeft = (MemberAccessExpressionSyntax)left;
                MemberAccessExpressionSyntax maRight = (MemberAccessExpressionSyntax)right;

                return TestEquivalenceSimple(maLeft.Name, maRight.Name)
                    && TestEquivalenceSimple(maLeft.Expression, maRight.Expression);
            }

            return false;
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            node = (BinaryExpressionSyntax)base.VisitBinaryExpression(node);

            if (node.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken))
            {
                if (TestEquivalenceSimple(node.Left, node.Right))
                {
                    Changed = true;
                    return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                }
            }
            else if (node.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken))
            {
                if (TestEquivalenceSimple(node.Left, node.Right))
                {
                    Changed = true;
                    return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                }
            }
            else if (node.OperatorToken.IsKind(SyntaxKind.BarBarToken))
            {
                Optional<object> left = GetConstantValueSafe(node.Left);
                if (left.HasValue && (left.Value is bool) && !(bool)left.Value)
                {
                    Changed = true;
                    return node.Right;
                }
                Optional<object> right = GetConstantValueSafe(node.Right);
                if (right.HasValue && (right.Value is bool) && !(bool)right.Value)
                {
                    Changed = true;
                    return node.Left;
                }
            }
            else if (node.OperatorToken.IsKind(SyntaxKind.AmpersandAmpersandToken))
            {
                Optional<object> left = GetConstantValueSafe(node.Left);
                if (left.HasValue && (left.Value is bool) && (bool)left.Value)
                {
                    Changed = true;
                    return node.Right;
                }
                Optional<object> right = GetConstantValueSafe(node.Right);
                if (right.HasValue && (right.Value is bool) && (bool)right.Value)
                {
                    Changed = true;
                    return node.Left;
                }
            }

            return node;
        }

        public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            node = (ParenthesizedExpressionSyntax)base.VisitParenthesizedExpression(node);

            // remove double parens (cosmetic)
            if (node.Expression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                Changed = true;
                return node.Expression;
            }

            return node;
        }

        public override SyntaxNode VisitArgument(ArgumentSyntax node)
        {
            node = (ArgumentSyntax)base.VisitArgument(node);

            // remove unnecessarily paren'd args (cosmetic)
            if (node.Expression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                Changed = true;
                node = node.WithExpression(((ParenthesizedExpressionSyntax)node.Expression).Expression);
            }

            return node;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            node = (BlockSyntax)base.VisitBlock(node);

            // remove nested statements (cosmetic)
            if ((node.Statements.Count == 1) && (node.Statements[0].IsKind(SyntaxKind.Block)))
            {
                Changed = true;
                node = (BlockSyntax)node.Statements[0];
            }

            return node;
        }

        public override SyntaxNode VisitCheckedStatement(CheckedStatementSyntax node)
        {
            node = (CheckedStatementSyntax)base.VisitCheckedStatement(node);

            if (node.Block.Statements.Count == 0)
            {
                Changed = true;
                return SyntaxFactory.EmptyStatement();
            }

            return node;
        }

#if false // TODO: not working - eliminating too aggressively
        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            SyntaxList<StatementSyntax> statements = node.Statements;

            // eliminate unreachable statements (cosmetic)
            List<int> remove = new List<int>();
            for (int i = 0; i < statements.Count; i++)
            {
                try
                {
                    ControlFlowAnalysis cfa = semanticModel.AnalyzeControlFlow(statements[i]);
                    if (!cfa.StartPointIsReachable)
                    {
                        remove.Add(i);
                    }
                }
                catch (ArgumentException)
                {
                    // probably node not in tree (because we rewrote it)
                    // do nothing and come back next time
                    Debug.Assert(Changed);
                    break;
                }
            }
            if (remove.Count != 0)
            {
                Changed = true;
                for (int i = remove.Count - 1; i >= 0; i--)
                {
                    statements = statements.RemoveAt(remove[i]);
                }
                node = node.WithStatements(statements);
            }

            return base.VisitBlock(node);
        }
#endif
    }
}
