using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AtlasExtractor
{
    internal static class CitybuildingRowInspector
    {
        public static void Run(
            object context,
            string outputFolder)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            Directory.CreateDirectory(outputFolder);

            object itemSet = GetMetaItemSet(
                context,
                "citybuilding");

            Type itemSetType = itemSet.GetType();

            MethodInfo allToDictionaryMethod = null;
            MethodInfo fillRowValsMethod = null;
            FieldInfo helperField = itemSetType.GetField(
                "_helper",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            object helper = helperField == null
                ? null
                : helperField.GetValue(itemSet);

            PropertyInfo currentRowProperty =
                helper == null
                    ? null
                    : helper.GetType().GetProperty(
                        "currentRow",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance);

            PropertyInfo rowValCountProperty =
                helper == null
                    ? null
                    : helper.GetType().GetProperty(
                        "rowValCount",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance);

            string reportPath = Path.Combine(
                outputFolder,
                "_citybuilding_row_diagnostic.csv");

            string summaryPath = Path.Combine(
                outputFolder,
                "_citybuilding_row_summary.txt");

            int rowsSeen = 0;
            int populatedDirect = 0;
            int populatedAfterFill = 0;
            int emptyDirect = 0;
            int exceptions = 0;
            int duplicateIds = 0;

            var ids = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            using (var writer = new StreamWriter(
                reportPath,
                false,
                new System.Text.UTF8Encoding(true)))
            {
                writer.WriteLine(
                    "enumerator_index,helper_current_row,row_val_count,direct_count,after_fill_count,id,status,error");

                IEnumerator enumerator = itemSet as IEnumerator;

                if (enumerator == null)
                {
                    throw new InvalidOperationException(
                        "citybuilding item set does not implement IEnumerator.");
                }

                int index = -1;

                while (true)
                {
                    bool moved;

                    try
                    {
                        moved = enumerator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        exceptions++;

                        writer.WriteLine(
                            Csv(index + 1) + "," +
                            ",,,,," +
                            Csv("MOVE_NEXT_EXCEPTION") + "," +
                            Csv(Unwrap(ex).ToString()));

                        break;
                    }

                    if (!moved)
                    {
                        break;
                    }

                    index++;
                    rowsSeen++;

                    object record = enumerator.Current;

                    int helperCurrentRow =
                        ReadIntProperty(
                            helper,
                            currentRowProperty);

                    int rowValCount =
                        ReadIntProperty(
                            helper,
                            rowValCountProperty);

                    int directCount = -1;
                    int afterFillCount = -1;
                    string id = string.Empty;
                    string status = string.Empty;
                    string error = string.Empty;

                    try
                    {
                        if (record == null)
                        {
                            status = "NULL_RECORD";
                        }
                        else
                        {
                            Type recordType = record.GetType();

                            if (allToDictionaryMethod == null)
                            {
                                allToDictionaryMethod =
                                    recordType.GetMethod(
                                        "AllToDictionary",
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Instance,
                                        null,
                                        Type.EmptyTypes,
                                        null);
                            }

                            if (fillRowValsMethod == null)
                            {
                                fillRowValsMethod =
                                    recordType.GetMethod(
                                        "FillRowVals",
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Instance,
                                        null,
                                        Type.EmptyTypes,
                                        null);
                            }

                            IDictionary<string, string> direct =
                                InvokeDictionary(
                                    record,
                                    allToDictionaryMethod);

                            directCount =
                                direct == null
                                    ? -1
                                    : direct.Count;

                            id = GetId(direct);

                            if (directCount > 0)
                            {
                                populatedDirect++;
                                status = "DIRECT_OK";
                            }
                            else
                            {
                                emptyDirect++;

                                if (fillRowValsMethod != null)
                                {
                                    fillRowValsMethod.Invoke(
                                        record,
                                        null);

                                    IDictionary<string, string> afterFill =
                                        InvokeDictionary(
                                            record,
                                            allToDictionaryMethod);

                                    afterFillCount =
                                        afterFill == null
                                            ? -1
                                            : afterFill.Count;

                                    if (string.IsNullOrEmpty(id))
                                    {
                                        id = GetId(afterFill);
                                    }

                                    if (afterFillCount > 0)
                                    {
                                        populatedAfterFill++;
                                        status = "FILL_OK";
                                    }
                                    else
                                    {
                                        status = "EMPTY_AFTER_FILL";
                                    }
                                }
                                else
                                {
                                    status = "EMPTY_NO_FILL_METHOD";
                                }
                            }

                            if (!string.IsNullOrEmpty(id) &&
                                !ids.Add(id))
                            {
                                duplicateIds++;
                                status += "|DUPLICATE_ID";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions++;
                        status = "ROW_EXCEPTION";

                        Exception actual = Unwrap(ex);

                        error =
                            actual.GetType().FullName +
                            ": " +
                            actual.Message;
                    }

                    writer.WriteLine(
                        Csv(index) + "," +
                        Csv(helperCurrentRow) + "," +
                        Csv(rowValCount) + "," +
                        Csv(directCount) + "," +
                        Csv(afterFillCount) + "," +
                        Csv(id) + "," +
                        Csv(status) + "," +
                        Csv(error));
                }
            }

            using (var writer = new StreamWriter(
                summaryPath,
                false,
                new System.Text.UTF8Encoding(true)))
            {
                writer.WriteLine(
                    "CITYBUILDING ROW DIAGNOSTIC");
                writer.WriteLine(
                    "========================================");
                writer.WriteLine(
                    "Rows seen:              " +
                    rowsSeen.ToString(
                        "N0",
                        CultureInfo.InvariantCulture));
                writer.WriteLine(
                    "Direct populated:       " +
                    populatedDirect.ToString(
                        "N0",
                        CultureInfo.InvariantCulture));
                writer.WriteLine(
                    "Populated after Fill:   " +
                    populatedAfterFill.ToString(
                        "N0",
                        CultureInfo.InvariantCulture));
                writer.WriteLine(
                    "Direct empty:           " +
                    emptyDirect.ToString(
                        "N0",
                        CultureInfo.InvariantCulture));
                writer.WriteLine(
                    "Exceptions:             " +
                    exceptions.ToString(
                        "N0",
                        CultureInfo.InvariantCulture));
                writer.WriteLine(
                    "Unique IDs:             " +
                    ids.Count.ToString(
                        "N0",
                        CultureInfo.InvariantCulture));
                writer.WriteLine(
                    "Duplicate IDs:          " +
                    duplicateIds.ToString(
                        "N0",
                        CultureInfo.InvariantCulture));
                writer.WriteLine();
                writer.WriteLine("CSV report:");
                writer.WriteLine(reportPath);
            }

            Console.WriteLine();
            Console.WriteLine(
                "CITYBUILDING ROW DIAGNOSTIC COMPLETE");
            Console.WriteLine(
                "========================================");
            Console.WriteLine(
                "Rows seen:            " +
                rowsSeen.ToString("N0"));
            Console.WriteLine(
                "Direct populated:     " +
                populatedDirect.ToString("N0"));
            Console.WriteLine(
                "After Fill populated: " +
                populatedAfterFill.ToString("N0"));
            Console.WriteLine(
                "Exceptions:           " +
                exceptions.ToString("N0"));
            Console.WriteLine(
                "Unique IDs:           " +
                ids.Count.ToString("N0"));
            Console.WriteLine();
            Console.WriteLine(reportPath);
            Console.WriteLine(summaryPath);
        }

        private static object GetMetaItemSet(
            object context,
            string tableName)
        {
            MethodInfo method = context
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
                    context.GetType().FullName,
                    "GetMetaItemSet(string, bool)");
            }

            object itemSet = method.Invoke(
                context,
                new object[]
                {
                    tableName,
                    false
                });

            if (itemSet == null)
            {
                throw new InvalidOperationException(
                    "GetMetaItemSet returned null for " +
                    tableName);
            }

            return itemSet;
        }

        private static IDictionary<string, string> InvokeDictionary(
            object record,
            MethodInfo method)
        {
            if (method == null)
            {
                throw new MissingMethodException(
                    record.GetType().FullName,
                    "AllToDictionary()");
            }

            object result = method.Invoke(
                record,
                null);

            return result as IDictionary<string, string>;
        }

        private static string GetId(
            IDictionary<string, string> dictionary)
        {
            if (dictionary == null)
            {
                return string.Empty;
            }

            string id;

            return dictionary.TryGetValue(
                "id",
                out id)
                ? id
                : string.Empty;
        }

        private static int ReadIntProperty(
            object source,
            PropertyInfo property)
        {
            if (source == null ||
                property == null)
            {
                return -1;
            }

            try
            {
                return Convert.ToInt32(
                    property.GetValue(
                        source,
                        null),
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return -1;
            }
        }

        private static string Csv(object value)
        {
            string text =
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture)
                ?? string.Empty;

            bool quote =
                text.IndexOf(',') >= 0 ||
                text.IndexOf('"') >= 0 ||
                text.IndexOf('\r') >= 0 ||
                text.IndexOf('\n') >= 0;

            if (!quote)
            {
                return text;
            }

            return "\"" +
                text.Replace("\"", "\"\"") +
                "\"";
        }

        private static Exception Unwrap(
            Exception exception)
        {
            TargetInvocationException invocation =
                exception as TargetInvocationException;

            return invocation != null &&
                   invocation.InnerException != null
                ? invocation.InnerException
                : exception;
        }
    }
}
