using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dasync.Ioc
{
    public class BasicIocContainer : IIocContainer
    {
        protected struct Binding
        {
            public Type DeclarationType;

            public object ImplementationObject;
        }

        protected class ResolveContext
        {
        }

        protected Dictionary<Type, Binding[]> Bindings { get; } = new Dictionary<Type, Binding[]>();

        public virtual void Bind(Type declarationType, Type implementationType)
        {
            if (declarationType == null)
                throw new ArgumentNullException(nameof(declarationType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            var binding = new Binding
            {
                DeclarationType = declarationType,
                ImplementationObject = implementationType
            };
            AddBinding(binding);
        }

        public virtual void Bind(Type declarationType, Func<object> implementationFactory)
        {
            if (declarationType == null)
                throw new ArgumentNullException(nameof(declarationType));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            var binding = new Binding
            {
                DeclarationType = declarationType,
                ImplementationObject = implementationFactory
            };
            AddBinding(binding);
        }

        public virtual void Bind(Type declarationType, object implementationObject)
        {
            if (declarationType == null)
                throw new ArgumentNullException(nameof(declarationType));
            if (implementationObject == null)
                throw new ArgumentNullException(nameof(implementationObject));

            var binding = new Binding
            {
                DeclarationType = declarationType,
                ImplementationObject = implementationObject
            };
            AddBinding(binding);
        }

        public virtual void RemoveBindings(Type declarationType)
        {
            if (declarationType == null)
                throw new ArgumentNullException(nameof(declarationType));

            lock (Bindings)
            {
                Bindings.Remove(declarationType);
            }
        }

        protected virtual void AddBinding(Binding binding)
        {
            lock (Bindings)
            {
                if (Bindings.TryGetValue(binding.DeclarationType, out var allBindings))
                {
                    Array.Resize(ref allBindings, allBindings.Length + 1);
                    allBindings[allBindings.Length - 1] = binding;
                }
                else
                {
                    allBindings = new Binding[] { binding };
                }
                Bindings[binding.DeclarationType] = allBindings;
            }
        }

        public virtual object Resolve(Type declarationType)
        {
            var context = new ResolveContext();
            return ResolveInternal(declarationType, context);
        }

        public virtual object[] ResolveAll(Type declarationType)
        {
            var context = new ResolveContext();
            return ResolveAllInternal(declarationType, context);
        }

        protected virtual object ResolveInternal(Type declarationType, ResolveContext context)
        {
            if (Bindings.TryGetValue(declarationType, out var allBindings) && allBindings.Length > 1)
                throw new InvalidOperationException(
                    $"More than 1 binding is available for '{declarationType}'.");

            var allResoluctions = ResolveAllInternal(declarationType, context);
            if (allResoluctions?.Length > 0)
                return allResoluctions[0];

            throw new InvalidOperationException($"Cannot resolve type '{declarationType}'.");
        }

        protected virtual object[] ResolveAllInternal(Type declarationType, ResolveContext context)
        {
            if (Bindings.TryGetValue(declarationType, out var allBindings))
            {
                var resolvedObjects = new object[allBindings.Length];

                for (var i = 0; i < allBindings.Length; i++)
                {
                    ref var binding = ref allBindings[i];
                    object resolvedObject;

                    switch (binding.ImplementationObject)
                    {
                        case Type implementationType:
                            if (declarationType == implementationType)
                                resolvedObject = BuildInstanceOfSelfBounfType(declarationType, context);
                            else
                                resolvedObject = ResolveInternal(implementationType, context);
                            binding.ImplementationObject = resolvedObject;
                            break;

                        case Func<object> implementationFactory:
                            resolvedObject = implementationFactory();
                            break;

                        default:
                            resolvedObject = binding.ImplementationObject;
                            break;
                    }

                    resolvedObjects[i] = resolvedObject;
                }

                return resolvedObjects;
            }
            else if (IsSelfBoundType(declarationType))
            {
                var resolvedObject = BuildInstanceOfSelfBounfType(declarationType, context);
                return new object[] { resolvedObject };
            }
            else
            {
                return new object[0];
            }
        }

        protected virtual ConstructorInfo SelectConstructor(Type type, ResolveContext context)
        {
            foreach (var ctorInfo in type.GetTypeInfo().DeclaredConstructors)
            {
                if (ctorInfo.IsPublic)
                    return ctorInfo;
            }
            throw new InvalidOperationException(
                $"Could not find a constructor to create an instance of '{type}'.");
        }

        private static bool IsSelfBoundType(Type type)
        {
            return type.GetTypeInfo().IsClass &&
                !type.GetTypeInfo().IsAbstract &&
                !type.GetTypeInfo().IsGenericTypeDefinition;
        }

        private object BuildInstanceOfSelfBounfType(Type type, ResolveContext context)
        {
            var ctorInfo = SelectConstructor(type, context);
            var parametersInfo = ctorInfo.GetParameters();
            var parameterValues = new object[parametersInfo.Length];

            for (var i = 0; i < parametersInfo.Length; i++)
            {
                var parameterInfo = parametersInfo[i];
                object parameterValue;

                if (parameterInfo.ParameterType.IsArray)
                {
                    var elementType = parameterInfo.ParameterType.GetElementType();
                    var allResolutions = ResolveAllInternal(elementType, context);
                    var allValues = Array.CreateInstance(elementType, allResolutions.Length);
                    Array.Copy(allResolutions, allValues, allResolutions.Length);
                    parameterValue = allValues;
                }
                else
                {
                    parameterValue = ResolveInternal(parameterInfo.ParameterType, context);
                }

                parameterValues[i] = parameterValue;
            }

            return ctorInfo.Invoke(parameterValues);
        }
    }
}
