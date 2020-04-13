using System;

namespace Overby.LINQPad.FileDriver
{
    public static class Parsers
    {
        public static bool ParseBool(string rawValue, string[] trueStrings, string[] falseStrings)
        {
            if (TryParseBool(rawValue, out var result, trueStrings, falseStrings))
                return result;

            throw new FormatException($@"can't parse bool: {new
            {
                rawValue,
                trueStrings = string.Join(",", trueStrings),
                falseStrings = string.Join(",", falseStrings)
            }}");
        }

        public static bool TryParseBool(string s, out bool result, string[] trueStrings, string[] falseStrings)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                result = default;
                return false;
            }

            s = s.Trim();

            foreach (var t in trueStrings ?? new[] { bool.TrueString })
            {
                if (t.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }
            }

            foreach (var f in falseStrings ?? new[] { bool.FalseString })
            {
                if (f.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}