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
        public StringValues TrueStrings { get; internal set; }
        public StringValues FalseStrings { get; internal set; }
        public StringValues NullStrings { get; internal set; }
        public Dictionary<string, BestType> BestTypes { get; internal set; }
        public bool Ignore { get;  set; }
    }
}
