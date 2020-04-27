namespace Overby.LINQPad.FileDriver.Configuration
{
    class StringValues
    {
        public string[] Values { get; set; }
        public bool IgnoreCase { get; set; }

        public StringValues(bool ignoreCase, params string[] values)
        {
            IgnoreCase = ignoreCase;
            Values = values;
        }
    }
}
