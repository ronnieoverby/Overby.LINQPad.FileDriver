using NUnit.Framework;
using Overby.LINQPad.FileDriver;
using System;
using System.IO;
using System.Linq;

namespace Tests
{
    public class Tests
    {
        [Test]
        public void CanEnumeratePathToRoot()
        {
            var root = new DirectoryInfo(@"c:\yeet\mydata");
            var file = root.GetFile(@"sub1\sub2\file.csv");

            var path = file.EnumeratePath(terminus: root).ToArray();

            Assert.AreEqual(4, path.Length);
            Assert.True(file.AreSame(path[0]));
            Assert.True(root.AreSame(path[^1]));
        }

        [Test]
        public void CanCreateNamespace()
        {
            var root = new DirectoryInfo(@"c:\yeet\mydata");
            var file = root.GetFile(@"sub1\sub2\file.csv");

            var typeName = file.EnumeratePath(terminus: root)
                .Reverse()
                .Select(x => x.GetNameIdentifier())
                .StringJoin(".");

            Assert.AreEqual("mydata.sub1.sub2.file_csv", typeName);
        }

        [Test]
        public void CS_Identifier_Underscores()
        {
            Assert.AreEqual("_2_birds_1_bush_csv", "2 birds 1 bush.csv".ToIdentifier());
        }

        [Test]
        public void FileSystemIdentifiersAre_Deduped_Scoped_To_Parents()
        {
            var file1 = new FileInfo(@"a\a..x");
            var file2 = new FileInfo(@"a\a--x");
            var file3 = new FileInfo(@"b\a--x");

            var id1 = file1.GetNameIdentifier();
            var id2 = file2.GetNameIdentifier();
            var id3 = file3.GetNameIdentifier();

            Assert.AreNotEqual(id1, id2);
            Assert.AreEqual(id1, id3);

        }

       

    }
}