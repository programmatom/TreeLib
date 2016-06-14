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
    public class CollectUnusedAndConstVariablesWalker : CSharpSyntaxWalker
    {
        private readonly Dictionary<ISymbol, ExpressionSyntax> constVariables = new Dictionary<ISymbol, ExpressionSyntax>();
        private readonly Dictionary<ISymbol, bool> unusedVariables = new Dictionary<ISymbol, bool>();
        private readonly SemanticModel semanticModel;

        public CollectUnusedAndConstVariablesWalker(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public Dictionary<ISymbol, ExpressionSyntax> ConstVariables { get { return constVariables; } }
        public Dictionary<ISymbol, bool> UnusedVariables { get { return unusedVariables; } }


        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (!node.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
                {
                    ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(node.SemicolonToken.Span.End, null, variable.Identifier.Text);
                    if (symbols.Count() != 1)
                    {
                        throw new InvalidOperationException();
                    }


                    //
                    // Const
                    //

                    if ((variable.Initializer != null) && semanticModel.GetConstantValue(variable.Initializer.Value).HasValue)
                    {
                        constVariables.Add(symbols[0], variable.Initializer.Value);
                    }


                    //
                    // Unused
                    //

                    unusedVariables.Add(symbols[0], false);


                    //
                    // Shared
                    //

                    // visit expressions in case there are references in them
                    Visit(variable.Initializer);
                    if (variable.ArgumentList != null)
                    {
                        foreach (ArgumentSyntax bracketArgument in variable.ArgumentList.Arguments)
                        {
                            Visit(bracketArgument);
                        }
                    }
                }
            }

            // don't visit using base - since VisitIdentifierName() would get invoked on the identifier
            // base.VisitLocalDeclarationStatement(node);
        }

        private readonly static SyntaxKind[] AllModifyingExpressionKinds = new SyntaxKind[]
        {
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.AddAssignmentExpression,
            SyntaxKind.SubtractAssignmentExpression,
            SyntaxKind.MultiplyAssignmentExpression,
            SyntaxKind.DivideAssignmentExpression,
            SyntaxKind.ModuloAssignmentExpression,
            SyntaxKind.AndAssignmentExpression,
            SyntaxKind.ExclusiveOrAssignmentExpression,
            SyntaxKind.OrAssignmentExpression,
            SyntaxKind.LeftShiftAssignmentExpression,
            SyntaxKind.RightShiftAssignmentExpression,

            SyntaxKind.PreIncrementExpression,
            SyntaxKind.PreDecrementExpression,
            SyntaxKind.PostIncrementExpression,
            SyntaxKind.PostDecrementExpression,
        };

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            SymbolInfo info = semanticModel.GetSymbolInfo(node);


            //
            // Const
            //

            if (info.Symbol != null)
            {
                bool remove = false;

                remove = remove || (node.Parent.IsKind(SyntaxKind.Argument)
                    && (((ArgumentSyntax)node.Parent).RefOrOutKeyword != null));

                remove = remove || (Array.IndexOf(AllModifyingExpressionKinds, node.Parent.Kind()) >= 0);

                if (remove)
                {
                    constVariables.Remove(info.Symbol);
                }
            }


            //
            // Unused
            //

            if (info.Symbol != null)
            {
                if (unusedVariables.ContainsKey(info.Symbol))
                {
                    unusedVariables.Remove(info.Symbol);
                }
            }


            base.VisitIdentifierName(node);
        }
    }

    public class RemoveUnusedVariablesRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel semanticModel;
        private readonly Dictionary<ISymbol, bool> unusedVariables;
        private readonly Dictionary<string, bool> unusedVariableNames = new Dictionary<string, bool>();
        private readonly Dictionary<ISymbol, ExpressionSyntax> constVariables;
        private readonly Dictionary<string, bool> constVariableNames = new Dictionary<string, bool>();
        private bool changed;

        public RemoveUnusedVariablesRewriter(
            SemanticModel semanticModel,
            Dictionary<ISymbol, bool> unusedVariables,
            Dictionary<ISymbol, ExpressionSyntax> constVariables)
        {
            this.semanticModel = semanticModel;

            this.unusedVariables = unusedVariables;
            foreach (KeyValuePair<ISymbol, bool> unusedVariable in unusedVariables)
            {
                unusedVariableNames[unusedVariable.Key.Name] = false;
            }

            this.constVariables = constVariables;
            foreach (KeyValuePair<ISymbol, ExpressionSyntax> constVariable in constVariables)
            {
                constVariableNames[constVariable.Key.Name] = false;
            }
        }

        public bool Changed { get { return changed; } }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            LocalDeclarationStatementSyntax original = node;
            node = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node);

            SeparatedSyntaxList<VariableDeclaratorSyntax> originalVariables = node.Declaration.Variables;
            int i = 0;
            foreach (VariableDeclaratorSyntax variable in originalVariables)
            {
                if (unusedVariableNames.ContainsKey(variable.Identifier.Text)) // pre-test on name for performance
                {
                    ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(original.SemicolonToken.Span.End, null, variable.Identifier.Text);
                    if (symbols.Count() != 1)
                    {
                        throw new InvalidOperationException();
                    }
                    if (unusedVariables.ContainsKey(symbols[0]))
                    {
                        changed = true;
                        node = node.WithDeclaration(
                            node.Declaration.WithVariables(
                                node.Declaration.Variables.RemoveAt(i)));
                        continue;
                    }
                }
                i++;
            }

            if (node.Declaration.Variables.Count == 0)
            {
                return SyntaxFactory.EmptyStatement();
            }

            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (constVariableNames.ContainsKey(node.Identifier.Text)) // pre-test on name for performance
            {
                ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(node.Span.End, null, node.Identifier.Text);
                if (symbols.Count() == 1)
                {
                    ExpressionSyntax substitution;
                    if (constVariables.TryGetValue(symbols[0], out substitution))
                    {
                        changed = true;
                        return substitution.WithTriviaFrom(node);
                    }
                }
            }

            return base.VisitIdentifierName(node);
        }
    }
}
