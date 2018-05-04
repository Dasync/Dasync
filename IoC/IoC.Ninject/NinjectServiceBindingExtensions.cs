using System;
using System.Linq;
using Ninject.Activation;
using Ninject.Syntax;

namespace Dasync.Ioc.Ninject
{
    public static class NinjectServiceBindingExtensions
    {
        public static IBindingWithOrOnSyntax<TImplementation> AsService<TImplementation>(
            this IBindingWithSyntax<TImplementation> syntax)
        {
            Type serviceType = null;

            if (typeof(TImplementation).IsClass && !typeof(TImplementation).IsAbstract)
            {
                if (serviceType == null)
                {
                    foreach (var interfaceType in typeof(TImplementation).GetInterfaces())
                    {
                        var bindings = syntax.Kernel.GetBindings(interfaceType).ToList();
                        foreach (var binding in bindings)
                        {
                            if (ReferenceEquals(binding.BindingConfiguration, syntax.BindingConfiguration))
                            {
                                serviceType = interfaceType;
                                break;
                            }
                        }
                        if (interfaceType != null)
                            break;
                    }
                }

                if (serviceType == null)
                {
                    if (typeof(TImplementation).BaseType.IsAbstract)
                    {
                        var bindings = syntax.Kernel.GetBindings(typeof(TImplementation).BaseType).ToList();
                        foreach (var binding in bindings)
                        {
                            if (ReferenceEquals(binding.BindingConfiguration, syntax.BindingConfiguration))
                            {
                                serviceType = typeof(TImplementation).BaseType;
                                break;
                            }
                        }
                    }
                }
            }

            if (serviceType == null)
                throw new Exception();

            var serviceBinding = new ServiceBindingInfo
            {
                ServiceType = serviceType,
                ImplementationType = typeof(TImplementation)
            };

            syntax.Kernel.Bind<ServiceBindingInfo>().ToConstant(serviceBinding);

            return syntax.WithMetadata(nameof(ServiceBindingInfo), serviceBinding);
        }

        public static IBindingWithOrOnSyntax<TService> ToExtrnalService<TService>(this IBindingToSyntax<TService> syntax)
        {
            var serviceBinding = new ServiceBindingInfo
            {
                ServiceType = typeof(TService),
                IsExternal = true
            };

            syntax.Kernel.Bind<ServiceBindingInfo>().ToConstant(serviceBinding);

            return syntax.ToMethod(ThrowOnAttemptToResolve<TService>)
                .WithMetadata(nameof(ServiceBindingInfo), serviceBinding);
        }

        //public static IBindingWithOrOnSyntax<T> AsWorkflow<T>(this IBindingWithSyntax<T> syntax)
        //{
        //    return syntax.WithMetadata("ServiceType", "workflow");
        //}

        private static T ThrowOnAttemptToResolve<T>(IContext context)
        {
            throw new InvalidOperationException();
        }
    }
}
