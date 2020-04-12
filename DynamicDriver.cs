using LINQPad.Extensibility.DataContext;
using Overby.Extensions.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Overby.LINQPad.FileDriver
{
    public class DynamicDriver : DynamicDataContextDriver
    {
        static readonly string NL = Environment.NewLine;

        static DynamicDriver()
        {
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception.StackTrace.Contains("Overby.LINQPad.FileDriver"))
                    Debugger.Launch();
            };
        }

        public override string Name => "Overby File Driver";

        public override string Author => "Ronnie Overby";

        public override string GetConnectionDescription(IConnectionInfo cxInfo) =>
            cxInfo.DisplayName ?? new ConnectionProperties(cxInfo).DataDirectoryPath;

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions) =>
            new ConnectionDialog(cxInfo).ShowDialog() == true;

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(
            IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
            var cxProps = new ConnectionProperties(cxInfo);
            var root = new DirectoryInfo(cxProps.DataDirectoryPath);

            var files = root.EnumerateFiles("*.csv")
                    .Select(file => (file, csid: ToIdentifier(file.Name)));

            BuildAssembly(files, assemblyToBuild, ref nameSpace, ref typeName);

            return files
                .Select(f => new ExplorerItem(f.csid, ExplorerItemKind.QueryableObject, ExplorerIcon.Table))
                .ToList();
        }

        private void BuildAssembly(IEnumerable<(FileInfo file, string csid)> files, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
            var source = GenerateCode(files, nameSpace, typeName);
            Compile(source, assemblyToBuild.CodeBase);
        }

        static string ToIdentifier(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("string was empty or whitespace", nameof(s));

            s = string.Concat(s.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)));
            s = Regex.Replace(s, @"\s+", "_");

            if (char.IsDigit(s[0]))
                s = "_" + s;

            return s;
        }

        string GenerateCode(IEnumerable<(FileInfo file, string csid)> files, string nameSpace, string typeName)
        {
            var fileGens = files.Select(GenerateCodeForFile).ToArray();

            (string types, string property) GenerateCodeForFile((FileInfo file, string csid) t)
            {
                var (file, fileClassName) = t;

                using var reader = new StreamReader(file.FullName);
                var bestTypes = ValueTyper.DetermineBestTypes(
                    reader.ReadCsvWithHeader().Select(rec => rec.Keys.Select(k => (k, rec[k]))));

                


                var recordClass = GenerateRecordClass();
                var fileProperty = GenerateFileProperty();
                return (recordClass, fileProperty);

                string GenerateFileProperty() 
                {
                    return $@"
        public IEnumerable<{fileClassName}> {fileClassName} 
        {{
            get
            {{
                using var streamReader = new StreamReader({VerbatimString(file.FullName)});
                var csvRecords = Overby.Extensions.Text.CsvParsingExtensions.ReadCsvWithHeader(streamReader);
                foreach(var record in csvRecords)
                    yield return {nameSpace}.{fileClassName}.Create(record);
            }}
        }}";
                }

                string GenerateRecordClass()
                {
                    var recordProperties =
                        from bt in bestTypes
                        let propName = ToIdentifier(bt.Key)
                        let csPropType = ValueTyper.GetTypeRef(bt.Value)
                        let propDef = $@"
        public {csPropType} {propName} {{ get; set; }}"
                        let rawValueExpression = $"csvRecord[{VerbatimString(bt.Key)}]"
                        let parser = ValueTyper.GetParserCode(bt.Value, rawValueExpression)

                        let propAssignment = $@"
            {propName} = {parser}"
                        select (propDef, propAssignment);

                    return $@"
    public class {fileClassName}
    {{
        {string.Join(NL, recordProperties.Select(t => t.propDef))}
        public static {fileClassName} Create(Overby.Extensions.Text.CsvRecord csvRecord)
        {{
            return new {nameSpace}.{fileClassName}
            {{
                {string.Join("," + NL, recordProperties.Select(t => t.propAssignment))}
            }};
            
        }}
    }}";
                }
            }

            var properties = string.Join(Environment.NewLine, fileGens.Select(x => x.property));
            var types = string.Join(Environment.NewLine, fileGens.Select(x => x.types));

            string source = $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace {nameSpace}
{{
    // The main typed data class. The user's queries subclass this, so they have easy access to all its members.
	public class {typeName}
	{{
        // One for each file
        {properties}		
	}}

    {types}
}}";

            return source;
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

            assembliesToReference = assembliesToReference.Concat(new[] {
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

        static string VerbatimString(string s) => $"@\"{s.Replace("\"", "\"\"")}\"";
    }
}