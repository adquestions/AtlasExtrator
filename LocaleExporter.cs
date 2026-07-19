using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AtlasExtractor
{
    internal static class LocaleExporter
    {
        public static int ExportEnglishLocale(
            string inputFolder,
            string outputFolder)
        {
            if (string.IsNullOrWhiteSpace(inputFolder))
            {
                throw new ArgumentException(
                    "Input folder cannot be empty.",
                    nameof(inputFolder));
            }

            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                throw new ArgumentException(
                    "Output folder cannot be empty.",
                    nameof(outputFolder));
            }

            string localePath = Path.Combine(
                inputFolder,
                "Locale_en.bytes");

            if (!File.Exists(localePath))
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Locale file not found: " + localePath);

                Console.WriteLine(
                    "Skipping English localization export.");

                return 0;
            }

            Directory.CreateDirectory(outputFolder);

            Assembly fmCommonAssembly = FindLoadedAssembly("FMCommon");

            if (fmCommonAssembly == null)
            {
                string fmCommonPath = Path.Combine(
                    inputFolder,
                    "FMCommon.dll");

                if (!File.Exists(fmCommonPath))
                {
                    throw new FileNotFoundException(
                        "FMCommon.dll was not found.",
                        fmCommonPath);
                }

                fmCommonAssembly =
                    Assembly.LoadFrom(fmCommonPath);
            }

            Type formatterType =
                fmCommonAssembly.GetType(
                    "FibMatrix.Config.ConfigFormatter",
                    true);

            MethodInfo deserializeMethod =
                formatterType
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Static)
                    .FirstOrDefault(method =>
                    {
                        if (!string.Equals(
                                method.Name,
                                "DeserializeLocale",
                                StringComparison.Ordinal))
                        {
                            return false;
                        }

                        ParameterInfo[] parameters =
                            method.GetParameters();

                        return parameters.Length == 1 &&
                               parameters[0].ParameterType ==
                               typeof(byte[]);
                    });

            if (deserializeMethod == null)
            {
                throw new MissingMethodException(
                    "Could not find " +
                    "ConfigFormatter.DeserializeLocale(byte[]).");
            }

            byte[] localeBytes =
                File.ReadAllBytes(localePath);

            object decodedObject =
                deserializeMethod.Invoke(
                    null,
                    new object[] { localeBytes });

            IDictionary<string, string> dictionary =
                ConvertToDictionary(decodedObject);

            string outputPath = Path.Combine(
                outputFolder,
                "localization_en.csv");

            WriteCsv(outputPath, dictionary);

            Console.WriteLine();
            Console.WriteLine("English localization");
            Console.WriteLine("------------------------------");
            Console.WriteLine(
                "Entries exported: " + dictionary.Count);

            Console.WriteLine(
                "Output: " + outputPath);

            ShowExample(dictionary, "citybuilding_name_1");
            ShowExample(dictionary, "top_resource_name_4");

            return dictionary.Count;
        }

        private static Assembly FindLoadedAssembly(
            string assemblyName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(assembly =>
                    string.Equals(
                        assembly.GetName().Name,
                        assemblyName,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static IDictionary<string, string>
            ConvertToDictionary(object decodedObject)
        {
            if (decodedObject == null)
            {
                throw new InvalidOperationException(
                    "DeserializeLocale returned null.");
            }

            var typedDictionary =
                decodedObject as IDictionary<string, string>;

            if (typedDictionary != null)
            {
                return typedDictionary;
            }

            var result =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            var nonGenericDictionary =
                decodedObject as IDictionary;

            if (nonGenericDictionary != null)
            {
                foreach (DictionaryEntry entry
                    in nonGenericDictionary)
                {
                    string key =
                        Convert.ToString(entry.Key) ?? string.Empty;

                    string value =
                        Convert.ToString(entry.Value) ?? string.Empty;

                    result[key] = value;
                }

                return result;
            }

            throw new InvalidOperationException(
                "Unexpected locale result type: " +
                decodedObject.GetType().FullName);
        }

        private static void WriteCsv(
            string outputPath,
            IDictionary<string, string> dictionary)
        {
            var utf8WithBom =
                new UTF8Encoding(
                    true);

            using (var writer = new StreamWriter(
                outputPath,
                false,
                utf8WithBom))
            {
                writer.WriteLine("key,value");

                foreach (KeyValuePair<string, string> item
                    in dictionary.OrderBy(
                        pair => pair.Key,
                        StringComparer.Ordinal))
                {
                    writer.Write(EscapeCsv(item.Key));
                    writer.Write(",");
                    writer.WriteLine(EscapeCsv(item.Value));
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;

            bool requiresQuotes =
                value.IndexOf(',') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0;

            if (!requiresQuotes)
            {
                return value;
            }

            return "\"" +
                   value.Replace("\"", "\"\"") +
                   "\"";
        }

        private static void ShowExample(
            IDictionary<string, string> dictionary,
            string key)
        {
            string value;

            if (dictionary.TryGetValue(key, out value))
            {
                Console.WriteLine(
                    key + " = " + value);
            }
        }
    }
}