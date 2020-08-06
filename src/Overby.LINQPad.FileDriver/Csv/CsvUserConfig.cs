using Overby.LINQPad.FileDriver.Configuration;
using Overby.LINQPad.FileDriver.TypeInference;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver.Csv
{
    public class CsvUserConfig
    {
        public char Delimiter { get; set; }

        public char TextQualifier { get; set; }

        public string Header { get; set; }
        public ValueTokens TrueStrings { get; set; }
        public ValueTokens FalseStrings { get; set; }
        public ValueTokens NullStrings { get; set; }
        public Dictionary<string, BestType> BestTypes { get; set; }
        public bool Ignore { get; set; }
    }
}
