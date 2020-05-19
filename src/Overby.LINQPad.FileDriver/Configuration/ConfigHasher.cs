using System;
using System.IO;
using System.Security.Cryptography;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public static class ConfigHasher
    {
        public static byte[] HashConfig(IFileConfig fileConfig)
        {
            if (fileConfig is null)
                throw new ArgumentNullException(nameof(fileConfig));

            using var md5 = MD5.Create();
            using var stream = new CryptoStream(Stream.Null, md5, CryptoStreamMode.Write);
            using var writer = new BinaryWriter(stream);
            writer.WriteDelimiter();

            fileConfig.HashConfigValues(value =>
            {
                writer.Write(value?.ToString() ?? "659ec705-5088-4d68-b22b-3d0672db8d75");
                writer.WriteDelimiter();
            });

            writer.Flush();
            stream.FlushFinalBlock();
            return md5.Hash;
        }
    }
}
