using LINQPad.Extensibility.DataContext;
using Overby.Extensions.Text;
using Overby.LINQPad.FileDriver.TypeInference;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static LINQPad.Extensibility.DataContext.ExplorerIcon;
using static LINQPad.Extensibility.DataContext.ExplorerItemKind;
using static Overby.LINQPad.FileDriver.TypeInference.BestType;
using static Overby.LINQPad.FileDriver.TypeInference.TypeInferrer;
using static System.Environment;

namespace Overby.LINQPad.FileDriver
{
    internal static class CodeGen
    {
        private const string _cacheFolder = ".5f969db29db8fe4dbd4738bf85c80219";

        public static string GetTypeRef(BestType bestType) => bestType switch
        {
            String => "System.String",
            BigInt => "System.Numerics.BigInteger",
            NullableBigInt => "System.Numerics.BigInteger?",
            Bool => "System.Boolean",
            NullableBool => "System.Boolean?",
            Char => "System.Char",
            NullableChar => "System.Char?",
            DateTime => "System.DateTime",
            NullableDateTime => "System.DateTime?",
            DateTimeOffset => "System.DateTimeOffset",
            NullableDateTimeOffset => "System.DateTimeOffset?",
            Decimal => "System.Decimal",
            NullableDecimal => "System.Decimal?",
            Double => "System.Double",
            NullableDouble => "System.Double?",
            Guid => "System.Guid",
            NullableGuid => "System.Guid?",
            Int64 => "System.Int64",
            NullableInt64 => "System.Int64?",
            Int32 => "System.Int32",
            NullableInt32 => "System.Int32?",
            Int16 => "System.Int16",
            NullableInt16 => "System.Int16?",
            Byte => "System.Byte",
            NullableByte => "System.Byte?",
            Timespan => "System.TimeSpan",
            NullableTimespan => "System.TimeSpan?",
            _ => throw new System.NotImplementedException("missing handler for " + bestType),
        };

