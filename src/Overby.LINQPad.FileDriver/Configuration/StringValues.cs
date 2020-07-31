using System;
using System.Collections.Generic;
using System.Linq;
using static System.StringComparer;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public class StringValues
    {
        public List<string> Values { get; set; }
        public bool IgnoreCase { get; set; }

        public StringValues()
        {

        }

        public StringValues(bool ignoreCase, params string[] values)
        {
            IgnoreCase = ignoreCase;
            Values = values.ToList();
        }

        public HashSet<string> GetHashSet() =>
            new HashSet<string>(Values, IgnoreCase ? OrdinalIgnoreCase : Ordinal);

        public static StringValues DefaultTrueStrings => new StringValues(true, "true");
        public static StringValues DefaultFalseStrings => new StringValues(true, "false");
        public static StringValues DefaultNullStrings => new StringValues(true, "", "null");

        public void WriteToConfigHash(Action<object> write)
        {
            write(IgnoreCase);

            foreach (var value in Values)
                write(value);
        }
    }
}
