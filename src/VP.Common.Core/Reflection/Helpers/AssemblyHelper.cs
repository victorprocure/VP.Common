using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
using System.Runtime.InteropServices;

namespace VP.Common.Core.Reflection
{
    public static class AssemblyHelper
    {
        private static readonly object _lockObject = new object();

        private static readonly HashSet<Assembly> _registeredAssemblies = new HashSet<Assembly>();
        private static readonly Dictionary<string, string> _assemblyMappings = new Dictionary<string, string>();


        /// <summary>
        /// Gets the loaded assemblies by using the right method. For Windows applications, it uses
        /// <c>AppDomain.GetAssemblies()</c>.
        /// </summary>
        /// <returns><see cref="List{Assembly}" /> of all loaded assemblies.</returns>
        public static List<Assembly> GetLoadedAssemblies()
        {
            return GetLoadedAssemblies(AppDomain.CurrentDomain);
        }

        public static Type[] GetAllTypesSafely(this Assembly assembly, bool logLoaderExceptions = true)
        {
            assembly.ThrowIfNull(nameof(assembly));

            Type[] foundAssemblyTypes;

            RegisterAssemblyWithVersionInfo(assembly);

            try
            {
                foundAssemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException typeLoadException)
            {
                foundAssemblyTypes = (from type in typeLoadException.Types
                                      where type != null
                                      select type).ToArray();

                if (logLoaderExceptions)
                {
                    foreach (var error in typeLoadException.LoaderExceptions)
                    {
                        // Fix mono issue https://github.com/Catel/Catel/issues/1071 
                        if (error != null)
                        {
                            
                        }
                    }
                }
            }
            catch (Exception)
            {
                foundAssemblyTypes =EnumerableExtensions.GetEmptyArray<Type>();
            }

            return foundAssemblyTypes;
        }

        /// <summary>
        /// Gets the loaded assemblies by using the right method. For Windows applications, it uses
        /// <c>AppDomain.GetAssemblies()</c>.
        /// </summary>
        /// <param name="appDomain">The app domain to search in.</param>
        /// <returns><see cref="List{Assembly}" /> of all loaded assemblies.</returns>
        public static List<Assembly> GetLoadedAssemblies(this AppDomain appDomain)
        {
            return GetLoadedAssemblies(appDomain, false);
        }

        /// <summary>
        /// Gets the loaded assemblies by using the right method. For Windows applications, it uses
        /// <c>AppDomain.GetAssemblies()</c>.
        /// </summary>
        /// <param name="appDomain">The app domain to search in.</param>
        /// <param name="ignoreDynamicAssemblies">if set to <c>true</c>, dynamic assemblies are being ignored.</param>
        /// <returns><see cref="List{Assembly}" /> of all loaded assemblies.</returns>
        public static List<Assembly> GetLoadedAssemblies(this AppDomain appDomain, bool ignoreDynamicAssemblies)
        {
            var assemblies = new List<Assembly>();

            assemblies.AddRange(appDomain.GetAssemblies());

            var finalAssemblies = new List<Assembly>();

            // Make sure to prevent duplicates
            foreach (var assembly in assemblies.Distinct())
            {
                if (ShouldIgnoreAssembly(assembly, ignoreDynamicAssemblies))
                {
                    continue;
                }

                finalAssemblies.Add(assembly);

                RegisterAssemblyWithVersionInfo(assembly);
            }

            return finalAssemblies;
        }

        /// <summary>
        /// Determines whether the specified assembly is a dynamic assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns><c>true</c> if the specified assembly is a dynamic assembly; otherwise, <c>false</c>.</returns>
        public static bool IsDynamicAssembly(this Assembly assembly)
        {
            // Simplest & fastest way out
            if (assembly.IsDynamic)
            {
                return true;
            }

            var isDynamicAssembly =
                (assembly is System.Reflection.Emit.AssemblyBuilder) &&
                string.Equals(assembly.GetType().FullName, "System.Reflection.Emit.InternalAssemblyBuilder", StringComparison.Ordinal)
                && !assembly.GlobalAssemblyCache
                && ((Assembly.GetExecutingAssembly() != null)
                && !string.Equals(assembly.CodeBase, Assembly.GetExecutingAssembly().CodeBase, StringComparison.OrdinalIgnoreCase))
                // Note: to make it the same for all platforms
                && true;

            return isDynamicAssembly;
        }

        private static void RegisterAssemblyWithVersionInfo(Assembly assembly)
        {
            lock (_lockObject)
            {
                try
                {
                    if (_registeredAssemblies.Contains(assembly))
                    {
                        return;
                    }

                    _registeredAssemblies.Add(assembly);

                    var assemblyNameWithVersion = assembly.FullName;
                    var assemblyNameWithoutVersion = TypeHelper.GetAssemblyNameWithoutOverhead(assemblyNameWithVersion);
                    _assemblyMappings[assemblyNameWithoutVersion] = assemblyNameWithVersion;
                }
                catch (Exception)
                {
                }
            }
        }

        private static bool ShouldIgnoreAssembly(Assembly assembly, bool ignoreDynamicAssemblies)
        {
            if (assembly == null)
            {
                return true;
            }

            if (ignoreDynamicAssemblies)
            {
                if (assembly.IsDynamicAssembly())
                {
                    return true;
                }
            }

            return false;
        }
    }
}