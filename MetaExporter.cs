using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AtlasExtractor.Models;

namespace AtlasExtractor
{
    internal sealed class MetaExporter
    {
        private readonly object _context;
        private readonly Assembly _gameAssembly;
        private readonly string _outputFolder;
        private readonly CsvWriter _csvWriter;
        private readonly Action<string> _log;
        private readonly List<Type> _candidateTypes;

        public MetaExporter(
            object context,
            Assembly gameAssembly,
            string outputFolder,
            CsvWriter csvWriter,
            Action<string> log)
        {
            _context = context
                ?? throw new ArgumentNullException("context");

            _gameAssembly = gameAssembly
                ?? throw new ArgumentNullException("gameAssembly");

            _outputFolder = outputFolder
                ?? throw new ArgumentNullException("outputFolder");

            _csvWriter = csvWriter
                ?? throw new ArgumentNullException("csvWriter");

            _log = log ?? delegate { };

            _candidateTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(ReflectionHelpers.GetLoadableTypes)
                .Where(type => type != null)
                .Where(
                    type =>
                        type.Name.EndsWith(
                            "Meta",
                            StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            _log(
                "Candidate Meta types discovered: " +
                _candidateTypes.Count.ToString("N0"));
        }

        public ExportSummary ExportAll()
        {
            List<string> tableNames = GetAllMetaNames()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var summary = new ExportSummary
            {
                TablesDiscovered = tableNames.Count
            };

            _log(string.Empty);
            _log(
                "Metadata tables discovered: " +
                tableNames.Count.ToString("N0"));
            _log("========================================");

            int index = 0;

            foreach (string tableName in tableNames)
            {
                index++;

                ExportResult result = ExportOne(
                    tableName,
                    index,
                    tableNames.Count);

                summary.Results.Add(result);

                if (result.Success)
                {
                    if (result.RowCount == 0)
                    {
                        summary.TablesEmpty++;
                    }
                    else
                    {
                        summary.TablesExported++;
                        summary.RowsExported += result.RowCount;
                    }
                }
                else
                {
                    summary.TablesFailed++;
                }
            }

            WriteFailureLog(summary);

            MetaDiagnostics.Write(
                _outputFolder,
                _candidateTypes,
                summary.Results);

            return summary;
        }

        private ExportResult ExportOne(
            string tableName,
            int index,
            int total)
        {
            var result = new ExportResult
            {
                TableName = tableName
            };

            _log(string.Empty);
            _log(
                "[" +
                index.ToString("N0") +
                "/" +
                total.ToString("N0") +
                "] " +
                tableName);

            object itemSet;

            try
            {
                itemSet = GetMetaItemSet(tableName);

                if (itemSet == null)
                {
                    throw new InvalidOperationException(
                        "GetMetaItemSet returned null.");
                }
            }
            catch (Exception ex)
            {
                Exception actual =
                    ex is TargetInvocationException &&
                    ex.InnerException != null
                        ? ex.InnerException
                        : ex;

                result.Success = false;
                result.ErrorMessage =
                    actual.GetType().FullName +
                    ": " +
                    actual.Message;

                _log("  FAILED - " + result.ErrorMessage);
                return result;
            }

            string csvPath = Path.Combine(
                _outputFolder,
                MakeSafeFileName(tableName) + ".csv");

            Exception generatedError = null;

            try
            {
                Type metaType = FindGeneratedMetaType(
                    tableName,
                    itemSet);

                result.MetaTypeName = metaType.FullName;

                InvokeGenerateMeta(metaType, itemSet);

                IEnumerator enumerator =
                    GetGeneratedEnumerator(metaType);

                long rowCount;

                try
                {
                    rowCount = _csvWriter.Write(
                        metaType,
                        enumerator,
                        csvPath);
                }
                finally
                {
                    IDisposable disposable =
                        enumerator as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                result.Success = true;
                result.RowCount = rowCount;
                result.OutputPath = csvPath;

                _log(
                    "  OK - " +
                    rowCount.ToString("N0") +
                    " rows - GENERATED - " +
                    metaType.FullName);

                return result;
            }
            catch (Exception ex)
            {
                generatedError =
                    ex is TargetInvocationException &&
                    ex.InnerException != null
                        ? ex.InnerException
                        : ex;
            }

            try
            {
                var rawExporter = new RawMetaExporter(
                    _csvWriter);

                long rawRowCount;
                string strategy;
                string rawError;

                bool rawSuccess = rawExporter.TryExport(
                    itemSet,
                    csvPath,
                    out rawRowCount,
                    out strategy,
                    out rawError);

                if (rawSuccess)
                {
                    result.Success = true;
                    result.RowCount = rawRowCount;
                    result.OutputPath = csvPath;
                    result.MetaTypeName =
                        "RAW:" + itemSet.GetType().FullName;

                    _log(
                        "  OK - " +
                        rawRowCount.ToString("N0") +
                        " rows - RAW - " +
                        strategy);

                    return result;
                }

                result.Success = false;
                result.ErrorMessage =
                    "Generated export: " +
                    generatedError.GetType().FullName +
                    ": " +
                    generatedError.Message +
                    " | Raw export: " +
                    rawError;

                _log("  FAILED - " + result.ErrorMessage);
                return result;
            }
            catch (Exception ex)
            {
                Exception rawActual =
                    ex is TargetInvocationException &&
                    ex.InnerException != null
                        ? ex.InnerException
                        : ex;

                result.Success = false;
                result.ErrorMessage =
                    "Generated export: " +
                    generatedError.GetType().FullName +
                    ": " +
                    generatedError.Message +
                    " | Raw export: " +
                    rawActual.GetType().FullName +
                    ": " +
                    rawActual.Message;

                _log("  FAILED - " + result.ErrorMessage);
                return result;
            }
        }

        private List<string> GetAllMetaNames()
        {
            MethodInfo method = _context
                .GetType()
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Name,
                            "GetAllMetaNames",
                            StringComparison.Ordinal) &&
                        IsSupportedGetAllMetaNamesSignature(candidate));

            if (method == null)
            {
                throw new MissingMethodException(
                    _context.GetType().FullName,
                    "GetAllMetaNames");
            }

            object[] arguments = method.GetParameters().Length == 0
                ? null
                : new object[] { false };

            object rawNames = method.Invoke(
                _context,
                arguments);

            List<string> names =
                ReflectionHelpers.ConvertToStringList(rawNames);

            if (names.Count == 0)
            {
                throw new InvalidOperationException(
                    "GetAllMetaNames returned no metadata names.");
            }

            return names;
        }

        private static bool IsSupportedGetAllMetaNamesSignature(
            MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            return
                parameters.Length == 0 ||
                (parameters.Length == 1 &&
                 parameters[0].ParameterType == typeof(bool));
        }

        private object GetMetaItemSet(string tableName)
        {
            MethodInfo method = _context
                .GetType()
                .GetMethod(
                    "GetMetaItemSet",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(bool)
                    },
                    null);

            if (method == null)
            {
                throw new MissingMethodException(
                    _context.GetType().FullName,
                    "GetMetaItemSet(string, bool)");
            }

            return method.Invoke(
                _context,
                new object[]
                {
                    tableName,
                    false
                });
        }

