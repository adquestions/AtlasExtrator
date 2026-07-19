using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AtlasExtractor
{
    internal sealed class CsvWriter
    {
        public long Write(
            Type metaType,
            IEnumerator enumerator,
            string csvPath)
        {
            if (metaType == null)
            {
                throw new ArgumentNullException("metaType");
            }

            if (enumerator == null)
            {
                throw new ArgumentNullException("enumerator");
            }

            string directory = Path.GetDirectoryName(csvPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!enumerator.MoveNext())
            {
                using (var writer = new StreamWriter(
                    csvPath,
                    false,
                    new UTF8Encoding(true)))
                {
                    writer.WriteLine();
                }

                return 0;
            }

            object firstRecord = enumerator.Current;

            if (firstRecord != null &&
                IsMetaKeyValueCollection(firstRecord.GetType()))
            {
                return WriteMetaKeyValueRecords(
                    firstRecord,
                    enumerator,
                    csvPath);
            }

            Type recordType = firstRecord == null
                ? metaType
                : firstRecord.GetType();

            List<CsvColumn> columns = BuildColumns(recordType);

            if (columns.Count == 0)
            {
                columns.Add(
                    new CsvColumn
                    {
                        Name = "Value",
                        GetValue = row => row
                    });
            }

            using (var writer = new StreamWriter(
                csvPath,
                false,
                new UTF8Encoding(true)))
            {
                writer.WriteLine(
                    string.Join(
                        ",",
                        columns.Select(
                            column => Escape(column.Name))));

                long rowCount = 0;

                if (firstRecord != null)
                {
                    WriteRecord(writer, columns, firstRecord);
                    rowCount++;
                }

                while (enumerator.MoveNext())
                {
                    object record = enumerator.Current;

                    if (record == null)
                    {
                        continue;
                    }

                    WriteRecord(writer, columns, record);
                    rowCount++;
                }

                return rowCount;
            }
        }

        private static bool IsMetaKeyValueCollection(
            Type type)
        {
            return type != null &&
                   string.Equals(
                       type.FullName,
                       "FibMatrix.Config.MetaKeyValueCollection",
                       StringComparison.Ordinal);
        }

        private static long WriteMetaKeyValueRecords(
            object firstRecord,
            IEnumerator enumerator,
            string csvPath)
        {
            MethodInfo allToDictionaryMethod =
                firstRecord.GetType().GetMethod(
                    "AllToDictionary",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);

            if (allToDictionaryMethod == null)
            {
                throw new MissingMethodException(
                    firstRecord.GetType().FullName,
                    "AllToDictionary()");
            }

            var rows =
                new List<IDictionary<string, string>>();

            var columns =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            AddMetaKeyValueRow(
                rows,
                columns,
                allToDictionaryMethod,
                firstRecord);

            while (enumerator.MoveNext())
            {
                object record = enumerator.Current;

                if (record == null)
                {
                    continue;
                }

                AddMetaKeyValueRow(
                    rows,
                    columns,
                    allToDictionaryMethod,
                    record);
            }

            List<string> orderedColumns = columns
                .OrderBy(
                    name =>
                        string.Equals(
                            name,
                            "id",
                            StringComparison.OrdinalIgnoreCase)
                            ? 0
                            : 1)
                .ThenBy(
                    name => name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            using (var writer = new StreamWriter(
                csvPath,
                false,
                new UTF8Encoding(true)))
            {
                writer.WriteLine(
                    string.Join(
                        ",",
                        orderedColumns.Select(Escape)));

                foreach (IDictionary<string, string> row in rows)
                {
                    writer.WriteLine(
                        string.Join(
                            ",",
                            orderedColumns.Select(
                                column =>
                                {
                                    string value;

                                    return Escape(
                                        row.TryGetValue(
                                            column,
                                            out value)
                                            ? value
                                            : string.Empty);
                                })));
                }
            }

            return rows.Count;
        }

        private static void AddMetaKeyValueRow(
            ICollection<IDictionary<string, string>> rows,
            ISet<string> columns,
            MethodInfo allToDictionaryMethod,
            object record)
        {
            object raw =
                allToDictionaryMethod.Invoke(
                    record,
                    null);

            IDictionary<string, string> dictionary =
                raw as IDictionary<string, string>;

            if (dictionary == null)
            {
                throw new InvalidOperationException(
                    "AllToDictionary() did not return IDictionary<string,string>.");
            }

            var snapshot =
                new Dictionary<string, string>(
                    dictionary,
                    StringComparer.OrdinalIgnoreCase);

            rows.Add(snapshot);

            foreach (string key in snapshot.Keys)
            {
                columns.Add(key);
            }
        }

        private static List<CsvColumn> BuildColumns(
            Type recordType)
        {
            PropertyInfo[] properties = recordType
                .GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance)
                .Where(
                    property =>
                        property.CanRead &&
                        property.GetIndexParameters().Length == 0)
                .OrderBy(
                    property =>
                        string.Equals(
                            property.Name,
                            "id",
                            StringComparison.OrdinalIgnoreCase)
                            ? 0
                            : 1)
                .ThenBy(property => property.Name)
                .ToArray();

            FieldInfo[] fields = recordType
                .GetFields(
                    BindingFlags.Public |
                    BindingFlags.Instance)
                .OrderBy(
                    field =>
                        string.Equals(
                            field.Name,
                            "id",
                            StringComparison.OrdinalIgnoreCase)
                            ? 0
                            : 1)
                .ThenBy(field => field.Name)
                .ToArray();

            var columns = new List<CsvColumn>();

            foreach (PropertyInfo property in properties)
            {
                PropertyInfo capturedProperty = property;

                columns.Add(
                    new CsvColumn
                    {
                        Name = capturedProperty.Name,
                        GetValue = row =>
                            capturedProperty.GetValue(row, null)
                    });
            }

            foreach (FieldInfo field in fields)
            {
                if (columns.Any(
                    column => string.Equals(
                        column.Name,
                        field.Name,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                FieldInfo capturedField = field;

                columns.Add(
                    new CsvColumn
                    {
                        Name = capturedField.Name,
                        GetValue = row =>
                            capturedField.GetValue(row)
                    });
            }

            return columns;
        }

        private static void WriteRecord(
            TextWriter writer,
            IEnumerable<CsvColumn> columns,
            object record)
        {
            string[] values = columns
                .Select(
                    column =>
                    {
                        object value;

                        try
                        {
                            value = column.GetValue(record);
                        }
                        catch (Exception ex)
                        {
                            value = "<ERROR:" + ex.GetType().Name + ">";
                        }

                        return Escape(FormatValue(value, 0));
                    })
                .ToArray();

            writer.WriteLine(string.Join(",", values));
        }

        private static string FormatValue(
            object value,
            int depth)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (depth >= 8)
            {
                return "<MAX_DEPTH>";
            }

            string text = value as string;

            if (text != null)
            {
                return text;
            }

            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }

            if (value is char)
            {
                return value.ToString();
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString(
                    "O",
                    CultureInfo.InvariantCulture);
            }

            Type valueType = value.GetType();

            if (valueType.IsEnum)
            {
                return value.ToString();
            }

            IDictionary dictionary = value as IDictionary;

            if (dictionary != null)
            {
                var pairs = new List<string>();

                foreach (DictionaryEntry entry in dictionary)
                {
                    pairs.Add(
                        FormatValue(entry.Key, depth + 1) +
                        ":" +
                        FormatValue(entry.Value, depth + 1));
                }

                return "{" + string.Join("|", pairs) + "}";
            }

            if (valueType.FullName != null &&
                valueType.FullName.StartsWith(
                    "System.ValueTuple",
                    StringComparison.Ordinal))
            {
                FieldInfo[] tupleFields = valueType
                    .GetFields(
                        BindingFlags.Public |
                        BindingFlags.Instance)
                    .OrderBy(field => field.Name)
                    .ToArray();

                return "(" +
                    string.Join(
                        "|",
                        tupleFields.Select(
                            field => FormatValue(
                                field.GetValue(value),
                                depth + 1))) +
                    ")";
            }

            IEnumerable enumerable = value as IEnumerable;

            if (enumerable != null)
            {
                var items = new List<string>();

                foreach (object item in enumerable)
                {
                    items.Add(FormatValue(item, depth + 1));
                }

                return "[" + string.Join("|", items) + "]";
            }

            IFormattable formattable = value as IFormattable;

            if (formattable != null)
            {
                return formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture);
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ?? string.Empty;
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

        private sealed class CsvColumn
        {
            public string Name { get; set; }

            public Func<object, object> GetValue { get; set; }
        }
    }
}
