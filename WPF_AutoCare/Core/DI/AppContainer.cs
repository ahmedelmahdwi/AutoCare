using System;

namespace C__project_Auto_Care.Core.DI
{
    /// <summary>
    /// Global application dependency container facade.
    /// Provides thread-safe, container-backed service resolution for the application.
    /// </summary>
    public static class AppContainer
    {
        private static IServiceProvider _container;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the current active root IServiceProvider instance.
        /// </summary>
        public static IServiceProvider Current
        {
            get
            {
                if (_container == null)
                {
                    lock (_lock)
                    {
                        if (_container == null)
                        {
                            Initialize();
                        }
                    }
                }
                return _container;
            }
            set
            {
                lock (_lock)
                {
                    _container = value;
                }
            }
        }

        /// <summary>
        /// Initializes the container with the specified database connection string.
        /// </summary>
        public static void Initialize(string connectionString = "")
        {
            lock (_lock)
            {
                _container?.Dispose();
                var services = new ServiceCollection();
                services.AddAutoCareCoreServices(connectionString);
                _container = services.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Resets and disposes the current container.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _container?.Dispose();
                _container = null;
            }
        }

        /// <summary>
        /// Resolves a registered service of type T.
        /// </summary>
        public static T Resolve<T>() => Current.GetRequiredService<T>();

        /// <summary>
        /// Resolves a registered service by Type.
        /// </summary>
        public static object Resolve(Type serviceType) => Current.GetService(serviceType);

        /// <summary>
        /// Creates a new lifetime scope for scoped dependencies (such as Unit of Work).
        /// </summary>
        public static IServiceScope CreateScope() => Current.CreateScope();
    }
}
