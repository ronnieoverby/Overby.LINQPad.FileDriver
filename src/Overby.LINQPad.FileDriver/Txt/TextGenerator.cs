using Overby.LINQPad.FileDriver.Analysis;
using Overby.LINQPad.FileDriver.Configuration;
using System;
using System.IO;
using static Overby.LINQPad.FileDriver.CodeGenConstants;

namespace Overby.LINQPad.FileDriver.Txt
{
    public class TextGenerator : ICodeGenerator
    {
        public (Action<TextWriter> WriteRecordMembers, Action<TextWriter> WriteReaderImplementation) GetCodeGenerators(IFileConfig fileConfig)
        {
            return (WriteRecordImpl, WriteReaderImplementation);

            void WriteRecordImpl(TextWriter writer)
            {
                writer.WriteLine($@"
public int LineNumber {{get;}}
public string Line {{get;}}

public {RecordClassName}(string line, int lineNumber)
{{
    Line = line;
    LineNumber = lineNumber;
}}");
            }
            
            void WriteReaderImplementation(TextWriter writer)
            {
                writer.WriteLine($@"using(var reader = new System.IO.StreamReader({ReaderFilePathVariableName}))
{{
    foreach(var (line, index) in Overby.Extensions.Text.TextReaderExtensions.ReadLinesIndexed(reader))
        yield return new {RecordClassName}(line, index + 1);
}}");
            }
        }

        public IFileConfig UpdateFileConfig(FileInfo file, IFileConfig previousConfig) => (TextConfig)previousConfig ?? new TextConfig();
    }
}
