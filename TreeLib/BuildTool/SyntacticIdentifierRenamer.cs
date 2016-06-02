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
    // Since our code template doesn't compile (due to duplicate alternative member implementations), we can't use the
    // semantic tree to rename (as shown in Roslyn samples). Therefore, this renamer works at the syntactic level.
    // One must be careful to not have multiple symbols tht use the same identifier name since it will rename all of them.
    public class SyntacticIdentifierRenamer : CSharpSyntaxRewriter
    {
        private readonly string originalName;
        private readonly string newName;

        public SyntacticIdentifierRenamer(string originalName, string newName)
        {
            this.originalName = originalName;
            this.newName = newName;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            SyntaxNode updatedNode = base.Visit(node);

            IdentifierNameSyntax identifierNameNode;
            if (((identifierNameNode = node as IdentifierNameSyntax) != null)
                && String.Equals(identifierNameNode.Identifier.Text, originalName))
            {
            }

            return updatedNode;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            if (String.Equals(node.Identifier.Text, originalName))
            {
                SyntaxToken replacement =
                    SyntaxFactory.Identifier(
                        node.Identifier.LeadingTrivia,
                        newName,
                        node.Identifier.TrailingTrivia);

                node = node.WithIdentifier(replacement);
            }

            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);

            if (String.Equals(node.Identifier.Text, originalName))
            {
                SyntaxToken replacement =
                    SyntaxFactory.Identifier(
                        node.Identifier.LeadingTrivia,
                        newName,
                        node.Identifier.TrailingTrivia);

                node = node.WithIdentifier(replacement);
            }

            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            node = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node);

            if (String.Equals(node.Identifier.Text, originalName))
            {
                SyntaxToken replacement =
                    SyntaxFactory.Identifier(
                        node.Identifier.LeadingTrivia,
                        newName,
                        node.Identifier.TrailingTrivia);

                node = node.WithIdentifier(replacement);
            }

            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node);

            if (String.Equals(node.Identifier.Text, originalName))
            {
                SyntaxToken replacement =
                    SyntaxFactory.Identifier(
                        node.Identifier.LeadingTrivia,
                        newName,
                        node.Identifier.TrailingTrivia);

                node = node.WithIdentifier(replacement);
            }

            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            node = (IdentifierNameSyntax)base.VisitIdentifierName(node);

            if (String.Equals(node.Identifier.Text, originalName))
            {
                SyntaxToken replacement =
                    SyntaxFactory.Identifier(
                        node.Identifier.LeadingTrivia,
                        newName,
                        node.Identifier.TrailingTrivia);

                node = node.WithIdentifier(replacement);
            }

            return node;
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            node = (GenericNameSyntax)base.VisitGenericName(node);

            if (String.Equals(node.Identifier.Text, originalName))
            {
                SyntaxToken replacement =
                    SyntaxFactory.Identifier(
                        node.Identifier.LeadingTrivia,
                        newName,
                        node.Identifier.TrailingTrivia);

                node = node.WithIdentifier(replacement);
            }

            return node;
        }


        // INCOMPLETE: add more as needed
    }
}
