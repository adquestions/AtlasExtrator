using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AtlasExtractor
{
    internal sealed class AssemblyLoader : IDisposable
    {
        private readonly string _inputFolder;
        private readonly Action<string> _log;
        private bool _disposed;

        public AssemblyLoader(string inputFolder, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(inputFolder))
            {
                throw new ArgumentException(
                    "An input folder is required.",
                    "inputFolder");
            }

            _inputFolder = Path.GetFullPath(inputFolder);
            _log = log ?? delegate { };

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        public Assembly LoadRequired(string fileName)
        {
            Assembly assembly = LoadIfPresent(fileName);

            if (assembly == null)
            {
                throw new FileNotFoundException(
                    fileName + " was not found in: " + _inputFolder);
            }

            return assembly;
        }

        public Assembly LoadIfPresent(string fileName)
        {
            string path = FindFile(fileName);

            if (path == null)
            {
                _log("Not found: " + fileName);
                return null;
            }

            string simpleName = Path.GetFileNameWithoutExtension(fileName);

            Assembly alreadyLoaded = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(
                    assembly => string.Equals(
                        assembly.GetName().Name,
                        simpleName,
                        StringComparison.OrdinalIgnoreCase));

            if (alreadyLoaded != null)
            {
                _log("Already loaded: " + fileName);
                return alreadyLoaded;
            }

            _log("Loading: " + fileName);
            return Assembly.LoadFrom(path);
        }

        public string FindFile(string fileName)
        {
            if (!Directory.Exists(_inputFolder))
            {
                return null;
            }

            return Directory
                .EnumerateFiles(
                    _inputFolder,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .FirstOrDefault(
                    path => string.Equals(
                        Path.GetFileName(path),
                        fileName,
                        StringComparison.OrdinalIgnoreCase));
        }

        private Assembly ResolveAssembly(
            object sender,
            ResolveEventArgs args)
        {
            try
            {
                string assemblyName = new AssemblyName(args.Name).Name;

                Assembly loaded = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(
                        assembly => string.Equals(
                            assembly.GetName().Name,
                            assemblyName,
                            StringComparison.OrdinalIgnoreCase));

                if (loaded != null)
                {
                    return loaded;
                }

                string dllPath = FindFile(assemblyName + ".dll");

                return dllPath == null
                    ? null
                    : Assembly.LoadFrom(dllPath);
            }
            catch (Exception ex)
            {
                _log(
                    "Assembly resolution failed for " +
                    args.Name +
                    ": " +
                    ex.Message);

                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            _disposed = true;
        }
    }
}