        private Type FindGeneratedMetaType(
            string tableName,
            object itemSet)
        {
            if (itemSet == null)
            {
                throw new ArgumentNullException("itemSet");
            }

            Type itemSetType = itemSet.GetType();

            string expectedName =
                ReflectionHelpers.ToGeneratedMetaTypeName(tableName);

            Type nameMatch = _candidateTypes.FirstOrDefault(
                type =>
                    string.Equals(
                        type.Name,
                        expectedName,
                        StringComparison.Ordinal) &&
                    HasCompatibleGenerateMetaMethod(
                        type,
                        itemSetType));

            if (nameMatch != null)
            {
                return nameMatch;
            }

            string normalizedTable =
                ReflectionHelpers.NormalizeIdentifier(tableName);

            List<Type> normalizedMatches = _candidateTypes
                .Where(
                    type =>
                        string.Equals(
                            ReflectionHelpers.NormalizeIdentifier(
                                type.Name),
                            normalizedTable,
                            StringComparison.Ordinal) &&
                        HasCompatibleGenerateMetaMethod(
                            type,
                            itemSetType))
                .ToList();

            if (normalizedMatches.Count == 1)
            {
                return normalizedMatches[0];
            }

            if (normalizedMatches.Count > 1)
            {
                return normalizedMatches
                    .OrderBy(type => type.FullName)
                    .First();
            }

