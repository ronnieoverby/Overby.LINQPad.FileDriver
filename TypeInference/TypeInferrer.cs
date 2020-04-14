using static Overby.LINQPad.FileDriver.TypeInference.ParsedValue;
using static System.StringComparison;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Overby.LINQPad.FileDriver.TypeInference
{
    internal static partial class TypeInferrer
    {
        public static Dictionary<TKey, BestType> DetermineBestTypes<TKey>(IEnumerable<IEnumerable<(TKey key, string value)>> sequence)
        {
            var parsedValues = new Dictionary<TKey, HashSet<ParsedValue>>();

            foreach (var record in sequence)
            {
                foreach (var (key, rawValue) in record)
                {
                    if (!parsedValues.TryGetValue(key, out var set))
                        set = parsedValues[key] = new HashSet<ParsedValue>();

                    if (set.Contains(String))
                        // no need
                        continue;

                    var value = rawValue.Trim();

                    // for similar types (numerics/dates/bools) parse from least wide to most wide types

                    if (string.IsNullOrWhiteSpace(value)) // todo consider short circuiting for large strings; what size?
                    {
                        set.Add(EmptyString);
                    }

                    // bool -> numeric -> char -> string

                    else if ("1" == value)
                    {
                        set.Add(One);
                    }
                    else if ("0" == value)
                    {
                        set.Add(Zero);
                    }

                    // bool -> char -> string

                    else if (ieq("t", "y"))
                    {
                        set.Add(TrueString1);
                    }
                    else if (ieq("f", "n"))
                    {
                        set.Add(FalseString1);
                    }

                    // bool -> string

                    else if (ieq("yes", "on", "true"))
                    {
                        set.Add(TrueString);
                    }
                    else if (ieq("no", "off", "false"))
                    {
                        set.Add(FalseString);
                    }

                    // numerics

                    else if (byte.TryParse(value, out var _))
                    {
                        set.Add(Byte);
                    }
                    else if (short.TryParse(value, out var _))
                    {
                        set.Add(Int16);
                    }
                    else if (int.TryParse(value, out var _))
                    {
                        set.Add(Int32);
                    }
                    else if (long.TryParse(value, out var _))
                    {
                        set.Add(Int64);
                    }
                    else if (BigInteger.TryParse(value, out var _))
                    {
                        set.Add(BigInt);
                    }
                    else if (decimal.TryParse(value, out var _))
                    {
                        set.Add(Decimal);
                    }
                    else if (double.TryParse(value, out var _))
                    {
                        set.Add(Double);
                    }

                    // char

                    else if (value.Length == 1)
                    {
                        set.Add(Char);
                    }

                    // dates

                    else if (System.DateTime.TryParse(value, out var _))
                    {
                        set.Add(DateTime);
                    }
                    else if (System.DateTimeOffset.TryParse(value, out var _))
                    {
                        set.Add(DateTimeOffset);
                    }

                    // other unambiguous, straight forward types

                    else if (System.Guid.TryParse(value, out var _))
                    {
                        set.Add(Guid);
                    }
                    else if (System.TimeSpan.TryParse(value, out var _))
                    {
                        set.Add(Timespan);
                    }

                    // everything else

                    else
                    {
                        set.Add(String);
                    }

                    bool ieq(params string[] xs) => // equals ignore case
                       xs.Any(x => value.Equals(x, OrdinalIgnoreCase));
                }
            }

            // determine best types

            var finalTypes = parsedValues
                .ToDictionary(p => p.Key, p => DetermineBestType(p.Value));

            return finalTypes;
        }

        static BestType DetermineBestType(HashSet<ParsedValue> parsedValues)
        {
            var nullable = parsedValues.Remove(EmptyString);

            if (parsedValues.Count == 0)
                // always empty
                return Best(EmptyString);

            if (parsedValues.Count == 1)
                // just the one (could be any type; all types need to map to a best type!)
                return Best(parsedValues.First());

            if (parsedValues.Contains(String))
                // fallback to string
                return Best(String);

            // not string
            // at least 2 values

            BestType best;

            // numerics
            if (WidestTypeMapped(out best,
                    new[] { Double, Decimal, BigInt, Int64, Int32, Int16, Byte, One, Zero },
                    new[] { Double, Decimal, BigInt, Int64, Int32, Int16, Byte, Byte, Byte }))
                return best;

            // booleans
            if (WidestTypeMapAll(out best,
                    TrueString, // all
                    TrueString, FalseString, TrueString1, FalseString1, One, Zero))
                return best;

            // chars
            if (WidestTypeMapAll(out best,
                    Char, // all
                    Char, One, Zero, TrueString1, FalseString1))
                return best;

            // dates
            if (WidestType(out best, DateTimeOffset, DateTime))
                return best;

            return BestType.String;

            bool IsSubset(params ParsedValue[] types) =>
                parsedValues.IsSubsetOf(types);

            bool WidestType(out BestType best, params ParsedValue[] types) =>
                WidestTypeMapped(out best, types, types);

            bool WidestTypeMapped(out BestType best, ParsedValue[] possibles, ParsedValue[] actuals)
            {
                // types are ordered widet -> narrow
                // if set is subset of types, take first

                if (IsSubset(possibles))
                {
                    best = Best(possibles
                        .Zip(actuals, (possible, actual) => (possible, actual))
                        .First(t => parsedValues.Contains(t.possible))
                        .actual);

                    return true;
                }

                best = default;
                return false;
            }

            bool WidestTypeMapAll(out BestType best, ParsedValue mapAllTo, params ParsedValue[] possibles)
            {
                var actuals = new ParsedValue[possibles.Length];

                for (int i = 0; i < actuals.Length; i++)
                    actuals[i] = mapAllTo;

                return WidestTypeMapped(out best, possibles, actuals);
            }

            BestType Best(ParsedValue type) => MapToBestType(type, nullable);
        }

        static BestType MapToBestType(ParsedValue parsedType, bool nullable) =>
            // all types need to map to a best type!
            parsedType switch
            {
                Char => nullable ? BestType.NullableChar : BestType.Char,

                Double => nullable ? BestType.NullableDouble : BestType.Double,
                Decimal => nullable ? BestType.NullableDecimal : BestType.Decimal,
                BigInt => nullable ? BestType.NullableBigInt : BestType.BigInt,
                Int64 => nullable ? BestType.NullableInt64 : BestType.Int64,
                Int32 => nullable ? BestType.NullableInt32 : BestType.Int32,
                Int16 => nullable ? BestType.NullableInt16 : BestType.Int16,
                Byte => nullable ? BestType.NullableByte : BestType.Byte,

                One => nullable ? BestType.NullableBool : BestType.Bool,
                Zero => nullable ? BestType.NullableBool : BestType.Bool,

                TrueString1 => nullable ? BestType.NullableBool : BestType.Bool,
                FalseString1 => nullable ? BestType.NullableBool : BestType.Bool,

                TrueString => nullable ? BestType.NullableBool : BestType.Bool,
                FalseString => nullable ? BestType.NullableBool : BestType.Bool,

                DateTimeOffset => nullable ? BestType.NullableDateTimeOffset : BestType.DateTimeOffset,
                DateTime => nullable ? BestType.NullableDateTime : BestType.DateTime,

                Timespan => nullable ? BestType.NullableTimespan : BestType.Timespan,

                Guid => nullable ? BestType.NullableGuid : BestType.Guid,

                EmptyString => BestType.String,
                String => BestType.String,

                _ => throw NoBestType(parsedType),

            };

        static System.Exception NoBestType(ParsedValue pt) => 
            new System.Exception($"`{pt}` is not mapped to a best type!");

      
    }
}
