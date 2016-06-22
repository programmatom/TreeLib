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
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BuildTool
{
    public class SelectFacetRewriter : CSharpSyntaxRewriter
    {
        private readonly FacetList[] facetAxes;

        private string currentMethod; // for debugging

        public SelectFacetRewriter(FacetList[] facetAxes)
        {
            this.facetAxes = facetAxes;
        }


        private bool RemoveTestAttributes(SyntaxList<AttributeListSyntax> attributeLists)
        {
            return AttributeMatchUtil.TestAttributes(attributeLists, facetAxes) == AttributeMatchUtil.FindResult.Exclude;
        }

        private bool RemoveType(BaseTypeDeclarationSyntax type)
        {
            return RemoveTestAttributes(type.AttributeLists);
        }

        private bool RemoveField(BaseFieldDeclarationSyntax field)
        {
            return RemoveTestAttributes(field.AttributeLists);
        }

        private bool RemoveMethod(BaseMethodDeclarationSyntax method)
        {
            return RemoveTestAttributes(method.AttributeLists);
        }

        private bool RemoveProperty(BasePropertyDeclarationSyntax property)
        {
            return RemoveTestAttributes(property.AttributeLists);
        }

        private bool RemoveParameter(ParameterSyntax parameter)
        {
            return RemoveTestAttributes(parameter.AttributeLists);
        }

        // Used for statements - where real attributes are not permitted, a fake attribute in a C-style comment
        // can be specified.
        private bool RemoveTestTriviaAnnotation(IEnumerable<SyntaxTrivia> trivia)
        {
            return !AttributeMatchUtil.TestTriviaAnnotation(trivia, facetAxes);
        }

        private bool RemoveBaseType(SimpleBaseTypeSyntax baseType)
        {
            return !AttributeMatchUtil.TestTriviaAnnotation(baseType.GetLeadingTrivia(), facetAxes);
        }

        private static readonly string[] RenameAttributes = new string[] { "Rename" };
        private bool Rename(SyntaxList<AttributeListSyntax> attributeLists, out ExpressionSyntax newNameStringOut)
        {
            foreach (FacetList facetsList in facetAxes)
            {
                if (AttributeMatchUtil.TestEnumeratedFacetAttribute(
                    attributeLists,
                    out newNameStringOut,
                    RenameAttributes,
                    facetsList))
                {
                    return true;
                }
            }
            newNameStringOut = null;
            return false;
        }


        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (RemoveType(node))
            {
                return null;
            }
            node = RewriteClassDeclarationBaseTypes(node);
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (RemoveType(node))
            {
                return null;
            }
            return base.VisitStructDeclaration(node);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (RemoveField(node))
            {
                return null;
            }
            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (RemoveMethod(node))
            {
                return null;
            }
            return base.VisitConstructorDeclaration(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string currentMethodOld = currentMethod;
            currentMethod = node.Identifier.Text;
            try
            {
                if (RemoveMethod(node))
                {
                    return null;
                }
                ExpressionSyntax newNameString;
                if (Rename(node.AttributeLists, out newNameString))
                {
                    string newName = ((LiteralExpressionSyntax)newNameString).Token.Text;
                    if (newName.StartsWith("\"") && newName.EndsWith("\""))
                    {
                        newName = newName.Substring(1, newName.Length - 2);
                    }
                    node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
                }
                return base.VisitMethodDeclaration(node);
            }
            finally
            {
                currentMethod = currentMethodOld;
            }
        }

        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            string currentMethodOld = currentMethod;
            currentMethod = node.ToString();
            try
            {
                if (RemoveMethod(node))
                {
                    return null;
                }
                return base.VisitOperatorDeclaration(node);
            }
            finally
            {
                currentMethod = currentMethodOld;
            }
        }

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            string currentMethodOld = currentMethod;
            currentMethod = node.ToString();
            try
            {
                if (RemoveProperty(node))
                {
                    return null;
                }
                return base.VisitIndexerDeclaration(node);
            }
            finally
            {
                currentMethod = currentMethodOld;
            }
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (RemoveProperty(node))
            {
                return null;
            }
            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitParameterList(ParameterListSyntax node)
        {
            node = NormalizeSeparatedListTrivia.NormalizeParameterList(node);

            ParameterListSyntax originalNode = node;
            //node = (ParameterListSyntax)base.VisitParameterList(node); can't - see below

            int i = 0;
            foreach (ParameterSyntax parameter in originalNode.Parameters)
            {
                if (RemoveParameter(parameter))
                {
                    node = node.Update(
                        node.OpenParenToken,
                        node.Parameters.RemoveAt(i),
                        node.CloseParenToken);
                    continue;
                }
                // Can't use base.VisitParameterList(node) to inner-process all parameters because some that are destined
                // to be stripped may cause processing errors (e.g. because they contain a generic type reference to a type
                // parameter that is being eliminated). Therefore, do the work ourselves using base.VisitParameter(parameter)
                // on each.
                node = node.Update(
                    node.OpenParenToken,
                    node.Parameters.RemoveAt(i).Insert(i, (ParameterSyntax)base.VisitParameter(parameter).WithTriviaFrom(node.Parameters[i])),
                    node.CloseParenToken);
                i++;
            }

            return node;
        }

        public override SyntaxNode VisitArgumentList(ArgumentListSyntax node)
        {
            node = (ArgumentListSyntax)base.VisitArgumentList(node);
            ArgumentListSyntax original = node;

            node = NormalizeSeparatedListTrivia.NormalizeArgumentList(node);

            bool didSomething = false;
            SeparatedSyntaxList<ArgumentSyntax> arguments = node.Arguments;
            for (int j = 0, i = 0; j < arguments.Count; j++)
            {
                SyntaxTriviaList leading = arguments[j].GetLeadingTrivia();
                if (RemoveTestTriviaAnnotation(leading))
                {
                    node = node.Update(
                        node.OpenParenToken,
                        node.Arguments.RemoveAt(i),
                        node.CloseParenToken);
                    didSomething = true;
                    continue;
                }
                i++;
            }

            return didSomething ? node : original;
        }


        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (RemoveTestTriviaAnnotation(node.GetLeadingTrivia()))
            {
                return null;
            }
            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode VisitForStatement(ForStatementSyntax node)
        {
            if (RemoveTestTriviaAnnotation(node.GetLeadingTrivia()))
            {
                return null;
            }
            return base.VisitForStatement(node);
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            if (RemoveTestTriviaAnnotation(node.GetLeadingTrivia()))
            {
                return null;
            }
            return base.VisitIfStatement(node);
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            if (RemoveTestTriviaAnnotation(node.GetLeadingTrivia()))
            {
                return null;
            }
            return base.VisitExpressionStatement(node);
        }

        public override SyntaxNode VisitTryStatement(TryStatementSyntax node)
        {
            if (RemoveTestTriviaAnnotation(node.GetLeadingTrivia()))
            {
                return null;
            }
            return base.VisitTryStatement(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            if (RemoveTestTriviaAnnotation(node.GetLeadingTrivia()))
            {
                return null;
            }
            return base.VisitBlock(node);
        }

        public override SyntaxNode VisitBreakStatement(BreakStatementSyntax node)
        {
            if (RemoveTestTriviaAnnotation(node.GetLeadingTrivia()))
            {
                return null;
            }
            return base.VisitBreakStatement(node);
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            node = (IdentifierNameSyntax)base.VisitIdentifierName(node);

            ExpressionSyntax newNameString;
            if (Rename(AttributeMatchUtil.TriviaAnnotationToAttributesList(node.GetLeadingTrivia()), out newNameString))
            {
                string newName = ((LiteralExpressionSyntax)newNameString).Token.Text;
                if (newName.StartsWith("\"") && newName.EndsWith("\""))
                {
                    newName = newName.Substring(1, newName.Length - 2);
                }
                node = node.WithIdentifier(SyntaxFactory.Identifier(newName));
            }

            return node;
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.OperatorToken.TrailingTrivia.Count != 0)
            {
                SyntaxTriviaList trivia = node.Name.GetLeadingTrivia();
                trivia = trivia.AddRange(node.OperatorToken.TrailingTrivia);
                node = node.WithName(node.Name.WithLeadingTrivia(trivia));
                node = node.WithOperatorToken(node.OperatorToken.WithTrailingTrivia());
            }

            return base.VisitMemberAccessExpression(node);
        }


        // TODO: this is a syntactic rather than semantic removal, so other symbols that are synonyms will be erroneously stripped
        private readonly List<string> suppressedTypeParameters = new List<string>();

        public override SyntaxNode VisitTypeParameterList(TypeParameterListSyntax node)
        {
            node = (TypeParameterListSyntax)base.VisitTypeParameterList(node);

            int i = 0;
            foreach (TypeParameterSyntax parameter in node.Parameters)
            {
                if (RemoveTestAttributes(parameter.AttributeLists))
                {
                    suppressedTypeParameters.Add(parameter.Identifier.Text);
                    node = node.Update(
                        node.LessThanToken,
                        node.Parameters.RemoveAt(i),
                        node.GreaterThanToken);
                    continue;
                }
                i++;
            }

            if (node.Parameters.Count == 0)
            {
                return null;
            }

            return node;
        }

        public override SyntaxNode VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node)
        {
            if (suppressedTypeParameters.Contains(node.Name.Identifier.Text))
            {
                return null;
            }

            return base.VisitTypeParameterConstraintClause(node);
        }

        // This doens't work to remove unwanted base classes/interfaces
        //public override SyntaxNode VisitSimpleBaseType(SimpleBaseTypeSyntax node)
        //{
        //    node = (SimpleBaseTypeSyntax)base.VisitSimpleBaseType(node);
        //
        //    if (RemoveBaseType(node))
        //    {
        //        return null;
        //    }
        //
        //    return node;
        //}
        //
        // So... doing it the hard way instead (called from VisitClassDeclaration):
        private ClassDeclarationSyntax RewriteClassDeclarationBaseTypes(ClassDeclarationSyntax node)
        {
            if (node.BaseList != null)
            {
                // comments preceding base type declaration are attributed to the correct base type declaration AS LONG AS
                // each type is listed on a separate source line.
                int i = 0;
                foreach (BaseTypeSyntax baseType1 in node.BaseList.Types)
                {
                    SimpleBaseTypeSyntax baseType;
                    if ((baseType = baseType1 as SimpleBaseTypeSyntax) != null)
                    {
                        //foreach (var x in baseType.GetLeadingTrivia())
                        //{
                        //    Console.WriteLine(x.ToFullString());
                        //}
                        //Console.WriteLine(baseType.ToString());
                        //Console.WriteLine();

                        if (RemoveBaseType(baseType))
                        {
                            node = node.WithBaseList(
                                node.BaseList.WithTypes(
                                    node.BaseList.Types.RemoveAt(i)));
                            continue;
                        }
                    }
                    i++;
                }
            }

            return node;
        }

        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            node = (TypeArgumentListSyntax)base.VisitTypeArgumentList(node);
            TypeArgumentListSyntax original = node;

            node = NormalizeSeparatedListTrivia.NormalizeTypeArgumentList(node);

            bool didSomething = false;
            SeparatedSyntaxList<TypeSyntax> arguments = node.Arguments;
            for (int j = 0, i = 0; j < arguments.Count; j++)
            {
                SyntaxTriviaList leading = arguments[j].GetLeadingTrivia();
                IdentifierNameSyntax typeName = arguments[j] as IdentifierNameSyntax;
                if (RemoveTestTriviaAnnotation(leading)
                    || ((typeName != null) && suppressedTypeParameters.Contains(typeName.Identifier.Text)))
                {
                    node = node.Update(
                        node.LessThanToken,
                        node.Arguments.RemoveAt(i),
                        node.GreaterThanToken);
                    didSomething = true;
                    continue;
                }
                i++;
            }

            if (node.Arguments.Count == 0)
            {
                didSomething = true;
                node = null;
            }

            return didSomething ? node : original;
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            try
            {
                node = (GenericNameSyntax)base.VisitGenericName(node);
            }
            catch (ArgumentException)
            {
                // Removing all of the type arguments from a GenericName isn't permitted. In that case we need to rewrite
                // the type as a simple type reference.
                return SyntaxFactory.IdentifierName(node.Identifier).WithTriviaFrom(node);
            }
            return node;
        }


        // INCOMPLETE - add others as needed
    }
}
