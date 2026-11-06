using System;
using System.Collections;
using System.Collections.Generic;

namespace C__project_Auto_Care.Core.DI
{
    /// <summary>
    /// Extended Service Provider interface supporting typed resolution and scopes.
    /// </summary>
    public interface IServiceProvider : System.IServiceProvider, IDisposable
    {
        /// <summary>
        /// Resolves a service of type T, returning null if not found.
        /// </summary>
        T GetService<T>();

        /// <summary>
        /// Resolves a service of type T, throwing an exception if not found.
        /// </summary>
        T GetRequiredService<T>();

        /// <summary>
        /// Creates a new lifetime scope for scoped services.
        /// </summary>
        IServiceScope CreateScope();
    }

    /// <summary>
    /// Encapsulates a lifetime scope. Disposing the scope disposes all scoped services.
    /// </summary>
    public interface IServiceScope : IDisposable
    {
        IServiceProvider ServiceProvider { get; }
    }

    /// <summary>
    /// Service collection contract for registering dependencies.
    /// </summary>
    public interface IServiceCollection : IEnumerable<ServiceDescriptor>
    {
        IServiceCollection Add(ServiceDescriptor descriptor);

        IServiceCollection AddSingleton<TService>() where TService : class;
        IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService;
        IServiceCollection AddSingleton<TService>(TService instance) where TService : class;
        IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory) where TService : class;

        IServiceCollection AddScoped<TService>() where TService : class;
        IServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService;
        IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory) where TService : class;

        IServiceCollection AddTransient<TService>() where TService : class;
        IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService;
        IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory) where TService : class;

        IServiceProvider BuildServiceProvider();
    }
}
