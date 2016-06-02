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
    public class CollectUnusedVariablesVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel semanticModel;
        private readonly Dictionary<ISymbol, bool> variables = new Dictionary<ISymbol, bool>();

        public CollectUnusedVariablesVisitor(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public Dictionary<ISymbol, bool> Variables { get { return variables; } }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            SymbolInfo info = semanticModel.GetSymbolInfo(node);
            if (info.Symbol != null)
            {
                if (variables.ContainsKey(info.Symbol))
                {
                    variables.Remove(info.Symbol);
                }
            }
            base.VisitIdentifierName(node);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
            {
                ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(node.SemicolonToken.Span.End, null, variable.Identifier.Text);
                if (symbols.Count() != 1)
                {
                    throw new InvalidOperationException();
                }
                variables.Add(symbols[0], false);

                // visit expressions in case there are references in them
                Visit(variable.Initializer);
                if (variable.ArgumentList != null)
                {
                    foreach (ArgumentSyntax bracketArgument in variable.ArgumentList.Arguments)
                    {
                        Visit(bracketArgument);
                    }
                }
                //this.VisitInitializerExpression((InitializerExpressionSyntax)variable.Initializer);
            }

            // DON'T visit using base - since VisitIdentifierName() would get invoked on the identifier
            // base.VisitLocalDeclarationStatement(node);
        }
    }

    public class RemoveUnusedVariablesRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel semanticModel;
        private readonly Dictionary<ISymbol, bool> variables;
        private bool changed;

        public RemoveUnusedVariablesRewriter(SemanticModel semanticModel, Dictionary<ISymbol, bool> variables)
        {
            this.semanticModel = semanticModel;
            this.variables = variables;
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
                ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(original.SemicolonToken.Span.End, null, variable.Identifier.Text);
                if (symbols.Count() != 1)
                {
                    throw new InvalidOperationException();
                }
                if (variables.ContainsKey(symbols[0]))
                {
                    node = node.WithDeclaration(
                        node.Declaration.WithVariables(
                            node.Declaration.Variables.RemoveAt(i)));
                    changed = true;
                    continue;
                }
                i++;
            }

            if (node.Declaration.Variables.Count == 0)
            {
                return SyntaxFactory.EmptyStatement();
            }

            return node;
        }
    }
}
