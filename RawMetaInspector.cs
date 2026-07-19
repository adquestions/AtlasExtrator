using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AtlasExtractor
{
    internal static class RawMetaInspector
    {
        public static void Run(
            object context,
            string outputFolder,
            params string[] tableNames)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            Directory.CreateDirectory(outputFolder);

            foreach (string tableName in tableNames)
            {
                Console.WriteLine();
                Console.WriteLine("Inspecting raw table: " + tableName);
                Console.WriteLine("----------------------------------------");

                object itemSet = GetMetaItemSet(
                    context,
                    tableName);

                string reportPath = Path.Combine(
                    outputFolder,
                    "_inspect_" + tableName + ".txt");

                using (var writer = new StreamWriter(
                    reportPath,
                    false))
                {
                    writer.WriteLine(
                        "TABLE: " + tableName);

                    writer.WriteLine(
                        "ITEM SET TYPE: " +
                        itemSet.GetType().FullName);

                    writer.WriteLine();

                    InspectObject(
                        writer,
                        itemSet,
                        "itemSet",
                        0,
                        new HashSet<object>(
                            ReferenceEqualityComparer.Instance));
                }

                Console.WriteLine("Saved:");
                Console.WriteLine(reportPath);
            }
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
                    "GetMetaItemSet returned null for: " +
                    tableName);
            }

            return itemSet;
        }

        private static void InspectObject(
            TextWriter writer,
            object value,
            string path,
            int depth,
            ISet<object> visited)
        {
            string indent =
                new string(' ', depth * 2);

            if (value == null)
            {
                writer.WriteLine(
                    indent + path + " = <null>");

                return;
            }

            Type type = value.GetType();

            writer.WriteLine(
                indent +
                path +
                " [" +
                type.FullName +
                "]");

            if (IsSimple(type))
            {
                writer.WriteLine(
                    indent +
                    "  VALUE: " +
                    FormatSimple(value));

                return;
            }

            if (!type.IsValueType)
            {
                if (visited.Contains(value))
                {
                    writer.WriteLine(
                        indent +
                        "  <already visited>");

                    return;
                }

                visited.Add(value);
            }

            writer.WriteLine(
                indent +
                "  Interfaces: " +
                string.Join(
                    ", ",
                    type.GetInterfaces()
                        .Select(item => item.FullName)));

            WriteMethods(
                writer,
                type,
                indent);

            WriteFields(
                writer,
                value,
                type,
                path,
                depth,
                visited);

            WriteProperties(
                writer,
                value,
                type,
                path,
                depth,
                visited);

            WriteEnumerablePreview(
                writer,
                value,
                path,
                depth,
                visited);
        }

        private static void WriteMethods(
            TextWriter writer,
            Type type,
            string indent)
        {
            writer.WriteLine(
                indent + "  Methods:");

            foreach (MethodInfo method in type
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)
                .OrderBy(method => method.Name))
            {
                writer.WriteLine(
                    indent +
                    "    " +
                    FormatMethod(method));
            }
        }

        private static void WriteFields(
            TextWriter writer,
            object source,
            Type type,
            string path,
            int depth,
            ISet<object> visited)
        {
            writer.WriteLine(
                new string(' ', depth * 2) +
                "  Fields:");

            foreach (FieldInfo field in type
                .GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .OrderBy(field => field.Name))
            {
                object fieldValue;

                try
                {
                    fieldValue = field.GetValue(source);
                }
                catch (Exception ex)
                {
                    writer.WriteLine(
                        new string(' ', depth * 2) +
                        "    " +
                        field.Name +
                        " [" +
                        field.FieldType.FullName +
                        "] <ERROR: " +
                        ex.GetType().Name +
                        ">");

                    continue;
                }

                writer.WriteLine(
                    new string(' ', depth * 2) +
                    "    " +
                    field.Name +
                    " [" +
                    field.FieldType.FullName +
                    "] = " +
                    PreviewValue(fieldValue));

                if (depth < 2 &&
                    ShouldDescend(fieldValue))
                {
                    InspectObject(
                        writer,
                        fieldValue,
                        path + "." + field.Name,
                        depth + 1,
                        visited);
                }
            }
        }

        private static void WriteProperties(
            TextWriter writer,
            object source,
            Type type,
            string path,
            int depth,
            ISet<object> visited)
        {
            writer.WriteLine(
                new string(' ', depth * 2) +
                "  Properties:");

            foreach (PropertyInfo property in type
                .GetProperties(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .OrderBy(property => property.Name))
            {
                if (!property.CanRead ||
                    property.GetIndexParameters().Length != 0)
                {
                    writer.WriteLine(
                        new string(' ', depth * 2) +
                        "    " +
                        property.Name +
                        " [" +
                        property.PropertyType.FullName +
                        "] <not safely readable>");

                    continue;
                }

                object propertyValue;

                try
                {
                    propertyValue =
                        property.GetValue(
                            source,
                            null);
                }
                catch (Exception ex)
                {
                    writer.WriteLine(
                        new string(' ', depth * 2) +
                        "    " +
                        property.Name +
                        " [" +
                        property.PropertyType.FullName +
                        "] <ERROR: " +
                        Unwrap(ex).GetType().Name +
                        " - " +
                        Unwrap(ex).Message +
                        ">");

                    continue;
                }

                writer.WriteLine(
                    new string(' ', depth * 2) +
                    "    " +
                    property.Name +
                    " [" +
                    property.PropertyType.FullName +
                    "] = " +
                    PreviewValue(propertyValue));

                if (depth < 2 &&
                    ShouldDescend(propertyValue))
                {
                    InspectObject(
                        writer,
                        propertyValue,
                        path + "." + property.Name,
                        depth + 1,
                        visited);
                }
            }
        }

        private static void WriteEnumerablePreview(
            TextWriter writer,
            object source,
            string path,
            int depth,
            ISet<object> visited)
        {
            IEnumerator enumerator =
                TryGetEnumerator(source);

            if (enumerator == null)
            {
                return;
            }

            writer.WriteLine(
                new string(' ', depth * 2) +
                "  Enumerator preview:");

            int index = 0;

            try
            {
                while (index < 3 &&
                       enumerator.MoveNext())
                {
                    object current =
                        enumerator.Current;

                    writer.WriteLine(
                        new string(' ', depth * 2) +
                        "    [" +
                        index +
                        "] " +
                        PreviewValue(current));

                    if (depth < 2 &&
                        ShouldDescend(current))
                    {
                        InspectObject(
                            writer,
                            current,
                            path +
                            "[" +
                            index +
                            "]",
                            depth + 1,
                            visited);
                    }

                    index++;
                }

                if (index == 0)
                {
                    writer.WriteLine(
                        new string(' ', depth * 2) +
                        "    <zero items>");
                }
            }
            catch (Exception ex)
            {
                Exception actual =
                    Unwrap(ex);

                writer.WriteLine(
                    new string(' ', depth * 2) +
                    "    <ERROR: " +
                    actual.GetType().Name +
                    " - " +
                    actual.Message +
                    ">");
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

        private static IEnumerator TryGetEnumerator(
            object value)
        {
            IEnumerator direct =
                value as IEnumerator;

            if (direct != null)
            {
                return direct;
            }

            IEnumerable enumerable =
                value as IEnumerable;

            if (enumerable != null &&
                !(value is string) &&
                !(value is byte[]))
            {
                try
                {
                    return enumerable.GetEnumerator();
                }
                catch
                {
                    return null;
                }
            }

            MethodInfo method = value
                .GetType()
                .GetMethod(
                    "GetEnumerator",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);

            if (method == null)
            {
                return null;
            }

            try
            {
                return method.Invoke(
                    value,
                    null) as IEnumerator;
            }
            catch
            {
                return null;
            }
        }

        private static bool ShouldDescend(
            object value)
        {
            if (value == null)
            {
                return false;
            }

            Type type = value.GetType();

            return !IsSimple(type) &&
                   !(value is string) &&
                   !(value is byte[]);
        }

        private static bool IsSimple(Type type)
        {
            return
                type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(Guid);
        }

        private static string PreviewValue(
            object value)
        {
            if (value == null)
            {
                return "<null>";
            }

            Type type = value.GetType();

            if (IsSimple(type))
            {
                return FormatSimple(value);
            }

            ICollection collection =
                value as ICollection;

            if (collection != null)
            {
                return
                    "<" +
                    type.FullName +
                    "; Count=" +
                    collection.Count +
                    ">";
            }

            return "<" + type.FullName + ">";
        }

        private static string FormatSimple(
            object value)
        {
            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture);
        }

        private static string FormatMethod(
            MethodInfo method)
        {
            return
                method.ReturnType.FullName +
                " " +
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

        private sealed class ReferenceEqualityComparer :
            IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance =
                new ReferenceEqualityComparer();

            public new bool Equals(
                object left,
                object right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(object value)
            {
                return System.Runtime.CompilerServices
                    .RuntimeHelpers
                    .GetHashCode(value);
            }
        }
    }
}
