﻿using System;
using System.Collections.Generic;
using static System.StringComparer;

namespace Overby.LINQPad.FileDriver.Configuration
{
    class StringValues
    {
        public string[] Values { get; set; }
        public bool IgnoreCase { get; set; }

        public StringValues()
        {

        }

        public StringValues(bool ignoreCase, params string[] values)
        {
            IgnoreCase = ignoreCase;
            Values = values;
        }

        public HashSet<string> GetHashSet() =>
            new HashSet<string>(Values, IgnoreCase ? OrdinalIgnoreCase : Ordinal);

        public static StringValues DefaultTrueStrings => new StringValues(true, "true", "1");
        public static StringValues DefaultFalseStrings => new StringValues(true, "false", "0");
        public static StringValues DefaultNullStrings => new StringValues(false, "");

        public void WriteToConfigHash(Action<object> write)
        {
            write(IgnoreCase);

            foreach (var value in Values)
                write(value);
        }
    }
}
