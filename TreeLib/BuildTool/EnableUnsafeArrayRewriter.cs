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
    // TODO: this is hacky with hardcoded names - need to rewrite to be generic


    public class RemoveFieldsPreparingForStructHoistRewriter : CSharpSyntaxRewriter
    {
        private readonly string className;
        private readonly SemanticModel semanticModel;
        private readonly Stack<bool> currentClassStack = new Stack<bool>();
        private readonly Stack<StructDeclarationSyntax> currentStructStack = new Stack<StructDeclarationSyntax>();

        private readonly static string[] RelocatedNames = new string[] { "key", "value" };

        public RemoveFieldsPreparingForStructHoistRewriter(string className, SemanticModel semanticModel)
        {
            this.className = className;
            this.semanticModel = semanticModel;
        }


        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            currentClassStack.Push(String.Equals(node.Identifier.Text, className));
            currentStructStack.Push(null);
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            currentClassStack.Pop();
            currentStructStack.Pop();
            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            currentClassStack.Push(false);
            currentStructStack.Push(node);
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
            currentClassStack.Pop();
            currentStructStack.Pop();
            return node;
        }

        // eliminate key/value fields from the Node structure
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if ((currentStructStack.Peek() != null)
                && String.Equals(currentStructStack.Peek().Identifier.Text, "Node")
                && (node.Declaration.Variables.Count == 1)
                && (Array.IndexOf(RelocatedNames, node.Declaration.Variables[0].Identifier.Text) >= 0))
            {
                return null;
            }

            return base.VisitFieldDeclaration(node);
        }

        // rewrite key/value references to nodes2 array
        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            node = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node);

            if (Array.IndexOf(RelocatedNames, node.Name.Identifier.Text) >= 0)
            {
                ElementAccessExpressionSyntax arrayRef;
                if ((arrayRef = node.Expression as ElementAccessExpressionSyntax) != null)
                {
                    IdentifierNameSyntax baseIdent;
                    if ((baseIdent = arrayRef.Expression as IdentifierNameSyntax) != null)
                    {
                        node = node.WithExpression(
                            SyntaxFactory.ElementAccessExpression(
                                SyntaxFactory.IdentifierName("nodes2"),
                                arrayRef.ArgumentList));
                    }
                }
            }

            return node;
        }
    }


    // Structs are considered "managed" types if contained in a generic container type, even if none of
    // their members are variant. Therefore, must hoist out of the containing class to use "fixed".
    public class HoistNodeStructRewriter : CSharpSyntaxRewriter
    {
        private readonly string className;
        private readonly Stack<ClassDeclarationSyntax> currentClassStack = new Stack<ClassDeclarationSyntax>();
        private readonly List<StructDeclarationSyntax> hoistedList = new List<StructDeclarationSyntax>();
        private readonly string[] hoisters = new string[] { "Node", "NodeRef" };
        private readonly Stack<StructDeclarationSyntax> currentHoistStruct = new Stack<StructDeclarationSyntax>();

        public HoistNodeStructRewriter(string className)
        {
            this.className = className;
        }

        private string ReplacementName(string baseName)
        {
            return String.Concat(baseName, "_", className);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            currentClassStack.Push(node);
            currentHoistStruct.Push(null);
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            currentClassStack.Pop();
            currentHoistStruct.Pop();
            return node;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            int i = Array.IndexOf(hoisters, node.Identifier.Text);
            if (i >= 0)
            {
                return SyntaxFactory.IdentifierName(ReplacementName(hoisters[i])).WithTriviaFrom(node);
            }
            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            node = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node);

            if (currentHoistStruct.Peek() != null)
            {
                node = node.WithIdentifier(SyntaxFactory.Identifier(ReplacementName(currentHoistStruct.Peek().Identifier.Text)).WithTriviaFrom(node.Identifier));
            }

            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (String.Equals(currentClassStack.Peek().Identifier.Text, className)
                && (Array.IndexOf(hoisters, node.Identifier.Text) >= 0))
            {
                currentHoistStruct.Push(node);
                node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
                currentHoistStruct.Pop();

                hoistedList.Add(node);

                return null;
            }

            currentHoistStruct.Push(null);
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
            currentHoistStruct.Pop();
            return node;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            node = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);

            for (int i = 0; i < hoistedList.Count; i++)
            {
                StructDeclarationSyntax hoisted = hoistedList[i];
                hoisted = hoisted.WithIdentifier(SyntaxFactory.Identifier(ReplacementName(hoisted.Identifier.Text)));
                for (int j = 0; j < hoisted.Modifiers.Count; j++)
                {
                    if (hoisted.Modifiers[j].Kind() == SyntaxKind.PrivateKeyword)
                    {
                        hoisted = hoisted.WithModifiers(hoisted.Modifiers.RemoveAt(j));
                        j--;
                    }
                }
                node = node.AddMembers(hoisted);
            }
            hoistedList.Clear();

            return node;
        }
    }


    public class EnableArrayFixationRewriter : CSharpSyntaxRewriter
    {
        private static bool IsMarkedForFixed(SyntaxList<AttributeListSyntax> attributeLists)
        {
            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    IdentifierNameSyntax identifierName;
                    if ((identifierName = attribute.Name as IdentifierNameSyntax) != null)
                    {
                        if (String.Equals(identifierName.Identifier.Text, "EnableFixed"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            if (IsMarkedForFixed(node.AttributeLists))
            {
                node = node.WithModifiers(
                    node.Modifiers.Add(
                        SyntaxFactory.Token(
                            new SyntaxTriviaList(),
                            SyntaxKind.UnsafeKeyword,
                            new SyntaxTriviaList().Add(SyntaxFactory.Space))));

                ExpressionSyntax target = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName("nodes"));
                VariableDeclarationSyntax pointerDeclaration =
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.PointerType(SyntaxFactory.IdentifierName("Node")).WithTrailingTrivia(SyntaxFactory.Space),
                        new SeparatedSyntaxList<VariableDeclaratorSyntax>().Add(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("nodes"),
                                null,
                                SyntaxFactory.EqualsValueClause(target))));

                FixedStatementSyntax fixedStatement =
                    SyntaxFactory.FixedStatement(
                        pointerDeclaration,
                        node.Body);

                fixedStatement = fixedStatement.WithLeadingTrivia(
                        SyntaxFactory.EndOfLine(Environment.NewLine),
                        SyntaxFactory.Trivia(
                            SyntaxFactory.IfDirectiveTrivia(
                                SyntaxFactory.PrefixUnaryExpression(
                                    SyntaxKind.LogicalNotExpression,
                                    SyntaxFactory.IdentifierName("DEBUG")).WithLeadingTrivia(SyntaxFactory.Space),
                                true/*isActive*/,
                                true/*branchTaken*/,
                                true/*conditionValue*/)),
                        SyntaxFactory.EndOfLine(Environment.NewLine));

                fixedStatement = fixedStatement.WithCloseParenToken(
                    fixedStatement.CloseParenToken.WithTrailingTrivia(
                        SyntaxFactory.EndOfLine(Environment.NewLine),
                        SyntaxFactory.Trivia(
                            SyntaxFactory.EndIfDirectiveTrivia(true/*isActive*/)),
                        SyntaxFactory.EndOfLine(Environment.NewLine)));

                node = node.WithBody(
                    SyntaxFactory.Block(
                        fixedStatement));
            }

            return node;
        }
    }
}
