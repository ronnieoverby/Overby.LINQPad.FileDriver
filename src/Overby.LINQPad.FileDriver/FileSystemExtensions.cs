using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Overby.LINQPad.FileDriver
{
    internal static class FileSystemExtensions
    {
        public static string GetNameIdentifier(this FileSystemInfo fsi) =>
            fsi.Name.ToIdentifier(fsi.GetParentDirectory().FullName);

        public static byte[] ComputeHash(this FileInfo file)
        {
            using var stream = file.OpenRead();
            using var md5 = MD5.Create();
            return md5.ComputeHash(stream);
        }

        public static string GetRelativePathFrom(this FileSystemInfo to, FileSystemInfo from) =>
            from.GetRelativePathTo(to);

        public static string GetRelativePathTo(this FileSystemInfo from, FileSystemInfo to)
        {
            static string getPath(FileSystemInfo fsi) =>
                fsi is DirectoryInfo d ? d.FullName.TrimEnd('\\') + "\\" : fsi.FullName;

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        public static DirectoryInfo GetParentDirectory(this FileSystemInfo fileSystemInfo)
        {
            var file = fileSystemInfo as FileInfo;
            return file != null ? file.Directory : ((DirectoryInfo)fileSystemInfo).Parent;
        }

        public static IEnumerable<FileSystemInfo> EnumeratePath(
            this FileSystemInfo from, FileSystemInfo terminus = null)
        {
            while (true)
            {
                yield return from;

                if (terminus?.AreSame(from) == true)
                    break;

                from = from.GetParentDirectory();

                if (from == null)
                    break;
            }
        }

        public static bool AreSame(this FileSystemInfo a, FileSystemInfo b) =>
            (a is FileInfo fa && b is FileInfo fb && fa.AreSame(fb)) ||
            (a is DirectoryInfo da && b is DirectoryInfo db && da.AreSame(db));

        public static bool AreSame(this DirectoryInfo a, DirectoryInfo b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (!a.Name.Equals(b.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (a.Parent == null && b.Parent == null)
                return true;

            if (a.Parent == null || b.Parent == null)
                return false;

            return a.Parent.FullName.Equals(b.Parent.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool AreSame(this FileInfo a, FileInfo b)
        {
            return ReferenceEquals(a, b) || a.FullName.Equals(b.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public static FileInfo GetFile(this DirectoryInfo directory, string file) =>
            new FileInfo(Path.Combine(directory.FullName, file));
    }
}