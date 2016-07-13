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
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BuildTool
{
    public class WidenIntRewriter : CSharpSyntaxRewriter
    {
        private const string WidenAttributeName = "Widen";
        private const string LongAppendText = "Long";


        public WidenIntRewriter()
        {
        }


        //
        // Primary wideners
        //

        private static TypeSyntax WidenType(TypeSyntax type)
        {
            switch (type.Kind())
            {
                default:
                    throw new ArgumentException();

                case SyntaxKind.PredefinedType:
                    if (((PredefinedTypeSyntax)type).Keyword.Kind() == SyntaxKind.IntKeyword)
                    {
                        type = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword)).WithTrailingTrivia(SyntaxFactory.Space);
                    }
                    else if (((PredefinedTypeSyntax)type).Keyword.Kind() == SyntaxKind.UIntKeyword)
                    {
                        type = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword)).WithTrailingTrivia(SyntaxFactory.Space);
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                    break;

                case SyntaxKind.GenericName:
                    GenericNameSyntax genericType = (GenericNameSyntax)type;
                    type = genericType.WithIdentifier(
                        SyntaxFactory.Identifier(genericType.Identifier.Text + LongAppendText)
                            .WithTriviaFrom(genericType.Identifier));
                    break;

                case SyntaxKind.IdentifierName:
                    IdentifierNameSyntax identifierType = (IdentifierNameSyntax)type;
                    type = identifierType.WithIdentifier(
                        SyntaxFactory.Identifier(identifierType.Identifier.Text + LongAppendText)
                            .WithTriviaFrom(identifierType.Identifier));
                    break;

                case SyntaxKind.ArrayType:
                    ArrayTypeSyntax arrayType = (ArrayTypeSyntax)type;
                    type = arrayType.WithElementType(WidenType(arrayType.ElementType));
                    break;
            }

            return type;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (AttributeMatchUtil.HasTriviaAnnotationSimple(node.GetLeadingTrivia(), WidenAttributeName))
            {
                node = (IdentifierNameSyntax)WidenType(node);
            }

            node = (IdentifierNameSyntax)base.VisitIdentifierName(node);
            return node;
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            node = node.WithTypeArgumentList((TypeArgumentListSyntax)VisitTypeArgumentList(node.TypeArgumentList));
            // Don't call base.VisitGenericName(), since VisitIdentifierName() may double-widen identifier

            if (AttributeMatchUtil.HasTriviaAnnotationSimple(node.GetLeadingTrivia(), WidenAttributeName))
            {
                node = (GenericNameSyntax)WidenType(node);
            }

            return node;
        }


        //
        // Proper attributes
        //

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node);

            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName))
            {
                node = node.WithDeclaration(node.Declaration.WithType(WidenType(node.Declaration.Type)));
            }

            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName))
            {
                node = node.WithType(WidenType(node.Type));
            }

            if (node.ExplicitInterfaceSpecifier != null)
            {
                // reassociate trivia from return type expression to interface type
                node = node
                    .WithType(
                        node.Type.WithTrailingTrivia(SyntaxTriviaList.Empty))
                    .WithExplicitInterfaceSpecifier(
                        node.ExplicitInterfaceSpecifier.WithLeadingTrivia(
                            node.Type.GetTrailingTrivia().AddRange(node.ExplicitInterfaceSpecifier.GetLeadingTrivia())));
            }

            node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node);
            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName))
            {
                node = node.WithReturnType(WidenType(node.ReturnType));
            }

            if (node.ExplicitInterfaceSpecifier != null)
            {
                // reassociate trivia from return type expression to interface type
                node = node
                    .WithReturnType(
                        node.ReturnType.WithTrailingTrivia(SyntaxTriviaList.Empty))
                    .WithExplicitInterfaceSpecifier(
                        node.ExplicitInterfaceSpecifier.WithLeadingTrivia(
                            node.ReturnType.GetTrailingTrivia().AddRange(node.ExplicitInterfaceSpecifier.GetLeadingTrivia())));
            }

            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
            return node;
        }

        // this one does both proper and pseudo attributes
        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            // Normal parameter lists use a proper attribute: [Widen]
            // Anonymous delegate parameter lists do not permit attributes, so must use trivia: /*[Widen]*/
            // Therefore, check for both
            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName)
                || AttributeMatchUtil.HasTriviaAnnotationSimple(node.GetLeadingTrivia(), WidenAttributeName))
            {
                node = node.WithType(WidenType(node.Type));
            }

            node = (ParameterSyntax)base.VisitParameter(node);
            return node;
        }


        // Trivia pseudo-attributes

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Comments preceding base type declaration are attributed to the correct base type declaration AS LONG AS
            // each type is listed on a separate source line. Therefore we're requiring that source formattig and not doing
            // normalization here - just delegating to GenericName widening. If we wish to make that more flexible, this
            // is the place to normalize trivia on node.BaseList.Types

            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            return node;
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            node = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node);

            if (AttributeMatchUtil.HasTriviaAnnotationSimple(node.GetLeadingTrivia(), WidenAttributeName))
            {
                if (node.Declaration.Type.IsKind(SyntaxKind.PredefinedType))
                {
                    node = node.WithDeclaration(
                        node.Declaration.WithType(
                            WidenType(node.Declaration.Type)).WithTriviaFrom(node.Declaration.Type));
                }
            }

            return node;
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            // reassociate trivia from 'new' to type expression
            node = node
                .WithNewKeyword(
                    node.NewKeyword.WithTrailingTrivia(SyntaxTriviaList.Empty))
                .WithType(
                    node.Type.WithLeadingTrivia(
                        node.NewKeyword.TrailingTrivia.AddRange(node.Type.GetLeadingTrivia())));

            node = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node);
            return node;
        }

        public override SyntaxNode VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            // reassociate trivia from 'new' to type expression
            node = node
                .WithNewKeyword(
                    node.NewKeyword.WithTrailingTrivia(SyntaxTriviaList.Empty))
                .WithType(
                    node.Type.WithLeadingTrivia(
                        node.NewKeyword.TrailingTrivia.AddRange(node.Type.GetLeadingTrivia())));

            node = (ArrayCreationExpressionSyntax)base.VisitArrayCreationExpression(node);
            return node;
        }

        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            node = (TypeArgumentListSyntax)base.VisitTypeArgumentList(node);

            int i = 0;
            foreach (TypeSyntax type in node.Arguments)
            {
                if (AttributeMatchUtil.HasTriviaAnnotationSimple(type.GetLeadingTrivia(), WidenAttributeName))
                {
                    if ((type is PredefinedTypeSyntax) || (type is GenericNameSyntax))
                    {
                        node = node.WithArguments(
                            node.Arguments.RemoveAt(i).Insert(i, WidenType(type).WithTriviaFrom(type)));
                    }
                }
                i++;
            }

            return node;
        }

        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node)
        {
            // reassociate trivia from cast's enclosing open paren to type expression
            node = node
                .WithOpenParenToken(
                    node.OpenParenToken.WithTrailingTrivia(SyntaxTriviaList.Empty))
                .WithType(
                    node.Type.WithLeadingTrivia(
                        node.OpenParenToken.TrailingTrivia.AddRange(node.Type.GetLeadingTrivia())));

            PredefinedTypeSyntax predefinedType;
            if ((predefinedType = node.Type as PredefinedTypeSyntax) != null)
            {
                if (predefinedType.Keyword.IsKind(SyntaxKind.IntKeyword) || predefinedType.Keyword.IsKind(SyntaxKind.UIntKeyword))
                {
                    if (AttributeMatchUtil.HasTriviaAnnotationSimple(node.Type.GetLeadingTrivia(), WidenAttributeName))
                    {
                        node = node.WithType(WidenType(node.Type));
                    }
                }
            }

            node = (CastExpressionSyntax)base.VisitCastExpression(node);
            return node;
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // reassociate trivia from operator to right hand side expression
            node = node
                .WithOperatorToken(
                    node.OperatorToken.WithTrailingTrivia(SyntaxTriviaList.Empty))
                .WithRight(
                    node.Right.WithLeadingTrivia(
                        node.OperatorToken.TrailingTrivia.AddRange(node.Right.GetLeadingTrivia())));

            node = (BinaryExpressionSyntax)base.VisitBinaryExpression(node);
            return node;
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
        {
            // reassociate trivia from open paren to type expression
            node = node
                .WithOpenParenToken(
                    node.OpenParenToken.WithTrailingTrivia(SyntaxTriviaList.Empty))
                .WithType(
                    node.Type.WithLeadingTrivia(
                        node.OpenParenToken.TrailingTrivia.AddRange(node.Type.GetLeadingTrivia())));

            node = (ForEachStatementSyntax)base.VisitForEachStatement(node);
            return node;
        }

        public override SyntaxNode VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            // reassociate trivia from open paren to type expression
            node = node
                .WithOpenParenToken(
                    node.OpenParenToken.WithTrailingTrivia(SyntaxTriviaList.Empty))
                .WithType(
                    node.Type.WithLeadingTrivia(
                        node.OpenParenToken.TrailingTrivia.AddRange(node.Type.GetLeadingTrivia())));

            node = (DefaultExpressionSyntax)base.VisitDefaultExpression(node);
            return node;
        }


        // INCOMPLETE -- add more as needed
    }
}
