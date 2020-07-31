using LINQPad;
using LINQPad.Extensibility.DataContext;
using Overby.Extensions.Text;
using Overby.LINQPad.FileDriver.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using static Overby.LINQPad.FileDriver.CodeGenConstants;

namespace Overby.LINQPad.FileDriver
{
    public class DynamicDriver : DynamicDataContextDriver
    {
        static DynamicDriver()
        {
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:
#if DEBUG    
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception.StackTrace.Contains("Overby.LINQPad.FileDriver"))
                    Debugger.Launch();
            };
#endif
        }

        public override string Name => "Overby File Driver";

        public override string Author => "Ronnie Overby";

        public override string GetConnectionDescription(IConnectionInfo cxInfo) =>
            cxInfo.DisplayName ?? new ConnectionProperties(cxInfo).DataDirectoryPath;

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions) =>
            new ConnectionDialog(cxInfo).ShowDialog() == true;




        public override List<ExplorerItem> GetSchemaAndBuildAssembly(IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
#if DEBUG
            //File.Delete(@"C:\Users\ronnie.overby\Desktop\dbug\4c5b497501bc4040a0c41dbf47805704");
            Debugger.Launch();
#endif

            var cxProps = new ConnectionProperties(cxInfo);
            var root = new DirectoryInfo(cxProps.DataDirectoryPath);
            typeName = $"{SchemaNameSpace}.{root.Name.ToIdentifier()}.{SchemaClassName}";
            var rootConfig = RootConfig.LoadRootConfig(root);
            var schema = FileVisitor.VisitRoot(root, rootConfig);
            rootConfig.Save(root);
            var cSharpSourceCode = GenerateCode(ref nameSpace, ref typeName, schema, root);

#if DEBUG
            File.WriteAllText(root.Parent.GetFile("the source codez.cs").FullName, cSharpSourceCode);
#endif

            Compile(cSharpSourceCode, assemblyToBuild.CodeBase);

            return schema;
        }


        private string GenerateCode(ref string nameSpace, ref string fullContextTypeName, List<ExplorerItem> schema, DirectoryInfo root)
        {
            var writer = new StringWriter();

            using (writer.NameSpace(nameSpace))
            {

             

                // generate schema types
                using (writer.NameSpace(SchemaNameSpace))
                    GenSchemaTypes(schema, writer, root);
            }

            return writer.ToString();
        }

        private void GenSchemaTypes(List<ExplorerItem> schema, StringWriter writer, DirectoryInfo root)
        {
            var flatSchema = schema.Flatten().ToArray();

            // alias all file/folder namespaces
            // this avoids conflicts in folder schema types
            // when referencing reader/record types
            // in namespaces that are named same as members
            using (writer.Region("File Namespace Aliases"))
                foreach (var tag in flatSchema.GetTags<IRefFileSystemInfo>())
                {
                    var nsFile = tag.FileSystemInfo.GetNameSpace(root);
                    var alias = tag.FileSystemInfo.FullName.UniqueIdentifier();
                    writer.WriteLine($"using {alias} = {nsFile};");
                }

            Write(root, schema);

            foreach (var (item, tag) in flatSchema.WithTag<FolderTag>())
                Write(tag.Folder, item.Children);

            void Write(DirectoryInfo folder, IList<ExplorerItem> items)
            {
                if (items.Count == 0)
                    return;

                var fileItems = items.WithTag<FileTag>().ToArray();

                using var _1 = writer.Region(folder);
                using var _2 = writer.NameSpace(folder.GetNameSpace(root));

                // write file records/readers
                foreach (var (item, fileTag) in fileItems)
                {
                    var fileConfig = fileTag.FileConfig;
                    var (WriteRecordMembers, WriteEnumeratorImplementation) =
                        fileTag.CodeGenerator.GetCodeGenerators(fileConfig);

                    using (writer.Region(fileTag.File))
                    using (writer.NameSpace(fileTag.File.GetNameIdentifier()))
                    {
                        // record class
                        writer.MemberComment("Record type for " + fileTag.File.FullName);
                        using (writer.Brackets("public class " + RecordClassName))
                            WriteRecordMembers(writer);

                        // reader class
                        writer.MemberComment("Reader for " + fileTag.File.FullName);
                        using (writer.Brackets($"public class {ReaderClassName} : {IEnumerable(RecordClassName)}"))
                        {
                            // file path property
                            writer.MemberComment("Path to " + fileTag.File.FullName);
                            writer.WriteLine(
                                $"public string {ReaderFilePathPropertyName} =>{fileTag.File.FullName.ToLiteral()};");

                            // pass file config
                            var userConfigType = CodeGen.GetTypeRef(fileConfig.GetUserConfigType());
                            writer.MemberComment("config comment");
                            using (writer.Brackets($"public void Configure(System.Action<{userConfigType}> configure)"))
                            {
                                // get config 
                                writer.WriteLine($@"var fileConfig =
    Overby.LINQPad.FileDriver.Configuration.RuntimeConfiguration
        .GetRootConfig({root.FullName.ToLiteral()})
        .GetFileConfig({fileConfig.RelativePath.ToLiteral()});

    var userConfig = ({userConfigType})fileConfig.GetUserConfig();
    configure(userConfig);
    fileConfig.UpdateFromUserConfig(userConfig);");
                            }

                            // GetEnumerator methods
                            writer.MemberComment("Reads records from " + fileTag.File.FullName);
                            using (writer.Brackets(
                                $"public System.Collections.Generic.IEnumerator<{RecordClassName}> GetEnumerator()"))
                                WriteEnumeratorImplementation(writer);

                            writer.WriteLine($"System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();");
                        }
                    }
                }

                // write folder schema types
                writer.MemberComment(folder.FullName);
                using var _3 = writer.Brackets("public class " + SchemaClassName);

                using (writer.Region("File Members"))
                    foreach (var (item, fileTag) in fileItems)
                    {
                        var fileIdentifier = fileTag.File.GetNameIdentifier();
                        var alias = fileTag.File.FullName.UniqueIdentifier();
                        var recordType = $"{alias}.{RecordClassName}";
                        var memberType = $"{alias}.{ReaderClassName}";
                        var readCall = $"new {memberType}()";

                        writer.MemberComment(fileTag.File.FullName);
                        writer.WriteLine(
                            $"public {memberType} {fileIdentifier} => {readCall};");
                    }

                using (writer.Region("Sub Folder Members"))
                {
                    foreach (var (subFolderItem, subFolderTag) in items.WithTag<FolderTag>())
                    {
                        var alias = subFolderTag.Folder.FullName.UniqueIdentifier();
                        var schemaType = $"{alias}.{SchemaClassName}";
                        var folderId = subFolderTag.Folder.GetNameIdentifier();
                        writer.MemberComment(subFolderTag.Folder.FullName);
                        writer.WriteLine(
                            $"public {schemaType} {folderId} {{ get; }} = new {schemaType}();");
                    }
                }

                // best place to set file children explorer items                
                // all code gen has taken place, the file config is stable
                foreach (var (item, tag) in fileItems)
                    item.Children = tag.FileConfig.GetFileChildItems();
            }
        }

        static void Compile(string cSharpSourceCode, string outputFile)
        {
            string[] assembliesToReference =
#if NETCORE
            // GetCoreFxReferenceAssemblies is helper method that returns the full set of .NET Core reference assemblies.
            // (There are more than 100 of them.)
            GetCoreFxReferenceAssemblies();
#else
            // .NET Framework - here's how to get the basic Framework assemblies:
            new[]
            {
                typeof (int).Assembly.Location,            // mscorlib
				typeof (Uri).Assembly.Location,            // System
				typeof (XmlConvert).Assembly.Location,     // System.Xml
				typeof (Enumerable).Assembly.Location,     // System.Core
				typeof (DataSet).Assembly.Location         // System.Data
			};
#endif
            assembliesToReference = assembliesToReference.Concat(new[]
            {
                typeof(CsvRecord).Assembly.Location,
                typeof(DynamicDriver).Assembly.Location,
            }).ToArray();

            // CompileSource is a static helper method to compile C# source code using LINQPad's built-in Roslyn libraries.
            // If you prefer, you can add a NuGet reference to the Roslyn libraries and use them directly.
            var compileResult = CompileSource(new CompilationInput
            {
                FilePathsToReference = assembliesToReference,
                OutputPath = outputFile,
                SourceCode = new[] { cSharpSourceCode }
            });

            if (compileResult.Errors.Length > 0)
                throw new Exception("Cannot compile typed context: " + compileResult.Errors[0]);
        }

        public override void OnQueryFinishing(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            RefreshConnectionIfNeeded(context, cxInfo);

            base.OnQueryFinishing(cxInfo, context, executionManager);
        }

        void RefreshConnectionIfNeeded(dynamic context, IConnectionInfo cxInfo)
        {
            // using dynamic for the context
            // because it's generated at runtime

            // using reflection for the call to cxInfo.ForceRefresh()
            // because ForceRefresh() is new and older
            // version of linqpad won't have the method
            // and ForceRefresh() is a default interface implementation
            // which cannot be called by the DLR

            if (RuntimeConfiguration.Changes)
            {
                RuntimeConfiguration.SaveRootConfig();

#if NETCORE
                if (LINQPadVersion >= new Version(6, 9, 2))
                    cxInfo.ForceRefresh();
                else
                    DisplayRefreshMessage();
#else
            DisplayRefreshMessage();
#endif

                static void DisplayRefreshMessage() =>
                     "Refresh the connection to apply changes.".Dump();
            }
        }
    }
}