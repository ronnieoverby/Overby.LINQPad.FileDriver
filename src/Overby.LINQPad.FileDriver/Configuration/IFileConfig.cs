using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public interface IFileConfig
    {
        string RelativePath { get; set; }

        bool Ignore { get; set; }

        byte[] LastHash { get; set; }

        long LastLength { get; set; }

        DateTime LastWriteTimeUtc { get; set; }

        List<ExplorerItem> GetFileChildItems();
    }
}