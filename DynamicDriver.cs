using LINQPad;
using LINQPad.Extensibility.DataContext;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Overby.LINQPad.FileDriver
{
	public class DynamicDriver : DynamicDataContextDriver
	{
		static DynamicDriver()
		{
			// Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:
			//AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
			//{
			//	if (args.Exception.StackTrace.Contains ("Overby.LINQPad.FileDriver"))
			//		Debugger.Launch ();
			//};
		}
	
		public override string Name => "(Name for your driver)";

		public override string Author => "Ronnie Overby";

		public override string GetConnectionDescription (IConnectionInfo cxInfo)
			=> "(Description for this connection)";

		public override bool ShowConnectionDialog (IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
			=> new ConnectionDialog (cxInfo).ShowDialog () == true;

		public override List<ExplorerItem> GetSchemaAndBuildAssembly (
			IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
		{
			// TODO - implement
			return new ExplorerItem[0].ToList();
		}
		
#if NETCORE
		// Put stuff here that's just for LINQPad 6+ (.NET Core).
#else
		// Put stuff here that's just for LINQPad 5 (.NET Framework)
#endif
	}
}