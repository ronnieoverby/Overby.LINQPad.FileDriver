using LINQPad.Extensibility.DataContext;
using Newtonsoft.Json;
using Overby.LINQPad.FileDriver.Configuration;
using System;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver.Csv
{
    public class CsvConfig : FileConfig
    {
        public char Delimiter { get; set; } = ',';

        public char TextQualifier { get; set; } = '"';

        /// <summary>
        /// An implicit header row, when the source text file has none.
        /// </summary>
        public string Header { get; set; }

        public ValueTokens TrueStrings { get; set; } = ValueTokens.DefaultTrueStrings;
        public ValueTokens FalseStrings { get; set; } = ValueTokens.DefaultFalseStrings;
        public ValueTokens NullStrings { get; set; } = ValueTokens.DefaultNullStrings;

        public Dictionary<string, BestType> BestTypes { get; set; }

        public override List<ExplorerItem> GetFileChildItems() => PropertyItems;
        

        public override void HashConfigValues(Action<object> write)
        {
            write(Delimiter);
            write(TextQualifier);
            write(Header);
            TrueStrings?.WriteToConfigHash(write);
            FalseStrings?.WriteToConfigHash(write);
            NullStrings?.WriteToConfigHash(write);
        }

        public override Type GetUserConfigType() => typeof(CsvUserConfig);

        public override object GetUserConfig(string fileAbsolutePath) =>
            new CsvUserConfig(fileAbsolutePath)
            {
                Delimiter = Delimiter,
                Header = Header,
                TextQualifier = TextQualifier,
                TrueStrings = TrueStrings,
                FalseStrings = FalseStrings,
                NullStrings = NullStrings,
                BestTypes = BestTypes.WithIdentifierComparer(),
                Ignore = Ignore
            };

        public override void UpdateFromUserConfig(object userConfig)
        {
            var cfg = (CsvUserConfig)userConfig;
            Ignore = cfg.Ignore;
            Delimiter = cfg.Delimiter;
            Header = cfg.Header;
            TextQualifier = cfg.TextQualifier;

            if (cfg.TrueStrings != null)
                TrueStrings = cfg.TrueStrings;

            if (cfg.FalseStrings != null)
                FalseStrings = cfg.FalseStrings;

            if (cfg.NullStrings != null)
                NullStrings = cfg.NullStrings;

            //Debugger.Launch();

            if (cfg.BestTypes != null)
                BestTypes = cfg.BestTypes;
        }

        [JsonIgnore]
        public List<ExplorerItem> PropertyItems { get; set; }
    }
}
