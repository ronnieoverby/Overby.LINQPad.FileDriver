using Overby.LINQPad.FileDriver.Analysis;
using Overby.LINQPad.FileDriver.Configuration;
using System;
using System.IO;

namespace Overby.LINQPad.FileDriver
{
    internal class FileTag : IRefFileSystemInfo
    {
        public FileInfo File { get; set; }
        public IFileConfig FileConfig { get; }

        public ICodeGenerator CodeGenerator { get; set; }        

        FileSystemInfo IRefFileSystemInfo.FileSystemInfo => File;

        public FileTag(FileInfo file, IFileConfig fileConfig, ICodeGenerator codeGenerator)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
            FileConfig = fileConfig ?? throw new ArgumentNullException(nameof(fileConfig));
            CodeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));

        }
    }
}
