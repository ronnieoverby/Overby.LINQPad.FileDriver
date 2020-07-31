using Overby.LINQPad.FileDriver.Configuration;
using System;
using System.IO;

namespace Overby.LINQPad.FileDriver.Analysis
{
    internal interface ICodeGenerator
    {
        IFileConfig UpdateFileConfig(FileInfo file, IFileConfig previousConfig);

        (Action<TextWriter> WriteRecordMembers,
         Action<TextWriter> WriteEnumeratorImplementation)
            GetCodeGenerators(IFileConfig fileConfig);
    }
}