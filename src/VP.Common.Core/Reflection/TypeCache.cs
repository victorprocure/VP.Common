using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VP.Common.Core.Threading;

namespace VP.Common.Core.Reflection
{
    public static class TypeCache
    {
        private static readonly Queue<Assembly> _assemblyQueue = new Queue<Assembly>();

        private static readonly Queue<Assembly> _assemblyLoadDelayQueue = new Queue<Assembly>();

        private static bool _isLoading = false;

        private static readonly Dictionary<Type, Type[]> _typesByInterface = new Dictionary<Type, Type[]>();

        private static readonly Dictionary<string, Dictionary<string, Type>> _typesByAssembly = new Dictionary<string, Dictionary<string, Type>>();

        private static readonly Dictionary<string, Type> _typesWithAssembly = new Dictionary<string, Type>();

        private static readonly Dictionary<string, Type> _typesWithAssemblyLowerCase = new Dictionary<string, Type>();

        private static readonly Dictionary<string, string> _typesWithoutAssembly = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> _typesWithoutAssemblyLowerCase = new Dictionary<string, string>();

        private static readonly HashSet<string> _loadedAssemblies = new HashSet<string>();

        private static readonly object _lockObject = new object();

        private static bool _hasInitializedOnce;

        static TypeCache()
        {
            ShouldIgnoreAssemblyEvaluators = new List<Func<Assembly, bool>>();
            ShouldIgnoreTypeEvaluators = new List<Func<Assembly, Type, bool>>();

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

            lock (_lockObject)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    InitializeTypes(assembly);
                }
            }
        }

        public static List<string> InitializedAssemblies
        {
            get
            {
                lock (_loadedAssemblies)
                {
                    return _loadedAssemblies.ToList();
                }
            }
        }

        private static void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            var assembly = args.LoadedAssembly;
            if (ShouldIgnoreAssembly(assembly))
            {
                return;
            }

            if (Monitor.TryEnter(_lockObject))
            {
                var assemblyName = assembly.FullName;
                if (!_loadedAssemblies.Contains(assemblyName))
                {
                    if (_isLoading)
                    {
                        _assemblyLoadDelayQueue.Enqueue(assembly);
                    }
                    else
                    {
                        try
                        {
                            _isLoading = true;
                            InitializeTypes(assembly);
                        }
                        finally
                        {
                            while (_assemblyLoadDelayQueue.Count > 0)
                            {
                                var delayedAssembly = _assemblyLoadDelayQueue.Dequeue();
                                InitializeTypes(delayedAssembly);
                            }

                            _isLoading = false;
                        }
                    }
                }

                var types = GetTypesOfAssembly(assembly);

                Monitor.Exit(_lockObject);

                var handler = AssemblyLoaded;
                if (!(handler is null))
                {
                    var eventArgs = new AssemblyLoadedEventArgs(assembly, types);
                    handler(null, eventArgs);
                }
            }
            else
            {
                lock (_assemblyQueue)
                {
                    _assemblyQueue.Enqueue(assembly);
                }
            }
        }

        public static List<Func<Assembly, bool>> ShouldIgnoreAssemblyEvaluators { get; private set; }
        public static List<Func<Assembly, Type, bool>> ShouldIgnoreTypeEvaluators { get; private set; }

        public static event EventHandler<AssemblyLoadedEventArgs> AssemblyLoaded;

        public static Type GetTypeWithAssembly(string typeName, string assemblyName, bool ignoreCase = false, bool allowInitialization = true)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            return GetTypeWithAssembly(typeName, assemblyName, ignoreCase, allowInitialization);
        }

        public static Type GetTypeWithoutAssembly(string typeName, bool ignoreCase = false, bool allowInitialization = true)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            return GetType(typeName, null, ignoreCase, allowInitialization);
        }

        public static Type GetType(string typeName, bool ignoreCase = false, bool allowInitialization = true)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            var realTypeName = TypeHelper.GetTypeName(typeName);
            var assemblyName = TypeHelper.GetAssemblyName(typeName);

            return GetType(typeName, assemblyName, ignoreCase, allowInitialization);
        }

        private static Type GetType(string typeName, string assemblyName, bool ignoreCase, bool allowInitialization = true)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (allowInitialization)
            {
                InitializeTypes();
            }

            lock (_lockObject)
            {
                var typesWithoutAssembly = ignoreCase ? _typesWithoutAssemblyLowerCase : _typesWithoutAssembly;
                var typesWithAssembly = ignoreCase ? _typesWithAssemblyLowerCase : _typesWithAssembly;

                var typeNameWithAssembly = string.IsNullOrEmpty(assemblyName) ? null : TypeHelper.FormatType(assemblyName, typeName);
                if (typeNameWithAssembly is null)
                {
                    if (typesWithoutAssembly.TryGetValue(typeName, out var typeNameMapping))
                    {
                        typeName = typeNameMapping;
                    }

                    if (typesWithAssembly.TryGetValue(typeName, out var cachedType))
                    {
                        return cachedType;
                    }

                    var fallbackType = GetTypeBySplittingInternals(typeName);
                    if (fallbackType is null)
                    {
                        return fallbackType;
                    }

                    typesWithAssembly[typeName] = fallbackType;
                }

                if (typesWithAssembly.TryGetValue(typeNameWithAssembly, out var typeWithAssembly))
                {
                    return typeWithAssembly;
                }

                var assemblyNameWithoutOverhead = TypeHelper.GetAssemblyNameWithoutOverhead(assemblyName);
                var typeNameWithoutAssemblyOverhead = TypeHelper.FormatType(assemblyNameWithoutOverhead, typeName);

                if (typesWithAssembly.TryGetValue(typeNameWithoutAssemblyOverhead, out var typeWithoutAssembly))
                {
                    return typeWithoutAssembly;
                }

                try
                {
                    var type = Type.GetType(typeNameWithAssembly, ignoreCase);
                    if (type != null)
                    {
                        typesWithAssembly[typeNameWithAssembly] = type;
                        return type;
                    }
                }
                catch (FileLoadException)
                {

                }

                InitializeTypes(assemblyName, false);

                if (typesWithAssembly.TryGetValue(typeNameWithAssembly, out var cachedTypeWithAssembly))
                {
                    return cachedTypeWithAssembly;
                }

                if (typesWithAssembly.TryGetValue(typeNameWithoutAssemblyOverhead, out var cachedTypeWithoutAssembly))
                {
                    return cachedTypeWithoutAssembly;
                }
            }

            return null;
        }

        private static Type GetTypeBySplittingInternals(string typeWithInnerTypes)
        {
            var fastType = Type.GetType(typeWithInnerTypes);
            if (fastType != null)
            {
                return fastType;
            }

            if (typeWithInnerTypes.EndsWith("[]"))
            {
                var arrayTypeElementString = typeWithInnerTypes.Replace("[]", string.Empty);
                var arrayTypeElement = GetType(arrayTypeElementString, allowInitialization: false);
                if (arrayTypeElement != null)
                {
                    return arrayTypeElement.MakeArrayType();
                }

                return null;
            }

            var innerTypes = new List<Type>();
            var innerTypesShortNames = TypeHelper.GetInnerTypes(typeWithInnerTypes);
            if (innerTypesShortNames.Length > 0)
            {
                foreach (var innerTypesShortName in innerTypesShortNames)
                {
                    var innerType = GetType(innerTypesShortName, allowInitialization: false);
                    if (innerType == null)
                    {
                        return null;
                    }

                    innerTypes.Add(innerType);
                }

                var innerTypesNames = new List<string>();
                foreach (var innerType in innerTypes)
                {
                    innerTypesNames.Add(innerType.AssemblyQualifiedName);
                }

                var firstBracketIndex = typeWithInnerTypes.IndexOf('[');
                var typeWithImprovedInnerTypes = string.Format("{0}[{1}]", typeWithInnerTypes.Substring(0, firstBracketIndex), TypeHelper.FormatInnerTypes(innerTypesNames.ToArray()));

                var fallbackType = Type.GetType(typeWithImprovedInnerTypes);
                return fallbackType;
            }

            return null;
        }

        public static Type[] GetTypes(Func<Type, bool> predicate = null, bool allowInitialization = true)
        {
            return GetTypesPrefilteredByAssembly(null, predicate, allowInitialization);
        }

        public static Type[] GetTypesOfAssembly(Assembly assembly, Func<Type, bool> predicate = null)
        {
            assembly.ThrowIfNull(nameof(assembly));

            return GetTypesPrefilteredByAssembly(assembly, predicate, true);
        }

        public static Type[] GetTypesImplementingInterface(Type interfaceType)
        {
            interfaceType.ThrowIfNull(nameof(interfaceType));

            lock (_lockObject)
            {
                if (!_typesByInterface.TryGetValue(interfaceType, out var typesByInterface))
                {
                    typesByInterface = GetTypes(x =>
                    {
                        if (x == interfaceType)
                        {
                            return false;
                        }

                        return x.ImplementsInterface(interfaceType);
                    }).ToArray();

                    _typesByInterface[interfaceType] = typesByInterface;
                }

                return typesByInterface;
            }
        }

        private static Type[] GetTypesPrefilteredByAssembly(Assembly assembly, Func<Type, bool> predicate, bool allowInitialization)
        {
            if (allowInitialization)
            {
                InitializeTypes(assembly);
            }

            var assemblyName = (assembly != null) ? TypeHelper.GetAssemblyNameWithoutOverhead(assembly.FullName) : string.Empty;

            Dictionary<string, Type> typeSource = null;

            lock (_lockObject)
            {
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    _typesByAssembly.TryGetValue(assemblyName, out typeSource);
                }
                else
                {
                    typeSource = _typesWithAssembly;
                }
            }

            if (typeSource == null)
            {
                return EnumerableExtensions.GetEmptyArray<Type>();
            }

            var retryCount = 3;
            while (retryCount > 0)
            {
                retryCount--;

                try
                {
                    if (predicate != null)
                    {
                        return typeSource.Values.Where(predicate).ToArray();
                    }

                    return typeSource.Values.ToArray();
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            return EnumerableExtensions.GetEmptyArray<Type>();
        }

        public static void InitializeTypes(string assemblyName, bool forceFullInitialization, bool allowMultithreadedInitialization = true)
        {
            // Important note: only allow explicit multithreaded initialization
            assemblyName.ThrowIfNullOrEmpty(nameof(assemblyName));

            lock (_lockObject)
            {
                if (_loadedAssemblies.Any(x =>
                {
                    var assemblyNameWithoutVersion = TypeHelper.GetAssemblyNameWithoutOverhead(x);
                    if (string.Equals(assemblyNameWithoutVersion, assemblyName))
                    {
                        return true;
                    }

                    return false;
                }))
                {
                    // No need to initialize (or get the loaded assemblies)
                    return;
                }

                var loadedAssemblies = AssemblyHelper.GetLoadedAssemblies();

                foreach (var assembly in loadedAssemblies)
                {
                    try
                    {
                        if (assembly.FullName.Contains(assemblyName))
                        {
                            InitializeTypes(assembly, forceFullInitialization, allowMultithreadedInitialization);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public static void InitializeTypes(Assembly assembly = null, bool forceFullInitialization = false, bool allowMultithreadedInitialization = false)
        {
            // Important note: only allow explicit multithreaded initialization

            var checkSingleAssemblyOnly = assembly != null;

            lock (_lockObject)
            {
                if (_hasInitializedOnce && !forceFullInitialization && !checkSingleAssemblyOnly)
                {
                    return;
                }

                if (!checkSingleAssemblyOnly)
                {
                    _hasInitializedOnce = true;
                }

                // CTL-877 Only clear when assembly != null
                if (forceFullInitialization && assembly == null)
                {
                    _loadedAssemblies.Clear();
                    _typesByAssembly?.Clear();
                    _typesWithAssembly?.Clear();
                    _typesWithAssemblyLowerCase?.Clear();
                    _typesWithoutAssembly?.Clear();
                    _typesWithoutAssemblyLowerCase?.Clear();
                }

                var assembliesToInitialize = checkSingleAssemblyOnly ? new List<Assembly>(new[] { assembly }) : AssemblyHelper.GetLoadedAssemblies();
                InitializeAssemblies(assembliesToInitialize, forceFullInitialization, allowMultithreadedInitialization);
            }
        }

        private static void InitializeAssemblies(IEnumerable<Assembly> assemblies, bool force, bool allowMultithreadedInitialization = false)
        {
            // Important note: only allow explicit multithreaded initialization

            var listForLoadedEvent = new List<Tuple<Assembly, Type[]>>();

            lock (_lockObject)
            {
                var assembliesToRetrieve = new List<Assembly>();

                foreach (var assembly in assemblies)
                {
                    var loadedAssemblyFullName = assembly.FullName;
                    var containsLoadedAssembly = _loadedAssemblies.Contains(loadedAssemblyFullName);
                    if (!force && containsLoadedAssembly)
                    {
                        continue;
                    }

                    if (!containsLoadedAssembly)
                    {
                        _loadedAssemblies.Add(loadedAssemblyFullName);
                    }

                    if (ShouldIgnoreAssembly(assembly))
                    {
                        continue;
                    }

                    assembliesToRetrieve.Add(assembly);
                }

                var typesToAdd = GetAssemblyTypes(assembliesToRetrieve, allowMultithreadedInitialization);
                foreach (var assemblyWithTypes in typesToAdd)
                {
                    foreach (var type in assemblyWithTypes.Value)
                    {
                        InitializeType(assemblyWithTypes.Key, type);
                    }
                }

                var lateLoadedAssemblies = new List<Assembly>();

                lock (_assemblyQueue)
                {
                    while (_assemblyQueue.Count > 0)
                    {
                        var assembly = _assemblyQueue.Dequeue();

                        lateLoadedAssemblies.Add(assembly);
                    }
                }

                foreach (var lateLoadedAssembly in lateLoadedAssemblies)
                {
                    var tuple = Tuple.Create(lateLoadedAssembly, GetTypesOfAssembly(lateLoadedAssembly));
                    listForLoadedEvent.Add(tuple);
                }
            }

            // Calling out of lock statement, but still may happens that would be called inside of it 
            var handler = AssemblyLoaded;
            if (handler != null)
            {
                foreach (var tuple in listForLoadedEvent)
                {
                    var assembly = tuple.Item1;
                    var types = tuple.Item2;
                    var eventArgs = new AssemblyLoadedEventArgs(assembly, types);

                    handler(null, eventArgs);
                }
            }
        }

        private static Dictionary<Assembly, HashSet<Type>> GetAssemblyTypes(List<Assembly> assemblies, bool allowMultithreadedInitialization = false)
        {
            var dictionary = new Dictionary<Assembly, HashSet<Type>>();

            if (allowMultithreadedInitialization)
            {
                const int PreferredNumberOfThreads = 20;

                var tasks = new List<Task<List<KeyValuePair<Assembly, HashSet<Type>>>>>();
                var taskLists = new List<List<Assembly>>();

                var assemblyCount = assemblies.Count;
                var assembliesPerBatch = (int)Math.Ceiling(assemblyCount / (double)PreferredNumberOfThreads);
                var batchCount = (int)Math.Ceiling(assemblyCount / (double)assembliesPerBatch);

                for (var i = 0; i < batchCount; i++)
                {
                    var taskList = new List<Assembly>();

                    var startIndex = (assembliesPerBatch * i);
                    var endIndex = Math.Min(assembliesPerBatch * (i + 1), assemblyCount);

                    for (var j = startIndex; j < endIndex; j++)
                    {
                        taskList.Add(assemblies[j]);
                    }

                    taskLists.Add(taskList);
                }

                for (var i = 0; i < taskLists.Count; i++)
                {
                    var taskList = taskLists[i];

                    var task = TaskRunner.Run(() =>
                    {
                        var taskResults = new List<KeyValuePair<Assembly, HashSet<Type>>>();

                        foreach (var assembly in taskList)
                        {
                            var assemblyTypes = assembly.GetAllTypesSafely();
                            taskResults.Add(new KeyValuePair<Assembly, HashSet<Type>>(assembly, new HashSet<Type>(assemblyTypes)));
                        }

                        return taskResults;
                    });

                    tasks.Add(task);
                }

                var waitTask = Task.WhenAll(tasks);
                waitTask.Wait();

                foreach (var task in tasks)
                {
                    var results = task.Result;

                    foreach (var result in results)
                    {
                        dictionary[result.Key] = result.Value;
                    }
                }
            }
            else
            {
                var types = (from assembly in assemblies
                             select new KeyValuePair<Assembly, HashSet<Type>>(assembly, new HashSet<Type>(assembly.GetAllTypesSafely())));

                var results = types.AsParallel();

                return results.ToDictionary(p => p.Key, p => p.Value);
            }

            return dictionary;
        }

        private static void InitializeType(Assembly assembly, Type type)
        {
            if (ShouldIgnoreType(assembly, type))
            {
                return;
            }

            var newAssemblyName = TypeHelper.GetAssemblyNameWithoutOverhead(assembly.FullName);
            var newFullType = TypeHelper.FormatType(newAssemblyName, type.FullName);

            if (!_typesByAssembly.TryGetValue(newAssemblyName, out var typesByAssembly))
            {
                typesByAssembly = new Dictionary<string, Type>();
                _typesByAssembly[newAssemblyName] = typesByAssembly;
            }

            if (!typesByAssembly.ContainsKey(newFullType))
            {
                typesByAssembly[newFullType] = type;

                _typesWithAssembly[newFullType] = type;
                _typesWithAssemblyLowerCase[newFullType] = type;

                var typeNameWithoutAssembly = TypeHelper.GetTypeName(newFullType);
                _typesWithoutAssembly[typeNameWithoutAssembly] = newFullType;
                _typesWithoutAssemblyLowerCase[typeNameWithoutAssembly] = newFullType;
            }
        }

        private static bool ShouldIgnoreAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return true;
            }

            if (assembly.ReflectionOnly)
            {
                return true;
            }

            var assemblyFullName = assembly.FullName;
            if (assemblyFullName.Contains("Anonymously Hosted DynamicMethods Assembly"))
            {
                return true;
            }

            foreach (var evaluator in ShouldIgnoreAssemblyEvaluators)
            {
                if (evaluator.Invoke(assembly))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldIgnoreType(Assembly assembly, Type type)
        {
            if (type == null)
            {
                return true;
            }

            var typeName = type.FullName;

            // CTL-653
            if (string.IsNullOrEmpty(typeName))
            {
                // Some types don't have a name (for example, after obfuscation), let's ignore these
                return true;
            }

            // Ignore useless types
            if (typeName.Contains("<PrivateImplementationDetails>") ||
                typeName.Contains("+<") || // C# compiler generated classes
                typeName.Contains("+_Closure") || // VB.NET compiler generated classes
                typeName.Contains(".__") || // System.Runtime.CompilerServices.*
                typeName.Contains("Interop+") || // System.IO.FileSystem, System.Net.Sockets, etc
                typeName.Contains("c__DisplayClass") ||
                typeName.Contains("d__") ||
                typeName.Contains("f__AnonymousType") ||
                typeName.Contains("o__") ||
                typeName.Contains("__DynamicallyInvokableAttribute") ||
                typeName.Contains("ProcessedByFody") ||
                typeName.Contains("FXAssembly") ||
                typeName.Contains("ThisAssembly") ||
                typeName.Contains("AssemblyRef") ||
                typeName.Contains("MS.Internal") ||
                typeName.Contains("::") ||
                typeName.Contains("\\*") ||
                typeName.Contains("_extraBytes_") ||
                typeName.Contains("CppImplementationDetails"))
            {
                return true;
            }

            if (typeName.Contains("System.Data.Metadata.Edm.") ||
                typeName.Contains("System.Data.EntityModel.SchemaObjectModel."))
            {
                return true;
            }

            foreach (var evaluator in ShouldIgnoreTypeEvaluators)
            {
                if (evaluator.Invoke(assembly, type))
                {
                    return true;
                }
            }

            return false;
        }
    }
}