        public static string GetParserCode(BestType bestType, string rawValueExpression) => bestType switch
        {
            String => rawValueExpression,
            BigInt => $"System.Numerics.BigInteger.Parse({rawValueExpression})",
            NullableBigInt => $"string.IsNullOrWhiteSpace({rawValueExpression}) ? default(System.Numerics.BigInteger?) : System.Numerics.BigInteger.Parse({rawValueExpression})",
            Bool => $"Overby.LINQPad.FileDriver.Parsers.ParseBool({rawValueExpression}, new[] {{ bool.TrueString, \"t\", \"y\", \"yes\", \"1\", \"on\" }}, new[] {{ bool.FalseString, \"f\", \"n\", \"no\", \"0\", \"off\" }})",
            NullableBool => $"string.IsNullOrWhiteSpace({rawValueExpression}) ? default(System.Boolean?) : Overby.LINQPad.FileDriver.Parsers.ParseBool({rawValueExpression}, new[] {{ bool.TrueString, \"t\", \"y\", \"yes\", \"1\", \"on\" }}, new[] {{ bool.FalseString, \"f\", \"n\", \"no\", \"0\", \"off\" }})",
            Char => $"System.Char.Parse({ rawValueExpression })",
            NullableChar => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Char?) : System.Char.Parse({ rawValueExpression })",
            DateTime => $"System.DateTime.Parse({ rawValueExpression })",
            NullableDateTime => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.DateTime?) : System.DateTime.Parse({ rawValueExpression })",
            DateTimeOffset => $"System.DateTimeOffset.Parse({ rawValueExpression })",
            NullableDateTimeOffset => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.DateTimeOffset?) : System.DateTimeOffset.Parse({ rawValueExpression })",
            Decimal => $"System.Decimal.Parse({ rawValueExpression })",
            NullableDecimal => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Decimal?) : System.Decimal.Parse({ rawValueExpression })",
            Double => $"System.Double.Parse({ rawValueExpression })",
            NullableDouble => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Double?) : System.Double.Parse({ rawValueExpression })",
            Guid => $"System.Guid.Parse({ rawValueExpression })",
            NullableGuid => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Guid?) : System.Guid.Parse({ rawValueExpression })",
            Int64 => $"System.Int64.Parse({ rawValueExpression })",
            NullableInt64 => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Int64?) : System.Int64.Parse({ rawValueExpression })",
            Int32 => $"System.Int32.Parse({ rawValueExpression })",
            NullableInt32 => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Int32?) : System.Int32.Parse({ rawValueExpression })",
            Int16 => $"System.Int16.Parse({ rawValueExpression })",
            NullableInt16 => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Int16?) : System.Int16.Parse({ rawValueExpression })",
            Byte => $"System.Byte.Parse({ rawValueExpression })",
            NullableByte => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Byte?) : System.Byte.Parse({ rawValueExpression })",
            Timespan => $"System.TimeSpan.Parse({ rawValueExpression })",
            NullableTimespan => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.TimeSpan?) : System.TimeSpan.Parse({ rawValueExpression })",
            _ => throw new System.NotImplementedException("missing parser for " + bestType),
        };

        internal static string GenerateCode(
            IEnumerable<(FileInfo file, string fileClassName)> files, 
            string nameSpace,
            string typeName,
            List<ExplorerItem> explorerItems,
            Metrics metrics)
        {
            var stopwatch = Stopwatch.StartNew();
            var fileGens = files.AsParallel().Select(t => 
                GenerateCodeForFile(nameSpace, t.file, t.fileClassName)).ToArray();

            explorerItems.AddRange(fileGens.Select(x => x.explorerItem));
            var properties = fileGens.Select(x => x.property).NewLineJoin();
            var types = fileGens.Select(x => x.types).NewLineJoin();

            var filePaths = files.Select(f =>
                $"public static string {f.fileClassName} => {f.file.FullName.ToVerbatimString()};")
                .NewLineJoin();

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
            metrics.GenerateCodeDuration = stopwatch.Elapsed;
            return source;
        }

        private static (string types, string property, ExplorerItem explorerItem) GenerateCodeForFile(string nameSpace, FileInfo file, string fileClassName)
        {
            var filehash = file.FullName.FileMD5();

            var cacheDir = new DirectoryInfo(
                Path.Combine(file.DirectoryName, _cacheFolder, filehash));

            if (!cacheDir.Exists)
                cacheDir.Create();

            var explorerFields = new List<ExplorerItem>();
            var bestTypes = GetBestTypes(file, cacheDir);
            var recordClass = GenerateRecordClass();
            var fileProperty = GenerateFileProperty();

            var explorerItem = new ExplorerItem(file.Name, QueryableObject, Table)
            {
                IsEnumerable = true,
                ToolTipText = file.FullName,
                Children = explorerFields,
                DragText = fileClassName
            };

            return (recordClass, fileProperty, explorerItem);

            string GenerateFileProperty()
            {
                return $@"
        public IEnumerable<{nameSpace}.RecordTypes.{fileClassName}> {fileClassName} 
        {{
            get
            {{
                using(var streamReader = new StreamReader({nameSpace}.FilePaths.{fileClassName}))
                {{
                    var csvRecords = Overby.Extensions.Text.CsvParsingExtensions.ReadCsvWithHeader(streamReader);
                    foreach(var record in csvRecords)
                        yield return {nameSpace}.RecordTypes.{fileClassName}.Create(record);
                }}
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
                     let rawValueExpression = $"csvRecord[{bt.Key.ToVerbatimString()}]"
                     let parser = GetParserCode(bt.Value, rawValueExpression)

                     let propAssignment = $@"
                {propName} = {parser}"

                     select (propDef, propAssignment, propName, csPropType, rawFieldKey: bt.Key)).ToArray();

                // add the child columns/fields to the explorer tree
                explorerFields.AddRange(
                    from rp in recordProperties
                    select new ExplorerItem(rp.rawFieldKey, Property, Column)
                    {
                            // show the type when hovering
                            ToolTipText = rp.csPropType,
                        DragText = rp.propName
                    });

                return $@"
    public class {fileClassName}
    {{
        // record properties
        {recordProperties.Select(t => t.propDef).NewLineJoin()}

        // factory method
        public static {fileClassName} Create(Overby.Extensions.Text.CsvRecord csvRecord)
        {{
            return new {nameSpace}.RecordTypes.{fileClassName}
            {{
                {recordProperties.Select(t => t.propAssignment).StringJoin("," + NewLine)}
            }};
        }}
    }}";
            }
        }


        private static Dictionary<string, BestType> GetBestTypes(FileInfo file, DirectoryInfo cacheDir)
        {
            var cacheFile = new FileInfo(Path.Combine(cacheDir.FullName, "besttypes.csv"));

            if (cacheFile.Exists)
            {
                using var textReader = new StreamReader(cacheFile.FullName);
                return textReader.ReadCsvWithHeader().ToDictionary(
                    x => x["key"],
                    x => x["type"].ParseEnum<BestType>());
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

        internal static string ToVerbatimString(this string s) =>
            $"@\"{s.Replace("\"", "\"\"")}\"";

        internal static string ToIdentifier(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new System.ArgumentException("string was empty or whitespace", nameof(s));

            s = Regex.Replace(s, @"[^a-zA-Z0-9_]+", "_");

            if (char.IsDigit(s[0]))
                return "_" + s;

            return s;
        }
    }
}
