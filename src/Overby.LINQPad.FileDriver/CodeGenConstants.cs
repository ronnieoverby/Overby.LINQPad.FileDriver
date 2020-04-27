namespace Overby.LINQPad.FileDriver
{
    public static class CodeGenConstants
    {
        #region Symbols
        public const string ReaderFilePathVariableName = "filePath";
        public const string RecordClassName = "Record";
        public const string ReaderClassName = "Reader";
        public const string ReaderReadMethodName = "Read";
        public const string ReaderFilePathConstName = "FilePath";
        public const string SchemaNameSpace = "Schema";
        public const string SchemaClassName = "Schema"; 
        #endregion

        public static string IEnumerable(string ofType) =>
            $"System.Collections.Generic.IEnumerable<{ofType}>";

        public const string StreamReader = "System.IO.StreamReader";
    }
}
