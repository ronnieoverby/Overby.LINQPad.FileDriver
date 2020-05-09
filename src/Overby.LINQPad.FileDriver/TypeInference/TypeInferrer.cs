using static Overby.LINQPad.FileDriver.TypeInference.ParsedValue;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Overby.Extensions.Text;

namespace Overby.LINQPad.FileDriver.TypeInference
{

    public static class TypeInferrer
    {
        public class Options
        {
            public TryParseValue UserParser { get; set; }

            public bool ParseBool { get; set; } = true;
            public bool ParseByte { get; set; } = true;
            public bool ParseInt16 { get; set; } = true;
            public bool ParseInt32 { get; set; } = true;
            public bool ParseInt64 { get; set; } = true;
            public bool ParseBigInteger { get; set; } = true;
            public bool ParseDecimal { get; set; } = true;
            public bool ParseDouble { get; set; } = true;
            public bool ParseChar { get; set; } = true;
            public bool ParseDateTime { get; set; } = true;
            public bool ParseTimeSpan { get; set; } = true;
            public bool ParseGuid { get; set; } = true;
        }

        public delegate bool TryParseValue(string value, out ParsedValue parsedValue);

        public static Dictionary<TKey, BestType> DetermineBestTypes<TKey>(
            IEnumerable<IEnumerable<(TKey key, string value)>> sequence,
            Options options = null)
        {
            options ??= new Options();

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

                    var value = rawValue == null ? "" : rawValue.Trim();

                    if (options.UserParser != null && options.UserParser(value, out var userParsedValue))
                    {
                        set.Add(userParsedValue);
                    }

                    // for similar types (numerics/dates/bools) parse from least wide to most wide types

                    else if (value.IsNullOrWhiteSpace())
                    {
                        set.Add(EmptyString);
                    }

                    else if (options.ParseBool && (value.EqualsI(bool.TrueString) || value.EqualsI(bool.FalseString)))
                    {
                        set.Add(Bool);
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

                    // numerics

                    else if (TryParseNumeric(value, out var pv))
                    {
                        set.Add(pv);
                    }

                    // char

                    else if (options.ParseChar && value.Length == 1)
                    {
                        set.Add(Char);
                    }

                    // dates

                    else if (options.ParseDateTime && System.DateTime.TryParse(value, out var _))
                    {
                        // datetime/datetimeoffset parsing are equally forgiving
                        // no need to try both

                        set.Add(DateTime);
                    }

                    // other unambiguous, straight forward types

                    else if (options.ParseGuid && System.Guid.TryParse(value, out var _))
                    {
                        set.Add(Guid);
                    }
                    else if (options.ParseTimeSpan && System.TimeSpan.TryParse(value, out var _))
                    {
                        set.Add(Timespan);
                    }

                    // everything else

                    else
                    {
                        set.Add(String);
                    }
                }
            }

            // determine best types

            var finalTypes = parsedValues
                .ToDictionary(p => p.Key, p => DetermineBestType(p.Value));

            return finalTypes;


            bool TryParseNumeric(string v, out ParsedValue pv)
            {
                if (!v.Contains('.'))
                {
                    // try integral types

                    if (options.ParseByte && byte.TryParse(v, out var _))
                    {
                        pv = Byte;
                        return true;
                    }
                    else if (options.ParseInt16 && short.TryParse(v, out var _))
                    {
                        pv = Int16;
                        return true;
                    }
                    else if (options.ParseInt32 && int.TryParse(v, out var _))
                    {
                        pv = Int32;
                        return true;
                    }
                    else if (options.ParseInt64 && long.TryParse(v, out var _))
                    {
                        pv = Int64;
                        return true;
                    }
                    else if (options.ParseDecimal && decimal.TryParse(v, out var _))
                    {
                        // decimal supports larger integers than int64

                        pv = Decimal;
                        return true;
                    }
                    else if (options.ParseBigInteger && BigInteger.TryParse(v, out var _))
                    {
                        // arbitrary integer size

                        pv = BigInt;
                        return true;
                    }
                    else if (options.ParseDouble && double.TryParse(v, out var _))
                    {
                        // not integral, but some values won't have a dot:
                        // -∞, ∞, NaN, 5E-324

                        pv = Double;
                        return true;
                    }
                }
                else if (options.ParseDecimal && decimal.TryParse(v, out var _))
                {
                    // decimal is more precise than double

                    pv = Decimal;
                    return true;
                }
                else if (options.ParseDouble && double.TryParse(v, out var _))
                {
                    // double is more forgiving than decimal

                    pv = Double;
                    return true;
                }

                pv = default;
                return false;
            }
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

            // bool
            if (WidestTypeMapAll(out best,
                    Bool, // all
                    Bool, One, Zero))
                return best;

            // numerics
            if (WidestTypeMapped(out best,
                    new[] { Double, Decimal, BigInt, Int64, Int32, Int16, Byte, One, Zero },
                    new[] { Double, Decimal, BigInt, Int64, Int32, Int16, Byte, Byte, Byte }))
                return best;

            // chars
            if (WidestTypeMapAll(out best,
                    Char, // all
                    Char, One, Zero))
                return best;

            return BestType.String;

            bool IsSubset(params ParsedValue[] types) =>
                parsedValues.IsSubsetOf(types);

            bool WidestType(out BestType best, params ParsedValue[] types) =>
                WidestTypeMapped(out best, types, types);

            bool WidestTypeMapped(out BestType best, ParsedValue[] possibles, ParsedValue[] actuals)
            {
                // types are ordered wide -> narrow
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
                Bool => nullable ? BestType.NullableBool : BestType.Bool,

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
