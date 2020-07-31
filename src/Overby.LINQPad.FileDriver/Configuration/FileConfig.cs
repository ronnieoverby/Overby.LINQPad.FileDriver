using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public abstract class FileConfig : IFileConfig
    {
        public string RelativePath { get; set; }

        public long LastLength { get; set; }

        public DateTime LastWriteTimeUtc { get; set; }

        public byte[] FileHash { get; set; } 

        public bool Ignore { get; set; } = false;

        public byte[] ConfigHash { get; set; }

        public virtual List<ExplorerItem> GetFileChildItems() => new List<ExplorerItem>();
        public abstract object GetUserConfig();
        public abstract Type GetUserConfigType();
        public abstract void HashConfigValues(Action<object> write);
        public abstract void UpdateFromUserConfig(object userConfig);
    }
}
