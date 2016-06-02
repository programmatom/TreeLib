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
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

// A NOTE for posterity: Don't use CSharpSyntaxVisitor - it won't do what you want. Use CSharpSyntaxWalker.

namespace BuildTool
{
    public class FacetList
    {
        public string axisTag { get; }
        public string[] facets { get; }

        public FacetList(string axisTag, string[] facets)
        {
            this.axisTag = axisTag;
            this.facets = facets;
        }

        public override string ToString()
        {
            return String.Format("{0}: {1}", axisTag, String.Join(", ", facets));
        }
    }

    public enum CFlags
    {
        None = 0,

        DowngradeCountToUint = 1,
        WidenInt = 2,
    }

    public class Config
    {
        public readonly string templateClassName; // for naming - this is also used as the input filename
        public readonly string storageClass; // for naming
        public readonly string specialization; // for naming
        public readonly CFlags flags;
        public readonly FacetList[] facetAxes;
        public readonly bool retainInCompilation;
        public readonly KeyValuePair<string, string>[] syntacticRenames = new KeyValuePair<string, string>[0];

        // this is also used as the output filename
        public string className { get { return String.Concat(templateClassName, storageClass, specialization); } } // for naming

        public Config(string templateClassName, string storageClass, string specialization, CFlags flags, FacetList[] facetAxes)
        {
            this.templateClassName = templateClassName;
            this.storageClass = storageClass;
            this.specialization = specialization;
            this.flags = flags;
            this.facetAxes = facetAxes;
        }

        public Config(string templateClassName, string storageClass, string specialization, CFlags flags, bool retainInCompilation, FacetList[] facetAxes)
            : this(templateClassName, storageClass, specialization, flags, facetAxes)
        {
            this.retainInCompilation = retainInCompilation;
        }

        public Config(string templateClassName, string storageClass, string specialization, CFlags flags, bool retainInCompilation, FacetList[] facetAxes, KeyValuePair<string, string>[] syntacticRenames)
            : this(templateClassName, storageClass, specialization, flags, retainInCompilation, facetAxes)
        {
            this.syntacticRenames = syntacticRenames;
        }

        public Config(XPathNavigator configNode)
        {
            this.templateClassName = configNode.SelectSingleNode("@templateClassName").Value;
            this.storageClass = configNode.SelectSingleNode("@storageClass").Value;
            this.specialization = configNode.SelectSingleNode("@specialization").Value;
            foreach (XPathNavigator flagsNode in configNode.Select("flags/flag"))
            {
                CFlags flag = (CFlags)Enum.Parse(typeof(CFlags), flagsNode.Value);
                this.flags |= flag;
            }
            List<FacetList> facetsList = new List<FacetList>();
            foreach (XPathNavigator facetAxisNode in configNode.Select("facetAxes/facetAxis"))
            {
                string tag = facetAxisNode.SelectSingleNode("@tag").Value;
                List<string> facets = new List<string>();
                foreach (XPathNavigator facetNode in facetAxisNode.Select("facet"))
                {
                    facets.Add(facetNode.Value);
                }
                facetsList.Add(new FacetList(tag, facets.ToArray()));
            }
            this.facetAxes = facetsList.ToArray();
            if (configNode.SelectSingleNode("retainInCompilation") != null)
            {
                this.retainInCompilation = configNode.SelectSingleNode("retainInCompilation").ValueAsBoolean;
            }
            List<KeyValuePair<string, string>> syntacticRenamesList = new List<KeyValuePair<string, string>>();
            foreach (XPathNavigator syntacticRenameNode in configNode.Select("syntacticRenames/rename"))
            {
                syntacticRenamesList.Add(new KeyValuePair<string, string>(syntacticRenameNode.SelectSingleNode("@from").Value, syntacticRenameNode.SelectSingleNode("@to").Value));
            }
            this.syntacticRenames = syntacticRenamesList.ToArray();
        }

#if false // TODO: remove
        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("config");

            writer.WriteStartAttribute("templateClassName");
            writer.WriteValue(this.templateClassName);
            writer.WriteEndAttribute(); // templateClassName

