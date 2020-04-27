using System.IO;

namespace Overby.LINQPad.FileDriver
{
    internal class FolderTag:IRefFileSystemInfo
    {
        public DirectoryInfo Folder { get; }

        FileSystemInfo IRefFileSystemInfo.FileSystemInfo => Folder;

        public FolderTag(DirectoryInfo folder)
        {
            Folder = folder ?? throw new System.ArgumentNullException(nameof(folder));
        }

    }
}
