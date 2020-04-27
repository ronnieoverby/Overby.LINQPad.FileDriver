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

        public byte[] LastHash { get; set; } 

        public bool Ignore { get; set; } = false;

        public virtual List<ExplorerItem> GetFileChildItems() => new List<ExplorerItem>();
    }
}
