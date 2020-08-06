using System;
using System.Collections.Generic;
using System.Linq;
using static System.StringComparer;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public class ValueTokens
    {
        public List<string> Values { get; set; }
        public bool IgnoreCase { get; set; }

        public ValueTokens()
        {

        }

        public ValueTokens(bool ignoreCase, params string[] values)
        {
            IgnoreCase = ignoreCase;
            Values = values.ToList();
        }

        public HashSet<string> GetHashSet() =>
            new HashSet<string>(Values, IgnoreCase ? OrdinalIgnoreCase : Ordinal);

        public static ValueTokens DefaultTrueStrings => new ValueTokens(true, "true");
        public static ValueTokens DefaultFalseStrings => new ValueTokens(true, "false");
        public static ValueTokens DefaultNullStrings => new ValueTokens(true, "", "null");

        public static implicit operator ValueTokens(string[] strings) => new ValueTokens(false, strings);

        public void WriteToConfigHash(Action<object> write)
        {
            write(IgnoreCase);

            foreach (var value in Values)
                write(value);
        }
    }
}
