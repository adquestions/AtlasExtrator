using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AtlasExtractor
{
    internal sealed class MetaLoader
    {
        private readonly Assembly _fmCommonAssembly;
        private readonly string _metaPath;
        private readonly Action<string> _log;

        public MetaLoader(
            Assembly fmCommonAssembly,
            string metaPath,
            Action<string> log)
        {
            _fmCommonAssembly = fmCommonAssembly
                ?? throw new ArgumentNullException("fmCommonAssembly");

            _metaPath = metaPath
                ?? throw new ArgumentNullException("metaPath");

            _log = log ?? delegate { };
        }

        public object Load()
        {
            _log("Reading Meta.bytes...");

            byte[] metaBytes = File.ReadAllBytes(_metaPath);

            Type formatterType = _fmCommonAssembly.GetType(
                "FibMatrix.Config.ConfigFormatter",
                true,
                false);

            MethodInfo method = formatterType
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance)
                .FirstOrDefault(IsDeserializeMetaMethod);

            if (method == null)
            {
                throw new MissingMethodException(
                    formatterType.FullName,
                    "DeserializeMeta(byte[], byte[], bool, bool)");
            }

            object instance = method.IsStatic
                ? null
                : Activator.CreateInstance(formatterType, true);

            object context = method.Invoke(
                instance,
                new object[]
                {
                    metaBytes,
                    null,
                    false,
                    false
                });

            if (context == null)
            {
                throw new InvalidOperationException(
                    "DeserializeMeta returned null.");
            }

            _log("Meta context: " + context.GetType().FullName);
            return context;
        }

        private static bool IsDeserializeMetaMethod(
            MethodInfo candidate)
        {
            if (!string.Equals(
                candidate.Name,
                "DeserializeMeta",
                StringComparison.Ordinal))
            {
                return false;
            }

            ParameterInfo[] parameters = candidate.GetParameters();

            return
                parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(byte[]) &&
                parameters[1].ParameterType == typeof(byte[]) &&
                parameters[2].ParameterType == typeof(bool) &&
                parameters[3].ParameterType == typeof(bool);
        }
    }
}
