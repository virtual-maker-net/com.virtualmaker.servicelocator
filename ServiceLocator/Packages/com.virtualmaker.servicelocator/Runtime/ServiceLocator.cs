using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualMaker
{
    public interface IService
    {
        void ServiceEnable();
        void ServiceDisable();
    }

    [ExecuteAlways]
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;

        private Dictionary<Type, IService> _services = new();
        private List<IService> _servicesInOrder = new();

        private void RegisterServices()
        {
            _instance = this;
            _services.Clear();
            _servicesInOrder.Clear();

            foreach (var service in gameObject.GetComponentsInChildren<IService>())
            {
                var monoBehaviour = service as MonoBehaviour;
                if (!_services.TryAdd(service.GetType(), service))
                {
                    Debug.LogError($"{service.GetType().Name} attached to {monoBehaviour.name} is already registered to service locator!");
                    continue;
                }

                _servicesInOrder.Add(service);
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
            if (TryGet(out T service))
            {
                return service;
            }

            if (_instance == null)
            {
                throw new Exception($"Service Locator not found");
            }

            throw new Exception($"Service {typeof(T)} not found");
        }

        public static bool TryGet<T>(out T service) where T : IService
        {
            var instance = GetOrCreate();

            if (instance == null)
            {
                // instance can be null if called while stopping play mode
                // because ServiceLocator is destroyed before other objects.
                service = default!;
                return false;
            }

            if (!instance._services.TryGetValue(typeof(T), out var knownSvc))
            {
                // Check if the service was just added in editor.
                if (!Application.isPlaying)
                {
                    instance.RegisterServices();

                    if (instance._services.TryGetValue(typeof(T), out var svc))
                    {
                        service = (T)svc;
                        return true;
                    }
                }

                service = default!;
                return false;
            }

            service = (T)knownSvc;
            return true;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_servicesInOrder.Count == 0)
            {
                RegisterServices();
            }

            foreach (var service in _servicesInOrder)
            {
                service.ServiceEnable();
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            for (int i = _servicesInOrder.Count - 1; i >= 0; i--)
            {
                _servicesInOrder[i].ServiceDisable();
            }
        }
    }
}