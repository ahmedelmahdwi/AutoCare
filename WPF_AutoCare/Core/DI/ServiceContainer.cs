using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace C__project_Auto_Care.Core.DI
{
    /// <summary>
    /// Default implementation of IServiceCollection.
    /// </summary>
    public class ServiceCollection : IServiceCollection
    {
        private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();

        public IServiceCollection Add(ServiceDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            _descriptors.Add(descriptor);
            return this;
        }

        public IServiceCollection AddSingleton<TService>() where TService : class
        {
            return Add(new ServiceDescriptor(typeof(TService), typeof(TService), ServiceLifetime.Singleton));
        }

        public IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService
        {
            return Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton));
        }

        public IServiceCollection AddSingleton<TService>(TService instance) where TService : class
        {
            return Add(new ServiceDescriptor(typeof(TService), instance));
        }

        public IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory) where TService : class
        {
            return Add(new ServiceDescriptor(typeof(TService), sp => factory(sp), ServiceLifetime.Singleton));
        }

        public IServiceCollection AddScoped<TService>() where TService : class
        {
            return Add(new ServiceDescriptor(typeof(TService), typeof(TService), ServiceLifetime.Scoped));
        }

        public IServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService
        {
            return Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped));
        }

        public IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory) where TService : class
        {
            return Add(new ServiceDescriptor(typeof(TService), sp => factory(sp), ServiceLifetime.Scoped));
        }

        public IServiceCollection AddTransient<TService>() where TService : class
        {
            return Add(new ServiceDescriptor(typeof(TService), typeof(TService), ServiceLifetime.Transient));
        }

        public IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService
        {
            return Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient));
        }

        public IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory) where TService : class
        {
            return Add(new ServiceDescriptor(typeof(TService), sp => factory(sp), ServiceLifetime.Transient));
        }

        public IServiceProvider BuildServiceProvider()
        {
            return new ServiceContainer(_descriptors);
        }

        public IEnumerator<ServiceDescriptor> GetEnumerator() => _descriptors.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Lightweight, high-performance, thread-safe Dependency Injection Container.
    /// Supports constructor injection, lifetime scoping, and disposable tracking.
    /// </summary>
    public class ServiceContainer : IServiceProvider
    {
        private readonly Dictionary<Type, ServiceDescriptor> _descriptors;
        private readonly ConcurrentDictionary<Type, object> _singletonInstances = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, object> _scopedInstances;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly object _lock = new object();
        private readonly ServiceContainer _root;
        private bool _isDisposed;

        [ThreadStatic]
        private static HashSet<Type> _resolutionStack;

        public bool IsRoot => _root == null;

        public ServiceContainer(IEnumerable<ServiceDescriptor> descriptors)
        {
            _descriptors = new Dictionary<Type, ServiceDescriptor>();
            foreach (var d in descriptors)
            {
                _descriptors[d.ServiceType] = d; // Last registration wins
                if (d.Instance != null)
                {
                    _singletonInstances[d.ServiceType] = d.Instance;
                }
            }
            _scopedInstances = new ConcurrentDictionary<Type, object>();
            _root = null;
        }

        private ServiceContainer(ServiceContainer root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _descriptors = root._descriptors;
            _singletonInstances = root._singletonInstances;
            _scopedInstances = new ConcurrentDictionary<Type, object>();
        }

        public T GetService<T>()
        {
            object instance = GetService(typeof(T));
            return instance == null ? default : (T)instance;
        }

        public T GetRequiredService<T>()
        {
            object instance = GetService(typeof(T));
            if (instance == null)
            {
                throw new InvalidOperationException($"No service for type '{typeof(T).FullName}' has been registered.");
            }
            return (T)instance;
        }

        public object GetService(Type serviceType)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceContainer));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            // Built-in container resolution
            if (serviceType == typeof(IServiceProvider) || serviceType == typeof(System.IServiceProvider))
            {
                return this;
            }

            if (!_descriptors.TryGetValue(serviceType, out var descriptor))
            {
                // If it's a concrete reference type not registered, try resolving via constructor injection
                if (!serviceType.IsAbstract && !serviceType.IsInterface && serviceType != typeof(string) && !serviceType.IsValueType && !serviceType.IsPrimitive)
                {
                    return CreateInstance(serviceType);
                }
                return null;
            }

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                return _singletonInstances.GetOrAdd(serviceType, _ => CreateAndTrackInstance(descriptor, isSingleton: true));
            }

            if (descriptor.Lifetime == ServiceLifetime.Scoped)
            {
                return _scopedInstances.GetOrAdd(serviceType, _ => CreateAndTrackInstance(descriptor, isSingleton: false));
            }

            // Transient
            return CreateAndTrackInstance(descriptor, isSingleton: false);
        }

        private object CreateAndTrackInstance(ServiceDescriptor descriptor, bool isSingleton)
        {
            object instance;
            if (descriptor.Factory != null)
            {
                instance = descriptor.Factory(this);
            }
            else if (descriptor.ImplementationType != null)
            {
                instance = CreateInstance(descriptor.ImplementationType);
            }
            else if (descriptor.Instance != null)
            {
                return descriptor.Instance;
            }
            else
            {
                throw new InvalidOperationException($"Invalid descriptor for '{descriptor.ServiceType.FullName}'.");
            }

            if (instance is IDisposable disposable)
            {
                if (isSingleton && _root != null)
                {
                    _root.TrackDisposable(disposable);
                }
                else
                {
                    TrackDisposable(disposable);
                }
            }

            return instance;
        }

        private void TrackDisposable(IDisposable disposable)
        {
            lock (_lock)
            {
                if (!_isDisposed)
                {
                    _disposables.Add(disposable);
                }
            }
        }

        private object CreateInstance(Type implementationType)
        {
            if (_resolutionStack == null)
            {
                _resolutionStack = new HashSet<Type>();
            }

            if (_resolutionStack.Contains(implementationType))
            {
                throw new InvalidOperationException($"Circular dependency detected for type '{implementationType.FullName}'.");
            }

            _resolutionStack.Add(implementationType);
            try
            {
                var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .OrderByDescending(c => c.GetParameters().Length)
                    .ToList();

                if (constructors.Count == 0)
                {
                    return Activator.CreateInstance(implementationType);
                }

                foreach (var ctor in constructors)
                {
                    var parameters = ctor.GetParameters();
                    bool canResolveAll = true;
                    var args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        object arg = GetService(paramType);
                        if (arg == null && !parameters[i].HasDefaultValue)
                        {
                            canResolveAll = false;
                            break;
                        }
                        args[i] = arg ?? parameters[i].DefaultValue;
                    }

                    if (canResolveAll)
                    {
                        return ctor.Invoke(args);
                    }
                }

                throw new InvalidOperationException($"Unable to resolve constructor parameters for '{implementationType.FullName}'.");
            }
            finally
            {
                _resolutionStack.Remove(implementationType);
            }
        }

        public IServiceScope CreateScope()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceContainer));
            var scopeContainer = new ServiceContainer(_root ?? this);
            return new ServiceScope(scopeContainer);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed) return;
                _isDisposed = true;

                foreach (var disposable in _disposables)
                {
                    try { disposable.Dispose(); } catch { }
                }
                _disposables.Clear();
                _scopedInstances.Clear();

                if (IsRoot)
                {
                    _singletonInstances.Clear();
                }
            }
        }

        private class ServiceScope : IServiceScope
        {
            private readonly ServiceContainer _scopedContainer;

            public IServiceProvider ServiceProvider => _scopedContainer;

            public ServiceScope(ServiceContainer scopedContainer)
            {
                _scopedContainer = scopedContainer;
            }

            public void Dispose()
            {
                _scopedContainer?.Dispose();
            }
        }
    }
}
