using NUnit.Framework;
using Overby.LINQPad.FileDriver;
using System;
using System.CodeDom.Compiler;
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

        [Test]
        [TestCase("test", "test")]
        [TestCase("bool", "_bool")]
        [TestCase("Bool", "Bool")]
        [TestCase("class", "_class")]
        [TestCase("123", "_123")]
        [TestCase("class123", "class123")]
        [TestCase("123class", "_123class")]
        [TestCase("hello world!", "hello_world_")]
        [TestCase("yeet world.csv", "yeet_world_csv")]
        [TestCase(" ", "_")]
        [TestCase("\t", "_")]
        [TestCase("\r\n", "__")]
        public void ValidIdentifierTests(string input, string expected)
        {
            string actual = input.ToIdentifier();
            Assert.AreEqual(expected, actual);
            
            using var codeDomProvider = CodeDomProvider.CreateProvider("CSharp");
            Assert.True(codeDomProvider.IsValidIdentifier(actual));
        }
    }
}