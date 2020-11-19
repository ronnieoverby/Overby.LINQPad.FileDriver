using Overby.Extensions.Text;
using Overby.LINQPad.FileDriver.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Overby.LINQPad.FileDriver.Csv
{
    public class CsvUserConfig
    {
        private readonly string _filePath;

        public CsvUserConfig(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new System.ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace", nameof(filePath));

            _filePath = filePath;
        }

        public char Delimiter { get; set; }

        public char TextQualifier { get; set; }

        public string Header { get; set; }
        public ValueTokens TrueStrings { get; set; }
        public ValueTokens FalseStrings { get; set; }
        public ValueTokens NullStrings { get; set; }
        public Dictionary<string, BestType> BestTypes { get; set; }
        public bool Ignore { get; set; }

        public void SetAutoHeader(Func<int, string> nameField = null, int sampleRecordCount = int.MaxValue)
        {
            if (sampleRecordCount < 1)
                throw new ArgumentOutOfRangeException(nameof(sampleRecordCount), $"{nameof(sampleRecordCount)} must be positive");

            nameField ??= n => $"Field{n}";

            using var reader = new StreamReader(_filePath);
            var record = new List<string>();

            var fieldCount = reader.ReadCsv(Delimiter, TextQualifier, record, trimValues: false)
                .Take(sampleRecordCount)
                .Max(x => x.Count());

            Header = string.Join(Delimiter.ToString(), Enumerable.Range(1, fieldCount).Select(nameField));
        }
    }
}
