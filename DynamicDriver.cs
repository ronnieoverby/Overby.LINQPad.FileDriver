using LINQPad.Extensibility.DataContext;
using Overby.Extensions.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using static Overby.LINQPad.FileDriver.CodeGen;

namespace Overby.LINQPad.FileDriver
{
    public class DynamicDriver : DynamicDataContextDriver
    {

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
            var metrics = new Metrics();
            var stopwatch = Stopwatch.StartNew();

            var cxProps = new ConnectionProperties(cxInfo);
            var root = new DirectoryInfo(cxProps.DataDirectoryPath);

            var files = root.EnumerateFiles("*.csv")
                    .Select(file => (file, csid: file.Name.ToIdentifier()));

            BuildAssembly(files, assemblyToBuild, ref nameSpace, ref typeName, out var explorerItems,metrics);
            explorerItems = explorerItems.OrderBy(x => x.Text).ToList();
            
            metrics.GetSchemaAndBuildAssemblyDuration = stopwatch.Elapsed;
            return explorerItems;
        }

        private void BuildAssembly(IEnumerable<(FileInfo file, string csid)> files, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName, out List<ExplorerItem> explorerItems, Metrics metrics)
        {
            var stopwatch = Stopwatch.StartNew();
            explorerItems = new List<ExplorerItem>();
            var source = GenerateCode(files, nameSpace, typeName, explorerItems, metrics);
            Compile(source, assemblyToBuild.CodeBase);
            metrics.BuildAssemblyDuration = stopwatch.Elapsed;
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


    }
}