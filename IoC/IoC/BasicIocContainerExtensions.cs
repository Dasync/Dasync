using System;
using System.Collections.Generic;

namespace Dasync.Ioc
{
    public static class BasicIocContainerExtensions
    {
        public static BasicIocContainer Load(
            this BasicIocContainer container,
            Dictionary<Type, Type> bindings)
        {
            foreach (var binding in bindings)
                container.Bind(binding.Key, binding.Value);
            return container;
        }

        public static void Rebind(
            this BasicIocContainer container,
            Type declarationType,
            Type implementationType)
        {
            container.RemoveBindings(declarationType);
            container.Bind(declarationType, implementationType);
        }

        public static void Rebind(
            this BasicIocContainer container,
            Type declarationType,
            Func<object> implementationFactory)
        {
            container.RemoveBindings(declarationType);
            container.Bind(declarationType, implementationFactory);
        }

        public static void Rebind(
            this BasicIocContainer container,
            Type declarationType,
            object implementationObject)
        {
            container.RemoveBindings(declarationType);
            container.Bind(declarationType, implementationObject);
        }

        public static T[] ResolveAll<T>(this BasicIocContainer container)
        {
            var resolutions = container.ResolveAll(typeof(T));
            var typedResolutions = new T[resolutions.Length];
            Array.Copy(resolutions, typedResolutions, resolutions.Length);
            return typedResolutions;
        }
    }
}
