using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public interface IFileConfig
    {
        string RelativePath { get; set; }

        bool Ignore { get; set; }

        byte[] FileHash { get; set; }

        long LastLength { get; set; }

        DateTime LastWriteTimeUtc { get; set; }

        List<ExplorerItem> GetFileChildItems();
        
        byte[] ConfigHash { get; set; }

        void HashConfigValues(Action<object> write);

        Type GetUserConfigType();
        object GetUserConfig(string fileAbsolutePath);
        void UpdateFromUserConfig(object userConfig);
    }
}