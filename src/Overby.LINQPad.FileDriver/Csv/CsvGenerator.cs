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
                            let value = rec.ContainsKey(k) ? "" : rec[k]
                            select (k, value);

                bool IsNullString(string s) =>
                    string.IsNullOrEmpty(s) || csvConfig.NullStrings.Values.Contains(s);

                return TypeInferrer.DetermineBestTypes(q,
                    csvConfig.FalseStrings.Values,
                    csvConfig.TrueStrings.Values,
                    isNullOrEmpty: IsNullString);
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

        public (Action<TextWriter> WriteRecordMembers, Action<TextWriter> WriteReaderImplementation)
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
               let rawValueExpression = $"{csvRecord}[{bt.Key.ToVerbatimString()}]"
               let parser = GetParserCode(bt.Value, rawValueExpression)
               let propAssignment = $@"{propName} = {parser}"
               select (propDef, propAssignment, propName, csPropType, rawFieldKey: bt.Key)).ToArray();

            SetPropertyExplorerItems();

            return (GenerateRecordMembers, GenerateReaderImplementation);

            void SetPropertyExplorerItems()
            {
                csvConfig.PropertyItems = new List<ExplorerItem>(
                    from rp in recordProperties
                    select new ExplorerItem(rp.rawFieldKey, ExplorerItemKind.Property, ExplorerIcon.Column)
                    {
                        DragText = rp.propName,
                        ToolTipText= rp.csPropType
                    });
            }

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
}}");
        }
    }
}