            writer.WriteStartAttribute("storageClass");
            writer.WriteValue(this.storageClass);
            writer.WriteEndAttribute(); // storageClass

            writer.WriteStartAttribute("specialization");
            writer.WriteValue(this.specialization);
            writer.WriteEndAttribute(); // specialization

            if (this.flags != (CFlags)0)
            {
                writer.WriteStartElement("flags");
                if ((this.flags & CFlags.DowngradeCountToUint) != 0)
                {
                    writer.WriteStartElement("flag");
                    writer.WriteValue(CFlags.DowngradeCountToUint.ToString());
                    writer.WriteEndElement(); // flag
                }
                if ((this.flags & CFlags.WidenInt) != 0)
                {
                    writer.WriteStartElement("flag");
                    writer.WriteValue(CFlags.WidenInt.ToString());
                    writer.WriteEndElement(); // flag
                }
                writer.WriteEndElement(); // flags
            }

            writer.WriteStartElement("facetAxes");
            foreach (FacetList facetList in this.facetAxes)
            {
                writer.WriteStartElement("facetAxis");

                writer.WriteStartAttribute("tag");
                writer.WriteValue(facetList.axisTag);
                writer.WriteEndAttribute(); // tag

                foreach (string facet in facetList.facets)
                {
                    writer.WriteStartElement("facet");
                    writer.WriteValue(facet);
                    writer.WriteEndElement(); // facet
                }

                writer.WriteEndElement(); // facetAxis
            }
            writer.WriteEndElement(); // facetAxes

            if (this.retainInCompilation)
            {
                writer.WriteStartElement("retainInCompilation");
                writer.WriteValue(this.retainInCompilation);
                writer.WriteEndElement(); // retainInCompilation
            }

            if (this.syntacticRenames.Length != 0)
            {
                writer.WriteStartElement("syntacticRenames");
                foreach (KeyValuePair<string, string> rename in this.syntacticRenames)
                {
                    writer.WriteStartElement("rename");

                    writer.WriteStartAttribute("from");
                    writer.WriteValue(rename.Key);
                    writer.WriteEndAttribute(); // from

                    writer.WriteStartAttribute("to");
                    writer.WriteValue(rename.Value);
                    writer.WriteEndAttribute(); // to

                    writer.WriteEndElement(); // rename
                }
                writer.WriteEndElement(); // syntacticRenames
            }

            writer.WriteEndElement(); // config
        }
#endif

        public static Config[] ReadConfigs(string path, out string[] unloadExceptions)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            List<string> unloadExceptionsList = new List<string>();
            foreach (XPathNavigator doNotUnloadNode in doc.CreateNavigator().Select("/configs/doNotUnloads/doNotUnload"))
            {
                unloadExceptionsList.Add(doNotUnloadNode.Value);
            }
            unloadExceptions = unloadExceptionsList.ToArray();

            List<Config> configList = new List<Config>();
            foreach (XPathNavigator configNode in doc.CreateNavigator().Select("/configs/config"))
            {
                Config config = new Config(configNode);
                configList.Add(config);
            }

            return configList.ToArray();
        }

#if false // TODO: remove
        public static void WriteConfigs(string path, Config[][] configsSets)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = new string(' ', 4);
            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartElement("configs");

                foreach (Config[] configs in configsSets)
                {
                    foreach (Config config in configs)
                    {
                        config.Write(writer);
                    }
                }

                writer.WriteEndElement(); // configs
            }
        }
