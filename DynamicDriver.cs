using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Overby.LINQPad.FileDriver
{
    public class DynamicDriver : DynamicDataContextDriver
    {
        static DynamicDriver()
        {
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception.StackTrace.Contains("Overby.LINQPad.FileDriver"))
                    Debugger.Launch();
            };
        }

        public override string Name => "Overby File Driver";

        public override string Author => "Ronnie Overby";

        public override string GetConnectionDescription(IConnectionInfo cxInfo) =>
            "A totally bad ass driver!";

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
            => new ConnectionDialog(cxInfo).ShowDialog() == true;

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(
            IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
            var cxProps = new ConnectionProperties(cxInfo);
            var root = new DirectoryInfo(cxProps.DataDirectoryPath);
            return EnumerateExplorerItems(root).ToList();

            static IEnumerable<ExplorerItem> EnumerateExplorerItems(DirectoryInfo directoryInfo)
            {
                foreach (var folder in directoryInfo.EnumerateDirectories())
                {
                    yield return new ExplorerItem(folder.Name, ExplorerItemKind.Category, ExplorerIcon.Schema)
                    {
                        IsEnumerable = false,
                        ToolTipText = folder.FullName,
                        Children = EnumerateExplorerItems(folder).ToList()
                    };
                }

                foreach (var file in directoryInfo.EnumerateFiles())
                {
                    yield return new ExplorerItem(file.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                    {
                        IsEnumerable = true,
                        ToolTipText = file.FullName
                    };
                }
            }
        }

#if NETCORE
        // Put stuff here that's just for LINQPad 6+ (.NET Core).
#else
		// Put stuff here that's just for LINQPad 5 (.NET Framework)
#endif
    }
}