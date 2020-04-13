using LINQPad.Extensibility.DataContext;
using Overby.Extensions.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static LINQPad.Extensibility.DataContext.ExplorerIcon;
using static LINQPad.Extensibility.DataContext.ExplorerItemKind;
using static Overby.LINQPad.FileDriver.ValueTyper;

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

            BuildAssembly(files, assemblyToBuild, ref nameSpace, ref typeName, out var explorerItems);

            return explorerItems.OrderBy(x => x.Text).ToList();
        }

        private void BuildAssembly(IEnumerable<(FileInfo file, string csid)> files, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName, out List<ExplorerItem> explorerItems)
        {
            explorerItems = new List<ExplorerItem>();
            var source = GenerateCode(files, nameSpace, typeName,explorerItems);
            Compile(source, assemblyToBuild.CodeBase);
        }

        static string ToIdentifier(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("string was empty or whitespace", nameof(s));

            s = Regex.Replace(s, @"[^a-zA-Z0-9_]+", "_");

            if (char.IsDigit(s[0]))
                return "_" + s;

            return s;
        }

        const string _cacheFolder = ".5f969db29db8fe4dbd4738bf85c80219";

        Dictionary<string,BestType> GetBestTypes(FileInfo file, DirectoryInfo cacheDir)
        {
            var cacheFile = new FileInfo(Path.Combine(cacheDir.FullName, "besttypes.csv"));

            if (cacheFile.Exists)
            {
                using var textReader = new StreamReader(cacheFile.FullName);
                return textReader.ReadCsvWithHeader().ToDictionary(
                    x => x["key"],
                    x => (BestType)Enum.Parse(typeof(BestType), x["type"]));
            }

            using var reader = new StreamReader(file.FullName);
            var bestTypes = DetermineBestTypes(
                reader.ReadCsvWithHeader().Select(rec => rec.Keys.Select(k => (k, rec[k]))));

            // save code for next time
            using var writer = new StreamWriter(cacheFile.FullName);
            var csv = new CsvWriter(writer);
            csv.AddRecord("key", "type");
            foreach (var pair in bestTypes)
                csv.AddRecord(pair.Key, pair.Value);

            return bestTypes;
        }

        string GenerateCode(IEnumerable<(FileInfo file, string fileClassName)> files, string nameSpace, string typeName, List<ExplorerItem> explorerItems)
        {
            var fileGens = files.AsParallel().Select(GenerateCodeForFile).ToArray();
            explorerItems.AddRange(fileGens.Select(x => x.explorerItem));

            (string types, string property, ExplorerItem explorerItem) GenerateCodeForFile((FileInfo file, string fileClassName) t)
            {
                var (file, fileClassName) = t;

                var filehash = GetFileHash(file.FullName);

                var cacheDir = new DirectoryInfo(
                    Path.Combine(file.DirectoryName, _cacheFolder, filehash));

                if (!cacheDir.Exists) 
                    cacheDir.Create();

                var explorerFields = new List<ExplorerItem>();
                var bestTypes = GetBestTypes(file, cacheDir);
                var recordClass = GenerateRecordClass();
                var fileProperty = GenerateFileProperty();

                var explorerItem = new ExplorerItem(fileClassName, ExplorerItemKind.QueryableObject,
                        ExplorerIcon.Table)
                {
                    IsEnumerable = true,
                    ToolTipText = file.FullName,
                    Children = explorerFields
                };

                return (recordClass, fileProperty, explorerItem);

                string GenerateFileProperty()
                {
                    return $@"
        public IEnumerable<{nameSpace}.RecordTypes.{fileClassName}> {fileClassName} 
        {{
            get
            {{
                using var streamReader = new StreamReader({nameSpace}.FilePaths.{fileClassName});
                var csvRecords = Overby.Extensions.Text.CsvParsingExtensions.ReadCsvWithHeader(streamReader);
                foreach(var record in csvRecords)
                    yield return {nameSpace}.RecordTypes.{fileClassName}.Create(record);
            }}
        }}";
                }

                string GenerateRecordClass()
                {
                    var recordProperties =
                        (from bt in bestTypes
                         let propName = ToIdentifier(bt.Key)
                         let csPropType = GetTypeRef(bt.Value)
                         let propDef = $@"
        public {csPropType} {propName} {{ get; set; }}"
                         let rawValueExpression = $"csvRecord[{VerbatimString(bt.Key)}]"
                         let parser = GetParserCode(bt.Value, rawValueExpression)

                         let propAssignment = $@"
                {propName} = {parser}"

                         select (propDef, propAssignment, propName, csPropType)).ToArray();

                    explorerFields.AddRange(
                        from rp in recordProperties
                        select new ExplorerItem(rp.propName, Property, Column)
                        {
                            // show the type when hovering
                            ToolTipText = rp.csPropType
                        });

                    return $@"
    public class {fileClassName}
    {{
        // record properties
        {string.Join(NL, recordProperties.Select(t => t.propDef))}

        // factory method
        public static {fileClassName} Create(Overby.Extensions.Text.CsvRecord csvRecord)
        {{
            return new {nameSpace}.RecordTypes.{fileClassName}
            {{
                {string.Join("," + NL, recordProperties.Select(t => t.propAssignment))}
            }};
        }}
    }}";
                }
            }

            var properties = string.Join(NL, fileGens.Select(x => x.property));
            var types = string.Join(NL, fileGens.Select(x => x.types));

            var filePaths = string.Join(NL, files.Select(f =>
                $"public static string {f.fileClassName} => {VerbatimString(f.file.FullName)};"));

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

    public static class FilePaths
    {{
        {filePaths}
    }}
}}

namespace {nameSpace}.RecordTypes
{{
    {types}
}}

";

            return source;
        }

        private string GetFileHash(string fullName)
        {
            using var stream = File.OpenRead(fullName);
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(stream);
            return string.Concat(hash.Select(b => b.ToString("x2")));
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