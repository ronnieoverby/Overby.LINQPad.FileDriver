using System.IO;

namespace Overby.LINQPad.FileDriver
{
    internal interface IRefFileSystemInfo
    {
        FileSystemInfo FileSystemInfo { get; }
    }
}