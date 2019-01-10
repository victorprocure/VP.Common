using System;
using System.Collections.Generic;
using System.Reflection;

namespace VP.Common.Core.Reflection
{
    public class AssemblyLoadedEventArgs : EventArgs
    {
        public AssemblyLoadedEventArgs(Assembly assembly, IEnumerable<Type> loadedTypes)
        {
            assembly.ThrowIfNull(nameof(assembly));
            loadedTypes.ThrowIfNull(nameof(loadedTypes));

            Assembly = assembly;
            LoadedTypes = loadedTypes;
        }

        public IEnumerable<Type> LoadedTypes { get; private set; }

        public Assembly Assembly { get; private set; }
    }
}