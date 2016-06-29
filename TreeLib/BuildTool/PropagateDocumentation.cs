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
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace BuildTool
{
    public static class PropagateDocumentation
    {
        private const string DocSourceAttributeName = "DocumentationSource";

        public static SyntaxNode Do(Compilation compilation, Compilation interfacesCompilation, SyntaxNode root, SyntaxNode targetTypeDeclaration)
        {
            // Couldn't get SymbolFinder.FindImplementedInterfaceMembersAsync or .FindImplementations working, so doing it the hard hacky way.

            if (targetTypeDeclaration is StructDeclarationSyntax)
            {
                return root; // not propagating documentation into the structs (used for enumeration entry definitions)
            }

            // collect all interfaces and the methods/properties they declare
            Dictionary<InterfaceDeclarationSyntax, List<MemberDeclarationSyntax>> interfaces = new Dictionary<InterfaceDeclarationSyntax, List<MemberDeclarationSyntax>>();
            foreach (Compilation compilation1 in new Compilation[] { interfacesCompilation, compilation })
            {
                foreach (SyntaxTree interfaceTree in compilation1.SyntaxTrees)
                {
                    foreach (SyntaxNode node in interfaceTree.GetRoot().DescendantNodesAndSelf().Where(
                        delegate (SyntaxNode candidate) { return candidate.IsKind(SyntaxKind.InterfaceDeclaration); }))
                    {
                        if (AttributeMatchUtil.HasAttributeSimple(((InterfaceDeclarationSyntax)node).AttributeLists, DocSourceAttributeName))
                        {
                            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();
                            foreach (SyntaxNode decl in node.DescendantNodesAndSelf().Where(delegate (SyntaxNode candidate)
                                { return candidate.IsKind(SyntaxKind.MethodDeclaration) || candidate.IsKind(SyntaxKind.PropertyDeclaration); }))
                            {
                                members.Add((MemberDeclarationSyntax)decl);
                            }
                            InterfaceDeclarationSyntax ifaceDecl = (InterfaceDeclarationSyntax)node;
                            interfaces.Add(ifaceDecl, members);
                        }
                    }
                }
            }

            // enumrate base types of generated class
            List<BaseTypeSyntax> baseTypes = new List<BaseTypeSyntax>();
            {
                IEnumerable<BaseTypeSyntax> baseTypeList = ((ClassDeclarationSyntax)targetTypeDeclaration).BaseList.Types;
                foreach (BaseTypeSyntax baseType in baseTypeList)
                {
                    baseTypes.Add(baseType);
                }
            }

            Dictionary<SyntaxNode, SyntaxNode> replacements = new Dictionary<SyntaxNode, SyntaxNode>();
            foreach (BaseTypeSyntax baseType in baseTypes)
            {
                // can we find an interface that matches this?
                foreach (KeyValuePair<InterfaceDeclarationSyntax, List<MemberDeclarationSyntax>> interfaceItem in interfaces)
                {
                    InterfaceDeclarationSyntax interfaceDeclaration = interfaceItem.Key;
                    // hack
                    string baseTypeString = GetStringForBaseTypeComparison(baseType);
                    string patternString = GetStringForInterfaceComparison(interfaceDeclaration);
                    //Console.WriteLine("{2}  {0}  ?  {1}", baseTypeString, patternString, String.Equals(baseTypeString, patternString) ? "*" : " ");
                    if (String.Equals(baseTypeString, patternString))
                    {
                        // can we find any members we implemented that are in the interface?
                        foreach (SyntaxNode node in targetTypeDeclaration.DescendantNodes(delegate (SyntaxNode descendInto) { return (descendInto == targetTypeDeclaration) || descendInto.IsKind(SyntaxKind.MethodDeclaration) || descendInto.IsKind(SyntaxKind.PropertyDeclaration); })
                            .Where(delegate (SyntaxNode candidate) { return candidate.IsKind(SyntaxKind.MethodDeclaration) || candidate.IsKind(SyntaxKind.PropertyDeclaration); }))
                        {
                            if (node.IsKind(SyntaxKind.MethodDeclaration))
                            {
                                MethodDeclarationSyntax implementation = (MethodDeclarationSyntax)node;
                                if (default(SyntaxToken) == implementation.Modifiers.FirstOrDefault(delegate (SyntaxToken candidate) { return candidate.IsKind(SyntaxKind.PublicKeyword); }))
                                {
                                    continue; // non-public can't be from an interface
                                }
                                foreach (MemberDeclarationSyntax prototype1 in interfaceItem.Value)
                                {
                                    if (prototype1.IsKind(SyntaxKind.MethodDeclaration))
                                    {
                                        MethodDeclarationSyntax prototype = (MethodDeclarationSyntax)prototype1;
                                        // HACK: should check argument and return types
                                        if (SyntaxFactory.AreEquivalent(implementation.Identifier, prototype.Identifier)
                                            && (implementation.ParameterList.Parameters.Count == prototype.ParameterList.Parameters.Count))
                                        {
                                            // copy documentation
                                            SyntaxNode replacement =
                                                node.WithLeadingTrivia(
                                                    node.GetLeadingTrivia()
                                                        .Add(SyntaxFactory.EndOfLine(Environment.NewLine))
                                                        .AddRange(CookDocumentationTrivia(prototype.GetLeadingTrivia())));
                                            if (!replacements.ContainsKey(node)) // in case exposed by multiple interfaces
                                            {
                                                replacements.Add(node, replacement);
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                            else if (node.IsKind(SyntaxKind.PropertyDeclaration))
                            {
                                PropertyDeclarationSyntax implementation = (PropertyDeclarationSyntax)node;
                                if (default(SyntaxToken) == implementation.Modifiers.FirstOrDefault(delegate (SyntaxToken candidate) { return candidate.IsKind(SyntaxKind.PublicKeyword); }))
                                {
                                    continue; // non-public can't be from an interface
                                }
                                foreach (MemberDeclarationSyntax prototype1 in interfaceItem.Value)
                                {
                                    if (prototype1.IsKind(SyntaxKind.PropertyDeclaration))
                                    {
                                        PropertyDeclarationSyntax prototype = (PropertyDeclarationSyntax)prototype1;
                                        // HACK
                                        if (SyntaxFactory.AreEquivalent(implementation.Identifier, prototype.Identifier))
                                        {
                                            // copy documentation
                                            SyntaxNode replacement =
                                                node.WithLeadingTrivia(
                                                    node.GetLeadingTrivia()
                                                        .Add(SyntaxFactory.EndOfLine(Environment.NewLine))
                                                        .AddRange(CookDocumentationTrivia(prototype.GetLeadingTrivia())));
                                            if (!replacements.ContainsKey(node)) // in case exposed by multiple interfaces
                                            {
                                                replacements.Add(node, replacement);
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                throw new ArgumentException();
                            }
                        }

                        break;
                    }
                }
            }
            SyntaxNodeReplacementRewriter syntaxNodeReplacementRewriter = new SyntaxNodeReplacementRewriter(replacements);
            root = syntaxNodeReplacementRewriter.Visit(root);

            // This is probably really slow - but we can use AreEquivalent() since we've only changed trivia
            targetTypeDeclaration = root.DescendantNodes().First(delegate (SyntaxNode candidate) { return SyntaxFactory.AreEquivalent(targetTypeDeclaration, candidate); });

            replacements.Clear();
            foreach (BaseTypeSyntax baseType in baseTypes)
            {
                // can we find an interface that matches this?
                foreach (KeyValuePair<InterfaceDeclarationSyntax, List<MemberDeclarationSyntax>> interfaceItem in interfaces)
                {
                    InterfaceDeclarationSyntax interfaceDeclaration = interfaceItem.Key;
                    // hack
                    if (String.Equals(baseType.Type.ToString(), String.Concat(interfaceDeclaration.Identifier.Text, interfaceDeclaration.TypeParameterList != null ? interfaceDeclaration.TypeParameterList.ToString() : null)))
                    {
                        // propagate interface comment to class
                        if (!replacements.ContainsKey(targetTypeDeclaration))
                        {
                            replacements.Add(
                                targetTypeDeclaration,
                                targetTypeDeclaration.WithLeadingTrivia(
                                    targetTypeDeclaration.GetLeadingTrivia()
                                        .Add(SyntaxFactory.EndOfLine(Environment.NewLine))
                                        .AddRange(CookDocumentationTrivia(interfaceDeclaration.GetLeadingTrivia()))));
                        }

                        break;
                    }
                }
            }
            syntaxNodeReplacementRewriter = new SyntaxNodeReplacementRewriter(replacements);
            root = syntaxNodeReplacementRewriter.Visit(root);

            return root;
        }

        private static SyntaxTriviaList CookDocumentationTrivia(SyntaxTriviaList original)
        {
            List<SyntaxTrivia> trivia = new List<SyntaxTrivia>();
            bool lastWasNewLine = false;
            foreach (SyntaxTrivia one in original)
            {
                if (one.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    if (lastWasNewLine)
                    {
                        trivia.Clear();
                    }
                    lastWasNewLine = true;
                }
                else
                {
                    lastWasNewLine = false;
                    trivia.Add(one);
                }
            }
            return new SyntaxTriviaList().AddRange(trivia);
        }

        private static string GetStringForBaseTypeComparison(BaseTypeSyntax baseType)
        {
            if (baseType.Type.IsKind(SyntaxKind.GenericName))
            {
                GenericNameSyntax genericType = ((GenericNameSyntax)baseType.Type);
                return String.Concat(genericType.Identifier.Text, "`", genericType.TypeArgumentList.Arguments.Count);
            }
            else
            {
                return baseType.Type.ToString();
            }
        }

        private static string GetStringForInterfaceComparison(InterfaceDeclarationSyntax interfaceDeclaration)
        {
            string text = interfaceDeclaration.Identifier.Text;
            if (interfaceDeclaration.TypeParameterList != null)
            {
                text = String.Concat(text, "`", interfaceDeclaration.TypeParameterList.Parameters.Count);
            }
            return text;
        }
    }
}
