using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Overby.LINQPad.FileDriver
{
    static class ValueTyper
    {
        public static Dictionary<TKey, BestType> DetermineBestTypes<TKey>(IEnumerable<IEnumerable<(TKey key, string value)>> sequence)
        {
            var parsedTypes = new Dictionary<TKey, HashSet<ParsedType>>();
            var parserFlags = new Dictionary<TKey, bool[]>();

            foreach (var record in sequence)
            {
                foreach (var (key, rawValue) in record)
                {
                    if (!parsedTypes.TryGetValue(key, out var set))
                        set = parsedTypes[key] = new HashSet<ParsedType>();

                    if (!parserFlags.TryGetValue(key, out var flags))
                    {
                        flags = parserFlags[key] =
                            new bool[Enum.GetValues(typeof(ParsedType)).Cast<ParsedType>().Count()];

                        for (int i = 0; i < flags.Length; i++)
                            flags[i] = true;
                    }

                    bool CheckFlag(ParsedType pt) => flags[(int)pt];
                    void KillFlags(params ParsedType[] pts)
                    {
                        foreach (var pt in pts)
                        {
                            var i = (int)pt;
                            flags[i] = !flags[i];
                        }
                    }

                    if (set.Contains(ParsedType.String))
                        // no need
                        continue;

                    var value = rawValue.Trim();

                    // for similar types (numerics/dates/bools) parse from least wide to most wide types

                    if (string.IsNullOrWhiteSpace(value))  // always check
                    {
                        set.Add(ParsedType.EmptyString);
                    }

                    // bool -> numeric -> char -> string

                    else if (CheckFlag(ParsedType.One) && "1" == value)
                    {
                        set.Add(ParsedType.One);
                    }
                    else if (CheckFlag(ParsedType.Zero) && "0" == value)
                    {
                        set.Add(ParsedType.Zero);
                    }

                    // bool -> char -> string

                    else if (CheckFlag(ParsedType.TrueString1) && ieq("t", "y"))
                    {
                        set.Add(ParsedType.TrueString1);
                    }
                    else if (CheckFlag(ParsedType.FalseString1) && ieq("f", "n"))
                    {
                        set.Add(ParsedType.FalseString1);
                    }

                    // bool -> string

                    else if (/*CheckFlag(ParsedType.TrueString) &&*/ ieq("yes", "on", "true"))
                    {
                        set.Add(ParsedType.TrueString);
                    }
                    else if (/*CheckFlag(ParsedType.FalseString) &&*/ ieq("no", "off", "false"))
                    {
                        set.Add(ParsedType.FalseString);
                    }

                    // numerics

                    else if (CheckFlag(ParsedType.Byte) && byte.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.Byte))
                            KillFlags(ParsedType.One, ParsedType.Zero);
                    }
                    else if (CheckFlag(ParsedType.Int16) && short.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.Int16))
                            KillFlags(ParsedType.Byte, ParsedType.One, ParsedType.Zero);
                    }
                    else if (CheckFlag(ParsedType.Int32) && int.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.Int32))
                            KillFlags(ParsedType.Int16, ParsedType.Byte, ParsedType.One, ParsedType.Zero);
                    }
                    else if (CheckFlag(ParsedType.Int64) && long.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.Int64))
                            KillFlags(ParsedType.Int32, ParsedType.Int16, ParsedType.Byte, ParsedType.One, ParsedType.Zero);
                    }
                    else if (CheckFlag(ParsedType.BigInt) && BigInteger.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.BigInt))
                            KillFlags(ParsedType.Int64, ParsedType.Int32, ParsedType.Int16, ParsedType.Byte, ParsedType.One, ParsedType.Zero);
                    }
                    else if (CheckFlag(ParsedType.Decimal) && decimal.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.Decimal))
                            KillFlags(ParsedType.BigInt, ParsedType.Int64, ParsedType.Int32, ParsedType.Int16, ParsedType.Byte, ParsedType.One, ParsedType.Zero);
                    }
                    else if (/*CheckFlag(ParsedType.Double) &&*/ double.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.Double))
                            KillFlags(ParsedType.Decimal, ParsedType.BigInt, ParsedType.Int64, ParsedType.Int32, ParsedType.Int16, ParsedType.Byte, ParsedType.One, ParsedType.Zero);
                    }

                    // char

                    else if (/*CheckFlag(ParsedType.Char) &&*/ value.Length == 1)
                    {
                        if (set.Add(ParsedType.Char))
                            KillFlags(ParsedType.One, ParsedType.Zero, ParsedType.TrueString1, ParsedType.FalseString1);
                    }

                    // dates

                    else if (CheckFlag(ParsedType.DateTime) && DateTime.TryParse(value, out var _))
                    {
                        set.Add(ParsedType.DateTime);
                    }
                    else if (/*CheckFlag(ParsedType.DateTimeOffset) &&*/ DateTimeOffset.TryParse(value, out var _))
                    {
                        if (set.Add(ParsedType.DateTimeOffset))
                            KillFlags(ParsedType.DateTime);
                    }

                    // other unambiguous, straight forward types

                    else if (/*CheckFlag(ParsedType.Guid) &&*/ Guid.TryParse(value, out var _))
                    {
                        set.Add(ParsedType.Guid);
                    }
                    else if (/*CheckFlag(ParsedType.Timespan) &&*/ TimeSpan.TryParse(value, out var _))
                    {
                        set.Add(ParsedType.Timespan);
                    }

                    // everything else

                    else
                    {
                        set.Add(ParsedType.String);
                    }

                    bool ieq(params string[] xs) => // equals ignore case
                       xs.Any(x => value.Equals(x, StringComparison.OrdinalIgnoreCase));
                }
            }

            // determine best types

            var parsedTypesCopy = parsedTypes
                .ToDictionary(p => p.Key, p => new HashSet<ParsedType>(p.Value));


            var finalTypes = parsedTypes
                .ToDictionary(p => p.Key, p => DetermineBestType(p.Value));

            return finalTypes;
        }

        static BestType DetermineBestType(HashSet<ParsedType> parsedTypes)
        {
            var nullable = parsedTypes.Remove(ParsedType.EmptyString);

            if (parsedTypes.Count == 0)
                // always empty
                return Best(ParsedType.EmptyString);

            if (parsedTypes.Count == 1)
                // just the one (could be any type; all types need to map to a best type!)
                return Best(parsedTypes.First());

            if (parsedTypes.Contains(ParsedType.String))
                // fallback to string
                return Best(ParsedType.String);

            // not string
            // at least 2 values

            BestType best;

            // numerics
            if (WidestTypeMapped(out best,
                    new[] { ParsedType.Double, ParsedType.Decimal, ParsedType.BigInt, ParsedType.Int64, ParsedType.Int32, ParsedType.Int16, ParsedType.Byte, ParsedType.One, ParsedType.Zero },
                    new[] { ParsedType.Double, ParsedType.Decimal, ParsedType.BigInt, ParsedType.Int64, ParsedType.Int32, ParsedType.Int16, ParsedType.Byte, ParsedType.Byte, ParsedType.Byte }))
                return best;

            // booleans
            if (WidestTypeMapAll(out best,
                    ParsedType.TrueString, // all
                    ParsedType.TrueString, ParsedType.FalseString, ParsedType.TrueString1, ParsedType.FalseString1, ParsedType.One, ParsedType.Zero))
                return best;

            // chars
            if (WidestTypeMapAll(out best,
                    ParsedType.Char, // all
                    ParsedType.Char, ParsedType.One, ParsedType.Zero, ParsedType.TrueString1, ParsedType.FalseString1))
                return best;

            // dates
            if (WidestType(out best, ParsedType.DateTimeOffset, ParsedType.DateTime))
                return best;

            return BestType.String;

            bool IsSubset(params ParsedType[] types) =>
                parsedTypes.IsSubsetOf(types);

            bool WidestType(out BestType best, params ParsedType[] types) =>
                WidestTypeMapped(out best, types, types);

            bool WidestTypeMapped(out BestType best, ParsedType[] possibles, ParsedType[] actuals)
            {
                // types are ordered widet -> narrow
                // if set is subset of types, take first

                if (IsSubset(possibles))
                {
                    best = Best(possibles
                        .Zip(actuals, (possible, actual) => (possible, actual))
                        .First(t => parsedTypes.Contains(t.possible))
                        .actual);

                    return true;
                }

                best = default;
                return false;
            }

            bool WidestTypeMapAll(out BestType best, ParsedType mapAllTo, params ParsedType[] possibles)
            {
                var actuals = new ParsedType[possibles.Length];

                for (int i = 0; i < actuals.Length; i++)
                    actuals[i] = mapAllTo;

                return WidestTypeMapped(out best, possibles, actuals);
            }

            BestType Best(ParsedType type) => MapToBestType(type, nullable);
        }

        static BestType MapToBestType(ParsedType parsedType, bool nullable) =>
            // all types need to map to a best type!
            parsedType switch
            {
                ParsedType.Char => nullable ? BestType.NullableChar : BestType.Char,

                ParsedType.Double => nullable ? BestType.NullableDouble : BestType.Double,
                ParsedType.Decimal => nullable ? BestType.NullableDecimal : BestType.Decimal,
                ParsedType.BigInt => nullable ? BestType.NullableBigInt : BestType.BigInt,
                ParsedType.Int64 => nullable ? BestType.NullableInt64 : BestType.Int64,
                ParsedType.Int32 => nullable ? BestType.NullableInt32 : BestType.Int32,
                ParsedType.Int16 => nullable ? BestType.NullableInt16 : BestType.Int16,
                ParsedType.Byte => nullable ? BestType.NullableByte : BestType.Byte,

                ParsedType.One => nullable ? BestType.NullableBool : BestType.Bool,
                ParsedType.Zero => nullable ? BestType.NullableBool : BestType.Bool,

                ParsedType.TrueString1 => nullable ? BestType.NullableBool : BestType.Bool,
                ParsedType.FalseString1 => nullable ? BestType.NullableBool : BestType.Bool,

                ParsedType.TrueString => nullable ? BestType.NullableBool : BestType.Bool,
                ParsedType.FalseString => nullable ? BestType.NullableBool : BestType.Bool,

                ParsedType.DateTimeOffset => nullable ? BestType.NullableDateTimeOffset : BestType.DateTimeOffset,
                ParsedType.DateTime => nullable ? BestType.NullableDateTime : BestType.DateTime,

                ParsedType.Timespan => nullable ? BestType.NullableTimespan : BestType.Timespan,

                ParsedType.Guid => nullable ? BestType.NullableGuid : BestType.Guid,

                ParsedType.EmptyString => BestType.String,
                ParsedType.String => BestType.String,

                _ => throw NoBestType(parsedType),

            };

        static Exception NoBestType(ParsedType pt) => new Exception($"'{nameof(ParsedType.One)}' is not mapped to a best type!");

        public enum BestType
        {
            String,
            BigInt,
            NullableBigInt,
            Bool,
            NullableBool,
            Char,
            NullableChar,
            DateTime,
            NullableDateTime,
            DateTimeOffset,
            NullableDateTimeOffset,
            Decimal,
            NullableDecimal,
            Double,
            NullableDouble,
            Guid,
            NullableGuid,
            Int64,
            NullableInt64,
            Int32,
            NullableInt32,
            Int16,
            NullableInt16,
            Byte,
            NullableByte,
            Timespan,
            NullableTimespan,
        }

        enum ParsedType
        {
            EmptyString,

            String,

            Char,

            Double,
            Decimal,
            BigInt,
            Int64,
            Int32,
            Int16,
            Byte,

            One, Zero, // could be bool/numeric/char

            TrueString1,
            FalseString1,
            TrueString,
            FalseString,

            DateTimeOffset,
            DateTime,

            Timespan,

            Guid,
        }

        public static string GetTypeRef(BestType bestType) => bestType switch
        {
            BestType.String => "System.String",
            BestType.BigInt => "System.Numerics.BigInteger",
            BestType.NullableBigInt => "System.Numerics.BigInteger?",
            BestType.Bool => "System.Boolean",
            BestType.NullableBool => "System.Boolean?",
            BestType.Char => "System.Char",
            BestType.NullableChar => "System.Char?",
            BestType.DateTime => "System.DateTime",
            BestType.NullableDateTime => "System.DateTime?",
            BestType.DateTimeOffset => "System.DateTimeOffset",
            BestType.NullableDateTimeOffset => "System.DateTimeOffset?",
            BestType.Decimal => "System.Decimal",
            BestType.NullableDecimal => "System.Decimal?",
            BestType.Double => "System.Double",
            BestType.NullableDouble => "System.Double?",
            BestType.Guid => "System.Guid",
            BestType.NullableGuid => "System.Guid?",
            BestType.Int64 => "System.Int64",
            BestType.NullableInt64 => "System.Int64?",
            BestType.Int32 => "System.Int32",
            BestType.NullableInt32 => "System.Int32?",
            BestType.Int16 => "System.Int16",
            BestType.NullableInt16 => "System.Int16?",
            BestType.Byte => "System.Byte",
            BestType.NullableByte => "System.Byte?",
            BestType.Timespan => "System.TimeSpan",
            BestType.NullableTimespan => "System.TimeSpan?",
            _ => throw new NotImplementedException("missing handler for " + bestType),
        };

        public static string GetParserCode(BestType bestType, string rawValueExpression) => bestType switch
        {
            BestType.String => rawValueExpression,
            BestType.BigInt => $"System.Numerics.BigInteger.Parse({rawValueExpression})",
            BestType.NullableBigInt => $"string.IsNullOrWhiteSpace({rawValueExpression}) ? default(System.Numerics.BigInteger?) : System.Numerics.BigInteger.Parse({rawValueExpression})",
            BestType.Bool => $"Overby.LINQPad.FileDriver.Parsers.ParseBool({rawValueExpression}, new[] {{ bool.TrueString, \"t\", \"y\", \"yes\", \"1\", \"on\" }}, new[] {{ bool.FalseString, \"f\", \"n\", \"no\", \"0\", \"off\" }})",
            BestType.NullableBool => $"string.IsNullOrWhiteSpace({rawValueExpression}) ? default(System.Boolean?) : Overby.LINQPad.FileDriver.Parsers.ParseBool({rawValueExpression}, new[] {{ bool.TrueString, \"t\", \"y\", \"yes\", \"1\", \"on\" }}, new[] {{ bool.FalseString, \"f\", \"n\", \"no\", \"0\", \"off\" }})",
            BestType.Char => $"System.Char.Parse({ rawValueExpression })",
            BestType.NullableChar => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Char?) : System.Char.Parse({ rawValueExpression })",
            BestType.DateTime => $"System.DateTime.Parse({ rawValueExpression })",
            BestType.NullableDateTime => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.DateTime?) : System.DateTime.Parse({ rawValueExpression })",
            BestType.DateTimeOffset => $"System.DateTimeOffset.Parse({ rawValueExpression })",
            BestType.NullableDateTimeOffset => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.DateTimeOffset?) : System.DateTimeOffset.Parse({ rawValueExpression })",
            BestType.Decimal => $"System.Decimal.Parse({ rawValueExpression })",
            BestType.NullableDecimal => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Decimal?) : System.Decimal.Parse({ rawValueExpression })",
            BestType.Double => $"System.Double.Parse({ rawValueExpression })",
            BestType.NullableDouble => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Double?) : System.Double.Parse({ rawValueExpression })",
            BestType.Guid => $"System.Guid.Parse({ rawValueExpression })",
            BestType.NullableGuid => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Guid?) : System.Guid.Parse({ rawValueExpression })",
            BestType.Int64 => $"System.Int64.Parse({ rawValueExpression })",
            BestType.NullableInt64 => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Int64?) : System.Int64.Parse({ rawValueExpression })",
            BestType.Int32 => $"System.Int32.Parse({ rawValueExpression })",
            BestType.NullableInt32 => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Int32?) : System.Int32.Parse({ rawValueExpression })",
            BestType.Int16 => $"System.Int16.Parse({ rawValueExpression })",
            BestType.NullableInt16 => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Int16?) : System.Int16.Parse({ rawValueExpression })",
            BestType.Byte => $"System.Byte.Parse({ rawValueExpression })",
            BestType.NullableByte => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.Byte?) : System.Byte.Parse({ rawValueExpression })",
            BestType.Timespan => $"System.TimeSpan.Parse({ rawValueExpression })",
            BestType.NullableTimespan => $"string.IsNullOrWhiteSpace({ rawValueExpression }) ? default(System.TimeSpan?) : System.TimeSpan.Parse({ rawValueExpression })",
            _ => throw new NotImplementedException("missing parser for " + bestType),
        };


    }
}