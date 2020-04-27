using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Overby.LINQPad.FileDriver
{
    internal static class InternalExtensions
    {
        public static string ComputeHash(this string s)
        {
            var encoded = Encoding.UTF8.GetBytes(s);
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(encoded);
            return hash.ToHexString();
        }

        static readonly string _uniqueIdentifierScope = Guid.NewGuid().ToString();

        public static string UniqueIdentifier(this string s)
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            return Memoizer.Instance.Get(s, () =>
                Guid.NewGuid().ToString("n").ToIdentifier(_uniqueIdentifierScope));
        }

        public static string ToHexString(this IEnumerable<byte> bytes) =>
            string.Concat(bytes.Select(b => b.ToString("x2")));

        public static IEnumerable<(ExplorerItem Item, T Tag)> WithTag<T>(this IEnumerable<ExplorerItem> items) where T : class =>
            from item in items
            let tag = item.Tag as T
            where tag != null
            select (item, tag);

        public static IEnumerable<T> GetTags<T>(this IEnumerable<ExplorerItem> items) where T : class =>
            items.WithTag<T>().Select(x => x.Tag);

        public static IEnumerable<ExplorerItem> Flatten(this IEnumerable<ExplorerItem> items) =>
            items.FlattenBreadthFirst(x => x.Children);

        public static bool EqualsI(this string s, string other) =>
            s.Equals(other, StringComparison.OrdinalIgnoreCase);

        public static T Clone<T>(this T value)
        {
            // this is wrapped because json.net will not
            // polymorphically deserialize root level object
            var wrapped = new { value };

            // serialize
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            Serializer.Serialize(writer, wrapped);
            writer.Flush();

            // deserialize
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return Deserialize(wrapped).value;

            W Deserialize<W>(W _) => Serializer.Deserialize<W>(reader);
        }

        public static T ParseEnum<T>(this string s, bool ignoreCase = true) =>
            (T)Enum.Parse(typeof(T), s, ignoreCase);

        public static string NewLineJoin<T>(this IEnumerable<T> sequence) =>
            string.Join(Environment.NewLine, sequence);

        public static string NewLineJoin<T>(this IEnumerable<T> sequence, Func<string, string> formatNewLine) =>
            string.Join(formatNewLine(Environment.NewLine), sequence);

        public static string StringJoin<T>(this IEnumerable<T> sequence, string separator) =>
            string.Join(separator, sequence);

        public static IEnumerable<T> FlattenBreadthFirst<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> getChildren)
        {
            var q = new Queue<T>(items);

            while (q.Count > 0)
            {
                var item = q.Dequeue();
                yield return item;

                var children = getChildren(item);

                if (children != null)
                    foreach (var child in children)
                        q.Enqueue(child);
            }
        }

        public static void WriteLines(this TextWriter writer, IEnumerable<string> strings)
        {
            foreach (var s in strings)
                writer.WriteLine(s);
        }

        public static IDisposable Surround(this TextWriter writer, string first, string last, bool newlines = true)
        {
            var write = newlines ? (Action<string>)writer.WriteLine : writer.Write;
            write(first);
            return new DelegatedDisposable(() => write(last));
        }

        public static IDisposable Brackets(this TextWriter writer, string beforeBrackets = "", bool newlines = true) =>
            Surround(writer, beforeBrackets + "{", "}", newlines);

        public static IDisposable NameSpace(this TextWriter writer, params string[] nameSpaceParts) =>
            Brackets(writer, $"namespace {nameSpaceParts.StringJoin(".")}", newlines: true);

        public static IDisposable Region(this TextWriter writer, DirectoryInfo folder) =>
            Region(writer, "Folder: " + folder.FullName);

        public static IDisposable Region(this TextWriter writer, FileInfo file) =>
            Region(writer, "File: " + file.FullName);

        public static IEnumerable<string> ReadLines(this TextReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null) break;
                yield return line;
            }
        }

        public static void MemberComment(this TextWriter writer, string summary)
        {
            var reader = new StringReader(summary);
            writer.WriteLine($@"
/// <summary>
{reader.ReadLines().Select(line => "/// " + line).NewLineJoin()}
/// </summary>");
        }

        public static IDisposable Region(this TextWriter writer, string text)
        {
            writer.WriteLine();
            writer.WriteLine($"#region {text}");
            return new DelegatedDisposable(() =>
            {
                writer.WriteLine();
                writer.WriteLine($"#endregion");
            });
        }

        public static string GetNameSpace(this FileSystemInfo file, DirectoryInfo root, int skip = 0)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            if (root is null)
                throw new ArgumentNullException(nameof(root));

            var keyData = new
            {
                file = file.FullName,
                root = root.FullName,
                skip
            };

            return Memoizer.Instance.Get(keyData, () =>
                file.EnumeratePath(root).Select(x => x.Name.ToIdentifier(x.GetParentDirectory().FullName)).Reverse().Skip(skip).StringJoin("."));
        }
    }
}