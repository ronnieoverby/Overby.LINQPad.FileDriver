using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public class RootConfig
    {
        public const string FileName = "4c5b497501bc4040a0c41dbf47805704";

        public Guid PopulationId { get; set; }

        public List<IFileConfig> Files { get; set; } = new List<IFileConfig>();

        public void AddOrReplace(IFileConfig fileConfig)
        {
            Files.RemoveAll(x => x.RelativePath.EqualsI(fileConfig.RelativePath));
            Files.Add(fileConfig);
        }

        public void Save(DirectoryInfo root)
        {
            Files = Files
                .OrderBy(x => Path.GetFileName( x.RelativePath))
                .ThenBy(x=> x.RelativePath.Length)
                
                
                .ToList();
            Serializer.Save(GetSaveFile(root).FullName, this);
        }

        public static RootConfig LoadRootConfig(DirectoryInfo root)
        {
            var saveFile = GetSaveFile(root);
            RootConfig rootConfig = saveFile.Exists ? Serializer.Load<RootConfig>(saveFile.FullName) : new RootConfig();
            rootConfig.PopulationId = Guid.NewGuid();
            return rootConfig;
        }

        private static FileInfo GetSaveFile(DirectoryInfo folder) =>
            folder.GetFile(FileName);
    }
}
