using LINQPad.Extensibility.DataContext;
using Newtonsoft.Json;
using Overby.LINQPad.FileDriver.Configuration;
using Overby.LINQPad.FileDriver.TypeInference;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver.Csv
{
    class CsvConfig : FileConfig
    {
        public char Delimiter { get; set; } = ',';

        public char TextQualifier { get; set; } = '"';

        /// <summary>
        /// An implicit header row, when the source text file has none.
        /// </summary>
        public string Header { get; set; }

        public StringValues TrueStrings { get; set; } = StringValues.DefaultTrueStrings;
        public StringValues FalseStrings { get; set; } = StringValues.DefaultFalseStrings;
        public StringValues NullStrings { get; set; } = StringValues.DefaultNullStrings;
        
        public Dictionary<string, BestType> BestTypes { get; set; }

        public override List<ExplorerItem> GetFileChildItems() => PropertyItems;

        [JsonIgnore]
        public List<ExplorerItem> PropertyItems { get; set; }
    }
}
