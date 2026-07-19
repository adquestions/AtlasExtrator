using System;
using System.IO;
using System.Reflection;

namespace AtlasExtractor
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("Atlas Extractor");
            Console.WriteLine("========================================");

            try
            {
                string projectFolder = Path.GetFullPath(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));

                string inputFolder = Path.GetFullPath(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\input"));

                string outputFolder = Path.Combine(projectFolder, "output");

                Directory.CreateDirectory(outputFolder);

                Console.WriteLine("Input folder:");
                Console.WriteLine(inputFolder);
                Console.WriteLine();

                Console.WriteLine("Output folder:");
                Console.WriteLine(outputFolder);
                Console.WriteLine();

                using (var assemblyLoader = new AssemblyLoader(inputFolder, Console.WriteLine))
                {
                    assemblyLoader.LoadIfPresent("LZ4.dll");
                    Assembly fmCommonAssembly = assemblyLoader.LoadRequired("FMCommon.dll");
                    assemblyLoader.LoadIfPresent("FMBase.dll");
                    assemblyLoader.LoadIfPresent("Assembly-CSharp.dll");
                    Assembly gameAssembly = assemblyLoader.LoadRequired("GameProj.dll");

                    string metaPath = assemblyLoader.FindFile("Meta.bytes");

                    if (metaPath == null)
                    {
                        throw new FileNotFoundException(
                            "Meta.bytes was not found in: " + inputFolder);
                    }

                    var metaLoader = new MetaLoader(
                        fmCommonAssembly,
                        metaPath,
                        Console.WriteLine);

                    object context = metaLoader.Load();

                    var csvWriter = new CsvWriter();
                    var exporter = new MetaExporter(
                        context,
                        gameAssembly,
                        outputFolder,
                        csvWriter,
                        Console.WriteLine);

                    Models.ExportSummary summary = exporter.ExportAll();

                    Console.WriteLine();
                    Console.WriteLine("EXPORT COMPLETE");
                    Console.WriteLine("========================================");
                    Console.WriteLine("Tables discovered: " + summary.TablesDiscovered.ToString("N0"));
                    Console.WriteLine("Tables exported:   " + summary.TablesExported.ToString("N0"));
                    Console.WriteLine("Tables empty:      " + summary.TablesEmpty.ToString("N0"));
                    Console.WriteLine("Tables failed:     " + summary.TablesFailed.ToString("N0"));
                    Console.WriteLine("Rows exported:     " + summary.RowsExported.ToString("N0"));
                    Console.WriteLine();
                    Console.WriteLine("Output:");
                    Console.WriteLine(outputFolder);

                    return summary.TablesFailed == 0 ? 0 : 2;
                }
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine();
                Console.WriteLine("GAME METHOD ERROR");
                Console.WriteLine("========================================");
                Console.WriteLine(
                    ex.InnerException != null
                        ? ex.InnerException.ToString()
                        : ex.ToString());

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("FATAL ERROR");
                Console.WriteLine("========================================");
                Console.WriteLine(ex);

                return 1;
            }
        }
    }
}
