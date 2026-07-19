using System;
using System.IO;

namespace AtlasExtractor
{
    internal static class OutputCleaner
    {
        public static void Clean(string outputFolder)
        {
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                throw new ArgumentException(
                    "An output folder is required.",
                    "outputFolder");
            }

            Directory.CreateDirectory(outputFolder);

            foreach (string csvPath in Directory.EnumerateFiles(
                outputFolder,
                "*.csv",
                SearchOption.TopDirectoryOnly))
            {
                File.Delete(csvPath);
            }

            DeleteIfPresent(
                Path.Combine(
                    outputFolder,
                    "_export_failures.log"));

            DeleteIfPresent(
                Path.Combine(
                    outputFolder,
                    "_unsupported_tables.log"));
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
