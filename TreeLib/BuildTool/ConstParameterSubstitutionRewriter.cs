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
    public class ConstParameterSubstitutionRewriter : CSharpSyntaxRewriter
    {
        private readonly FacetList featureFacetAxis;
        private readonly SemanticModel semanticModel;
        private const bool trace = false;

        // substitution done at syntactic level - so ensure template does not declare multiple fields with same identifier!
        private readonly Dictionary<ISymbol, ExpressionSyntax> substitutions = new Dictionary<ISymbol, ExpressionSyntax>();
        private readonly Dictionary<string, bool> substitutionNames = new Dictionary<string, bool>();

        public ConstParameterSubstitutionRewriter(FacetList[] facetAxes, SemanticModel semanticModel)
        {
            facetAxes = Array.FindAll(facetAxes, delegate (FacetList candidate) { return String.Equals(candidate.axisTag, "Feature"); });
            if (facetAxes.Length != 1)
            {
                throw new ArgumentException();
            }
            featureFacetAxis = facetAxes[0];

            this.semanticModel = semanticModel;
        }


        private SyntaxNode GetTraceParent(SyntaxNode node)
        {
            SyntaxNode parent = node;
            while (parent != null)
            {
                if ((parent is StatementSyntax) || (parent is MethodDeclarationSyntax))
                {
                    break;
                }
                parent = parent.Parent;
            }

            return parent != null ? parent : node;
        }


        // TODO: this is top-down propagation only - C# is ordering invariant and this really ought to be done in two passes,
        // first accumulate over entire tree, then apply over entire tree.


        // accumulate constant substitution mapping activated by current feature facet selection

        private readonly static string[] ConstAttributeAliases = new string[] { "Const", "Const2" };
        private bool TestConstSubstAttribute(SyntaxList<AttributeListSyntax> attributeLists, out ExpressionSyntax substConst)
        {
            return AttributeMatchUtil.TestEnumeratedFacetAttribute(attributeLists, out substConst, ConstAttributeAliases, featureFacetAxis);
        }

        private bool TestConstSubstSuppressionAttribute(SyntaxList<AttributeListSyntax> attributeLists)
        {
            foreach (AttributeListSyntax attributeList in attributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    IdentifierNameSyntax attributeName;
                    if ((attributeName = attribute.Name as IdentifierNameSyntax) != null)
                    {
                        if (String.Equals(attributeName.Identifier.Text, "SuppressConst"))
                        {
                            foreach (AttributeArgumentSyntax argument in attribute.ArgumentList.Arguments)
                            {
                                MemberAccessExpressionSyntax argEnumTag = (MemberAccessExpressionSyntax)argument.Expression;
                                if (Array.IndexOf(featureFacetAxis.facets, argEnumTag.Name.Identifier.Text) >= 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (VariableDeclaratorSyntax variable in node.Declaration.Variables)
            {
                ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(variable.Identifier.Span.Start, null, variable.Identifier.Text);
                if (symbols.Length != 1)
                {
                    Debug.Assert(false);
                    throw new ArgumentException(String.Format("Template contains none or multiple symbols in scope for \"{0}\" which ought to be impossible (but could happen due to template code errors)", variable.Identifier.Text));
                }
                ISymbol identifierSymbol = symbols[0];

                if (substitutions.ContainsKey(identifierSymbol))
                {
                    throw new ArgumentException(String.Format("template contains multiple field names \"{0}\"", identifierSymbol.Name));
                }

                ExpressionSyntax substConst;
                if (TestConstSubstAttribute(node.AttributeLists, out substConst) && !TestConstSubstSuppressionAttribute(node.AttributeLists))
                {
                    if (!substitutions.ContainsKey(identifierSymbol))
                    {
                        substitutions.Add(identifierSymbol, substConst);
                        substitutionNames[identifierSymbol.Name] = false;
#pragma warning disable CS0162 // unreachable
                        if (trace)
                        {
                            Console.WriteLine("ADDCONST {0} ==> {1}", identifierSymbol.Name, substConst);
                        }
#pragma warning restore CS0162
                    }
                    else
                    {
                        if (!substConst.IsEquivalentTo(substitutions[identifierSymbol]))
                        {
                            throw new ArgumentException(String.Format("Multiple ConstAttribute() instances on \"{0}\" declare different substitution values", identifierSymbol.Name));
                        }
                    }
                }
            }

            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            node = (ParameterSyntax)base.VisitParameter(node);

            int position = -1;
            if ((position < 0) && (node.Parent.Parent is BasePropertyDeclarationSyntax))
            {
                BasePropertyDeclarationSyntax pp = (BasePropertyDeclarationSyntax)node.Parent.Parent;
                foreach (AccessorDeclarationSyntax decl in pp.AccessorList.Accessors)
                {
                    if (decl.Body != null)
                    {
                        position = decl.Body.Span.Start;
                    }
                }
            }
            if ((position < 0) && (node.Parent.Parent is BaseMethodDeclarationSyntax))
            {
                BaseMethodDeclarationSyntax pp = (BaseMethodDeclarationSyntax)node.Parent.Parent;
                if (pp.Body != null)
                {
                    position = pp.Body.Span.Start;
                }
            }
            if (position < 0)
            {
                // no scope bodies for this formal parameter - no need to record for substitution
                return node;
            }

            ExpressionSyntax substConst;
            if (TestConstSubstAttribute(node.AttributeLists, out substConst) && !TestConstSubstSuppressionAttribute(node.AttributeLists))
            {
                ImmutableArray<ISymbol> symbols = semanticModel.LookupSymbols(position, null, node.Identifier.Text);
                if (symbols.Length != 1)
                {
                    Debug.Assert(false);
                    throw new ArgumentException(String.Format("Template contains none or multiple symbols in scope for \"{0}\" which ought to be impossible (but could happen due to template code errors)", node.Identifier.Text));
                }
                ISymbol identifierSymbol = symbols[0];

                if (substitutions.ContainsKey(identifierSymbol))
                {
                    throw new ArgumentException(String.Format("template contains multiple field names \"{0}\"", identifierSymbol.Name));
                }


                if (!substitutions.ContainsKey(identifierSymbol))
                {
                    substitutions.Add(identifierSymbol, substConst);
                    substitutionNames[identifierSymbol.Name] = false;
#pragma warning disable CS0162 // unreachable
                    if (trace)
                    {
                        Console.WriteLine("ADDCONST {0} ==> {1}", identifierSymbol.Name, substConst);
                    }
#pragma warning restore CS0162
                }
                else
                {
                    if (!substConst.IsEquivalentTo(substitutions[identifierSymbol]))
                    {
                        throw new ArgumentException(String.Format("Multiple ConstAttribute() instances on \"{0}\" declare different substitution values", identifierSymbol.Name));
                    }
                }
            }

            return node;
        }


        // apply constant substitution

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (substitutionNames.ContainsKey(node.Name.Identifier.Text)) // pre-test on name for performance
            {
                ISymbol identifierSymbol = semanticModel.GetSymbolInfo(node).Symbol;
                if (identifierSymbol != null)
                {
                    ExpressionSyntax constValue;
                    if (substitutions.TryGetValue(identifierSymbol, out constValue))
                    {
#pragma warning disable CS0162 // unreachable
                        if (trace)
                        {
                            Console.WriteLine("CONSTSUBST: {0}: {1} ==> {2}", GetTraceParent(node).ToFullString(), node.ToFullString(), constValue.ToFullString());
                        }
#pragma warning restore CS0162
                        return constValue.WithTriviaFrom(node);
                    }
                }
            }

            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            IdentifierNameSyntax rhsExpression = node.Right as IdentifierNameSyntax;
            if ((rhsExpression != null) && substitutionNames.ContainsKey(rhsExpression.Identifier.Text)) // pre-test on name for performance
            {
                ISymbol identifierSymbol = semanticModel.GetSymbolInfo(node.Right).Symbol;
                if (identifierSymbol != null)
                {
                    Debug.Assert(rhsExpression != null);

                    ExpressionSyntax constValue;
                    if (substitutions.TryGetValue(identifierSymbol, out constValue))
                    {
                        SyntaxNode newNode = node.Update(
                            node.Left,
                            node.OperatorToken,
                            constValue);
#pragma warning disable CS0162 // unreachable
                        if (trace)
                        {
                            Console.WriteLine("CONSTSUBST: {0}: {1} ==> {2}", GetTraceParent(node).ToFullString(), node.ToFullString(), newNode.ToFullString());
                        }
#pragma warning restore CS0162
                        return newNode;
                    }
                }
            }

            return base.VisitAssignmentExpression(node);
        }

        public override SyntaxNode VisitArgumentList(ArgumentListSyntax node)
        {
            // if we rewrite elements of the argumentlist as a result of VisitArgumentList(), the newly created symbols
            // might not be in the tree (throwing from semanticModel.GetSymbolInfo()), so use the original list to lookup.
            SyntaxNode originalNode = node;
            SeparatedSyntaxList<ArgumentSyntax> originalArguments = node.Arguments;
            node = (ArgumentListSyntax)base.VisitArgumentList(node);
            node = NormalizeArgumentListUtil.NormalizeArgumentList(node);

            bool changed = false;
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                ArgumentSyntax argument = node.Arguments[i];

                IdentifierNameSyntax nodeExpression;
                if ((nodeExpression = argument.Expression as IdentifierNameSyntax) != null)
                {
                    if (substitutionNames.ContainsKey(nodeExpression.Identifier.Text)) // pre-test on name for performance
                    {
                        ISymbol identifierSymbol = semanticModel.GetSymbolInfo(originalArguments[i]).Symbol;
                        if (identifierSymbol != null)
                        {
                            ExpressionSyntax constValue;
                            if (substitutions.TryGetValue(identifierSymbol, out constValue))
                            {
                                changed = true;
                                node = node.Update(
                                    node.OpenParenToken,
                                    node.Arguments.Replace(argument, SyntaxFactory.Argument(constValue).WithTriviaFrom(argument)),
                                    node.CloseParenToken);
                            }
                        }
                    }
                }
                i++;
            }

            if (trace && changed)
            {
                Console.WriteLine("CONSTSUBST: {0}: {1} ==> {2}", GetTraceParent(originalNode).ToFullString(), originalArguments.ToFullString(), node.ToFullString());
            }

            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (substitutionNames.ContainsKey(node.Identifier.Text)) // pre-rest on name for performance
            {
                ISymbol identifierSymbol = semanticModel.GetSymbolInfo(node).Symbol;
                if (identifierSymbol != null)
                {
                    ExpressionSyntax constValue;
                    if (substitutions.TryGetValue(identifierSymbol, out constValue))
                    {
#pragma warning disable CS0162 // unreachable
                        if (trace)
                        {
                            Console.WriteLine("CONSTSUBST: {0}: {1} ==> {2}", GetTraceParent(node).ToFullString(), node.ToFullString(), constValue.ToFullString());
                        }
#pragma warning restore CS0162
                        return constValue.WithTriviaFrom(node);
                    }
                }
            }

            return base.VisitIdentifierName(node);
        }
    }
}
