using static System.StringComparer;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver.Configuration
{
    class StringValues
    {
        public string[] Values { get; set; }
        public bool IgnoreCase { get; set; }

        public StringValues(bool ignoreCase, params string[] values)
        {
            IgnoreCase = ignoreCase;
            Values = values;
        }

        public HashSet<string> GetHashSet() =>
            new HashSet<string>(Values, IgnoreCase ? OrdinalIgnoreCase : Ordinal);

        public static readonly StringValues DefaultTrueStrings = new StringValues(true, "true", "1");
        public static readonly StringValues DefaultFalseStrings = new StringValues(true, "false", "0");
        public static readonly StringValues DefaultNullStrings = new StringValues(false, "");
    }
}
