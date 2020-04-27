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

        public StringValues TrueStrings { get; set; } = new StringValues(true, bool.TrueString, "1");
        public StringValues FalseStrings { get; set; } = new StringValues(true, bool.FalseString, "0");
        public StringValues NullStrings { get; set; } = new StringValues(false, string.Empty);
        
        public Dictionary<string, BestType> BestTypes { get; set; }

        public override List<ExplorerItem> GetFileChildItems() => PropertyItems;

        [JsonIgnore]
        public List<ExplorerItem> PropertyItems { get; set; }
    }
}
