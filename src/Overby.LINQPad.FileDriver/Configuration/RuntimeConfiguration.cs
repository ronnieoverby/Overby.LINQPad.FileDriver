using System.IO;

namespace Overby.LINQPad.FileDriver.Configuration
{
    /// <summary>
    /// Used by queries at runtime to alter file configs.
    /// </summary>
    public static class RuntimeConfiguration
    {
        private static readonly object _mutex = new object();

        private static (RootConfig rootConfig, DirectoryInfo rootDir) _state;

        public static RootConfig GetRootConfig(string rootPath)
        {
            lock (_mutex)
            {
                if (_state == default)
                {
                    var rootDir = new DirectoryInfo(rootPath);
                    _state = (RootConfig.LoadRootConfig(rootDir), rootDir);
                }
            }

            return _state.rootConfig;
        }

        public static bool ConfigChanges => _state != default;

        public static void SaveRootConfig()
        {
            lock (_mutex)
            {
                if (ConfigChanges)
                {
                    _state.rootConfig.Save(_state.rootDir);
                    _state = default;
                }
            }
        }


        public static bool ForceRefresh { private get; set; }

        public static bool ShouldRefresh =>
            ForceRefresh || ConfigChanges;
    }
}
