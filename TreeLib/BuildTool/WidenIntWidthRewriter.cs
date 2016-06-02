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
    public class WidenIntWidthRewriter : CSharpSyntaxRewriter
    {
        private const string WidenAttributeName = "Widen";
        private const string LongAppendText = "Long";

        private readonly Dictionary<string, string> interfaceNameReplacements = new Dictionary<string, string>();

        // hacky, but way easier
        private static readonly Dictionary<string, string> hardcodedReplacements = CreateHardcodedReplacements();


        public WidenIntWidthRewriter()
        {
        }

        private static Dictionary<string, string> CreateHardcodedReplacements()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string name in new string[] { "Range", "Range2MapEntry", "MultiRankMapEntry" })
            {
                dict.Add(name, String.Concat(name, LongAppendText));
            }
            return dict;
        }

        private static TypeSyntax WidenIntegerType(TypeSyntax type)
        {
            if ((type.Kind() == SyntaxKind.PredefinedType) && (((PredefinedTypeSyntax)type).Keyword.Kind() == SyntaxKind.IntKeyword))
            {
                type = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword)).WithTrailingTrivia(SyntaxFactory.Space);
            }
            else if ((type.Kind() == SyntaxKind.PredefinedType) && (((PredefinedTypeSyntax)type).Keyword.Kind() == SyntaxKind.UIntKeyword))
            {
                type = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword)).WithTrailingTrivia(SyntaxFactory.Space);
            }
            else
            {
                throw new ArgumentException();
            }

            return type;
        }


        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
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
                        if (AttributeMatchUtil.HasTriviaAnnotationSimple(baseType.GetLeadingTrivia(), WidenAttributeName))
                        {
                            IdentifierNameSyntax interfaceIdentifier;
                            GenericNameSyntax interfaceIdentifierParameterized;

                            if ((interfaceIdentifier = baseType.Type as IdentifierNameSyntax) != null)
                            {
                                string interfaceName = interfaceIdentifier.Identifier.Text;
                                string interfaceNameLong = String.Concat(interfaceName, LongAppendText);
                                node = node.WithBaseList(
                                    node.BaseList.WithTypes(
                                        node.BaseList.Types.RemoveAt(i).Insert(
                                            i,
                                            SyntaxFactory.SimpleBaseType(
                                                SyntaxFactory.IdentifierName(interfaceNameLong).WithTriviaFrom(interfaceIdentifier))
                                                .WithTriviaFrom(node.BaseList.Types[i]))));
                                interfaceNameReplacements.Add(interfaceName, interfaceNameLong);
                            }
                            else if ((interfaceIdentifierParameterized = baseType.Type as GenericNameSyntax) != null)
                            {
                                string interfaceName = interfaceIdentifierParameterized.Identifier.Text;
                                string interfaceNameLong = String.Concat(interfaceName, LongAppendText);

                                GenericNameSyntax replacement = SyntaxFactory.GenericName(
                                    SyntaxFactory.Identifier(
                                        interfaceIdentifierParameterized.GetLeadingTrivia(),
                                        interfaceNameLong,
                                        interfaceIdentifierParameterized.GetTrailingTrivia()),
                                    interfaceIdentifierParameterized.TypeArgumentList).WithTriviaFrom(interfaceIdentifierParameterized);

                                node = node.WithBaseList(
                                    node.BaseList.WithTypes(
                                        node.BaseList.Types.RemoveAt(i).Insert(
                                            i,
                                            SyntaxFactory.SimpleBaseType(replacement).WithTriviaFrom(node.BaseList.Types[i]))));
                                interfaceNameReplacements.Add(interfaceName, interfaceNameLong);
                            }
                        }
                    }
                    i++;
                }
            }

            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            return node;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node);

            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName))
            {
                node = node.WithDeclaration(node.Declaration.WithType(WidenIntegerType(node.Declaration.Type)));
            }

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
                            WidenIntegerType(node.Declaration.Type)).WithTriviaFrom(node.Declaration.Type));
                }
            }

            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            node = (IdentifierNameSyntax)base.VisitIdentifierName(node);

            string replacement;
            if (hardcodedReplacements.TryGetValue(node.Identifier.Text, out replacement))
            {
                if (forceReplacement || (node.Parent is TypeSyntax))
                {
                    node = SyntaxFactory.IdentifierName(replacement).WithTriviaFrom(node);
                }
            }

            return node;
        }

        private bool forceReplacement;
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            bool oldForceReplacement = forceReplacement;

            forceReplacement = true;
            node = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node);

            forceReplacement = oldForceReplacement;
            return node;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            node = (ParameterSyntax)base.VisitParameter(node);

            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName))
            {
                node = node.WithType(WidenIntegerType(node.Type));
            }

            return node;
        }

        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            node = (TypeArgumentListSyntax)base.VisitTypeArgumentList(node);

            node = NormalizeArgumentListUtil.NormalizeTypeArgumentList(node);

            int i = 0;
            foreach (TypeSyntax type1 in node.Arguments)
            {
                if (AttributeMatchUtil.HasTriviaAnnotationSimple(type1.GetLeadingTrivia(), WidenAttributeName))
                {
                    PredefinedTypeSyntax type;
                    if ((type = type1 as PredefinedTypeSyntax) != null)
                    {
                        node = node.WithArguments(
                            node.Arguments.RemoveAt(i).Insert(i, WidenIntegerType(type).WithTriviaFrom(type)));
                    }
                }
                i++;
            }

            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName))
            {
                if (node.ReturnType.IsKind(SyntaxKind.PredefinedType))
                {
                    node = node.WithReturnType(WidenIntegerType(node.ReturnType));
                }
                else
                {
                }
            }

            return node;
        }

        public override SyntaxNode VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node)
        {
            node = (ExplicitInterfaceSpecifierSyntax)base.VisitExplicitInterfaceSpecifier(node);

            string replacement;
            if (interfaceNameReplacements.TryGetValue(((IdentifierNameSyntax)node.Name).Identifier.Text, out replacement))
            {
                node = node.WithName(SyntaxFactory.IdentifierName(replacement).WithTriviaFrom(node.Name));
            }

            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node);

            if (AttributeMatchUtil.HasAttributeSimple(node.AttributeLists, WidenAttributeName))
            {
                if (node.Type.IsKind(SyntaxKind.PredefinedType))
                {
                    node = node.WithType(WidenIntegerType(node.Type));
                }
            }

            return node;
        }
    }
}
