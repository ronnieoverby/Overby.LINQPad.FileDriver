using Overby.LINQPad.FileDriver.TypeInference;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using static Overby.LINQPad.FileDriver.CodeGenConstants;
using static Overby.LINQPad.FileDriver.TypeInference.BestType;

namespace Overby.LINQPad.FileDriver
{
    internal static class CodeGen
    {


        private static readonly ThreadLocal<CodeDomProvider> _provider = new ThreadLocal<CodeDomProvider>(() =>
            CodeDomProvider.CreateProvider("CSharp"));

        private static CodeDomProvider Provider => _provider.Value;



        public static string GetTypeRef(BestType bestType) => bestType switch
        {
            String => "string",
            Byte => "byte",
            NullableByte => "byte?",
            Int16 => "short",
            NullableInt16 => "short?",
            Int32 => "int",
            NullableInt32 => "int?",
            Int64 => "long",
            NullableInt64 => "long?",
            Decimal => "decimal",
            NullableDecimal => "decimal?",
            Double => "double",
            NullableDouble => "double?",
            Bool => "bool",
            NullableBool => "bool?",
            Char => "char",
            NullableChar => "char?",
            DateTime => "System.DateTime",
            NullableDateTime => "System.DateTime?",
            DateTimeOffset => "System.DateTimeOffset",
            NullableDateTimeOffset => "System.DateTimeOffset?",
            Guid => "System.Guid",
            NullableGuid => "System.Guid?",
            Timespan => "System.TimeSpan",
            NullableTimespan => "System.TimeSpan?",
            BigInt => "System.Numerics.BigInteger",
            NullableBigInt => "System.Numerics.BigInteger?",
            _ => throw new System.NotImplementedException("missing handler for " + bestType),
        };

        public static string GetTypeRef<T>() =>
            GetTypeRef(typeof(T));

        public static string GetTypeRef(System.Type type) => Memoizer.Instance.Get(type,
            new CodeTypeReferenceExpression(type).GenCode);

        public static string GetParserCode(BestType bestType, string rawValueExpression) => bestType switch
        {
            // todo dynamic true/false strings

            String => $"{IsNullFunctionName}({rawValueExpression}) ? default(string) : {rawValueExpression}",
            BigInt => $"System.Numerics.BigInteger.Parse({rawValueExpression})",
            NullableBigInt => $@"{IsNullFunctionName}({rawValueExpression}) ? default(System.Numerics.BigInteger?) : System.Numerics.BigInteger.Parse({rawValueExpression})",
            Bool => $"{ParseBoolFunctionName}({rawValueExpression})",
            NullableBool => $"{IsNullFunctionName}({rawValueExpression}) ? default(bool?) : {ParseBoolFunctionName}({rawValueExpression})",
            Char => $"char.Parse({ rawValueExpression })",
            NullableChar => $"{IsNullFunctionName}({ rawValueExpression }) ? default(char?) : char.Parse({ rawValueExpression })",
            DateTime => $"System.DateTime.Parse({ rawValueExpression })",
            NullableDateTime => $"{IsNullFunctionName}({ rawValueExpression }) ? default(System.DateTime?) : System.DateTime.Parse({ rawValueExpression })",
            DateTimeOffset => $"System.DateTimeOffset.Parse({ rawValueExpression })",
            NullableDateTimeOffset => $"{IsNullFunctionName}({ rawValueExpression }) ? default(System.DateTimeOffset?) : System.DateTimeOffset.Parse({ rawValueExpression })",
            Decimal => $"decimal.Parse({ rawValueExpression })",
            NullableDecimal => $"{IsNullFunctionName}({ rawValueExpression }) ? default(decimal?) : decimal.Parse({ rawValueExpression })",
            Double => $"double.Parse({ rawValueExpression })",
            NullableDouble => $"{IsNullFunctionName}({ rawValueExpression }) ? default(double?) : double.Parse({ rawValueExpression })",
            Guid => $"System.Guid.Parse({ rawValueExpression })",
            NullableGuid => $"{IsNullFunctionName}({ rawValueExpression }) ? default(System.Guid?) : System.Guid.Parse({ rawValueExpression })",
            Int64 => $"long.Parse({ rawValueExpression })",
            NullableInt64 => $"{IsNullFunctionName}({ rawValueExpression }) ? default(long?) : long.Parse({ rawValueExpression })",
            Int32 => $"int.Parse({ rawValueExpression })",
            NullableInt32 => $"{IsNullFunctionName}({ rawValueExpression }) ? default(int?) : int.Parse({ rawValueExpression })",
            Int16 => $"short.Parse({ rawValueExpression })",
            NullableInt16 => $"{IsNullFunctionName}({ rawValueExpression }) ? default(short?) : short.Parse({ rawValueExpression })",
            Byte => $"byte.Parse({ rawValueExpression })",
            NullableByte => $"{IsNullFunctionName}({ rawValueExpression }) ? default(byte?) : byte.Parse({ rawValueExpression })",
            Timespan => $"System.TimeSpan.Parse({ rawValueExpression })",
            NullableTimespan => $"{IsNullFunctionName}({ rawValueExpression }) ? default(System.TimeSpan?) : System.TimeSpan.Parse({ rawValueExpression })",
            _ => throw new System.NotImplementedException("missing parser for " + bestType),
        };

        internal static string ToLiteral<T>(this T input) => Memoizer.Instance.Get(new { input }, () =>
            new CodePrimitiveExpression(input).GenCode());

        internal static string GenCode(this CodeExpression expr)
        {
            using var writer = new StringWriter();
            Provider.GenerateCodeFromExpression(expr, writer, null);
            return writer.ToString();
        }

        internal static string ToVerbatimString(this string s) =>
            $"@\"{s.Replace("\"", "\"\"")}\"";

        private static (HashSet<string> registry, object mutex) GetIdRegistry(string scope) =>
            Memoizer.Instance.Get(new { scope }, () => (new HashSet<string>(), new object()));


        internal static string ToIdentifier(this string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new System.ArgumentException("string was empty", nameof(s));

            return Memoizer.Instance.Get(s, () =>
            {
                if (Provider.IsValidIdentifier(s))
                    return s;

                // CreateValidIdentifier(string) only returns a different value
                // when the passed value is a reserved keyword
                // so this code must check for non-identifier characters
                // and leading digits

                var identifier = Regex.Replace(s, @"[^a-zA-Z0-9_]", "_");

                if (char.IsDigit(identifier[0]))
                    return "_" + identifier;

                return Provider.IsValidIdentifier(identifier)
                    ? identifier
                    : Provider.CreateValidIdentifier(identifier);
            });
        }
        internal static string ToIdentifier(this string s, string scope)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new System.ArgumentException("string was empty", nameof(s));

            return Memoizer.Instance.Get(new { scope, s }, () =>
            {
                var identifier = ToIdentifier(s);

                var (registry, mutex) = GetIdRegistry(scope);
                lock (mutex)
                {
                    var attempt = identifier;

                    var i = 2;
                    while (!registry.Add(attempt))
                        attempt = identifier + i++;

                    identifier = attempt;
                }

                return identifier;
            });
        }


    }
}
