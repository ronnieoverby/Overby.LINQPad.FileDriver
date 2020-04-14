using LINQPad;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Overby.LINQPad.FileDriver
{
    public static class Extensions
    {
        public static void WriteCsv<T>(this IEnumerable<T> sequence, string path)
        {
            Util.WriteCsv(sequence, path);
        }

        internal static string FileMD5(this string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(stream);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        public static T ParseEnum<T>(this string s, bool ignoreCase = true) => 
            (T)Enum.Parse(typeof(T), s, ignoreCase);

        public static string NewLineJoin<T>(this IEnumerable<T> sequence) =>
            string.Join(Environment.NewLine, sequence);

        public static string StringJoin<T>(this IEnumerable<T> sequence, string separator) =>
            string.Join(separator, sequence);
    }
}
