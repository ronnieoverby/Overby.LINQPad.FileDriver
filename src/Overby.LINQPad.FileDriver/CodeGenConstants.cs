namespace Overby.LINQPad.FileDriver
{
    public static class CodeGenConstants
    {
        #region Symbols
        public const string RecordClassName = "Record";
        public const string ReaderClassName = "Reader";
        public const string ReaderReadMethodName = "Read";
        public const string ReaderFilePathPropertyName = "FilePath";
        public const string SchemaNameSpace = "Schema";
        public const string SchemaClassName = "Schema";

        public const string IsNullFunctionName = "IsNull";
        public const string ParseBoolFunctionName = "ParseBool";
        #endregion

        public static string IEnumerable(string ofType) =>
            $"System.Collections.Generic.IEnumerable<{ofType}>";

        public const string StreamReader = "System.IO.StreamReader";
    }
}
