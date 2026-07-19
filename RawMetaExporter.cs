using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AtlasExtractor
{
    internal sealed class RawMetaExporter
    {
        private readonly CsvWriter _csvWriter;

        public RawMetaExporter(CsvWriter csvWriter)
        {
            _csvWriter = csvWriter
                ?? throw new ArgumentNullException("csvWriter");
        }

        public bool TryExport(
            object itemSet,
            string csvPath,
            out long rowCount,
            out string strategy,
            out string error)
        {
            rowCount = 0;
            strategy = null;
            error = null;

            if (itemSet == null)
            {
                error = "The item set was null.";
                return false;
            }

            List<EnumeratorCandidate> candidates =
                FindCandidates(itemSet);

            if (candidates.Count == 0)
            {
                error =
                    "No safe enumerable container was found on " +
                    itemSet.GetType().FullName +
                    ".";
                return false;
            }

            var failures = new List<string>();

            foreach (EnumeratorCandidate candidate in candidates)
            {
                IEnumerator enumerator = null;

                try
                {
                    enumerator = candidate.CreateEnumerator();

                    if (enumerator == null)
                    {
                        failures.Add(
                            candidate.Description +
                            ": returned null");
                        continue;
                    }

                    string temporaryPath =
                        csvPath + ".raw.tmp";

                    long candidateRows = _csvWriter.Write(
                        itemSet.GetType(),
                        enumerator,
                        temporaryPath);

                    if (candidateRows == 0)
                    {
                        TryDelete(temporaryPath);

                        failures.Add(
                            candidate.Description +
                            ": zero rows");
                        continue;
                    }

                    TryDelete(csvPath);
                    File.Move(temporaryPath, csvPath);

                    rowCount = candidateRows;
                    strategy = candidate.Description;
                    return true;
                }
                catch (Exception ex)
                {
                    TryDelete(csvPath + ".raw.tmp");

                    Exception actual =
                        ex is TargetInvocationException &&
                        ex.InnerException != null
                            ? ex.InnerException
                            : ex;

                    failures.Add(
                        candidate.Description +
                        ": " +
                        actual.GetType().Name +
                        " - " +
                        actual.Message);
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
            }

            error = string.Join(
                " | ",
                failures.Take(10));

            return false;
        }

        private static List<EnumeratorCandidate> FindCandidates(
            object itemSet)
        {
            var candidates =
                new List<EnumeratorCandidate>();

            Type itemSetType = itemSet.GetType();

            AddEnumerableCandidate(
                candidates,
                itemSet,
                "itemSet itself",
                1000);

            MethodInfo getEnumerator = itemSetType.GetMethod(
                "GetEnumerator",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (getEnumerator != null &&
                typeof(IEnumerator).IsAssignableFrom(
                    getEnumerator.ReturnType))
            {
                candidates.Add(
                    new EnumeratorCandidate
                    {
                        Description =
                            itemSetType.FullName +
                            ".GetEnumerator()",
                        Score = 950,
                        CreateEnumerator = () =>
                            (IEnumerator)getEnumerator.Invoke(
                                itemSet,
                                null)
                    });
            }

            foreach (PropertyInfo property in itemSetType
                .GetProperties(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance))
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                int score = ScoreMember(property.Name);

                if (score <= 0)
                {
                    continue;
                }

                PropertyInfo captured = property;

                candidates.Add(
                    new EnumeratorCandidate
                    {
                        Description =
                            itemSetType.FullName +
                            "." +
                            captured.Name +
                            " property",
                        Score = score,
                        CreateEnumerator = () =>
                        {
                            object value =
                                captured.GetValue(itemSet, null);

                            return CreateEnumerator(value);
                        }
                    });
            }

            foreach (FieldInfo field in itemSetType
                .GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance))
            {
                int score = ScoreMember(field.Name);

                if (score <= 0)
                {
                    continue;
                }

                FieldInfo captured = field;

                candidates.Add(
                    new EnumeratorCandidate
                    {
                        Description =
                            itemSetType.FullName +
                            "." +
                            captured.Name +
                            " field",
                        Score = score,
                        CreateEnumerator = () =>
                        {
                            object value =
                                captured.GetValue(itemSet);

                            return CreateEnumerator(value);
                        }
                    });
            }

            return candidates
                .OrderByDescending(candidate => candidate.Score)
                .GroupBy(
                    candidate => candidate.Description,
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
        }

        private static void AddEnumerableCandidate(
            ICollection<EnumeratorCandidate> candidates,
            object value,
            string description,
            int score)
        {
            if (!IsSafeEnumerable(value))
            {
                return;
            }

            candidates.Add(
                new EnumeratorCandidate
                {
                    Description = description,
                    Score = score,
                    CreateEnumerator = () =>
                        CreateEnumerator(value)
                });
        }

        private static IEnumerator CreateEnumerator(
            object value)
        {
            if (!IsSafeEnumerable(value))
            {
                return null;
            }

            IEnumerator direct = value as IEnumerator;

            if (direct != null)
            {
                return direct;
            }

            IEnumerable enumerable = value as IEnumerable;

            return enumerable == null
                ? null
                : enumerable.GetEnumerator();
        }

        private static bool IsSafeEnumerable(object value)
        {
            if (value == null ||
                value is string ||
                value is byte[])
            {
                return false;
            }

            return
                value is IEnumerator ||
                value is IEnumerable;
        }

        private static int ScoreMember(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            string normalized =
                name.TrimStart('_').ToLowerInvariant();

            if (normalized == "items" ||
                normalized == "itemdict" ||
                normalized == "itemmap" ||
                normalized == "metas" ||
                normalized == "data")
            {
                return 900;
            }

            if (normalized.Contains("item"))
            {
                return 850;
            }

            if (normalized.Contains("dict") ||
                normalized.Contains("map"))
            {
                return 800;
            }

            if (normalized.Contains("data") ||
                normalized.Contains("meta"))
            {
                return 750;
            }

            if (normalized.Contains("value") ||
                normalized.Contains("list") ||
                normalized.Contains("set"))
            {
                return 700;
            }

            return 0;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private sealed class EnumeratorCandidate
        {
            public string Description { get; set; }

            public int Score { get; set; }

            public Func<IEnumerator> CreateEnumerator { get; set; }
        }
    }
}
