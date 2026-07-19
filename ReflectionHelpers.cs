using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AtlasExtractor
{
    internal static class ReflectionHelpers
    {
        public static IEnumerable<Type> GetLoadableTypes(
            Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }

        public static MethodInfo FindMethodInHierarchy(
            Type type,
            string methodName,
            BindingFlags flags)
        {
            Type currentType = type;

            while (currentType != null)
            {
                MethodInfo method = currentType.GetMethod(
                    methodName,
                    flags | BindingFlags.DeclaredOnly);

                if (method != null)
                {
                    return method;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        public static string ToGeneratedMetaTypeName(
            string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return null;
            }

            var builder = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char character in tableName)
            {
                if (!char.IsLetterOrDigit(character))
                {
                    capitalizeNext = true;
                    continue;
                }

                builder.Append(
                    capitalizeNext
                        ? char.ToUpperInvariant(character)
                        : character);

                capitalizeNext = false;
            }

            builder.Append("Meta");
            return builder.ToString();
        }

        public static string NormalizeIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            string normalized = builder.ToString();

            if (normalized.EndsWith(
                "meta",
                StringComparison.Ordinal))
            {
                normalized = normalized.Substring(
                    0,
                    normalized.Length - 4);
            }

            return normalized;
        }

        public static List<string> ConvertToStringList(object value)
        {
            var results = new List<string>();

            if (value == null)
            {
                return results;
            }

            IDictionary dictionary = value as IDictionary;

            if (dictionary != null)
            {
                foreach (object key in dictionary.Keys)
                {
                    AddString(results, key);
                }

                return results;
            }

            IEnumerable enumerable = value as IEnumerable;

            if (enumerable != null && !(value is string))
            {
                foreach (object item in enumerable)
                {
                    AddString(results, item);
                }

                return results;
            }

            IEnumerator enumerator = value as IEnumerator;

            if (enumerator != null)
            {
                try
                {
                    while (enumerator.MoveNext())
                    {
                        AddString(results, enumerator.Current);
                    }
                }
                finally
                {
                    IDisposable disposable = enumerator as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                return results;
            }

            AddString(results, value);
            return results;
        }

        private static void AddString(
            ICollection<string> destination,
            object value)
        {
            if (value == null)
            {
                return;
            }

            string text = Convert.ToString(value);

            if (!string.IsNullOrWhiteSpace(text))
            {
                destination.Add(text.Trim());
            }
        }
    }
}