            List<Type> parameterMatches = _candidateTypes
                .Where(
                    type =>
                        HasCompatibleGenerateMetaMethod(
                            type,
                            itemSetType))
                .ToList();

            if (parameterMatches.Count == 1)
            {
                return parameterMatches[0];
            }

            if (parameterMatches.Count > 1)
            {
                throw new InvalidOperationException(
                    "Multiple generated Meta types accept item set " +
                    itemSetType.FullName +
                    ": " +
                    string.Join(
                        ", ",
                        parameterMatches
                            .Take(20)
                            .Select(type => type.FullName)));
            }

            throw new TypeLoadException(
                "No generated Meta type accepts item set: " +
                itemSetType.FullName);
        }

        private static bool HasCompatibleGenerateMetaMethod(
            Type type,
            Type itemSetType)
        {
            return type
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                .Any(
                    method =>
                    {
                        if (!string.Equals(
                            method.Name,
                            "GenerateMeta",
                            StringComparison.Ordinal))
                        {
                            return false;
                        }

                        ParameterInfo[] parameters =
                            method.GetParameters();

                        return
                            parameters.Length == 2 &&
                            parameters[1].ParameterType ==
                            typeof(bool) &&
                            parameters[0].ParameterType
                                .IsAssignableFrom(itemSetType);
                    });
        }

        private static void InvokeGenerateMeta(
            Type metaType,
            object itemSet)
        {
            Type itemSetType = itemSet.GetType();

            MethodInfo method = metaType
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                .Where(
                    candidate => string.Equals(
                        candidate.Name,
                        "GenerateMeta",
                        StringComparison.Ordinal))
                .FirstOrDefault(
                    candidate =>
                    {
                        ParameterInfo[] parameters =
                            candidate.GetParameters();

                        return
                            parameters.Length == 2 &&
                            parameters[1].ParameterType == typeof(bool) &&
                            parameters[0].ParameterType.IsAssignableFrom(
                                itemSetType);
                    });

            if (method == null)
            {
                throw new MissingMethodException(
                    metaType.FullName,
                    "GenerateMeta(itemSet, bool)");
            }

            method.Invoke(
                null,
                new object[]
                {
                    itemSet,
                    false
                });
        }

        private static IEnumerator GetGeneratedEnumerator(
            Type metaType)
        {
            MethodInfo method =
                ReflectionHelpers.FindMethodInHierarchy(
                    metaType,
                    "GetMetaEnumerator",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static);

            if (method == null)
            {
                throw new MissingMethodException(
                    metaType.FullName,
                    "GetMetaEnumerator");
            }

            object value = method.Invoke(null, null);

            IEnumerator enumerator = value as IEnumerator;

            if (enumerator == null)
            {
                IEnumerable enumerable = value as IEnumerable;

                if (enumerable != null)
                {
                    enumerator = enumerable.GetEnumerator();
                }
            }

            if (enumerator == null)
            {
                throw new InvalidOperationException(
                    "GetMetaEnumerator did not return IEnumerator or IEnumerable.");
            }

            return enumerator;
        }

        private void WriteFailureLog(ExportSummary summary)
        {
            string logPath = Path.Combine(
                _outputFolder,
                "_export_failures.log");

            using (var writer = new StreamWriter(
                logPath,
                false))
            {
                foreach (ExportResult failure in summary.Results
                    .Where(result => !result.Success))
                {
                    writer.WriteLine(
                        failure.TableName +
                        "\t" +
                        failure.ErrorMessage);
                }
            }
        }

        private static string MakeSafeFileName(string tableName)
        {
            char[] invalidCharacters =
                Path.GetInvalidFileNameChars();

            return new string(
                tableName
                    .Select(
                        character =>
                            invalidCharacters.Contains(character)
                                ? '_'
                                : character)
                    .ToArray());
        }
    }
}
