using LINQPad.Extensibility.DataContext;
using Overby.LINQPad.FileDriver.Analysis;
using Overby.LINQPad.FileDriver.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Overby.LINQPad.FileDriver
{
    public static class FileVisitor
    {
        public static List<ExplorerItem> VisitRoot(DirectoryInfo root, RootConfig rootConfig)
        {
            var mutex = new object();
            return VisitFolder(root).Children;

            ExplorerItem VisitFolder(DirectoryInfo folder)
            {
                var children = new List<ExplorerItem>();
                var mutex = new object();

                void AddChild(ExplorerItem item)
                {
                    lock (mutex)
                        children.Add(item);
                }

                var subs = folder.EnumerateDirectories();
                var files = folder.EnumerateFiles().Where(f =>
                    !f.Name.Equals(RootConfig.FileName, StringComparison.OrdinalIgnoreCase));

                var popts = new ParallelOptions()
#if DEBUG
                { MaxDegreeOfParallelism = 1 }
#endif
                ;

                Parallel.ForEach(subs, popts, sub =>
                {
                    var item = VisitFolder(sub);
                    if (item.Children.Count > 0)
                        AddChild(item);
                });

                Parallel.ForEach(files, popts, file =>
                {
                    var item = VisitFile(file);
                    if (item != null)
                        AddChild(item);
                });

                return new ExplorerItem(folder.Name, ExplorerItemKind.Schema, ExplorerIcon.Schema)
                {
                    Children = children.OrderBy(x => x.Tag != null).ThenBy(x => x.Text).ToList(),
                    ToolTipText = folder.FullName,
                    Tag = new FolderTag(folder),
                    DragText = folder.GetNameSpace(root, skip: 1)
                };
            }

            ExplorerItem VisitFile(FileInfo file)
            {
                var relativePath = root.GetRelativePathTo(file);
                var codeGenerator = CreateCodeGenerator(file);                
                var newFileHash = new Lazy<byte[]>(() => file.ComputeHash());

                if (codeGenerator == null)
                {
                    // now way to analyze the file; ignore it
                    // return IgnoredFileConfig to avoid repeated hashing
                    // todo could reset by driver version
                    // example, can't analyze file now
                    // but maybe a later driver version could
                    // so don't hash until the driver is updated
                    lock (mutex)
                        rootConfig.AddOrReplace(new IgnoredFileConfig
                        {
                            RelativePath = relativePath,
                            Ignore = true
                        });

                    return null;
                }

                var fileConfig = FindFileConfigByPath(relativePath);
                var newConfigHash = new Lazy<byte[]>(() => ConfigHasher.HashConfig(fileConfig));

                if (fileConfig?.Ignore == true || fileConfig is IgnoredFileConfig)
                    // ignored
                    return null;

                var isNewFile = fileConfig == null;
                if (isNewFile || HasFileChanged())
                {
                    // known hash?
                    var similarFileConfig = FindFileConfigByHash(newFileHash.Value);
                    if (similarFileConfig != null)
                    {
                        // copy similar config
                        fileConfig = similarFileConfig.Clone();
                        fileConfig.Ignore = false;
                    }
                    else
                    {
                        fileConfig = codeGenerator.UpdateFileConfig(file, fileConfig);
                    }

                    lock (mutex)
                        rootConfig.AddOrReplace(fileConfig);
                }
                else if (HasConfigChanged())
                {
                    fileConfig = codeGenerator.UpdateFileConfig(file, fileConfig);

                    lock (mutex)
                        rootConfig.AddOrReplace(fileConfig);
                }
            
                // update file's stamps
                fileConfig.RelativePath = relativePath;
                fileConfig.LastLength = file.Length;
                fileConfig.LastWriteTimeUtc = file.LastWriteTimeUtc;

                if (fileConfig.FileHash is null || fileConfig.FileHash.Length == 0 || newFileHash.IsValueCreated)
                    fileConfig.FileHash = newFileHash.Value;

                fileConfig.ConfigHash = newConfigHash.Value;

                return new ExplorerItem(file.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                {
                    IsEnumerable = true,
                    ToolTipText = file.FullName,
                    DragText = file.GetNameSpace(root, skip: 1),
                    Tag = new FileTag(file, fileConfig, codeGenerator),
                };

                bool HasConfigChanged() => 
                    fileConfig.ConfigHash == null || !fileConfig.ConfigHash.SequenceEqual(newConfigHash.Value);

                bool HasFileChanged()
                {
                    var fileWasUpdated = fileConfig.LastLength != file.Length
                       || fileConfig.LastWriteTimeUtc != file.LastWriteTimeUtc;

                    if (fileWasUpdated)
                    {
                        var lastHash = fileConfig.FileHash ?? new byte[0];
                        return !lastHash.SequenceEqual(newFileHash.Value); // hash changed?
                    }

                    return false;
                }
            }

            IFileConfig FindFileConfigByPath(string relativePath)
            {
                lock (mutex)
                    return rootConfig.Files?.SingleOrDefault(fc => fc.RelativePath?.EqualsI(relativePath) == true);
            }

            IFileConfig FindFileConfigByHash(byte[] hash)
            {
                lock (mutex)
                    return rootConfig.Files?
                        .Where(fc => fc.FileHash?.SequenceEqual(hash) == true)
                        .OrderByDescending(fc => fc.LastWriteTimeUtc)
                        .FirstOrDefault();
            }

            static ICodeGenerator CreateCodeGenerator(FileInfo file) => file.Extension.ToLowerInvariant() switch
            {
                ".csv" => new Csv.CsvGenerator(),
                ".tsv" => new Csv.CsvGenerator(),
                _ => null
            };
        }
    }
}