#endif
    }

    public class Program
    {
        private const bool ShowNotepadForErrors = true;
        private static bool notepaded;


        private static void RebuildSemanticModel(ref SyntaxTree oldTree, ref SyntaxTree tree, ref SyntaxNode root, ref Compilation compilation, out SemanticModel semanticModel)
        {
            if (oldTree != null)
            {
                compilation = compilation.RemoveSyntaxTrees(oldTree);
            }
            oldTree = root.SyntaxTree;
            compilation = compilation.AddSyntaxTrees(root.SyntaxTree);
            tree = compilation.SyntaxTrees.Last();
            root = tree.GetRoot();
            semanticModel = compilation.GetSemanticModel(tree);
        }

        private static void EliminateDeadCode(ref SyntaxTree oldTree, ref SyntaxTree tree, ref SyntaxNode root, ref Compilation compilation, out SemanticModel semanticModel)
        {
            while (true)
            {
                RebuildSemanticModel(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);

                EliminateDeadBranchesRewriter dead = new EliminateDeadBranchesRewriter(semanticModel);
                root = dead.Visit(root);
                if (!dead.Changed)
                {
                    break;
                }
            }
        }

        private static void Generate(Config config, Compilation compilation, out Compilation compilationReplacement, string sourceFilePath, string targetFilePath)
        {
            compilationReplacement = null;

            // always enable DEBUG for code manipulation purposes (this does imply we can't have !DEBUG sections)
            CSharpParseOptions parseOptions = new CSharpParseOptions().WithPreprocessorSymbols("DEBUG");
            SyntaxTree tree = SyntaxFactory.ParseCompilationUnit(File.ReadAllText(sourceFilePath), 0, parseOptions).SyntaxTree;
            compilation = compilation.AddSyntaxTrees(tree);

            foreach (SyntaxNode node in tree.GetRoot().DescendantNodesAndSelf().Where(delegate (SyntaxNode candidate)
                { return candidate.IsKind(SyntaxKind.ClassDeclaration) || candidate.IsKind(SyntaxKind.StructDeclaration); }))
            {
                TypeDeclarationSyntax targetTypeDeclaration = (TypeDeclarationSyntax)node;
                if (String.Equals(targetTypeDeclaration.Identifier.Value, config.templateClassName))
                {
                    SyntaxNode root = tree.GetRoot();

                    SyntaxTree oldTree = null;
                    SemanticModel semanticModel;


                    // add warning about file being autogenerateds
                    {
                        SyntaxTriviaList leadingTrivia = root.GetLeadingTrivia();
                        leadingTrivia = leadingTrivia.Insert(0, SyntaxFactory.SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, "// NOTE: This file is auto-generated. DO NOT MAKE CHANGES HERE! They will be overwritten on rebuild."));
                        leadingTrivia = leadingTrivia.Insert(1, SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine));
                        leadingTrivia = leadingTrivia.Insert(2, SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine));
                        root = root.WithLeadingTrivia(leadingTrivia);
                    }

                    root = new SyntacticIdentifierRenamer(config.templateClassName, config.className).Visit(root);
                    string privateEnumEntryName = String.Concat(config.className, "Entry");
                    root = new SyntacticIdentifierRenamer(String.Concat(config.templateClassName, "Entry"), privateEnumEntryName).Visit(root);
                    foreach (KeyValuePair<string, string> optionalSyntacticRenames in config.syntacticRenames)
                    {
                        root = new SyntacticIdentifierRenamer(optionalSyntacticRenames.Key, optionalSyntacticRenames.Value).Visit(root);
                    }

                    // Template reduction I: remove Array/Object distinction.
                    // This is done by itself up front to eliminate duplicate declarations of certain internal
                    // types (such as Node) to reduce the number of errors, allowing compilation to generate a more complete
                    // semantic model, therefore enabling better semantic replacements, rather than syntactical replacements
                    // which are fragile.
                    root = new SelectFacetRewriter(Array.FindAll(config.facetAxes, delegate (FacetList candidate) { return String.Equals(candidate.axisTag, "Storage"); })).Visit(root);
                    // change Count integer width
                    if ((config.flags & CFlags.DowngradeCountToUint) != 0)
                    {
                        root = new NarrowCountWidthRewriter().Visit(root);
                    }
                    // widen rank information
                    if ((config.flags & CFlags.WidenInt) != 0)
                    {
                        root = new WidenIntWidthRewriter().Visit(root);
                    }
                    RebuildSemanticModel(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);

                    // TODO: disabled for now because class's interface list causes errors because inappropriate
                    // (non-facet) interfaces are still in tree.
                    {
                        List<Diagnostic> errors = new List<Diagnostic>(semanticModel.GetDiagnostics().Where(delegate (Diagnostic candidate) { return (candidate.Severity >= DiagnosticSeverity.Error) && !String.Equals(candidate.Id, "CS0535"); }));
                        string[] linesForErrors = root.ToFullString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                        if (errors.Count != 0)
                        {
                            foreach (Diagnostic error in errors)
                            {
                                Console.WriteLine("ERROR: {0}", error);
                                Console.WriteLine(linesForErrors[error.Location.GetLineSpan().StartLinePosition.Line]);
                            }
                            string temp = Path.GetTempFileName();
                            File.WriteAllText(temp, root.ToFullString());
                            string message = String.Format("Errors after storage reduction; these will prevent subsequent reductions from working properly (see output in \"{0}\")", temp);
                            Console.WriteLine(message);
                            if (!notepaded && ShowNotepadForErrors)
                            {
                                notepaded = true;
                                Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "notepad.exe"), temp);
                            }
                            throw new ArgumentException(message);
                        }
                    }

                    // apply constant substitution for members that are held constant for the current facets
                    if (Array.FindIndex(config.facetAxes, delegate (FacetList candidate) { return String.Equals(candidate.axisTag, "Feature"); }) >= 0)
                    {
                        root = new ConstSubstitutionRewriter(config.facetAxes, semanticModel).Visit(root);

                        // eliminate dead comparisons and branches as a result of constant substitution
                        EliminateDeadCode(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);
                    }

                    // Template reduction II: remove members that are not appropriate for the currently selected facets
                    root = new SelectFacetRewriter(config.facetAxes).Visit(root);

                    // recompile to detect errors due to now-missing variables
                    RebuildSemanticModel(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);

                    //Console.WriteLine("Errors for {0}:", config.className);
                    {
                        const bool ShowMessages = false;

                        List<Diagnostic> errors = new List<Diagnostic>();
                        string[] linesForErrors = null;
                        if (ShowMessages)
                        {
                            linesForErrors = root.ToFullString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                        }
                        string[] codes = new string[]
                        {
                                    // CS103 occurs due to stripping of inappropriate fields after template reduction
                                    "CS0103", // The name 'identifier' does not exist in the current context

                                    // CS1061 occurs due to stripping of inappropriate methods after template reduction
                                    "CS1061", // 'type' does not contain a definition for 'member' and no extension method 'name' accepting a first argument of type 'type' could be found

                                    // CS0131 occurs due to constant literal substitution for lhs (assignment target) of fields
                                    "CS0131", // The left-hand side of an assignment must be a variable, property or indexer
                        };
                        foreach (Diagnostic diagnostic in semanticModel.GetDiagnostics())
                        {
                            bool added;
                            if (added = (Array.IndexOf(codes, diagnostic.Id) >= 0)) // reference to something nonexistent in current context
                            {
                                errors.Add(diagnostic);
                            }
                            if (ShowMessages)
                            {
                                Console.WriteLine("{0}{1}({2}): {3}",
                                    added ? "*" : " ",
                                    diagnostic.Id,
                                    diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                                    linesForErrors[diagnostic.Location.GetLineSpan().StartLinePosition.Line]);
                            }
                        }
                        if (ShowMessages)
                        {
                            Console.WriteLine("---");
                        }

                        // remove statements containing references to missing variables
                        root = new RemoveBrokenStatementsRewriter(errors).Visit(root);

                        // clean up dead branches and empty blocks/semicolons after elimination of unneded facets
                        EliminateDeadCode(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);

                        // remove now-unreferenced variables
                        bool changed;
                        do
                        {
                            RebuildSemanticModel(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);
                            CollectUnusedVariablesVisitor unusedVariables = new CollectUnusedVariablesVisitor(semanticModel);
                            unusedVariables.Visit(root);
                            RemoveUnusedVariablesRewriter removeUnusuedVariables = new RemoveUnusedVariablesRewriter(semanticModel, unusedVariables.Variables);
                            root = removeUnusuedVariables.Visit(root);
                            changed = removeUnusuedVariables.Changed;
                        } while (changed);

                        // clean empty blocks/semicolons after unused variable removal
                        EliminateDeadCode(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);
                    }

                    // eliminate no-op array indexing syntax from the "Object" versions
                    RebuildSemanticModel(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);
                    if (Array.FindIndex(config.facetAxes,
                        delegate (FacetList candidate)
                        {
                            return String.Equals(candidate.axisTag, "Storage") && (candidate.facets.Length == 1) && String.Equals(candidate.facets[0], "Object");
                        }) >= 0)
                    {
                        CollectArrayIndirectionTypeSubstitution substitutions = new CollectArrayIndirectionTypeSubstitution(semanticModel);
                        substitutions.Visit(root);

                        root = new EliminateArrayIndirectionRewriter(semanticModel, substitutions.Replace).Visit(root);
                    }
                    else
                    {
                        // add unsafe array references for array versions

                        // TODO: disabled - this actually degrades performance
                        // IF ENABLING, UNCOMMENT "Node2" struct, "nodes2" array, and references in "EnsureFree"
                        //root = new RemoveFieldsPreparingForStructHoistRewriter(config.className, semanticModel).Visit(root);
                        //root = new EnableArrayFixationRewriter().Visit(root);
                        //root = new HoistNodeStructRewriter(config.className).Visit(root);
                    }

                    // meld private enumeration entry struct into shared enumeration entry struct
                    {
                        Debug.Assert(privateEnumEntryName.StartsWith(config.className));
                        string targetEnumEntryName = String.Concat("Entry", config.specialization);
                        root = new EnumEntryMeldRewriter(privateEnumEntryName, targetEnumEntryName).Visit(root);
                    }

                    // list final errors
                    RebuildSemanticModel(ref oldTree, ref tree, ref root, ref compilation, out semanticModel);
                    {
                        List<Diagnostic> errors = new List<Diagnostic>(semanticModel.GetDiagnostics().Where(delegate (Diagnostic candidate) { return candidate.Severity >= DiagnosticSeverity.Error; }));
                        if (errors.Count != 0)
                        {
                            string[] linesForErrors = root.ToFullString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                            foreach (Diagnostic diagnostic in errors)
                            {
                                Console.WriteLine("{0}({1}): {2}",
                                    // list final errors
                                    diagnostic.Id,
                                    diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                                    diagnostic);
                                Console.WriteLine(linesForErrors[diagnostic.Location.GetLineSpan().StartLinePosition.Line]);
                            }
                            string temp = Path.GetTempFileName();
                            File.WriteAllText(temp, root.ToFullString());
                            string message = String.Format("See output in: \"{0}\"", temp);
                            Console.WriteLine(message);
                            if (!notepaded && ShowNotepadForErrors)
                            {
                                notepaded = true;
                                Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "notepad.exe"), temp);
                            }
                        }
                    }

                    if (config.retainInCompilation)
                    {
                        compilationReplacement = compilation;
                    }

                    File.WriteAllText(targetFilePath, root.ToFullString());
                }
            }
        }

        private static void UpdateFile(string path, string className, SyntaxNode tree)
        {
            string newText = tree.ToFullString();
            string filePath = Path.Combine(path, String.Format("{0}.cs", className));
            File.WriteAllText(filePath, newText);
        }

        private static bool NeedsUpdate(
            Config config,
            Project interfacesProject,
            Project targetProject,
            string templateSourceFilePath,
            string targetPath)
        {
            DateTime sourceMostRecentStamp = DateTime.MinValue;
            DateTime targetStamp = DateTime.MinValue;

            {
                DateTime templateStamp = File.GetLastWriteTime(templateSourceFilePath);
                if (sourceMostRecentStamp < templateStamp)
                {
                    sourceMostRecentStamp = templateStamp;
                }
            }

            foreach (Project project in new Project[] { interfacesProject, targetProject })
            {
                foreach (Document document in project.Documents)
                {
                    string filePath = document.FilePath;

                    DateTime fileStamp = File.GetLastWriteTime(filePath);
                    if (sourceMostRecentStamp < fileStamp)
                    {
                        sourceMostRecentStamp = fileStamp;
                    }
                }
            }

            string thisPath = Assembly.GetAssembly(typeof(Program)).Location;
            DateTime thisStamp = File.GetLastWriteTime(thisPath);
            if (sourceMostRecentStamp < thisStamp)
            {
                sourceMostRecentStamp = thisStamp;
            }

            if (File.Exists(targetPath))
            {
                targetStamp = File.GetLastWriteTime(targetPath);
            }

            return targetStamp <= sourceMostRecentStamp;
        }

        private static string FindFirstFile(string path, string extension)
        {
            foreach (string file in Directory.GetFiles(path, String.Concat("*", extension)))
            {
                return file;
            }
            throw new ArgumentException();
        }

        public static int Main(string[] args)
        {
            try
            {
                if (args.Length != 3)
                {
                    throw new ArgumentException();
                }

                string solutionBasePath = args[0];
                string interfacesProjectBasePath = args[1];
                string targetProjectName = args[2];

                string targetSolutionFilePath = FindFirstFile(solutionBasePath, ".sln");

                string interfacesProjectName = Path.GetFileName(interfacesProjectBasePath);
                string interfacesSolutionFilePath = FindFirstFile(Path.GetDirectoryName(interfacesProjectBasePath), ".sln");

                string templateProjectBasePath = Path.Combine(solutionBasePath, "Template"); // TODO: hardcoded

                string[] doNotUnload;
                Config[] configs = Config.ReadConfigs(Path.Combine(solutionBasePath, targetProjectName, "transform.xml"), out doNotUnload);



                Project interfacesProject, targetProject;
                Stopwatch loadTime = Stopwatch.StartNew();
                {
                    Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();

                    // load interface project (required for checking source time stamps)
                    Solution interfacesSolution = workspace.OpenSolutionAsync(interfacesSolutionFilePath).Result;
                    interfacesProject = interfacesSolution.Projects.First(delegate (Project p) { return String.Equals(p.Name, interfacesProjectName); });

                    // load target project
                    Solution targetSolution = workspace.OpenSolutionAsync(targetSolutionFilePath).Result;
                    targetProject = targetSolution.Projects.First(delegate (Project p) { return String.Equals(p.Name, targetProjectName); });
                    // remove all preexisting specializations since we'll be regenerating them and don't want multiply-defined errors
                    foreach (DocumentId documentId in targetProject.DocumentIds)
                    {
                        if (Array.IndexOf(doNotUnload, targetProject.GetDocument(documentId).Name) < 0)
                        {
                            targetProject = targetProject.RemoveDocument(documentId);
                        }
                    }
                }
                loadTime.Stop();
                Console.WriteLine("Initialization time: {0:F1} seconds", loadTime.ElapsedMilliseconds / 1000d);



                Compilation compilation = null;

                foreach (Config config in configs)
                {
                    string templateSourceFilePath = Path.Combine(templateProjectBasePath, Path.ChangeExtension(config.templateClassName, ".cs"));
                    string targetFilePath = Path.Combine(solutionBasePath, targetProjectName, "Generated", Path.ChangeExtension(config.className, ".cs"));

                    bool needsUpdate = NeedsUpdate(config, interfacesProject, targetProject, templateSourceFilePath, targetFilePath);

                    ConsoleColor oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(config.className);
                    Console.ForegroundColor = oldColor;
                    if (!needsUpdate)
                    {
                        Console.Write(" - skipped (up to date)");
                    }
                    Console.WriteLine();

                    if (needsUpdate)
                    {
                        // lazy init
                        if (compilation == null)
                        {
                            compilation = targetProject.GetCompilationAsync().Result;
                        }

                        Compilation compilationReplacement;
                        Generate(config, compilation, out compilationReplacement, templateSourceFilePath, targetFilePath);
                        if (compilationReplacement != null)
                        {
                            compilation = compilationReplacement;
                        }
                    }
                }



                Console.WriteLine("Finished");
                if (Debugger.IsAttached)
                {
                    Console.ReadLine();
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
    }
}
