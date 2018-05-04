using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Activation.Caching;
using Ninject.Parameters;
using Ninject.Planning;
using Ninject.Planning.Bindings;

namespace Dasync.Ioc.Ninject
{
    public sealed class NinjectIocContainerAdapter : IIocContainer, IAppServiceIocContainer
    {
        public NinjectIocContainerAdapter(IKernel kernel)
        {
            Kernel = kernel;
        }

        public IKernel Kernel { get; private set; }

        public object Resolve(Type serviceType) => Kernel.Get(serviceType);

        public IEnumerable<ServiceBindingInfo> DiscoverServices()
        {
            return Kernel.GetAll<ServiceBindingInfo>();
        }

        public void RebindService(Type serviceType, Func<object> serviceImplementationProvider)
        {
            Kernel.Rebind(serviceType).ToMethod(ctx => serviceImplementationProvider()).InTransientScope();
        }

        public bool TryGetImplementationType(Type serviceType, out Type implementationType)
        {
            var bindings = Kernel.GetBindings(serviceType).ToList();

            foreach (var binding in bindings)
            {
                if (binding.Target == BindingTarget.Type)
                {
                    var request = Kernel.CreateRequest(
                        serviceType,
                        null,
                        Enumerable.Empty<IParameter>(),
                        isOptional: false,
                        isUnique: true);

                    var context = new Context(
                        Kernel,
                        request,
                        binding,
                        Kernel.Components.Get<ICache>(),
                        Kernel.Components.Get<IPlanner>(),
                        Kernel.Components.Get<IPipeline>());

                    var provider = binding.GetProvider(context);
                    implementationType = provider.Type;
                    return true;
                }
            }

            implementationType = null;
            return false;
        }
    }
}
