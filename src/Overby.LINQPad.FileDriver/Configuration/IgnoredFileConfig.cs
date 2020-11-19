using System;
using System.IO;

namespace Overby.LINQPad.FileDriver.Configuration
{
    /// <summary>
    /// Not going to ack. this file.
    /// </summary>
    class IgnoredFileConfig : FileConfig
    {
        public override object GetUserConfig(string fileAbsPath)
        {
            return this;
        }

        public override Type GetUserConfigType()
        {
            return GetType();
        }

        public override void HashConfigValues(Action<object> write)
        {

        }

        public override void UpdateFromUserConfig(object userConfig)
        {
        }
    }
}