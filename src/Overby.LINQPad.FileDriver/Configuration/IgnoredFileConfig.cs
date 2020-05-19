using System;
using System.IO;

namespace Overby.LINQPad.FileDriver.Configuration
{
    /// <summary>
    /// Not going to ack. this file.
    /// </summary>
    class IgnoredFileConfig : FileConfig
    {
        public override void HashConfigValues(Action<object> write)
        {

        }
    }
}