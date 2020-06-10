using LINQPad.Extensibility.DataContext;
using Overby.Extensions.Text;
using Overby.LINQPad.FileDriver.Analysis;
using Overby.LINQPad.FileDriver.Configuration;
using Overby.LINQPad.FileDriver.TypeInference;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Overby.LINQPad.FileDriver.CodeGen;
using static Overby.LINQPad.FileDriver.CodeGenConstants;

namespace Overby.LINQPad.FileDriver.Csv
{
    internal class CsvGenerator : ICodeGenerator
    {
        public IFileConfig UpdateFileConfig(FileInfo file, IFileConfig prevConfig)
        {
            var csvConfig = prevConfig as CsvConfig ?? new CsvConfig
            {
                Delimiter = GetDefaultDelimiter(),
            };

            csvConfig.BestTypes = GetBestTypes();

            return csvConfig;

            Dictionary<string, BestType> GetBestTypes()
            {
                using var fileReader = file.OpenText();
                using var reader = GetTextReader(fileReader);

                var record = new List<string>();
                var q = from rec in reader.ReadCsvWithHeader(
                            delimiter: csvConfig.Delimiter,
                            textQualifier: csvConfig.TextQualifier,
                            record: record)
                        select
                            from k in rec.Keys
                            let value = rec.ContainsKey(k) ? rec[k] : string.Empty
                            select (k, value);

                return TypeInferrer.DetermineBestTypes(q, new TypeInferrer.Options { UserParser = CreateUserParser() });

                TypeInferrer.TryParseValue CreateUserParser()
                {
                    var trueSet = csvConfig.TrueStrings?.GetHashSet();
                    var falseSet = csvConfig.FalseStrings?.GetHashSet();
                    var nullSet = csvConfig.NullStrings?.GetHashSet();

                    return (string v, out ParsedValue x) =>
                    {
                        if (trueSet?.Contains(v) == true || falseSet?.Contains(v) == true)
                        {
                            x = ParsedValue.Bool;
                            return true;
                        }

                        if (nullSet?.Contains(v) == true)
                        {
                            x = ParsedValue.EmptyString;
                            return true;
                        }

                        x = default;
                        return false;
                    };
                }
            }

            TextReader GetTextReader(StreamReader fileReader)
            {
                if (string.IsNullOrWhiteSpace(csvConfig.Header))
                    return fileReader;

                return new CompositeTextReader(new TextReader[]
                {
                    new StringReader(csvConfig.Header + Environment.NewLine),
                    fileReader
                });
            }

            char GetDefaultDelimiter() => file.Extension.ToLowerInvariant() switch
            {
                ".tsv" => '\t',
                _ => ','
            };
        }

        public (Action<TextWriter> WriteRecordMembers,
            Action<TextWriter> WriteReaderImplementation)
            GetCodeGenerators(IFileConfig fileConfig)
        {
            var csvConfig = (CsvConfig)fileConfig;
            var bestTypes = csvConfig.BestTypes;

            const string csvRecords = nameof(csvRecords);
            const string csvRecord = nameof(csvRecord);
            const string reader = nameof(reader);

            var propIdentifierScope = $"property:{fileConfig.RelativePath}";

            var recordProperties =
              (from bt in bestTypes
               let propName = bt.Key.ToIdentifier(propIdentifierScope)
               let csPropType = GetTypeRef(bt.Value)
               let propDef = $@"public {csPropType} {propName} {{ get; set; }}"
               let rawValueExpression = $"{csvRecord}[{bt.Key.ToVerbatimString()}]" +

                    // trim values before parsing; don't trim unparsed strings
                    (bt.Value == BestType.String ? "" : ".Trim()")

               let parser = GetParserCode(bt.Value, rawValueExpression)
               let propAssignment = $@"{propName} = {parser}"
               select (propDef, propAssignment, propName, csPropType, rawFieldKey: bt.Key)).ToArray();

            SetPropertyExplorerItems();

            return (GenerateRecordMembers, GenerateReaderImplementation);

            void GenerateRecordMembers(TextWriter writer) =>
                writer.WriteLines(recordProperties.Select(x => x.propDef));

            void GenerateReaderImplementation(TextWriter writer) => writer.WriteLine($@"
using(var {reader} = new System.IO.StreamReader({ReaderFilePathVariableName}))
{{
    var {csvRecords} = Overby.Extensions.Text.CsvParsingExtensions
        .ReadCsvWithHeader({reader}, delimiter: {csvConfig.Delimiter.ToLiteral()});

    foreach(var {csvRecord} in {csvRecords})
    {{
        yield return new {RecordClassName}
        {{
            {recordProperties.Select(rp => rp.propAssignment).StringJoin("," + Environment.NewLine)}
        }};
    }}
}}

{GenerateIsNullFunction()}

{GenerateParseBoolFunction()}");

            string GenerateIsNullFunction()
            {
                var nullStrings = csvConfig.NullStrings ?? StringValues.DefaultNullStrings;

                var predicates = string.Join(" || ",
                    csvConfig.NullStrings.Values.Select(GetPredicate));

                return $"bool IsNull(string value) => {predicates};";

                string GetPredicate(string nul) => csvConfig.NullStrings.IgnoreCase
                        ? $"value.Equals({nul.ToLiteral()}, System.StringComparison.OrdinalIgnoreCase)"
                        : $"value == {nul.ToLiteral()}";
            }

            string GenerateParseBoolFunction()
            {
                // I don't think there's a point in comparing the false strings;
                // They were only useful for identifying if the type was boolean or not

                var trueStrings = csvConfig.TrueStrings ?? StringValues.DefaultTrueStrings;

                var predicates = string.Join(" || ",
                    csvConfig.TrueStrings.Values.Select(GetPredicate));

                return $"bool ParseBool(string value) => {predicates};";

                string GetPredicate(string s) => csvConfig.TrueStrings.IgnoreCase
                        ? $"value.Equals({s.ToLiteral()}, System.StringComparison.OrdinalIgnoreCase)"
                        : $"value == {s.ToLiteral()}";
            }

            void SetPropertyExplorerItems()
            {
                // adds property/column child items to file explorer item

                csvConfig.PropertyItems = new List<ExplorerItem>(
                    from rp in recordProperties
                    select new ExplorerItem(rp.rawFieldKey, ExplorerItemKind.Property, ExplorerIcon.Column)
                    {
                        DragText = rp.propName,
                        ToolTipText = rp.csPropType
                    });
            }
        }
    }
}