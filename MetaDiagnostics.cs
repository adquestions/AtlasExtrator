using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AtlasExtractor.Models;

namespace AtlasExtractor
{
    internal static class MetaDiagnostics
    {
        public static void Write(
            string outputFolder,
            IEnumerable<Type> candidateTypes,
            IEnumerable<ExportResult> results)
        {
            if (outputFolder == null)
            {
                throw new ArgumentNullException("outputFolder");
            }

            Directory.CreateDirectory(outputFolder);

            List<Type> types = candidateTypes
                .Where(type => type != null)
                .OrderBy(type => type.FullName)
                .ToList();

            List<ExportResult> resultList = results
                .Where(result => result != null)
                .ToList();

            WriteCatalog(
                Path.Combine(
                    outputFolder,
                    "_meta_type_catalog.csv"),
                types,
                resultList);

            WriteUnusedTypes(
                Path.Combine(
                    outputFolder,
                    "_unused_meta_types.csv"),
                types,
                resultList);
        }

        private static void WriteCatalog(
            string path,
            IEnumerable<Type> types,
            IEnumerable<ExportResult> results)
        {
            var usedTypeNames = new HashSet<string>(
                results
                    .Where(result => result.Success)
                    .Select(result => result.MetaTypeName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)),
                StringComparer.Ordinal);

            using (var writer = new StreamWriter(
                path,
                false,
                new UTF8Encoding(true)))
            {
                writer.WriteLine(
                    "Assembly,FullTypeName,TypeName,UsedBySuccessfulExport," +
                    "GenerateMetaSignatures,GetMetaEnumeratorDeclaringType," +
                    "StaticStringMembers");

                foreach (Type type in types)
                {
                    string signatures = string.Join(
                        " | ",
                        type.GetMethods(
                                BindingFlags.Public |
                                BindingFlags.NonPublic |
                                BindingFlags.Static)
                            .Where(
                                method => string.Equals(
                                    method.Name,
                                    "GenerateMeta",
                                    StringComparison.Ordinal))
                            .Select(FormatMethod));

                    MethodInfo enumeratorMethod =
                        ReflectionHelpers.FindMethodInHierarchy(
                            type,
                            "GetMetaEnumerator",
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static);

                    string staticStrings = ReadStaticStrings(type);

                    writer.WriteLine(
                        string.Join(
                            ",",
                            Escape(type.Assembly.GetName().Name),
                            Escape(type.FullName),
                            Escape(type.Name),
                            Escape(
                                usedTypeNames.Contains(type.FullName)
                                    ? "true"
                                    : "false"),
                            Escape(signatures),
                            Escape(
                                enumeratorMethod == null
                                    ? string.Empty
                                    : enumeratorMethod.DeclaringType.FullName),
                            Escape(staticStrings)));
                }
            }
        }

        private static void WriteUnusedTypes(
            string path,
            IEnumerable<Type> types,
            IEnumerable<ExportResult> results)
        {
            var usedTypeNames = new HashSet<string>(
                results
                    .Where(result => result.Success)
                    .Select(result => result.MetaTypeName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)),
                StringComparer.Ordinal);

            using (var writer = new StreamWriter(
                path,
                false,
                new UTF8Encoding(true)))
            {
                writer.WriteLine(
                    "Assembly,FullTypeName,GenerateMetaSignatures");

                foreach (Type type in types
                    .Where(type => !usedTypeNames.Contains(type.FullName)))
                {
                    string signatures = string.Join(
                        " | ",
                        type.GetMethods(
                                BindingFlags.Public |
                                BindingFlags.NonPublic |
                                BindingFlags.Static)
                            .Where(
                                method => string.Equals(
                                    method.Name,
                                    "GenerateMeta",
                                    StringComparison.Ordinal))
                            .Select(FormatMethod));

                    writer.WriteLine(
                        string.Join(
                            ",",
                            Escape(type.Assembly.GetName().Name),
                            Escape(type.FullName),
                            Escape(signatures)));
                }
            }
        }

        private static string FormatMethod(MethodInfo method)
        {
            return method.DeclaringType.FullName +
                "." +
                method.Name +
                "(" +
                string.Join(
                    ", ",
                    method.GetParameters()
                        .Select(
                            parameter =>
                                parameter.ParameterType.FullName +
                                " " +
                                parameter.Name)) +
                ")";
        }

        private static string ReadStaticStrings(Type type)
        {
            var values = new List<string>();

            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly))
            {
                if (field.FieldType != typeof(string))
                {
                    continue;
                }

                try
                {
                    string value = field.GetValue(null) as string;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(
                            field.Name + "=" + value);
                    }
                }
                catch
                {
                    // Diagnostic only; inaccessible initializers are ignored.
                }
            }

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly))
            {
                if (property.PropertyType != typeof(string) ||
                    !property.CanRead ||
                    property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                try
                {
                    string value =
                        property.GetValue(null, null) as string;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(
                            property.Name + "=" + value);
                    }
                }
                catch
                {
                    // Diagnostic only; throwing getters are ignored.
                }
            }

            return string.Join(" | ", values);
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            bool needsQuotes =
                value.IndexOf(',') >= 0 ||
                value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0;

            if (!needsQuotes)
            {
                return value;
            }

            return "\"" +
                value.Replace("\"", "\"\"") +
                "\"";
        }
    }
}
