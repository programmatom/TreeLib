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
    public static class ArrayIndexingHelper
    {
        private const string ArrayIndexingAttributeName = "ArrayIndexing";

        public static bool HasArrayIndexingAttribute(SemanticModel semanticModel, SyntaxList<AttributeListSyntax> attributeLists, out ISymbol subst)
        {
            subst = null;
            foreach (AttributeListSyntax attributeList in attributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    IdentifierNameSyntax identifierName;
                    if ((identifierName = attribute.Name as IdentifierNameSyntax) != null)
                    {
                        if (String.Equals(identifierName.Identifier.Text, ArrayIndexingAttributeName))
                        {
                            if ((attribute.ArgumentList != null) && (attribute.ArgumentList.Arguments.Count != 0))
                            {
                                if (attribute.ArgumentList.Arguments.Count != 1)
                                {
                                    throw new ArgumentException();
                                }
                                AttributeArgumentSyntax argument = attribute.ArgumentList.Arguments[0];
                                Optional<object> o = semanticModel.GetConstantValue(argument.Expression);
                                string name = null;
                                if (!o.HasValue || ((name = o.Value as string) == null))
                                {
                                    throw new ArgumentException();
                                }

                                ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(attribute.Span.Start, null, name);
                                if (symbols.Length != 1)
                                {
                                    Debug.Assert(false);
                                    throw new ArgumentException(String.Format("Template contains none or multiple symbols in scope for \"{0}\" which ought to be impossible (but could happen due to template code errors)", name));
                                }
                                subst = symbols[0];
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public class CollectArrayIndirectionTypeSubstitution : CSharpSyntaxWalker
    {
        private readonly SemanticModel semanticModel;
        private readonly Dictionary<ISymbol, ISymbol> replace = new Dictionary<ISymbol, ISymbol>();

        public CollectArrayIndirectionTypeSubstitution(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public Dictionary<ISymbol, ISymbol> Replace { get { return replace; } }


        public override void Visit(SyntaxNode node)
        {
            base.Visit(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ISymbol subst;
            if (ArrayIndexingHelper.HasArrayIndexingAttribute(semanticModel, node.AttributeLists, out subst))
            {
                if (subst != null)
                {
                    ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(node.Identifier.Span.Start, null, node.Identifier.Text);
                    if (symbols.Length != 1)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException(String.Format("Template contains none or multiple symbols in scope for \"{0}\" which ought to be impossible (but could happen due to template code errors)", node.Identifier.Text));
                    }
                    ISymbol originalSymbol = symbols[0];

                    replace.Add(originalSymbol, subst);
                }
            }

            base.VisitClassDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            ISymbol subst;
            if (ArrayIndexingHelper.HasArrayIndexingAttribute(semanticModel, node.AttributeLists, out subst))
            {
                if (subst != null)
                {
                    ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(node.Identifier.Span.Start, null, node.Identifier.Text);
                    if (symbols.Length != 1)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException(String.Format("Template contains none or multiple symbols in scope for \"{0}\" which ought to be impossible (but could happen due to template code errors)", node.Identifier.Text));
                    }
                    ISymbol originalSymbol = symbols[0];

                    replace.Add(originalSymbol, subst);
                }
            }

            base.VisitStructDeclaration(node);
        }
    }

    public class EliminateArrayIndirectionRewriter : CSharpSyntaxRewriter
    {
        private readonly Stack<FieldHolder> fieldStack = new Stack<FieldHolder>();
        private readonly SemanticModel semanticModel;
        private readonly Dictionary<ISymbol, ISymbol> replace;

        public EliminateArrayIndirectionRewriter(SemanticModel semanticModel, Dictionary<ISymbol, ISymbol> replace)
        {
            this.semanticModel = semanticModel;
            this.replace = replace;
        }

        private class FieldHolder
        {
            public ISymbol Symbol { get; set; }
        }

        private ISymbol CurrentFieldSymbol
        {
            get
            {
                return fieldStack.Count != 0 ? fieldStack.Peek().Symbol : null;
            }
            set
            {
                if (fieldStack.Count == 0)
                {
                    throw new InvalidOperationException();
                }
                fieldStack.Peek().Symbol = value;
            }
        }

        private bool MatchesAnyFieldToSuppress(ExpressionSyntax arrayBase)
        {
            SymbolInfo symbol = semanticModel.GetSymbolInfo(arrayBase);
            foreach (FieldHolder fieldHolder in fieldStack)
            {
                ISymbol fieldSymbol = fieldHolder.Symbol;
                if (fieldSymbol != null)
                {
                    if (symbol.Symbol.Equals(fieldSymbol))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            ISymbol subst;
            if (ArrayIndexingHelper.HasArrayIndexingAttribute(semanticModel, node.AttributeLists, out subst))
            {
                return null;
            }

            fieldStack.Push(new FieldHolder());
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            fieldStack.Pop();

            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            ISymbol subst;
            if (ArrayIndexingHelper.HasArrayIndexingAttribute(semanticModel, node.AttributeLists, out subst))
            {
                return null;
            }

            fieldStack.Push(new FieldHolder());
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
            fieldStack.Pop();

            return node;
        }

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            ISymbol subst;
            if (ArrayIndexingHelper.HasArrayIndexingAttribute(semanticModel, node.AttributeLists, out subst))
            {
                return null;
            }

            return base.VisitIndexerDeclaration(node);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node);

            foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
            {
                ISymbol subst;
                if (ArrayIndexingHelper.HasArrayIndexingAttribute(semanticModel, node.AttributeLists, out subst))
                {
                    if (subst != null)
                    {
                        throw new ArgumentException("ArrayIndexing substution type not permitted on fields");
                    }

                    ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(variable.Identifier.Span.Start, null, variable.Identifier.Text);
                    if (symbols.Length != 1)
                    {
                        Debug.Assert(false);
                        throw new ArgumentException(String.Format("Template contains none or multiple symbols in scope for \"{0}\" which ought to be impossible (but could happen due to template code errors)", variable.Identifier.Text));
                    }
                    ISymbol identifierSymbol = symbols[0];

                    if (CurrentFieldSymbol != null)
                    {
                        Debug.Assert(false);
                        throw new InvalidOperationException();
                    }

                    CurrentFieldSymbol = identifierSymbol;

                    node = node.Update(
                        node.AttributeLists,
                        node.Modifiers,
                        node.Declaration.Update(
                            node.Declaration.Type,
                            node.Declaration.Variables.Remove(variable)),
                        node.SemicolonToken);
                    if (node.Declaration.Variables.Count == 0)
                    {
                        return null;
                    }
                }
            }

            return node;
        }

        public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            ElementAccessExpressionSyntax originalNode = node;
            node = (ElementAccessExpressionSyntax)base.VisitElementAccessExpression(node);

            if (node.Expression is IdentifierNameSyntax)
            {
                if (node.ArgumentList.Arguments.Count == 1)
                {
                    if (MatchesAnyFieldToSuppress(/*must use node that is in tree*/originalNode.Expression))
                    {
                        return node.ArgumentList.Arguments[0].Expression.WithTriviaFrom(node);
                    }
                }
            }
            else if (node.Expression is MemberAccessExpressionSyntax)
            {
                MemberAccessExpressionSyntax arrayBase = (MemberAccessExpressionSyntax)node.Expression;
                if (MatchesAnyFieldToSuppress(/*must use node that is in tree*/((MemberAccessExpressionSyntax)originalNode.Expression).Name))
                {
                    return node.ArgumentList.Arguments[0].Expression.WithTriviaFrom(node);
                }
            }

            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            ISymbol identifierSymbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (identifierSymbol != null)
            {
                ISymbol subst;
                if (replace.TryGetValue(identifierSymbol, out subst))
                {
                    return node.WithIdentifier(
                        SyntaxFactory.Identifier(
                            node.Identifier.LeadingTrivia,
                            subst.Name,
                            node.Identifier.TrailingTrivia));
                }
            }

            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            SymbolInfo symbol = semanticModel.GetSymbolInfo(node.Type);
            if (symbol.Symbol != null)
            {
                ISymbol subst;
                if (replace.TryGetValue(symbol.Symbol, out subst))
                {
                    if (node.ArgumentList.Arguments.Count != 1)
                    {
                        throw new ArgumentException();
                    }
                    return node.ArgumentList.Arguments[0].Expression.WithTriviaFrom(node);
                }
            }

            return base.VisitObjectCreationExpression(node);
        }
    }
}
