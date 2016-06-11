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
    public static class AttributeMatchUtil
    {
        public static bool HasAttributeSimple(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
        {
            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    IdentifierNameSyntax identifierName;
                    if ((identifierName = attribute.Name as IdentifierNameSyntax) != null)
                    {
                        if (String.Equals(identifierName.Identifier.Text, attributeName))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Used for statements: where real attributes are not permitted, a fake attribute in a C-style comment
        // can be specified.
        public static bool HasTriviaAnnotationSimple(IEnumerable<SyntaxTrivia> trivia, string attributeName)
        {
            foreach (SyntaxTrivia trivium in trivia)
            {
                if (trivium.Kind() == SyntaxKind.MultiLineCommentTrivia)
                {
                    // extract [{tag}]

                    string s = trivium.ToFullString().Trim();
                    if (!(s.StartsWith("/*") && s.EndsWith("*/")))
                    {
                        continue;
                    }
                    s = s.Substring(2, s.Length - 2 - 2).Trim();
                    if (!(s.StartsWith("[") && s.EndsWith("]")))
                    {
                        continue;
                    }
                    s = s.Substring(1, s.Length - 1 - 1).Trim();

                    if (String.Equals(s, attributeName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        public enum FindResult { NotFound, Include, Exclude };

        public static FindResult TestAttributes(SyntaxList<AttributeListSyntax> attributeLists, FacetList[] facetAxes)
        {
            FindResult result = FindResult.NotFound; // do not remove if no attribute present

            foreach (FacetList facetAxis in facetAxes)
            {
                foreach (var attributeList in attributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        IdentifierNameSyntax identifierName;
                        if ((identifierName = attribute.Name as IdentifierNameSyntax) != null)
                        {
                            if (String.Equals(identifierName.Identifier.Text, facetAxis.axisTag))
                            {
                                if (result == FindResult.NotFound)
                                {
                                    result = FindResult.Include;
                                }

                                bool found = false;
                                foreach (var attributeArgument in attribute.ArgumentList.Arguments)
                                {
                                    MemberAccessExpressionSyntax attributeArgumentEnumTag;
                                    if ((attributeArgumentEnumTag = attributeArgument.Expression as MemberAccessExpressionSyntax) != null)
                                    {
                                        if (Array.IndexOf(facetAxis.facets, attributeArgumentEnumTag.Name.Identifier.Text) >= 0)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                }

                                if (!found) // remove if attribute present but facet not listed
                                {
                                    result = FindResult.Exclude;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        // Used for statements: where real attributes are not permitted, a fake attribute in a C-style comment
        // can be specified.
        public static bool TestTriviaAnnotation(IEnumerable<SyntaxTrivia> trivia, FacetList[] facetAxes)
        {
            bool foundDisjuction = true;
            foreach (FacetList facetAxis in facetAxes)
            {
                foreach (SyntaxTrivia trivium in trivia)
                {
                    if (trivium.Kind() == SyntaxKind.MultiLineCommentTrivia)
                    {
                        // extract [{tag}({tag}.{facet})]

                        string s = trivium.ToFullString().Trim();
                        if (!(s.StartsWith("/*") && s.EndsWith("*/")))
                        {
                            continue;
                        }
                        s = s.Substring(2, s.Length - 2 - 2).Trim();
                        if (!(s.StartsWith("[") && s.EndsWith("]")))
                        {
                            continue;
                        }
                        s = s.Substring(1, s.Length - 1 - 1).Trim();

                        int i = s.IndexOf('(');
                        int j = s.IndexOf(')');
                        if (!((i >= 0) && (j >= i) && String.Equals(s.Substring(0, i).Trim(), facetAxis.axisTag)))
                        {
                            continue;
                        }
                        s = s.Substring(i + 1, j - (i + 1));

                        bool found = false;
                        foreach (string t in s.Split(','))
                        {
                            string tt = t;
                            i = tt.IndexOf('.');
                            if ((i >= 0) && String.Equals(tt.Substring(0, i).Trim(), facetAxis.axisTag))
                            {
                                tt = tt.Substring(i + 1).Trim();
                                if (Array.IndexOf(facetAxis.facets, tt) >= 0)
                                {
                                    found = true;
                                }
                            }
                        }

                        foundDisjuction = foundDisjuction && found;
                    }
                }
            }

            return foundDisjuction;
        }


        public static bool TestEnumeratedFaceAttribute(SyntaxList<AttributeListSyntax> attributeLists, out ExpressionSyntax expressionOut, string[] attributeAliases, FacetList facetsList)
        {
            foreach (AttributeListSyntax attributeList in attributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    IdentifierNameSyntax attributeName;
                    if ((attributeName = attribute.Name as IdentifierNameSyntax) != null)
                    {
                        if (Array.IndexOf(attributeAliases, attributeName.Identifier.Text) >= 0)
                        {
                            expressionOut = attribute.ArgumentList.Arguments[0].Expression;

                            for (int i = 1; i < attribute.ArgumentList.Arguments.Count; i++)
                            {
                                MemberAccessExpressionSyntax argumentEnumTag = (MemberAccessExpressionSyntax)attribute.ArgumentList.Arguments[i].Expression;
                                if (Array.IndexOf(facetsList.facets, argumentEnumTag.Name.Identifier.Text) >= 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            expressionOut = null;
            return false;
        }
    }
}
