using System;

namespace C__project_Auto_Care.Core.DI
{
    /// <summary>
    /// Specifies the lifetime of a service in the IoC container.
    /// Clean Architecture standard.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// A single instance is created once and shared across all requests.
        /// </summary>
        Singleton,

        /// <summary>
        /// A new instance is created once per scope (e.g. Unit of Work transaction scope).
        /// </summary>
        Scoped,

        /// <summary>
        /// A new instance is created every time the service is requested (e.g. ViewModels).
        /// </summary>
        Transient
    }

    /// <summary>
    /// Describes a service with its service type, implementation, and lifetime.
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public ServiceLifetime Lifetime { get; }
        public Func<IServiceProvider, object> Factory { get; }
        public object Instance { get; internal set; }

        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            Lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, object instance)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Lifetime = ServiceLifetime.Singleton;
        }
    }
}
