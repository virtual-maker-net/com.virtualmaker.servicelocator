using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualMaker
{
    public interface IService
    {
    }

    [ExecuteAlways]
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;

        private Dictionary<Type, IService> _services = new();

        private void RegisterServices()
        {
            _instance = this;
            _services.Clear();

            foreach (var service in gameObject.GetComponentsInChildren<IService>())
            {
                var monoBehaviour = service as MonoBehaviour;
                if (service is IService s && !_services.TryAdd(s.GetType(), s))
                {
                    Debug.LogError($"{s.GetType().Name} attached to {monoBehaviour.name} is already registered to service locator!");
                }
            }
        }

        private static ServiceLocator GetOrCreate()
        {
            if (_instance == null)
            {
                #if UNITY_2023_1_OR_NEWER
                    _instance = FindFirstObjectByType<ServiceLocator>();
                #else
                    _instance = FindObjectOfType<ServiceLocator>();
                #endif

                if (_instance != null)
                {
                    _instance.RegisterServices();
                }
            }

            return _instance;
        }

        public static T Get<T>() where T : IService
        {
            var instance = GetOrCreate();

            if (instance == null)
            {
                throw new Exception("ServiceLocator not found");
            }

            if (!instance._services.TryGetValue(typeof(T), out var service))
            {
                // Check if the service was just added in editor.
                if (!Application.isPlaying)
                {
                    instance.RegisterServices();

                    if (instance._services.TryGetValue(typeof(T), out service))
                    {
                        return (T)service;
                    }
                }

                throw new Exception($"Service {typeof(T)} not found");
            }

            return (T)service;
        }
    }
}