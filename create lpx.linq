<Query Kind="Program">
  <Namespace>System.IO.Compression</Namespace>
</Query>

void Main()
{
	CreateLPX();
	CreateLPX6();
}

void CreateLPX()
{
	var queryFolder = new FileInfo(Util.CurrentQueryPath).DirectoryName;

	var net46Dir = new DirectoryInfo(
		Path.Combine(queryFolder, "bin", "Release", "net46"));

	var lpxFile = Path.Combine(queryFolder, "Overby.LINQPad.FileDriver.lpx");

	File.Delete(lpxFile);
	ZipFile.CreateFromDirectory(net46Dir.FullName, lpxFile);

	using var fs = File.Open(lpxFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
	using var za = new ZipArchive(fs, ZipArchiveMode.Update);
	var headerEntry = za.CreateEntry("header.xml");
	using var manstream = headerEntry.Open();
	using var manwriter = new StreamWriter(manstream);

	manwriter.WriteLine(
	$@"<?xml version=""1.0"" encoding=""utf-8"" ?>
   <DataContextDriver>
      <MainAssembly>Overby.LINQPad.FileDriver.dll</MainAssembly>
      <SupportUri>http://ronn.io</SupportUri>
   </DataContextDriver>");
}

void CreateLPX6()
{
	var queryFolder = new FileInfo(Util.CurrentQueryPath).DirectoryName;

	var net46Dir = new DirectoryInfo(
		Path.Combine(queryFolder, "bin", "Release", "netcoreapp3.0"));

	var lpxFile = Path.Combine(queryFolder, "Overby.LINQPad.FileDriver.lpx6");

	File.Delete(lpxFile);
	ZipFile.CreateFromDirectory(net46Dir.FullName, lpxFile);
}

