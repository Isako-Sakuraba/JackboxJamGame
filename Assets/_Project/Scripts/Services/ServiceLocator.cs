using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// Stores globally accessible runtime services by type.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _services.Clear();
        }

        /// <summary>
        /// Registers a service instance for its type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="service">Service instance to store.</param>
        public static void Register<T>(T service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            Type type = typeof(T);

            if (_services.TryGetValue(type, out object existing) && !ReferenceEquals(existing, service))
            {
                Debug.LogError("ServiceAlreadyRegistered!");
                _services[typeof(T)] = service;
                return;
                //throw new InvalidOperationException($"Service already registered for type {type.Name}");
            }

            _services[typeof(T)] = service;
        }

        /// <summary>
        /// Gets a registered service by type.
        /// </summary>
        /// <typeparam name="T">Service type to resolve.</typeparam>
        /// <returns>The registered service instance.</returns>
        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new Exception($"Service of type {typeof(T)} is not registered.");
        }

        /// <summary>
        /// Tries to get a registered service by type.
        /// </summary>
        /// <typeparam name="T">Service type to resolve.</typeparam>
        /// <param name="service">Receives the service when registered.</param>
        /// <returns><see langword="true"/> when the service is registered.</returns>
        public static bool TryGet<T>(out T service)
        {
            if (_services.TryGetValue(typeof(T), out object obj))
            {
                service = (T)obj;
                return true;
            }

            service = default;
            return false;
        }

        /// <summary>
        /// Removes a registered service by type.
        /// </summary>
        /// <typeparam name="T">Service type to remove.</typeparam>
        public static void Unregister<T>()
        {
            _services.Remove(typeof(T));
        }
    }
}
