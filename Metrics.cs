using System;

namespace Overby.LINQPad.FileDriver
{
    public class Metrics
    {
        public TimeSpan CodeGenDuration { get; set; }
        public TimeSpan GetSchemaAndBuildAssemblyDuration { get; internal set; }
        public TimeSpan BuildAssemblyDuration { get; internal set; }
        public TimeSpan GenerateCodeDuration { get; internal set; }
    }
}
